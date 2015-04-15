using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;
using Entities = KiwiBoard.Entities;

namespace KiwiBoard.BL
{
    public class JobDiagnosticProcessor
    {
        private static object syncObj = new object();

        static JobDiagnosticProcessor()
        {
            Instance = new JobDiagnosticProcessor();
        }

        public static JobDiagnosticProcessor Instance = null;

        public JobDiagnosticProcessor()
        {
            this.EnvironmentMachineMap = Utils.GetEnvironmentMachineMap();
            this.jobStateCache = MemoryCache.Default;
        }

        private ObjectCache jobStateCache;

        public IDictionary<string, string[]> EnvironmentMachineMap { get; set; }

        public Entities.JobStates FetchAllIscopeJobState(string environment, string runtime)
        {
            if (string.IsNullOrEmpty(environment))
            {
                throw new ArgumentNullException();
            }

            var cacheName = string.Join("_", environment, runtime);

            var machines = this.EnvironmentMachineMap.First(e => e.Key.Equals(environment, StringComparison.InvariantCultureIgnoreCase)).Value;

            var stateXml = PhxAutomation.Instance.FetchIscopeJobStateXml(machines, runtime);
            stateXml = string.Join(Environment.NewLine, stateXml.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Where(l => !l.StartsWith("<?xml")));
            stateXml = string.Format("<JobStates Environment=\"{0}\">", environment) + stateXml + "</JobStates>";
            var jobStates = Utils.XmlDeserialize<Entities.JobStates>(stateXml);

            this.jobStateCache[cacheName] = jobStates;

            return jobStates;
        }

        public Entities.JobStatesJobsJob FetchIscopeJobState(string environment, string runtime, string jobId)
        {
            if (string.IsNullOrEmpty(environment) || string.IsNullOrEmpty(runtime) || string.IsNullOrEmpty(jobId))
            {
                throw new ArgumentNullException();
            }

            var cacheName = string.Join("_", environment, runtime);

            Entities.JobStatesJobsJob result = this.FetchJobStateFromCache(cacheName, jobId);

            if (result == null)
            {
                this.FetchAllIscopeJobState(environment, runtime);

                result = this.FetchJobStateFromCache(cacheName, jobId);
            }

            return result;
        }

        public string FetchJobProfile(Entities.JobStatesJobsJob jobState)
        {
            if (jobState == null)
            {
                throw new ArgumentNullException();
            }

            return this.FetchJobProfile(jobState.TargetAPCluster, jobState.TargetCosmosCluster, jobState.Runtime.Value, jobState.Runtime.Dereferenced, jobState.Guid);
        }

        public string FetchJobProfile(string apCluster, string cosmosCluster, string runtime, string runtimeCodeName, string jobId)
        {
            if (string.IsNullOrEmpty(apCluster) || string.IsNullOrEmpty(cosmosCluster) || string.IsNullOrEmpty(runtime) || string.IsNullOrEmpty(runtimeCodeName) || string.IsNullOrEmpty(jobId))
            {
                throw new ArgumentNullException();
            }

            var jmMachines = Utils.GetFunctionMachines(apCluster, cosmosCluster, "JM");
            var profileTxt = PhxAutomation.Instance.FetchProfileLog(cosmosCluster, runtime, runtimeCodeName, jobId, jmMachines);

            return profileTxt;
        }

        public IEnumerable<Entities.CsLog> FetchCsLogs(string apCluster, string cosmosCluster, DateTime startTime, DateTime endTime, string searchPattern = "*")
        {
            if (string.IsNullOrEmpty(apCluster) || string.IsNullOrEmpty(cosmosCluster) || startTime == null || endTime == null || endTime <= startTime)
            {
                throw new ArgumentException();
            }

            var jmMachines = Utils.GetFunctionMachines(apCluster, cosmosCluster, "JM");
            return PhxAutomation.Instance.FetchCsLogEntries(startTime, endTime, searchPattern, jmMachines);
        }

        public IEnumerable<Entities.CsLog> FetchJmDispatcherLog(string apCluster, string cosmosCluster, DateTime startTime, DateTime endTime)
        {
            return this.FetchCsLogs(apCluster, cosmosCluster, startTime, endTime, "cosmosErrorLog_JobManagerDispatcher.exe*");
        }

        #region Private methods
        private Entities.JobStatesJobsJob FetchJobStateFromCache(string cacheName, string jobId)
        {
            Entities.JobStatesJobsJob result = null;

            var cachedJobs = this.jobStateCache[cacheName] as Entities.JobStates;
            if (cachedJobs != null)
            {
                foreach (var jobs in cachedJobs.Jobs)
                {
                    result = jobs.Job.FirstOrDefault(j => j.Guid.Equals(jobId, StringComparison.InvariantCultureIgnoreCase));
                    if (result != null)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Force get job state.xml from PHX machine and put in cache. returns XML content.
        /// </summary>
        private string ForceFetchAllJobStates(string machineName, string runtime)
        {
            if (string.IsNullOrEmpty(machineName) || string.IsNullOrEmpty(runtime))
            {
                throw new ArgumentNullException();
            }

            var cachedXmlFile = this.GetCachedIscopeJobStateXmlPath(machineName, runtime);

            var stateXmlString = string.Empty;

            try
            {
                stateXmlString = PhxAutomation.Instance.FetchIscopeJobStateXml(machineName, runtime);
            }
            catch (PhxAutomationException ex)
            {
                if (ex.ErrorCode == PhxAutomationErrorCode.MachineNotFound)
                {
                    throw new JobNotFoundException(ex.Message);
                }

                throw ex;
            }

            if (!string.IsNullOrEmpty(stateXmlString))
            {
                // write to cache.
                this.WriteIscopeJobStateXmlCache(cachedXmlFile, stateXmlString);
            }
            else
            {
                throw new JobNotFoundException("Job state XML not found or empty. Please check machine and runtime name.");
            }

            return stateXmlString;
        }

        private XElement TryParseDesiredJobStateFromStateXml(string xmlString, string jobId)
        {
            if (string.IsNullOrEmpty(xmlString) || string.IsNullOrEmpty(jobId))
            {
                throw new ArgumentNullException();
            }

            XDocument xdoc = XDocument.Parse(xmlString);
            return xdoc.XPathSelectElements(string.Format("/Jobs/Job[translate(./Guid/text(),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{0}']", jobId.ToLower())).SingleOrDefault();
        }

        private XElement TryParseDesiredJobStateFromCachedFile(string xmlPath, string jobId)
        {
            if (string.IsNullOrEmpty(xmlPath) || string.IsNullOrEmpty(jobId))
            {
                throw new ArgumentNullException();
            }

            if (File.Exists(xmlPath))
            {
                return this.TryParseDesiredJobStateFromStateXml(File.ReadAllText(xmlPath), jobId);
            }

            return null;
        }

        private string GetJobStateFromCachedFile(string filePath, string jobId)
        {
            var jobState = this.TryParseDesiredJobStateFromStateXml(File.ReadAllText(filePath), jobId);
            return jobState == null ? string.Empty : jobState.ToString();
        }

        private string GetCachedIscopeJobStateXmlPath(string machineName, string runtime)
        {
            return string.Format(HttpContext.Current.Server.MapPath(@"~/App_Data/{0}_{1}_iscopestate.xml.cache"), machineName, runtime);
        }

        private void WriteIscopeJobStateXmlCache(string filePath, string content)
        {
            lock (syncObj)
            {
                File.WriteAllText(filePath, content);
            }
        } 
        #endregion
    }

    public class JobNotFoundException : Exception
    {

        public JobNotFoundException() : base() { }
        public JobNotFoundException(string message, params string[] parameters)
            : base(message) { }
    }
}
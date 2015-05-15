using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
            this.EnvironmentMachineMap = Settings.EnvironmentMachineMapping;
        }

        public IDictionary<string, string[]> EnvironmentMachineMap { get; set; }

        public Entities.JobStates FetchJobStatesFromEnvrionment(string environment, string runtime, string machineSearchPattern = "*")
        {
            if (string.IsNullOrEmpty(environment))
            {
                throw new ArgumentNullException("Wrong query parameters!");
            }

            var machines = this.EnvironmentMachineMap.First(e => e.Key.Equals(environment, StringComparison.InvariantCultureIgnoreCase)).Value;
            machines = machines.Where(m => Regex.IsMatch(m, machineSearchPattern)).ToArray();

            if (machines.Length == 0)
            {
                throw new ArgumentException("Machine not found in given envrionment.");
            }

            var stateXmls = PhxAutomation.DefaultInstance.FetchIscopeJobStateXml(runtime, machines);
            var jobXmls = stateXmls.Select(x => Utils.XmlDeserialize<Entities.Jobs>(x.OuterXml)).ToArray();

            return new Entities.JobStates() { Environment = environment, Jobs = jobXmls };
        }

        public string FetchJobProfile(string apCluster, string cosmosCluster, string runtime, string runtimeCodeName, string jobId, out string machineName)
        {
            if (string.IsNullOrEmpty(apCluster) || string.IsNullOrEmpty(cosmosCluster) || string.IsNullOrEmpty(runtime) || string.IsNullOrEmpty(runtimeCodeName) || string.IsNullOrEmpty(jobId))
            {
                throw new ArgumentNullException();
            }

            machineName = string.Empty;
            var jmMachines = Utils.GetFunctionMachines(apCluster, cosmosCluster, "JM");

            var profile = FileCache.Default.TryGetProfile(jobId, out machineName);
            if (profile == null)
            {
                profile = PhxAutomation.DefaultInstance.SearchProfileLog(cosmosCluster, runtime, runtimeCodeName, jobId, out machineName, jmMachines);
                FileCache.Default.SetProfile(profile, jobId, machineName);
            }

            return profile;
        }

        public JobAnalyzer.Job ParseAnalyzerJobFromProfile(string profile)
        {
            using (var profileStream = new MemoryStream(Encoding.UTF8.GetBytes(profile)))
            {
                return JobAnalyzer.ProfileStreamParser.ParseProfileStreamFromStream(profileStream);
            }
        }

        public string GetJobAnalyzerResult(string profileFile)
        {
            if (string.IsNullOrEmpty(profileFile) || !File.Exists(profileFile))
            {
                throw new ArgumentException();
            }

            string workDir = HttpContext.Current.Server.MapPath(@"~/App_Data/jobAnalyzer");

            string script = "$a = import-module " + Settings.JobAnalyzerModule + ";";
            script += Environment.NewLine + "pushd " + workDir + ";";
            script += Environment.NewLine + "$b = Generate-IScopeJobSummary2 -profileFile \"" + profileFile + "\";";
            script += Environment.NewLine + "Get-Content .\\report_iscope.html";
            return string.Join(Environment.NewLine, PhxAutomation.DefaultInstance.RunScript<string>(script).ToArray());
        }

        public IEnumerable<Entities.CsLog> SearchCsLogs(string environment, DateTime startTime, DateTime endTime, string searchPattern = "*")
        {
            if (string.IsNullOrEmpty(environment) || startTime == null || endTime == null || endTime <= startTime)
            {
                throw new ArgumentException("Wrong search parameters!");
            }

            //var jmMachines = Utils.GetFunctionMachines(apCluster, cosmosCluster, "JM");
            var jmMachines = Settings.CsLogEnvironmentMachineMapping.First(kv => kv.Key.Equals(environment, StringComparison.InvariantCultureIgnoreCase)).Value;
            return this.SearchCsLogs(jmMachines, startTime, endTime, searchPattern);
        }

        public IEnumerable<Entities.CsLog> SearchCsLogs(string[] machines, DateTime startTime, DateTime endTime, string searchPattern = "*")
        {
            if (machines == null || machines.Length == 0 || startTime == null || endTime == null || endTime <= startTime)
            {
                throw new ArgumentException("Wrong search parameters!");
            }

            return PhxAutomation.DefaultInstance.FetchCsLogEntries(startTime, endTime, searchPattern, machines);
        }

        public IEnumerable<dynamic> BrowserDirectory(string environment, string path, string machineFunction = "JM")
        {
            if (string.IsNullOrEmpty(environment) || string.IsNullOrEmpty(path) || string.IsNullOrEmpty(machineFunction))
            {
                throw new ArgumentException();
            }

            //var jmMachines = Utils.GetFunctionMachines(apCluster, cosmosCluster, machineFunction);
            var jmMachines = Settings.CsLogEnvironmentMachineMapping.First(kv => kv.Key.Equals(environment, StringComparison.InvariantCultureIgnoreCase)).Value;
            return PhxAutomation.DefaultInstance.ReadPhxFileAsCsv(path, jmMachines);
        }

        #region Private methods
        private Entities.Job FetchJobStateFromCache(string cacheName, string jobId)
        {
            Entities.Job result = null;
            if (string.IsNullOrEmpty(jobId))
            {
                return result;
            }

            var cachedJobs = MemoryCache.Default[cacheName] as Entities.JobStates;
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

    public class NotFoundException : Exception
    {

        public NotFoundException() : base() { }
        public NotFoundException(string message, params string[] parameters)
            : base(message) { }
    }
}
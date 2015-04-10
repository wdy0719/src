using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;

namespace KiwiBoard.BL
{
    public class JobDiagnosticProcessor
    {
        private static object syncObj = new object();

        public static JobDiagnosticProcessor Instance = new JobDiagnosticProcessor();

        public string FetchIscopeJobState(string machineName, string runtime, string jobId = null)
        {
            if (string.IsNullOrEmpty(machineName) || string.IsNullOrEmpty(runtime))
            {
                throw new ArgumentNullException();
            }

            var cachedXmlFile = this.GetCachedIscopeJobStateXmlPath(machineName, runtime);

            if (string.IsNullOrEmpty(jobId))
            {
                return this.ForceFetchAllJobStates(machineName, runtime);
            }
            else
            {
                // parse from cache first
                var desiredJobSate = this.TryParseDesiredJobStateFromCachedFile(cachedXmlFile, jobId);
                if (desiredJobSate != null)
                {
                    return desiredJobSate.ToString();
                }
                else
                {
                    // update cache
                    var downloadedXml = this.ForceFetchAllJobStates(machineName, runtime);
                    var found = this.TryParseDesiredJobStateFromStateXml(downloadedXml, jobId);
                    if (found == null)
                    {
                        throw new JobNotFoundException("Job state not found in state.xml.");
                    }

                    return found.ToString();
                }
            }
        }

        #region Private methods
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
using KiwiBoard.BL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Xml.Linq;
using System.Xml.XPath;

namespace KiwiBoard.Controllers_API
{
    [RoutePrefix("api/PhxUtils")]
    public class PhxUtilsController : ApiController
    {
        private static object syncObj = new object();

        [Route("IscopeJobState")]
        [HttpGet]
        public HttpResponseMessage IscopeJobState(string machineName, string runtime)
        {
            return this.IscopeJobState(machineName, runtime, null);
        }

        [Route("IscopeJobState")]
        [HttpGet]
        public HttpResponseMessage IscopeJobState(string machineName, string runtime, string jobId)
        {
            try
            {
                var stateXmlString = this.FetchIscopeJobState(machineName, runtime, jobId);

                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(stateXmlString) };
            }
            catch (ArgumentException)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest) { ReasonPhrase = "Wrong query parameters! Machine name and runtime cannot be null." };
            }
            catch (JobNotFoundException ex)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound) { ReasonPhrase = ex.Message };
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent(ex.ToString()) };
            }
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
                this.WriteIscopeJobStateXmlCache(cachedXmlFile, stateXmlString).Start();
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

        private string GetJobStateFromCachedFile(string filePath, string jobId)
        {
            var jobState = this.TryParseDesiredJobStateFromStateXml(File.ReadAllText(filePath), jobId);
            return jobState == null ? string.Empty : jobState.ToString();
        }

        private string FetchIscopeJobState(string machineName, string runtime, string jobId = null)
        {
            if (string.IsNullOrEmpty(machineName) || string.IsNullOrEmpty(runtime))
            {
                throw new ArgumentNullException();
            }

            var cachedXmlFile = this.GetCachedIscopeJobStateXmlPath(machineName, runtime);

            if (string.IsNullOrEmpty(jobId))
            {
                // Get all XML, will refresh cached file

                var stateXml = this.ForceFetchAllJobStates(machineName, runtime);
                if (string.IsNullOrEmpty(stateXml))
                {
                    throw new JobNotFoundException("Job state XML not found or empty.");
                }

                return stateXml;
            }
            else
            {
                // parse from cache first
                var desiredJobSate = this.TryParseDesiredJobStateFromStateXml(File.ReadAllText(cachedXmlFile), jobId);
                if (desiredJobSate != null)
                {
                    return desiredJobSate.ToString();
                }

                var downloadedXml = this.ForceFetchAllJobStates(machineName, runtime);
                desiredJobSate = this.TryParseDesiredJobStateFromStateXml(downloadedXml, jobId);
                if (desiredJobSate == null)
                {
                    throw new JobNotFoundException("Job state not found in state.xml.");
                }

                return desiredJobSate.ToString();
            }
        }

        private string GetCachedIscopeJobStateXmlPath(string machineName, string runtime)
        {
            return string.Format(HttpContext.Current.Server.MapPath(@"~/App_Data/{0}_{1}_iscopestate.xml.cache"), machineName, runtime);
        }

        private Task<bool> WriteIscopeJobStateXmlCache(string filePath, string content)
        {
            return Task.Run<bool>(() =>
            {
                lock (syncObj)
                {
                    File.WriteAllText(filePath, content);
                    return true;
                }
            });
        }
    }

    public class JobNotFoundException : Exception
    {

        public JobNotFoundException() : base() { }
        public JobNotFoundException(string message, params string[] parameters)
            : base(message) { }
    }
}

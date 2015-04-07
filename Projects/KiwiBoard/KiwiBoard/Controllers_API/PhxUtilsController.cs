using KiwiBoard.BL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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

        private PhxAutomation phxPowershell;
        public PhxUtilsController()
        {
            phxPowershell = new PhxAutomation();
        }

        [Route("IscopeJobState")]
        [HttpGet]
        public HttpResponseMessage IscopeJobState(string machineName, string runtime = "iscope_beta")
        {
            try
            {
                if (string.IsNullOrEmpty(machineName) || string.IsNullOrEmpty(runtime))
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest) { ReasonPhrase = "Wrong query parameters!" };
                }

                var stateXmlString = this.phxPowershell.FetchIscopeJobStateXml(machineName, runtime);
                if (!string.IsNullOrEmpty(stateXmlString))
                {
                    // Write to cache
                    this.WriteIscopeJobStateXmlCache(this.GetCachedIscopeJobStateXmlPath(machineName, runtime), stateXmlString);
                }
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(stateXmlString) };
            }
            catch (JobNotFoundException ex)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound) { ReasonPhrase = "Job Not Found", Content = new StringContent(ex.Message) };
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent(ex.ToString()) };
            }
        }

        [Route("IscopeJobState")]
        [HttpGet]
        public HttpResponseMessage IscopeJobState(string jobId, string machineName, string runtime = "iscope_beta")
        {
            try
            {
                if (string.IsNullOrEmpty(jobId) || string.IsNullOrEmpty(machineName) || string.IsNullOrEmpty(runtime))
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest) { ReasonPhrase = "Wrong query parameters!" };
                }

                var stateXmlString = this.FetchIscopeJobState(jobId, machineName, runtime);

                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(stateXmlString) };
            }
            catch (JobNotFoundException ex)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound) { ReasonPhrase = "Job Not Found", Content = new StringContent(ex.Message) };
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent(ex.ToString()) };
            }
        }

        private string FetchIscopeJobState(string jobId, string machineName, string runtime)
        {
            Func<string, XElement> parseJobXml = (xmlString) =>
            {
                XDocument xdoc = XDocument.Parse(xmlString);
                return xdoc.XPathSelectElements(string.Format("/Jobs/Job[translate(./Guid/text(),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{0}']", jobId.ToLower())).SingleOrDefault();
            };

            XElement jobState = null; ;

            var cachedXmlFile = this.GetCachedIscopeJobStateXmlPath(machineName, runtime);

            // Read from cache.
            if (File.Exists(cachedXmlFile) && (jobState = parseJobXml(File.ReadAllText(cachedXmlFile))) != null)
            {
                return jobState.ToString();
            }
            else
            {
                var stateXmlString = this.phxPowershell.FetchIscopeJobStateXml(machineName, runtime);
                if (!string.IsNullOrEmpty(stateXmlString))
                {
                    // write to cache.
                    this.WriteIscopeJobStateXmlCache(cachedXmlFile, stateXmlString);
                }
                else
                {
                    throw new JobNotFoundException("Job State.xml not found.");
                }

                jobState = parseJobXml(stateXmlString);
                if (jobState == null)
                {
                    throw new JobNotFoundException("Job not found in State.xml.");
                }

                return jobState.ToString();
            }
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
    }

    public class JobNotFoundException : Exception
    {

        public JobNotFoundException() : base() { }
        public JobNotFoundException(string message, params string[] parameters)
            : base(message) { }
    }
}

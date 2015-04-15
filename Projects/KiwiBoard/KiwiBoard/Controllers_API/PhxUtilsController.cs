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
                var stateXmlString = JobDiagnosticProcessor.Instance.FetchIscopeJobState(machineName, runtime, jobId);

                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Utils.XmlSerialize(stateXmlString)) };
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

        [HttpGet]
        public Entities.JobStates GetJobStates(string environment, string runtime)
        {
            return this.handleExceptions(() => JobDiagnosticProcessor.Instance.FetchAllIscopeJobState(environment, runtime));
        }

        [HttpGet]
        public Entities.JobStatesJobsJob GetJobStates(string environment, string runtime, string jobId)
        {
            return this.handleExceptions(() => JobDiagnosticProcessor.Instance.FetchIscopeJobState(environment, runtime, jobId));
        }

        [HttpGet]
        public string GetProfile(string environment, string runtime, string runtimeCodeName, string jobId)
        {
            return this.handleExceptions(() => JobDiagnosticProcessor.Instance.FetchJobProfile(Constants.ApCluster, environment, runtime, runtimeCodeName, jobId));
        }

        [HttpGet]
        public IEnumerable<Entities.CsLog> GetJmDispatcherLog(string environment, DateTime startTime, DateTime endTime)
        {
            return this.handleExceptions(() => JobDiagnosticProcessor.Instance.FetchJmDispatcherLog(Constants.ApCluster, environment, startTime, endTime));
        }

        [HttpGet]
        public IEnumerable<Entities.CsLog> GetJmDispatcherLog(string environment, int last =0)
        {
            var now = DateTime.Now;
            return this.handleExceptions(() => JobDiagnosticProcessor.Instance.FetchJmDispatcherLog(Constants.ApCluster, environment, now.AddMinutes(last * -1), now));
        }

        [HttpGet]
        public IEnumerable<Entities.CsLog> GetCsLog(string apCluster, string cosmosCluster, DateTime startTime, DateTime endTime, string searchPattern)
        {
            return this.handleExceptions(() => JobDiagnosticProcessor.Instance.FetchCsLogs(apCluster, cosmosCluster, startTime, endTime, searchPattern));
        }

        private T handleExceptions<T>(Func<T> action)
        {
            try
            {
                return action();
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest) { ReasonPhrase = "Wrong query parameters! Runtime name and job Id cannot be null." });
            }
            catch (JobNotFoundException ex)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound) { ReasonPhrase = ex.Message });
            }
            catch (Exception ex)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent(ex.ToString()) });
            }
        }
    }
}

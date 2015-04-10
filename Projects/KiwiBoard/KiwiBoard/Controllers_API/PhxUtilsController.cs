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
    }
}

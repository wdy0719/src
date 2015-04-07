using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using KiwiBoard.BL;

namespace KiwiBoard.Controllers_API
{
    public class KiwiTestResultController : ApiController
    {
        public async Task<HttpResponseMessage> Add([FromUri]string categories = "Default", [FromUri]string runtime = "Default", [FromUri]string cluster = "Default")
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            string root = HttpContext.Current.Server.MapPath("~/App_Data/KiwiTests");
            var provider = new MultipartFormDataStreamProvider(root);

            try
            {
                await Request.Content.ReadAsMultipartAsync(provider);

                foreach (MultipartFileData file in provider.FileData)
                {
                    // Trace.WriteLine(file.Headers.ContentDisposition.FileName);
                    // Trace.WriteLine("Server file path: " + file.LocalFileName);
                    var testResult = new TestResult();
                    testResult.CreateTime = DateTime.UtcNow;
                    testResult.Categories = categories;
                    testResult.Runtime = runtime;
                    testResult.Cluster = cluster;
                    testResult.TestResultFile = file.LocalFileName;
                    DataProvider.KiwiBoardEntities.TestResults.Add(testResult);
                }

                DataProvider.KiwiBoardEntities.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (System.Exception e)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
        }
    }
}
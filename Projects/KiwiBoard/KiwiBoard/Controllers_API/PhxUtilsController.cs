using KiwiBoard.BL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Script.Serialization;
using System.Xml.Linq;
using System.Xml.XPath;

namespace KiwiBoard.Controllers_API
{
    [RoutePrefix("api/PhxUtils")]
    public class PhxUtilsController : ApiController
    {
        private static object syncObj = new object();

        [HttpGet]
        [Route("JobStates/{environment}/{runtime}/{machine}/{jobId}")]
        public IEnumerable<Entities.JobStatesJobs> JobStates(string environment, string runtime, string machine, string jobId)
        {
            return this.handleExceptions<IEnumerable<Entities.JobStatesJobs>>(() =>
            {
                if (machine.ToLower() == "all")
                {
                    machine = @".*";
                }

                var jobs = JobDiagnosticProcessor.Instance.FetchJobStatesFromEnvrionment(environment, runtime, machine).Jobs;
                if (jobId.ToLower() == "all")
                {
                    return jobs;
                }
                else
                {
                    var result = new List<Entities.JobStatesJobs>();
                    foreach (var job in jobs)
                        foreach (var jobstate in job.Job)
                        {
                            if (jobstate.Guid.ToLower() == jobId.ToLower())
                            {
                                var expectedJobs = new Entities.JobStatesJobsJob[] { jobstate };
                                result.Add(new Entities.JobStatesJobs { Job = expectedJobs, MachineName = job.MachineName });
                            }
                        }

                    return result;
                }
            });
        }

        [HttpGet]
        [Route("Test_JobStates/{environment}/{runtime}")]
        public IEnumerable<Entities.JobStatesJobs> Test_JobStates(string environment, string runtime)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            var result = jss.Deserialize<Entities.JobStatesJobs[]>(File.ReadAllText(HttpContext.Current.Server.MapPath(@"~/app_data/jobstates.json")));
            return result;
        }

        [HttpGet]
        public Entities.JobStatesJobsJob GetJobStates(string environment, string runtime, string jobId)
        {
            return this.handleExceptions(() => JobDiagnosticProcessor.Instance.FetchJobStateByIdFromEnvrionment(environment, runtime, jobId));
        }

        [HttpGet]
        public string GetProfile(string apcluster, string environment, string runtime, string runtimeCodeName, string jobId)
        {
            return this.handleExceptions(() => JobDiagnosticProcessor.Instance.FetchJobProfile(apcluster, environment, runtime, runtimeCodeName, jobId));
        }

        [HttpGet]
        public IEnumerable<Entities.CsLog> GetJmDispatcherLog(string apcluster, string environment, DateTime startTime, DateTime endTime)
        {
            return this.handleExceptions(() => JobDiagnosticProcessor.Instance.FetchJmDispatcherLog(apcluster, environment, startTime, endTime));
        }

        [HttpGet]
        public IEnumerable<Entities.CsLog> GetJmDispatcherLog(string apcluster, string environment, int last = 0)
        {
            var now = DateTime.Now;
            return this.handleExceptions(() => JobDiagnosticProcessor.Instance.FetchJmDispatcherLog(apcluster, environment, now.AddMinutes(last * -1), now));
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

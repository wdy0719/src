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
        [HttpGet]
        [Route("JobStates/{environment}/{runtime}/{machine}/{jobId}")]
        public async Task<IEnumerable<Entities.Jobs>> JobStates(string environment, string runtime, string machine, string jobId)
        {
            return await this.handleExceptions<IEnumerable<Entities.Jobs>>(() =>
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
                    var result = new List<Entities.Jobs>();
                    foreach (var job in jobs)
                        if (job.Job != null)
                        {
                            foreach (var jobstate in job.Job)
                            {
                                if (jobstate.Guid.ToLower() == jobId.ToLower())
                                {
                                    var expectedJobs = new Entities.Job[] { jobstate };
                                    result.Add(new Entities.Jobs { Job = expectedJobs, MachineName = job.MachineName });
                                    return result;
                                }
                            }
                        }

                    return result;
                }
            });
        }

        [HttpGet]
        [Route("Test_JobStates/{environment}/{runtime}")]
        public IEnumerable<Entities.Jobs> Test_JobStates(string environment, string runtime)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            var result = jss.Deserialize<Entities.Jobs[]>(File.ReadAllText(HttpContext.Current.Server.MapPath(@"~/app_data/jobstates.json")));
            return result;
        }

        [HttpGet]
        [Route("GetProfile/{apcluster}/{environment}/{runtime}/{runtimeCodeName}/{jobId:guid}")]
        public async Task<dynamic> GetProfile(string apcluster, string environment, string runtime, string runtimeCodeName, string jobId)
        {
            return await this.handleExceptions(() => {
                
                var onMachine=string.Empty;
                var profile =JobDiagnosticProcessor.Instance.FetchJobProfile(apcluster, environment, runtime, runtimeCodeName, jobId, out onMachine);

                return new { Machine = onMachine, Profile = profile };
            });
        }

        [HttpGet]
        [Route("GetProfileTest/{apcluster}/{environment}/{runtime}/{runtimeCodeName}/{jobId:guid}")]
        public async Task<dynamic> GetProfileTest(bool obj = false)
        {
            return await this.handleExceptions(() =>
            {

                var onMachine = "Test";
                var profile = File.ReadAllText(@"C:\Users\v-dayow\Desktop\profile.tmp");

                return new { Machine = onMachine, Profile = profile };
            });
        }

        [HttpGet]
        [Route("GetProfileProcesses/{apcluster}/{environment}/{runtime}/{runtimeCodeName}/{jobId:guid}")]
        public async Task<dynamic> GetProfileProcesses(string apcluster, string environment, string runtime, string runtimeCodeName, string jobId)
        {
            return await this.handleExceptions(() =>
            {

                var onMachine = string.Empty;
                var profile = JobDiagnosticProcessor.Instance.FetchJobProfile(apcluster, environment, runtime, runtimeCodeName, jobId, out onMachine);
                // var profile = File.ReadAllText(@"C:\Users\v-dayow\Desktop\profile.tmp");
                var profileJob = JobDiagnosticProcessor.Instance.ParseAnalyzerJobFromProfile(profile);

                //// unflatted obj
                //var stages = profileJob.Stages.Select(s => new
                //{
                //    StageName = s.StageName,
                //    Vertices = s.Vertices.Select(v => new
                //    {
                //        VertexName = v.Key,
                //        UpVertices = v.Value.UpVertices.Select(upv => upv.VertexName).ToArray(),
                //        DownVertices = v.Value.DownVertices.Select(dnv => dnv.VertexName).ToArray(),
                //        Processes = v.Value.Versions.Select(p => new
                //        {
                //            Guid = p.Guid,
                //            Name = p.Name,
                //            Machine = p.Machine,
                //            RuntimeStats = p.RuntimeStats,
                //            ProcessStartTime = p.ProcessStartTime,
                //            ProcessCompleteTime = p.ProcessCompleteTime,
                //            ExitStatus = p.ExitStatus,
                //            CommentOnVertexLatencies = p.CommentOnVertexLatencies,
                //            CreationReason = p.CreationReason,
                //            CreatorVertexName = p.CreatorVertexName,
                //            TotalDataRead = p.TotalDataRead,
                //            TotalDataWritten = p.TotalDataWritten
                //        }).ToArray()
                //    }).ToArray(),
                //});

                //return new
                //{
                //    Summary = profileJob.JobTimingStats,
                //    IsComplete = profileJob.IsComplete,
                //    Algebra = profileJob.Algebra,
                //    Stages = stages.ToArray(),
                //    CriticalPath = profileJob.CriticalPath().Select(cp => cp.Guid).ToArray()
                //};

                var criticalPath = profileJob.CriticalPath().Select(p=>p.Guid);
                var processes = profileJob.Processes.Select(p => new
                {
                    StageName = p.Vertex.Stage.StageName,
                    VertexName = p.VertexName,
                    Guid = p.Guid,
                    Name = p.Name,
                    Machine = p.Machine,
                    RuntimeStats = p.RuntimeStats,
                    ProcessStartTime = p.ProcessStartTime,
                    ProcessCompleteTime = p.ProcessCompleteTime,
                    ExitStatus = p.ExitStatus,
                    CommentOnVertexLatencies = p.CommentOnVertexLatencies,
                    CreationReason = p.CreationReason,
                    CreatorVertexName = p.CreatorVertexName,
                    TotalDataRead = p.TotalDataRead,
                    TotalDataWritten = p.TotalDataWritten,
                    IsCriticalPath=criticalPath.Contains(p.Guid),

                    UpVertices = p.Vertex.UpVertices.Select(upv => upv.VertexName).ToArray(),
                    DownVertices = p.Vertex.DownVertices.Select(dnv => dnv.VertexName).ToArray(),
                });

                return new
                {
                    Summary = profileJob.JobTimingStats,
                    IsComplete = profileJob.IsComplete,
                    Algebra = profileJob.Algebra,
                    ProfileLocation = onMachine,
                    CriticalPath = profileJob.CriticalPath().Select(cp => cp.Guid).ToArray(),
                    Processes = processes.ToArray()
                };

            });
        }

        [HttpGet]
        [Route("GetJmDispatcherLog/{apcluster}/{environment}/{startTime}/{endTime}")]
        public async Task<IEnumerable<Entities.CsLog>> GetJmDispatcherLog(string apcluster, string environment, DateTime startTime, DateTime endTime)
        {
            return await this.handleExceptions(() => JobDiagnosticProcessor.Instance.FetchJmDispatcherLog(apcluster, environment, startTime, endTime));
        }

        [HttpGet]
        public async Task<IEnumerable<Entities.CsLog>> GetJmDispatcherLog(string apcluster, string environment, int last = 0)
        {
            var now = DateTime.Now;
            return await this.handleExceptions(() => JobDiagnosticProcessor.Instance.FetchJmDispatcherLog(apcluster, environment, now.AddMinutes(last * -1), now));
        }

        [HttpGet]
        [Route("GetCsLog/{apcluster}/{cosmosCluster}")]
        public async Task<IEnumerable<Entities.CsLog>> GetCsLog(string apCluster, string cosmosCluster, string startTime, string endTime, string searchPattern)
        {
            return await this.handleExceptions(() => JobDiagnosticProcessor.Instance.FetchCsLogs(apCluster, cosmosCluster, DateTime.Parse(startTime.Trim('\'')), DateTime.Parse(endTime.Trim('\'')), searchPattern.Trim('\'')));
        }

        private async Task<T> handleExceptions<T>(Func<T> action)
        {
            return await Task.Run(() =>
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
            });
        }
    }
}

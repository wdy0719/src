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
        [Route("GetProfileProcesses/{apcluster}/{environment}/{runtime}/{runtimeCodeName}/{jobId:guid}")]
        public async Task<dynamic> GetProfileProcesses(string apcluster, string environment, string runtime, string runtimeCodeName, string jobId)
        {
            return await this.handleExceptions(() =>
            {
                var onMachine = string.Empty;
                var profile = JobDiagnosticProcessor.Instance.FetchJobProfile(apcluster, environment, runtime, runtimeCodeName, jobId, out onMachine);

                if (string.IsNullOrEmpty(profile))
                {
                    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound) { ReasonPhrase = "Profile Not Found." });
                }

                // var profile = File.ReadAllText(@"C:\Users\v-dayow\Desktop\profile.tmp");
                var profileJob = JobDiagnosticProcessor.Instance.ParseAnalyzerJobFromProfile(profile);

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
        [Route("CsLog/{environment}/Category")]
        public async Task<IEnumerable<dynamic>> GetCsLogCategory(string environment)
        {
            return await this.handleExceptions(() =>
            {
                var logFiles = JobDiagnosticProcessor.Instance.BrowserDirectory(environment, "data/Cslogs/local/*.log").ToArray();
                if (logFiles == null)
                    return null;
                return logFiles.OrderBy(f => f.filename).Select(f => Regex.Replace(f.filename, @"_\d+.log", "_*")).Distinct();
            });
        }

        /// <summary>
        /// FE: searchPattern = "cosmosErrorLog_JobManagerDispatcher.exe*"
        /// </summary>
        [HttpGet]
        [Route("CsLog/{environment}/Logs")]
        public async Task<IEnumerable<Entities.CsLog>> GetCsLog(string environment, string machine, string startTime, string endTime, string searchPattern)
        {
            return await this.handleExceptions(() =>
                {
                    if (string.IsNullOrEmpty(environment) && string.IsNullOrEmpty(machine))
                    {
                        throw new ArgumentException("Environment or machine name not specified!");
                    }

                    DateTime start = DateTime.MinValue, end = DateTime.MinValue;
                    if (string.IsNullOrEmpty(searchPattern) || !DateTime.TryParse(startTime, out start) || !DateTime.TryParse(endTime, out end) || end <= start)
                    {
                        throw new ArgumentException("Wrong query parameters!");
                    }

                    var jmMachines = string.IsNullOrEmpty(machine) || machine == "*" ? Settings.CsLogEnvironmentMachineMapping.First(kv => kv.Key.Equals(environment, StringComparison.InvariantCultureIgnoreCase)).Value : new string[] { machine };
                    return JobDiagnosticProcessor.Instance.SearchCsLogs(jmMachines, start.AddSeconds(-1), end.AddSeconds(1), searchPattern.Trim('\''));
                });
        }

        private async Task<T> handleExceptions<T>(Func<T> action)
        {
            return await Task.Run(() =>
            {
                try
                {
                    return action();
                }
                catch (ArgumentException ex)
                {
                    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest) { ReasonPhrase = ex.Message });
                }
                catch (NotFoundException ex)
                {
                    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound) { ReasonPhrase = ex.Message });
                }
                catch (HttpResponseException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent(ex.ToString()) });
                }
            });
        }
    }
}

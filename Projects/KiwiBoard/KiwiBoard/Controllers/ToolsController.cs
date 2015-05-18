using KiwiBoard.Models.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using KiwiBoard.BL;
using System.Text;

namespace KiwiBoard.Controllers
{
    [RoutePrefix("tools")]
    public class ToolsController : Controller
    {
        [HttpGet]
        [Route("")]
        [Route("JobState")]
        [Route("JobState/{environment}")]
        public ActionResult JobStateView(string environment)
        {
            var model = new JobStateViewModel
            {
                Environment = environment ?? Settings.Environments.First(),
                Machines=new List<string>()
            };

            model.Machines.Add("*");
            model.Machines.AddRange(Settings.EnvironmentMachineMapping[model.Environment]);

            return View(model);
        }

        [HttpGet]
        [Route("JobProfile/{apCluster}/{environment}/{runtime}/{dereferencedRuntime}/{jobId}")]
        public ActionResult JobProfileView(string apCluster, string environment, string runtime, string dereferencedRuntime, string jobId)
        {
            return View();
        }

        [HttpGet]
        [Route("JobProfile/{jobId}/download")]
        public ActionResult DownLoadCachedProfile(string jobId)
        {
            var m = string.Empty;
            var profile = FileCache.Default.TryGetProfile(jobId, out m);
            if (string.IsNullOrEmpty(profile))
            {
                return new HttpNotFoundResult("Profile not found!");
            }

            return File(Encoding.Unicode.GetBytes(profile), System.Net.Mime.MediaTypeNames.Application.Octet, string.Format(@"profile_{{{0}}}.txt", jobId));
        }

        [Route("CsLog")]
        [Route("CsLog/{environment}")]
        public ActionResult CsLogView(string environment, CsLogViewModel model)
        {
            if (this.Request.HttpMethod.ToLower() == "post" && ModelState.IsValid)
            {
                if (model.EndTime.Value > model.StartTime.Value)
                {
                    model.SearchUrl = string.Format("/api/PhxUtils/CsLog/{0}/Logs?startTime={1}&endTime={2}&searchPattern={3}&machine={4}", model.Environment, HttpUtility.UrlEncode(model.StartTime.ToString()), HttpUtility.UrlEncode(model.EndTime.ToString()), HttpUtility.UrlEncode(model.SearchPattern), model.Machine);
                }
                else
                {
                    ModelState.AddModelError("EndTime", "EndTime should greater than start time.");
                }
            }

            return View(model);
        }
    }
}
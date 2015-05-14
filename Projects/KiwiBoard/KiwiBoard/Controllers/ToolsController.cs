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
        public FileResult DownLoadCachedProfile(string jobId)
        {
            var m = string.Empty;
            var profile = FileCache.Default.TryGetProfile(jobId, out m);
            if (string.IsNullOrEmpty(profile))
            {
                throw new NotFoundException("Profile not found!");
            }

            return File(Encoding.Unicode.GetBytes(profile), System.Net.Mime.MediaTypeNames.Application.Octet, string.Format(@"profile_{{{0}}}.txt", jobId));
        }

        [HttpGet]
        [Route("CsLog")]
        [Route("CsLog/{environment}")]
        public ActionResult CsLogView(string environment, string startTime, string endTime, string machine, string searchPattern)
        {
            if (!ControllerContext.RouteData.Values.ContainsKey("environment"))
            {
                return View(new CsLogViewModel(null));
            }
            else
            {
                if (Request.QueryString.Count == 0)
                {
                    return View(new CsLogViewModel(environment));
                }
                else
                {
                    return View(new CsLogViewModel(environment, startTime, endTime, machine, searchPattern));
                }
            }
        }

        [HttpGet]
        [Route("test")]
        public ActionResult Test()
        {
            return View();
        }
    }
}
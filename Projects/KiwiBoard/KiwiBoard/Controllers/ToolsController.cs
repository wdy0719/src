using KiwiBoard.Models.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using KiwiBoard.BL;

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
        [Route("CsLog/{apCluster}/{environment}")]
        public ActionResult CsLogView(string apCluster, string environment, string startTime, string endTime, string searchPattern)
        {
            return View();
        }
    }
}
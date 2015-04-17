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
        public ActionResult Index(string env = null)
        {
            try
            {
                return View(new IScopeJobDiagnosticModel(env));
            }
            catch
            {
                return HttpNotFound();
            }
        }

        public ActionResult CsLogs(string env, DateTime? startTime, DateTime? endTime, string searchPattern)
        {
            try
            {
                return View();
            }
            catch
            {
                return HttpNotFound();
            }
        }

        [HttpGet]
        [Route("")]
        [Route("JobStateView")]
        [Route("JobStateView/{environment}")]
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
    }
}
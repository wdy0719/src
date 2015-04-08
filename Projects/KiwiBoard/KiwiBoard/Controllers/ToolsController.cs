using KiwiBoard.Models.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace KiwiBoard.Controllers
{
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
    }
}
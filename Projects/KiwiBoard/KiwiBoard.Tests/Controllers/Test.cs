using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KiwiBoard;
using KiwiBoard.Controllers;
using KiwiBoard.BL;
using System.Xml.Linq;
using KiwiBoard.Controllers_API;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

namespace KiwiBoard.Tests.Controllers
{
    [TestClass]
    public class Test
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
        }

        [TestMethod]
        public void PhxAutomationTest()
        {
            //var stateXml = new PhxAutomation().FetchIscopeJobState("b6d48c4d-f355-47a1-8a67-f36f30adf746","BN4SCH103190147", "Iscope_beta");

        }


        [TestMethod]
        public void FetchIscopeJobStateXml()
        {
           var test = PhxAutomation.Instance.FetchIscopeJobStateXml(new string[] { "BN4SCH103190147", "BN4SCH103190148" }, "IScope_default");

           test =Regex.Replace(test, @"<\?xml.*\?>", Environment.NewLine);
            Assert.IsNotNull(test);
        }

        [TestMethod]
        public void FetchProfileLog()
        {
            var test = PhxAutomation.Instance.FetchProfileLog("kobo04-test-bn2", "iscope_beta", "nazhan_runtime_0319", "076EC92E-A8F5-4E73-ACC1-180102B0747F-", "BN4SCH103200743", "BN4SCH103200843");

            // Assert
            Assert.IsNotNull(test);
        }

        [TestMethod]
        public void GetProfile()
        {
            var test = JobDiagnosticProcessor.Instance.FetchJobProfile("bn2", "kobo04-test-bn2", "iscope_beta", "nazhan_runtime_0319", "716dce6f-8931-4efd-a880-20ef6844f029");

            // Assert
            Assert.IsNotNull(test);
        }

        [TestMethod]
        public void FetchJmDispatcherLog()
        {
            var test = JobDiagnosticProcessor.Instance.FetchJmDispatcherLog("bn2", "kobo04-test-bn2", DateTime.Parse("04/13/2015 15:40:00"), DateTime.Parse("04/13/2015 15:45:00"));

            Assert.IsNotNull(test);
        }


        [TestMethod]
        public void ExeScriptInMultiThread()
        {
            var tasks = new List<Task<string>>();
            for (int i = 10; i > 0; i--)
            {
                tasks.Add(Task.Run<string>(() => PhxAutomation.Instance.FetchIscopeJobStateXml(new string[] { "BN4SCH103190147", "BN4SCH103190148" }, "IScope_default")));
            }

            Task.WaitAll(tasks.ToArray());

            Assert.IsTrue(tasks.All(t => t.Result != null));
        }
    }
}

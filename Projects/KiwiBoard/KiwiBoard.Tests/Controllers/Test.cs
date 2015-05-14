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
        public void RunScriptWithDynamicOutput()
        {
            var test = PhxAutomation.DefaultInstance.RunScript<dynamic>(@"'BN4SCH103200843' | Read-PhxFile 'data\JobManagerService\kobo04-test-bn2\iscope_Default\nazhan_runtime_0427\profile_{FE74530A-8574-44A8-89F1-EE8F7394A76E}.txt'").ToArray();

        }

        [TestMethod]
        public void FetchIscopeJobStateXml()
        {
            var test = PhxAutomation.DefaultInstance.FetchIscopeJobStateXml("IScope_default", "BN4SCH103190147", "BN4SCH103190148");

            Assert.IsNotNull(test);
        }

        [TestMethod]
        public void FetchProfileLog()
        {
            var m = string.Empty;
            var test = PhxAutomation.DefaultInstance.SearchProfileLog("kobo04-test-bn2", "iscope_default", "nazhan_runtime_0427", "FE74530A-8574-44A8-89F1-EE8F7394A76E", out m, "BN4SCH103200743", "BN4SCH103200843");

            // Assert
            Assert.IsNotNull(test);
        }

        [TestMethod]
        public void GetProfile()
        {
            var machine = string.Empty;
            var test = JobDiagnosticProcessor.Instance.FetchJobProfile("bn2", "kobo04-test-bn2", "iscope_default", "nazhan_runtime_0429", "22fb30fc-4305-4426-a51c-8ed47a104481", out machine);

            // Assert
            Assert.IsNotNull(test);
        }

        //[TestMethod]
        public void FetchJmDispatcherLog()
        {
            var test = JobDiagnosticProcessor.Instance.SearchCsLogs("kobo04-test-bn2", DateTime.Parse("04/13/2015 15:40:00"), DateTime.Parse("04/13/2015 15:45:00"));

            Assert.IsNotNull(test);
        }


        [TestMethod]
        public void GetJobAnalyzerResult()
        {
            var test = JobDiagnosticProcessor.Instance.GetJobAnalyzerResult(@"C:\Users\v-dayow\Desktop\profile_{076EC92E-A8F5-4E73-ACC1-180102B0747F}.txt");
            
            Assert.IsNotNull(test);
        }

        [TestMethod]
        public void JobAnalyzerTest()
        {
            var profile = @"C:\Users\v-dayow\Desktop\profile.tmp";
           // var profileString = PhxAutomation.Instance.RunScript(@"'BN4SCH103200843' | Read-PhxFile 'data\JobManagerService\kobo04-test-bn2\iscope_Default\nazhan_runtime_0504_disablebroadcast2\profile_{2088fdaf-465a-4887-8f83-f81709c75eda}.txt'");
            //File.WriteAllText(profile, profileString);
            var profileString = File.ReadAllText(profile);
            var output = JobDiagnosticProcessor.Instance.ParseAnalyzerJobFromProfile(profileString);

        }


        [TestMethod]
        public void GetProfileObj()
        {
            var test = new PhxUtilsController().GetProfileProcesses("bn2", "kobo04-test-bn2", "IScope_Default", "nazhan_runtime_0505", "3225b9d1-8147-4fe2-b3d5-58a06063b547").Result;
        }

        [TestMethod]
        public void ReadPhxFile()
        {
            var files = PhxAutomation.DefaultInstance.ReadPhxFileAsCsv(@"data\JobManagerService\kobo04-test-bn2\iscope_beta\nazhan_runtime_0319\profile_{*}.txt", "BN4SCH103200743", "BN4SCH103200843").ToArray();

            var categoires = files.Select(f => f.filename.Split('_')[0]).Distinct();
        }

        public Stream GenerateStreamFromString(string s)
        {
             MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KiwiBoard;
using KiwiBoard.Controllers;
using KiwiBoard.BL;

namespace KiwiBoard.Tests.Controllers
{
    [TestClass]
    public class Test
    {

        [TestMethod]
        public void PhxAutomationTest()
        {
            //var stateXml = new PhxAutomation().FetchIscopeJobState("b6d48c4d-f355-47a1-8a67-f36f30adf746","BN4SCH103190147", "Iscope_beta");

        }


        [TestMethod]
        public void Index()
        {
            // Arrange
            HomeController controller = new HomeController();

            // Act
            ViewResult result = controller.Index() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
        }
    }
}

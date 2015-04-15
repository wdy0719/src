using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace KiwiBoard.BL
{
    public class Constants
    {
        public static string CosmosSrcRoot = @"C:\src\cosmos";
        public static string ApGoldSrcRoot = @"C:\src\apgold";
        public static string ApCluster = WebConfigurationManager.AppSettings["ISCOPEJOBDIAGNOSTIC_CLUSTER"];
        public static string[] Environments = WebConfigurationManager.AppSettings["ISCOPEJOBDIAGNOSTIC_ENVRIONMENT"].Split(',').Select(e => e.Trim()).ToArray(); 

        public static string CoreXTAutomationModule = @"D:\tools\CoreXtAutomationGit\CoreXTAutomation.psd1";
        public static string PhxAutomationModule = @"D:\tools\PhxAutomation\PHXAutomation.psd1";
    }
}
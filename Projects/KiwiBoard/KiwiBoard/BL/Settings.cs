using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace KiwiBoard.BL
{
    public class Settings
    {
        public static string CosmosSrcRoot = @"C:\src\cosmos";
        public static string ApGoldSrcRoot = @"C:\src\apgold";
        public static string[] Environments = WebConfigurationManager.AppSettings["ISCOPEJOBDIAGNOSTIC_ENVRIONMENT"].Split(',').Select(e => e.Split('|')[1].Trim()).ToArray();
        public static string[] Runtimes = WebConfigurationManager.AppSettings["ISCOPEJOBDIAGNOSTIC_RUNTIME"].Split(',').Select(e => e.Trim()).ToArray();
        public static IDictionary<string, string[]> EnvironmentMachineMapping = Utils.GetEnvironmentMachineMap(WebConfigurationManager.AppSettings["ISCOPEJOBDIAGNOSTIC_ENVRIONMENT"]);

        public static string CoreXTAutomationModule = @"D:\tools\CoreXtAutomationGit\CoreXTAutomation.psm1";
        public static string PhxAutomationModule = @"D:\tools\PhxAutomation\PHXAutomation.psm1";
    }
}
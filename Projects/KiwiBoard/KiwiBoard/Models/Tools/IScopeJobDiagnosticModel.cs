using KiwiBoard.BL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace KiwiBoard.Models.Tools
{
    public class IScopeJobDiagnosticModel
    {
        public IScopeJobDiagnosticModel()
        {
            this.Cluster = "Bn2";
            this.Environment = "KoboFrontend04-Test-Bn2";
            this.Machines = this.GetIKIEMachines(this.Cluster, this.Environment);
        }

        public string Cluster { get; set; }
        public string Environment { get; set; }
        public string[] Machines { get; set; }

        private string[] GetIKIEMachines(string cluster, string env)
        {
            var machinesCSV = Path.Combine(Constants.ApGoldSrcRoot, cluster, env, "Machines.csv");
            return File.ReadAllLines(machinesCSV)
                .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("#") && l.Split(',')[2] == "IKFE")
                .Select(line => line.Split(',')[0])
                .ToArray();
        }
    }
}
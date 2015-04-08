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
        public IScopeJobDiagnosticModel(string environment = null)
        {
           var map = this.GetEnvironmentMachineMap(this.Cluster);

           if (environment == null)
           {
               environment = this.Environments.First();
           }

           this.Machines = map[environment];
        }

        public string Cluster { get { return "Bn2"; } }

        public IEnumerable<string> Environments
        {
            get
            {
                yield return "KoboFrontend04-Test-Bn2";
                yield return "KoboFrontend02-Test-Bn2";
            }
        }

        public IEnumerable<string> Machines { get; set; }

        private IDictionary<string, IEnumerable<string>> GetEnvironmentMachineMap(string cluster)
        {
            var dict = new Dictionary<string, IEnumerable<string>>();
            foreach (var env in this.Environments)
            {
                var machinesCSV = Path.Combine(Constants.ApGoldSrcRoot, "autopilotservice", cluster, env, "Machines.csv");


                var machines = File.ReadAllLines(machinesCSV)
                    .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("#") && l.Split(',')[2] == "IKFE")
                    .Select(line => line.Split(',')[0]);

                if (machines.Count() != 0)
                {
                    dict.Add(env, machines);
                }
            }

            return dict;
        }
    }
}
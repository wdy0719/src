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
        static object syncObj = new object();

        static IDictionary<string, IEnumerable<string>> EnvironmentMachineMap = null;

        public IScopeJobDiagnosticModel(string env)
        {
            if (EnvironmentMachineMap == null)
            {
                lock (syncObj)
                {
                    EnvironmentMachineMap = new Dictionary<string, IEnumerable<string>>(Utils.GetEnvironmentMachineMap());
                }
            }

            this.SetEnvironment(env);
        }

        public string SelectedEnvironment { get; set; }

        public string Cluster { get; set; }

        public string[] Environments { get; set; }

        public string[] Machines { get; set; }

        public void SetEnvironment(string env)
        {
            env = string.IsNullOrEmpty(env) ? "*" : env.Trim();

            if (env == "*")
            {
                this.Machines = new string[] { };
            }
            else
            {
                this.Machines = EnvironmentMachineMap.First(m => m.Key.Equals(env, StringComparison.InvariantCultureIgnoreCase)).Value.ToArray();
            }

            this.Environments = EnvironmentMachineMap.Keys.ToArray();

            this.SelectedEnvironment = env;
        }
    }
}
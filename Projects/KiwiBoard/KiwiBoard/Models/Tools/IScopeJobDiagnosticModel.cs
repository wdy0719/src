using KiwiBoard.BL;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace KiwiBoard.Models.Tools
{
    public class IScopeJobDiagnosticModel
    {
        static object syncObj = new object();

        static IDictionary<string, string[]> EnvironmentMachineMap = null;

        public IScopeJobDiagnosticModel()
        {
            this.Init();
        }

        public IScopeJobDiagnosticModel(string env)
        {
            this.SelectedEnvironment = env;
            this.Init();
        }

        [Required(ErrorMessage = "*")]
        public string SelectedEnvironment { get; set; }
      
        [Required(ErrorMessage = "*")]
        public string SelectedRuntime { get; set; }

        [Required(ErrorMessage = "*")]
        [RegularExpression(@"^(([a-zA-Z]|[a-zA-Z][a-zA-Z0-9\-]*[a-zA-Z0-9])\.)*([A-Za-z]|[A-Za-z][A-Za-z0-9\-]*[A-Za-z0-9])$")]
        public string SelectedMachine { get; set; }

        public string SelectedJobId { get; set; }

        public string JobState { get; set; }

        public string Cluster { get; set; }
        public string[] Environments { get; set; }
        public string[] Machines { get; set; }
        public string[] Runtimes { get; set; }

        public void FetchLogs()
        {
            try
            {
                this.JobState = JobDiagnosticProcessor.Instance.FetchIscopeJobState(this.SelectedMachine, this.SelectedRuntime, this.SelectedJobId);
            }
            catch (ArgumentException)
            {
                this.JobState = string.Format("Error: Wrong query parameters!  Machine name and runtime cannot be null.");
            }
            catch (JobNotFoundException ex)
            {
                this.JobState = string.Format("Not Found. {0}", ex.Message);
            }
            catch (Exception ex)
            {
                this.JobState = string.Format("Error: Internal Server Error. \r\n{0}", ex.ToString());
            }
        }

        private void Init()
        {
            if (EnvironmentMachineMap == null)
            {
                lock (syncObj)
                {
                    EnvironmentMachineMap = new Dictionary<string, string[]>(Utils.GetEnvironmentMachineMap());
                    EnvironmentMachineMap.Add("Unknown", new string[] { });
                }
            }

            this.Environments = EnvironmentMachineMap.Keys.ToArray();

            this.Runtimes = WebConfigurationManager.AppSettings["ISCOPEJOBDIAGNOSTIC_RUNTIME"].Split(',');

            if (string.IsNullOrEmpty(this.SelectedEnvironment))
            {
                this.SelectedEnvironment = "Unknown";
            }

            if (string.IsNullOrEmpty(this.SelectedRuntime))
            {
                this.SelectedRuntime = this.Runtimes.First();
            }

            this.Machines = EnvironmentMachineMap.First(m => m.Key.Equals(this.SelectedEnvironment, StringComparison.InvariantCultureIgnoreCase)).Value.ToArray();
        }
    }
}
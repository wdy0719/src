using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KiwiBoard.Models.Tools
{
    public class IScopeJobDiagnosticModel
    {
        public IScopeJobDiagnosticModel()
        {

        }

        public IScopeJobDiagnosticModel(string machineName, string runtime, string jobId)
        {
            this.MachineName = machineName;
            this.RunTime = runtime;
            this.JobId = jobId;
        }

        public string MachineName { get; set; }
        public string RunTime { get; set; }
        public string JobId { get; set; }
        public string JobState { get; set; }
    }
}
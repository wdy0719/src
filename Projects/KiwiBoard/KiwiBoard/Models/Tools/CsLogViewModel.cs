using KiwiBoard.BL;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace KiwiBoard.Models.Tools
{
    public class CsLogViewModel
    {
        static object syncObj = new object();
        public CsLogViewModel():this(null)
        { }

        public CsLogViewModel(string environment = null)
        {
            this.Environment = environment ?? Settings.CsLogEnvironmentMachineMapping.Keys.First();
            this.StartTime = DateTime.Now.AddMinutes(-10);
            this.EndTime = DateTime.Now;
            this.Machine = "*";
            this.SearchPattern = this.GetLogModules().First();
        }

        [Required]
        public string Environment { get; set; }

        [Required]
        public string Machine { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime? StartTime { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime? EndTime { get; set; }

        [Required]
        public string SearchPattern { get; set; }

        public string SearchUrl { get; set; }

        public string[] GetEnvrionments()
        {
            return Settings.CsLogEnvironmentMachineMapping.Keys.ToArray();
        }

        public string[] GetLogModules()
        {
            var env = this.Environment.ToLower();
            if (!System.Runtime.Caching.MemoryCache.Default.Contains(env))
            {
                lock (syncObj)
                {
                    var logFiles = JobDiagnosticProcessor.Instance.BrowserDirectory(env, "data/Cslogs/local/*.log").ToArray();
                    var modules = logFiles.OrderBy(f => f.filename).Select(f => Regex.Replace(f.filename, @"_\d+.log", "_*")).Distinct().Cast<string>().ToArray();
                    System.Runtime.Caching.MemoryCache.Default.Add(env, modules, new DateTimeOffset(DateTime.Now.AddDays(2)));
                }
            }

            return (string[])System.Runtime.Caching.MemoryCache.Default[env];
        }

        public string[] GetMachines()
        {
            return Settings.CsLogEnvironmentMachineMapping.First(kv => kv.Key.Equals(this.Environment, StringComparison.InvariantCultureIgnoreCase)).Value;
        }
    }
}
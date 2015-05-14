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
        public CsLogViewModel(string environment = null)
        {
            this.Environment = environment ?? Settings.CsLogEnvironmentMachineMapping.Keys.First();
            this.StartTime = DateTime.Now.AddMinutes(-10);
            this.EndTime = DateTime.Now;
            this.Machine = "*";
            this.SearchPattern = this.GetLogModules().First();
        }

        public CsLogViewModel(string environment, string startTime, string endTime, string machine, string searchPattern)
        {
            this.Environment = environment;
            this.StartTime = TryParse(startTime);
            this.EndTime = TryParse(endTime);
            this.SearchPattern = searchPattern ?? this.GetLogModules().First();
            this.Machine = machine ?? "*";

            this.SearchUrl = string.Format("/api/PhxUtils/CsLog/{0}/Logs?startTime={1}&endTime={2}&searchPattern={3}&machine={4}", this.Environment, HttpUtility.UrlEncode(this.StartTime.ToString()), HttpUtility.UrlEncode(this.EndTime.ToString()), HttpUtility.UrlEncode(this.SearchPattern), this.Machine);
        }

        public string Environment { get; set; }

        public string Machine { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

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

        private DateTime? TryParse(string text)
        {
            DateTime date;
            if (DateTime.TryParse(text, out date))
            {
                return date;
            }
            else
            {
                return null;
            }
        }
    }
}
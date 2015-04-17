using KiwiBoard.BL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KiwiBoard.Models.Tools
{
    public class CsLogViewModel
    {
        public CsLogViewModel(string cosmosCluster, DateTime startTime, DateTime endTime, string searchPattern)
        {
           // this.ApCluster = Settings.ApCluster;
        }

        public string ApCluster { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }
        public string SearchPattern { get; set; }
    }
}
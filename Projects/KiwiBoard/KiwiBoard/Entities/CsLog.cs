using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Management.Automation;

namespace KiwiBoard.Entities
{
    public class CsLog
    {
        public string PSComputerName { get; set; }
        public string Level { get; set; }
        public DateTime Time { get; set; }
        public string Component { get; set; }
        public string Title { get; set; }
        public string Info { get; set; }
        public string SrcFile { get; set; }
        public string SrcFunc { get; set; }
        public string SrcLine { get; set; }
        public string Pid { get; set; }
        public string Tid { get; set; }
        public string TS { get; set; }
        public string String1 { get; set; }
    }
}
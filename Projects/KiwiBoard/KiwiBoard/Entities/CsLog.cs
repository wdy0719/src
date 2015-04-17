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

    public class CsLogConverter:PSTypeConverter
    {
        public override bool CanConvertFrom(object sourceValue, Type destinationType)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvertTo(object sourceValue, Type destinationType)
        {
            throw new NotImplementedException();
        }

        public override object ConvertFrom(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            throw new NotImplementedException();
        }

        public override object ConvertTo(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            throw new NotImplementedException();
        }
    }
}
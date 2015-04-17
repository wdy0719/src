using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using KiwiBoard.BL;

namespace KiwiBoard.Models.Tools
{
    public class JobStateViewModel
    {
        [Required(ErrorMessage = "*")]
        public string Environment { get; set; }

        public List<string> Machines { get; set; }

        //[Required(ErrorMessage = "*")]
        //public string Runtime { get; set; }

        //[Required(ErrorMessage = "*")]
        //public string JobId { get; set; }
    }
}
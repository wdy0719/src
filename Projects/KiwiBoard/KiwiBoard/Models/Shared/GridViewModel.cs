using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace KiwiBoard.Models.Shared
{
    public class GridViewModel
    {
        public IEnumerable<dynamic> Data { get; set; }

        public string DataUrl { get; set; }

        public bool Pagination { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mvc_VD.Models.FG
{
    public class ExportToExcelReceiFG
    {
        public  string po { get; set; }
        public  string product { get; set; }
        public  string product_nm { get; set; }
        public  int? gr_qty { get; set; }
        public string sts_nm { get; set; }
        public  string bb_no { get; set; }
        public string buyer_qr { get; set; }
        public string lot_date { get; set; }
    }
}


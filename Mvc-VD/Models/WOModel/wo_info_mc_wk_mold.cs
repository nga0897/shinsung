using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mvc_VD.Models.WOModel
{
    public class wo_info_mc_wk_mold
    {
        public int id_actual { get; set; }
        public string code { get; set; }
        public string use_yn { get; set; }
        public int pmid { get; set; }
        public string staff_tp { get; set; }
        public string start_dt { get; set; }
        public string end_dt { get; set; }
        public string type { get; set; }
        public string name { get; set; }
        public string het_ca { get; set; }
    }
}
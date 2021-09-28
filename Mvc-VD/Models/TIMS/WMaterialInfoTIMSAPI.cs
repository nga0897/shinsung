using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mvc_VD.Models.TIMS
{
    public class WMaterialInfoTIMSAPI
    {
        public int stt { get; set; }
        public int psid { get; set; }
        public int? id_actual { get; set; }
        public string staff_id { get; set; }
        public string staff_name { get; set; }
        public int? actualQty { get; set; }
        public string defectQty { get; set; }
        public DateTime? reg_date { get; set; }
        public string reg_date_convert { get { return this.reg_date?.ToString("yyyy-MM-dd"); } }
        public string shift { get; set; }
    }
}
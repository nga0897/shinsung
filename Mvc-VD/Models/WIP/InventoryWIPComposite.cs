using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mvc_VD.Models.WIP
{
    public class InventoryWIPComposite
    {
        public string wmtid { get; set; }
         public string model { get; set; }
        public string product_cd { get; set; }
        public string product_name { get; set; }
        public string at_no { get; set; }
        public string mt_cd { get; set; }
        public string bb_no { get; set; }
        public string quantity { get; set; }
        public string BTP { get; set; }
        public string TP { get; set; }
        public string reg_date { get; set; }
        
      
        //public DateTime? recevingDate { get; set; }
        //public DateTime? rece_wip_dt { get; set; }
        //public string reg_date_convert { get { return this.recevingDate?.ToString("yyyy-MM-dd"); } }
        //public string receving_date_convert { get { return this.rece_wip_dt?.ToString("yyyy-MM-dd HH:mm:ss"); } }
   
    }
}

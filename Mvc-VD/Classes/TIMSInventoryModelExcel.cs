using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mvc_VD.Classes
{
    public class TIMSInventoryModelExcel
    {
        public string Id { get; set; }
        public string model { get; set; }
        public string product_cd { get; set; }
        public string product_nm { get; set; }
   
        public string at_no { get; set; }
       
        public string bobbin { get; set; }
        public string mt_cd { get; set; }
        public string Quantity { get; set; }
        public string HCK { get; set; }
        public string DKT { get; set; }
        public string CKT { get; set; }
        public string HDG { get; set; }
        public string MAPIINGBUYER { get; set; }
      
        public string SORTING { get; set; }
        public string Total { get; set; }
        public string buyer_qr { get; set; }
        public string WMaterialReceivedDate { get; set; }

    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mvc_VD.Classes
{
    public class TIMSInventoryMain
    {
        public string Id { get; set; }
        public string product_nm { get; set; }
        public string product_cd { get; set; }
        public string md_cd { get; set; }
        public string at_no { get; set; }
       
        public int HCK { get; set; }
        public int DKT { get; set; }
        public int HDG { get; set; }
        public int CKT { get; set; }
        public int MAPPINGBUYER { get; set; }
        public int SORTING { get; set; }
       
    }
}
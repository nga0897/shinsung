using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mvc_VD.Classes
{
    public class BuyerQRModel
    {
        public int id { get; set; }
        public string buyer_qr { get; set; }
        public string bx_no { get; set; }
        public string stamp_code { get; set; }
        public string product_code { get; set; }
        public string product_name { get; set; }
        public string vendor_line { get; set; }
        public string lotNo { get; set; }
        public string model { get; set; }
        public int quantity { get; set; }
        public string stamp_name  { get; set; }
        public string part_name  { get; set; }
        public string nsx { get; set; }
        public string hsd { get; set; }
        public string expiry_month { get; set; }
        public string ssver { get; set; }
        public string supplier { get; set; }
        public string vendor_code { get; set; }
        public string nhietdobaoquan { get; set; }
        public string Description { get; set; }
        public System.DateTime? reg_dt { get; set; }
    }
}
﻿

namespace Mvc_VD.Models.FG
{
    public class ShippingFGSortingDetailModel
    {
        public int id { get; set; }
        public string ShippingCode { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public string buyer_qr { get; set; }
        public string location { get; set; }
        public string locationname { get; set; }
        public string CreateId { get; set; }
        public System.DateTime CreateDate { get; set; }
        public string ChangeId { get; set; }
        public System.DateTime ChangeDate { get; set; }

    }
}

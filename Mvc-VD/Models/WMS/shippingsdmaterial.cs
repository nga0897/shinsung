﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mvc_VD.Models.WOModel
{
    public class shippingsdmaterial
    {
        public int? id { get; set; }
        public string sd_no { get; set; }
        public string mt_no { get; set; }
        public Double quantity { get; set; }
        public Double meter { get; set; }
        public string reg_id { get; set; }
        public DateTime reg_dt { get; set; }
   
    }
}
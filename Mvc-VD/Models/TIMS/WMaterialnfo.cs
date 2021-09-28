﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mvc_VD.Models.TIMS
{
    public class WMaterialnfo
    {
        //public int wmtid { get; set; }
        //public DateTime date { get; set; }
        //public string mt_cd { get; set; }
        //public string mt_no { get; set; }
        //public string bbmp_sts_cd { get; set; }
        //public double gr_qty { get; set; }
        //public double gr_qty1{ get; set; }
        //public string mt_qrcode { get; set; }
        //public string lct_cd { get; set; }
        //public string bb_no { get; set; }
        //public string mt_barcode { get; set; }
        //public DateTime chg_dt { get; set; }
        public int wmtid { get; set; }
        public Nullable<int> id_actual { get; set; }
        public Nullable<int> id_actual_oqc { get; set; }
        public string at_no { get; set; }
        public string product { get; set; }
        public string staff_id { get; set; }
        public string staff_id_oqc { get; set; }
        public string machine_id { get; set; }
        public string mt_type { get; set; }
        public string mt_cd { get; set; }
        public string mt_no { get; set; }
        public Nullable<double> gr_qty { get; set; }
        public Nullable<double> gr_qty1 { get; set; }
        public Nullable<double> real_qty { get; set; }
        public Nullable<int> staff_qty { get; set; }
        public string sp_cd { get; set; }
        public string rd_no { get; set; }
        public string sd_no { get; set; }
        public string ext_no { get; set; }
        public string ex_no { get; set; }
        public string dl_no { get; set; }
        public string recevice_dt { get; set; }
        
        public string return_date { get; set; }
        public Nullable<int> alert_NG { get; set; }
        public string expiry_dt { get; set; }
        public string dt_of_receipt { get; set; }
        public string expore_dt { get; set; }
        public string recevice_dt_tims { get; set; }
        public string rece_wip_dt { get; set; }
        public string picking_dt { get; set; }
        public string shipping_wip_dt { get; set; }
        public Nullable<System.DateTime> end_production_dt { get; set; }
        public string lot_no { get; set; }
        public string mt_barcode { get; set; }
        public string mt_qrcode { get; set; }
        public string mt_sts_cd { get; set; }
        public string bb_no { get; set; }
        public string bbmp_sts_cd { get; set; }
        public string lct_cd { get; set; }
        public string lct_sts_cd { get; set; }
        public string from_lct_cd { get; set; }
        public string to_lct_cd { get; set; }
        public string output_dt { get; set; }
        public string input_dt { get; set; }
        public string buyer_qr { get; set; }
        public string orgin_mt_cd { get; set; }
        public string remark { get; set; }
        public string sts_update { get; set; }
        public string use_yn { get; set; }
        public string reg_id { get; set; }
        public string chg_id { get; set; }
        //public System.DateTime reg_dt { get; set; }
        //public System.DateTime chg_dt { get; set; }
        public string date { get; set; }
        //public DateTime reg_dt { get; set; }
        public string reg_dt { get; set; }
        //public DateTime chg_dt { get; set; }
        public string chg_dt { get; set; }
        //public string date { get; set; }
        public string  ShiftName { get; set; }

    }
}
using Mvc_VD.Classes;
using Mvc_VD.Controllers;
using Mvc_VD.Models;
using Mvc_VD.Models.TIMS;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.SessionState;

namespace Mvc_VD.Services
{
    public interface ICreateBuyerQRService
    {
        int Insertstampdetail(stamp_detail item);
        BuyerQRModel GetStamp(int id);
    }

    public class CreateBuyerQRService : ICreateBuyerQRService
    {
        private readonly Entities db;
        private readonly HttpSessionState Session;
        public CreateBuyerQRService(IDbFactory dbFactory)
        {
            db = dbFactory.Init();
            Session = HttpContext.Current.Session;
        }

        public BuyerQRModel GetStamp(int id)
        {
            string QuerySQL = @"SELECT stamp.id, stamp.buyer_qr ,stamp.product_code, stamp.vendor_line, stamp.stamp_code,c.md_cd model,c.part_nm part_name,stamp.standard_qty quantity,stamp.lot_date lotNo,c.expiry_month,c.expiry hsd,stamp.ssver,stamp.vendor_code, c.drawingname as nhietdobaoquan  FROM (SELECT a.*
                            FROM stamp_detail AS a
                            WHERE a.id = @1 ) AS stamp
                            JOIN stamp_master AS b
                            ON stamp.stamp_code = b.stamp_code
                            JOIN d_style_info AS c
                            ON stamp.product_code = c.style_no ";
            return db.Database.SqlQuery<BuyerQRModel>(QuerySQL, new MySqlParameter("1", id)).FirstOrDefault();
        }

        public int Insertstampdetail(stamp_detail item)
        {
            string sql = @"INSERT INTO stamp_detail(buyer_qr,stamp_code,product_code,vendor_code,vendor_line,label_printer,is_sample,pcn,lot_date,serial_number,machine_line,shift,
                standard_qty,is_sent,box_code,reg_id,reg_dt,chg_id,chg_dt, ssver)
            Values(@1,@2,@3,@4,@5,@6,@7,@8,@9,@10,@11,@12,@13,@14,@15,@16,NOW(),@17,NOW(),@18);
                SELECT LAST_INSERT_ID()";

            return db.Database.SqlQuery<int>(sql, new MySqlParameter("@1", item.buyer_qr)
                , new MySqlParameter("@2", item.stamp_code), new MySqlParameter("@3", item.product_code), new MySqlParameter("@4", item.vendor_code)
                , new MySqlParameter("@5", item.vendor_line), new MySqlParameter("@6", item.label_printer), new MySqlParameter("@7", item.is_sample)
                , new MySqlParameter("@8", item.pcn), new MySqlParameter("@9", item.lot_date), new MySqlParameter("@10", item.serial_number)
                , new MySqlParameter("@11", item.machine_line), new MySqlParameter("@12", item.shift), new MySqlParameter("@13", item.standard_qty)
                , new MySqlParameter("@14", item.is_sent), new MySqlParameter("@15", item.box_code), new MySqlParameter("@16", item.reg_id)
                , new MySqlParameter("@17", item.chg_id)
                , new MySqlParameter("18", item.ssver)
                ).FirstOrDefault();
        }
    }
}
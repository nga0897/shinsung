using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Mvc_VD.Models;
using System.Collections;
using System.IO;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using PagedList;
using Mvc_VD.Extensions;
using System.ComponentModel;
using Mvc_VD.Models.Language;

namespace Mvc_VD.Controllers
{
    public class SupplierController : BaseController
    {
        private Entities db = new Entities();
        protected override JsonResult Json(object data, string contentType,
        Encoding contentEncoding, JsonRequestBehavior behavior)
        {
            return new JsonResult()
            {
                Data = data,
                ContentType = contentType,
                ContentEncoding = contentEncoding,
                JsonRequestBehavior = behavior,
                MaxJsonLength = Int32.MaxValue
            };
        }
        //
        // GET: /Supplier/
        #region Supplier_QR_Management
        public ActionResult Supplier_QR_Management()
        {
            return SetLanguage("");
        }

        public ActionResult Getpopupmaterial(string mt_cd, string mt_no)
        {
            var list = db.d_material_info.Where(x => x.del_yn == "N" && x.mt_cd.StartsWith(mt_cd) && x.mt_type == "MMT" && x.mt_no != mt_no).ToList();
            return Json(new { rows = list }, JsonRequestBehavior.AllowGet);

        }

        public JsonResult searchmaterial(Pageing pageing, string type, string name, string code, string start, string end)
        {
            // Get Data from ajax function
            var dateConvert = new DateTime();
            if (DateTime.TryParse(end, out dateConvert))
            {
                end = dateConvert.ToString("yyyy/MM/dd");
            }
            if (DateTime.TryParse(start, out dateConvert))
            {
                start = dateConvert.ToString("yyyy/MM/dd");
            }
            var user = Session["userid"];
            Dictionary<string, string> list = PagingAndOrderBy(pageing, " ORDER BY a.mtid DESC ");
            string tempSql = " SELECT a.barcode,a.mtid, a.mt_cd, a.thick, a.thick_unit, a.stick, a.stick_unit, a.tot_price, a.price_least_unit, a.area, a.area_unit, a.item_vcd, a.qc_range_cd,  "
                + "(select dt_nm from comm_dt where mt_cd='COM004' and dt_cd=mt_type) mt_type_nm, a.mt_type, a.bundle_qty,a.bundle_unit, a.mt_no, a.mt_nm, a.unit_cd, a.mf_cd, a.sp_cd, a.mt_no_origin, a.s_lot_no, a.width, a.gr_qty, a.width_unit, "
                + " a.spec, a.spec_unit, a.price, a.price_unit, a.photo_file, a.re_mark, a.reg_id, a.reg_dt, a.chg_id, a.chg_dt, a.consum_yn consumable, "
                + " (case when a.price IS NULL then '' ELSE a.price END) AS new_price, "
                + " (case when a.spec IS NULL then '' ELSE a.spec END) AS new_spec, "
                + " (case when a.width IS NULL then '' ELSE a.width END) AS new_with,"
                + " (case when a.area IS NULL then '' ELSE a.area END) AS area_all, "
                + " (case when a.tot_price IS NULL then '' ELSE a.tot_price END) AS tot_price_new, "
                + " (case when a.stick IS NULL then '' ELSE a.stick END) AS stick_new, "
                + " (case when a.thick IS NULL then '' ELSE a.thick END) AS thick_new, "
                + " (case when (a.item_vcd IS NULL OR a.item_vcd = '') then '' ELSE (SELECT item_nm FROM qc_item_mt WHERE item_vcd = a.item_vcd)END) AS item_nm, "
                + " (case when (a.qc_range_cd IS NULL OR a.qc_range_cd = '') then '' ELSE (SELECT dt_nm FROM comm_dt WHERE dt_cd = a.qc_range_cd AND mt_cd = 'COM017')END) AS qc_range_cd_nm, "
                + " (case when (a.bundle_unit IS NULL OR a.bundle_unit = '') then '' ELSE (SELECT dt_nm FROM comm_dt WHERE dt_cd = a.bundle_unit AND use_yn = 'Y' AND mt_cd = 'COM027')END) AS bundle_unit_nm, "
                + " ROW_NUMBER() OVER (ORDER BY a.mtid DESC) AS RowNum "
                + " FROM d_material_info AS a "
                + " WHERE a.del_yn='N' AND a.sp_cd='"+user+"' "
                + " AND (a.mt_nm LIKE '%" + name + "%') "
                + " AND (a.mt_type LIKE '%" + type + "%') "
                + " AND (a.mt_no LIKE '%" + code + "%') "
                + " AND ('" + start + "'='' OR DATE_FORMAT(a.reg_dt,'%Y/%m/%d') >= DATE_FORMAT('" + start + "','%Y/%m/%d')) "
                + " AND ('" + end + "'='' OR DATE_FORMAT(a.reg_dt,'%Y/%m/%d') <= DATE_FORMAT('" + end + "','%Y/%m/%d')) "
                ;
            string countSql = " SELECT COUNT(*) FROM ( " + tempSql + " ) a ";
            string viewSql = " SELECT * FROM ( " + tempSql + " ) a WHERE a.RowNum BETWEEN " + list["start"] + " AND " + list["end"] + list["orderBy"];
            return SearchAndPaging(countSql, viewSql, int.Parse(list["index"]), int.Parse(list["size"]));
        }
        public JsonResult SearchAndPaging(string countSql, string viewSql, int pageIndex, int pageSize)
        {
            int totalRecords = db.Database.SqlQuery<int>(countSql).FirstOrDefault();
            int totalPages = (int)Math.Ceiling((float)totalRecords / (float)pageSize);
            var data = new InitMethods().ReturnDataTableNonConstraints(new StringBuilder(viewSql));
            var values = ConvertDataTableToJson(data);
            var result = new
            {
                total = totalPages,
                page = pageIndex,
                records = totalRecords,
                rows = values.Data
            };
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public Dictionary<string, string> PagingAndOrderBy(Pageing pageing, string orderByStr)
        {
            Dictionary<string, string> list = new Dictionary<string, string>();
            int pageIndex = pageing.page;
            int pageSize = pageing.rows;
            int start_r = pageing.page > 1 ? ((pageIndex - 1) * pageSize) : pageing.page;
            int end_r = (pageIndex * pageSize);
            string order_by = pageing.sidx != null ? (" ORDER BY " + pageing.sidx + " " + pageing.sord) : orderByStr;
            list.Add("index", pageIndex.ToString());
            list.Add("size", pageSize.ToString());
            list.Add("start", start_r.ToString());
            list.Add("end", end_r.ToString());
            list.Add("orderBy", order_by);
            return list;
        }
        public JsonResult ConvertDataTableToJson(DataTable data)
        {
            return Json(GetTableRows(data), JsonRequestBehavior.AllowGet);
        }
        public List<Dictionary<string, object>> GetTableRows(DataTable data)
        {
            var lstRows = new List<Dictionary<string, object>>();
            Dictionary<string, object> dictRow = null;

            foreach (DataRow row in data.Rows)
            {
                dictRow = new Dictionary<string, object>();
                foreach (DataColumn column in data.Columns)
                {
                    dictRow.Add(column.ColumnName, row[column]);
                }
                lstRows.Add(dictRow);
            }
            return lstRows;
        }

        #endregion

        #region MachiningQR
        public ActionResult MachiningQR()
        {
            return SetLanguage("");
        }
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var lang = HttpContext.Request.Cookies.AllKeys.Contains("language")
              ? HttpContext.Request.Cookies["language"].Value
              : "en";
            var router = this.ControllerContext.RouteData.Values.Values.ElementAt(0).ToString();

            string sqlQuerry = string.Format(@"SELECT keyname,{0} FROM language WHERE router='{1}' or router='public'", lang, router);

            var result = db.Database.SqlQuery<Language>(sqlQuerry).ToList();
            foreach (var item in result)
            {
                if (lang == "en")
                {
                    ViewData.Add(new KeyValuePair<string, object>(item.keyname, item.en));
                }
                else if (lang == "vi")
                {
                    ViewData.Add(new KeyValuePair<string, object>(item.keyname, item.vi));
                }
                else if (lang == "kr")
                {
                    ViewData.Add(new KeyValuePair<string, object>(item.keyname, item.kr));
                }
            }

        }
        public JsonResult searchMachiningQR(Pageing pageing, string type, string name, string code, string start, string end)
        {
            // Get Data from ajax function
            var dateConvert = new DateTime();
            if (DateTime.TryParse(end, out dateConvert))
            {
                end = dateConvert.ToString("yyyy/MM/dd");
            }
            if (DateTime.TryParse(start, out dateConvert))
            {
                start = dateConvert.ToString("yyyy/MM/dd");
            }
            var user = Session["userid"];
            Dictionary<string, string> list = PagingAndOrderBy(pageing, " ORDER BY a.mtid DESC ");
            string tempSql = " SELECT a.barcode,a.mtid, a.mt_cd, a.thick, a.thick_unit, a.stick, a.stick_unit, a.tot_price, a.price_least_unit, a.area, a.area_unit, a.item_vcd, a.qc_range_cd,  "
                + "(select dt_nm from comm_dt where mt_cd='COM004' and dt_cd=mt_type) mt_type_nm, a.mt_type, a.bundle_qty,a.bundle_unit, a.mt_no, a.mt_nm, a.unit_cd, a.mf_cd, a.sp_cd, a.mt_no_origin, a.s_lot_no, a.width, a.gr_qty, a.width_unit, "
                + " a.spec, a.spec_unit, a.price, a.price_unit, a.photo_file, a.re_mark, a.reg_id, a.reg_dt, a.chg_id, a.chg_dt, a.consum_yn consumable, "
                + " (case when a.price IS NULL then '' ELSE a.price END) AS new_price, "
                + " (case when a.spec IS NULL then '' ELSE a.spec END) AS new_spec, "
                + " (case when a.width IS NULL then '' ELSE a.width END) AS new_with,"
                + " (case when a.area IS NULL then '' ELSE a.area END) AS area_all, "
                + " (case when a.tot_price IS NULL then '' ELSE a.tot_price END) AS tot_price_new, "
                + " (case when a.stick IS NULL then '' ELSE a.stick END) AS stick_new, "
                + " (case when a.thick IS NULL then '' ELSE a.thick END) AS thick_new, "
                + " (case when (a.item_vcd IS NULL OR a.item_vcd = '') then '' ELSE (SELECT item_nm FROM qc_item_mt WHERE item_vcd = a.item_vcd)END) AS item_nm, "
                + " (case when (a.qc_range_cd IS NULL OR a.qc_range_cd = '') then '' ELSE (SELECT dt_nm FROM comm_dt WHERE dt_cd = a.qc_range_cd AND mt_cd = 'COM017')END) AS qc_range_cd_nm, "
                + " (case when (a.bundle_unit IS NULL OR a.bundle_unit = '') then '' ELSE (SELECT dt_nm FROM comm_dt WHERE dt_cd = a.bundle_unit AND use_yn = 'Y' AND mt_cd = 'COM027')END) AS bundle_unit_nm, "
                + " ROW_NUMBER() OVER (ORDER BY a.mtid DESC) AS RowNum "
                + " FROM d_material_info AS a "
                + " WHERE a.del_yn='N' AND  a.mt_type ='PMT'  "
                + " AND (a.mt_nm LIKE '%" + name + "%') "
                + " AND (a.mt_type LIKE '%" + type + "%') "
                + " AND (a.mt_no LIKE '%" + code + "%') "
                + " AND ('" + start + "'='' OR DATE_FORMAT(a.reg_dt,'%Y/%m/%d') >= DATE_FORMAT('" + start + "','%Y/%m/%d')) "
                + " AND ('" + end + "'='' OR DATE_FORMAT(a.reg_dt,'%Y/%m/%d') <= DATE_FORMAT('" + end + "','%Y/%m/%d')) "
                ;
            string countSql = " SELECT COUNT(*) FROM ( " + tempSql + " ) a ";
            string viewSql = " SELECT * FROM ( " + tempSql + " ) a WHERE a.RowNum BETWEEN " + list["start"] + " AND " + list["end"] + list["orderBy"];
            return SearchAndPaging(countSql, viewSql, int.Parse(list["index"]), int.Parse(list["size"]));
        }
        #endregion
    }
}

using ClosedXML.Excel;
using Mvc_VD.Classes;
using Mvc_VD.Models;
using Mvc_VD.Models.FG;
using Mvc_VD.Models.Language;
using Mvc_VD.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace Mvc_VD.Controllers
{

    public class fgwmsController : BaseController
    {
        //
        // GET: /MaterialInformation/
        //private Entities db = new Entities();
        private readonly Entities db;
        private readonly IFGWmsService _iFGWmsService;

        #region Constructor
        public fgwmsController(IFGWmsService iFGWmsService, IDbFactory DbFactory)
        {
            _iFGWmsService = iFGWmsService;
            db = DbFactory.Init();
        }
        #endregion

        #region Init methods

        private JsonResult ConvertDataTableToJson(DataTable data)
        {
            return Json(GetTableRows(data), JsonRequestBehavior.AllowGet);
        }

        public List<Dictionary<string, object>> GetTableRows(DataTable data)
        {
            var lstRows = new List<Dictionary<string,
             object>>();
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

        private JsonResult SearchAndPaging(string countSql, string viewSql, int pageIndex, int pageSize)
        {
            int totalRecords = db.Database.SqlQuery<int>(countSql).FirstOrDefault();
            int totalPages = (int)Math.Ceiling((float)totalRecords / (float)pageSize);
            var data = new DataTable();
            using (var cmd = db.Database.Connection.CreateCommand())
            {
                db.Database.Connection.Open();
                cmd.CommandText = viewSql;
                using (var reader = cmd.ExecuteReader())
                {
                    data.Load(reader);
                }
            }
            db.Database.Connection.Close();
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

        private Dictionary<string, string> PagingAndOrderBy(Pageing pageing, string orderByStr)
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

        #endregion Init methods
       
        #region Finish_warehouse(M)

        public ActionResult FininshWhlocation()
        {
            return View();
        }

        public ActionResult GetFnWh()
        {
            var vaule = db.lct_info.Where(item => item.lct_cd.StartsWith("003G")).OrderBy
              (item => item.lct_cd).ThenBy(item => item.level_cd).ToList();
            return Json(new { rows = vaule }, JsonRequestBehavior.AllowGet);
        }

        private string CreateId_FG(int id)
        {
            if (id.ToString().Length < 2)
            {
                return string.Format("0{0}", id);
            }

            if (id.ToString().Length < 3)
            {
                return string.Format("{0}", id);
            }
            return string.Empty;
        }

        public ActionResult insertFnWh(lct_info WHL, string lct_nm, string re_mark, int lctno, string use_yn, string real_use_yn, string mv_yn, string nt_yn)
        {
            WHL.mv_yn = mv_yn;
            WHL.nt_yn = nt_yn;
            WHL.use_yn = use_yn;
            WHL.real_use_yn = real_use_yn;
            WHL.sf_yn = WHL.sf_yn;

            if (WHL.sf_yn == null)
            {
                WHL.sf_yn = "N";
            }
            if (WHL.mv_yn == null)
            {
                WHL.mv_yn = "N";
            }
            if (WHL.nt_yn == null)
            {
                WHL.nt_yn = "N";
            }
            string[] listtring = { "0", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
            WHL.lct_nm = lct_nm;
            WHL.order_no = 1;
            WHL.re_mark = re_mark;
            string root_yn = Request.QueryString["c_root_yn"];
            var list = db.lct_info.Where(item => item.lct_cd.StartsWith("003G")).
                  OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();
            var upLevel = db.lct_info.FirstOrDefault(item => item.lctno == lctno);
            if (list.Count == 0)
            {
                WHL.index_cd = "G01";
                WHL.lct_cd = "003G01000000000000";
                WHL.shelf_cd = "0";
                WHL.level_cd = "000";
                WHL.lct_bar_cd = WHL.index_cd + "-" + WHL.shelf_cd;
            }
            else
            {
                if (root_yn == "1")
                {
                    var menuCd = string.Empty;
                    var subMenuIdConvert = 0;
                    var listY = list.Where(item => item.level_cd == "000").ToList().OrderByDescending(item => item.reg_dt).FirstOrDefault();
                    if (listY != null)
                    {
                        var subMenuId = listY.lct_cd.Substring(4, 2);
                        int.TryParse(subMenuId, out subMenuIdConvert);//tra
                    }
                    menuCd = string.Format("{0}{1}", menuCd, CreateId_FG((subMenuIdConvert + 1)));

                    WHL.index_cd = "G" + menuCd;
                    WHL.lct_cd = "003" + WHL.index_cd + "000000000000";
                    WHL.shelf_cd = "0";
                    WHL.level_cd = "000";
                    WHL.lct_bar_cd = WHL.index_cd + "-" + WHL.shelf_cd;
                }
                else
                {
                    WHL.up_lct_cd = upLevel.lct_cd;
                    var Whcd = string.Empty;
                    var subMenuIdConvert = 0;
                    var sublog = new lct_info();
                    var x = "";
                    var l = "";
                    var subnew = "";
                    switch (upLevel.level_cd)
                    {
                        case "000":
                            Whcd = upLevel.lct_cd.Substring(0, 6);
                            WHL.level_cd = "001";
                            sublog = list.Where(item => item.up_lct_cd == upLevel.lct_cd).OrderByDescending(item => item.reg_dt).FirstOrDefault();

                            if (sublog != null)
                            {
                                subnew = sublog.lct_cd.Substring(8, 1);
                                for (int i = 0; i < listtring.Length; i++)
                                {
                                    var m = listtring[i];
                                    if (m == subnew)
                                    {
                                        i++;
                                        subnew = listtring[i];
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                subnew = "A";
                            }
                            Whcd = string.Format("{0}{1}", Whcd, "00" + subnew + "000000000");
                            WHL.lct_cd = Whcd;
                            WHL.index_cd = (Whcd.Substring(3, 3));
                            WHL.shelf_cd = subnew;
                            WHL.lct_bar_cd = WHL.index_cd + "-" + WHL.shelf_cd;
                            WHL.mn_full = upLevel.mn_full + ">" + WHL.lct_nm;
                            break;

                        case "001":
                            Whcd = upLevel.lct_cd.Substring(0, 9);
                            WHL.level_cd = "002";
                            sublog = list.Where(item => item.up_lct_cd == upLevel.lct_cd).OrderByDescending(item => item.reg_dt).FirstOrDefault();
                            if (sublog != null)
                            {
                                var subMenuId = sublog.lct_cd.Substring(9, 3);
                                int.TryParse(subMenuId, out subMenuIdConvert);
                            }
                            Whcd = string.Format("{0}{1}000000", Whcd, "0" + CreateId_FG((subMenuIdConvert + 1)));
                            WHL.lct_cd = Whcd;
                            WHL.index_cd = (Whcd.Substring(3, 3));
                            x = ((Whcd.Substring(14, 1) != "0") ? Whcd.Substring(14, 1) : string.Empty);
                            l = (((Whcd.Substring(16, 2) != "0" && Whcd.Substring(16, 2) != "00")) ? Whcd.Substring(16, 2) : string.Empty);
                            WHL.shelf_cd = Whcd.Substring(8, 1) + Whcd.Substring(10, 2) + x + l;
                            WHL.lct_bar_cd = WHL.index_cd + "-" + WHL.shelf_cd;
                            WHL.mn_full = upLevel.mn_full + ">" + WHL.lct_nm;
                            break;

                        case "002":
                            Whcd = upLevel.lct_cd.Substring(0, 12);
                            WHL.level_cd = "003";
                            sublog = list.Where(item => item.up_lct_cd == upLevel.lct_cd).OrderByDescending(item => item.reg_dt).FirstOrDefault();

                            if (sublog != null)
                            {
                                subnew = sublog.lct_cd.Substring(14, 1);
                                for (int i = 0; i < listtring.Length; i++)
                                {
                                    var m = listtring[i];
                                    if (m == subnew)
                                    {
                                        i++;
                                        subnew = listtring[i];
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                subnew = "A";
                            }
                            Whcd = string.Format("{0}{1}000", Whcd, "00" + subnew);
                            WHL.lct_cd = Whcd;
                            WHL.index_cd = (Whcd.Substring(3, 3));
                            x = ((Whcd.Substring(14, 1) != "0") ? Whcd.Substring(14, 1) : string.Empty);
                            l = (((Whcd.Substring(16, 2) != "0" && Whcd.Substring(16, 2) != "00")) ? Whcd.Substring(16, 2) : string.Empty);
                            WHL.shelf_cd = Whcd.Substring(8, 1) + Whcd.Substring(10, 2) + x + l;
                            WHL.lct_bar_cd = WHL.index_cd + "-" + WHL.shelf_cd;
                            WHL.mn_full = upLevel.mn_full + ">" + WHL.lct_nm;
                            break;

                        case "003":
                            Whcd = upLevel.lct_cd.Substring(0, 15);
                            WHL.level_cd = "004";
                            sublog = list.Where(item => item.up_lct_cd == upLevel.lct_cd).OrderByDescending(item => item.reg_dt).FirstOrDefault();
                            if (sublog != null)
                            {
                                var subMenuId = sublog.lct_cd.Substring(15, 3);
                                int.TryParse(subMenuId, out subMenuIdConvert);
                            }
                            Whcd = string.Format("{0}{1}", Whcd, "0" + CreateId_FG((subMenuIdConvert + 1)));
                            WHL.lct_cd = Whcd;
                            WHL.index_cd = (Whcd.Substring(3, 3));
                            x = ((Whcd.Substring(14, 1) != "0") ? Whcd.Substring(14, 1) : string.Empty);
                            l = (((Whcd.Substring(16, 2) != "0" && Whcd.Substring(16, 2) != "00")) ? Whcd.Substring(16, 2) : string.Empty);
                            WHL.shelf_cd = Whcd.Substring(8, 1) + Whcd.Substring(10, 2) + x + l;
                            WHL.lct_bar_cd = WHL.index_cd + "-" + WHL.shelf_cd;
                            WHL.mn_full = upLevel.mn_full + ">" + WHL.lct_nm;
                            break;

                        case "004":
                            Whcd = upLevel.lct_cd.Substring(0, 3);
                            WHL.level_cd = "000";
                            sublog = list.Where(item => item.level_cd == "000").OrderByDescending(item => item.reg_dt).FirstOrDefault();
                            if (sublog != null)
                            {
                                var subMenuId = sublog.lct_cd.Substring(4, 2);
                                int.TryParse(subMenuId, out subMenuIdConvert);
                            }
                            Whcd = string.Format("{0}{1}", Whcd, "W" + CreateId_FG((subMenuIdConvert + 1)) + "000000000000");

                            WHL.lct_cd = Whcd;
                            WHL.index_cd = (Whcd.Substring(3, 3));
                            WHL.shelf_cd = "0";
                            WHL.lct_bar_cd = WHL.index_cd;
                            WHL.mn_full = upLevel.mn_full + ">" + WHL.lct_nm;
                            break;
                    }
                }
            }
            DateTime reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
            String dateString = reg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");

            DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
            String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
            WHL.reg_dt = reg_dt;
            WHL.chg_dt = chg_dt;
            if (WHL.lct_cd.Contains("001W0100A"))
            {
                WHL.real_use_yn = "N";
            }
            if (ModelState.IsValid)
            {
                db.Entry(WHL).State = EntityState.Added;
                db.SaveChanges();
                return Json(WHL, JsonRequestBehavior.AllowGet);
                ;
            }

            return View(WHL);
        }

        public ActionResult updateFnWh(string lct_nm, string re_mark, int lctno, string use_yn, string real_use_yn, string sf_yn, string mv_yn, string nt_yn)
        {
            // Search user = userid fg_yn
            if (nt_yn == null)
            {
                nt_yn = "N";
            }
            if (sf_yn == null)
            {
                sf_yn = "N";
            }
            if (mv_yn == null)
            {
                mv_yn = "N";
            }

            lct_info b = db.lct_info.Find(lctno);
            b.sf_yn = sf_yn;
            b.nt_yn = nt_yn;
            b.mv_yn = mv_yn;
            b.use_yn = use_yn;
            b.real_use_yn = real_use_yn;
            b.lct_nm = lct_nm;
            b.mn_full = b.lct_nm;
            b.re_mark = re_mark;

            //DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
            //String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
            //b.chg_dt = chg_dt;

            if (ModelState.IsValid)
            {
                db.Entry(b).State = EntityState.Modified;
                db.SaveChanges();

                return Json(b, JsonRequestBehavior.AllowGet);
            }
            return View(b);
        }

        public JsonResult SearchFnWh()
        {
            // Get Data from ajax function
            var W = Request["whouse"];
            var A = Request["aisle"];
            var B = Request["bay"];

            var sql = new StringBuilder();
            sql.Append(" SELECT * ")
            .Append("FROM  lct_info as a ")
            .Append("Where a.index_cd like 'G%' and ('" + A + "'='' OR  (SUBSTRING(a.lct_cd,9,1)) like '%" + A + "%' )")
            .Append("AND ('" + B + "'='' OR  (SUBSTRING(a.lct_cd,11, 2)) like '%" + B + "%' )")
            .Append("AND ('" + W + "'='' OR  a.index_cd like '%" + W + "%')");

            var data = db.lct_info.SqlQuery(sql.ToString()).ToList<lct_info>();
            return Json(data, JsonRequestBehavior.AllowGet);
            ;
        }

        #endregion Finish_warehouse(M)

        #region selected_finish

        public ActionResult fnWarehouse(lct_info lct_info)
        {
            var lists = db.lct_info.Where(item => item.index_cd.StartsWith("G")).
              OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).Select(item => item.index_cd).Distinct().ToList();
            var result = new List<lct_info>();
            foreach (var name in lists)
            {
                var item = new lct_info();
                item.index_cd = name;
                result.Add(item);
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult fnAisle(lct_info lct_info)
        {
            var lists = db.lct_info.Where(item => item.index_cd.StartsWith("G")).
              OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();
            var result = new List<lct_info>();
            foreach (var name in lists)
            {
                var item = new lct_info();
                item.lct_cd = name.lct_cd.Substring(8, 1);
                result.Add(item);
            }
            var result1 = new List<lct_info>();
            var list2 = result.Select(item => item.lct_cd).Distinct().ToList();
            foreach (var name in list2)
            {
                var item = new lct_info();
                item.lct_cd = name;
                if (item.lct_cd != "0")
                {
                    result1.Add(item);
                }
            }
            return Json(result1, JsonRequestBehavior.AllowGet);
        }

        public ActionResult fnBay(lct_info lct_info)
        {
            var lists = db.lct_info.Where(item => item.index_cd.StartsWith("G")).
              OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();
            var result = new List<lct_info>();
            foreach (var name in lists)
            {
                var item = new lct_info();
                item.lct_cd = name.lct_cd.Substring(10, 2);
                if (item.lct_cd != "00")
                {
                    result.Add(item);
                }
            }
            var result1 = new List<lct_info>();
            var list2 = result.Select(item => item.lct_cd).Distinct().ToList();
            foreach (var name in list2)
            {
                var item = new lct_info();
                item.lct_cd = name;
                if (item.lct_cd != "0")
                {
                    result1.Add(item);
                }
            }
            return Json(result1, JsonRequestBehavior.AllowGet);
        }

        #endregion selected_finish

        #region N_Receiving_scan_FG

        public ActionResult Receving_Scan(string code)
        {
            ViewData["Message"] = code;
            return SetLanguage("~/Views/fgwms/Receiving_Scan/Receving_Scan.cshtml");
            
        }

        public ActionResult GetEXTInfo(string ext_no, string ext_nm)
        {
            var sql = new StringBuilder();
            sql.Append(" SELECT a.* ")

               .Append(" FROM w_ext_info as a ")
                .Append(" WHERE a.use_yn ='Y' and (a.alert > 0 OR  a.ext_sts_cd <> '000' ) ")
                 .Append("AND ('" + ext_no + "'='' OR  a.ext_no like '%" + ext_no + "%' )")
                 .Append("AND ('" + ext_nm + "'='' OR  a.ext_nm like '%" + ext_nm + "%' )")

                .Append(" order by extid desc ");

            return new InitMethods().ConvertDataTableToJsonAndReturn(sql);
        }

        public ActionResult GetReceivingScanMLQR(Pageing pageing, string product, string buyer, string po, string lot_date, string lot_date_end)
        {
            StringBuilder varname1 = new StringBuilder();
            varname1.Append("SELECT a.at_no po,a.product,product.style_nm product_nm,  a.wmtid,a.mt_no,a.gr_qty, a.mt_sts_cd,comm.dt_nm sts_nm,  a.mt_cd, a.bb_no, a.buyer_qr, stamp.lot_date \n");

         
            varname1.Append("FROM w_material_info AS a \n");
            varname1.Append(" JOIN stamp_detail AS stamp ON  a.buyer_qr = stamp.buyer_qr \n");
            varname1.Append(" JOIN d_style_info AS product ON a.product = product.style_no \n");
            varname1.Append(" JOIN comm_dt AS comm ON a.mt_sts_cd = comm.dt_cd AND comm.mt_cd='WHS005' \n");

            varname1.Append("WHERE a.mt_sts_cd='010' AND a.lct_cd LIKE '006%'  AND a.buyer_qr IS NOT null ");
            varname1.Append("and ('" + product + "'='' or a.product LIKE '%" + product + "%')");
            varname1.Append("and ('" + buyer + "'='' or a.buyer_qr LIKE '%" + buyer + "%')");
            varname1.Append("and ('" + po + "'='' or a.at_no LIKE '%" + po + "%')");
            varname1.Append("and('" + lot_date + "'='' OR DATE_FORMAT(stamp.lot_date,'%Y/%m/%d') >= DATE_FORMAT('" + lot_date + "','%Y/%m/%d'))");
            varname1.Append("and('" + lot_date_end + "'='' OR DATE_FORMAT(stamp.lot_date,'%Y/%m/%d')  <= DATE_FORMAT('" + lot_date_end + "','%Y/%m/%d'))");

         
            //varname1.Append("and ('" + lot_date + "'='' or stamp.lot_date LIKE '%" + lot_date + "%')");

            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("wmtid"));
            return new InitMethods().ReturnJsonResultWithPaging(pageing, total, result);
        }

        public int CountingReceivingFG()
        {
            StringBuilder sql = new StringBuilder($" SELECT count(a.wmtid) AS isNew FROM w_material_info AS a WHERE a.mt_sts_cd='010' AND a.lct_cd LIKE '006%' ;");
            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);
            int total = 0;
            if (dt.Rows.Count > 0)
            {
                total = int.Parse(dt.Rows[0]["isNew"].ToString());
            }
            return total;
        }

        public ActionResult PartialView_Receving_Scan_Wip_Missing_M_Popup_FG(string extid = "", string ext_no = "", string alert = "")
        {
            ViewBag.extid = extid;
            ViewBag.alert = alert;
            ViewBag.ext_no = ext_no;
            return PartialView("~/Views/fgwms/Receiving_Scan/PartialView_Receving_Scan_Wip_Missing_M_Popup_FG.cshtml");
        }

        public ActionResult Get_List_MissMaterial(string ext_no)
        {
            var datalist = (from a in db.w_material_info
                            where a.ext_no.Equals(ext_no) && a.mt_sts_cd.Equals("000")
                            select new
                            {
                                wmtid = a.wmtid,
                                mt_no = a.mt_no,
                                mt_type = a.mt_type,
                                lot_no = a.lot_no,
                                expiry_dt = a.expiry_dt,
                                dt_of_receipt = a.dt_of_receipt,
                                expore_dt = a.expore_dt,
                                sd_sts_cd = a.mt_sts_cd,
                                sts_nm = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == a.mt_sts_cd).Select(x => x.dt_nm),

                                mt_cd = a.mt_cd,
                                bb_no = a.bb_no,
                                buyer_qr = a.buyer_qr,
                                gr_qty = a.gr_qty,
                                recevice_dt_tims = a.recevice_dt_tims,

                                from_lct_cd = a.from_lct_cd,
                                from_lct_nm = db.lct_info.Where(x => x.lct_cd == a.lct_cd).Select(x => x.lct_nm),

                                lct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002" && x.dt_cd == a.lct_sts_cd).Select(x => x.dt_nm),

                                mt_type_nm = db.comm_dt.Where(x => x.mt_cd == "COM004" && x.dt_cd == a.mt_type).Select(x => x.dt_nm),

                                mt_sts_cd = a.mt_sts_cd,
                            }
                      ).ToList();
            return Json(datalist, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult finishBuyer([System.Web.Http.FromBody] int[] data)
        {
            var kiemtra = db.w_material_info.Where(x => data.Contains(x.wmtid)).ToList();
            try
            {
                foreach (var item in kiemtra)
                {
                    item.input_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                    item.lct_cd = "003G01000000000000";
                    item.from_lct_cd = "006000000000000000";
                    item.to_lct_cd = "003G01000000000000";
                    item.mt_sts_cd = "001";
                    item.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    db.Entry(item).State = EntityState.Modified;
                }
                if (db.SaveChanges() > 0)
                    return Json(new { result = true, data = kiemtra, message = "Thành Công!!!" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { result = false, message = "Lỗi hệ thống" }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult InsertMTQR_EXT_List(string buyer_qr)
        {
            try
            {
                if (string.IsNullOrEmpty(buyer_qr))
                {
                    return Json(new { result = false, message = "Làm ơn kiểm tra lại mã buyer này rỗng" }, JsonRequestBehavior.AllowGet);
                }
                //UPDATE w_material_info
                var kiemtra = db.w_material_info.Where(x => x.buyer_qr == buyer_qr).ToList();

                if (kiemtra.Count == 1)
                {
                    if (kiemtra.FirstOrDefault().lct_cd.StartsWith("003"))
                    {
                        return Json(new { result = false, message = "Buyer Qr này đã được đưa vào kho thành phẩm" }, JsonRequestBehavior.AllowGet);
                    }
                    if (kiemtra.FirstOrDefault().lct_cd.StartsWith("004"))
                    {
                        return Json(new { result = false, message = "Buyer Qr này đã được xuất đến khách hàng" }, JsonRequestBehavior.AllowGet);
                    }
                }
                var check = kiemtra.Where(x => x.mt_sts_cd == "010" && x.lct_cd.StartsWith("006") && x.buyer_qr != null && x.buyer_qr != "").ToList();
                var success = 0;
                var erro = 0;

                foreach (var item in check)
                {
                    try
                    {
                        item.input_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                        item.lct_cd = "003G01000000000000";
                        item.from_lct_cd = "006000000000000000";
                        item.to_lct_cd = "003G01000000000000";
                        item.mt_sts_cd = "001";
                        item.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        db.Entry(item).State = EntityState.Modified;
                        db.SaveChanges();
                        success++;
                    }
                    catch (Exception e)
                    {
                        erro++;
                    }
                }
                if (success > 0)
                {
                    return Json(new { result = true, data = check, message = "Thành Công!!!" }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, data = check, message = "Không tìm thấy!!!" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public JsonResult UploadExcelFGRecei(List<FGShippingByExcelModel> tempList)
        {
            if (tempList == null)
            {
                return Json(new { flag = false, message = "Chưa chọn File excel" }, JsonRequestBehavior.AllowGet);
            }
            var buyerList = tempList.Select(x => x.Code);

            var success = false;
            if (tempList.Count > 0)
            {
                //lấy ra danh sách đã vào kho FG
                var dataDaTonTai = (from a in db.w_material_info
                                    where buyerList.Contains(a.buyer_qr)
                                    && a.lct_cd.StartsWith("003G01000000000000")
                                    select new
                                    {
                                        wmtid = a.wmtid,

                                    }
                   ).Count();
                //lấy danh sách thỏa điều kiện để update
                var dataThoaDk = (from a in db.w_material_info
                                  where buyerList.Contains(a.buyer_qr)
                                  && a.lct_cd.StartsWith("006")
                                  && (a.mt_sts_cd.Equals("010"))
                                 && a.gr_qty > 0
                                  select new
                                  {
                                      wmtid = a.wmtid,

                                  }
                    ).ToList();


                //danh sách không tồn tại trong kho FG
                int listNoExist = tempList.Count() - dataThoaDk.Count() - dataDaTonTai;

                foreach (var item in dataThoaDk)
                {
                    try
                    {
                        var ListUpdate = db.w_material_info.Find(item.wmtid);

                        ListUpdate.input_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                        ListUpdate.lct_cd = "003G01000000000000";
                        ListUpdate.from_lct_cd = "006000000000000000";
                        ListUpdate.to_lct_cd = "003G01000000000000";
                        ListUpdate.mt_sts_cd = "001";
                        ListUpdate.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();

                        db.Entry(ListUpdate).State = EntityState.Modified;
                        success = true;
                    }
                    catch (Exception e)
                    {
                        return Json(new { result = false, message = "" }, JsonRequestBehavior.AllowGet);
                    }
                }
                if (success)
                {
                    db.SaveChanges();
                    return Json(new { result = true, ss = true, ok = dataThoaDk.Count(), dsDatontai = dataDaTonTai, ng = listNoExist, dataThoaDk, message = "Thành Công!!!" }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = true, ss = false, ok = dataThoaDk.Count(), dsDatontai = dataDaTonTai, ng = listNoExist, dataThoaDk, message = "Thành Công!!!" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { result = false, message = "File excel không hợp lệ." }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public JsonResult UploadExcelFGRecei1(List<FGShippingByExcelModel> tempList)
        {
            // get productCODE
            var listCode = tempList.Select(x => x.Code).ToArray();
            //var product = _iFGWmsService.FindStamp(listCode[0]);

            //lấy product 
            string productCode = listCode[0].Substring(0, listCode[0].IndexOf("DZIH"));

            //// Lân: Cần get DB xem có tồn tại không mới check được => Sai logic, subtring không bao giờ trả về null
            //// if product no exist in db
            //if (productCode == null)
            //{
            //    return Json(new { result = false, message = "Product không có trong hệ thống." }, JsonRequestBehavior.AllowGet);
            //}


            var dataPRoduct = _iFGWmsService.FindOneProductById(productCode);
            string model = "";
            string ProductName = "";
            if (dataPRoduct != null)
            {
                //lấy model + product name
                model = dataPRoduct.md_cd;
                ProductName = dataPRoduct.style_nm;
            }
            else
            {
                return Json(new { result = false, message = "Product không có trong hệ thống." }, JsonRequestBehavior.AllowGet);
            }
            // check ds cung product
            var listDataSameProduct = listCode.Where(x => x.StartsWith(productCode)).ToArray();

            var buyer_qrs = db.generalfgs.AsNoTracking().Select(item => item.buyer_qr).ToArray();

            var dataChuaNhapKho = listDataSameProduct
                .Where(item => buyer_qrs.Contains(item) == false)
                .ToArray();
            var soluongdatadanhapkho = listDataSameProduct.Except(dataChuaNhapKho).Count();
            var listDataDiffProduct = listCode.Where(x => !x.StartsWith(productCode)).Select(z => new { BuyerCode = z }).ToArray();
            //var dataDaNhapKho = db.generalfgs.Where(x => listDataSameProduct.Contains(x.buyer_qr));

            //LIST BAN DAU
            //List<string> dataChuaNhapKho = listDataSameProduct.Where(s => !dataDaNhapKho.Any(x => x.buyer_qr == s)).ToList();
            //int countThoaDK = listDataSameProduct.Count() - db.generalfgs.Where(x => listDataSameProduct.Contains(x.buyer_qr))
            //    .Select(x => x.buyer_qr).Count();

            string param = "";
            for (int i = 0; i < dataChuaNhapKho.Length; i++)
            {
                param += "'" + dataChuaNhapKho[i] + "'";
                if (i != dataChuaNhapKho.Length - 1)
                {
                    param += ',';
                }
            }
            StringBuilder sql = new StringBuilder();
            if (!string.IsNullOrEmpty(param))
            {
                sql.AppendLine("SET sql_mode = '';SET @@sql_mode = '';");
                sql.AppendLine("WITH FILTER_w_material_info");
                sql.AppendLine("AS");
                sql.AppendLine(" ( 	SELECT  m.wmtid AS wmtid,");
                sql.AppendLine(" m.product ProductNo,");
                sql.AppendLine(" m.buyer_qr BuyerCode,");
                sql.AppendLine(" m.gr_qty Quantity,");
                sql.AppendLine(" 'MES'  TypeSystem , ");
                sql.AppendLine(" m.bb_no,  ");
                sql.AppendLine($" '{model}' Model, ");
                sql.AppendLine($" '{ProductName}' ProductName ");
                sql.AppendLine(" FROM	w_material_info m ");
                sql.AppendLine($" WHERE m.buyer_qr IN ({param}) ),");
                sql.AppendLine(" FILTER_stam_detail AS ");
                sql.AppendLine("( SELECT s.id AS wmtid,       ");
                sql.AppendLine("  s.product_code ProductNo, ");
                sql.AppendLine(" s.buyer_qr BuyerCode,      ");
                sql.AppendLine(" s.standard_qty Quantity,   ");
                sql.AppendLine(" 'SAP' TypeSystem ,          ");
                sql.AppendLine(" '' bb_no ,          ");
                sql.AppendLine($" '{model}' Model, ");
                sql.AppendLine($" '{ProductName}' ProductName ");
                sql.AppendLine("FROM stamp_detail s");
                sql.AppendLine($"WHERE s.buyer_qr IN   ( {param}) ),");
                sql.AppendLine(" FILTER_RESULT_EXIST AS  (");
                sql.AppendLine("    SELECT *");
                sql.AppendLine("    FROM    FILTER_w_material_info UNION ");
                sql.AppendLine(" SELECT *  FROM    FILTER_stam_detail )");
                sql.AppendLine("SELECT  f.wmtid, f.ProductNo, min(f.BuyerCode) BuyerCode,f.Quantity,f.TypeSystem, f.bb_no,f.Model,f.ProductName ");
                sql.AppendLine("FROM    FILTER_RESULT_EXIST f ");
                sql.AppendLine(" GROUP BY f.BuyerCode  ");
            }
            if (string.IsNullOrEmpty(sql.ToString()))
            {
                return Json(new
                {
                    result = true,
                    ss = true,
                    ok = 0,
                    khacproduct = listDataDiffProduct.Count(),
                    dsDatontai = soluongdatadanhapkho,
                    dataThoaDk = 0,
                    dataKhacProduct = listDataDiffProduct,
                    dataSoluongKhongCoTrongHeThong = 0,
                    dataKhongCoTrongHeThong = 0,

                    message = "Thành Công!!!"
                }, JsonRequestBehavior.AllowGet);
            };
            var resultDataOfMES = db.Database.SqlQuery<FGReceiData>(sql.ToString());
            var dataInDB = resultDataOfMES.Select(item => item.BuyerCode).ToArray();
            var resultNotExist = dataChuaNhapKho
                .Except(dataInDB)
                .Select(z => new { BuyerCode = z })
                .ToArray();

            return Json(new
            {
                result = true,
                ss = true,
                ok = dataInDB.Length,
                khacproduct = listDataDiffProduct.Count(),
                dsDatontai = soluongdatadanhapkho,
                dataThoaDk = resultDataOfMES,
                dataKhacProduct = listDataDiffProduct,
                dataSoluongKhongCoTrongHeThong = resultNotExist.Length,
                dataKhongCoTrongHeThong = resultNotExist,

                message = "Thành Công!!!"
            }, JsonRequestBehavior.AllowGet);
        }
        //public JsonResult UploadExcelFGRecei1(List<FGShippingByExcelModel> tempList)
        //{
        //    if (tempList == null)
        //    {
        //        return Json(new { flag = false, message = "Chưa chọn File excel" }, JsonRequestBehavior.AllowGet);
        //    }
        //    var buyerList = tempList.Select(x => x.Code);
        //    //lấy product để nhận
        //    //tìm product bảng stamp_detail
        //    var product = _iFGWmsService.FindStamp(tempList[0].Code);
        //    if (product == null)
        //    {
        //        return Json(new { result = false, message = "Product không có trong hệ thống." }, JsonRequestBehavior.AllowGet);
        //    }
        //    var dataPRoduct = _iFGWmsService.FindOneProductById(product.product_code);
        //    if (tempList.Count > 0)
        //    {
        //        var productName = dataPRoduct.style_nm;
        //        var ModelName = dataPRoduct.md_cd;

        //        var dataDaTonTai = db.generalfgs.Where(a => buyerList.Contains(a.buyer_qr)
        //                         ).ToList();

        //        //lấy danh sách cùng product
        //        var prod = product.product_code.Replace("-", "");
        //        var dataCungProduct = tempList.Where(x => x.Code.StartsWith(prod)).ToList();
        //        buyerList = dataCungProduct.Select(x => x.Code);
        //        var dataKhacProduct11 = tempList.Where(x => !dataCungProduct.Any(p => p.Code == x.Code)).ToList();
        //        //lấy danh sách khác product
        //        StringBuilder idsListpro = new StringBuilder();


        //        if (dataKhacProduct11 != null)
        //        {
        //            foreach (var item in dataKhacProduct11)
        //            {
        //                idsListpro.Append($"'{item.Code}',");

        //            }

        //        }
        //        string listMes1 = new InitMethods().RemoveLastComma(idsListpro);

        //        var dataKhacProduct = _iFGWmsService.GetListKhacProduct(listMes1).ToList();

        //        //lấy ra danh sách đã vào kho FG


        //        //tem gói đã trừ đi hàng vào kho
        //        tempList = dataCungProduct.Where(s => !dataDaTonTai.Any(p => p.buyer_qr == s.Code)).ToList();
        //        var buyerList1 = tempList.Select(x => x.Code);

        //        //lấy danh sách thỏa điều kiện để update
        //        var dataThoaDk = (from a in db.w_material_info
        //                          where buyerList1.Contains(a.buyer_qr)
        //                          select new
        //                          {
        //                              wmtid = a.wmtid,
        //                              ProductNo = a.product,
        //                              ProductName = productName,
        //                              Model = ModelName,
        //                              BuyerCode = a.buyer_qr,
        //                              Quantity = a.gr_qty,
        //                              TypeSystem = "MES"
        //                          }
        //        ).ToList();

        //        //tem gói đã trừ đi cái thỏa điều kiện 
        //    var   tempList2 = tempList.Where(s => !dataThoaDk.Any(p => p.BuyerCode == s.Code)).ToList();
        //        buyerList = tempList2.Select(x => x.Code);

        //        //kiểm tra có trong bảng stamp_detail không

        //        var dataThoaDkSap = (from a in db.stamp_detail
        //                             where buyerList.Contains(a.buyer_qr)
        //                         && a.product_code.Equals(product.product_code)
        //                             select new
        //                             {
        //                                 wmtid = a.id,
        //                                 ProductNo = a.product_code,
        //                                 ProductName = productName,
        //                                 Model = ModelName,
        //                                 BuyerCode = a.buyer_qr,
        //                                 Quantity = a.standard_qty,
        //                                 TypeSystem = "SAP"
        //                             }
        //             ).ToList();

        //        //lấy số cuộn không có trong hệ thống
        //        int soluongDu = tempList.Count() - (dataThoaDk.Count() + dataThoaDkSap.Count());



        //        return Json(new
        //        {
        //            result = true,
        //            ss = true,
        //            ok = dataThoaDk.Count() + dataThoaDkSap.Count()
        //            ,
        //            khacproduct = dataKhacProduct.ToList().Count(),
        //            dsDatontai = dataDaTonTai.Count(),
        //            dataThoaDk,
        //            dataKhacProduct,
        //            dataThoaDkSap,
        //            dataKhongCoTrongHeThong = soluongDu,
        //            message = "Thành Công!!!"
        //        }, JsonRequestBehavior.AllowGet);
        //    }
        //    else
        //    {
        //        return Json(new { result = false, message = "File excel không hợp lệ." }, JsonRequestBehavior.AllowGet);
        //    }
        //}
        public ActionResult Feed_back_EXT_no(string extid)
        {
            try
            {
                if (string.IsNullOrEmpty(extid))
                {
                    return Json(new { result = false, message = "Làm ơn kiểm tra lại mã EXT này rỗng" }, JsonRequestBehavior.AllowGet);
                }
                var id = Convert.ToInt32(extid);
                var Updatesd = db.w_ext_info.Find(id);
                if (Updatesd != null && Updatesd.alert != 2)
                {
                    Updatesd.alert = 2;
                    Updatesd.ext_sts_cd = "000";
                    //Updatesd.chg_dt = DateTime.Now;
                    Updatesd.chg_id = Session["authName"] == null ? null : Session["authName"].ToString();

                    db.Entry(Updatesd).State = EntityState.Modified;
                    db.SaveChanges();

                    return Json(new { result = true, data = Updatesd, message = "Thành công!!!" }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = "Không tồn tại danh sách này!!!" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Cancel_EXT_no(string extid)
        {
            try
            {
                if (string.IsNullOrEmpty(extid))
                {
                    return Json(new { result = false, message = "Làm ơn kiểm tra lại mã EXT này rỗng" }, JsonRequestBehavior.AllowGet);
                }
                var id = Convert.ToInt32(extid);
                var Updatesd = db.w_ext_info.Find(id);
                if (Updatesd != null && Updatesd.alert == 2)
                {
                    Updatesd.alert = 1;
                    Updatesd.ext_sts_cd = "000";
                    //Updatesd.chg_dt = DateTime.Now;
                    Updatesd.chg_id = Session["authName"] == null ? null : Session["authName"].ToString();

                    db.Entry(Updatesd).State = EntityState.Modified;
                    db.SaveChanges();

                    return Json(new { result = true, data = Updatesd, message = "Thành công!!!" }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = "Không tồn tại danh sách này!!!" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
            }
        }

        #region EXT_Info_popup

        public ActionResult PartialView_EXT_Info_Popup(string ext_no)
        {
            ViewBag.ext_no = ext_no;
            return PartialView("~/Views/fgwms/Receiving_Scan/PartialView_EXT_Info_Popup.cshtml");
        }

        public ActionResult GetTimsShippingScanPP(string ext_no)
        {
            var listdata = (from a in db.w_ext_info

                            where a.ext_no.Equals(ext_no)

                            select new
                            {
                                ext_no = a.ext_no,
                                ext_nm = a.ext_nm,
                                ext_sts_cd = a.ext_sts_cd,
                                remark = a.remark,
                            }).ToList();

            return Json(new { data = listdata }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetTimsShippingScanListPP(string ext_no)
        {
            try
            {
                var data1 = (from a in db.w_material_info
                             where a.ext_no.Equals(ext_no)
                             select new
                             {
                                 wmtid = a.wmtid,
                                 mt_cd = a.mt_cd,
                                 bb_no = a.bb_no,
                                 gr_qty = a.gr_qty,
                                 recevice_dt_tims = a.recevice_dt_tims,

                                 from_lct_cd = a.from_lct_cd,
                                 from_lct_nm = db.lct_info.Where(x => x.lct_cd == a.lct_cd).Select(x => x.lct_nm),

                                 lct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002" && x.dt_cd == a.lct_sts_cd).Select(x => x.dt_nm),

                                 mt_type_nm = db.comm_dt.Where(x => x.mt_cd == "COM004" && x.dt_cd == a.mt_type).Select(x => x.dt_nm),
                                 mt_type = a.mt_type,

                                 mt_sts_cd = a.mt_sts_cd,
                                 sts_nm = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == a.mt_sts_cd).Select(x => x.dt_nm),
                             }
                   ).ToList();

                return Json(new { data = data1 }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult PrintEXT_LIST(string ext_no)
        {
            ViewData["Message"] = ext_no;
            return PartialView("~/Views/TIMS/ShippingScan/PrintEXT_LIST.cshtml");
        }

        #endregion EXT_Info_popup


        public JsonResult RecevingBuyerCodeScanning(string buyerCode, string type)
        {
            if (string.IsNullOrEmpty(buyerCode))
            {
                return Json(new { flag = false, message = "Quét mã tem gói để tiếp tục." }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                var buyer = db.w_material_info.Where(x => x.buyer_qr == buyerCode).FirstOrDefault();//ko ở trong tồn của MES 
                if (buyer == null)
                {
                    return Json(new { flag = false, message = "Mã buyer không tồn tại." }, JsonRequestBehavior.AllowGet);

                }

                var tempData = db.w_material_info
                            .Where(x => x.buyer_qr == buyerCode && x.lct_cd.Equals("006000000000000000") && x.mt_sts_cd.Equals("010"))
                            .ToList();


                if (tempData.Count < 1)
                {
                    if (buyer.lct_cd.Contains("003"))
                    {
                        return Json(new { flag = false, message = "Mã Buyer này đã được đưa vào kho FG" }, JsonRequestBehavior.AllowGet);
                    }
                    if (buyer.lct_cd.Contains("004"))
                    {
                        return Json(new { result = false, message = "Buyer Qr này đã được xuất đến khách hàng" }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { flag = false, message = "Mã buyer không đủ điều kiện để Scan" }, JsonRequestBehavior.AllowGet);
                }

                //string sql = " Select x.wmtid as Id , x.mt_cd as MaterialCode, x.mt_no as MaterialNo, x.buyer_qr as BuyerCode, "
                //              + " x.gr_qty as Quantity,   'MES' as TypeSystem, "
                //              + " (SELECT p.product FROM w_actual_primary AS p  JOIN w_actual AS q  ON p.at_no = q.at_no   WHERE q.id_actual = x.id_actual) AS ProductNo "

                //              + " FROM w_material_info AS x"

                //              + " WHERE x.buyer_qr= '" + buyerCode + "' ";
                //var data = db.Database.SqlQuery<MappedProductModel>(sql).FirstOrDefault();




                var data = tempData
                            .Select(x => new
                            {
                                Id = x.wmtid,
                                PoNo = x.at_no,
                                ProductNo = x.product,
                                MaterialCode = x.mt_cd,
                                Quantity = x.gr_qty,
                                BuyerCode = x.buyer_qr,
                                BobbinNo = x.bb_no,


                            }).FirstOrDefault();
                return Json(new { data = data, flag = true, message = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { flag = false, message = "Lỗi hệ thống" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult CheckStatusBuyerToRece(string buyerCode)
        {
            try
            {
                if (string.IsNullOrEmpty(buyerCode))
                {
                    return Json(new { result = false, message = "Quét mã tem gói để tiếp tục." }, JsonRequestBehavior.AllowGet);

                }
                var tempData = _iFGWmsService.CheckBuyerStatus(buyerCode).FirstOrDefault();
                //thỏa điều kiện nhập kho thì return luôn
                if (tempData != null)
                {
                    return Json(new { data = tempData, result = true, message = "Đủ điều kiện nhập kho" }, JsonRequestBehavior.AllowGet);
                }

                var buyer = _iFGWmsService.FindOneMaterialInfoById(buyerCode);//ko ở trong tồn của MES 
                if (buyer == null)
                {
                    //add tem gói vào sap
                    //kiểm tra tồn ở sap chưa
                    var isExistGeneral = _iFGWmsService.FindOneBuyerInfoById(buyerCode);
                    if (isExistGeneral == null)
                    {
                        //nếu chưa có thì kiểm tra bảng stamp_detail

                        var isExistStampDetail = _iFGWmsService.FindStamp(buyerCode);
                        if (isExistStampDetail != null)
                        {
                            var data1 = new
                            {
                                wmtid = isExistStampDetail.id,
                                ProductNo = isExistStampDetail.product_code,
                                ProductName = db.d_style_info.Where(y => y.style_no == isExistStampDetail.product_code).Select(a => a.style_nm),
                                Model = db.d_style_info.Where(y => y.style_no == isExistStampDetail.product_code).Select(a => a.md_cd),
                                BuyerCode = isExistStampDetail.buyer_qr,
                                Quantity = isExistStampDetail.standard_qty,
                                TypeSystem = "SAP",
                                bb_no = "",

                            };
                            return Json(new { data = data1, result = true, message = "" }, JsonRequestBehavior.AllowGet);

                        }
                        return Json(new { flag = false, message = "Tem này không được tạo ở hệ thống" }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { flag = false, message = "Tem gói này đã được vào tồn kho" }, JsonRequestBehavior.AllowGet);
                }
               
                if (tempData == null)
                {
                    if (buyer.lct_cd.Contains("003"))
                    {
                        //kiểm tra tồn ở sap chưa
                        var isExistGeneral = _iFGWmsService.FindOneBuyerInfoById(buyerCode);
                        if (isExistGeneral == null)

                        {
                            //nếu chưa có thì kiểm tra bảng stamp_detail

                            var isExistStampDetail = _iFGWmsService.FindStamp(buyerCode);
                            if (isExistStampDetail != null)
                            {
                                var data1 = new
                                {
                                    wmtid = isExistStampDetail.id,
                                    ProductNo = isExistStampDetail.product_code,
                                    ProductName = db.d_style_info.Where(y => y.style_no == isExistStampDetail.product_code).Select(a => a.style_nm),
                                    Model = db.d_style_info.Where(y => y.style_no == isExistStampDetail.product_code).Select(a => a.md_cd),
                                    BuyerCode = isExistStampDetail.buyer_qr,
                                    Quantity = isExistStampDetail.standard_qty,
                                    TypeSystem = "SAP",
                                    bb_no = "",
                                };
                                return Json(new { data = data1, result = true, message = "" }, JsonRequestBehavior.AllowGet);

                            }
                            return Json(new { result = false, message = "Mã Buyer này đã được đưa vào kho FG" }, JsonRequestBehavior.AllowGet);
                        }
                        return Json(new { result = false, message = "Mã Buyer này đã được đưa vào kho FG" }, JsonRequestBehavior.AllowGet);
                    }
                    if (buyer.lct_cd.Contains("004"))
                    {
                        return Json(new { result = false, message = "Buyer Qr này đã được xuất đến khách hàng" }, JsonRequestBehavior.AllowGet);
                    }
                   
                }
                return Json(new { result = false, message = "Mã buyer không đủ điều kiện để Scan" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult UpdateReciFG(FGRecevingScan model)
        {
            try
            {
                StringBuilder idsListMEs = new StringBuilder();
                StringBuilder idsListsAP = new StringBuilder();
                if (model.WmtidMES == null)
                {
                    if (model.WmtidsAP == null)
                    {

                        return Json(new { flag = false, message = "Vui lòng scan tem gói để tiếp tục." });
                    }

                }


                if (model.WmtidMES != null)
                {
                    foreach (var item1 in model.WmtidMES)
                    {
                        idsListMEs.Append($"'{item1}',");

                    }
                }
                if (model.WmtidsAP != null)
                {
                    foreach (var item1 in model.WmtidsAP)
                    {
                        idsListsAP.Append($"'{item1}',");

                    }
                }

                string listMes = new InitMethods().RemoveLastComma(idsListMEs);
                string listsAP = new InitMethods().RemoveLastComma(idsListsAP);




                //update mã tem ở buyer
                var item = new w_material_info();
                item.input_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                item.lct_cd = "003G01000000000000";
                item.from_lct_cd = "006000000000000000";
                item.to_lct_cd = "003G01000000000000";
                item.mt_sts_cd = "001";
                item.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                item.chg_dt = DateTime.Now;
                _iFGWmsService.UpdateReceFGWMaterialInfo(item, listMes.Replace("'", ""));

                //insert mes vào bảng sap 
                var userID = Session["userid"] == null ? null : Session["userid"].ToString();
                _iFGWmsService.InsertToMesgeneralfg(listMes.Replace("'", ""), model.ModelCode, userID);

                //insert vào bảng sap - generalfg
                //nếu chưa có thì kiểm tra bảng stamp_detail
                if (model.WmtidsAP != null)
                {


                    var scripts = new StringBuilder();

                    for (int i = 0; i < model.WmtidsAP.Count; i++)
                    {
                        //kiểm tra bảng stamp_detail có tồn tại hay không?
                        var isExistStampDetail = _iFGWmsService.FindStampForId(model.WmtidsAP[i]);


                        if (isExistStampDetail != null)
                        {


                            //kiểm tra mã buyer này đã tồn kho bảng generalfg chưa
                            //if (_iFGWmsService.FindOneBuyerInfoById(isExistStampDetail.buyer_qr) == null)
                            //{
                                //var itemModel = new generalfg();
                                //itemModel.buyer_qr = isExistStampDetail.buyer_qr;
                                //itemModel.product_code = isExistStampDetail.product_code;
                                //itemModel.md_cd = model.ModelCode;
                                //itemModel.dl_no = null;
                                //itemModel.qty = isExistStampDetail.standard_qty;
                                //itemModel.lot_no = isExistStampDetail.lot_date;
                                //itemModel.sts_cd = "001";
                                //itemModel.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                                //itemModel.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                                //itemModel.reg_dt = DateTime.Now;
                                //itemModel.chg_dt = DateTime.Now;
                                //
                                //
                                //_iFGWmsService.Insertgeneralfg(itemModel);


                                var sql = $@"

                             INSERT INTO generalfg(buyer_qr, product_code, md_cd, qty, lot_no, sts_cd, reg_id, chg_id, TYPE)
                            SELECT * FROM (SELECT '{isExistStampDetail.buyer_qr}','{isExistStampDetail.product_code}','{model.ModelCode}',                                       '{isExistStampDetail.standard_qty}','{isExistStampDetail.lot_date}','001','{userID}'  AS reg_id ,'{userID}' as chg_id,'SAP') AS tmp
                            WHERE NOT EXISTS (
                                SELECT buyer_qr FROM generalfg WHERE buyer_qr = '{isExistStampDetail.buyer_qr}'
                            ) LIMIT 1;";

                                scripts.AppendLine(sql);

                            //}
                           
                        }
                    }

                    if(string.IsNullOrEmpty(scripts.ToString().Trim()) == false)
                    {
                        var sql = string.Format("START TRANSACTION; {0} {1} {0} COMMIT;", Environment.NewLine, scripts);

                        db.Database.ExecuteSqlCommand(sql);
                    }    
                }

                return Json(new { result = true, message = "Nhập kho thành công" }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {

                throw;
            }
        }
        #endregion N_Receiving_scan_FG

        #region N_Shipping_Scan_FG

        public ActionResult FGShipping_Scan()
        {
            return SetLanguage("~/Views/fgwms/Shipping_Scan/FGShipping_Scan.cshtml");
    
        }

        public JsonResult GetDLInfo(Pageing paging, string dl_no, string dl_nm, string productCode, string start, string end)
        {
            try
            {
                if (string.IsNullOrEmpty(dl_no))
                {
                    dl_no = "";
                }
                if (string.IsNullOrEmpty(dl_nm))
                {
                    dl_nm = "";
                }
                if (string.IsNullOrEmpty(productCode))
                {
                    productCode = "";
                }
                if (string.IsNullOrEmpty(start))
                {
                    start = "0001-01-01";
                }
                if (string.IsNullOrEmpty(end))
                {
                    end = "9999-12-31";
                }
                var sql = new StringBuilder();


                // (
                //SELECT IFNULL(SUM(g.qty), 0)
                //FROM generalfg g
                //WHERE g.dl_no = a.dl_no) AS quantity


                sql.Append(@"SELECT MAX(a.dlid) AS dlid, MAX(a.dl_no) AS dl_no, MAX(a.dl_no) AS dl_no1, 
                        MAX(a.dl_nm) AS dl_nm, MAX(a.dl_sts_cd) AS dl_sts_cd,
                         MAX(a.work_dt) AS work_dt, MAX(a.lct_cd) AS lct_cd, MAX(a.remark) AS remark,IFNULL(SUM(g.qty), 0)  AS quantity
                       
                        FROM w_dl_info a
                        LEFT JOIN generalfg g ON  g.dl_no = a.dl_no
                        WHERE a.use_yn = 'Y' AND a.dl_no LIKE '%" + dl_no + @"%'
                        AND ('" + dl_nm + "'='' OR  a.dl_nm like '%" + dl_nm + @"%' )
                        AND ('" + productCode + "'='' OR  g.product_code like '%" + productCode + @"%' )
              
                         AND STR_TO_DATE(a.work_dt, '%Y-%m-%d') >= STR_TO_DATE( '" + start + @"', '%Y-%m-%d')
                          AND STR_TO_DATE(a.work_dt, '%Y-%m-%d') <= STR_TO_DATE('" + end + @"', '%Y-%m-%d')
                        GROUP BY a.dlid
                        ORDER BY dlid DESC");

           

                DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);
                int total = dt.Rows.Count;
                var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("dlid"));
                return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
            }
            catch (Exception)
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult DeleteDL(int id)
        {
            var dl = db.w_dl_info.Find(id);
            if (dl == null)
            {
                return Json(new { result = false, message = "Không tìm thấy mã DL này." }, JsonRequestBehavior.AllowGet);
            }
            dl.use_yn = "N";
            db.Entry(dl).State = EntityState.Modified;
            db.SaveChanges();
            return Json(new { result = true, message = "Mã Đã được xóa.", value = dl.dlid }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult InsertDlInfo(w_dl_info w_dl_info)
        {
            try
            {
                #region Tang tự động

                String dl_no = "DL1";

                var dl_no_last = db.w_dl_info.ToList().LastOrDefault();
                if (dl_no_last != null)
                {
                    var dl_noCode = dl_no_last.dl_no;
                    dl_no = string.Concat("DL", (int.Parse(dl_noCode.Substring(2)) + 1).ToString());
                }

                #endregion Tang tự động

                w_dl_info.dl_no = dl_no;
                w_dl_info.lct_cd = "004000000000000000";

                DateTime dt = DateTime.Now;
                string day_now = dt.ToString("yyyy-MM-dd");
                //nếu là quyền admin thì lấy input date insert ngược lại lấy today
              var userAdmin =   Session["authName"] == null ? null : Session["authName"].ToString();
                if (string.IsNullOrEmpty(userAdmin))
                {
                    return Json(new { result = true, message = "Vui lòng đăng nhập tài khoản trước khi tạo" }, JsonRequestBehavior.AllowGet);
                }
                if (userAdmin.Equals("Admin"))
                {
                    w_dl_info.work_dt = w_dl_info.work_dt;
                }
                else
                {
                    w_dl_info.work_dt = day_now;
                }
               

                w_dl_info.dl_sts_cd = "000";
                w_dl_info.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                w_dl_info.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();

                w_dl_info.use_yn = "Y";

                w_dl_info.reg_dt = DateTime.Now;
                w_dl_info.chg_dt = DateTime.Now;

                db.w_dl_info.Add(w_dl_info);
                db.SaveChanges();

                var sql = new StringBuilder();
                sql.Append(" SELECT a.* ,a.dl_no AS dl_no1 ")

                  .Append(" FROM w_dl_info as a ")
                  .Append(" WHERE a.dl_no ='" + dl_no + "' ");

                var ddd = new InitMethods().ConvertDataTableToJsonAndReturn(sql);
                return Json(new { result = true, data = ddd.Data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { message = "Lỗi hệ thống !", result = false }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult ModifyDlInfo(w_dl_info w_dl_info)
        {
            try
            {
                var KTTT = db.w_dl_info.Find(w_dl_info.dlid);
                if (KTTT == null)
                {
                    return Json(new { result = false, message = "Không tìm thấy !!!" }, JsonRequestBehavior.AllowGet);
                }

                var userAdmin = Session["authName"] == null ? null : Session["authName"].ToString();
                if (string.IsNullOrEmpty(userAdmin))
                {
                    return Json(new { result = true, message = "Vui lòng đăng nhập tài khoản trước khi sửa" }, JsonRequestBehavior.AllowGet);
                }
                if (userAdmin.Equals("Admin"))
                {
                    KTTT.work_dt = w_dl_info.work_dt;
                }
               

                KTTT.dl_nm = w_dl_info.dl_nm;
                KTTT.remark = w_dl_info.remark;

                KTTT.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                //KTTT.chg_dt = DateTime.Now;

                db.Entry(KTTT).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { result = true, data = KTTT }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
            }
        }


        public ActionResult GetShipping_ScanMLQR_FG(string bx_no)
        {
            try
            {
                if (string.IsNullOrEmpty(bx_no))
                {
                    return Json(new { result = false, message = "Vui lòng kiểm tra lại mã tem thùng bị rỗng" }, JsonRequestBehavior.AllowGet);
                }
                //nếu thỏa điều kiện tem thùng trong bảng w_box_mapping: sts= 000 là chưa xuất
                var checkBox = _iFGWmsService.CheckStampBox(bx_no, "000");
                if (checkBox != null)
                {
                    return Json(new { result = true, message = "Thỏa điều kiện xuất hàng",data = checkBox }, JsonRequestBehavior.AllowGet);
                }
                //nếu không thỏa điều kiện thì kiểm tra nó có tồn tại trong db khôg
                var checkBoxExist = _iFGWmsService.CheckStampBoxExist(bx_no);
                if (checkBoxExist == null)
                {
                    return Json(new { result = false, message = "Không tìm thấy mã box này - hoặc tem gói chưa được scan!!!" }, JsonRequestBehavior.AllowGet);
                }
                if (checkBoxExist.sts.Equals("001"))
                {
                    return Json(new { result = false, message = "Thùng này đã được xuất kho"  }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = "Không tìm thấy mã box này - hoặc tem gói chưa được scan!!!" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult GetShipping_ScanMLQR_FG1(string bx_no)
        {
            try
            {
                if (string.IsNullOrEmpty(bx_no))
                {
                    return Json(new { result = false, message = "Vui lòng kiểm tra lại mã tem thùng bị rỗng" }, JsonRequestBehavior.AllowGet);
                }

                var kttt_null = db.w_box_mapping.Any(x => x.bx_no == bx_no);
                if (kttt_null == false)
                {
                    return Json(new { result = false, message = "Không tìm thấy mã box này!!!" }, JsonRequestBehavior.AllowGet);
                }
                var L_ml_nobx = db.w_box_mapping.Where(x => x.bx_no == bx_no).Select(x => x.buyer_cd);

                if (L_ml_nobx == null)
                {
                    return Json(new { result = false, message = "Không tìm thấy mã box này - hoặc tem gói chưa được scan!!!" }, JsonRequestBehavior.AllowGet);
                }
                StringBuilder buyersList = new StringBuilder();
                foreach (var item in L_ml_nobx)
                {
                    buyersList.Append($"'{item}',");

                }
                string listMes = new InitMethods().RemoveLastComma(buyersList);
                var count_rowpanMes = db.w_material_info.Count(a => L_ml_nobx.Contains(a.buyer_qr)
                      && string.IsNullOrEmpty(a.dl_no) && a.lct_cd == "003G01000000000000" && a.mt_sts_cd == "010");

                var count_rowpanSap = db.generalfgs.Count(b => L_ml_nobx.Contains(b.buyer_qr) && b.sts_cd.Contains("010"));
                var rowpan = count_rowpanMes + count_rowpanSap;
                //if (count_rowpan == 0)
                //{
                //    return Json(new { result = false, message = "Không tìm thấy mã box này - hoặc chưa scan tem gói !!!" }, JsonRequestBehavior.AllowGet);
                //}
                StringBuilder sql = new StringBuilder();
                sql.Append($" SELECT * FROM ( SELECT a.wmtid, a.buyer_qr, a.gr_qty, (SELECT dt_nm FROM comm_dt WHERE mt_cd = 'WHS005' AND dt_cd = a.mt_sts_cd) AS sts_nm,'{bx_no}'  as bx_no,'{rowpan}' as rowpan,'MES' as TypeSystem  ")
                   .Append($" FROM w_material_info a ")

                   .Append($" WHERE (a.dl_no = '' OR a.dl_no IS NULL) ")
                   .Append($" AND a.buyer_qr IN ({listMes}) AND a.lct_cd = '003G01000000000000' AND a.mt_sts_cd = '010'  ")

                   .Append($" UNION ALL ")
                   .Append($" SELECT c.id AS wmtid, c.buyer_qr,c.qty, (SELECT dt_nm FROM comm_dt WHERE mt_cd = 'WHS005' AND dt_cd = c.sts_cd) AS sts_nm, '{bx_no}'  as bx_no ,'{rowpan}' as rowpan ,'SAP' as TypeSystem ")
                   .Append($" FROM generalfg c ")
                   .Append($" WHERE (c.dl_no = '' OR c.dl_no IS NULL) AND c.buyer_qr IN ({listMes})  AND c.sts_cd = '010') AS datatable ");


                var ddd = new InitMethods().ConvertDataTableToJsonAndReturn(sql);


                //var dsCommomlct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002");
                //var dsCommommt_type_nm = db.comm_dt.Where(x => x.mt_cd == "COM004");
                //var dsCommommt_sts_nm = db.comm_dt.Where(x => x.mt_cd == "WHS005");
                //var dsLocation = db.lct_info;

                //var data1 = (from a in db.w_material_info
                //            where L_ml_nobx.Contains(a.buyer_qr) && string.IsNullOrEmpty(a.dl_no)
                //            select new
                //            {
                //                wmtid = a.wmtid,
                //                mt_no = a.mt_no,
                //                mt_type = a.mt_type,
                //                lot_no = a.lot_no,
                //                expiry_dt = a.expiry_dt,
                //                dt_of_receipt = a.dt_of_receipt,
                //                expore_dt = a.expore_dt,
                //                sd_sts_cd = a.mt_sts_cd,
                //                sts_nm = dsCommommt_sts_nm.Where(x => x.dt_cd == a.mt_sts_cd).Select(x => x.dt_nm),

                //                mt_cd = a.mt_cd,
                //                bb_no = a.bb_no,
                //                buyer_qr = a.buyer_qr,
                //                gr_qty = a.gr_qty,
                //                recevice_dt_tims = a.recevice_dt_tims,

                //                from_lct_cd = a.from_lct_cd,
                //                from_lct_nm = db.lct_info.Where(x => x.lct_cd == a.from_lct_cd).Select(x => x.lct_nm),

                //                lct_cd = a.lct_cd,
                //                lct_nm = db.lct_info.Where(x => x.lct_cd == a.lct_cd).Select(x => x.lct_nm),

                //                to_lct_cd = a.to_lct_cd,
                //                to_lct_nm = db.lct_info.Where(x => x.lct_cd == a.to_lct_cd).Select(x => x.lct_nm),

                //                lct_sts_cd = dsCommomlct_sts_cd.Where(x => x.dt_cd == a.lct_sts_cd).Select(x => x.dt_nm),

                //                mt_type_nm = dsCommommt_type_nm.Where(x => x.dt_cd == a.mt_type).Select(x => x.dt_nm),
                //                mt_sts_cd = a.mt_sts_cd,

                //                //rowpan = count_rowpan,
                //                bx_no = bx_no,
                //            }
                //      ).ToList();
                if (ddd == null)
                {
                    return Json(new { result = false, message = "Không tìm thấy mã box này!!!" }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = true, message = "Tạo danh sách thành công!!!", data = ddd.Data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult ShowingScanBoxTemporary(List<FGShippingByExcelModel> tempList)
        {
            bool check = true;
            List<WBoxMapping> listData = new List<WBoxMapping>();
            var box_no_issue = "";
            if (tempList == null)
            {
                return Json(new { result = false, message = "Vui lòng chọn file excel" }, JsonRequestBehavior.AllowGet);
            }    
            if (tempList != null)
            {
                foreach (var item in tempList)
                {

                    if (string.IsNullOrEmpty(item.Code))
                    {
                        check = false;
                        break;
                    }
                    //nếu không thỏa điều kiện thì kiểm tra nó có tồn tại trong db khôg
                    var checkBoxExist = _iFGWmsService.CheckStampBoxExist(item.Code);
                    if (checkBoxExist == null)
                    { 
                        check = false;
                        box_no_issue = item.Code;
                        break;
                    }
                    var checkBox = _iFGWmsService.CheckStampBox(item.Code, "000");
                    if (checkBox == null)
                    {
                        check = false;
                        box_no_issue = item.Code;
                        break;
                    }
                    listData.Add(checkBox);
                 

                }
                if (check)
                {
                    return Json(new { result = true, message = "Tạo danh sách thành công!!!", listData }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { result = false, message = "Lỗi do không tìm thấy mã tem thùng hoặc tem thùng rỗng" + box_no_issue }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                return null;
            }

        }
        public JsonResult ShowingScanBoxTemporary_old(List<FGShippingByExcelModel> tempList)
        {
            bool check = true;
            List<SentBoxModel> listData = new List<SentBoxModel>();

            if (tempList.Count > 0)
            {
                foreach (var item in tempList)
                {

                    if (string.IsNullOrEmpty(item.Code))
                    {
                        check = false;
                        break;
                    }

                    var kttt_null = db.w_box_mapping.Any(x => x.bx_no == item.Code);
                    if (kttt_null == false)
                    {
                        check = false;
                        break;
                    }
                    var L_ml_nobx = db.w_box_mapping.Where(x => x.bx_no == item.Code).Select(x => x.mt_cd);

                    if (L_ml_nobx == null)
                    {
                        check = false;
                        break;
                    }

                    var count_rowpan = db.w_material_info.Count(a => L_ml_nobx.Contains(a.mt_cd)
                          && string.IsNullOrEmpty(a.dl_no));

                    if (count_rowpan == 0)
                    {
                        check = false;
                        break;
                    }

                    var dsCommomlct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002");
                    var dsCommommt_type_nm = db.comm_dt.Where(x => x.mt_cd == "COM004");
                    var dsCommommt_sts_nm = db.comm_dt.Where(x => x.mt_cd == "WHS005");
                    var dsLocation = db.lct_info;

                    var data = (from a in db.w_material_info
                                where L_ml_nobx.Contains(a.mt_cd) && string.IsNullOrEmpty(a.dl_no)
                                select new SentBoxModel
                                {
                                    wmtid = a.wmtid,
                                    mt_cd = a.mt_cd,
                                    bb_no = a.bb_no,
                                    buyer_qr = a.buyer_qr,
                                    mt_type_nm = dsCommommt_type_nm.Where(x => x.dt_cd == a.mt_type).Select(x => x.dt_nm).FirstOrDefault(),
                                    gr_qty = a.gr_qty,
                                    sts_nm = dsCommommt_sts_nm.Where(x => x.dt_cd == a.mt_sts_cd).Select(x => x.dt_nm).FirstOrDefault()
                                }
                          ).ToList();
                    if (data.Count == 0)
                    {
                        check = false;
                        break;
                    }

                    foreach (var i in data)
                    {
                        listData.Add(i);
                    }

                }
                if (check)
                {
                    return Json(new { result = true, message = "Tạo danh sách thành công!!!", listData }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { result = false, message = "Lỗi do không tìm thấy mã tem thùng hoặc tem thùng rỗng" }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                return null;
            }

        }

        public ActionResult GetShipping_ScanBuyerQR_FG(string buyerCode)
        {
            try
            {
                if (string.IsNullOrEmpty(buyerCode))
                {
                    return Json(new { result = false, message = "Vui lòng kiểm tra lại mã tem gói này bị rỗng" }, JsonRequestBehavior.AllowGet);
                }

                var dsCommomlct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002");
                var dsCommommt_type_nm = db.comm_dt.Where(x => x.mt_cd == "COM004");
                var dsCommommt_sts_nm = db.comm_dt.Where(x => x.mt_cd == "WHS005");
                var dsLocation = db.lct_info;

                var data = (from a in db.w_material_info
                            where a.buyer_qr.Contains(buyerCode) && string.IsNullOrEmpty(a.dl_no)
                            && a.lct_cd.StartsWith("003") && a.mt_sts_cd.Contains("001")
                            select new
                            {
                                wmtid = a.wmtid,
                                mt_no = a.mt_no,
                                mt_type = a.mt_type,
                                lot_no = a.lot_no,
                                expiry_dt = a.expiry_dt,
                                dt_of_receipt = a.dt_of_receipt,
                                expore_dt = a.expore_dt,
                                sd_sts_cd = a.mt_sts_cd,
                                sts_nm = dsCommommt_sts_nm.Where(x => x.dt_cd == a.mt_sts_cd).Select(x => x.dt_nm),

                                mt_cd = a.mt_cd,
                                bb_no = a.bb_no,
                                buyer_qr = a.buyer_qr,
                                gr_qty = a.gr_qty,
                                recevice_dt_tims = a.recevice_dt_tims,

                                from_lct_cd = a.from_lct_cd,
                                from_lct_nm = db.lct_info.Where(x => x.lct_cd == a.from_lct_cd).Select(x => x.lct_nm),

                                lct_cd = a.lct_cd,
                                lct_nm = db.lct_info.Where(x => x.lct_cd == a.lct_cd).Select(x => x.lct_nm),

                                to_lct_cd = a.to_lct_cd,
                                to_lct_nm = db.lct_info.Where(x => x.lct_cd == a.to_lct_cd).Select(x => x.lct_nm),

                                lct_sts_cd = dsCommomlct_sts_cd.Where(x => x.dt_cd == a.lct_sts_cd).Select(x => x.dt_nm),

                                mt_type_nm = dsCommommt_type_nm.Where(x => x.dt_cd == a.mt_type).Select(x => x.dt_nm),
                                mt_sts_cd = a.mt_sts_cd,
                            }
                      ).ToList();
                if (data.Count == 0)
                {
                    return Json(new { result = false, message = "Không tìm thấy mã tem gói này!!!" }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = true, message = "Thành công!!", data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult GetDeliveryFG(string dl_no)
        {

            try
            {
                StringBuilder sql = new StringBuilder($@"SELECT max(b.id) id, SUM(b.qty) qty, MAX(b.product_code)product, MAX(b.lot_no)AS end_production_dt, 
MAX(b.dl_no) dl_no, MAX(dl.work_dt) work_dt
                    FROM generalfg AS b
                    left JOIN w_dl_info AS  dl ON  b.dl_no = dl.dl_no
                    WHERE b.dl_no = '{dl_no}'
                    GROUP BY b.product_code  ORDER BY b.product_code ");

                var data = db.Database.SqlQuery<FGShippingScanExportExcelModel>(sql.ToString());

                var values = data.Select(x => new
                {
                    id = x.id,
                    product = x.product,
                    lotNo = x.end_production_dt,
                    DeliveryDt = x.work_dt,
                    qty = x.qty,

                }).ToList();
                return Json(new { data = values }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception)
            {

                return Json(new { result = false, message = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public ActionResult ShippingExportToExcel(string dlNo)
        {
            dlNo = dlNo.TrimStart('[').TrimEnd(']');

            if (dlNo.Length > 0)
            {
                StringBuilder sql = new StringBuilder($" Call spFGWMS_ExportShippingScanToExcel('{dlNo}'); ");

                //string[] keys = Request.Form.AllKeys;

                //var value = Request.Form[keys[0]];

                var data = db.Database.SqlQuery<FGShippingScanExportExcelModel>(sql.ToString());

                var values = data.ToList().AsEnumerable().Select(x => new
                {
                    product = x.product,
                    lotNo = x.end_production_dt,
                    //buyer_qr = x.buyer_qr,
                    //box_code = x.box_code,
                    DeliveryDt = x.work_dt,
                    qty = x.qty,

                }).ToArray();

                String[] labelList = new string[4] { "Product", "Lot No", "Delivery Date", "Quantity" };

                Response.ClearContent();

                Response.AddHeader("content-disposition", "attachment;filename=DanhSachXuatHang.xls");

                Response.AddHeader("Content-Type", "application/vnd.ms-excel");

                new InitMethods().WriteHtmlTable(values, Response.Output, labelList);

                Response.End();
            }

            return View("~/Views/fgwms/Shipping_Scan/FGShipping_Scan.cshtml");
        }

        [HttpPost]
        public ActionResult ExportToExcel(string dlNo)
        {
            dlNo = dlNo.TrimStart('[').TrimEnd(']');

            if (dlNo.Length > 0)
            {
                StringBuilder sql = new StringBuilder($" Call spFGWMS_ShippingDL_ExportExcel('{dlNo}'); ");

                //string[] keys = Request.Form.AllKeys;

                //var value = Request.Form[keys[0]];

                var data = db.Database.SqlQuery<FGExportExcelModel>(sql.ToString());

                var values = data.ToList().AsEnumerable().Select(x => new
                {
                    box_code = x.box_code,
                    buyer_qr = x.buyer_qr,
                    bien_null = "",
                    lot_no = x.lot_no

                }).ToArray();

                String[] labelList = new string[4] { "BOX", "BUYER","","ETC/REMARK" };

                Response.ClearContent();

                Response.AddHeader("content-disposition", "attachment;filename=DanhSachMaBuyer.xls");

                Response.AddHeader("Content-Type", "application/vnd.ms-excel");

                new InitMethods().WriteHtmlTable(values, Response.Output, labelList);

                Response.End();
            }

            return View("~/Views/fgwms/Shipping_Scan/FGShipping_Scan.cshtml");
        }
        //[HttpPost]
        //public JsonResult ShowingScanBuyerTemporary(List<FGShippingByExcelModel> tempList)
        //{
        //    if (tempList.Count > 0)
        //    {
        //        var dsCommomlct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002");
        //        var dsCommommt_type_nm = db.comm_dt.Where(x => x.mt_cd == "COM004");
        //        var dsCommommt_sts_nm = db.comm_dt.Where(x => x.mt_cd == "WHS005");
        //        var dsLocation = db.lct_info;
        //        List<FGShippingModel> allData = new List<FGShippingModel>();
        //        foreach (var item in tempList)
        //        {
        //            FGShippingModel data = (from a in db.w_material_info
        //                                    where a.buyer_qr.Contains(item.Code) && string.IsNullOrEmpty(a.dl_no)
        //                                    && a.lct_cd.StartsWith("003") && a.mt_sts_cd.Contains("001")
        //                                    select new FGShippingModel
        //                                    {
        //                                        wmtid = a.wmtid,
        //                                        mt_no = a.mt_no,
        //                                        mt_type = a.mt_type,
        //                                        lot_no = a.lot_no,
        //                                        expiry_dt = a.expiry_dt,
        //                                        dt_of_receipt = a.dt_of_receipt,
        //                                        expore_dt = a.expore_dt,
        //                                        sd_sts_cd = a.mt_sts_cd,
        //                                        sts_nm = dsCommommt_sts_nm.Where(x => x.dt_cd == a.mt_sts_cd).Select(x => x.dt_nm).FirstOrDefault(),
        //                                        mt_cd = a.mt_cd,
        //                                        bb_no = a.bb_no,
        //                                        buyer_qr = a.buyer_qr,
        //                                        gr_qty = a.gr_qty,
        //                                        recevice_dt_tims = a.recevice_dt_tims,
        //                                        from_lct_cd = a.from_lct_cd,
        //                                        from_lct_nm = db.lct_info.Where(x => x.lct_cd == a.from_lct_cd).Select(x => x.lct_nm).FirstOrDefault(),
        //                                        lct_cd = a.lct_cd,
        //                                        lct_nm = db.lct_info.Where(x => x.lct_cd == a.lct_cd).Select(x => x.lct_nm).FirstOrDefault(),
        //                                        to_lct_cd = a.to_lct_cd,
        //                                        to_lct_nm = db.lct_info.Where(x => x.lct_cd == a.to_lct_cd).Select(x => x.lct_nm).FirstOrDefault(),
        //                                        lct_sts_cd = dsCommomlct_sts_cd.Where(x => x.dt_cd == a.lct_sts_cd).Select(x => x.dt_nm).FirstOrDefault(),
        //                                        mt_type_nm = dsCommommt_type_nm.Where(x => x.dt_cd == a.mt_type).Select(x => x.dt_nm).FirstOrDefault(),
        //                                        mt_sts_cd = a.mt_sts_cd,
        //                                    }
        //                  ).ToList().FirstOrDefault();
        //            allData.Add(data);
        //        }
        //        return Json(allData, JsonRequestBehavior.AllowGet);
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        public ActionResult UpdateMTQR_DeliveryList(FGBoxMappingModel model)
        {
            try
            {

                // check input
                if (string.IsNullOrEmpty(model.DlNo))
                {
                    return Json(new { result = false, message = "Vui lòng chọn tên danh sách xuất hàng." }, JsonRequestBehavior.AllowGet);
                }
                StringBuilder listBox = new StringBuilder();
                DateTime dt = DateTime.Now;
                string day_now = dt.ToString("yyyyMMdd");
                string full_date = dt.ToString("yyyy-MM-dd HH:mm:ss");
                var user = Session["userid"] == null ? "" : Session["userid"].ToString();
                
                var html = "";

                for (int i = 0; i < model.ListBoxCode.Count; i++)
                {
                    html += "'" + model.ListBoxCode[i] + "'";
              
                    if (i != model.ListBoxCode.Count() - 1)
                    {
                        html += ',';
                    }
                }




                html = html.Replace("'", "");
                html = "'" + html + "'";

                StringBuilder excuteSql = new StringBuilder($" call ShippingFGToDelivery({html},'{user}','{model.DlNo}')");

                db.Database.ExecuteSqlCommand(excuteSql.ToString());


                //string listBoxCodes = new InitMethods().RemoveLastComma(listBox);
                //var box_mapping = new w_box_mapping();
                //box_mapping.sts = "001";
                //box_mapping.chg_id = user;
                //box_mapping.chg_dt = dt;
                //int UpdateBoxNoMapping = _iFGWmsService.UpdateWBoxMapping(box_mapping,listBoxCodes);

                //var material_info = new w_material_info();
                //material_info.dl_no = model.DlNo;
                //material_info.mt_sts_cd = "000";
                //material_info.lct_cd = "004000000000000000";
                //material_info.from_lct_cd = "004000000000000000";
                //material_info.to_lct_cd = "004000000000000000";
                //material_info.output_dt = full_date;
                //material_info.chg_dt = dt;
                //material_info.chg_id = user;
                //int UpdateWMaterialInfo = _iFGWmsService.UpdateWMaterialInfo(material_info, listBoxCodes);

                //var generalfg = new generalfg();
                //generalfg.dl_no = model.DlNo;
                //generalfg.sts_cd = "000";
                //generalfg.chg_dt = dt;
                //generalfg.chg_id = user;
                //int Updategeneralfg = _iFGWmsService.UpdateGenneral(generalfg, listBoxCodes);

                //var stamp_detail = new stamp_detail();
                //stamp_detail.is_sent ="Y";
                //stamp_detail.chg_dt = dt;
                //stamp_detail.chg_id = user;
                //int Updatestamp_detail = _iFGWmsService.Updatestamp_detail(stamp_detail, listBoxCodes);

                var listDLInfo = _iFGWmsService.GetListDlNo(model.DlNo);

                return Json(new { result = true, message = "Thành công!!!", listDLInfo }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult UpdateMTQR_DeliveryList_old(string data, string dl_no)
        {
            try
            {
                // check input
                if (string.IsNullOrEmpty(dl_no))
                {
                    return Json(new { result = false, message = "Vui lòng chọn tên danh sách xuất hàng." }, JsonRequestBehavior.AllowGet);
                }

                if (string.IsNullOrEmpty(data))
                {
                    return Json(new { result = false, message = "Vui lòng scan mã thùng!!!" }, JsonRequestBehavior.AllowGet);
                }

                DateTime dt = DateTime.Now;
                string day_now = dt.ToString("yyyyMMdd");
                string full_date = dt.ToString("yyyy-MM-dd HH:mm:ss");
                var user = Session["userid"] == null ? "" : Session["userid"].ToString();

                string[] temp_id = data.TrimStart('[').TrimEnd(']').Split(',');
                List<int> temp = new List<int>();
                for (int i = 0; i < temp_id.Length; i++)
                {
                    temp.Add(int.Parse(temp_id[i]));
                }

                var listdata_mt_cd = (from a in db.w_material_info

                                      where temp.Contains(a.wmtid)

                                      select new
                                      {
                                          mt_cd = a.mt_cd
                                      }).ToList();

                var html = "";

                for (int i = 0; i < listdata_mt_cd.Count; i++)
                {
                    html += "'" + listdata_mt_cd[i].mt_cd + "'";
                    if (i != listdata_mt_cd.Count - 1)
                    {
                        html += ',';
                    }
                }

                //UPDATE w_box_mapping  COLUMN mt_sts_cd
                var sql = new StringBuilder();
                sql.Append(" UPDATE  w_box_mapping  ")

                .Append("  SET  w_box_mapping.sts = '001', ")
                .Append(" w_box_mapping.chg_id = '" + user + "'  ,w_box_mapping.chg_dt = '" + full_date + "'  ")

                .Append(" WHERE w_box_mapping.mt_cd in (" + html + ") ");

                using (var cmd = db.Database.Connection.CreateCommand())
                {
                    db.Database.Connection.Open();

                    cmd.CommandText = sql.ToString();
                    var reader = cmd.ExecuteNonQuery();
                    int result1 = Int32.Parse(reader.ToString());
                    db.Database.Connection.Close();
                }

                //UPDATE w_material_info  COLUMN dl_no
                var sql2 = new StringBuilder();
                sql2.Append(" UPDATE  w_material_info  ")

                .Append("  SET w_material_info.dl_no = '" + dl_no + "' , w_material_info.mt_sts_cd = '000', ")
                .Append("   w_material_info.lct_cd = '004000000000000000' , w_material_info.from_lct_cd = '003G01000000000000' , w_material_info.recevice_dt_tims = '" + day_now + "' ,")
                .Append("   w_material_info.to_lct_cd = '004000000000000000' ,w_material_info.chg_id = '" + user + "'")
                //.Append(" ,w_material_info.chg_dt = '" + full_date + "'  ")

                .Append(" WHERE w_material_info.wmtid in (" + data + ") ");

                using (var cmd = db.Database.Connection.CreateCommand())
                {
                    db.Database.Connection.Open();

                    cmd.CommandText = sql2.ToString();
                    var reader = cmd.ExecuteNonQuery();
                    int result1 = Int32.Parse(reader.ToString());
                    db.Database.Connection.Close();
                }

                html = html.Replace("'", "");
                html = "'" + html + "'";

                StringBuilder excuteSql = new StringBuilder($" call spFGWMS_ShippingDL_UpdateMTQR_DeliveryList({html}) ");
                db.Database.ExecuteSqlCommand(excuteSql.ToString());


                //select bảng 1
                var sql_Select = new StringBuilder();
                sql_Select.Append($" SELECT MAX(a.dlid) AS dlid, MAX(a.dl_no) AS dl_no, MAX(a.dl_no) AS dl_no1, MAX(a.dl_nm) AS dl_nm,  MAX(a.remark) AS remark,  (  ")
                .Append($" SELECT IFNULL(SUM(b.gr_qty),0) FROM w_material_info b  ")
                .Append($" WHERE b.dl_no = a.dl_no) AS quantity ")
                .Append($" FROM w_dl_info AS a ")

                .Append($" WHERE a.dl_no = '" + dl_no + "'  ");

                var ddd = new InitMethods().ConvertDataTableToJsonAndReturn(sql_Select);
                return Json(new { result = true, message = "Thành công!!!", data = ddd.Data }, JsonRequestBehavior.AllowGet);


            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult CancelTemDelivery(string buyer_qr)
        {
            if (string.IsNullOrEmpty(buyer_qr))
            {
                return Json(new { result = false, message = "Không tìm được mã tem gói trong cơ sở dữ liệu." }, JsonRequestBehavior.AllowGet);
            }

            var kttt = db.stamp_detail.Where(x => x.buyer_qr == buyer_qr).SingleOrDefault();
            if (kttt == null)
            {
                return Json(new { result = false, message = "Không tìm được mã tem gói trong cơ sở dữ liệu." }, JsonRequestBehavior.AllowGet);
            }
            StringBuilder sql = new StringBuilder($"CALL spfgwms_CancelTemDelivery('{buyer_qr}');");
            try
            {
                db.Database.ExecuteSqlCommand(sql.ToString());
                return Json(new { result = true, message = "Xóa thành công." }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception)
            {
                return Json(new { result = true, message = "Lỗi hệ thống." }, JsonRequestBehavior.AllowGet);

            }
        }
        public JsonResult Cancel1ThungDelivery(string box_no)
        {
            if (string.IsNullOrEmpty(box_no))
            {
                return Json(new { result = false, message = "Không tìm được mã thùng trong cơ sở dữ liệu." }, JsonRequestBehavior.AllowGet);
            }

            //var kttt = db.stamp_detail.Any(x => x.box_code == box_no);
            //if (kttt == false)
            //{
            //    return Json(new { result = false, message = "Không tìm được mã thùng trong cơ sở dữ liệu." }, JsonRequestBehavior.AllowGet);
            //}
            
            StringBuilder sql = new StringBuilder($"CALL spfgwms_Cancel1ThungDelivery('{box_no}');");

            try
            {
                db.Database.ExecuteSqlCommand(sql.ToString());
                return Json(new { result = true, message = "Xóa thành công." }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Lỗi hệ thống." }, JsonRequestBehavior.AllowGet);

            }
        }
        public JsonResult CancelDeliveryAll(string dlid)
        {
            if (string.IsNullOrEmpty(dlid))
            {
                return Json(new { result = false, message = "Dữ liệu không được tìm thấy." }, JsonRequestBehavior.AllowGet);
            }
            var id = Convert.ToInt32(dlid);
            var kttt = db.w_dl_info.Where(x => x.dlid == id).SingleOrDefault();
            if (kttt == null)
            {
                return Json(new { result = false, message = "Dữ liệu không được tìm thấy." }, JsonRequestBehavior.AllowGet);
            }
            StringBuilder sql = new StringBuilder($"CALL spfgwms_CancelDeliveryAll('{kttt.dl_no}');");
            try
            {
                db.Database.ExecuteSqlCommand(sql.ToString());
                return Json(new { result = true, message = "Xóa thành công." }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Lỗi hệ thống." }, JsonRequestBehavior.AllowGet);

            }
        }


        public ActionResult DeleteDelivery(int id)
        {
            try
            {
                //kiểm tra trong mã này có tồn tại ko
                var CheckIsExist = _iFGWmsService.CheckDLExist(id);
                if (CheckIsExist == null)
                {
                    return Json(new { result = false, message = "Code này không tồn tại" }, JsonRequestBehavior.AllowGet);
                }
                //kiểm tra xem đã đơn này đã chứa hàng chưa, nếu chưa là cho xóa
                var CheckAnyGenneral = _iFGWmsService.CheckAnyGenneral(CheckIsExist.dl_no);
                if (CheckAnyGenneral)
                {
                    return Json(new { result = false, message ="Code này đã có mã đóng thùng"}, JsonRequestBehavior.AllowGet);
                }
                int IsSS = _iFGWmsService.DeleteDeliveryForId(id);

                if (IsSS > 0)
                {
                    return Json(new { result = true, id , message ="Xóa thành công"}, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = "Xóa thất bại" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {

                return Json(new { result = false, message = "Lỗi hệ thống" }, JsonRequestBehavior.AllowGet);
            }
        }
        #region Delivery_shipping_FG_Info_popup

        public ActionResult PartialView_DL_Info_Popup(string dl_no)
        {
            ViewBag.dl_no = dl_no;
            return PartialView("~/Views/fgwms/Shipping_Scan/PartialView_DL_Info_Popup.cshtml");
        }

        public ActionResult Get_DL_ShippingScanPP(string dl_no)
        {
            var listdata = (from a in db.w_dl_info

                            where a.dl_no.Equals(dl_no)

                            select new
                            {
                                dl_no = a.dl_no,
                                dl_nm = a.dl_nm,
                                work_dt = a.work_dt,
                                remark = a.remark,
                            }).ToList();

            return Json(new { data = listdata }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Get_DL_ShippingScanListPP(string dl_no)
        {
            try
            {
                //var dsCommomlct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002");
                //var dsCommommt_type_nm = db.comm_dt.Where(x => x.mt_cd == "COM004");
                //var dsCommommt_sts_nm = db.comm_dt.Where(x => x.mt_cd == "WHS005");
                //var dsLocation = db.lct_info;
                //var dsboxMapping = db.w_box_mapping.ToList();

                //var data1 = (from a in db.w_material_info
                //             where a.dl_no.Equals(dl_no)
                //             select new
                //             {
                //                 wmtid = a.wmtid,
                //                 mt_no = a.mt_no,
                //                 mt_type = a.mt_type,
                //                 lot_no = a.lot_no,
                //                 expiry_dt = a.expiry_dt,
                //                 dt_of_receipt = a.dt_of_receipt,
                //                 expore_dt = a.expore_dt,
                //                 sd_sts_cd = a.mt_sts_cd,
                //                 sts_nm = dsCommommt_sts_nm.Where(x => x.dt_cd == a.mt_sts_cd).Select(x => x.dt_nm),

                //                 mt_cd = a.mt_cd,
                //                 bb_no = a.bb_no,
                //                 buyer_qr = a.buyer_qr,
                //                 gr_qty = a.gr_qty,
                //                 recevice_dt_tims = a.recevice_dt_tims,

                //                 from_lct_cd = a.from_lct_cd,
                //                 from_lct_nm = db.lct_info.Where(x => x.lct_cd == a.from_lct_cd).Select(x => x.lct_nm),

                //                 lct_cd = a.lct_cd,
                //                 lct_nm = db.lct_info.Where(x => x.lct_cd == a.lct_cd).Select(x => x.lct_nm),

                //                 to_lct_cd = a.to_lct_cd,
                //                 to_lct_nm = db.lct_info.Where(x => x.lct_cd == a.to_lct_cd).Select(x => x.lct_nm),

                //                 lct_sts_cd = dsCommomlct_sts_cd.Where(x => x.dt_cd == a.lct_sts_cd).Select(x => x.dt_nm),

                //                 mt_type_nm = dsCommommt_type_nm.Where(x => x.dt_cd == a.mt_type).Select(x => x.dt_nm),
                //                 mt_sts_cd = a.mt_sts_cd,
                //                 box_no = db.w_box_mapping.Where(x => x.buyer_cd == a.buyer_qr).Select(x => x.bx_no)
                //             }
                //   ).ToList();

                //view danh sách 
                //StringBuilder sql = new StringBuilder();
                //sql.Append($" SELECT a.wmtid, a.buyer_qr, a.gr_qty, (SELECT dt_nm FROM comm_dt WHERE mt_cd = 'WHS005' AND dt_cd = a.mt_sts_cd) AS sts_nm,  ")
                //   .Append($" (SELECT bx_no FROM w_box_mapping WHERE buyer_cd = a.buyer_qr ) AS box_no,  a.output_dt AS shippingDt ")
                //   .Append($" FROM w_material_info a ")

                //   .Append($" WHERE a.dl_no = '{dl_no}' ")

                //   .Append($" UNION ALL ")
                //   .Append($" SELECT c.id AS wmtid, c.buyer_qr,c.qty, (SELECT dt_nm FROM comm_dt WHERE mt_cd = 'WHS005' AND dt_cd = c.sts_cd) AS sts_nm, ")
                //   .Append($"(SELECT bx_no FROM w_box_mapping WHERE buyer_cd = c.buyer_qr ) AS box_no, ")
                //   .Append($" c.chg_dt  AS shippingDt ")
                //   .Append($" FROM  generalfg c ")
                //   .Append($" WHERE c.dl_no = '{dl_no}' ");


                //var ddd = new InitMethods().ConvertDataTableToJsonAndReturn(sql);

                var listData = _iFGWmsService.GetListShippingScanIDelivery(dl_no);

                return Json(new { data = listData }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult PrintDL_ShippingFG_LIST(string dl_no)
        {
            ViewData["Message"] = dl_no;
            return PartialView("~/Views/fgwms/Shipping_Scan/PrintDL_ShippingFG_LIST.cshtml");
        }

        #endregion Delivery_shipping_FG_Info_popup


        #endregion N_Shipping_Scan_FG

        #region FGWMSGeneral

        public ActionResult inventoryGeneral()
        {
            return View("~/Views/fgwms/Inventory/General.cshtml");
        }
        public JsonResult Search_BuyergetFGGeneral(string buyerCode)
        {
            if (string.IsNullOrEmpty(buyerCode))
            {
                return Json(new { result = false, message = "Vui lòng scan mã Buyer" }, JsonRequestBehavior.AllowGet);
            }
            var kiemtraDt = db.w_material_info.Where(x => x.buyer_qr == buyerCode).SingleOrDefault();
            if (kiemtraDt == null)
            {
                return Json(new { result = false, message = "Mã Buyer không tồn tại" }, JsonRequestBehavior.AllowGet);
            }
            if (kiemtraDt.lct_cd.Equals("003G01000000000000"))
            {
                var sts = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == kiemtraDt.mt_sts_cd).FirstOrDefault().dt_nm;
                return Json(new { result = true, message = "Đã được đưa vào KHO THÀNH PHẨM, Trạng thái là:  " + sts }, JsonRequestBehavior.AllowGet);
            }
            if (kiemtraDt.lct_cd.Equals("006000000000000000"))
            {
                var sts = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == kiemtraDt.mt_sts_cd).FirstOrDefault().dt_nm;
                return Json(new { result = true, message = "Đang ở kho TIMS, Trạng thái là:  " + sts }, JsonRequestBehavior.AllowGet);
            }
            return null;
        }
        public JsonResult getFGGeneral(Pageing paging)
        {
            string buyerCode = Request["buyerCode"] == null ? "" : Request["buyerCode"].Trim();
            string productCode = Request["productCode"] == null ? "" : Request["productCode"].Trim();
            string poCode = Request["poCode"] == null ? "" : Request["poCode"].Trim();
            string recevice_dt_start = Request["recevice_dt_start"] == null ? "" : Request["recevice_dt_start"].Trim();
            string recevice_dt_end = Request["recevice_dt_end"] == null ? "" : Request["recevice_dt_end"].Trim();

            var dateConvert = new DateTime();
            if (DateTime.TryParse(recevice_dt_end, out dateConvert))
            {
                recevice_dt_end = dateConvert.ToString("yyyyMMdd");
            }
            else
            {
                recevice_dt_end = DateTime.MaxValue.ToString("yyyyMMdd");
            }
            if (DateTime.TryParse(recevice_dt_start, out dateConvert))
            {
                recevice_dt_start = dateConvert.ToString("yyyyMMdd");
            }
            else
            {
                recevice_dt_start = DateTime.MinValue.ToString("yyyyMMdd");
            }


            StringBuilder sql = new StringBuilder();

            sql.Append($"call spFGWMMS_GetFGGeneral('{productCode}','{poCode}','{recevice_dt_start}','{recevice_dt_end}','{buyerCode}')");
            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);

            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<string>("product"));
            return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
        }

        public JsonResult getFGgeneralDetail(Pageing paging)
        {
            try
            {
                string id = Request["id"] == null ? "" : Request["id"].Trim();
                string buyerCode = Request["buyerCode"] == null ? "" : Request["buyerCode"].Trim();
                string poCode = Request["poCode"] == null ? "" : Request["poCode"].Trim();
                string recevice_dt_start = Request["recevice_dt_start"] == null ? "" : Request["recevice_dt_start"].Trim();
                string recevice_dt_end = Request["recevice_dt_end"] == null ? "" : Request["recevice_dt_end"].Trim();

                if (string.IsNullOrEmpty(recevice_dt_start))
                {
                    recevice_dt_start = DateTime.MinValue.ToString("yyyyMMdd");
                }
                else
                {
                    recevice_dt_start = recevice_dt_start.Replace("-", "");
                }
                if (string.IsNullOrEmpty(recevice_dt_end))
                {
                    recevice_dt_end = DateTime.MaxValue.ToString("yyyyMMdd");
                }
                else
                {
                    recevice_dt_end = recevice_dt_end.Replace("-", "");
                }
                if (string.IsNullOrEmpty(id))
                {
                    return Json(new { result = false, st = "error" }, JsonRequestBehavior.AllowGet);
                }
                var id_actual_pr = Convert.ToInt32(id);
                var productCode = db.w_actual_primary.Find(id_actual_pr).product;
                StringBuilder sql = new StringBuilder($"CALL spFGWMMS_GetFGgeneralDetail('{productCode}', '{poCode}', '{recevice_dt_start}', '{recevice_dt_end}', '{buyerCode}');");

                DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);

                int total = dt.Rows.Count;
                var result = dt.AsEnumerable().OrderBy(x => x.Field<string>("end_production_dt"));
                return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult PrintFGGeneral()
        {
            string[] keys = Request.Form.AllKeys;

            var value = "";
            value = Request.Form[keys[0]];

            ViewData["Message"] = value;
            return View("~/Views/fgwms/Inventory/PrintGeneral.cshtml");
        }

        public ActionResult qrGeneral(string mt_no)
        {
            if (mt_no != "")
            {
                StringBuilder varname1 = new StringBuilder();
                varname1.Append("SELECT a.wmtid,a.mt_cd,b.mt_nm,CONCAT(ifnull(a.gr_qty,''),ifnull(b.unit_cd,'')) lenght,CONCAT(ifnull(b.width,0),'*',ifnull(a.gr_qty,0)) AS size,ifnull(b.spec,0) spec,a.mt_no, ");
                varname1.Append(" CONCAT((case when b.bundle_unit ='Roll' then  (a.gr_qty/b.spec) ELSE a.gr_qty END),' ',b.bundle_unit) qty, ");
                varname1.Append(" a.input_dt recevice_dt, ");
                varname1.Append("(SELECT dt_nm FROM comm_dt WHERE comm_dt.dt_cd=a.mt_sts_cd AND comm_dt.mt_cd='WHS005') sts_nm,a.lot_no,a.expore_dt,a.dt_of_receipt,a.expiry_dt ");
                varname1.Append("FROM w_material_info a ");
                varname1.Append("LEFT JOIN d_material_info  b ON a.mt_no=b.mt_no ");
                varname1.Append("WHERE a.lct_cd LIKE '003%' AND a.wmtid IN (" + mt_no + ")  ORDER BY a.mt_no, a.mt_cd ");
                return new InitMethods().ConvertDataTableToJsonAndReturn(varname1);
            }
            return View();
        }
        public JsonResult getbuyer_popup(Pageing paging)
        {
            try
            {
                string productCode = Request["productCode"] == null ? "" : Request["productCode"].Trim();
                string poCode = Request["poCode"] == null ? "" : Request["poCode"].Trim();
                string recevice_dt_start = Request["recevice_dt_start"] == null ? "" : Request["recevice_dt_start"].Trim();
                string recevice_dt_end = Request["recevice_dt_end"] == null ? "" : Request["recevice_dt_end"].Trim();

                if (string.IsNullOrEmpty(recevice_dt_start))
                {
                    recevice_dt_start = DateTime.MinValue.ToString("yyyyMMdd");
                }
                else
                {
                    recevice_dt_start = recevice_dt_start.Replace("-", "");
                }
                if (string.IsNullOrEmpty(recevice_dt_end))
                {
                    recevice_dt_end = DateTime.MaxValue.ToString("yyyyMMdd");
                }
                else
                {
                    recevice_dt_end = recevice_dt_end.Replace("-", "");
                }

                StringBuilder sql = new StringBuilder($"CALL popup_buyer_qr('{productCode}', '{poCode}', '{recevice_dt_start}', '{recevice_dt_end}');");

                DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);

                int total = dt.Rows.Count;
                var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("wmtid"));
                return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public ActionResult ExportFGgeneralToExcel(string productCode, string poCode, string recevice_dt_start, string recevice_dt_end)
        {
            
            if (string.IsNullOrEmpty(productCode))
            {
                productCode = "";
            }
            if (string.IsNullOrEmpty(poCode))
            {
                poCode = "";
            }
            if (string.IsNullOrEmpty(recevice_dt_start))
            {
                recevice_dt_start = DateTime.MinValue.ToString("yyyyMMdd");
            }
            else
            {
                recevice_dt_start = recevice_dt_start.Replace("-", "");
            }
            if (string.IsNullOrEmpty(recevice_dt_end))
            {
                recevice_dt_end = DateTime.MaxValue.ToString("yyyyMMdd");
            }
            else
            {
                recevice_dt_end = recevice_dt_end.Replace("-", "");
            }
            StringBuilder sql = new StringBuilder($"CALL spFGWMS_ExportFGgeneralToExcel('{productCode}', '{poCode}', '{recevice_dt_start}', '{recevice_dt_end}');");
            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);

            DataSet ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables[0].TableName = "FG_Inventory";

            using (XLWorkbook wb = new XLWorkbook())
            {
                var ws = wb.AddWorksheet(dt);

                ws.Columns().AdjustToContents();
                ws.Rows().AdjustToContents();
              
                ws.Cells("A1").Value = "Lot Code";
                ws.Cells("B1").Value = "Buyer";
                ws.Cells("C1").Value = "PO";
                ws.Cells("D1").Value = "Composite";
                ws.Cells("E1").Value = "QTy (Roll/EA)";
                ws.Cells("F1").Value = "Status";
                ws.Cells("G1").Value = "Recevied Date";
                ws.Cells("H1").Value = "Product";
                ws.Cells("I1").Value = "Expiry Date";

                wb.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                wb.Style.Alignment.ShrinkToFit = true;
                wb.Style.Font.Bold = true;

                Response.Clear();
                Response.Buffer = true;
                Response.Charset = "";
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;filename=FG Inventory.xlsx");
                using (MemoryStream MyMemoryStream = new MemoryStream())
                {
                    wb.SaveAs(MyMemoryStream);
                    MyMemoryStream.WriteTo(Response.OutputStream);
                    Response.Flush();
                    Response.End();
                }
            }
            return View();
            //return View("~/Views/WIP/Inventory/General.cshtml");
        }

        #endregion FGWMSGeneral

        #region FG Mapping

        public ActionResult Mapping_Box()
        {
            return SetLanguage("~/Views/fgwms/Mapping/MappingBox.cshtml");
          
        }

        public JsonResult GetBuyerCode(string buyerCode, string materialCode, string productCode, Pageing paging)
        {
            if (string.IsNullOrEmpty(buyerCode))
            {
                buyerCode = "";
            }
            if (string.IsNullOrEmpty(materialCode))
            {
                materialCode = "";
            }

            if (string.IsNullOrEmpty(productCode))
            {
                productCode = "";
            }
          
            var tempData = _iFGWmsService.GetListStampBuyerQr(buyerCode, productCode);

            List<d_style_info> stampTypeList = db.d_style_info.ToList();

            int start = (paging.page - 1) * paging.rows;
            int end = (paging.page - 1) * paging.rows + paging.rows;
            var data1 = tempData
                            .Select(x => new MappedProductModel
                            {
                                Id = x.id,
                                MaterialNo = x.product_code,
                                MaterialCode = x.md_cd,
                                BuyerCode = x.buyer_qr,
                                Quantity = x.qty.ToString(),
                                StampType = stampTypeList.Where(a => a.style_no.Contains(x.product_code)).Select(a => a.stamp_code).ToList().FirstOrDefault()
                            })
                            .ToList();

            int totals = data1.Count();
            int totalPages = (int)Math.Ceiling((float)totals / paging.rows);
            var dataaaa = data1.Skip<MappedProductModel>(start).Take(paging.rows);
            //IEnumerable<DatawActualPrimary> dataactual = aaa.Skip<DatawActualPrimary>(start).Take(pageing.rows);
            var jsonReturn = new
            {
                total = totalPages,
                page = paging.page,
                records = totals,
                rows = dataaaa
            };
            return Json(jsonReturn, JsonRequestBehavior.AllowGet);

            //return Json(new { data, flag = true, message = "" }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult BuyerCodeGEtProuct(string buyerCode)
        {
            if (string.IsNullOrEmpty(buyerCode))
            {
                return Json(new { flag = false, message = "Quét mã tem gói để tiếp tục." }, JsonRequestBehavior.AllowGet);
            }
            try
            {


                var buyer = db.w_material_info.Where(x => x.buyer_qr == buyerCode).ToList();
                if (buyer.Count == 0)
                {
                    return Json(new { flag = false, message = "Code không tồn tại." }, JsonRequestBehavior.AllowGet);
                }
                var id_actual = buyer.FirstOrDefault().id_actual;

                string sql = " Select "
                            + " (SELECT product FROM w_actual_primary AS p  JOIN w_actual AS q  ON p.at_no = q.at_no   WHERE q.id_actual = x.id_actual) AS ProductNo "

                            + " FROM w_material_info AS x"

                            + " WHERE x.buyer_qr= '" + buyerCode + "' ";
                var data = db.Database.SqlQuery<MappedProductModel>(sql).ToList().Select(x => x.ProductNo);





                return Json(new { data, flag = true, message = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { flag = false, message = "Lỗi hệ thống" }, JsonRequestBehavior.AllowGet);
            }
        }
        //[HttpPost]
        //public JsonResult BuyerCodeScanning(string buyerCode, string type)
        //{
        //    if (string.IsNullOrEmpty(buyerCode))
        //    {
        //        return Json(new { flag = false, message = "Quét mã tem gói để tiếp tục." }, JsonRequestBehavior.AllowGet);
        //    }
        //    try
        //    {
        //        var buyer = db.w_material_info.Where(x => x.buyer_qr == buyerCode).ToList();//ko ở trong tồn của MES 
        //        if (buyer.Count == 0)
        //        {
        //            //kiểm tra tồn SAP
        //            var buyerSAP = db.generalfgs.Where(x => x.buyer_qr == buyerCode && x.sts_cd.Equals("001")).ToList();
        //            if (buyerSAP.Count == 0)
        //            {
        //                return Json(new { flag = false, message = "Mã tem gói không tồn kho" }, JsonRequestBehavior.AllowGet);
        //            }

        //            else
        //            {
        //                string sql1 = " Select x.id as Id , x.md_cd as MaterialCode, x.product_code as ProductNo, x.buyer_qr as BuyerCode, "
        //                   + " x.qty as Quantity , 'SAP' as TypeSystem, CONCAT(SUBSTRING(x.lot_no,1,4),'-', SUBSTRING(x.lot_no,5,2),'-', SUBSTRING(x.lot_no,7,2)) AS lot_date "

        //                   + " FROM generalfg AS x"

        //                   + " WHERE x.buyer_qr= '" + buyerCode + "' ";
        //                var data1 = db.Database.SqlQuery<MappedProductModel>(sql1).FirstOrDefault();
        //                return Json(new { data = data1, flag = true, message = "" }, JsonRequestBehavior.AllowGet);
        //            }

        //        }

        //        //var stampType = db.stamp_detail.Where(x => x.buyer_qr == buyerCode).SingleOrDefault().stamp_code;
        //        //if (!string.IsNullOrEmpty(type) && stampType != type && type != "undefined")
        //        //{
        //        //    return Json(new { flag = false, message = "Tem gói không cùng loại sản phẩm. Vui lòng kiểm tra lại." }, JsonRequestBehavior.AllowGet);
        //        //}




        //        var existedProductList = new HashSet<string>(db.w_box_mapping.Where(x => x.buyer_cd == buyerCode).Select(x => x.mt_cd));

        //        var tempData = db.w_material_info
        //                    .Where(x => x.buyer_qr != null && x.buyer_qr == buyerCode && x.lct_cd.Contains("003") && x.gr_qty > 0)
        //                    .ToList();
        //        var checkData = tempData.Where(p => !existedProductList.Contains(p.mt_cd)).ToList();

        //        if (checkData.Count < 1)
        //        {
        //            return Json(new { flag = false, message = "Không có product nào thuộc về code này hoặc tất cả các product đã được mapped" }, JsonRequestBehavior.AllowGet);
        //        }

        //        string sql = " Select x.wmtid as Id , (SELECT lot_date FROM stamp_detail where  buyer_qr = x.buyer_qr) AS lot_date, x.buyer_qr as BuyerCode, "
        //                      + " x.gr_qty as Quantity,   'MES' as TypeSystem, "
        //                      + " x.product AS ProductNo "

        //                      + " FROM w_material_info AS x"

        //                      + " WHERE x.buyer_qr= '" + buyerCode + "' ";
        //        var data = db.Database.SqlQuery<MappedProductModel>(sql).FirstOrDefault();




        //        //var data = checkData
        //        //            .Select(x => new MappedProductModel
        //        //            {
        //        //                Id = x.wmtid,
        //        //                MaterialCode = x.mt_cd,
        //        //                MaterialNo = x.mt_no,
        //        //                ProductNo = (SELECT product FROM w_actual_primary AS p JOIN w_actual AS q ON p.at_no = q.at_no    WHERE q.id_actual = a.id_actual
        //        //                BuyerCode = x.buyer_qr,
        //        //                Quantity = x.gr_qty.ToString(),
        //        //                Count = checkData.Count,
        //        //                StampType = stampType
        //        //            })
        //        //            .ToList();
        //        return Json(new { data = data, flag = true, message = "" }, JsonRequestBehavior.AllowGet);
        //    }
        //    catch (Exception)
        //    {
        //        return Json(new { flag = false, message = "Lỗi hệ thống" }, JsonRequestBehavior.AllowGet);
        //    }
        //}
        [HttpPost]
        public JsonResult BuyerCodeScanning(string buyerCode, string type)
        {
            if (string.IsNullOrEmpty(buyerCode))
            {
                return Json(new { flag = false, message = "Quét mã tem gói để tiếp tục." }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                string sql1 = " Select x.id as Id ,x.sts_cd as Status, x.md_cd as MaterialCode, SUBSTRING_INDEX(x.buyer_qr, 'DZIH', 1) ProductNo, x.buyer_qr as BuyerCode, "
                    + " x.qty as Quantity , x.lot_no  as lot_date "
                  //  + "x.product_code as ProductNo"
                          + " FROM generalfg AS x"

                    + " WHERE x.buyer_qr= '" + buyerCode + "'  ";

                var data1 = db.Database.SqlQuery<MappedProductModel>(sql1).FirstOrDefault();

                if (data1 != null)
                {
                    if (data1.Status.Equals("001"))
                    {
                        return Json(new { data = data1, flag = true, message = "" }, JsonRequestBehavior.AllowGet);
                    }
                    if (data1.Status.Equals("101"))
                    {
                        return Json(new { flag = false, message = "Mã tem gói đã đưa vào kho" }, JsonRequestBehavior.AllowGet);
                    }
                    if (data1.Status.Equals("010"))
                    {
                        return Json(new { flag = false, message = "Mã tem gói đã được đóng thùng" }, JsonRequestBehavior.AllowGet);
                    }
                }
                return Json(new { flag = false, message = "Mã tem gói không có trong kho" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { flag = false, message = "Lỗi hệ thống" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult ShowingScanBuyerTemporary(List<FGShippingByExcelModel> tempList)
        {
            if (tempList == null)
            {
                return Json(new { flag = false, message = "Chưa chọn File excel" }, JsonRequestBehavior.AllowGet);
            }
            List<MappedProductModel> listData = new List<MappedProductModel>();

            if (tempList.Count > 0)
            {
                string BuyerCode = tempList[0].Code;
                var STAPM = _iFGWmsService.FindGeneralfg(tempList[0].Code);
                //lấy product để xét file excel đang tạo tem thùng cho product nào. 1 file excel tạo tem thùng chỉ 1 product.
                string temgoi = "";
                for (int i = 0; i < tempList.Count; i++)
                {
                    temgoi = "";
                    temgoi = tempList[i].Code;
                    //var buyer = db.w_material_info.Where(x => x.buyer_qr == item.Code && x.lct_cd.StartsWith("003")).ToList();
                    //kiểm tra có đủ điều kiện để đóng thùng không
                    var buyer = _iFGWmsService.IsCheckBuyerExist(tempList[i].Code).ToList();
                    //kiểm tra tem đã đóng thùng chưa
                    var STAPMBox = _iFGWmsService.IsFindStampBox(tempList[i].Code);
                    if (STAPMBox != null)
                    {
                        return Json(new { flag = false, message = $"Tem gói: {temgoi} đã được đóng thùng rồi <br/> Mã thùng :{STAPMBox.bx_no} <br/> Hàng chứa tem bị lỗi trong file excel:  {i + 2}" }, JsonRequestBehavior.AllowGet);
                    }

                    if (buyer.Count == 0)
                    {
                        return Json(new { flag = false, message = $"Tem này chưa được đưa vào kho <br/>{temgoi}<br/> Hàng chứa tem bị lỗi: {i + 2}" }, JsonRequestBehavior.AllowGet);
                    }
                    if (buyer[0].ProductNo != STAPM.product_code)
                    {
                        return Json(new { flag = false, message = $"Tem: {temgoi} không cùng loại product { buyer[0].ProductNo} <br/> Hàng chứa tem bị lỗi:  {i + 2}" }, JsonRequestBehavior.AllowGet);
                    }
                    
                    var data = buyer
                            .Select(x => new MappedProductModel
                            {
                                Id = x.Id,

                                BuyerCode = x.BuyerCode,
                                Quantity = x.Quantity.ToString(),
                                ProductNo = x.ProductNo.Replace("-",""),
                                TypeSystem = "MES",
                                lot_date = x.lot_date
                            })
                            .ToList().FirstOrDefault();

                    var checkData1 = listData.Where(p => p.Id.Equals(data.Id)).ToList();
                    if (checkData1.Count == 0)
                    {
                        listData.Add(data);
                    }
                }
                return Json(new { listData, flag = true, message = "" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { flag = false, message = "File excel không hợp lệ." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult BoxCodeScanning(string boxCode)
        {
            if (string.IsNullOrEmpty(boxCode))
            {
                return Json(new { flag = false, message = "Quét mã box để tiếp tục." }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                List<IGrouping<string, w_box_mapping>> boxList = db.w_box_mapping.GroupBy(x => x.bx_no).Select(x => x).ToList();
                foreach (var item in boxList)
                {
                    if (item.Key == boxCode && item.ElementAtOrDefault(0)?.sts == "001")
                    {
                        return Json(new { flag = false, message = "Mã box này đã được chuyển đi." }, JsonRequestBehavior.AllowGet);
                    }
                }
                StringBuilder sql = new StringBuilder($"CALL spfgwms_BoxCodeScanning('{boxCode}');");
                List<ScanBoxModel> list = new InitMethods().ConvertDataTable<ScanBoxModel>(sql);
                if (list.Count > 0)
                {
                    return Json(new { data = list[0], flag = true, message = "" }, JsonRequestBehavior.AllowGet);
                }
                ScanBoxModel data = new ScanBoxModel();
                data.bx_no = boxCode;
                data.totalQty = 0;
                return Json(new { data, flag = true, message = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { flag = false, message = "Lỗi hệ thống." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult CheckMappingBox(FGBoxMappingModel model)
        {

            StringBuilder sql = new StringBuilder();
            StringBuilder idsList = new StringBuilder();
            if (model.Wmtids == null || model.Wmtids.Count == 0)
            {
                return Json(new { flag = false, message = "Vui lòng scan tem gói để tiếp tục." });

            }
            if (model.Wmtids != null)
            {
                foreach (var item in model.Wmtids)
                {
                    idsList.Append($"'{item}',");

                }
            }
            string listId = new InitMethods().RemoveLastComma(idsList);
            //string userId = Session["userid"] == null ? "" : Session["userid"].ToString();
            try
            {
                sql.Append($" SELECT c.id, c.buyer_qr,c.qty, c.product_code,  c.lot_no as  lot_date ")
                    .Append($" FROM generalfg c ")
                    .Append($" WHERE c.id IN ({listId})  AND c.sts_cd = '001' ");

                List<FGMappingBoxModel> listData = new List<FGMappingBoxModel>();
                listData = new InitMethods().ConvertDataTableToList<FGMappingBoxModel>(sql);
                if (listData.Count == 0)
                {
                    return Json(new { flag = false, message = "Không tìm thấy mã tem gói trong tồn kho" });
                }

                FGMappingBoxModel minLOTDateInList = listData.FirstOrDefault();
                StringBuilder idStampDetail = new StringBuilder();
                string proCode = minLOTDateInList.product_code;

                foreach (var item in listData)
                {
                    idStampDetail.Append($"'{item.id}',");
                }
                string listIdStampDetail = new InitMethods().RemoveLastComma(idStampDetail);

                StringBuilder sqlStampDetail = new StringBuilder();
                sqlStampDetail.Append($" SELECT  MIN(b.lot_no) as lot_date from generalfg b where b.product_code = '{proCode}' and b.id  in ({listIdStampDetail}); ");
                List<LotDateModel> str = new InitMethods().ConvertDataTableToList<LotDateModel>(sqlStampDetail);
                if (str.Count == 0)
                {
                    return Json(new { flag = true, message = "" });
                }
                else
                {
                    return Json(new { result = true, exist = false, message = "" });
                    //string minLotDate = str.FirstOrDefault().lot_date;
                    //if (minLotDate == "")
                    //{
                    //    return Json(new { flag = true, message = "Lot no đang rỗng, vui lòng nhập lot no bên tồn kho FG" });
                    //}
                    //if (DateTime.ParseExact(minLotDate, "yyyy-MM-dd", CultureInfo.InvariantCulture) >= DateTime.ParseExact(minLOTDateInList.lot_date, "yyyy-MM-dd", CultureInfo.InvariantCulture))
                    //{
                    //    return Json(new { result = true, exist = false, message = "" });
                    //}
                    //else
                    //{
                    //    return Json(new { result = true, exist = true, message = "" });
                    //}
                }

            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống." });
            }
        }

        //[HttpPost]
        //public JsonResult MappingBox(FGBoxMappingModel model)
        //{
        //    StringBuilder sql = new StringBuilder();
        //    StringBuilder idsListMEs = new StringBuilder();
        //    StringBuilder idsListsAP = new StringBuilder();
        //    StringBuilder ids = new StringBuilder();
        //    if (model.WmtidMES == null || model.WmtidMES.Count == 0)
        //    {
        //        if (model.WmtidsAP == null || model.WmtidsAP.Count == 0)
        //        {

        //            return Json(new { flag = false, message = "Vui lòng scan tem gói để tiếp tục." });
        //        }

        //    }
        //    if (model.WmtidMES != null)
        //    {
        //        foreach (var item in model.WmtidMES)
        //        {
        //            idsListMEs.Append($"'{item}',");

        //        }
        //    }
        //    if (model.WmtidsAP != null)
        //    {
        //        foreach (var item in model.WmtidsAP)
        //        {
        //            idsListsAP.Append($"'{item}',");

        //        }
        //    }

        //    string listMes = new InitMethods().RemoveLastComma(idsListMEs);
        //    string listsAP = new InitMethods().RemoveLastComma(idsListsAP);

        //    sql.Append($" SELECT * FROM (SELECT a.wmtid, b.id, b.buyer_qr, b.standard_qty, b.lot_date, b.product_code, b.stamp_code ")
        //          .Append($" FROM w_material_info a ")
        //          .Append($" JOIN stamp_detail b ON a.buyer_qr = b.buyer_qr ")
        //          .Append($" WHERE (b.is_sent = 'N' OR b.is_sent IS NULL) ")
        //          .Append($" AND a.wmtid IN ({listMes}) AND a.lct_cd = '003G01000000000000' AND a.mt_sts_cd = '001'  ")

        //          .Append($" UNION ALL ")
        //          .Append($" SELECT c.id AS wmtid, d.id , c.buyer_qr,c.qty, d.lot_date, c.product_code, d.stamp_code  ")
        //          .Append($" FROM generalfg c ")
        //          .Append($" JOIN stamp_detail d ON c.buyer_qr = d.buyer_qr ")
        //          .Append($" WHERE (d.is_sent = 'N' OR d.is_sent IS NULL) AND c.id IN ({listsAP})  AND c.sts_cd = '001') AS datatable ")
        //          .Append($" ORDER BY STR_TO_DATE(datatable.lot_date, '%Y-%m-%d') ASC ; ");

        //    List<FGMappingBoxModel> listData = new List<FGMappingBoxModel>();
        //    listData = new InitMethods().ConvertDataTableToList<FGMappingBoxModel>(sql);
        //    int totalQty = 0;
        //    string buyerQRCode = listData.FirstOrDefault().buyer_qr;
        //    stamp_detail stampdetail = new stamp_detail();
        //    d_style_info product = new d_style_info();
        //    using (Entities db = new Entities())
        //    {
        //        stampdetail = db.stamp_detail.Where(x => x.buyer_qr == buyerQRCode).FirstOrDefault();
        //        product = db.d_style_info.Where(x => x.style_no == stampdetail.product_code).FirstOrDefault();
        //    }
        //    string stampCode = stampdetail.stamp_code;

        //    //LẤY THÔNG TIN ĐỂ TẠO MÃ BOX
        //    string productCode = stampdetail.product_code;
        //    string exp = product.expiry_month;
        //    int i;
        //    int j = 0;
        //    bool success = Int32.TryParse(exp, out i);
        //    if (success)
        //    {
        //        j = i;
        //    }

        //    string vendorCode = stampdetail.vendor_code;
        //    string vendorLine = stampdetail.vendor_line;
        //    string labelPrinter = stampdetail.label_printer;
        //    string sampleCode = stampdetail.is_sample;
        //    string pcnCode = stampdetail.pcn;

        //    //CHỌN 1 LOT DATE BẤT KỲ TRONG LIST TEM GÓI ĐƯỢC ĐÓNG THÙNG
        //    //string mfgDate = new CreateBuyerQRController().DateFormatByShinsungRule(stampdetail.lot_date); cũ

        //    DateTime dt = DateTime.Now; //Today 
        //    string day_now = dt.ToString("yyyyMMdd");
        //    string mfgDate = new CreateBuyerQRController().DateFormatByShinsungRule(day_now);//lấy ngày tạo mã thùng lớn 
        //    string tempBoxQR = string.Concat(productCode.Replace("-", ""), vendorCode, vendorLine, labelPrinter, sampleCode, pcnCode, mfgDate);


        //    if (vendorCode == null)
        //    {
        //        var gtri5 = buyerQRCode.Substring(10, 8);
        //        tempBoxQR = string.Concat(productCode.Replace("-", ""), gtri5, mfgDate);
        //    }

        //    StringBuilder countSql = new StringBuilder();
        //    countSql.Append($" SELECT MAX(a.id) AS id FROM stamp_detail a WHERE a.box_code IS NOT NULL AND a.box_code LIKE '{tempBoxQR}%' GROUP BY a.box_code;");
        //    List<ListIntModel> listInt = new InitMethods().ConvertDataTableToList<ListIntModel>(countSql);
        //    int num = listInt.Count + 1;
        //    foreach (var item in listData)
        //    {
        //        totalQty += int.Parse(item.standard_qty);
        //    }
        //    string boxQR = string.Concat(tempBoxQR, new CreateBuyerQRController().BuyerQRSerialFormat(num), new CreateBuyerQRController().ProductQuantityFormatForBoxQR(totalQty), new CreateBuyerQRController().ChangeNumberToCharacter(j).ToString());

        //    try
        //    {
        //        using (Entities db = new Entities())
        //        {
        //            foreach (var item in listData)
        //            {
        //                ids.Append("'" + item.id + "',");
        //            }
        //            string idStr = new InitMethods().RemoveLastComma(ids);
        //            StringBuilder executeSql = new StringBuilder($" UPDATE stamp_detail SET box_code = '{boxQR}' WHERE id IN ({idStr}) ;");
        //            db.Database.ExecuteSqlCommand(executeSql.ToString());

        //            StringBuilder mesID = new StringBuilder();
        //            StringBuilder sapID = new StringBuilder();

        //            if (model.WmtidMES != null)
        //            {
        //                foreach (var item in model.WmtidMES)
        //                {
        //                    mesID.Append(item + ",");

        //                }
        //            }
        //            if (model.WmtidsAP != null)
        //            {
        //                foreach (var item in model.WmtidsAP)
        //                {
        //                    sapID.Append(item + ",");

        //                }
        //            }

        //            string mesIDStr = new InitMethods().RemoveLastComma(mesID).Replace("'", "");
        //            string sapIDStr = new InitMethods().RemoveLastComma(sapID).Replace("'", "");
        //            string userId = Session["userid"] == null ? "" : Session["userid"].ToString();

        //            StringBuilder insertIntoBoxMappingTable = new StringBuilder($"CALL spfgwms_MappingBox('{boxQR}','{product.style_no}','{mesIDStr}','{sapIDStr}','{userId}');");
        //            //db.Database.ExecuteSqlCommand(insertIntoBoxMappingTable.ToString());

        //            return new InitMethods().JsonResultAndMessageFromQuery(insertIntoBoxMappingTable, "Tạo tem thùng thành công.");
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        return Json(new { result = false, message = "Lỗi hệ thống." }, JsonRequestBehavior.AllowGet);
        //        throw;
        //    }
        //}
        [HttpPost]
        public JsonResult MappingBox(FGBoxMappingModel model)
        {
            StringBuilder sql = new StringBuilder();
            StringBuilder ids = new StringBuilder();
            if (model.Wmtids == null || model.Wmtids.Count == 0)
            {
                return Json(new { flag = false, message = "Vui lòng scan tem gói để tiếp tục." });

            }
            if (model.Wmtids != null)
            {
                foreach (var item in model.Wmtids)
                {
                    ids.Append($"'{item}',");

                }
            }

            string listId = new InitMethods().RemoveLastComma(ids);

            sql.Append($" SELECT c.id, c.buyer_qr,c.qty standard_qty, c.product_code,c.lot_no as  lot_date ")
                      .Append($" FROM generalfg c ")
                      .Append($" WHERE c.id IN ({listId})  AND c.sts_cd = '001' order by  c.lot_no   ");

            List<FGMappingBoxModel> listData = new List<FGMappingBoxModel>();

            listData = new InitMethods().ConvertDataTableToList<FGMappingBoxModel>(sql);
           
            int totalQty = 0;
            string buyerQRCode = listData.LastOrDefault().buyer_qr;
            stamp_detail stampdetail = new stamp_detail();
            d_style_info product = new d_style_info();
            using (Entities db = new Entities())
            {
                stampdetail = db.stamp_detail.Where(x => x.product_code == model.ProductCode).FirstOrDefault();
                product = db.d_style_info.Where(x => x.style_no.Replace("-","") == model.ProductCode.Replace("-","")).FirstOrDefault();
            }
     
            //KIỂM TRA product có phải bất quy tắc 
            var typeProduct = _iFGWmsService.ChecktypeProduct(product.style_no);
            if (typeProduct.Equals(0))
            {
                if (stampdetail == null)
                {
                    return Json(new { flag = false, message = "Vui lòng tạo một tem gói ở hệ thống MES để làm tiền đề tạo tem thùng" });
                }
                if (product == null)
                {
                    return Json(new { flag = false, message = "Product này chưa được đăng kí ở hệ thống." });
                }
                string stampCode = stampdetail.stamp_code;

                //LẤY THÔNG TIN ĐỂ TẠO MÃ BOX
                string productCode = stampdetail.product_code;
                string exp = product.expiry_month;
                int i;
                int j = 0;
                bool success = Int32.TryParse(exp, out i);
                if (success)
                {
                    j = i;
                }

                string vendorCode = stampdetail.vendor_code;
                string vendorLine = stampdetail.vendor_line;
                string labelPrinter = stampdetail.label_printer;
                string sampleCode = stampdetail.is_sample;
                string pcnCode = stampdetail.pcn;

                //CHỌN 1 LOT DATE BẤT KỲ TRONG LIST TEM GÓI ĐƯỢC ĐÓNG THÙNG
                //string mfgDate = new CreateBuyerQRController().DateFormatByShinsungRule(stampdetail.lot_date); cũ

                DateTime dt = DateTime.Now; //Today 
                string day_now = dt.ToString("yyyyMMdd");


                var lastedItem = listData.LastOrDefault();
                //string day_now = lastedItem != null ? lastedItem.lot_date.Replace("-", "") : string.Empty;

                string mfgDate = new InitMethods().DateFormatByShinsungRule(day_now);//lấy ngày tạo mã thùng lớn 
                string tempBoxQR = string.Concat(productCode.Replace("-", ""), vendorCode, vendorLine, labelPrinter, sampleCode, pcnCode, mfgDate);


                if (vendorCode == null)
                {
                    var gtri5 = buyerQRCode.Substring(10, 8);
                    tempBoxQR = string.Concat(productCode.Replace("-", ""), gtri5, mfgDate);
                }

                StringBuilder countSql = new StringBuilder();
                //countSql.Append($" SELECT MAX(a.bmno) AS id FROM w_box_mapping a WHERE a.bx_no IS NOT NULL AND a.bx_no LIKE '{tempBoxQR}%' GROUP BY a.bx_no;");
                countSql.Append($" SELECT SUBSTRING(max(a.bx_no), LENGTH('{tempBoxQR}')+1, 3) AS bientang FROM w_box_mapping a WHERE a.bx_no IS NOT NULL AND a.bx_no LIKE '{tempBoxQR}%' ORDER BY a.bx_no DESC LIMIT 1; ");
                List<ListIntModel> listInt = new InitMethods().ConvertDataTableToList<ListIntModel>(countSql);
                int num = 1;
                if (listInt[0].bientang != null && listInt[0].bientang != "")
                {
                    num = int.Parse(listInt[0].bientang) + 1;
                }

                foreach (var item in listData)
                {
                    totalQty += int.Parse(item.standard_qty);
                }
                string boxQR = string.Concat(tempBoxQR, new InitMethods().BuyerQRSerialFormat(num), new InitMethods().ProductQuantityFormatForBoxQR(totalQty), new InitMethods().ChangeNumberToCharacter(j).ToString());

                try
                {
                    using (Entities db = new Entities())
                    {
                        foreach (var item in listData)
                        {
                            ids.Append("'" + item.buyer_qr + "',");
                        }
                        string idStr = new InitMethods().RemoveLastComma(ids);
                        StringBuilder executeSql = new StringBuilder($" UPDATE stamp_detail SET box_code = '{boxQR}' WHERE buyer_qr IN ({idStr}) ;");
                        db.Database.ExecuteSqlCommand(executeSql.ToString());

                        StringBuilder listID = new StringBuilder();

                        if (model.Wmtids != null)
                        {
                            foreach (var item in model.Wmtids)
                            {
                                listID.Append(item + ",");

                            }
                        }

                        string IDStr = new InitMethods().RemoveLastComma(listID).Replace("'", "");
                        string userId = Session["userid"] == null ? "" : Session["userid"].ToString();

                        StringBuilder insertIntoBoxMappingTable = new StringBuilder();

                        insertIntoBoxMappingTable.Append($" INSERT INTO w_box_mapping (bx_no, buyer_cd, gr_qty, mapping_dt, sts, use_yn, del_yn, reg_id, reg_dt, chg_id, chg_dt,type,product) ");
                        insertIntoBoxMappingTable.Append($"SELECT '{boxQR}', a.buyer_qr, a.qty, CURDATE(), '000', 'Y', 'N', '{userId}', NOW(), '{userId}', NOW(), 'SAP',  a.product_code ");
                        insertIntoBoxMappingTable.Append($" FROM  generalfg AS a ");
                        insertIntoBoxMappingTable.Append($" WHERE  a.id IN ({IDStr}); ");
                        insertIntoBoxMappingTable.Append($" UPDATE generalfg  SET sts_cd = '010' WHERE id IN ({IDStr}); ");
                        insertIntoBoxMappingTable.Append($" select box.bx_no, sum(box.gr_qty) as totalQty, min(box.bmno) as id from w_box_mapping box where box.bx_no = '{boxQR}' group by box.bx_no;  ");

                        //StringBuilder insertIntoBoxMappingTable = new StringBuilder($"CALL spfgwms_MappingBox('{boxQR}','{product.style_no}','','{IDStr}','{userId}');");
                        //db.Database.ExecuteSqlCommand(insertIntoBoxMappingTable.ToString());

                        return new InitMethods().JsonResultAndMessageFromQuery(insertIntoBoxMappingTable, "Tạo tem thùng thành công.");
                    }
                }
                catch (Exception e)
                {
                    return Json(new { result = false, message = "Lỗi hệ thống." }, JsonRequestBehavior.AllowGet);
                    throw;
                }
            }
            else
            {
                //LẤY PRODUCT Ở TỪ TEM GÓI để tạo tem thùng

                int FindPositionDZIH = buyerQRCode.IndexOf("DZIH");
              
                if (FindPositionDZIH < 1)
                {
                    return Json(new { result = false, message = "Tem gói không hợp lệ vui lòng thử lại" }, JsonRequestBehavior.AllowGet);
                }
                string ProductNew = buyerQRCode.Substring(0, FindPositionDZIH);
                var CheckProduct = _iFGWmsService.GetStyleNo(ProductNew);
                if (CheckProduct == null)
                {
                    return Json(new { result = false, message = "Product chưa được đăng kí" }, JsonRequestBehavior.AllowGet);
                }
                stampdetail = db.stamp_detail.Where(x => x.product_code == CheckProduct.style_no).FirstOrDefault();
                if (stampdetail == null)
                {
                    return Json(new { flag = false, message = "Vui lòng tạo một tem gói ở hệ thống MES để làm tiền đề tạo tem thùng" });
                }
               
                //return null;

                string stampCode = stampdetail.stamp_code;

                //LẤY THÔNG TIN ĐỂ TẠO MÃ BOX
                string productCode = stampdetail.product_code;
                string exp = product.expiry_month;
                int i;
                int j = 0;
                bool success = Int32.TryParse(exp, out i);
                if (success)
                {
                    j = i;
                }

                string vendorCode = stampdetail.vendor_code;
                string vendorLine = stampdetail.vendor_line;
                string labelPrinter = stampdetail.label_printer;
                string sampleCode = stampdetail.is_sample;
                string pcnCode = stampdetail.pcn;

                //CHỌN 1 LOT DATE BẤT KỲ TRONG LIST TEM GÓI ĐƯỢC ĐÓNG THÙNG
                //string mfgDate = new CreateBuyerQRController().DateFormatByShinsungRule(stampdetail.lot_date); cũ

                DateTime dt = DateTime.Now; //Today 
                string day_now = dt.ToString("yyyyMMdd");


                var lastedItem = listData.LastOrDefault();
                //string day_now = lastedItem != null ? lastedItem.lot_date.Replace("-", "") : string.Empty;

                string mfgDate = new InitMethods().DateFormatByShinsungRule(day_now);//lấy ngày tạo mã thùng lớn 
                string tempBoxQR = string.Concat(productCode.Replace("-", ""), vendorCode, vendorLine, labelPrinter, sampleCode, pcnCode, mfgDate);


                if (vendorCode == null)
                {
                    var gtri5 = buyerQRCode.Substring(10, 8);
                    tempBoxQR = string.Concat(productCode.Replace("-", ""), gtri5, mfgDate);
                }

                StringBuilder countSql = new StringBuilder();
                //countSql.Append($" SELECT MAX(a.bmno) AS id FROM w_box_mapping a WHERE a.bx_no IS NOT NULL AND a.bx_no LIKE '{tempBoxQR}%' GROUP BY a.bx_no;");
                countSql.Append($" SELECT SUBSTRING(max(a.bx_no), LENGTH('{tempBoxQR}')+1, 3) AS bientang FROM w_box_mapping a WHERE a.bx_no IS NOT NULL AND a.bx_no LIKE '{tempBoxQR}%' ORDER BY a.bx_no DESC LIMIT 1; ");
                List<ListIntModel> listInt = new InitMethods().ConvertDataTableToList<ListIntModel>(countSql);
                int num = 1;
                if (listInt[0].bientang != null && listInt[0].bientang != "")
                {
                    num = int.Parse(listInt[0].bientang) + 1;
                }

                foreach (var item in listData)
                {
                    totalQty += int.Parse(item.standard_qty);
                }
                string boxQR = string.Concat(tempBoxQR, new InitMethods().BuyerQRSerialFormat(num), new InitMethods().ProductQuantityFormatForBoxQR(totalQty), new InitMethods().ChangeNumberToCharacter(j).ToString());

                try
                {
                    using (Entities db = new Entities())
                    {
                        foreach (var item in listData)
                        {
                            ids.Append("'" + item.buyer_qr + "',");
                        }
                        string idStr = new InitMethods().RemoveLastComma(ids);
                        StringBuilder executeSql = new StringBuilder($" UPDATE stamp_detail SET box_code = '{boxQR}' WHERE buyer_qr IN ({idStr}) ;");
                        db.Database.ExecuteSqlCommand(executeSql.ToString());

                        StringBuilder listID = new StringBuilder();

                        if (model.Wmtids != null)
                        {
                            foreach (var item in model.Wmtids)
                            {
                                listID.Append(item + ",");

                            }
                        }

                        string IDStr = new InitMethods().RemoveLastComma(listID).Replace("'", "");
                        string userId = Session["userid"] == null ? "" : Session["userid"].ToString();

                        StringBuilder insertIntoBoxMappingTable = new StringBuilder();

                        insertIntoBoxMappingTable.Append($" INSERT INTO w_box_mapping (bx_no, buyer_cd, gr_qty, mapping_dt, sts, use_yn, del_yn, reg_id, reg_dt, chg_id, chg_dt,type,product) ");
                        insertIntoBoxMappingTable.Append($"SELECT '{boxQR}', a.buyer_qr, a.qty, CURDATE(), '000', 'Y', 'N', '{userId}', NOW(), '{userId}', NOW(), 'SAP',  '{productCode}' ");
                        insertIntoBoxMappingTable.Append($" FROM  generalfg AS a ");
                        insertIntoBoxMappingTable.Append($" WHERE  a.id IN ({IDStr}); ");
                        insertIntoBoxMappingTable.Append($" UPDATE generalfg  SET sts_cd = '010' WHERE id IN ({IDStr}); ");
                        insertIntoBoxMappingTable.Append($" select box.bx_no, sum(box.gr_qty) as totalQty, min(box.bmno) as id from w_box_mapping box where box.bx_no = '{boxQR}' group by box.bx_no;  ");

                        //StringBuilder insertIntoBoxMappingTable = new StringBuilder($"CALL spfgwms_MappingBox('{boxQR}','{product.style_no}','','{IDStr}','{userId}');");
                        //db.Database.ExecuteSqlCommand(insertIntoBoxMappingTable.ToString());

                        return new InitMethods().JsonResultAndMessageFromQuery(insertIntoBoxMappingTable, "Tạo tem thùng thành công.");
                    }
                }
                catch (Exception e)
                {
                    return Json(new { result = false, message = "Lỗi hệ thống." }, JsonRequestBehavior.AllowGet);
                    throw;
                }
            }
           // return Json(new { result = false, message = "Lỗi hệ thống." }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UnMappingBox(string boxCode)
        {
            if (string.IsNullOrEmpty(boxCode))
            {
                return Json(new { flag = false, message = "No box code selected" });
            }
            StringBuilder sql = new StringBuilder($"CALL spfgwms_UnMappingBox('{boxCode}');");
            try
            {
                db.Database.ExecuteSqlCommand(sql.ToString());
                return Json(new { flag = true, message = "Un-Mapping Thành công" });
            }
            catch (Exception)
            {
                return Json(new { flag = false, message = "Lỗi hệ thống" });
            }
        }

        [HttpPost]
        public JsonResult UnMappingBuyer(string buyerCode)
        {
            if (string.IsNullOrEmpty(buyerCode))
            {
                return Json(new { flag = false, message = "Không tìm được mã tem gói trong cơ sở dữ liệu." });
            }
            StringBuilder sql = new StringBuilder($"CALL spfgwms_UnMappingBuyer('{buyerCode}');");
            try
            {
                db.Database.ExecuteSqlCommand(sql.ToString());
                return Json(new { flag = true, message = "Xóa tem gói thành công." });
            }
            catch (Exception)
            {
                return Json(new { flag = false, message = "Lỗi hệ thống." });
            }
        }

        public JsonResult GetSumMappedBoxes(string boxCode, string ProductCode, string sDate, string BuyerCode)
        {
            StringBuilder sql = new StringBuilder($"CALL spfgwms_GetSumMappedBoxes('{boxCode}','{ProductCode}','{sDate}','{BuyerCode}');");
            return new InitMethods().JsonResultAndMessageFromQuery(sql, "");
        }

        public JsonResult GetMappedProducts(string boxCodeMapped, string buyerCode)
        {
            StringBuilder sql = new StringBuilder($"CALL spfgwms_GetMappedProducts('{boxCodeMapped}','{buyerCode}');");

            var tempData = new InitMethods().ConvertDataTable<MappedProductModel>(sql);


            var data = tempData
                            .Select(x => new MappedProductModel
                            {
                                Id = x.Id,
                                MaterialCode = x.MaterialCode,
                                ProductNo = x.ProductNo,
                                BuyerCode = x.BuyerCode,
                                Quantity = x.Quantity,
                                lot_date = x.lot_date,

                            })
                            .ToList();
            return Json(new { data, flag = true, message = "" }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetMappedProducts_old(string boxCodeMapped)
        {
            StringBuilder sql = new StringBuilder($"CALL spfgwms_GetMappedProducts('{boxCodeMapped}');");

            var tempData = new InitMethods().ConvertDataTable<MappedProductModel>(sql);

            Dictionary<string, int> checkCount = new Dictionary<string, int>();
            Dictionary<string, int> checkTotal = new Dictionary<string, int>();

            var listCount = from x in tempData
                            group x by x.BuyerCode into g
                            let count = g.Count()
                            orderby count descending
                            select new { Value = g.Key, Count = count };
            foreach (var x in listCount)
            {
                checkCount.Add(x.Value, x.Count);
            }

            var listTotal = (from x in tempData
                             group x by x.BuyerCode into g
                             select new
                             {
                                 Value = g.Key,
                                 Total = g.Sum(f => f.Quantity == null ? 0 : int.Parse(f.Quantity))
                             }).ToList();
            foreach (var x in listTotal)
            {
                checkTotal.Add(x.Value, x.Total);
            }

            var data = tempData
                            .Select(x => new MappedProductModel
                            {
                                Id = x.Id,
                                MaterialCode = x.MaterialCode,
                                MaterialNo = x.MaterialNo,
                                BuyerCode = x.BuyerCode,
                                Quantity = x.Quantity,
                                Count = checkCount.Where(a => a.Key == x.BuyerCode).Select(a => a.Value).FirstOrDefault(),
                                Total = checkTotal.Where(a => a.Key == x.BuyerCode).Select(a => a.Value).FirstOrDefault()
                            })
                            .ToList();
            return Json(new { data, flag = true, message = "" }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult exportTemToExcel()
        {

            StringBuilder varname1 = new StringBuilder();

            varname1.Append("SELECT  	SELECT	a.bx_no , a.buyer_cd ");
            varname1.Append("	FROM w_box_mapping a ");
            varname1.Append(" JOIN stamp_detail AS b 	ON a.buyer_cd = b.buyer_qr ");


            varname1.Append("	order by a.bmno DESC ");


            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);


            List<w_box_mapping> listTotal = new InitMethods().ConvertDataTable<w_box_mapping>(varname1);

            DataTable datatb = new InitMethods().ConvertListToDataTable(listTotal.ToList());


            DataSet ds2 = new DataSet();

            ds2.Tables.Add(datatb);
            ds2.Tables[0].TableName = "Export Tem";


            using (XLWorkbook wb = new XLWorkbook())
            {
                var ws = wb.AddWorksheet(datatb);

                ws.Columns().AdjustToContents();



                ws.Cells("B1").Value = "Large Package";
                ws.Cells("C1").Value = "SmallPackage";

                //ws.Cells("I1").Value = "Status";
                //ws.Cells("L1").Value = "Machine";
                ws.Columns("A").Hide(); //an cot I

                ws.Columns("E").Hide(); //an cot I
                ws.Columns("F").Hide(); //an cot I
                ws.Columns("J").Hide(); //an cot I
                ws.Columns("M").Hide(); //an cot I
                ws.Columns("N").Hide(); //an cot I



                ws.Rows().AdjustToContents();
                //ws.Name = "111";
                //Worksheets
                wb.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wb.Style.Alignment.ShrinkToFit = true;
                //wb.c

                wb.Style.Font.Bold = true;
                Response.Clear();
                Response.Buffer = true;
                Response.Charset = "";
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;filename=History.xlsx");
                using (MemoryStream MyMemoryStream = new MemoryStream())
                {
                    wb.SaveAs(MyMemoryStream);
                    MyMemoryStream.WriteTo(Response.OutputStream);
                    Response.Flush();
                    Response.End();
                }
            }
            return View("~/Views/fgwms/Mapping/MappingBox.cshtml");
        }
        #endregion FG Mapping

        #region FG_ProductLot

        public ActionResult FGProductLot()
        {
            return SetLanguage("~/Views/fgwms/Inventory/ProductLot.cshtml");
          
        }

        public JsonResult getFGProductLot(Pageing paging)
        {
            string mt_no = Request["mt_no"] == null ? "" : Request["mt_no"].Trim();
            string buyer_gr = Request["buyer_gr"] == null ? "" : Request["buyer_gr"].Trim();
            string recevice_dt_start = Request["recevice_dt_start"] == null ? "" : Request["recevice_dt_start"].Trim();
            string recevice_dt_end = Request["recevice_dt_end"] == null ? "" : Request["recevice_dt_end"].Trim();

            var dateConvert = new DateTime();

            if (DateTime.TryParse(recevice_dt_end, out dateConvert))
            {
                recevice_dt_end = dateConvert.ToString("yyyy/MM/dd");
            }
            if (DateTime.TryParse(recevice_dt_start, out dateConvert))
            {
                recevice_dt_start = dateConvert.ToString("yyyy/MM/dd");
            }

            //StringBuilder varname1 = new StringBuilder();
            //varname1.Append(" SELECT a.bmno,a.mt_no,a.mapping_dt,a.buyer_qr,a.bx_no,a.sts_nm, ");
            //varname1.Append(" CONCAT(a.gr_qty,ifnull(a.unit_cd,'')) lenght,  CONCAT(ifnull(a.width,0),'*',ifnull(a.gr_qty,0)) AS size,ifnull(a.spec,0) spec  , ");
            //varname1.Append(" (SELECT case	when a.bundle_unit = 'Roll' then	case when a.gr_qty IS NULL OR a.spec IS NULL then 0 ELSE a.gr_qty/a.spec	end else ifnull(a.gr_qty, 0) END) AS qty ");
            //varname1.Append(" FROM view_fgproduct a ");

            //varname1.Append("WHERE ('" + mt_no + "'='' OR  a.bx_no like '%" + mt_no + "%' ) ");
            //varname1.Append(" AND ('" + buyer_gr + "'='' OR  a.buyer_qr like '%" + buyer_gr + "%' ) ");

            //varname1.Append(" AND ('" + recevice_dt_start + "'='' OR DATE_FORMAT(a.mapping_dt,'%Y/%m/%d') >= DATE_FORMAT('" + recevice_dt_start + "','%Y/%m/%d')) ");
            //varname1.Append(" AND ('" + recevice_dt_end + "'='' OR DATE_FORMAT(a.mapping_dt,'%Y/%m/%d') <= DATE_FORMAT('" + recevice_dt_end + "','%Y/%m/%d')) ");

            StringBuilder varname1 = new StringBuilder();
            varname1.Append(" SELECT a.bmno,a.bx_no, a.gr_qty qty, ");
            varname1.Append(" a.sts_nm, ");
            varname1.Append(" a.buyer_qr,a.mapping_dt ");
            varname1.Append(" FROM view_fgproduct a ");
            //varname1.Append(" GROUP BY a.bx_no ");
            varname1.Append(" WHERE ('" + mt_no + "'='' OR  a.bx_no like '%" + mt_no + "%' ) ");
            varname1.Append(" AND ('" + buyer_gr + "'='' OR  a.buyer_qr like '%" + buyer_gr + "%' ) ");

            varname1.Append(" AND ('" + recevice_dt_start + "'='' OR DATE_FORMAT(a.mapping_dt,'%Y/%m/%d') >= DATE_FORMAT('" + recevice_dt_start + "','%Y/%m/%d')) ");
            varname1.Append(" AND ('" + recevice_dt_end + "'='' OR DATE_FORMAT(a.mapping_dt,'%Y/%m/%d') <= DATE_FORMAT('" + recevice_dt_end + "','%Y/%m/%d')) ");

            //varname1.Append("LEFT JOIN d_material_info  b ON a.mt_no=b.mt_no ");
            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("bmno"));
            return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
        }

        public JsonResult get_bieudo_tonghop(string value)
        {
            try
            {
                //check_tontai trong box
                var html = "";
                var box = db.w_box_mapping.Where(x => x.bx_no == value).ToList();
                if (box.Count > 0)
                {
                    html += "<ul><li><a>" + box.FirstOrDefault().bx_no + "</a><ul>";
                    foreach (var item in box)
                    {
                        html += "<li>";
                        html += "<a>" + item.mt_cd + "</a>";
                        html += "</li>";
                    }
                    html += "</ul></li></ul>";
                    return Json(new { result = false, kq = html }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { result = false, message = "Can not View " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Can not View " }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult get_bieudo_tonghop6(string value)
        {
            try
            {
                var html = "";
                var mt_lot = "";

                var box = db.w_box_mapping.Where(x => x.bx_no == value).ToList();
                if (!string.IsNullOrEmpty(value))
                {
                    var id_box = box[0].bmno;
                    html += "";//cha
                    html += "<li><span class='caret1'  >" + value + "(Box)</span>";//b1

                    html += "<ul class='nested'>";
                    foreach (var item_box in box)
                    {
                        html += "<li><span class='caret1'>" + item_box.buyer_cd + " </span>(Buyer)";//b2
                        var md_cd_buyer = db.w_material_info.Where(x => x.buyer_qr == item_box.buyer_cd).ToList();

                        html += "<ul class='nested'>";

                        //gộp các DV lại với nhau
                        var dspartTolist_OQC = new List<FG_DV>();

                        foreach (var data_ti222 in md_cd_buyer)
                        {
                            var model = new FG_DV();
                            model.id = data_ti222.wmtid;
                            model.id_actual = data_ti222.id_actual;
                            model.mt_cd = data_ti222.mt_cd;
                            dspartTolist_OQC.Add(model);
                        }
                        var list_new_OQC = KT_Lot_dv(dspartTolist_OQC);
                        List<FG_DV> myList_OQC = list_new_OQC.Cast<FG_DV>().ToList();

                        foreach (var item_cd_buyer in myList_OQC)
                        {
                            mt_lot = KT_MTLot_DV(item_cd_buyer.mt_cd);
                            html += "<li><span class='caret1'>" + item_cd_buyer.mt_cd + " </span>(OQC) <span class='detail_fg'  data-id= " + id_box + " data-mt_lot= " + mt_lot + " onclick='PpDetailFGProductLot(this);'><i class='fa fa-info'></i></span>";//b2
                                                                                                                                                                                                                                                             //hàm kiểm tra có phải DV không

                            //Start b3
                            var data_ti = (from a in db.w_material_mapping
                                           join b in db.w_material_info
                                           on a.mt_cd equals b.mt_cd
                                           where a.mt_lot == mt_lot

                                           select new
                                           {
                                               mt_cd = a.mt_cd,
                                               id = a.wmmid,
                                               id_actual = b.id_actual
                                           }).ToList();

                            //gộp các DV lại với nhau
                            var dspartTolist = new List<FG_DV>();

                            foreach (var data_ti222 in data_ti)
                            {
                                var model = new FG_DV();
                                model.id = data_ti222.id;
                                model.id_actual = data_ti222.id_actual;
                                model.mt_cd = data_ti222.mt_cd;
                                dspartTolist.Add(model);
                            }
                            var list_new = KT_Lot_dv(dspartTolist);
                            List<FG_DV> myList = list_new.Cast<FG_DV>().ToList();

                            html += "<ul class='nested'>";//b3
                            foreach (var itemdata_ti in myList)
                            {
                                html += "<li><span class='caret1'>" + itemdata_ti.mt_cd + " </span>(TIQC)<span class='detail_fg'  data-id= " + id_box + " data-mt_lot= " + itemdata_ti.mt_cd + " onclick='PpDetailFGProductLot(this);'><i class='fa fa-info'></i></span>";//b3
                                                                                                                                                                                                                                                                          //hàm kiểm tra có phải DV không
                                mt_lot = KT_MTLot_DV(itemdata_ti.mt_cd);
                                //Start b4
                                var data_ti2 = (from a in db.w_material_mapping
                                                join b in db.w_material_info
                                                on a.mt_cd equals b.mt_cd
                                                where a.mt_lot == mt_lot

                                                select new
                                                {
                                                    mt_cd = a.mt_cd,
                                                    id = a.wmmid,
                                                    id_actual = b.id_actual
                                                }).ToList();

                                var dspartTolist1 = new List<FG_DV>();
                                foreach (var data_ti222 in data_ti2)
                                {
                                    var model = new FG_DV();
                                    model.id = data_ti222.id;
                                    model.id_actual = data_ti222.id_actual;
                                    model.mt_cd = data_ti222.mt_cd;
                                    dspartTolist1.Add(model);
                                }
                                //gộp các DV lại với nhau

                                #region cmt

                                //var dsdv_new1 = new List<w_material_info>();

                                //foreach (var item_data_ti2 in data_ti2)
                                //{
                                //    var dsdv_new = new w_material_info();
                                //    if (item_data_ti2.mt_cd.Contains("-DV"))
                                //    {
                                //        var kt_co_dv = dsdv_new1.Where(x => x.mt_cd.Contains("-DV")).ToList();

                                //        if (kt_co_dv.Count > 0)
                                //        {
                                //            var mt_cd_cong_don = kt_co_dv.FirstOrDefault();
                                //            if (!mt_cd_cong_don.mt_cd.Contains(item_data_ti2.mt_cd))
                                //            {
                                //                mt_cd_cong_don.mt_cd = mt_cd_cong_don.mt_cd + "," + item_data_ti2.mt_cd;
                                //                break;
                                //            }

                                //        }
                                //        else
                                //        {
                                //            dsdv_new.mt_cd = item_data_ti2.mt_cd;
                                //            dsdv_new.id_actual = item_data_ti2.id_actual;

                                //            dsdv_new1.Add(dsdv_new);
                                //        }

                                //    }
                                //    else
                                //    {
                                //        dsdv_new.mt_cd = item_data_ti2.mt_cd;
                                //        dsdv_new.id_actual = item_data_ti2.id_actual;
                                //        dsdv_new1.Add(dsdv_new);
                                //    }

                                //}

                                #endregion cmt

                                var htht = KT_Lot_dv(dspartTolist1);

                                List<FG_DV> myList1 = htht.Cast<FG_DV>().ToList();

                                var namecongdonamms = "";
                                if (myList1.Count > 0)
                                {
                                    var id_actual_mms = data_ti2[0].id_actual;

                                    var congdoan_mms = db.w_actual.Where(x => x.id_actual == id_actual_mms).ToList();
                                    if (congdoan_mms.Count > 0)
                                    {
                                        namecongdonamms = congdoan_mms.FirstOrDefault().name;
                                    }
                                }

                                html += "<ul class='nested'>";//b4
                                foreach (var item_data_ti2 in myList1)
                                {
                                    //hàm kiểm tra có phải DV không
                                    mt_lot = KT_MTLot_DV(item_data_ti2.mt_cd);

                                    html += "<li><span class='caret1 buoc4'>" + item_data_ti2.mt_cd + "(" + namecongdonamms + ")</span><span class='detail_fg'  data-id= " + id_box + " data-mt_lot= " + item_data_ti2.mt_cd + " onclick='PpDetailFGProductLot(this);'><i class='fa fa-info'></i></span>";//b4

                                    //Start b5
                                    var data_mms1 = (from a in db.w_material_mapping
                                                     join b in db.w_material_info
                                                     on a.mt_cd equals b.mt_cd
                                                     where a.mt_lot == mt_lot

                                                     select new
                                                     {
                                                         mt_cd = a.mt_cd,
                                                         id = a.wmmid,
                                                         id_actual = b.id_actual
                                                     }).ToList();

                                    var namecongdona = "";
                                    if (data_mms1.Count > 0)
                                    {
                                        var id_actual = data_mms1[0].id_actual;

                                        var congdoan = db.w_actual.Where(x => x.id_actual == id_actual).ToList();
                                        if (congdoan.Count > 0)
                                        {
                                            namecongdona = congdoan.FirstOrDefault().name;
                                        }
                                    }

                                    html += "<ul class='nested'>";//b5
                                    foreach (var item_data_mms1 in data_mms1)
                                    {
                                        //hàm kiểm tra có phải DV không
                                        mt_lot = KT_MTLot_DV(item_data_mms1.mt_cd);
                                        //start b6
                                        var data_mms2 = (from a in db.w_material_mapping
                                                         join b in db.w_material_info
                                                         on a.mt_cd equals b.mt_cd
                                                         where a.mt_lot == mt_lot

                                                         select new
                                                         {
                                                             mt_cd = a.mt_cd,
                                                             id = a.wmmid,
                                                             id_actual = b.id_actual
                                                         }).ToList();

                                        var namecongdona1 = "";
                                        if (data_mms2.Count > 0)
                                        {
                                            var id_actual1 = data_mms2[0].id_actual;

                                            var congdoan1 = db.w_actual.Where(x => x.id_actual == id_actual1).ToList();
                                            if (congdoan1.Count > 0)
                                            {
                                                namecongdona1 = congdoan1.FirstOrDefault().name;
                                            }
                                        }

                                        if (data_mms2.Count == 0)
                                        {
                                            html += "<li><span class='caret1 nosub'>" + item_data_mms1.mt_cd + "(" + namecongdona + ")</span>";//b5
                                        }
                                        else
                                        {
                                            html += "<li><span class='caret1'>" + item_data_mms1.mt_cd + "(" + namecongdona + ")</span>";//b5
                                        }

                                        html += "<ul class='nested'>";//b6
                                        foreach (var itemdata_mms2 in data_mms2)
                                        {
                                            html += "<li><span class='caret1'>" + itemdata_mms2.mt_cd + "(" + namecongdona1 + ")</span>";//b6

                                            //hàm kiểm tra có phải DV không
                                            mt_lot = KT_MTLot_DV(itemdata_mms2.mt_cd);
                                            //Start b7
                                            var data_mms3 = (from a in db.w_material_mapping
                                                             join b in db.w_material_info
                                                             on a.mt_cd equals b.mt_cd
                                                             where a.mt_lot == mt_lot

                                                             select new
                                                             {
                                                                 mt_cd = a.mt_cd,
                                                                 id = a.wmmid,

                                                                 id_actual = b.id_actual
                                                             }).ToList();

                                            var namecongdona3 = "";
                                            if (data_mms3.Count > 0)
                                            {
                                                var id_actual3 = data_mms3[0].id_actual;

                                                var congdoan3 = db.w_actual.Where(x => x.id_actual == id_actual3).ToList();
                                                if (congdoan3.Count > 0)
                                                {
                                                    namecongdona3 = congdoan3.FirstOrDefault().name;
                                                }
                                            }

                                            html += "<ul class='nested'>";//b7
                                            foreach (var item_mms3 in data_mms3)
                                            {
                                                //hàm kiểm tra có phải DV không
                                                mt_lot = KT_MTLot_DV(item_mms3.mt_cd);
                                                var data_mms4 = (from a in db.w_material_mapping
                                                                 join b in db.w_material_info
                                                                 on a.mt_cd equals b.mt_cd
                                                                 where a.mt_lot == mt_lot

                                                                 select new
                                                                 {
                                                                     mt_cd = a.mt_cd,
                                                                     id = a.wmmid
                                                                 }).ToList();

                                                if (data_mms4.Count > 0)
                                                {
                                                    html += "<li><span class='caret1 '>" + item_mms3.mt_cd + "(" + namecongdona3 + ")</span>";//b7
                                                                                                                                              //Start b7
                                                }
                                                else
                                                {
                                                    html += "<li><span class='caret1 nosub'>" + item_mms3.mt_cd + "(" + namecongdona3 + ")</span>";//b7
                                                                                                                                                   //Start b7
                                                }

                                                html += "<ul class='nested'>";//b8
                                                foreach (var item_mms4 in data_mms4)
                                                {
                                                    html += "<li><span class=' caret1 nosub'>" + item_mms4.mt_cd + " </span></li>";//b8
                                                }
                                                html += "</li>";//end b7
                                                html += "</ul>";//end b8
                                            }

                                            html += "</li>";//end b6
                                            html += "</ul>";//end b7
                                        }
                                        html += " </li>"; //end b5
                                        html += " </ul>"; //end b6
                                    }
                                    html += " </li>"; //end b4
                                    html += " </ul>"; //end b5
                                }
                                html += " </li>"; //end b3
                                html += " </ul>"; //end b4
                            }
                            html += " </li>"; //b2
                            html += " </ul>";//b3 //end b3
                        }
                        html += " </ul>";//end b2
                        html += " </li>"; //end b1
                    }
                    html += " </ul>";//end b2
                    html += " </li>"; //end b1
                }
                return Json(new { result = true, kq = html }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Can not View " }, JsonRequestBehavior.AllowGet);
            }
        }

        public static IEnumerable KT_Lot_dv(List<FG_DV> list)
        {
            var list_new = new List<FG_DV>();
            foreach (var item in list)
            {
                var dsdv_new = new FG_DV();

                if (item.mt_cd.Contains("-DV"))
                {
                    var mt_lot = "";
                    var fff = item.mt_cd.IndexOf("-DV");
                    mt_lot = item.mt_cd.Remove(fff);

                    var kt_co_dv = list_new.Where(x => x.mt_cd.Contains(mt_lot)).ToList();

                    if (kt_co_dv.Count > 0)
                    {
                        var mt_cd_cong_don = kt_co_dv.FirstOrDefault();
                        if (!mt_cd_cong_don.mt_cd.Contains(item.mt_cd))
                        {
                            mt_cd_cong_don.mt_cd = mt_cd_cong_don.mt_cd + "," + item.mt_cd;
                        }
                    }
                    else
                    {
                        dsdv_new.mt_cd = item.mt_cd;
                        dsdv_new.id_actual = item.id_actual;

                        list_new.Add(dsdv_new);
                    }
                }
                else
                {
                    dsdv_new.mt_cd = item.mt_cd;
                    dsdv_new.id_actual = item.id_actual;
                    list_new.Add(dsdv_new);
                }
            }
            return list_new;
        }

        private string KT_MTLot_DV(string mt_lot)
        {
            var mt_lot1 = "";
            if (mt_lot.Contains("-DV"))
            {
                var fff = mt_lot.IndexOf("-DV");
                mt_lot1 = mt_lot.Remove(fff);
                return mt_lot1;
            }
            if (mt_lot.Contains("-RT"))
            {
                var fff = mt_lot.IndexOf("-RT");
                mt_lot1 = mt_lot.Remove(fff);
                return mt_lot1;
            }
            if (mt_lot.Contains("-MG"))
            {
                var fff = mt_lot.IndexOf("-MG");
                mt_lot1 = mt_lot.Remove(fff);
                return mt_lot1;
            }
            else
            {
                return mt_lot;
            }
        }

        #region PartialView_FG_bb_no

        public ActionResult PartialView_FG_bb_no(string bb_no)
        {
            ViewBag.bb_no = bb_no;
            return PartialView("~/Views/fgwms/Inventory/PartialView_FG_bb_no.cshtml");
        }

        public JsonResult GetFGProductLot_bb_no(string bb_no, string mt_lot)
        {
            try
            {
                StringBuilder sql = new StringBuilder();

                if (!string.IsNullOrEmpty(bb_no))
                {
                    var check_bb_no = db.d_bobbin_lct_hist.Where(x => x.bb_no == bb_no).ToList();
                    var dem = 0;
                    foreach (var item in check_bb_no)
                    {
                        if (dem != 0)
                        {
                            sql.Append($"union ");
                        }
                        var find_lot = db.w_material_info.Where(x => x.mt_cd == item.mt_cd).SingleOrDefault();
                        if (find_lot != null)
                        {
                            if (!string.IsNullOrEmpty(find_lot.buyer_qr) || find_lot.mt_sts_cd == "010")
                            {
                                sql.Append($"CALL spTesting2('{item.mt_cd}','','" + find_lot.buyer_qr + "');");
                            }
                            else
                            {
                                sql.Append($"CALL spTesting2('{item.mt_cd}','CP','');");
                            }

                        }
                        dem++;
                    }
                }
                else
                {
                    var find_lot = db.w_material_info.Where(x => x.mt_cd == mt_lot).SingleOrDefault();
                    if (find_lot != null)
                    {
                        if (!string.IsNullOrEmpty(find_lot.buyer_qr) || find_lot.mt_sts_cd == "010")
                        {
                            sql.Append($"CALL spTesting2('{mt_lot}','','" + find_lot.buyer_qr + "');");
                        }
                        else
                        {
                            sql.Append($"CALL spTesting2('{mt_lot}','CP','');");
                        }

                    }
                }

                return new InitMethods().JsonResultAndMessageFromQuery(sql, "");
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Data dont has exist!!!", data = 0 }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult GetFGProductLot_PO(string po)
        {
            try
            {
                var Checkdata = _iFGWmsService.CheckPOMove(po);
                if (!Checkdata)
                {
                    StringBuilder sql1 = new StringBuilder($"CALL Lot_PO_custom('{po}');");
                    return new InitMethods().JsonResultAndMessageFromQuery(sql1, "");
                }
                StringBuilder sql = new StringBuilder($"CALL Lot_PO_custom_move('{po}');");
                return new InitMethods().JsonResultAndMessageFromQuery(sql, "");
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Data dont has exist!!!", data = 0, exception = e.ToString() }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult GetFGProductLot_buyer_qr(string buyer_qr)
        {
            try
            {
                //var List_buyer = db.w_material_info.Where(x => x.buyer_qr == buyer_qr).ToList();
                //StringBuilder sql = new StringBuilder();
                //var dem = 0;
                //foreach (var item in List_buyer)
                //{
                //    if (dem != 0)
                //    {
                //        sql.Append($"union ");
                //    }
                //    var po = _iFGWmsService.FindOneMaterialInfoById(buyer_qr);
                //    var Checkdata = _iFGWmsService.CheckPOMove(po.at_no);
                //    if (!Checkdata)
                //    {
                //        sql.Append($"CALL spTesting2('{item.mt_cd}','CP','" + buyer_qr + "');");
                //    }
                //    sql.Append($"CALL spTesting2_move('{item.mt_cd}','CP','" + buyer_qr + "');");
                //    //sql.Append($"CALL spTesting2('{item.mt_cd}','CP','');");

                //    dem++;
                //}
                //return new InitMethods().JsonResultAndMessageFromQuery(sql, "");


                StringBuilder sql = new StringBuilder();

                var po = _iFGWmsService.FindOneMaterialInfoById(buyer_qr);
                var Checkdata = _iFGWmsService.CheckPOMove(po.at_no);
                var CheckProcess = _iFGWmsService.CheckProcess(po.at_no);
                //kiểm tra nếu mà tims có 2 công đoạn 
                if (!Checkdata)
                {
                    //vì nếu hơn 2 công đoạn thì oqc không hiển thị
                    if (CheckProcess> 2)
                    {
                        sql.Append($"CALL spTesting2('{po.mt_cd}','CP','" + buyer_qr + "');");
                    }
                    else
                    {
                        sql.Append($"CALL spTesting2('{po.mt_cd}','CP','" + buyer_qr + "');");
                    }
                }
                else
                {
                    sql.Append($"CALL spTesting2_move('{po.mt_cd}','CP','" + buyer_qr + "');");
                }
                var data = new InitMethods().JsonResultAndMessageFromQuery(sql, "");
                return data;
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Data dont has exist!!!", data = 0 }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetFGProductLot_box_no(string box_no)
        {
            try
            {
                var danhsach = (from a in db.w_box_mapping

                                join b in db.w_material_info on a.mt_cd equals b.mt_cd
                                select new
                                {
                                    bx_no = a.bx_no,
                                    buyer_cd = a.buyer_cd,
                                    mt_cd = b.mt_cd,
                                    gr_qty = a.gr_qty,
                                }).ToList();
                return Json(new { result = true, message = "Data dont has exist!!!", data = danhsach }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Data dont has exist!!!", data = 0 }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion PartialView_FG_bb_no

        #region Popup FG product list

        public ActionResult PartialView_FG_Info_Popup(string sts, string value)
        {
            ViewBag.sts = sts;
            ViewBag.value = value;
            return PartialView("~/Views/fgwms/Inventory/PartialView_FG_Info_Popup.cshtml");
        }
        public ActionResult PartialView_FG_Material(List<NVL_MT> NVL_MT)
        {
            var listOqc = NVL_MT.Where(x => x.process != "OQC").ToList();
       


            return PartialView("~/Views/fgwms/Inventory/PartialView_FG_Material.cshtml", listOqc);
        }
        public ActionResult PartialView_FG_PO(string value)
        {
            ViewBag.value = value;
            var ketqua = (from a in db.w_material_info
                          join b in db.w_actual on a.id_actual equals b.id_actual
                          where a.buyer_qr != null && b.at_no == value
                          select new { buyer_qr = a.buyer_qr, gr_qty = a.gr_qty }).AsEnumerable().Select(x => new
                          {
                              buyer_qr = x.buyer_qr + " (" + x.gr_qty + ")",
                              gr_qty = x.gr_qty
                          }).ToList();
            //var csv = string.Join("<br> ", ketqua.Select(x => x.buyer_qr));
            var listBuyers = ketqua.Select(x => x.buyer_qr).ToList();


            string itemTable = "";


            var totalRow = listBuyers.Count() / 5;

            for (int i = 0; i <= totalRow; i++)
            {


                var item = "";
                for (int j = i * 5; j < (i * 5 + 5 < listBuyers.Count() ? i * 5 + 5 : listBuyers.Count()); j++)
                {
                    item += $"<td>{listBuyers[j]}</td>";
                }

                itemTable += $"<tr>{item}</td>";
            }


            string tableHtml = string.Format(
                @"  <table class=custom-table-buyers>
                        <tr>
                            {0}
                        </tr>
                    </table>", itemTable)
                ;



            ViewBag.buyer = tableHtml;
            ViewBag.quantity = ketqua.Sum(x => x.gr_qty);
            var check_value = db.w_actual_primary.Where(x => x.at_no == value).ToList();
            ViewBag.product = (check_value.Count > 0 ? check_value.FirstOrDefault().product : "");

            return PartialView("~/Views/fgwms/Inventory/PartialView_FG_PO.cshtml");
        }

        public class NVL_MT
        {
            public string congnhan_time { get; set; }
            public string process { get; set; }
            public string mt_cd { get; set; }
            public string mt_no { get; set; }
            public string mt_nm { get; set; }
            public string size { get; set; }
            public string lot_no { get; set; }
            public int order_lv { get; set; }
            public string SLLD { get; set; }
            public string expiry_dt { get; set; }
            public string dt_of_receipt { get; set; }
            public string expore_dt { get; set; }

        }

        public ActionResult GetFGProductLotBuyer(int id)
        {
            try
            {
                var kttt = db.w_box_mapping.Find(id);
                if (kttt == null)
                {
                    return Json(new { result = false, message = "Data dont has exist!!!" }, JsonRequestBehavior.AllowGet);
                }
                var boxx = kttt.bx_no;
                var data = (from a in db.w_box_mapping
                            where a.bx_no.Equals(boxx)

                            select new
                            {
                                bmno = a.bmno,
                                bx_no = a.bx_no,
                                buyer_cd = a.buyer_cd,
                                gr_qty = a.gr_qty,
                                mapping_dt = a.mapping_dt,
                                sts = db.comm_dt.Where(x => x.mt_cd == "WHS013" && x.dt_cd == a.sts).Select(x => x.dt_nm),
                            }
                ).ToList();

                return Json(new { result = true, message = "Data has exist!!!", data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Data dont has exist!!!" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetFGProductLot_mt_lot(string mt_lot)
        {
            try
            {
                StringBuilder sql = new StringBuilder($"CALL spTesting2('{mt_lot}');");

                return new InitMethods().JsonResultAndMessageFromQuery(sql, "");
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Data dont has exist!!!" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Pro_GetFGProductLot_Machine(string mt_lot)
        {
            try
            {
                StringBuilder sql = new StringBuilder($"CALL Pro_GetFGProductLot_Machine('{mt_lot}');");

                return new InitMethods().JsonResultAndMessageFromQuery(sql, "");
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Data dont has exist!!!" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Pro_GetFGProductLot_staff(string mt_lot)
        {
            try
            {
                StringBuilder sql = new StringBuilder($"CALL Pro_GetFGProductLot_staff('{mt_lot}');");

                return new InitMethods().JsonResultAndMessageFromQuery(sql, "");
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Data dont has exist!!!" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetFGProductLot_mt_CD(int id)
        {
            try
            {
                var kttt = db.w_box_mapping.Find(id);
                if (kttt == null)
                {
                    return Json(new { result = false, message = "Data dont has exist!!!" }, JsonRequestBehavior.AllowGet);
                }
                var boxx = kttt.bx_no;
                var data1 = (from a in db.w_box_mapping
                             where a.bx_no.Equals(boxx)

                             select new
                             {
                                 bmno = a.bmno,
                                 mt_cd = a.mt_cd,
                             }
                ).ToList();

                var data = new List<FG_Product_lot>();
                var data_2 = new List<FG_Product_lot>();

                foreach (var item in data1)
                {
                    var list_mt_cd = (from a in db.w_material_mapping
                                      join b in db.w_material_info
                                      on a.mt_cd equals b.mt_cd

                                      join c in db.d_pro_unit_mc
                                      on b.id_actual equals c.id_actual into ps
                                      from c1 in ps.DefaultIfEmpty()

                                      join d in db.d_pro_unit_staff
                                on b.id_actual equals d.id_actual into ps1
                                      from d1 in ps1.DefaultIfEmpty()

                                      where a.mt_lot.Equals(item.mt_cd)

                                      select new
                                      {
                                          wmmid = a.wmmid,
                                          mt_lot = a.mt_lot,
                                          mt_cd = a.mt_cd,
                                          mt_no = a.mt_no,
                                          gr_qty = b.gr_qty,
                                          id_actual = b.id_actual,
                                          mc_no = c1.mc_no,
                                          start_dt = d1.start_dt,
                                          end_dt = d1.end_dt,
                                          staff_id = d1.staff_id,
                                          mt_sts_cd = b.mt_sts_cd,
                                          lct_cd = b.lct_cd,
                                      }

                            ).ToList();
                    foreach (var item1 in list_mt_cd)
                    {
                        var w_material_info_Property = new FG_Product_lot();

                        w_material_info_Property.wmmid = item1.wmmid;
                        w_material_info_Property.mt_lot = item1.mt_lot;
                        w_material_info_Property.mt_cd = item1.mt_cd;
                        w_material_info_Property.mt_no = item1.mt_no;
                        w_material_info_Property.gr_qty = item1.gr_qty;
                        w_material_info_Property.id_actual = item1.id_actual;
                        w_material_info_Property.mc_no = item1.mc_no;
                        w_material_info_Property.start_dt = item1.start_dt;
                        w_material_info_Property.end_dt = item1.end_dt;
                        w_material_info_Property.staff_id = item1.staff_id;
                        w_material_info_Property.mt_sts_cd = item1.mt_sts_cd;
                        w_material_info_Property.lct_cd = item1.lct_cd;

                        data.Add(w_material_info_Property);
                    }
                }

                return Json(new { result = true, message = "Data has exist!!!", data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Data dont has exist!!!" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetFGProductLot_mt_CD1(int id)
        {
            try
            {
                var kttt = db.w_box_mapping.Find(id);
                if (kttt == null)
                {
                    return Json(new { result = false, message = "Data dont has exist!!!" }, JsonRequestBehavior.AllowGet);
                }
                var boxx = kttt.bx_no;
                var data1 = (from a in db.w_box_mapping
                             where a.bx_no.Equals(boxx)

                             select new
                             {
                                 bmno = a.bmno,
                                 mt_cd = a.mt_cd,
                             }
                ).ToList();

                var data = new List<FG_Product_lot>();
                var data_2 = new List<FG_Product_lot>();

                foreach (var item in data1)
                {
                    var list_mt_cd = (from a in db.w_material_mapping
                                      join b in db.w_material_info
                                      on a.mt_cd equals b.mt_cd

                                      join c in db.d_pro_unit_mc
                                      on b.id_actual equals c.id_actual into ps
                                      from c1 in ps.DefaultIfEmpty()

                                      join d in db.d_pro_unit_staff
                                on b.id_actual equals d.id_actual into ps1
                                      from d1 in ps1.DefaultIfEmpty()

                                      where a.mt_lot.Equals(item.mt_cd)

                                      select new
                                      {
                                          wmmid = a.wmmid,
                                          mt_lot = a.mt_lot,
                                          mt_cd = a.mt_cd,
                                          mt_no = a.mt_no,
                                          gr_qty = b.gr_qty,
                                          id_actual = b.id_actual,
                                          mc_no = c1.mc_no,
                                          start_dt = d1.start_dt,
                                          end_dt = d1.end_dt,
                                          staff_id = d1.staff_id,
                                          mt_sts_cd = b.mt_sts_cd,
                                          lct_cd = b.lct_cd,
                                      }

                            ).ToList();
                    foreach (var item1 in list_mt_cd)
                    {
                        var w_material_info_Property = new FG_Product_lot();

                        w_material_info_Property.wmmid = item1.wmmid;
                        w_material_info_Property.mt_lot = item1.mt_lot;
                        w_material_info_Property.mt_cd = item1.mt_cd;
                        w_material_info_Property.mt_no = item1.mt_no;
                        w_material_info_Property.gr_qty = item1.gr_qty;
                        w_material_info_Property.id_actual = item1.id_actual;
                        w_material_info_Property.mc_no = item1.mc_no;
                        w_material_info_Property.start_dt = item1.start_dt;
                        w_material_info_Property.end_dt = item1.end_dt;
                        w_material_info_Property.staff_id = item1.staff_id;
                        w_material_info_Property.mt_sts_cd = item1.mt_sts_cd;
                        w_material_info_Property.lct_cd = item1.lct_cd;

                        data.Add(w_material_info_Property);

                        foreach (var itemee in data)
                        {
                            var list_mt_cd1 = (from a in db.w_material_mapping
                                               join b in db.w_material_info
                                               on a.mt_cd equals b.mt_cd

                                               join c in db.d_pro_unit_mc
                                               on b.id_actual equals c.id_actual into ps
                                               from c1 in ps.DefaultIfEmpty()

                                               join d in db.d_pro_unit_staff
                                         on b.id_actual equals d.id_actual into ps1
                                               from d1 in ps1.DefaultIfEmpty()

                                               where a.mt_lot.Equals(itemee.mt_cd)

                                               select new
                                               {
                                                   wmmid = a.wmmid,
                                                   mt_lot = a.mt_lot,
                                                   mt_cd = a.mt_cd,
                                                   mt_no = a.mt_no,
                                                   gr_qty = b.gr_qty,
                                                   id_actual = b.id_actual,
                                                   mc_no = c1.mc_no,
                                                   start_dt = d1.start_dt,
                                                   end_dt = d1.end_dt,
                                                   staff_id = d1.staff_id,
                                                   mt_sts_cd = b.mt_sts_cd,
                                                   lct_cd = b.lct_cd,
                                               }
                                        ).ToList();
                            foreach (var item123 in list_mt_cd1)
                            {
                                var w_material_info_Property1 = new FG_Product_lot();

                                w_material_info_Property1.wmmid = item123.wmmid;
                                w_material_info_Property1.mt_lot = item123.mt_lot;
                                w_material_info_Property1.mt_cd = item123.mt_cd;
                                w_material_info_Property1.mt_no = item123.mt_no;
                                w_material_info_Property1.gr_qty = item123.gr_qty;
                                w_material_info_Property1.id_actual = item123.id_actual;
                                w_material_info_Property1.mc_no = item123.mc_no;
                                w_material_info_Property1.start_dt = item123.start_dt;
                                w_material_info_Property1.end_dt = item123.end_dt;
                                w_material_info_Property1.staff_id = item123.staff_id;
                                w_material_info_Property1.mt_sts_cd = item123.mt_sts_cd;
                                w_material_info_Property1.lct_cd = item123.lct_cd;

                                data_2.Add(w_material_info_Property1);
                            }
                        }
                    }
                }
                foreach (var itemqwe in data_2)
                {
                    var w_material_info_Property = new FG_Product_lot();

                    w_material_info_Property.wmmid = itemqwe.wmmid;
                    w_material_info_Property.mt_lot = itemqwe.mt_lot;
                    w_material_info_Property.mt_cd = itemqwe.mt_cd;
                    w_material_info_Property.mt_no = itemqwe.mt_no;
                    w_material_info_Property.gr_qty = itemqwe.gr_qty;
                    w_material_info_Property.id_actual = itemqwe.id_actual;
                    w_material_info_Property.mc_no = itemqwe.mc_no;
                    w_material_info_Property.start_dt = itemqwe.start_dt;
                    w_material_info_Property.end_dt = itemqwe.end_dt;
                    w_material_info_Property.staff_id = itemqwe.staff_id;
                    w_material_info_Property.mt_sts_cd = itemqwe.mt_sts_cd;
                    w_material_info_Property.lct_cd = itemqwe.lct_cd;

                    data.Add(w_material_info_Property);
                }

                return Json(new { result = true, message = "Data has exist!!!", data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Data dont has exist!!!" }, JsonRequestBehavior.AllowGet);
            }
        }

        public class FG_DV
        {
            public string mt_cd { get; set; }

            public int id { get; set; }
            public int? id_actual { get; set; }
        }

        public class FG_Product_lot
        {
            public int wmmid { get; set; }
            public string mt_lot { get; set; }
            public string mt_cd { get; set; }
            public string mt_no { get; set; }
            public double? gr_qty { get; set; }
            public int? id_actual { get; set; }
            public string mc_no { get; set; }
            public string start_dt { get; set; }
            public string end_dt { get; set; }
            public string staff_id { get; set; }
            public string mt_sts_cd { get; set; }
            public string lct_cd { get; set; }
        }

        #endregion Popup FG product list

        private string vong_lapbd()
        {
            var html = "";
            html += "dsd";
            return html = "";
        }

        public ActionResult Partial_getTreeDash(string value)
        {
            try
            {
                //check_tontai trong box
                var html = "";
                var box = db.w_box_mapping.Where(x => x.bx_no == value).ToList();
                if (box.Count > 0)
                {
                    html += "<li><code1>" + box.FirstOrDefault().bx_no + "</code1>";
                    html += "<ul>";
                    foreach (var item in box)
                    {
                        html += "<li>";

                        html += "<code1>" + item.mt_cd + "</code1>";
                        var list_mt_cd_buyer = db.w_material_info.Where(y => y.mt_cd == item.mt_cd).Select(x => x.buyer_qr).ToList();
                        foreach (var itembuyer in list_mt_cd_buyer)
                        {
                            html += "<ul><li>";
                            html += "<code1>" + itembuyer + "</code1>";

                            var list_mt_cd = db.w_material_info.Where(y => y.buyer_qr == itembuyer).Select(x => x.mt_cd).ToList();

                            var list_qc = db.w_product_qc.Where(x => x.ml_no == item.mt_cd).Select(x => x.pq_no).ToList();
                            html += "<ul>";
                            foreach (var item_mt_cd in list_qc)
                            {
                                html += "<li><code1> " + item_mt_cd + " </code1></li> ";
                                var list_tqc = db.m_facline_qc.Where(x => x.ml_no == item.mt_cd && x.item_vcd.StartsWith("TI")).Select(x => x.fq_no).ToList();

                                html += "<ul>";
                                foreach (var item_mt_cd_qc in list_tqc)
                                {
                                    html += "<li><code1> " + item_mt_cd_qc + " </code1></li> ";
                                }

                                //html += "</ul>";
                                html += "</ul>";
                            }
                            html += "</ul>";
                            html += "</ul>";
                        }

                        html += "</li>";
                    }

                    html += "</ul></li>";
                    return PartialView("~/Views/fgwms/Inventory/Partial_getTreeDash.cshtml", html);

                    //new { result = false, message = ex.Message }                    //return PartialView(data);
                }

                return Json(new { result = false, message = "Can not View " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { result = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion FG_ProductLot

        #region FG OK=>NG(TIMS)

        public ActionResult Return()
        {
            return SetLanguage("~/Views/fgwms/Return/Return.cshtml");

        }

        public JsonResult GetDilevery_Return(Pageing paging, string dl_no, string dl_nm, string buyer)
        {
            try
            {
                var sql = new StringBuilder();
                sql.Append(" SELECT a.wmtid,a.buyer_qr,(c.at_no)po,(select product from w_actual_primary where at_no=c.at_no) product,a.mt_cd,a.gr_qty qty,(select dt_nm from comm_dt where a.mt_sts_cd=dt_cd and mt_cd ='WHS005') sts_nm FROM w_material_info AS a")
                   .Append(" JOIN w_dl_info AS b ON a.dl_no = b.dl_no ")
                   .Append(" JOIN w_actual AS c ON a.id_actual = c.id_actual ")
                   .Append(" WHERE a.lct_cd LIKE '004%' ")
                   .Append("AND ('" + dl_no + "'='' OR  b.dl_no like '%" + dl_no + "%' )")
                   .Append("AND ('" + dl_nm + "'='' OR  b.dl_nm like '%" + dl_nm + "%' )")
                   .Append("AND ('" + buyer + "'='' OR  a.buyer_qr like '%" + buyer + "%' )")
                   .Append(" order by b.dlid desc ");

                DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);
                int total = dt.Rows.Count;
                var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("wmtid"));
                return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
            }
            catch (Exception)
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetSortingOK_NG(string buyer_no)
        {
            try
            {
                //check string null khoong

                if (string.IsNullOrEmpty(buyer_no))
                {
                    return Json(new { result = false, message = "Please Scan again" }, JsonRequestBehavior.AllowGet);
                }
                //check ton tai khong
                var check_exits = db.w_material_info.Where(x => x.buyer_qr == buyer_no && (x.lct_cd.StartsWith("004") || x.lct_cd.StartsWith("003"))).ToList();
                if (check_exits.Count() == 0)
                {
                    return Json(new { result = false, message = "Buyer can not found" }, JsonRequestBehavior.AllowGet);
                }
                //scan vao bang tam thoi truoc

                var dsCommomlct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002");
                var dsCommommt_type_nm = db.comm_dt.Where(x => x.mt_cd == "COM004");
                var dsCommommt_sts_nm = db.comm_dt.Where(x => x.mt_cd == "WHS005");
                var dsLocation = db.lct_info;

                var data = (from a in check_exits
                            select new
                            {
                                wmtid = a.wmtid,
                                mt_no = a.mt_no,
                                mt_type = a.mt_type,
                                lot_no = a.lot_no,
                                expiry_dt = a.expiry_dt,
                                dt_of_receipt = a.dt_of_receipt,
                                expore_dt = a.expore_dt,
                                sd_sts_cd = a.mt_sts_cd,
                                sts_nm = dsCommommt_sts_nm.Where(x => x.dt_cd == a.mt_sts_cd).Select(x => x.dt_nm),

                                mt_cd = a.mt_cd,
                                bb_no = a.bb_no,
                                buyer_qr = a.buyer_qr,
                                gr_qty = a.gr_qty,
                                recevice_dt_tims = a.recevice_dt_tims,

                                from_lct_cd = a.from_lct_cd,
                                from_lct_nm = db.lct_info.Where(x => x.lct_cd == a.from_lct_cd).Select(x => x.lct_nm),

                                lct_cd = a.lct_cd,
                                lct_nm = db.lct_info.Where(x => x.lct_cd == a.lct_cd).Select(x => x.lct_nm),

                                to_lct_cd = a.to_lct_cd,
                                to_lct_nm = db.lct_info.Where(x => x.lct_cd == a.to_lct_cd).Select(x => x.lct_nm),

                                lct_sts_cd = dsCommomlct_sts_cd.Where(x => x.dt_cd == a.lct_sts_cd).Select(x => x.dt_nm),

                                mt_type_nm = dsCommommt_type_nm.Where(x => x.dt_cd == a.mt_type).Select(x => x.dt_nm),
                                mt_sts_cd = a.mt_sts_cd,
                            }
                      ).ToList();
                if (data.Count == 0)
                {
                    return Json(new { result = false, message = "Data has not exist!!!" }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = true, message = "Data has exist!!!", data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult change_sts_fg_tims(string buyer_code)
        {
            //lay danhsach va chuyen trang thai ve NG va tra ve kho tims
            try
            {
                var data = (from a in db.w_material_info
                            where buyer_code.Contains(a.buyer_qr) && (a.lct_cd.StartsWith("004") || a.lct_cd.StartsWith("003"))
                            select new
                            {
                                mt_cd = a.mt_cd,
                                mt_sts_cd = a.mt_sts_cd,
                                lct_cd = a.lct_cd,
                                wmtid = a.wmtid,
                            }
          ).ToList();
                foreach (var item in data)
                {
                    var find = db.w_material_info.Find(item.wmtid);
                    find.lct_cd = "006000000000000000";
                    find.mt_sts_cd = "003";
                    if (ModelState.IsValid)
                    {
                        db.Entry(find).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
                return Json(new { result = true, message = "Successfully!!!", count = data.Count() }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult PrintReturnTims()
        {
            string[] keys = Request.Form.AllKeys;

            var value = "";
            value = Request.Form[keys[0]];

            ViewData["Message"] = value;
            return View("~/Views/fgwms/Return/PrintReturnTims.cshtml");
        }

        public JsonResult qrPrintQr(string buyer_code)
        {
            try
            {
                if (buyer_code != "")
                {
                    var data = (from a in db.w_material_info
                                where buyer_code.Contains(a.buyer_qr) && (a.lct_cd.StartsWith("004") || a.lct_cd.StartsWith("003"))
                                select new
                                {
                                    mt_cd = a.mt_cd,
                                    mt_sts_cd = a.mt_sts_cd,
                                    lct_cd = a.lct_cd,
                                    wmtid = a.wmtid,
                                    buyer_qr = a.buyer_qr,
                                }
                        ).ToList();
                    return Json(new { result = true, data }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = "Not Found Buyer" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion FG OK=>NG(TIMS)

        #region PrintQR_ma_thung
        public ActionResult PrintQR(string id)
        {
            ViewData["Message"] = id;
            return View("~/Views/fgwms/Mapping/PrintQR.cshtml");

        }
        public ActionResult QRbarcodeInfo(string id)
        {

            using (Entities db = new Entities())
            {
                try
                {
                    var multiIDs = id.TrimStart('[').TrimEnd(']').Split(',');
                    var row_data = new List<BuyerQRModel>();


                    for (int i = 0; i < multiIDs.Length; i++)
                    {
                        var id2 = int.Parse(multiIDs[i]);

                        var bo_no = db.w_box_mapping.Find(id2).bx_no;

                        string sql = @"SELECT MAX(a.bmno) AS id,MAX(a.product) AS product_code, MAX(c.md_cd) AS model,MAX(c.part_nm) AS part_name, " +
                               "  MAX(b.lot_no) AS lotNo,MAX(c.expiry_month) AS expiry_month, MAX(c.expiry) AS hsd, " +
                               "  MAX(c.ssver) AS ssver, 'DZIH'  AS supplier, " +
                               "  MAX(a.bx_no) AS  bx_no, MAX(a.buyer_cd) AS buyer_cd, SUM(a.gr_qty) AS quantity, MAX(c.stamp_code) AS stamp_code ,MAX(c.drawingname) AS nhietdobaoquan , c.Description " +
                               "  FROM w_box_mapping AS a " +
                               "  LEFT JOIN generalfg AS b ON a.buyer_cd = b.buyer_qr " +
                               "  LEFT JOIN d_style_info AS c ON a.product = c.style_no " +
                               " WHERE a.bx_no = '" + bo_no + "' " +
                               " GROUP BY a.bx_no ";


                        var data = db.Database.SqlQuery<BuyerQRModel>(sql).FirstOrDefault();

                        var timDZIH = bo_no.IndexOf("DZIH") + 4; //product+DZIH
                        var vendor_line =  bo_no.Substring(timDZIH, 1) == null ? "" : "(" + bo_no.Substring(timDZIH, 1) + ")";
                        if (data == null)
                        {
                            return Json(new { result = false }, JsonRequestBehavior.AllowGet);
                        }
                        var itemBuyer = new BuyerQRModel();
                        itemBuyer.id = data.id;
                        itemBuyer.bx_no = data.bx_no;
                        itemBuyer.model = data.model;
                        itemBuyer.buyer_qr = data.buyer_qr;
                        itemBuyer.stamp_code = data.stamp_code;
                        itemBuyer.part_name = data.part_name;
                        itemBuyer.product_code = data.product_code;
                        itemBuyer.quantity = data.quantity;
                        itemBuyer.lotNo = data.lotNo == null ? "" : string.Concat(data.lotNo.Replace("-", ""));
                        //itemBuyer.nsx = data.lotNo;

                        if (!string.IsNullOrEmpty(data.lotNo))
                        {
                            var ymd = string.Concat(data.lotNo.Replace("-", ""));
                            string y = ymd.Substring(0, 4);
                            string m = ymd.Substring(4, 2);
                            string d = ymd.Substring(6, 2);

                            itemBuyer.nsx = d + "/" + m + "/" + y;
                        }
                        else
                        {
                            itemBuyer.nsx = "";
                        }

                        itemBuyer.nhietdobaoquan = data.nhietdobaoquan == null ? "" : data.nhietdobaoquan;
                        itemBuyer.ssver = data.ssver == null ? "" : data.ssver;
                        itemBuyer.Description = data.Description == null ? "" : data.Description;
                        itemBuyer.supplier = data.supplier;
                        itemBuyer.vendor_line = vendor_line;
                        string hsd = "";
                        //if (data.stamp_code == "001")
                        if (data.expiry_month == "0")
                        {
                            hsd = data.hsd;
                        }
                        else
                        {

                            string s = data.lotNo;

                            DateTime dt = DateTime.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                            if (!string.IsNullOrEmpty(data.expiry_month))
                            {
                                hsd = dt.AddMonths(int.Parse(data.expiry_month)).ToString("dd/MM/yyyy");
                            }
                        }

                        itemBuyer.hsd = hsd;
                        row_data.Add(itemBuyer);
                    }
                    return Json(new { result = true, row_data }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception e)
                {
                    return Json(new { result = false, message = e.Message }, JsonRequestBehavior.AllowGet);
                }

            }


        }
        public ActionResult ExportToExcelReceiFG(string product, string buyer, string po, string lot_date, string lot_date_end)
        {
                StringBuilder sql = new StringBuilder($" Call ExportToExcelReceiFG('{po}','{product}','{buyer}','{lot_date}','{lot_date_end}'); ");

                var data = db.Database.SqlQuery<ExportToExcelReceiFG>(sql.ToString());

                var values = data.ToList().AsEnumerable().Select(x => new
                {
                    po = x.po,
                    product = x.product,
                    product_nm = x.product_nm,
                    lot_date = x.lot_date,
                    buyer_qr = x.buyer_qr,
                    bb_no = x.bb_no,
                    gr_qty = x.gr_qty,

                }).ToArray();

                String[] labelList = new string[7] { "PO", "Product Code", "Product Name", "Lot date",  "Buyer QR" ,"Bobbin", "Quantity" };

                Response.ClearContent();

                Response.AddHeader("content-disposition", "attachment;filename=DanhSachNhanKhoFG.xls");

                Response.AddHeader("Content-Type", "application/vnd.ms-excel");

                new InitMethods().WriteHtmlTable(values, Response.Output, labelList);

                Response.End();
            

            return View("~/Views/fgwms/Receiving_Scan/Receving_Scan.cshtml");
        }
        #endregion

        #region Popup PO
        public JsonResult Getdataw_popupactual(Pageing pageing, string product, string at_no, string reg_dt)
        {
            StringBuilder varname1 = new StringBuilder();
            varname1.Append("SELECT * FROM w_actual_primary as a where  ('" + product + "'='' OR  a.product like '%" + product + "%' )");
            varname1.Append(" AND ('" + at_no + "'='' OR  a.at_no like '%" + at_no + "%' )");
            varname1.Append("AND ('" + reg_dt + "'='' OR DATE_FORMAT(a.reg_dt,'%Y/%m/%d') >= DATE_FORMAT('" + reg_dt + "','%Y/%m/%d'))");

            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("id_actualpr"));
            return new InitMethods().ReturnJsonResultWithPaging(pageing, total, result);
        }
        #endregion


        #region Sorting FG Shipping
        public ActionResult ShippingSortingFG()
        {
            return SetLanguage("~/Views/fgwms/SortingFG/ShippingFG/ShippingSortingFGScan.cshtml");
        }
        public ActionResult searchShippingSortingFG(Pageing pageing, string ShippingCode, string ProductCode, string ProductName, string Description)
        {

            Dictionary<string, string> list = PagingAndOrderBy(pageing, " ORDER BY MyDerivedTable.ExportCode DESC ");



            int totalRecords = _iFGWmsService.TotalRecordsSearchShippingSortingFG(ShippingCode, ProductCode, ProductName, Description);
            int pagesize = int.Parse(list["size"]);
            int totalPages = 0;
            try
            {
                totalPages = (int)Math.Ceiling((float)totalRecords / (float)pagesize);
            }
            catch (Exception e)
            {
                totalPages = totalRecords / pagesize;
            }

            IEnumerable<ShippingFGSortingModel> Data = _iFGWmsService.GetListSearchShippingSortingFG(ShippingCode, ProductCode, ProductName, Description);

            var result = new
            {
                total = totalPages,
                page = int.Parse(list["index"]),
                records = totalRecords,
                rows = Data
            };
            return Json(result, JsonRequestBehavior.AllowGet);

        }
        public JsonResult InsertShippingFGSorting(ShippingFGSortingModel item)
        {
            try
            {
                #region Tang tự động
                String ShippingCode = "SF1";

                var Shippinglast = _iFGWmsService.GetLastShippingFGSorting().FirstOrDefault();

                if (Shippinglast != null)
                {
                    ShippingCode = Shippinglast.ShippingCode;
                    ShippingCode = string.Concat("SF", (int.Parse(ShippingCode.Substring(2)) + 1).ToString());
                }
                #endregion
                item.ShippingCode = ShippingCode;
                item.IsFinish = "N";
                item.CreateId = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                item.CreateDate = DateTime.Now;
                item.ChangeId = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                item.ChangeDate = DateTime.Now;

                int id_ShippingFG = _iFGWmsService.InsertToShippingFGSorting(item);
                if (id_ShippingFG > 0)
                {
                    item.id = id_ShippingFG;
                    return Json(new { result = true, message = "Thành công!!!", data = item }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = "Tạo thất bại" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Tạo thất bại" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult ModifyShippingFGSorting(ShippingFGSortingModel item)
        {
            try
            {
                item.ChangeId = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                item.ChangeDate = DateTime.Now;

             _iFGWmsService.ModifyShippingFGSorting(item);

               
                    return Json(new { result = true, message = "Thành công!!!", data = item }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Sửa thất bại" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult GetExport_ScanBuyer_FG(string BuyerCode)
        {
            try
            {
                //Check input
                if (string.IsNullOrEmpty(BuyerCode))
                {
                    return Json(new { result = false, message = "Mã tem gói trống, Vui lòng Scan lại" }, JsonRequestBehavior.AllowGet);
                }
                BuyerCode = BuyerCode.Trim();
                //Kiểm tra mã materialCode có tồn tại ở kho NVL không
                var IsExistBuyerQr = _iFGWmsService.CheckIsExistBuyerCode(BuyerCode);
                //kiểm tra xem mã khác 002 là kho khác
                if (IsExistBuyerQr != null)
                {
                    if (IsExistBuyerQr.sts_cd.Equals("010"))
                    {
                        return Json(new { result = false, message = "Mã Này Đang Được Mapping với Tem Thùng" }, JsonRequestBehavior.AllowGet);
                    }
                    string trangthai = IsExistBuyerQr.sts_cd == "001" ? "Tồn kho" : "Đã giao hàng";
                    IsExistBuyerQr.sts_cd = trangthai;
                    return Json(new { result = true, message = "Thành công", data = IsExistBuyerQr }, JsonRequestBehavior.AllowGet);
                }
                {
                    return Json(new { result = false, message = "Mã bạn vừa quét là: " + BuyerCode }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Sửa thất bại" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult UpdateShippingSortingFG(string ShippingCode, string ListId)
        {
            try
            {
                //Check input
                if (string.IsNullOrEmpty(ShippingCode))
                {
                    return Json(new { result = false, message = "Vui lòng Chọn một phiếu xuất" }, JsonRequestBehavior.AllowGet);
                }
                if (string.IsNullOrEmpty(ListId))
                {
                    return Json(new { result = false, message = "Mã Nguyên vật liệu trống, Vui lòng Scan lại" }, JsonRequestBehavior.AllowGet);
                }
                ListId = ListId.Trim();


                //update trạng thái là storting cho tem gói

                var generalfg = new generalfg();
                generalfg.sts_cd = "015";

                _iFGWmsService.UpdateShippingSortingFG(generalfg, ListId);

                //insert phiếu xuất và tem gói vào bảng shippingfgsortingdetail
                var UserID = Session["userid"] == null ? null : Session["userid"].ToString();
                _iFGWmsService.InsertShippingSortingFGDetail(ShippingCode,ListId, UserID);


                return Json(new { result = true, message = "Thành công" }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống" }, JsonRequestBehavior.AllowGet);
            }
        }
        public string checktrangthai(string mt_sts_cd)
        {
            var checktrangthai = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == mt_sts_cd).ToList();
            var csv = string.Join(", ", checktrangthai.Select(x => x.dt_nm));
            return csv;
        }
        #region PartialViewShippingFGSortingPP
        public ActionResult PartialViewShippingFGSortingPP(string ShippingCode)
        {
            ViewBag.ShippingCode = ShippingCode;
            return PartialView("~/Views/fgwms/SortingFG/ShippingFG/PartialViewShippingFGSortingPP.cshtml");
        }
        public ActionResult GetShippingSortingFGPP(string ShippingCode)
        {
            var listdata = _iFGWmsService.GetListSearchShippingSortingFGPP(ShippingCode);

            return Json(new { data = listdata }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetListShippingFGSorting(string ShippingCode)
        {
            var listdata = _iFGWmsService.GetListShippingFGSorting(ShippingCode);

            return Json(new { data = listdata }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetShippingScanPP_Countbuyer(string ShippingCode)
        {
            try
            {
                StringBuilder sql = new StringBuilder($"CALL GetShippingScanPP_Countbuyer('{ShippingCode}');");
                var list2 = new InitMethods().ConvertDataTableToJsonAndReturn(sql);
                return Json(new { data = list2.Data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }

        }
        #endregion
        #endregion
        #region Sorting FG Receiving
        public ActionResult RecevingSortingFG()
        {
            return SetLanguage("~/Views/fgwms/SortingFG/RecevingFG/RecevingSortingFG.cshtml");
        }
        public ActionResult ScanReceivingFGBuyer(string buyer_qr, string ShippingCode)
        {
            try
            {
                buyer_qr = buyer_qr.Trim();
                //Check input
                if (string.IsNullOrEmpty(ShippingCode))
                {
                    return Json(new { result = false, message = "Vui lòng chọn một phiếu ST!!!" }, JsonRequestBehavior.AllowGet);
                }
                if (string.IsNullOrEmpty(buyer_qr))
                {
                    return Json(new { result = false, message = "Vui lòng scan một tem gói" }, JsonRequestBehavior.AllowGet);
                }



                //KIỂM TRA XEM SD CÓ TỒN TẠI KHÔNG.
                if (_iFGWmsService.CheckSTinfo(ShippingCode))
                {
                    return Json(new { result = false, message = "ST này không tồn tại" }, JsonRequestBehavior.AllowGet);
                }

                //Step1: kiểm tra tem gói có thuộc phiếu xuất đó ko

                var isCheckExistSF = _iFGWmsService.isCheckExistSF(ShippingCode, buyer_qr);
                if (isCheckExistSF == null)
                {
                    return Json(new { result = false, message = "Không có tem gói được trả về" }, JsonRequestBehavior.AllowGet);
                }
                //Step2: kiểm tra tem gói này đã tồn kho chưa
                var isCheckExistBuyerQRST = _iFGWmsService.isCheckExistGenneral(buyer_qr);
                if (isCheckExistBuyerQRST == null)
                {
                    return Json(new { result = false, message = "Tem gói không tồn tại" }, JsonRequestBehavior.AllowGet);
                }
                if (isCheckExistBuyerQRST.sts_cd != "015")
                {
                    var status = _iFGWmsService.CheckStatus(isCheckExistBuyerQRST.sts_cd);
                    return Json(new { result = false, message = "Tem gói này đang ở Trạng thái:" + status }, JsonRequestBehavior.AllowGet);
                }
                // //Step3: update location shippingtimssortingdetail 
                isCheckExistSF.ChangeId = Session["userid"] == null ? null : Session["userid"].ToString();
                isCheckExistSF.location = "003G01000000000000";
                _iFGWmsService.UpdateLocationtimSorting(isCheckExistSF);

                //Step3: update w_material_info sts= 001 and lct = 003G01000000000000
                var isCheckExistWmaterialBuyer = _iFGWmsService.isCheckExistWmaterialBuyer(buyer_qr);
                if (isCheckExistSF == null)
                {
                    return Json(new { result = false, message = "Không có tem gói được trả về" }, JsonRequestBehavior.AllowGet);
                }
                isCheckExistWmaterialBuyer.lct_cd = "003G01000000000000";
                isCheckExistWmaterialBuyer.mt_sts_cd = "001";
                _iFGWmsService.UpdateWMaterialInfoStatus(isCheckExistWmaterialBuyer);

                //Step4: update generalfg  tem gói sts_cd = 001
                isCheckExistBuyerQRST.sts_cd = "001";
                isCheckExistBuyerQRST.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                _iFGWmsService.UpdateBuyerGeneral(isCheckExistBuyerQRST);


                return (Json(new { result = true, data = "", message = "Thành công" }, JsonRequestBehavior.AllowGet));

            }
            catch (Exception e)
            {
                return Json(new { result = false, message = e }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion
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
    }

    public class ProductLotListModel
    {
        public int total_page { get; set; }
    }
}
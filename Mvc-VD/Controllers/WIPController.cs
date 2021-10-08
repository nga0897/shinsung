﻿using ClosedXML.Excel;
using Mvc_VD.Models;
using Mvc_VD.Models.WIP;
using Mvc_VD.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Mvc_VD.Controllers
{
    public class WIPController : BaseController
    {
        private readonly IWIPService _IWIPService;
        private readonly Entities db;

        // private Entities db = new Entities();
        string exs = "Lỗi hệ thống!!!";
        string ss = "Thành công!!!";

        public WIPController(
            IWIPService IWIPService,
             IDbFactory DbFactory)
        {

            _IWIPService = IWIPService;
            db = DbFactory.Init();
        }
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
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var lang = HttpContext.Request.Cookies.AllKeys.Contains("language")
                  ? HttpContext.Request.Cookies["language"].Value
                  : "en";

       
            var router = this.ControllerContext.RouteData.Values.Values.ElementAt(0).ToString();
            var result = _IWIPService.GetLanguage(lang, router);
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
        #region General

        #region tab1_Material
        public JsonResult getGeneral_Material(Pageing pageing)
        {
            string mt_no = Request["mt_no"] == null ? "" : Request["mt_no"].Trim();
            string mt_cd = Request["mt_cd"] == null ? "" : Request["mt_cd"].Trim();
            string mt_nm = Request["mt_nm"] == null ? "" : Request["mt_nm"].Trim();
            string product_cd = Request["product_cd"] == null ? "" : Request["product_cd"].Trim();
            string lct_cd = Request["lct_cd"] == null ? "" : Request["lct_cd"].Trim();
            string recevice_dt_start = Request["recevice_dt_start"] == null ? "" : Request["recevice_dt_start"].Trim();
            string recevice_dt_end = Request["recevice_dt_end"] == null ? "" : Request["recevice_dt_end"].Trim();
            string sts = Request["sts"] == null ? "" : Request["sts"].Trim();
            var dateConvert = new DateTime();

            if (DateTime.TryParse(recevice_dt_end, out dateConvert))
            {
                recevice_dt_end = dateConvert.ToString("yyyy/MM/dd");
            }
            if (DateTime.TryParse(recevice_dt_start, out dateConvert))
            {
                recevice_dt_start = dateConvert.ToString("yyyy/MM/dd");
            }
            if (!string.IsNullOrEmpty(lct_cd))
            {
                var isExistLocation = _IWIPService.CheckIsExistLocation(lct_cd);
                if (isExistLocation == null)
                {
                    return Json(new { result = false, message = "Kệ này không tồn tại" }, JsonRequestBehavior.AllowGet);
                }
                //nếu level = 000 thì search like 002 
                if (isExistLocation.level_cd.Equals("000"))
                {
                    lct_cd = lct_cd.Substring(0, 6);
                }
                //kiểm tra location có rỗng ko và nếu level = 002 thì search bằng chính nó, ngược lại search like
                if (!string.IsNullOrEmpty(lct_cd) && isExistLocation.level_cd.Equals("001"))
                {
                    lct_cd = lct_cd.Substring(0, 9);
                }
            }
          
            Dictionary<string, string> list = PagingAndOrderBy(pageing, " ORDER BY MyDerivedTable.mt_no DESC ");
             
            
            //int totalRecords = _IWIPService.TotalRecordsSearchGeneralMaterialWIP(mt_no, product_cd, mt_nm, recevice_dt_start, recevice_dt_end, sts, lct_cd, mt_cd);
           
            IEnumerable<GeneralWIP> Data = _IWIPService.GetListGeneralMaterialWIP(mt_no, product_cd, mt_nm, recevice_dt_start, recevice_dt_end,sts, lct_cd, mt_cd);
            int totalRecords = Data.Count();
                 int totalPages = (int)Math.Ceiling((float)totalRecords / (float)int.Parse(list["size"]));
            var result = new
            {
                total = totalPages,
                page = int.Parse(list["index"]),
                records = totalRecords,
                rows = Data
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
        public JsonResult GetgeneralDetail_List()
        {
            string mt_no = Request["mt_no"] == null ? "" : Request["mt_no"].Trim();
            string mt_nm = Request["mt_nm"] == null ? "" : Request["mt_nm"].Trim();
            string recevice_dt_start = Request["recevice_dt_start"] == null ? "" : Request["recevice_dt_start"].Trim();
            string recevice_dt_end = Request["recevice_dt_end"] == null ? "" : Request["recevice_dt_end"].Trim();
            string sts = Request["sts"] == null ? "" : Request["sts"].Trim();
            string product_cd = Request["product_cd"] == null ? "" : Request["product_cd"].Trim();
            string lct_cd = Request["lct_cd"] == null ? "" : Request["lct_cd"].Trim();
            string mt_cd = Request["mt_cd"] == null ? "" : Request["mt_cd"].Trim();
            var dateConvert = new DateTime();

            if (DateTime.TryParse(recevice_dt_end, out dateConvert))
            {
                recevice_dt_end = dateConvert.ToString("yyyy/MM/dd");
            }
            if (DateTime.TryParse(recevice_dt_start, out dateConvert))
            {
                recevice_dt_start = dateConvert.ToString("yyyy/MM/dd");
            }

            if (!string.IsNullOrEmpty(lct_cd))
            {
                var isExistLocation = _IWIPService.CheckIsExistLocation(lct_cd);
                if (isExistLocation == null)
                {
                    return Json(new { result = false, message = "Kệ này không tồn tại" }, JsonRequestBehavior.AllowGet);
                }
                //nếu level = 000 thì search like 002 
                if (isExistLocation.level_cd.Equals("000"))
                {
                    lct_cd = lct_cd.Substring(0, 6);
                }
                //kiểm tra location có rỗng ko và nếu level = 002 thì search bằng chính nó, ngược lại search like
                if (!string.IsNullOrEmpty(lct_cd) && isExistLocation.level_cd.Equals("001"))
                {
                    lct_cd = lct_cd.Substring(0, 9);
                }
            }



            StringBuilder varname1 = new StringBuilder();
            varname1.Append("SELECT * FROM ( SELECT a.wmtid,a.mt_cd,b.mt_nm, lct.lct_nm, CONCAT(ifnull(a.gr_qty,''),' ',ifnull(b.unit_cd,'')) lenght,CONCAT(ifnull(b.width,0),'*',ifnull(a.gr_qty,0)) AS size,ifnull(b.spec,0) spec,a.mt_no, ");
            varname1.Append(" (case when b.bundle_unit ='Roll' then  ROUND((a.gr_qty/b.spec),2) ELSE ROUND(a.gr_qty,2) END) qty, b.bundle_unit, ");
            //varname1.Append("a.recevice_dt, a.sd_no,");
            varname1.Append(" a.sd_no,");
            varname1.Append("com.dt_nm sts_nm, ");
            varname1.Append("a.rece_wip_dt recevice_dt, ");
            varname1.Append("(CASE WHEN('08:00:00' <= DATE_FORMAT(CAST(a.rece_wip_dt AS datetime), '%H:%i:%s') AND  DATE_FORMAT(CAST(a.rece_wip_dt AS datetime), '%H:%i:%s') < '23:59:59') THEN DATE_FORMAT(CAST(a.rece_wip_dt AS DATETIME), '%Y-%m-%d')    when(DATE_FORMAT(CAST(a.rece_wip_dt AS datetime), '%H:%i:%s') < '08:00:00') THEN  DATE_FORMAT(CAST(a.rece_wip_dt AS DATETIME) - interval 1 DAY, '%Y-%m-%d') ELSE ''  END) as recevice_dt1 ");
            varname1.Append("FROM w_material_info a ");
            varname1.Append("LEFT JOIN d_material_info  b ON a.mt_no=b.mt_no ");
            varname1.Append(" LEFT JOIN  w_sd_info info ON info.sd_no = a.sd_no  ");
            varname1.Append("LEFT JOIN lct_info AS lct ON a.lct_cd = lct.lct_cd  ");
            varname1.Append(" LEFT JOIN comm_dt as com ON a.mt_sts_cd  = com.dt_cd AND com.mt_cd='WHS005'  ");
            varname1.Append("WHERE a.lct_cd LIKE '002%' AND (a.ExportCode IS NULL OR a.ExportCode = '')  AND a.mt_no='" + mt_no + "' and a.mt_type<> 'CMT' AND a.mt_sts_cd!='005'   ");
            varname1.Append(" AND ('" + mt_nm + "'='' OR  b.mt_nm like '%" + mt_nm + "%' ) ");
            varname1.Append(" AND ('" + lct_cd + "'='' OR  a.lct_cd like '%" + lct_cd + "%' ) ");
            varname1.Append(" AND ('" + mt_cd + "'='' OR  a.mt_cd like '%" + mt_cd + "%' ) ");
            varname1.Append(" AND ('" + mt_nm + "'='' OR  b.mt_nm like '%" + mt_nm + "%' ) ");
            varname1.Append(" AND ('" + product_cd + "'='' OR  info.product_cd like '%" + product_cd + "%' ) ");
            varname1.Append(" AND ('" + sts + "'='' OR  a.mt_sts_cd in (" + sts + ") ) )AS TABLE1 ");
            varname1.Append(" WHERE ('" + recevice_dt_start + "'='' OR DATE_FORMAT(TABLE1.recevice_dt1,'%Y/%m/%d') >= DATE_FORMAT('" + recevice_dt_start + "','%Y/%m/%d')) ");
            varname1.Append(" AND ('" + recevice_dt_end + "'='' OR DATE_FORMAT(TABLE1.recevice_dt1,'%Y/%m/%d') <= DATE_FORMAT('" + recevice_dt_end + "','%Y/%m/%d')) order by TABLE1.sd_no asc");

            var data = new InitMethods().ConvertDataTableToJsonAndReturn(varname1);
            return Json(data.Data, JsonRequestBehavior.AllowGet);
        }

        //public JsonResult GetgeneralDetail_List(Pageing paging)
        //{
        //    string mt_no = Request["mt_no"] == null ? "" : Request["mt_no"].Trim();
        //    string mt_nm = Request["mt_nm"] == null ? "" : Request["mt_nm"].Trim();
        //    string recevice_dt_start = Request["recevice_dt_start"] == null ? "" : Request["recevice_dt_start"].Trim();
        //    string recevice_dt_end = Request["recevice_dt_end"] == null ? "" : Request["recevice_dt_end"].Trim();
        //    string sts = Request["sts"] == null ? "" : Request["sts"].Trim();
        //    var dateConvert = new DateTime();

        //    if (DateTime.TryParse(recevice_dt_end, out dateConvert))
        //    {
        //        recevice_dt_end = dateConvert.ToString("yyyy/MM/dd");
        //    }
        //    if (DateTime.TryParse(recevice_dt_start, out dateConvert))
        //    {
        //        recevice_dt_start = dateConvert.ToString("yyyy/MM/dd");
        //    }
        //    StringBuilder varname1 = new StringBuilder();
        //    varname1.Append("SELECT a.wmtid,a.mt_cd,b.mt_nm,CONCAT(ifnull(a.gr_qty,''),ifnull(b.unit_cd,'')) lenght,CONCAT(ifnull(b.width,0),'*',ifnull(a.gr_qty,0)) AS size,ifnull(b.spec,0) spec,a.mt_no, ");
        //    varname1.Append(" (case when b.bundle_unit ='Roll' then  ROUND((a.gr_qty/b.spec),2) ELSE ROUND(a.gr_qty,2) END) qty, b.bundle_unit, ");
        //    varname1.Append("a.recevice_dt, ");
        //    varname1.Append("(SELECT dt_nm FROM comm_dt WHERE comm_dt.dt_cd=a.mt_sts_cd AND comm_dt.mt_cd='WHS005') sts_nm ");
        //    varname1.Append("FROM w_material_info a ");
        //    varname1.Append("LEFT JOIN d_material_info  b ON a.mt_no=b.mt_no ");
        //    varname1.Append("WHERE a.lct_cd LIKE '002%' AND a.mt_no='" + mt_no + "' and a.mt_type<> 'CMT' AND a.mt_sts_cd!='005'  ");
        //    varname1.Append(" AND ('" + mt_nm + "'='' OR  b.mt_nm like '%" + mt_nm + "%' ) ");
        //    varname1.Append(" AND ('" + sts + "'='' OR  a.mt_sts_cd in (" + sts + ") ) ");
        //    varname1.Append(" AND ('" + recevice_dt_start + "'='' OR DATE_FORMAT(a.recevice_dt,'%Y/%m/%d') >= DATE_FORMAT('" + recevice_dt_start + "','%Y/%m/%d')) ");
        //    varname1.Append(" AND ('" + recevice_dt_end + "'='' OR DATE_FORMAT(a.recevice_dt,'%Y/%m/%d') <= DATE_FORMAT('" + recevice_dt_end + "','%Y/%m/%d')) ");

        //    DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

        //    int total = dt.Rows.Count;
        //    var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("wmtid"));
        //    return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
        //}
        public ActionResult Search_mdCodeGeneral(string mt_cd)
        {
            try
            {
                if (string.IsNullOrEmpty(mt_cd))
                {

                    return Json(new { result = false, message = "Vui lòng SCan lại" }, JsonRequestBehavior.AllowGet);
                }
                //var kttt = db.w_material_info.Where(x => x.mt_cd == mt_cd).ToList();
                var kttt = _IWIPService.CheckWMaterialInfo(mt_cd);
                if (kttt == null)
                {
                    return Json(new { result = false, message = "Không tìm thấy mã MATERIAL CODE" }, JsonRequestBehavior.AllowGet);
                }
                //var kt_wip = kttt.Where(x => x.mt_cd == mt_cd && x.lct_cd.Equals("002000000000000000")).ToList();

                 var kt_wip = _IWIPService.CheckIsExistMaterial(mt_cd);
                if (kt_wip == null)//ko co trong phong sản xuât
                {
                   
                    //kiểm tra xem nó đang ở kho tims ko
                    if (kttt.lct_cd.StartsWith("006"))
                    {
                      
                        var lctName1 = db.lct_info.Where(x => x.lct_cd == kttt.lct_cd).FirstOrDefault().lct_nm;
                        var StatusName1 = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == kttt.mt_sts_cd).FirstOrDefault().dt_nm;

                        return Json(new { result = false, message = "Mã này đang ở trong kho" + lctName1 + "Trạng thái là:" + StatusName1 }, JsonRequestBehavior.AllowGet);
                    }
                  
                    var StatusName = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == kttt.mt_sts_cd).FirstOrDefault().dt_nm; ;

                    return Json(new { result = false, message = "Mã này đang ở trong máy" + kttt.LoctionMachine + "Trạng thái là:" + StatusName }, JsonRequestBehavior.AllowGet);
                }
               // var data = kt_wip.Where(x => x.mt_cd == mt_cd).FirstOrDefault();
                return Json(new { result = true, data = kt_wip }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {

                return Json(new { result = false, message = "Không tồn tại" }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion tab1_Material


        public class WIP_ParentInventoryModel
        {
            public string mt_no { get; set; }
            public string mt_nm { get; set; }

            public string mt_cd { get; set; }

            public string qty { get; set; }
            public string DSD { get; set; }
            public string CSD { get; set; }
            public string TK { get; set; }
            public string lenght { get; set; }
            public string size { get; set; }
            public string recevice_dt { get; set; }
            public string sts_nm { get; set; }
            public string lct_nm { get; set; }
            public string sd_no { get; set; }

        }
        public ActionResult PrintExcelFile_v1(string mtCode, string mtNo, string mtName, string recDate)
        {
            string mt_no = Request["mt_no"] == null ? "" : Request["mt_no"].Trim();
            string lct_cd = Request["lct_cd"] == null ? "" : Request["lct_cd"].Trim();
            string mt_nm = Request["mt_nm"] == null ? "" : Request["mt_nm"].Trim();
            string recevice_dt_start = Request["recevice_dt_start"] == null ? "" : Request["recevice_dt_start"].Trim();
            string recevice_dt_end = Request["recevice_dt_end"] == null ? "" : Request["recevice_dt_end"].Trim();
            string sts = Request["sts"] == null ? "" : Request["sts"].Trim();
            string sts_search = "";
            switch (sts)
            {
                case "002":
                    sts_search = " AND DSD >0";
                    break;

                case "001,004":
                    sts_search = "AND CSD >0";
                    break;
            }
            var dateConvert = new DateTime();

            if (DateTime.TryParse(recevice_dt_end, out dateConvert))
            {
                recevice_dt_end = dateConvert.ToString("yyyy/MM/dd");
            }
            if (DateTime.TryParse(recevice_dt_start, out dateConvert))
            {
                recevice_dt_start = dateConvert.ToString("yyyy/MM/dd");
            }
            StringBuilder varname1 = new StringBuilder();
            varname1.Append("SELECT ''mt_cd,a.mt_no,a.mt_nm,a.qty,concat(a.DSD,'m')DSD,concat(a.CSD,'m')CSD,(a.DSD + a.CSD)TK, \n");
            varname1.Append(" ''lenght,''size,''recevice_dt,''sts_nm, '' lct_nm , '' sd_no \n");
            varname1.Append("FROM getgeneral_material AS a \n");

            varname1.Append(" WHERE ('" + mt_no + "'='' OR a.mt_no like '%" + mt_no + "%' ) ");
            varname1.Append(" AND ('" + mt_nm + "'='' OR  a.mt_nm like '%" + mt_nm + "%' ) ");
            varname1.Append("" + sts_search + " ");
            varname1.Append(" AND ('" + recevice_dt_start + "'='' OR DATE_FORMAT(a.recevice_dt,'%Y/%m/%d') >= DATE_FORMAT('" + recevice_dt_start + "','%Y/%m/%d')) ");
            varname1.Append(" AND ('" + recevice_dt_end + "'='' OR DATE_FORMAT(a.recevice_dt,'%Y/%m/%d') <= DATE_FORMAT('" + recevice_dt_end + "','%Y/%m/%d')) ");

            varname1.Append("UNION \n");
            varname1.Append(" \n");
            varname1.Append("SELECT a.mt_cd,a.mt_no,''mt_nm,CONCAT((CASE WHEN b.bundle_unit ='Roll' THEN ROUND((a.gr_qty/b.spec),2) ELSE ROUND(a.gr_qty,2) END),b.bundle_unit)qty, \n");
            varname1.Append("'' DSD,''CSD,''TK, \n");
            varname1.Append("CONCAT(IFNULL(a.gr_qty,''), IFNULL(b.unit_cd,'')) lenght, CONCAT(IFNULL(b.width,0),'*', IFNULL(a.gr_qty,0)) AS size,a.recevice_dt, ( \n");
            varname1.Append("SELECT dt_nm \n");
            varname1.Append("FROM comm_dt \n");
            varname1.Append("WHERE comm_dt.dt_cd=a.mt_sts_cd AND comm_dt.mt_cd='WHS005') sts_nm  , location.lct_nm, a.sd_no  \n");
            varname1.Append("FROM w_material_info a \n");
            varname1.Append("LEFT JOIN d_material_info b ON a.mt_no=b.mt_no \n");
            varname1.Append(" LEFT JOIN lct_info location ON a.lct_cd=location.lct_cd  \n");
            varname1.Append("WHERE a.lct_cd LIKE '002%'  and a.mt_type<> 'CMT' AND a.mt_sts_cd!='005'  ");
            varname1.Append("  AND (a.ExportCode IS NULL OR a.ExportCode = '') ");
            varname1.Append(" AND ('" + mt_no + "'='' OR  a.mt_no like '%" + mt_no + "%' ) ");
            varname1.Append(" AND ('" + lct_cd + "'='' OR  a.lct_cd like '%" + lct_cd + "%' ) ");
            varname1.Append(" AND ('" + mt_nm + "'='' OR  b.mt_nm like '%" + mt_nm + "%' ) ");
            varname1.Append(" AND ('" + sts + "'='' OR  a.mt_sts_cd in (" + sts + ") ) ");
            varname1.Append(" AND ('" + recevice_dt_start + "'='' OR DATE_FORMAT(a.recevice_dt,'%Y/%m/%d') >= DATE_FORMAT('" + recevice_dt_start + "','%Y/%m/%d')) ");
            varname1.Append(" AND ('" + recevice_dt_end + "'='' OR DATE_FORMAT(a.recevice_dt,'%Y/%m/%d') <= DATE_FORMAT('" + recevice_dt_end + "','%Y/%m/%d')) ");

            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

            List<WIP_ParentInventoryModel> listTotal = new InitMethods().ConvertDataTable<WIP_ParentInventoryModel>(varname1);

            listTotal = listTotal.OrderBy(x => x.mt_no).ThenBy(x => x.mt_cd).ToList();

            DataTable datatb = new InitMethods().ConvertListToDataTable(listTotal);

            DataSet ds2 = new DataSet();

            ds2.Tables.Add(datatb);
            ds2.Tables[0].TableName = "WIP_General";

            using (XLWorkbook wb = new XLWorkbook())
            {
                var ws = wb.AddWorksheet(datatb);

                ws.Columns().AdjustToContents();
                ws.Cells("A1").Value = "MT Code";
                ws.Cells("B1").Value = "MT Name";
                ws.Cells("C1").Value = "Composite Code";

                ws.Cells("E1").Value = "QTy (Roll/EA)";
                ws.Cells("F1").Value = "Đang sử dụng Qty Length(M)";
                ws.Cells("G1").Value = "Chưa sử dụng Qty Length(M)";
                ws.Cells("H1").Value = "Tồn Kho Qty Length(M)";
                ws.Cells("I1").Value = "Lenght";
                ws.Cells("J1").Value = "Size";
                ws.Cells("K1").Value = "Status";
                ws.Cells("L1").Value = "Location";
                ws.Cells("M1").Value = "SD No";
                //ws.Columns("K").Hide(); //an cot I

                ws.Rows().AdjustToContents();
                wb.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wb.Style.Alignment.ShrinkToFit = true;

                wb.Style.Font.Bold = true;
                Response.Clear();
                Response.Buffer = true;
                Response.Charset = "";
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;filename=WIP_General.xlsx");
                using (MemoryStream MyMemoryStream = new MemoryStream())
                {
                    wb.SaveAs(MyMemoryStream);
                    MyMemoryStream.WriteTo(Response.OutputStream);
                    Response.Flush();
                    Response.End();
                }
            }
            return View("~/Views/WIP/Inventory/General.cshtml");
        }
        public ActionResult PrintExcelFile()
        {
            string product_cd = Request["product_cd"] == null ? "" : Request["product_cd"].Trim();
            string mt_no = Request["mt_no"] == null ? "" : Request["mt_no"].Trim();
            string mt_cd = Request["mt_cd"] == null ? "" : Request["mt_cd"].Trim();
            string lct_cd = Request["lct_cd"] == null ? "" : Request["lct_cd"].Trim();
            string mt_nm = Request["mt_nm"] == null ? "" : Request["mt_nm"].Trim();
            string recevice_dt_start = Request["recevice_dt_start"] == null ? "" : Request["recevice_dt_start"].Trim();
            string recevice_dt_end = Request["recevice_dt_end"] == null ? "" : Request["recevice_dt_end"].Trim();
            string sts = Request["sts"] == null ? "" : Request["sts"].Trim();
           
            var dateConvert = new DateTime();

            if (DateTime.TryParse(recevice_dt_end, out dateConvert))
            {
                recevice_dt_end = dateConvert.ToString("yyyy/MM/dd");
            }
            if (DateTime.TryParse(recevice_dt_start, out dateConvert))
            {
                recevice_dt_start = dateConvert.ToString("yyyy/MM/dd");
            }

            if (!string.IsNullOrEmpty(lct_cd))
            {
                var isExistLocation = _IWIPService.CheckIsExistLocation(lct_cd);
                if (isExistLocation == null)
                {
                    return Json(new { result = false, message = "Kệ này không tồn tại" }, JsonRequestBehavior.AllowGet);
                }
                //nếu level = 000 thì search like 002 
                if (isExistLocation.level_cd.Equals("000"))
                {
                    lct_cd = lct_cd.Substring(0, 6);
                }
                //kiểm tra location có rỗng ko và nếu level = 002 thì search bằng chính nó, ngược lại search like
                if (!string.IsNullOrEmpty(lct_cd) && isExistLocation.level_cd.Equals("001"))
                {
                    lct_cd = lct_cd.Substring(0, 9);
                }
            }


            StringBuilder varname1 = new StringBuilder();
            varname1.Append("SELECT TABLE1.mt_cd, TABLE1.mt_no, TABLE1.mt_nm,   \n");
            varname1.Append("CONCAT((CASE WHEN (MAX(TABLE1.bundle_unit) = 'Roll')  THEN ROUND((SUM(TABLE1.gr_qty) / MAX(TABLE1.spec)),2)     \n");
            varname1.Append("ELSE ROUND(SUM(TABLE1.gr_qty),2) END),' ', MAX(TABLE1.bundle_unit)) AS qty ,    \n");
            varname1.Append(" SUM( CASE  WHEN TABLE1.mt_sts_cd='002' THEN TABLE1.gr_qty ELSE 0  END)AS 'DSD',  \n");
            varname1.Append(" SUM( CASE WHEN (TABLE1.mt_sts_cd='001' or TABLE1.mt_sts_cd='004') THEN TABLE1.gr_qty ELSE 0  END)  AS 'CSD' ,    \n");
            varname1.Append(" SUM(TABLE1.gr_qty) TK,  \n");
            varname1.Append("'' AS lenght,''size,''recevice_dt,''sts_nm, '' lct_nm , '' sd_no , TABLE1.recevice_dt1   \n");
            varname1.Append("  FROM ( SELECT '' mt_cd, a.mt_no, b.mt_nm,  b.bundle_unit,a.gr_qty,b.spec,a.mt_sts_cd,'' AS lenght,   \n");
            varname1.Append("  (CASE WHEN ('08:00:00' <= DATE_FORMAT( CAST( a.rece_wip_dt AS datetime ),'%H:%i:%s') AND  DATE_FORMAT( CAST( a.rece_wip_dt AS datetime ),'%H:%i:%s')  <  '23:59:59') THEN     DATE_FORMAT( CAST( a.rece_wip_dt AS DATETIME ),'%Y-%m-%d')   \n");
            varname1.Append("   WHEN (DATE_FORMAT( CAST( a.rece_wip_dt AS datetime ),'%H:%i:%s')  < '08:00:00') THEN  DATE_FORMAT( CAST( a.rece_wip_dt AS DATETIME ) - interval 1 DAY ,'%Y-%m-%d')  \n");
            varname1.Append("    ELSE ''     END )  as recevice_dt1    \n");
            varname1.Append("  FROM w_material_info AS a    \n");
            varname1.Append("  JOIN d_material_info AS b ON a.mt_no = b.mt_no  \n");
            varname1.Append("   LEFT JOIN  w_sd_info info ON info.sd_no = a.sd_no  \n");
            varname1.Append("WHERE a.lct_cd LIKE '002%'  and a.mt_type <> 'CMT' AND a.mt_sts_cd!='005'  ");
            varname1.Append("  AND (a.ExportCode IS NULL OR a.ExportCode = '') ");
            varname1.Append(" AND ('" + mt_no + "'='' OR  a.mt_no like '%" + mt_no + "%' ) ");
            varname1.Append(" AND ('" + product_cd + "'='' OR  info.product_cd like '%" + product_cd + "%' ) ");
            varname1.Append(" AND ('" + mt_cd + "'='' OR  a.mt_cd like '%" + mt_cd + "%' ) ");
            varname1.Append(" AND ('" + lct_cd + "'='' OR  a.lct_cd like '%" + lct_cd + "%' ) ");
            varname1.Append(" AND ('" + mt_nm + "'='' OR  b.mt_nm like '%" + mt_nm + "%' ) ");
            varname1.Append(" AND FIND_IN_SET(a.mt_sts_cd, '" + sts + "') != 0 ");
            varname1.Append(" ) TABLE1 ");
            varname1.Append(" WHERE ('" + recevice_dt_start + "'='' OR DATE_FORMAT(TABLE1.recevice_dt1,'%Y/%m/%d') >= DATE_FORMAT('" + recevice_dt_start + "','%Y/%m/%d')) ");
            varname1.Append(" AND ('" + recevice_dt_end + "'='' OR DATE_FORMAT(TABLE1.recevice_dt1,'%Y/%m/%d') <= DATE_FORMAT('" + recevice_dt_end + "','%Y/%m/%d')) ");

            varname1.Append(" GROUP BY TABLE1.mt_no \n");
            varname1.Append("UNION \n");
            varname1.Append(" \n");
            varname1.Append("SELECT * FROM( SELECT  a.mt_cd,a.mt_no,''mt_nm,CONCAT((CASE WHEN b.bundle_unit ='Roll' THEN ROUND((a.gr_qty/b.spec),2) ELSE ROUND(a.gr_qty,2) END),b.bundle_unit)qty, \n");
            varname1.Append("'' DSD,''CSD,''TK, \n");
            varname1.Append("CONCAT(IFNULL(a.gr_qty,''), IFNULL(b.unit_cd,'')) lenght, CONCAT(IFNULL(b.width,0),'*', IFNULL(a.gr_qty,0)) AS size,a.rece_wip_dt, ( \n");
            varname1.Append("SELECT dt_nm \n");
            varname1.Append("FROM comm_dt \n");
            varname1.Append("WHERE comm_dt.dt_cd=a.mt_sts_cd AND comm_dt.mt_cd='WHS005') sts_nm  , location.lct_nm, a.sd_no , \n");
            varname1.Append("  (CASE WHEN ('08:00:00' <= DATE_FORMAT( CAST( a.rece_wip_dt AS datetime ),'%H:%i:%s') AND  DATE_FORMAT( CAST( a.rece_wip_dt AS datetime ),'%H:%i:%s')  <  '23:59:59') THEN     DATE_FORMAT( CAST( a.rece_wip_dt AS DATETIME ),'%Y-%m-%d')   \n");
            varname1.Append("   WHEN (DATE_FORMAT( CAST( a.rece_wip_dt AS datetime ),'%H:%i:%s')  < '08:00:00') THEN  DATE_FORMAT( CAST( a.rece_wip_dt AS DATETIME ) - interval 1 DAY ,'%Y-%m-%d')  \n");
            varname1.Append("    ELSE ''     END )  as recevice_dt1    \n");
            varname1.Append("FROM w_material_info a \n");
            varname1.Append("LEFT JOIN d_material_info b ON a.mt_no=b.mt_no \n");
            varname1.Append(" LEFT JOIN lct_info location ON a.lct_cd=location.lct_cd  \n");
            varname1.Append("  LEFT JOIN comm_dt as com ON a.mt_sts_cd  = com.dt_cd AND com.mt_cd='WHS005'   \n");
            varname1.Append(" LEFT JOIN  w_sd_info info ON info.sd_no = a.sd_no   \n");
            varname1.Append("WHERE a.lct_cd LIKE '002%'  and a.mt_type <> 'CMT' AND a.mt_sts_cd!='005'  ");
            varname1.Append("  AND (a.ExportCode IS NULL OR a.ExportCode = '') ");
            varname1.Append(" AND ('" + mt_no + "'='' OR  a.mt_no like '%" + mt_no + "%' ) ");
            varname1.Append(" AND ('" + product_cd + "'='' OR  info.product_cd like '%" + product_cd + "%' ) ");
            varname1.Append(" AND ('" + mt_cd + "'='' OR  a.mt_cd like '%" + mt_cd + "%' ) ");
            varname1.Append(" AND ('" + lct_cd + "'='' OR  a.lct_cd like '%" + lct_cd + "%' ) ");
            varname1.Append(" AND ('" + mt_nm + "'='' OR  b.mt_nm like '%" + mt_nm + "%' ) ");
            varname1.Append(" AND FIND_IN_SET(a.mt_sts_cd, '"+ sts + "') != 0 ");
            varname1.Append(") TABLE1 ");
            varname1.Append(" WHERE ('" + recevice_dt_start + "'='' OR DATE_FORMAT(TABLE1.recevice_dt1,'%Y/%m/%d') >= DATE_FORMAT('" + recevice_dt_start + "','%Y/%m/%d')) ");
            varname1.Append(" AND ('" + recevice_dt_end + "'='' OR DATE_FORMAT(TABLE1.recevice_dt1,'%Y/%m/%d') <= DATE_FORMAT('" + recevice_dt_end + "','%Y/%m/%d')) ");


            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

            List<WIP_ParentInventoryModel> listTotal = new InitMethods().ConvertDataTable<WIP_ParentInventoryModel>(varname1);

            listTotal = listTotal.OrderBy(x => x.mt_no).ThenBy(x => x.mt_cd).ToList();

            DataTable datatb = new InitMethods().ConvertListToDataTable(listTotal);

            DataSet ds2 = new DataSet();

            ds2.Tables.Add(datatb);
            ds2.Tables[0].TableName = "WIP_General";

            using (XLWorkbook wb = new XLWorkbook())
            {
                var ws = wb.AddWorksheet(datatb);

                ws.Columns().AdjustToContents();
                ws.Cells("A1").Value = "MT Code";
                ws.Cells("B1").Value = "MT Name";
                ws.Cells("C1").Value = "Composite Code";

                ws.Cells("D1").Value = "QTy (Roll/EA)";
                ws.Cells("E1").Value = "Đang sử dụng Qty Length(M)";
                ws.Cells("F1").Value = "Chưa sử dụng Qty Length(M)";
                ws.Cells("G1").Value = "Tồn Kho Qty Length(M)";
                ws.Cells("H1").Value = "Lenght";
                ws.Cells("I1").Value = "Size";
                ws.Cells("J1").Value = "Receving Date";
                ws.Cells("K1").Value = "Status";
                ws.Cells("L1").Value = "Location";
                ws.Cells("M1").Value = "SD No";
                //ws.Columns("K").Hide(); //an cot I

                ws.Rows().AdjustToContents();
                wb.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wb.Style.Alignment.ShrinkToFit = true;

                wb.Style.Font.Bold = true;
                Response.Clear();
                Response.Buffer = true;
                Response.Charset = "";
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;filename=WIP_General.xlsx");
                using (MemoryStream MyMemoryStream = new MemoryStream())
                {
                    wb.SaveAs(MyMemoryStream);
                    MyMemoryStream.WriteTo(Response.OutputStream);
                    Response.Flush();
                    Response.End();
                }
            }
            return View("~/Views/WIP/Inventory/General.cshtml");
        }
        public ActionResult General()
        {
            return SetLanguage("~/Views/WIP/Inventory/General.cshtml");
      
        }
        public JsonResult getGeneral(Pageing paging)
        {
            string at_no = Request["at_no"] == null ? "" : Request["at_no"].Trim();
            string model = Request["model"] == null ? "" : Request["model"].Trim();
            string product = Request["product"] == null ? "" : Request["product"].Trim();
            string product_name = Request["product_name"] == null ? "" : Request["product_name"].Trim();
            string reg_dt_start = Request["reg_dt_start"] == null ? "" : Request["reg_dt_start"].Trim();
            string reg_dt_end = Request["reg_dt_end"] == null ? "" : Request["reg_dt_end"].Trim();
            string mt_cd = Request["mt_cd"] == null ? "" : Request["mt_cd"].Trim();
            string bb_no = Request["bb_no"] == null ? "" : Request["bb_no"].Trim();



            Dictionary<string, string> list = PagingAndOrderBy(paging, " ");


            IEnumerable<InventoryWIPComposite> Data = _IWIPService.GetListSearchShowInventoryWIPCPS(at_no, model,product, product_name, reg_dt_start, reg_dt_end,mt_cd, bb_no);
         
            int totalRecords = Data.Count();
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



            var result = new
            {
                total = totalPages,
                page = int.Parse(list["index"]),
                records = totalRecords,
                rows = Data
            };
            return Json(result, JsonRequestBehavior.AllowGet);


            //  StringBuilder varname1 = new StringBuilder();
            //  varname1.Append("SET sql_mode = '';\n");

            //if (sts != "" && sts != null)
            //      {
            //          varname1.Append(" select * from (");
            //      }

            //          varname1.Append("SELECT  a.wmtid,concat(COUNT(1),' ', 'Roll') qty ,a.product product_cd,(SELECT style_nm FROM d_style_info WHERE style_no=a.product)product_nm, \n");
            //  varname1.Append("         case when \n");
            //  varname1.Append("				b.`level` !=(SELECT max(`level`)level_id FROM w_actual \n");
            //  varname1.Append("								WHERE at_no=a.at_no AND `type`='SX') AND b.`type`!='TIMS' \n");
            //  varname1.Append(" AND ('" + product + "'='' OR  a.product like '%" + product + "%' ) ");
            //  varname1.Append(" AND ('" + bb_no + "'='' OR  a.bb_no like '%" + bb_no + "%' ) ");
            //  varname1.Append("								then \n");
            //  varname1.Append("						SUM(a.gr_qty) \n");
            //  varname1.Append("			ELSE 0 \n");
            //  varname1.Append("			END AS 'BTP', \n");
            //  varname1.Append("			 \n");
            //  varname1.Append("			 case when \n");
            //  varname1.Append("				b.`level` =(SELECT max(`level`)level_id FROM w_actual \n");
            //  varname1.Append("								WHERE at_no=a.at_no AND `type`='SX') AND b.`type`!='TIMS' \n");
            //  varname1.Append(" AND ('" + product + "'='' OR  a.product like '%" + product + "%' ) ");
            //  varname1.Append(" AND ('" + bb_no + "'='' OR  a.bb_no like '%" + bb_no + "%' ) ");
            //  varname1.Append("								then \n");
            //  varname1.Append("						SUM(a.real_qty) \n");
            //  varname1.Append("			ELSE 0 \n");
            //  varname1.Append("			END AS 'TP' \n");
            //  varname1.Append("FROM     `w_material_info` `a` \n");
            //  varname1.Append("JOIN w_actual AS b ON a.id_actual=b.id_actual \n");
            //  varname1.Append("WHERE (a.lct_cd = '002002000000000000' OR a.lct_cd = '002000000000000000') AND a.mt_type='CMT' AND \n");
            //  varname1.Append("( a.mt_sts_cd!='005' AND a.mt_sts_cd!='003' ) \n");
            //  varname1.Append(" AND ('" + product + "'='' OR  a.product like '%" + product + "%' ) ");
            //  varname1.Append(" AND ('" + bb_no + "'='' OR  a.bb_no like '%" + bb_no + "%' ) ");
            //  varname1.Append("GROUP BY `a`.`product`");

            //  if (sts != "" && sts != null)
            //  {
            //      varname1.Append(" ) table_new where " + sts + " >0 ;");
            //  }

            //DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

            //int total = dt.Rows.Count;
            //var result = dt.AsEnumerable().OrderByDescending(x => x.Field<string>("product_cd"));
            //return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
        }

        //public JsonResult getGeneral(Pageing paging)
        //{
        //    string product = Request["product"] == null ? "" : Request["product"].Trim();
        //    string bb_no = Request["bb_no"] == null ? "" : Request["bb_no"].Trim();
        //    string sts = Request["sts"] == null ? "" : Request["sts"].Trim();

        //    StringBuilder varname1 = new StringBuilder();
        //    varname1.Append("SELECT * from General_WIP as a");
        //    varname1.Append(" Where ('" + product + "'='' OR  a.product_cd like '%" + product + "%' ) ");
        //    varname1.Append(" AND ('" + bb_no + "'='' OR  a.bb_no like '%" + bb_no + "%' ) and a.BTP>0 ");
        //    if (sts != "" && sts != null)
        //    {
        //        varname1.Append(" and a." + sts + " >0");
        //    }
        //    DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

        //    int total = dt.Rows.Count;
        //    var result = dt.AsEnumerable().OrderByDescending(x => x.Field<string>("product_cd"));
        //    return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
        //}

        public JsonResult GetgeneralDetail(Pageing paging)
        {
            string product = Request["product"] == null ? "" : Request["product"].Trim();
            string at_no = Request["at_no"] == null ? "" : Request["at_no"].Trim();
            string model = Request["model"] == null ? "" : Request["model"].Trim();
           
            string product_name = Request["product_name"] == null ? "" : Request["product_name"].Trim();
            string reg_dt_start = Request["reg_dt_start"] == null ? "" : Request["reg_dt_start"].Trim();
            string reg_dt_end = Request["reg_dt_end"] == null ? "" : Request["reg_dt_end"].Trim();
            string mt_cd = Request["mt_cd"] == null ? "" : Request["mt_cd"].Trim();
            string bb_no = Request["bb_no"] == null ? "" : Request["bb_no"].Trim();
            //string sts = "TP";

            StringBuilder varname1 = new StringBuilder();
            varname1.Append("SELECT * FROM( SELECT a.wmtid, \n");
            varname1.Append("       a.mt_cd,at_no, \n");
            varname1.Append("       a.mt_no,    CONCAT(a.gr_qty, ' EA') qty,  \n");
            //varname1.Append("       ( CASE \n");
            //varname1.Append("           WHEN b.bundle_unit = 'Roll' THEN Round(( a.gr_qty / b.spec ), 2) \n");
            //varname1.Append("           ELSE Round(a.gr_qty, 2) \n");
            //varname1.Append("         end )                                              qty, \n");
            //varname1.Append("       b.bundle_unit, \n");
            varname1.Append("        a.reg_dt,a.bb_no,  \n");
            varname1.Append("   (CASE WHEN ('08:00:00' <= DATE_FORMAT( CAST( a.reg_dt AS datetime ),'%H:%i:%s') AND  DATE_FORMAT( CAST( a.reg_dt AS datetime ),'%H:%i:%s')  <  '23:59:00') THEN  DATE_FORMAT( CAST( a.reg_dt AS DATETIME ),'%Y-%m-%d')  when (DATE_FORMAT( CAST( a.reg_dt AS datetime ),'%H:%i:%s')  < '08:00:00') THEN  DATE_FORMAT( CAST( a.reg_dt AS DATETIME ) - interval 1 DAY ,'%Y-%m-%d') ELSE ''END) as reg_date \n");
            varname1.Append("FROM w_material_info AS a \n");
            //varname1.Append(" JOIN d_material_info AS b ON a.mt_no=b.mt_no \n");
            //varname1.Append("JOIN comm_dt AS com ON com.dt_cd=a.mt_sts_cd  AND com.mt_cd ='WHS005'  \n");
            //varname1.Append("WHERE a.lct_cd LIKE '002%' AND a.mt_type= 'CMT' AND a.mt_sts_cd NOT IN ('005','003') \n");
            varname1.Append("WHERE a.lct_cd LIKE '002%' AND a.mt_type= 'CMT' AND a.mt_sts_cd ='002' \n");
            varname1.Append("AND a.product='" + product + "'");
            varname1.Append(" AND ('" + at_no + "'='' OR  a.at_no like '%" + at_no + "%' ) ");
            varname1.Append(" AND ('" + mt_cd + "'='' OR  a.mt_cd like '%" + mt_cd + "%' ) ");
            varname1.Append(" AND ('" + bb_no + "'='' OR  a.bb_no like '%" + bb_no + "%' ) ");
            varname1.Append(" ) TABLE1 ");
            varname1.Append("   where  ('" + reg_dt_start + "'='' OR DATE_FORMAT(table1.reg_date,'%Y/%m/%d') >= DATE_FORMAT('" + reg_dt_start + "','%Y/%m/%d'))  ");
            varname1.Append("   AND('" + reg_dt_end + "' = '' OR DATE_FORMAT(table1.reg_date, '%Y/%m/%d') <= DATE_FORMAT('" + reg_dt_end + "', '%Y/%m/%d')) ");

            //if (sts == "TP")
            //{
            //    varname1.Append("AND a.id_actual IN ( \n");
            //    varname1.Append(" \n");
            //    varname1.Append("SELECT `l`.`id_actual` \n");
            //    varname1.Append("FROM (`w_actual` `l` \n");
            //    varname1.Append("JOIN `w_actual_primary` `m` ON((`l`.`at_no` = `m`.`at_no`))) \n");
            //    varname1.Append("WHERE ((`l`.`type` = 'SX') AND `l`.`reg_dt`not in ( \n");
            //    varname1.Append("SELECT MAX(`w_actual`.`reg_dt`) \n");
            //    varname1.Append("FROM `w_actual` \n");
            //    varname1.Append("WHERE (`w_actual`.`type` = 'SX') \n");
            //    varname1.Append("GROUP BY `w_actual`.`at_no`) IS FALSE AND (`m`.`product` = `d`.`product`)) \n");
            //    varname1.Append(" \n");
            //    varname1.Append(")");
            //}
            //if (sts == "BTP")
            //{
            //    varname1.Append("AND a.id_actual IN ( \n");
            //    varname1.Append(" \n");
            //    varname1.Append("SELECT `l`.`id_actual` \n");
            //    varname1.Append("FROM (`w_actual` `l` \n");
            //    varname1.Append("JOIN `w_actual_primary` `m` ON((`l`.`at_no` = `m`.`at_no`))) \n");
            //    varname1.Append("WHERE ((`l`.`type` = 'SX') AND `l`.`reg_dt`  in ( \n");
            //    varname1.Append("SELECT MAX(`w_actual`.`reg_dt`) \n");
            //    varname1.Append("FROM `w_actual` \n");
            //    varname1.Append("WHERE (`w_actual`.`type` = 'SX') \n");
            //    varname1.Append("GROUP BY `w_actual`.`at_no`) IS FALSE AND (`m`.`product` = `d`.`product`)) \n");
            //    varname1.Append(" \n");
            //    varname1.Append(")");
            //}
            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("wmtid"));
            return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
        }
        public ActionResult PrintExcelTab2()
        {
            string at_no = Request["at_no"] == null ? "" : Request["at_no"].Trim();
            string model = Request["model"] == null ? "" : Request["model"].Trim();
            string product = Request["product"] == null ? "" : Request["product"].Trim();
            string product_name = Request["product_name"] == null ? "" : Request["product_name"].Trim();
            string reg_dt_start = Request["reg_dt_start"] == null ? "" : Request["reg_dt_start"].Trim();
            string reg_dt_end = Request["reg_dt_end"] == null ? "" : Request["reg_dt_end"].Trim();
            string mt_cd = Request["mt_cd"] == null ? "" : Request["mt_cd"].Trim();
            string bb_no = Request["bb_no"] == null ? "" : Request["bb_no"].Trim();

            StringBuilder sql = new StringBuilder($"Call spWIP_InventoryGeneral_Composite('{at_no}','{model}','{product}','{product_name}','{reg_dt_start}','{reg_dt_end}','{mt_cd}','{bb_no}');");
            List<ExcelInventoryWIPComposite> listTotal = new InitMethods().ConvertDataTable<ExcelInventoryWIPComposite>(sql);
            List<ExcelInventoryWIPComposite> listOrderBy = new List<ExcelInventoryWIPComposite>();
            listOrderBy = listTotal.OrderBy(x => x.product_cd).ThenBy(x => x.mt_cd).ToList();

            DataTable dt = new InitMethods().ConvertListToDataTable(listOrderBy);
            DataSet ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables[0].TableName = "WIP_Inventory_Composite";

            using (XLWorkbook wb = new XLWorkbook())
            {
                var ws = wb.AddWorksheet(dt);
                ws.Columns().AdjustToContents();
                ws.Rows().AdjustToContents();
                wb.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wb.Style.Alignment.ShrinkToFit = true;
                wb.Style.Font.Bold = true;
                Response.Clear();
                Response.Buffer = true;
                Response.Charset = "";
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;filename= TIMS_Inventory_General.xlsx");
                using (MemoryStream MyMemoryStream = new MemoryStream())
                {
                    wb.SaveAs(MyMemoryStream);
                    MyMemoryStream.WriteTo(Response.OutputStream);
                    Response.Flush();
                    Response.End();
                }
            }
            return View("~/Views/WIP/Inventory/General.cshtml");
        }

        public ActionResult qrGeneral(string mt_no)
        {
            if (mt_no != "")
            {
                StringBuilder varname1 = new StringBuilder();
                varname1.Append("SELECT a.wmtid,a.mt_cd,b.mt_nm,CONCAT(ifnull(a.gr_qty,''),ifnull(b.unit_cd,'')) lenght,CONCAT(ifnull(b.width,0),'MM*',ifnull(B.spec,0),'M') AS size,ifnull(b.spec,0) spec,a.mt_no, ");
                varname1.Append(" (case when b.bundle_unit ='Roll' then  ROUND((a.gr_qty/b.spec),2) ELSE ROUND(a.gr_qty,2) END) qty,b.bundle_unit bundle_unit,  ");
                varname1.Append("a.recevice_dt, ");
                varname1.Append("(SELECT dt_nm FROM comm_dt WHERE comm_dt.dt_cd=a.mt_sts_cd AND comm_dt.mt_cd='WHS005') sts_nm,a.lot_no,a.expore_dt,a.dt_of_receipt,a.expiry_dt ");
                varname1.Append("FROM w_material_info a ");
                varname1.Append("LEFT JOIN d_material_info  b ON a.mt_no=b.mt_no ");
                varname1.Append("WHERE a.lct_cd LIKE '002%' AND a.wmtid IN (" + mt_no + ")  ORDER BY a.mt_no, a.mt_cd ");

                //varname1.Append("LEFT JOIN d_material_info  b ON a.mt_no=b.mt_no ");
                //varname1.Append("SELECT * FROM v_wipqrgeneral a ");
                //varname1.Append("WHERE a.wmtid IN (" + mt_no + ")");

                return new InitMethods().ConvertDataTableToJsonAndReturn(varname1);
            }
            return View();
        }

        public ActionResult ExportgeneralToExcel()
        {


            StringBuilder varname1 = new StringBuilder();
            varname1.Append("SELECT * FROM ( ");
            varname1.Append("SELECT v_excelwipgeneral_one.product_cd ProductCode, v_excelwipgeneral_one.mt_no 'CODE', '' AS 'CompositeCode',v_excelwipgeneral_one.mt_nm 'NAME', CONCAT(IFNULL(v_excelwipgeneral_one.qty, 0), ' ', IFNULL(v_excelwipgeneral_one.bundle_unit, '')) 'QTY', CONCAT(IFNULL(v_excelwipgeneral_one.qty2, 0), ' ', IFNULL(v_excelwipgeneral_one.bundle_unit, '')) stk_qty, ");
            varname1.Append("v_excelwipgeneral_one.lenght 'LENGTH', v_excelwipgeneral_one.size SIZE,'' AS STATUS, '' AS 'ReceviceDate',v_excelwipgeneral_one.mt_no 'MT_NO' ");
            varname1.Append("FROM v_excelwipgeneral_one ");
            varname1.Append("UNION ");
            varname1.Append("SELECT '' AS ProductCode,'' AS 'CODE',v_excelwipgeneral_two.mt_cd 'CompositeCode', v_excelwipgeneral_two.mt_nm 'NAME', ");
            varname1.Append("v_excelwipgeneral_two.qty 'QTY', '' stk_qty, v_excelwipgeneral_two.lenght 'LENGTH', v_excelwipgeneral_two.size SIZE, v_excelwipgeneral_two.sts_nm 'STATUS', IFNULL(DATE_FORMAT(v_excelwipgeneral_two.recevice_dt, '%Y-%m-%d'),'') 'ReceviceDATE', ");
            varname1.Append("v_excelwipgeneral_two.mt_no 'MT_NO' ");
            varname1.Append("FROM v_excelwipgeneral_two) AS RESULTS");
            //varname1.Append("        ORDER BY RESULTS.MT_NO, RESULTS.CompositeCode");

            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

            List<wipgeneralexportexcel> listTotal = new InitMethods().ConvertDataTable<wipgeneralexportexcel>(varname1);

            listTotal = listTotal.OrderBy(x => x.MT_NO).ThenBy(x => x.CompositeCode).ToList();

            DataTable datatb = new InitMethods().ConvertListToDataTable(listTotal);

            DataSet ds2 = new DataSet();

            ds2.Tables.Add(datatb);
            ds2.Tables[0].TableName = "WIP_General";
            //ds2.Tables[1].TableName = "DETAIL";
            //ds2.Tables.Add(data3);
            //ds.Tables.Add(dtEmpOrder);

            using (XLWorkbook wb = new XLWorkbook())
            {
                //var ws = wb.AddWorksheet("ds2");
                //ws.Columns().AdjustToContents();
                //ws.Rows().AdjustToContents();

                //wb.Worksheets.Add(ds2);

                var ws = wb.AddWorksheet(datatb);

                ws.Columns().AdjustToContents();
                ws.Cells("C1").Value = "Composite Code";
                ws.Cells("E1").Value = "QTY (Roll/EA)";
                ws.Cells("F1").Value = "QTY STOCK (Roll/EA)";
                ws.Cells("H1").Value = "Recevice Date";
                ws.Columns("K").Hide(); //an cot I

                //ws.Columns(1, 3).Hide(); //cot 1-2-3
                ////ws.Rows(1, 3).Hide(); // an dong 1-2-3
                //ws.Column(2).Unhide(); // bo an cdong

                //ws.Cells("I1").EntireRow.
                //ws.Cell(1, 1).Value = "TEST_";
                //ws.Cell(2, 1).InsertData(datatb);
                //ws.Style.co
                //ws.co

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
                Response.AddHeader("content-disposition", "attachment;filename=WIP_General.xlsx");
                using (MemoryStream MyMemoryStream = new MemoryStream())
                {
                    wb.SaveAs(MyMemoryStream);
                    MyMemoryStream.WriteTo(Response.OutputStream);
                    Response.Flush();
                    Response.End();
                }
            }
            return View("~/Views/WIP/Inventory/General.cshtml");
        }

        public class wipgeneralexportexcel
        {
            public string ProductCode { get; set; }

            public string CODE { get; set; }
            public string CompositeCode { get; set; }
            public string NAME { get; set; }
            public string QTY { get; set; }
            public string stk_qty { get; set; }

            public string LENGTH { get; set; }
            public string SIZE { get; set; }
            public string STATUS { get; set; }
            public string ReceviceDate { get; set; }

            //public string qty { get; set; }
            //public string return_date { get; set; }
            public string MT_NO { get; set; }
        }

        [HttpPost]
        public ActionResult PrintGeneral()
        {
            string[] keys = Request.Form.AllKeys;

            var value = "";
            value = Request.Form[keys[0]];

            ViewData["Message"] = value;
            return View("~/Views/WIP/Inventory/PrintGeneral.cshtml");
        }

        public ActionResult qrPrintQr(string mt_no)
        {
            if (mt_no != "")
            {
                StringBuilder varname1 = new StringBuilder();
                varname1.Append("SELECT a.wmtid,a.mt_cd,b.mt_nm, ");
                varname1.Append("CONCAT(ifnull(a.gr_qty,''),ifnull(b.unit_cd,'')) lenght, ");
                varname1.Append("ifnull(a.gr_qty,'') lenght1, ");
                varname1.Append("CONCAT(ifnull(b.width,0),'MM*',ifnull(b.spec,0),'M') AS size,");
                //varname1.Append("CONCAT(ifnull(b.width,0),'*',ifnull(a.gr_qty,0)) AS size, ");
                varname1.Append("ifnull(b.spec,0) spec,a.mt_no,  ROUND(CONCAT((case when b.bundle_unit ='Roll' then  (a.gr_qty/b.spec) ELSE a.gr_qty END)),2) qty, ");
                varname1.Append("b.bundle_unit, a.return_date, a.recevice_dt,a.lot_no,a.expore_dt,a.dt_of_receipt,a.expiry_dt,");
                varname1.Append(" (SELECT dt_nm FROM comm_dt WHERE comm_dt.dt_cd=a.mt_sts_cd AND comm_dt.mt_cd='WHS005') sts_nm , ");
                varname1.Append(" (SELECT w_actual_primary.product  FROM   w_actual JOIN w_actual_primary ON w_actual.at_no=w_actual_primary.at_no WHERE  a.id_actual = w_actual.id_actual LIMIT 1) product, ");
                varname1.Append("(SELECT name FROM w_actual WHERE a.id_actual=w_actual.id_actual LIMIT 1) AS name ");
                varname1.Append(" FROM w_material_info a LEFT JOIN d_material_info  b ON a.mt_no=b.mt_no ");
                varname1.Append("WHERE a.mt_sts_cd='004' AND a.sts_update='Return' AND a.wmtid IN (" + mt_no + ")  ORDER BY a.mt_no, a.mt_cd  ");



                return new InitMethods().ConvertDataTableToJsonAndReturn(varname1);
            }
            return View();
        }

        #region PrintQr

        public ActionResult PrintQR()
        {
            HttpCookie cookie = HttpContext.Request.Cookies["language"];
            if (cookie != null)
            {
                ViewBag.language = cookie.Value;
            }
            return View("~/Views/WIP/PrintQR/PrintQR.cshtml");
        }

        public ActionResult btnPrintQR()
        {
            string[] keys = Request.Form.AllKeys;

            var value = "";
            value = Request.Form[keys[0]];

            ViewData["Message"] = value;
            return View("~/Views/WIP/PrintQR/BtnPrintQr.cshtml");
        }

        public JsonResult getPrintQR(Pageing paging)
        {
            string mt_no = Request["mt_no"] == null ? "" : Request["mt_no"].Trim();
            string return_date = Request["return_date"] == null ? "" : Request["return_date"].Trim();
            string recevice_dt_start = Request["recevice_dt_start"] == null ? "" : Request["recevice_dt_start"].Trim();
            string recevice_dt_end = Request["recevice_dt_end"] == null ? "" : Request["recevice_dt_end"].Trim();
            //getpicMpaging

            var dateConvert = new DateTime();
            if (DateTime.TryParse(return_date, out dateConvert))
            {
                return_date = dateConvert.ToString("yyyy/MM/dd");
            }
            if (DateTime.TryParse(recevice_dt_end, out dateConvert))
            {
                recevice_dt_end = dateConvert.ToString("yyyy/MM/dd");
            }
            if (DateTime.TryParse(recevice_dt_start, out dateConvert))
            {
                recevice_dt_start = dateConvert.ToString("yyyy/MM/dd");
            }

            StringBuilder varname1 = new StringBuilder();
            varname1.Append("SELECT a.wmtid,a.mt_cd,b.mt_nm, ");
            varname1.Append("CONCAT(SUM(a.gr_qty),ifnull(b.unit_cd,'')) lenght, ");
            varname1.Append("CONCAT(ifnull(b.width,0),'*',ifnull(a.gr_qty,0)) AS size, ");
            varname1.Append("ifnull(b.spec,0) spec,a.mt_no, SUM((case when b.bundle_unit ='Roll' then  ROUND((a.gr_qty/b.spec),2) ELSE a.gr_qty END)) qty, ");
            varname1.Append("b.bundle_unit, a.return_date, ");
            varname1.Append("(SELECT dt_nm FROM comm_dt WHERE comm_dt.dt_cd=a.mt_sts_cd AND comm_dt.mt_cd='WHS005' LIMIT 1) sts_nm , ");
            varname1.Append(" (SELECT w_actual_primary.product FROM w_actual JOIN w_actual_primary ON w_actual.at_no=w_actual_primary.at_no WHERE  a.id_actual = w_actual.id_actual LIMIT 1) product, ");
            varname1.Append("(SELECT name FROM w_actual WHERE a.id_actual=w_actual.id_actual LIMIT 1) AS name ");
            varname1.Append("FROM w_material_info a ");
            varname1.Append("LEFT JOIN d_material_info  b ON a.mt_no=b.mt_no  ");
            varname1.Append("WHERE a.mt_sts_cd='004' AND a.sts_update='Return' and a.lct_cd LIKE '002%'  ");
            varname1.Append(" AND ('" + mt_no + "'='' OR  a.mt_no like '%" + mt_no + "%' ) ");
            varname1.Append(" AND ('" + return_date + "'='' OR DATE_FORMAT(a.return_date,'%Y/%m/%d') = DATE_FORMAT('" + return_date + "','%Y/%m/%d')) ");
            varname1.Append(" AND ('" + recevice_dt_start + "'='' OR DATE_FORMAT(a.recevice_dt,'%Y/%m/%d') >= DATE_FORMAT('" + recevice_dt_start + "','%Y/%m/%d')) ");
            varname1.Append(" AND ('" + recevice_dt_end + "'='' OR DATE_FORMAT(a.recevice_dt,'%Y/%m/%d') <= DATE_FORMAT('" + recevice_dt_end + "','%Y/%m/%d')) ");
            varname1.Append("GROUP BY a.mt_no");

            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("wmtid"));
            return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
        }

        public JsonResult getPrintQRDetail(Pageing paging)
        {
            //int id = Convert.ToInt32(Request["wmtid_DT"] == null ? "" : Request["wmtid_DT"].Trim());
            //var mt_no = db.w_material_info.FirstOrDefault(x => x.wmtid == id).mt_no;

            string productCode = Request["productCode"] == null ? "" : Request["productCode"].Trim();
            string return_date = Request["return_date"] == null ? "" : Request["return_date"].Trim();
            string mtCode = Request["mtCode"] == null ? "" : Request["mtCode"].Trim();
            string poCode = Request["poCode"] == null ? "" : Request["poCode"].Trim();

            StringBuilder varname1 = new StringBuilder();

            //varname1.Append("SELECT a.wmtid,a.mt_cd,b.mt_nm, ");
            //varname1.Append("CONCAT(ifnull(a.gr_qty,''),ifnull(b.unit_cd,'')) lenght, ");
            //varname1.Append("ifnull(a.gr_qty,'') lenght1, ");
            //varname1.Append("CONCAT(ifnull(b.width,0),'*',ifnull(a.gr_qty,0)) AS size, ");
            //varname1.Append("ifnull(b.spec,0) spec,a.mt_no,  CONCAT((case when b.bundle_unit ='Roll' then  (a.gr_qty/b.spec) ELSE a.gr_qty END),' ',ifnull(b.bundle_unit,'')) qty, ");
            //varname1.Append(" b.bundle_unit, a.return_date, ");
            //varname1.Append(" (SELECT dt_nm FROM comm_dt WHERE comm_dt.dt_cd=a.mt_sts_cd AND comm_dt.mt_cd='WHS005') sts_nm , ");
            //varname1.Append(" (SELECT w_actual_primary.product FROM w_actual JOIN w_actual_primary ON w_actual.at_no=w_actual_primary.at_no WHERE  a.id_actual = w_actual.id_actual LIMIT 1) product, ");
            //varname1.Append("(SELECT name FROM w_actual WHERE a.id_actual=w_actual.id_actual LIMIT 1) AS name ");
            //varname1.Append(" FROM w_material_info a LEFT JOIN d_material_info  b ON a.mt_no=b.mt_no ");
            //varname1.Append("WHERE a.mt_sts_cd='004' and a.lct_cd LIKE '002%' AND a.sts_update='Return' ");

            varname1.Append("SELECT a.wmtid, ");
            varname1.Append("       a.mt_cd, ");
            varname1.Append("       b.mt_nm, ");
            varname1.Append("       CONCAT(ifnull(a.gr_qty, ''), ifnull(b.unit_cd, '')) lenght, ");
            varname1.Append("       ifnull(a.gr_qty, '') lenght1, ");
            varname1.Append("       CONCAT(ifnull(b.width, 0), '*', ifnull(a.gr_qty, 0)) AS size, ");
            varname1.Append("       ifnull(b.spec, 0) spec, ");
            varname1.Append("       a.mt_no, ");
            varname1.Append("       CONCAT((CASE ");
            varname1.Append("                   WHEN b.bundle_unit ='Roll' THEN (a.gr_qty/b.spec) ");
            varname1.Append("                   ELSE a.gr_qty ");
            varname1.Append("               END),' ', ifnull(b.bundle_unit, '')) qty, ");
            varname1.Append("       b.bundle_unit, ");
            varname1.Append("       a.return_date, ");
            varname1.Append(" ");
            varname1.Append("  (SELECT dt_nm ");
            varname1.Append("   FROM comm_dt ");
            varname1.Append("   WHERE comm_dt.dt_cd=a.mt_sts_cd ");
            varname1.Append("     AND comm_dt.mt_cd='WHS005' LIMIT 1) sts_nm, ");
            varname1.Append("       a.sts_update,(SELECT machine_id from w_material_info WHERE mt_cd=a.orgin_mt_cd)AS machine ");
            varname1.Append("FROM w_material_info a ");
            varname1.Append("LEFT JOIN d_material_info b ON a.mt_no=b.mt_no ");
            varname1.Append("WHERE a.mt_sts_cd='004' ");
            varname1.Append("  AND a.lct_cd LIKE '002%' ");
            varname1.Append("  AND a.sts_update='Return' ");
            varname1.Append("  AND (STR_TO_DATE(SUBSTRING(a.return_date, 1, 10), '%Y%m%d') = STR_TO_DATE('" + return_date + "', '%Y-%m-%d') OR '' = '') ");
            varname1.Append("  AND a.mt_cd LIKE '%" + mtCode + "%' ");

            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("wmtid"));
            return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
        }

        public class w_material_model
        {
            public string mt_cd { get; set; }
            public string wmtid { get; set; }
            public string mt_nm { get; set; }
            public string lenght { get; set; }
            public string lenght1 { get; set; }
            public string size { get; set; }
            public string spec { get; set; }
            public string mt_no { get; set; }
            public string qty { get; set; }
            public string bundle_unit { get; set; }
            public string return_date { get; set; }
            public string sts_nm { get; set; }
            public string product { get; set; }
            public string name { get; set; }
        }

        public JsonResult update_lenght_qty(string wmtid, string lenght_qty)
        {
            var id = int.Parse(wmtid);

            var flag = db.w_material_info.Where(x => x.wmtid == id).ToList();

            if (flag.Count() == 1)
            {
                w_material_info db_w_material = flag.FirstOrDefault();
                //không để n âm nhé
                var vaule1 = db.w_material_info.Where(x => x.mt_cd == db_w_material.orgin_mt_cd).ToList();

                if (vaule1.Count() == 1)
                {
                    var gr_qty_tmp = db_w_material.gr_qty;
                    var item = vaule1.FirstOrDefault();
                    var vaule2 = item.gr_qty;
                    if (int.Parse(lenght_qty) <= item.real_qty)
                    {
                        DateTime dt = DateTime.Now;
                        db_w_material.gr_qty = int.Parse(lenght_qty);
                        db.Entry(db_w_material).State = EntityState.Modified;
                        db.SaveChanges();




                        w_material_info db_w_material_rm = db.w_material_info.Find(item.wmtid);
                        db_w_material_rm.gr_qty = vaule2 - (int.Parse(lenght_qty) - gr_qty_tmp);
                        //db_w_material_rm.chg_dt = DateTime.Now;
                        db.Entry(db_w_material_rm).State = EntityState.Modified;
                        db.SaveChanges();

                        StringBuilder varname1 = new StringBuilder();
                        varname1.Append("SELECT a.wmtid,a.mt_cd,b.mt_nm, ");
                        varname1.Append("CONCAT(ifnull(a.gr_qty,''),ifnull(b.unit_cd,'')) lenght, ");
                        varname1.Append("ifnull(a.gr_qty,'') lenght1, ");
                        varname1.Append("CONCAT(ifnull(b.width,0),'*',ifnull(a.gr_qty,0)) AS size, ");
                        varname1.Append("ifnull(b.spec,0) spec,a.mt_no,  CONCAT((case when b.bundle_unit ='Roll' then  (a.gr_qty/b.spec) ELSE a.gr_qty END),' ',b.bundle_unit) qty, ");
                        varname1.Append("b.bundle_unit, a.return_date, ");
                        varname1.Append(" (SELECT dt_nm FROM comm_dt WHERE comm_dt.dt_cd=a.mt_sts_cd AND comm_dt.mt_cd='WHS005') sts_nm , ");
                        varname1.Append(" (SELECT w_actual_primary.product FROM w_actual JOIN w_actual_primary ON w_actual.at_no=w_actual_primary.at_no  WHERE a.id_actual=w_actual.id_actual LIMIT 1) AS product, ");
                        varname1.Append("(SELECT name FROM w_actual WHERE a.id_actual=w_actual.id_actual LIMIT 1) AS name ");
                        varname1.Append(" FROM w_material_info a LEFT JOIN d_material_info  b ON a.mt_no=b.mt_no ");
                        varname1.Append("WHERE a.mt_sts_cd='004' and a.lct_cd LIKE '002%' AND a.sts_update='Return' AND a.wmtid='" + wmtid + "' ");

                        var list2 = new InitMethods().ConvertDataTableToList<w_material_model>(varname1);
                        return Json(new
                        {
                            result = true,
                            message = ss,
                            data = list2.ToList()
                        }, JsonRequestBehavior.AllowGet);
                    }

                    return Json(
                          new
                          {
                              result = false,
                              message = "Số lượng đã vượt quá số lượng cuộn đầu!!!",
                          }, JsonRequestBehavior.AllowGet);
                }

            }

            return Json(
                  new
                  {
                      result = false,
                      message = exs,
                  }, JsonRequestBehavior.AllowGet);
        }

        //[HttpPost]
        public ActionResult printQRConfirm(string id)
        {
            try
            {
                var list = new ArrayList();
                var a2 = id.TrimStart('[').TrimEnd(']').Split(',');
                for (int i = 0; i < a2.Length; i++)
                {
                    var id2 = int.Parse(a2[i]);

                    var flag = db.w_material_info.Where(x => x.wmtid == id2 && x.mt_sts_cd == "001").Count();

                    if (flag == 1)
                    {
                        continue;
                    }

                    w_material_info db_w_material = db.w_material_info.Find(id2);

                    //var gr_qty_tmp = db_w_material.gr_qty;
                    DateTime dt = DateTime.Now;
                    //db_w_material.chg_dt = dt;
                    if (db_w_material.mt_type != "CMT")
                    {
                        db_w_material.id_actual = 0;
                    }
                    db_w_material.mt_sts_cd = "001";
                    db.Entry(db_w_material).State = EntityState.Modified;

                    db.SaveChanges();
                }
                StringBuilder varname1 = new StringBuilder();
                varname1.Append("SELECT a.wmtid,a.mt_cd,b.mt_nm, ");
                varname1.Append("CONCAT(ifnull(a.gr_qty,''),ifnull(b.unit_cd,'')) lenght, ");
                varname1.Append("ifnull(a.gr_qty,'') lenght1, ");
                varname1.Append("CONCAT(ifnull(b.width,0),'*',ifnull(a.gr_qty,0)) AS size, ");
                varname1.Append("ifnull(b.spec,0) spec,a.mt_no,  CONCAT((case when b.bundle_unit ='Roll' then  (a.gr_qty/b.spec) ELSE a.gr_qty END),' ',b.bundle_unit) qty, ");
                varname1.Append("b.bundle_unit, a.return_date, ");
                varname1.Append(" (SELECT dt_nm FROM comm_dt WHERE comm_dt.dt_cd=a.mt_sts_cd AND comm_dt.mt_cd='WHS005') sts_nm , ");
                varname1.Append(" (SELECT w_actual_primary.product  FROM   w_actual JOIN w_actual_primary ON w_actual.at_no=w_actual_primary.at_no WHERE  a.id_actual = w_actual.id_actual LIMIT 1) product, ");
                varname1.Append("(SELECT name FROM w_actual WHERE a.id_actual=w_actual.id_actual LIMIT 1) AS name ");
                varname1.Append(" FROM w_material_info a LEFT JOIN d_material_info  b ON a.mt_no=b.mt_no ");
                varname1.Append("WHERE a.wmtid IN (" + id + ")  ");
                var list2 = new InitMethods().ConvertDataTableToList<w_material_model>(varname1);
                return Json(new
                {
                    result = true,
                    message = ss,
                    data = list2.ToList()
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(
                new
                {
                    result = false,
                    message = exs
                }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetMemo(Pageing paging)
        {
            string MTCode = Request["MTCode"] == null ? "" : Request["MTCode"].Trim();
            string memoProductCode = Request["memoProductCode"] == null ? "" : Request["memoProductCode"].Trim();
            StringBuilder sql = new StringBuilder();
            sql.Append($" SELECT ")
                .Append($" MAX(a.mt_cd) AS mt_cd ")
                .Append($" , MAX(a.width) AS width ")
                .Append($" , MAX(a.spec) AS spec ")
                .Append($" , SUM(a.TX) AS total_roll ")
                .Append($" , MAX(a.chg_dt) AS chg_dt ")
                .Append($" FROM w_material_info_memo a ")
                .Append($" WHERE a.mt_cd LIKE '%{MTCode}%' ")
                .Append($" AND a.style_no LIKE '%{memoProductCode}%' ")
                .Append($" GROUP BY a.width, a.spec,  a.mt_cd ")
                .Append($" ; ")
                ;
            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);

            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<DateTime>("chg_dt"));
            return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
        }

        public JsonResult GetMemoDetail(Pageing paging)
        {
            string mtCodeMemo = Request["mtCodeMemo"] == null ? "" : Request["mtCodeMemo"].Trim();
            string memoWidth = Request["memoWidth"] == null ? "" : Request["memoWidth"].Trim();
            string memoSpec = Request["memoSpec"] == null ? "" : Request["memoSpec"].Trim();
            string productCodeMemo = Request["productCodeMemo"] == null ? "" : Request["productCodeMemo"].Trim();
            StringBuilder sql = new StringBuilder();
            sql.Append($" SELECT a.id, a.md_cd, a.style_no, a.style_nm, a.width, a.width_unit, a.spec, a.spec_unit, a.TX, a.receiving_dt")
                .Append($" FROM w_material_info_memo a ")
                .Append($" WHERE a.mt_cd LIKE '%{mtCodeMemo}%' ")
                .Append($" AND a.width LIKE '%{memoWidth}%' ")
                .Append($" AND a.spec LIKE '%{memoSpec}%' ")
                .Append($" AND a.style_no LIKE '%{productCodeMemo}%' ")
                .Append($" ; ")
                ;
            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);

            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("id"));
            return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
        }

        #endregion PrintQr

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        #endregion General

        #region Check_inventory
        public ActionResult Check_inventory()
        {
            return View();
        }
        public JsonResult GetCheckInvenSchedule(Pageing pageing, string vn_nm, string start, string end)
        {
            try
            {
                StringBuilder sql = new StringBuilder($"SELECT * from  w_vt_mt as a ");
                sql.Append(" where   ('" + vn_nm + "'='' or a.vn_nm LIKE '%" + vn_nm + "%')")
                .Append(" AND ('" + start + "'='' OR DATE_FORMAT(a.start_dt,'%Y/%m/%d') >= DATE_FORMAT('" + start + "','%Y/%m/%d') )")
                .Append(" AND ('" + end + "'='' OR DATE_FORMAT(a.end_dt,'%Y/%m/%d') <= DATE_FORMAT('" + end + "','%Y/%m/%d') )");
                DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);

                int total = dt.Rows.Count;
                var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("vno"));
                return new InitMethods().ReturnJsonResultWithPaging(pageing, total, result);
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public ActionResult insertSchedule(w_vt_mt w_vt_mt, string vn_cd)
        {
            try
            {
                String dateChString = DateTime.Now.ToString("yyMMdd");

                var bien_1 = "IV" + dateChString;
                var check_con = db.w_vt_mt.Where(x => x.vn_cd.StartsWith(bien_1)).ToList();
                if (check_con.Count == 0)
                {
                    w_vt_mt.vn_cd = "IV" + dateChString + "001";
                }
                else
                {
                    var menuCd = string.Empty;
                    var subMenuIdConvert = 0;
                    var list1 = check_con.OrderBy(x => x.vn_cd).LastOrDefault();
                    var bien1 = list1.vn_cd;
                    var subMenuId = bien1.Substring(bien1.Length - 3, 3);
                    int.TryParse(subMenuId, out subMenuIdConvert);
                    menuCd = bien_1 + string.Format("{0}{1}", menuCd, CreateIV((subMenuIdConvert + 1)));
                    w_vt_mt.vn_cd = menuCd;
                }

                w_vt_mt.use_yn = "Y";
                w_vt_mt.del_yn = "N";
                w_vt_mt.reg_dt = DateTime.Now;
                w_vt_mt.chg_dt = DateTime.Now;
                w_vt_mt.reg_id = Session["authName"] == null ? null : Session["authName"].ToString();

                if (ModelState.IsValid)
                {
                    db.Entry(w_vt_mt).State = EntityState.Added;
                    db.SaveChanges(); // line that threw exception
                }
                return Json(new { result = true, kq = w_vt_mt, message = "Thành công!!!" }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
            }
        }
        private string CreateIV(int id)
        {
            if (id.ToString().Length < 2)
            {
                return string.Format("00{0}", id);
            }

            if (id.ToString().Length < 3)
            {
                return string.Format("0{0}", id);
            }

            if (id.ToString().Length < 4)
            {
                return string.Format("{0}", id);
            }

            return string.Empty;
        }
        public ActionResult updateSchedule(int vno, string vn_nm, string start_dt, string end_dt, string re_mark, string use_yn)
        {
            try
            {
                w_vt_mt w_vt_mt = db.w_vt_mt.Find(vno);
                w_vt_mt.vn_nm = vn_nm;
                w_vt_mt.start_dt = start_dt;
                w_vt_mt.end_dt = end_dt;
                w_vt_mt.re_mark = re_mark;
                w_vt_mt.use_yn = "Y";
                w_vt_mt.del_yn = "N";

                w_vt_mt.chg_id = Session["authName"] == null ? null : Session["authName"].ToString();
                w_vt_mt.chg_dt = DateTime.Now;
                if (ModelState.IsValid)
                {
                    db.Entry(w_vt_mt).State = EntityState.Modified;
                    db.SaveChanges();
                    return Json(new { result = w_vt_mt }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }

        }

        public JsonResult GetInventorySX(Pageing paging)
        {
            string mt_no = Request["mt_no"] == null ? "" : Request["mt_no"].Trim();
            string mt_nm = Request["mt_nm"] == null ? "" : Request["mt_nm"].Trim();
            string recevice_dt_start = Request["recevice_dt_start"] == null ? "" : Request["recevice_dt_start"].Trim();
            string recevice_dt_end = Request["recevice_dt_end"] == null ? "" : Request["recevice_dt_end"].Trim();
            string sts = Request["sts"] == null ? "" : Request["sts"].Trim();
            string vd_cd = Request["vd_cd"] == null ? "" : Request["vd_cd"].Trim();
            var dateConvert = new DateTime();

            if (DateTime.TryParse(recevice_dt_end, out dateConvert))
            {
                recevice_dt_end = dateConvert.ToString("yyyy/MM/dd");
            }
            if (DateTime.TryParse(recevice_dt_start, out dateConvert))
            {
                recevice_dt_start = dateConvert.ToString("yyyy/MM/dd");
            }
            StringBuilder varname1 = new StringBuilder();
            varname1.Append("SELECT a.wmtid,a.mt_cd,b.mt_nm,CONCAT(ifnull(a.gr_qty,''),ifnull(b.unit_cd,'')) lenght,CONCAT(ifnull(b.width,0),'*',ifnull(a.gr_qty,0)) AS size,ifnull(b.spec,0) spec,a.mt_no, ");
            varname1.Append(" (case when b.bundle_unit ='Roll' then  ROUND((a.gr_qty/b.spec),2) ELSE ROUND(a.gr_qty,2) END) qty, b.bundle_unit, ");
            varname1.Append("a.recevice_dt, ");
            varname1.Append("(SELECT dt_nm FROM comm_dt WHERE comm_dt.dt_cd=a.mt_sts_cd AND comm_dt.mt_cd='WHS005') sts_nm ");
            varname1.Append("FROM w_material_info a ");
            varname1.Append("LEFT JOIN d_material_info  b ON a.mt_no=b.mt_no ");
            varname1.Append("WHERE a.lct_cd LIKE '002%'  and a.mt_type<> 'CMT' AND a.mt_sts_cd not in ('005','013')  AND a.mt_cd NOT IN (SELECT mt_cd FROM w_vt_dt WHERE vn_cd='" + vd_cd + "') ");
            varname1.Append(" AND ('" + mt_nm + "'='' OR  b.mt_nm like '%" + mt_nm + "%' ) ");
            varname1.Append(" AND ('" + recevice_dt_start + "'='' OR DATE_FORMAT(a.recevice_dt,'%Y/%m/%d') >= DATE_FORMAT('" + recevice_dt_start + "','%Y/%m/%d')) ");
            varname1.Append(" AND ('" + recevice_dt_end + "'='' OR DATE_FORMAT(a.recevice_dt,'%Y/%m/%d') <= DATE_FORMAT('" + recevice_dt_end + "','%Y/%m/%d')) ");

            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("wmtid"));
            return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
        }
        public ActionResult GetPickingScanMLQR(string ml_cd, string vn_cd)
        {
            try
            {
                if (string.IsNullOrEmpty(ml_cd))
                {
                    return Json(new { result = false, message = "Làm ơn scan lại!!" }, JsonRequestBehavior.AllowGet);
                }

                var kttt_null = db.w_material_info.Where(x => x.mt_cd == ml_cd).SingleOrDefault();
                if (kttt_null == null)
                {
                    return Json(new { result = false, message = "Mã Này Không có trong tồn kho !!!" }, JsonRequestBehavior.AllowGet);
                }

                if (!kttt_null.lct_cd.StartsWith("002"))
                {
                    var vitri = checkvitri(kttt_null.lct_cd);
                    return Json(new { result = false, message = "Mã Này đang ở kho " + vitri + " !!!" }, JsonRequestBehavior.AllowGet);
                }

                if (kttt_null.mt_type == "CMT")
                {
                    return Json(new { result = false, message = "Mã Này là mã bán thành phẩm!!!" }, JsonRequestBehavior.AllowGet);
                }

                if (kttt_null.mt_sts_cd != "001")
                {
                    var trangthai = checktrangthai(kttt_null.mt_sts_cd);
                    return Json(new { result = false, message = "Trạng Thái mã này đang là " + trangthai + " Không phải tồn kho" }, JsonRequestBehavior.AllowGet);
                }
                if (db.w_vt_dt.Any(x => x.mt_cd == ml_cd && x.vn_cd == vn_cd))
                {
                    return Json(new { result = false, message = "Mã này đã được vào danh sách " + vn_cd + " rồi !!!" }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = true, message = "Thành công!!!", data = kttt_null }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult InsertMTQRIVList(string data, string vn_cd)
        {
            try
            {
                //UPDATE w_material_info_tam  COLUMN SD
                var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var people = Session["authName"] == null ? null : Session["authName"].ToString();

                StringBuilder varname1 = new StringBuilder();
                varname1.Append("INSERT INTO `w_vt_dt` ( `vn_cd`, `mt_cd`, `wmtid`, `id_actual`, `id_actual_oqc`, `staff_id`, \n");
                varname1.Append(" `staff_id_oqc`, `machine_id`, `mt_type`, `mt_no`, `gr_qty`, `real_qty`, `staff_qty`, `sp_cd`, `rd_no`, \n");
                varname1.Append(" `sd_no`, `ext_no`, `ex_no`, `dl_no`, `recevice_dt`, `date`, `return_date`, `alert_NG`, `expiry_dt`, \n");
                varname1.Append(" `dt_of_receipt`, `expore_dt`, `recevice_dt_tims`, `rece_wip_dt`, `picking_dt`, `shipping_wip_dt`, \n");
                varname1.Append(" `end_production_dt`, `lot_no`, `mt_barcode`, `mt_qrcode`, `mt_sts_cd`, `bb_no`, `bbmp_sts_cd`, \n");
                varname1.Append(" `lct_cd`, `lct_sts_cd`, `from_lct_cd`, `to_lct_cd`, `output_dt`, `input_dt`, `buyer_qr`, `orgin_mt_cd`, `reg_id`, `reg_dt`, `chg_id`, `chg_dt`) \n");
                varname1.Append("SELECT '" + vn_cd + "',`mt_cd`, `wmtid`, `id_actual`, `id_actual_oqc`, `staff_id`, `staff_id_oqc`, `machine_id`, \n");
                varname1.Append("`mt_type`, `mt_no`, `gr_qty`, `real_qty`, `staff_qty`, `sp_cd`, `rd_no`, `sd_no`, `ext_no`, `ex_no`, \n");
                varname1.Append("`dl_no`, `recevice_dt`, `date`, `return_date`, `alert_NG`, `expiry_dt`, `dt_of_receipt`, `expore_dt`, \n");
                varname1.Append(" `recevice_dt_tims`, `rece_wip_dt`, `picking_dt`, `shipping_wip_dt`, `end_production_dt`, `lot_no`, \n");
                varname1.Append("  `mt_barcode`, `mt_qrcode`, `mt_sts_cd`, `bb_no`, `bbmp_sts_cd`, `lct_cd`, `lct_sts_cd`, `from_lct_cd`, \n");
                varname1.Append("   `to_lct_cd`, `output_dt`, `input_dt`, `buyer_qr`, `orgin_mt_cd`,'" + people + "',NOW(),'" + people + "',NOW() \n");
                varname1.Append("FROM w_material_info \n");
                varname1.Append("WHERE wmtid IN (" + data + ");");
                int effect_rows = new Excute_query().Execute_NonQuery(varname1);

                return Json(new { result = true, message = ss }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }

        }


        public string checkvitri(string lct_Cd)
        {
            var checkvitri = db.lct_info.Where(x => x.lct_cd == lct_Cd).ToList();
            var csv = string.Join(", ", checkvitri.Select(x => x.lct_nm));
            return csv;
        }
        public string checktrangthai(string mt_sts_cd)
        {
            var check_trangthai = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == mt_sts_cd).ToList();
            var csv = string.Join(", ", check_trangthai.Select(x => x.dt_nm));

            return csv;
        }
        public string checkcondoan(string name)
        {
            var check_trangthai = db.comm_dt.Where(x => x.mt_cd == "COM007" && x.dt_cd == name).ToList();
            var csv = string.Join(", ", check_trangthai.Select(x => x.dt_nm));

            return csv;
        }
        #endregion
    }
}
using ClosedXML.Excel;
using Microsoft.AspNet.SignalR.Client;
using Mvc_VD.Models;
using Mvc_VD.Models.Language;
using Mvc_VD.Models.WIP;
using Mvc_VD.Models.WOModel;
using Mvc_VD.Services;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Ninject.Activation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Mvc_VD.Controllers
{
    public class ExportToMachineController : BaseController
    {
        private readonly IWIPService _IWIPService;
        private Entities db;


        public ExportToMachineController(
             IWIPService IWIPService,
           IDbFactory DbFactory)
        {

            _IWIPService = IWIPService;
            db = DbFactory.Init();

        }
        #region danh_sach_bang_xuat_lieu_cho_may
        public ActionResult ExportToMachineScan()
        {

            return SetLanguage("~/Views/wipwms/ExportToMachine/ExportToMachineScan.cshtml");
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
        public ActionResult listExportMaterial(Pageing pageing, string ExportCode, string ExportName, string ProductCode, string ProductName, string Description,bool IsFinish)
        {
            Dictionary<string, string> list = PagingAndOrderBy(pageing, " ORDER BY MyDerivedTable.ChangDate DESC ");
            int totalRecords = _IWIPService.TotalListExportToMachine(ExportCode, ExportName, ProductCode, ProductName, Description,false);
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

            IEnumerable<ExportToMachineModel> Data = _IWIPService.GetListExportToMachine(ExportCode, ExportName, ProductCode, ProductName, Description, IsFinish);

            var result = new
            {
                total = totalPages,
                page = int.Parse(list["index"]),
                records = totalRecords,
                rows = Data
            };
            return Json(result, JsonRequestBehavior.AllowGet);

        }
        public ActionResult searchExportMaterial(Pageing pageing, string ExportCode, string ExportName,string ProductCode,string ProductName, string Description)
        {

            Dictionary<string, string> list = PagingAndOrderBy(pageing, " ORDER BY MyDerivedTable.ExportCode DESC ");



            int totalRecords = _IWIPService.TotalRecordsSearchExportToMachine(ExportCode, ExportName, ProductCode, ProductName, Description);
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

            IEnumerable<ExportToMachineModel> Data = _IWIPService.GetListSearchExportToMachine(ExportCode, ExportName, ProductCode, ProductName, Description);

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
        public JsonResult InsertExporttomachine(ExportToMachineModel item)
        {
            try
            {
                #region Tang tự động
                String ExportCode = "EP1";

                var Exportlast = _IWIPService.GetListExportToMachine().FirstOrDefault();

                if (Exportlast != null)
                {
                     ExportCode = Exportlast.ExportCode;
                    ExportCode = string.Concat("EP", (int.Parse(ExportCode.Substring(2)) + 1).ToString());
                }
                #endregion
                item.ExportCode = ExportCode;
                item.IsFinish = "N";
                item.CreateId = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                item.CreateDate = DateTime.Now;
                item.ChangeId = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                item.ChangeDate = DateTime.Now;

                int list = _IWIPService.InsertToExportToMachine(item);
                if (list > 0)
                {
                    IEnumerable<ExportToMachineModel> Data = _IWIPService.GetListSearchExportToMachine(ExportCode,"", "",  "", "");
                    return Json(new { result = true, message = "Thành công!!!", data = Data }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = "Tạo thất bại"}, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Tạo thất bại" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult ModifyExporttomachine(ExportToMachineModel item)
        {
            try
            {
                item.ChangeId = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                item.ChangeDate = DateTime.Now;

              _IWIPService.ModifyToExportToMachine(item);
               
                IEnumerable<ExportToMachineModel> Data = _IWIPService.GetListSearchExportToMachine(item.ExportCode, "", "", "", "");
                return Json(new { result = true, message = "Thành công!!!", data = Data }, JsonRequestBehavior.AllowGet);
                
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Sửa thất bại" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult DeleteExporttomachine(int id,string ExportCode)
        {
            try
            {
                if (string.IsNullOrEmpty(ExportCode))
                {
                    return Json(new { result = false, message = "Vui lòng Chọn một phiếu xuất để xóa" }, JsonRequestBehavior.AllowGet);
                }
                //kiểm tra xem ep này đã xuất bất kì liệu nào ra máy chưa/ nếu chưa mới cho xóa
                if (!_IWIPService.CheckMaterialEP(ExportCode))
                {
                    return Json(new { result = false, message = "Phiếu này đã có Nguyên Vật Liệu xuất, Bạn không thể xóa" }, JsonRequestBehavior.AllowGet);
                }
                _IWIPService.DeleteToExportToMachine(id);

             
                return Json(new { result = true, message = "Thành công!!!"}, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Sửa thất bại" }, JsonRequestBehavior.AllowGet);
            }
        }
        //them chuc nang finish
        [Route("ExportToMachine/FinishExtoMachine")]
        public JsonResult FinishExtoMachine(ExportToMachineModel item)
        {
            try
            {
                item.ChangeId = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                item.ChangeDate = DateTime.Now;

                _IWIPService.FinishExportToMachine(item);
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Json(new { message = "My error message"});

            }
            catch (Exception e)
            {
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Json(new { result = false, message = "Sửa thất bại", ErrorMessage = e.ToString() }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult GetExport_ScanMLQR_WIP(string materialCode)
        {
            try
            {
                //Check input
                if (string.IsNullOrEmpty(materialCode))
                {
                    return Json(new { result = false, message = "Mã Nguyên vật liệu trống, Vui lòng Scan lại" }, JsonRequestBehavior.AllowGet);
                }
                materialCode = materialCode.Trim();
                //Kiểm tra mã materialCode có tồn tại ở kho NVL không
                var IsWmaterial = _IWIPService.CheckWMaterialInfo(materialCode);
                //kiểm tra xem mã khác 002 là kho khác
                if (IsWmaterial != null)
                {
                    if (!IsWmaterial.lct_cd.StartsWith("002"))
                    {
                        var VITRI = checkvitri(IsWmaterial.lct_cd);
                        return Json(new { result = false, message = "Mã Này Đang ở " + VITRI }, JsonRequestBehavior.AllowGet);
                    }
                    //kiểm tra mã này có trạng thái gì, nếu != 001 thì return
                    if (IsWmaterial.mt_sts_cd != "001")
                    {
                        var VITRI = checktrangthai(IsWmaterial.mt_sts_cd);
                        return Json(new { result = false, message = "Mã Này Đang " + VITRI }, JsonRequestBehavior.AllowGet);
                    }
                    //kiểm tra mã này đã đưa lên kệ nào chưa
                    if (!string.IsNullOrEmpty(IsWmaterial.ExportCode) && !string.IsNullOrEmpty(IsWmaterial.LoctionMachine))
                    {
                        //var VITRI = checkvitri(IsWmaterial.lct_cd);
                        return Json(new { result = false, message = "Mã này đang ở MÁY, PHIẾU XUẤT "+ IsWmaterial.ExportCode }, JsonRequestBehavior.AllowGet);
                    }
                   
                    return Json(new { result = true, message = "Thành công", data = IsWmaterial }, JsonRequestBehavior.AllowGet);
                }

                {
                    return Json(new { result = false, message ="Mã bạn vừa quét là: "+ materialCode +  "<br/> NVL này chưa được nhập kho sản xuất!!!" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Sửa thất bại" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult UpdateMaterialToMachine(string ExportCode, string MachineCode,string ListId)
        {
            try
            {
                //Check input
                if (string.IsNullOrEmpty(ExportCode))
                {
                    return Json(new { result = false, message ="Vui lòng Chọn một phiếu xuất" }, JsonRequestBehavior.AllowGet);
                }
                if (string.IsNullOrEmpty(ListId))
                {
                    return Json(new { result = false, message = "Mã Nguyên vật liệu trống, Vui lòng Scan lại" }, JsonRequestBehavior.AllowGet);
                }
                ListId = ListId.Trim();


                //update kệ và máy cho mã nguyên vật liệu này

                var IsWmaterial = new WMaterialInfoNew();
                IsWmaterial.ExportCode = ExportCode;
                IsWmaterial.LoctionMachine = MachineCode;
                IsWmaterial.ShippingToMachineDatetime = DateTime.Now;

                _IWIPService.UpdateMaterialToMachine(IsWmaterial, ListId);

                return Json(new { result = true, message = "Thành công" }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống" }, JsonRequestBehavior.AllowGet);
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
            var checktrangthai = db.comm_dt.Where(x =>x.mt_cd == "WHS005" && x.dt_cd == mt_sts_cd).ToList();
            var csv = string.Join(", ", checktrangthai.Select(x => x.dt_nm));
            return csv;
        }
        #endregion
        #region PartialViewExportToMachinePP
        public ActionResult PartialViewExportToMachinePP(string ExportCode,bool edit)
        {
            ViewBag.Deleted = edit;
            ViewBag.ExportCode = ExportCode;
            return PartialView("~/Views/wipwms/ExportToMachine/PartialViewExportToMachinePP.cshtml");
        }
        public ActionResult GetShippingtoMachine(string ExportCode)
        {
            var listdata = _IWIPService.GetListSearchExportToMachine(ExportCode, "", "", "", "");

            return Json(new { data = listdata }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetListExportToMachine(string ExportCode)
        {
            var listdata = _IWIPService.GetListExportToMachine(ExportCode);

            return Json(new { data = listdata }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetShippingScanPP_Count_MT_no(string ExportCode)
        {
            try
            {
                StringBuilder sql = new StringBuilder($"CALL GetExportScanPP_Count_MT_no('{ExportCode}');");
                var list2 = new InitMethods().ConvertDataTableToJsonAndReturn(sql);
                return Json(new { data = list2.Data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }

        }

        public ActionResult PrintExport_LIST(string ExportCode)
        {
            ViewData["Message"] = ExportCode;
            return PartialView("~/Views/wipwms/ExportToMachine/PrintExport_LIST.cshtml");
        }
        public ActionResult GetShippingtoMachinePP(string ExportCode)
        {
            var listdata = _IWIPService.GetListSearchExportToMachinePP(ExportCode);

            return Json(new { data = listdata }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult DeleteRecortEP(string mt_cd)
        {
            try
            {
                //Check input
                if (string.IsNullOrEmpty(mt_cd))
                {
                    return Json(new { result = false, message = "Vui lòng chọn mã cần xóa" }, JsonRequestBehavior.AllowGet);
                }

                mt_cd = mt_cd.Trim();
              
                //Kiểm tra mã materialCode có tồn tại ở kho NVL không
                var IsWmaterial = _IWIPService.CheckWMaterialInfo(mt_cd);
                //kiểm tra xem mã khác 002 là kho khác
                if (IsWmaterial != null)
                {
                    if (IsWmaterial.mt_sts_cd.Equals("001"))
                    {
                        //xóa liệu này ra khỏi phiếu xuất
                        IsWmaterial.ExportCode = "";
                        IsWmaterial.LoctionMachine = "";
                        IsWmaterial.ShippingToMachineDatetime = null;
                        IsWmaterial.chg_dt = DateTime.Now;
                        IsWmaterial.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();

                        _IWIPService.UpdateMaterialToWIP(IsWmaterial);
                        return Json(new { result = true, message = "Thành công" }, JsonRequestBehavior.AllowGet);
                    }
                    var VITRI = checktrangthai(IsWmaterial.mt_sts_cd);
                    return Json(new { result = false, message = "Mã Này Đang " + VITRI }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = "Lỗi hệ thống" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống" }, JsonRequestBehavior.AllowGet);
            }
        }
       
        #endregion

        #region FActory_location
        public ActionResult Factory_Location()
        {
            return SetLanguage("~/Views/wipwms/FactionWip/Factory_Location.cshtml");
        }
        public ActionResult ViewLoactionDetail()
        {
            string[] keys = Request.Form.AllKeys;
            if (keys.Length >= 1)
            {
                var value = Request.Form[keys[0]];
                var LocationName = checkvitri(value);

                ViewBag.lct_cd = (value == "undefined" ? "" : value);
                ViewBag.LocationName = (LocationName == "undefined" ? "" : LocationName);
                
            }
            else
            {
                ViewBag.Error = "Không có dữ liệu được gửi lên";
            }
            return View("~/Views/wipwms/FactionWip/ViewLocationDetail.cshtml");
        }
        public ActionResult DetailRackWIP(string lct_cd)
        {
            try
            {
                //kiểm tra kệ này có tồn tại hay không
                var isExistLocation = _IWIPService.CheckIsExistLocation(lct_cd);
                if (isExistLocation == null)
                {
                    return Json(new { result = false, message = "Kệ này không tồn tại" }, JsonRequestBehavior.AllowGet);
                }
                //nếu level = 000 thì search like 002 
                if (isExistLocation.level_cd.Equals("000"))
                {
                    lct_cd = "002";
                }
               //kiểm tra location có rỗng ko và nếu level = 002 thì search bằng chính nó, ngược lại search like
                if (!string.IsNullOrEmpty(lct_cd) && isExistLocation.level_cd.Equals("001"))
                {
                    lct_cd = lct_cd.Substring(0, 9);
                }
                //StringBuilder varname1 = new StringBuilder();
                //varname1.Append(" SELECT a.wmtid,SUM(a.gr_qty) AS soMet,COUNT(a.mt_cd) AS soCuon, (SELECT md_cd from d_style_info WHERE style_no = (SELECT product_cd from w_sd_info WHERE sd_no = max(a.sd_no) ) ) AS md_cd,");
                //varname1.Append(" (SELECT product_cd from w_sd_info WHERE sd_no = max(a.sd_no)) AS product_cd, a.mt_no, a.gr_qty, lct_cd,");
                //varname1.Append(" (SELECT lct_nm from lct_info WHERE lct_cd = max(a.lct_cd)) AS lct_nm ");
                //varname1.Append(" FROM w_material_info AS a ");
                //varname1.Append(" WHERE a.lct_cd like '"+ lct_cd + "%' AND a.mt_type = 'PMT' GROUP BY a.lct_cd,a.mt_no  ORDER BY a.lct_cd");
                //var list2 = new InitMethods().ConvertDataTableToJsonAndReturn(varname1);
                var lll = lct_cd + "%";
                DateTime datetime = DateTime.Now;
             

                TimeSpan Timestart = new TimeSpan(00, 0, 0); //00 o'clock
                TimeSpan Timeend = new TimeSpan(08, 0, 0); //08 o'clock
                TimeSpan Timenow = DateTime.Now.TimeOfDay;

                if ((Timenow > Timestart) && (Timenow < Timeend))
                   datetime = datetime.AddDays(-1);

                string datenow = datetime.ToString("yyyy-MM-dd HH:mm:ss");

                StringBuilder sql = new StringBuilder($"CALL ViewDsRackWIP('{lll}','{datenow}');");
                var list2 = new InitMethods().ConvertDataTableToJsonAndReturn(sql);
                return Json(new { data = list2.Data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult SearchFactory_Location(string lct_cd = null, string lct_nm = null)
        {
            var Data = db.lct_info.Where(item => (item.lct_cd.StartsWith("002")

           && item.lct_cd.Contains(lct_cd)
           && item.lct_nm.Contains(lct_nm)
           && item.level_cd.Equals("002")

            )).OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();


            return Json(Data, JsonRequestBehavior.AllowGet);

        }

        public ActionResult GetFactory_Location()
        {
            var Data = db.lct_info.Where(item => (item.lct_cd.StartsWith("002")
           && item.level_cd.Equals("002")

            )).OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();
            return Json(Data, JsonRequestBehavior.AllowGet);


            //var sql = new StringBuilder();

            //sql.Append(@"SELECT * FROM lct_info where lct_cd like '002%' level_cd = '002'  order by lct_cd asc, level_cd asc ");



            //return new InitMethods().ConvertDataTableToJsonAndstring(sql.ToString());
        }
        #endregion
        #region Return Material from machine to Wip
        public ActionResult ReturnMaterialToWip()
        {
            return SetLanguage("~/Views/wipwms/ExportToMachine/ReturnMaterialToWip.cshtml");
        }
        public ActionResult GetReturn_ScanMLQR_WIP(string materialCode)
        {
            try
            {
                //Check input
                if (string.IsNullOrEmpty(materialCode))
                {
                    return Json(new { result = false, message = "Mã Nguyên vật liệu trống, Vui lòng Scan lại" }, JsonRequestBehavior.AllowGet);
                }
                materialCode = materialCode.Trim();
                //Kiểm tra mã materialCode có tồn tại ở kho NVL không
                var IsWmaterial = _IWIPService.CheckWMaterialInfo(materialCode);
                //kiểm tra xem mã khác 002 là kho khác
                if (IsWmaterial != null)
                {
                    if (!IsWmaterial.lct_cd.StartsWith("002"))
                    {
                        var VITRI = checkvitri(IsWmaterial.lct_cd);
                        return Json(new { result = false, message = "Mã Này Đang ở " + VITRI }, JsonRequestBehavior.AllowGet);
                    }

                    //kiểm tra mã này đã đưa lên kệ nào chưa
                    if (string.IsNullOrEmpty(IsWmaterial.ExportCode) && string.IsNullOrEmpty(IsWmaterial.LoctionMachine))
                    {
                        var VITRI = checkvitri(IsWmaterial.lct_cd);

                        return Json(new { result = false, message = "Mã này đã được đang ở " + VITRI }, JsonRequestBehavior.AllowGet);
                    }
                    //kiểm tra mã này có trạng thái gì, nếu != 001 thì return
                    if (IsWmaterial.mt_sts_cd != "001")
                    {
                        var VITRI = checktrangthai(IsWmaterial.mt_sts_cd);
                        return Json(new { result = false, message = "Mã Này Đang " + VITRI }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { result = true, message = "Thành công", data = IsWmaterial }, JsonRequestBehavior.AllowGet);
                }

                {
                    return Json(new { result = false, message = "NVL này chưa được nhập kho sản xuất!!!" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Sửa thất bại" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult UpdateReturnMaterialToMachine(string SelectRack, string ListId)
        {
            try
            {
                //Check input
                if (string.IsNullOrEmpty(SelectRack))
                {
                    return Json(new { result = false, message = "Vui lòng Chọn một kệ" }, JsonRequestBehavior.AllowGet);
                }
                if (string.IsNullOrEmpty(ListId))
                {
                    return Json(new { result = false, message = "Mã Nguyên vật liệu trống, Vui lòng Scan lại" }, JsonRequestBehavior.AllowGet);
                }
                ListId = ListId.Trim();

                //INSERT MÃ BỊ TRẢ TỪ KHO MÁY VỀ LẠI KHO SẢN XUẤT

                _IWIPService.InsertReturnMaterialToWIP(ListId);


                //update kệ và máy cho mã nguyên vật liệu này
                var datetime  = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var IsWmaterial = new WMaterialInfoNew();
                IsWmaterial.lct_cd = SelectRack;
                IsWmaterial.ExportCode = "";
                IsWmaterial.LoctionMachine = "";
                IsWmaterial.ShippingToMachineDatetime = null;
                //IsWmaterial.picking_dt = datetime;
                //IsWmaterial.sts_update = "Máy trả về kho" ; 
                IsWmaterial.chg_dt = DateTime.Now;
                IsWmaterial.chg_id = Session["userid"] == null ? null : Session["userid"].ToString() ;

                _IWIPService.UpdateReturnMaterialToWIP(IsWmaterial, ListId);

                return Json(new { result = true, message = "Thành công" }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống" }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion
        #region ChangeRack
        public ActionResult ChangeRack()
        {
            return SetLanguage("~/Views/wipwms/ExportToMachine/ChangeRack.cshtml");
        }
        public ActionResult GetRack_ScanMLQR_WIP(string materialCode)
        {
            try
            {
                //Check input
                if (string.IsNullOrEmpty(materialCode))
                {
                    return Json(new { result = false, message = "Mã Nguyên vật liệu trống, Vui lòng Scan lại" }, JsonRequestBehavior.AllowGet);
                }
                materialCode = materialCode.Trim();
                //Kiểm tra mã materialCode có tồn tại ở kho NVL không
                var IsWmaterial = _IWIPService.CheckWMaterialInfo(materialCode);
                //kiểm tra xem mã khác 002 là kho khác
                if (IsWmaterial != null)
                {
                    if (!IsWmaterial.lct_cd.StartsWith("002"))
                    {
                        var VITRI = checkvitri(IsWmaterial.lct_cd);
                        return Json(new { result = false, message = "Mã Này Đang ở " + VITRI }, JsonRequestBehavior.AllowGet);
                    }
                    //kiểm tra mã này có trạng thái gì, nếu != 001 thì return
                    if (IsWmaterial.mt_sts_cd != "001")
                    {
                        var VITRI = checktrangthai(IsWmaterial.mt_sts_cd);
                        return Json(new { result = false, message = "Mã Này Đang " + VITRI }, JsonRequestBehavior.AllowGet);
                    }
                    //kiểm tra mã này đã đưa lên kệ nào chưa
                    if (!string.IsNullOrEmpty(IsWmaterial.ExportCode) && !string.IsNullOrEmpty(IsWmaterial.LoctionMachine))
                    {
                        var VITRI = IsWmaterial.LoctionMachine;

                        return Json(new { result = false, message = "Mã này đã ở Máy: " + VITRI }, JsonRequestBehavior.AllowGet);
                    }
                    
                    return Json(new { result = true, message = "Thành công", data = IsWmaterial }, JsonRequestBehavior.AllowGet);
                }
                {
                    return Json(new { result = false, message = "NVL này chưa được nhập kho sản xuất!!!" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Sửa thất bại" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult UpdateChangeRackMaterialToMachine(string SelectRack, string ListId)
        {
            try
            {
                //Check input
                if (string.IsNullOrEmpty(SelectRack))
                {
                    return Json(new { result = false, message = "Vui lòng Chọn một kệ" }, JsonRequestBehavior.AllowGet);
                }
                if (string.IsNullOrEmpty(ListId))
                {
                    return Json(new { result = false, message = "Mã Nguyên vật liệu trống, Vui lòng Scan lại" }, JsonRequestBehavior.AllowGet);
                }
                ListId = ListId.Trim();


                //update kệ và máy cho mã nguyên vật liệu này
                var datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var IsWmaterial = new WMaterialInfoNew();
                IsWmaterial.lct_cd = SelectRack;
                //IsWmaterial.rece_wip_dt = datetime;
                //IsWmaterial.sts_update = "Đổi kệ";
                IsWmaterial.chg_dt = DateTime.Now;
                IsWmaterial.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();

                _IWIPService.UpdateChangeRackMaterialToMachine(IsWmaterial, ListId);

                return Json(new { result = true, message = "Thành công" }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống" }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion
        #region viewRack
        public ActionResult GetLocation(string lct_cd)
        {
            try
            {
                var listdata = _IWIPService.GetLocationWIP(lct_cd);

                return Json(listdata, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {

                return Json(new { result = false, message = "Lỗi hệ thống" }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion
        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}

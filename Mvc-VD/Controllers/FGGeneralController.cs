using ClosedXML.Excel;
using Mvc_VD.Models;
using Mvc_VD.Models.FG;
using Mvc_VD.Models.Language;
using Mvc_VD.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Mvc_VD.Controllers
{
    public class FGWmsGeneralController : BaseController
    {

        private readonly Entities db;
        private readonly IFGWmsService _iFGWmsService;
        private readonly ITIMSService _TIMSService;
        #region Constructor
        public FGWmsGeneralController(IFGWmsService iFGWmsService, ITIMSService ITIMSService, IDbFactory DbFactory)
        {
            _iFGWmsService = iFGWmsService;
            _TIMSService = ITIMSService;
            db = DbFactory.Init();
        }
        #endregion
        public ActionResult General()
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
        public JsonResult getFGGeneral(Pageing paging)
        {
            string buyerCode = Request["buyerCode"] == null ? "" : Request["buyerCode"].Trim();
            string productCode = Request["productCode"] == null ? "" : Request["productCode"].Trim();
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

            StringBuilder sql = new StringBuilder();

            StringBuilder varname1 = new StringBuilder();
            varname1.Append(" SELECT MAX(a.id) AS id, MAX(a.product_code) AS product_code, MAX(a.md_cd) AS md_cd, SUM(a.qty) AS qty, max(a.reg_dt) as reg_dt  ");
            varname1.Append("FROM generalfg as a where a.sts_cd in ('001','010') ");
            varname1.Append(" AND a.product_code LIKE CONCAT('%','"+ productCode + "','%')");
            varname1.Append(" AND a.buyer_qr LIKE CONCAT('%','"+ buyerCode + "' , '%')");

            varname1.Append(" AND ('" + recevice_dt_start + "'='' OR DATE_FORMAT(a.reg_dt,'%Y/%m/%d') >= DATE_FORMAT('" + recevice_dt_start + "','%Y/%m/%d')) ");
            varname1.Append(" AND ('" + recevice_dt_end + "'='' OR DATE_FORMAT(a.reg_dt,'%Y/%m/%d') <= DATE_FORMAT('" + recevice_dt_end + "','%Y/%m/%d')) ");
            varname1.Append(" group by a.product_code ");
     
         
            //return new InitMethods().ConvertDataTableToJsonAndReturn(varname1);

            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<string>("product_code"));
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
                var id_FG = Convert.ToInt32(id);
                var productCode = db.generalfgs.Find(id_FG).product_code;

                StringBuilder varname1 = new StringBuilder();
                varname1.Append("SELECT a.*,(SELECT dt_nm FROM comm_dt WHERE comm_dt.mt_cd ='WHS005' AND comm_dt.dt_cd = a.sts_cd) AS statusName ");
                varname1.Append("FROM generalfg as a ");
                varname1.Append(" where a.product_code = '" + productCode + "' ");
                varname1.Append(" AND a.buyer_qr LIKE CONCAT('%','" + buyerCode + "' , '%')");
                varname1.Append(" AND a.sts_cd in ('001','010') ");
                varname1.Append("  AND case ");
                varname1.Append("    when('"+ recevice_dt_start + "' <> '00010101' AND a.reg_dt IS not NULL) then DATE_FORMAT(a.reg_dt, '%Y%m%d') >= STR_TO_DATE('" + recevice_dt_start + "', '%Y%m%d')");
                varname1.Append("        when a.reg_dt IS NULL then 1 = 0");
                varname1.Append("        ELSE '' = ''");
                varname1.Append("        end                         AND ");
                varname1.Append("    case ");
                varname1.Append("   when('" + recevice_dt_end + "' <> '99991231' AND a.reg_dt IS not NULL) then DATE_FORMAT(a.reg_dt, '%Y%m%d') <= STR_TO_DATE('" + recevice_dt_end + "', '%Y%m%d')");
                varname1.Append("       when a.reg_dt IS NULL then 1 = 0");
                varname1.Append("       ELSE '' = '' end");


                //StringBuilder sql = new StringBuilder($"CALL spFGWMMS_GetFGgeneralDetail('{productCode}', '{poCode}', '{recevice_dt_start}', '{recevice_dt_end}', '{buyerCode}');");

                DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

                int total = dt.Rows.Count;
                var result = dt.AsEnumerable().OrderByDescending(x => x.Field<DateTime>("reg_dt"));
                return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public ActionResult updateQuantity(int? buyerQtuantity, int? id)
        {
            try
            {
                if (buyerQtuantity > 0 && id > 0)
                {
                    //var userAcount = Session["userid"] == null ? null : Session["userid"].ToString();
                    //var userWIP = "";
                    //if (!string.IsNullOrEmpty(userAcount))
                    //{
                    //    var dsMbInfo = _iFGWmsService.GetMbInfoGrade(userAcount);
                    //    userWIP = dsMbInfo.grade;
                    //}
                    //if (userWIP != "Admin")
                    //{
                    //    return Json(new { result = false, message = "Tài khoản thuộc quyền Admin mới được chỉnh sửa", id = id }, JsonRequestBehavior.AllowGet);
                    //}
                    generalfg item = new generalfg();
                    item.id = (int)id;
                    item.qty = (int)buyerQtuantity;

                    item.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    item.chg_dt = DateTime.Now;
                    _iFGWmsService.UpdateQtyGeneral(item);

                    return Json(new { result = true,message ="Cập nhập số lượng thành công"}, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, kq = "Cập nhập số lượng lỗi" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, kq = e }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult CheckStatusBuyerToSAP(string buyerCode)
        {
            try
            {
                if (string.IsNullOrEmpty(buyerCode))
                {
                    return Json(new { flag = false, message = "Scan Tem gói rỗng hãy scan lại" }, JsonRequestBehavior.AllowGet);
                }
                //kiểm tra mã buyer có trong tồn sap chưa
                var isExist = _iFGWmsService.FindOneBuyerInfoById(buyerCode);
                if (isExist != null)
                {
                    if (isExist.sts_cd.Equals("001"))
                    {
                        return Json(new { result = false, message = "Tem gói đang tồn kho"  }, JsonRequestBehavior.AllowGet);
                    }
                    if (isExist.sts_cd.Equals("010"))
                    {
                        return Json(new { result = false, message = "Tem đã được đóng thùng rồi" }, JsonRequestBehavior.AllowGet);
                    }
                    if (isExist.sts_cd.Equals("000"))
                    {
                        return Json(new { result = false, message = "Đã giao khách với đơn giao là: " + isExist.dl_no }, JsonRequestBehavior.AllowGet);
                    }
                }

                //insert generalfg
                //lấy product
                buyerCode = buyerCode.ToUpper();
                int index = buyerCode.IndexOf("DZIH");
                if (index <1)
                {
                    return Json(new { result = false, message = "Tem gói không hợp lệ vui lòng thử lại" }, JsonRequestBehavior.AllowGet);
                }
                string productcode = buyerCode.Substring(0, index);
                //lấy ra lot_date bằng cách trừ đi product và 8 kí tự sẽ tìm được mã lot_date

                int startBien = index + 8;

                //lot_date
                var date = buyerCode.Substring(startBien, 3);
                string lot_date = DateFormatByShinsungRule(date);

                var ktra_stamp_cd = _iFGWmsService.GetStyleNo(productcode);
                if (ktra_stamp_cd == null)
                {
                    return Json(new { result = false, message = "Product chưa được đăng kí" }, JsonRequestBehavior.AllowGet);
                }

                //insert generalfg
                var generalfg = new generalfg()
                {
                    buyer_qr = buyerCode,
                    product_code = ktra_stamp_cd.style_no,
                    md_cd = ktra_stamp_cd.md_cd,
                    qty = ktra_stamp_cd.pack_amt.HasValue ? ktra_stamp_cd.pack_amt.Value : 0,
                    lot_no = lot_date,
                    reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString(),
                    chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString(),
                    reg_dt = DateTime.Now,
                    chg_dt = DateTime.Now,
                    sts_cd = "001",
                };
                _iFGWmsService.Insertgeneralfg(generalfg);

                //kiểm tra nó có trùng tem đang chờ nhập kho của MES không thì update lun
                var checkisExist = _iFGWmsService.CheckBuyerFG(buyerCode);
                if (checkisExist != null)
                {
                    //update mã tem ở buyer
                    checkisExist.input_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                    checkisExist.lct_cd = "003G01000000000000";
                    checkisExist.from_lct_cd = "006000000000000000";
                    checkisExist.to_lct_cd = "003G01000000000000";
                    checkisExist.mt_sts_cd = "001";
                    checkisExist.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    checkisExist.chg_dt = DateTime.Now;
                    _iFGWmsService.UpdateReceFGWMaterial(checkisExist);
                }

                return Json(new { result = true, message = "Thêm thành công", data = generalfg }, JsonRequestBehavior.AllowGet);
               
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống." }, JsonRequestBehavior.AllowGet);
            }
        }
        public string DateFormatByShinsungRule(string input)
        {
            var y = input.Substring(0, 1);
            var m = input.Substring(1, 1);
            var d = input.Substring(2, 1);
            string year = ChangeCharacterToNumber(y).ToString();
            string month = ChangeCharacterToNumber(m).ToString();
            string date = ChangeCharacterToNumber(d).ToString();
            StringBuilder result = new StringBuilder();
            year = "20" + year + "-";

            if (month.Length < 2)
            {
                month = "0" + month + "-";
            }
            else
            {
                month = month + "-";
            }
            if (date.Length < 2)
            {
                date = "0" + date;
            }

            result.Append(year);
            result.Append(month);
            result.Append(date);
            return result.ToString();
        }
        public int ChangeCharacterToNumber(string number)
        {
            int temp;
            if (int.TryParse(number, out temp))
            {

                return int.Parse(number);
            }

            char c = 'Z';
            char a = char.Parse(number);
            int i = 35;
            do
            {

                c--;
                i--;
            }
            while (c != a);
            return i;


        }
        public class ModelInsertTemGeneral
        {
            public string Code { get; set; }
            public string Model { get; set; }
            public string BuyerCode { get; set; }
            public string Quantity { get; set; }
            public string LotNo { get; set; }
           
        }
        public JsonResult InsertTemGeneral(generalfg generalfg, stamp_detail stamp_detail, List<ModelInsertTemGeneral> InsertTemGeneral)
        {
            var data_update = 0;
            var data_create = 0;
            var data_error = 0;
         
            if (InsertTemGeneral == null)
            {
                return Json(new { result = false, message = "File excel rỗng" }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                foreach (var item in InsertTemGeneral)
                {
                    if (item.BuyerCode == null)
                    {
                        data_error++;
                    }
                    else
                    {
                        // get productCODE
                        //lấy product 
                        string productCode = item.BuyerCode.Substring(0, item.BuyerCode.IndexOf("DZIH"));
                        var dataPRoduct = _iFGWmsService.FindOneProductById(productCode);
                        // if product no exist in db
                        if (dataPRoduct != null)
                        {
                        
                            var list1 = _iFGWmsService.FindGeneralfg(item.BuyerCode);

                            if (list1 == null)
                            {
                                //lấy ra lot_date bằng cách trừ đi product và 8 kí tự sẽ tìm được mã lot_date
                                var startBien = productCode.Length + 8;
                                //CEDMB2976DZIHA1N0L4E001010100
                                //lot_date
                                var date = item.BuyerCode.Substring(startBien, 3);
                                var lot_date = DateFormatByShinsungRule(date);
                                //lấy số lượng. nếu có thì lấy , ngược lại lấy trong bảng product

                                int quantity = 0;
                                if (!string.IsNullOrEmpty(item.Quantity))
                                {
                                    quantity = Convert.ToInt32(item.Quantity);
                                }
                                else
                                {
                                    quantity = (int)dataPRoduct.pack_amt;
                                }
                                generalfg.md_cd = dataPRoduct.md_cd;
                                generalfg.lot_no = lot_date;
                                generalfg.qty = quantity;
                                generalfg.product_code = dataPRoduct.style_no;
                                generalfg.sts_cd = "001";
                                generalfg.type = "SAP";
                                generalfg.buyer_qr = item.BuyerCode;
                                generalfg.chg_dt = DateTime.Now;
                                generalfg.reg_dt = DateTime.Now;
                                generalfg.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                                generalfg.reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();

                                if (_iFGWmsService.InsertToSAPGeneralfg(generalfg) > 0)
                                {
                                    data_create++;

                                    //kiểm tra nó có trùng tem đang chờ nhập kho của MES không thì update lun
                                    var isExist = _iFGWmsService.CheckBuyerFG(item.BuyerCode);
                                    if (isExist != null)
                                    {
                                        //update mã tem ở buyer

                                        isExist.input_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                                        isExist.lct_cd = "003G01000000000000";
                                        isExist.from_lct_cd = "006000000000000000";
                                        isExist.to_lct_cd = "003G01000000000000";
                                        isExist.mt_sts_cd = "001";
                                        isExist.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                                        isExist.chg_dt = DateTime.Now;
                                        _iFGWmsService.UpdateReceFGWMaterial(isExist);
                                    }
                                }
                            }
                            else
                            {
                                data_update++;
                            }

                        }
                        else
                        {
                            data_error++;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                data_error++;
            };
            return Json(new
            {
                result = true,
                data_update = data_update,
                data_create = data_create,
                data_error = data_error
            }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult updateQuantityBuyer(ListIdGenneralFG model)
        {
            try
            {
                StringBuilder idsList = new StringBuilder();
                if (model.listId != null)
                {
                    foreach (var item in model.listId)
                    {
                        idsList.Append($"{item},");

                    }
                }
                string listId = new InitMethods().RemoveLastComma(idsList);

                if (model.gr_qty > 0  && model.listId != null)
                {
                    var userAcount = Session["userid"] == null ? null : Session["userid"].ToString();
                    var userWIP = "";
                    if (string.IsNullOrEmpty(userAcount))
                    {
                        return Json(new { result = false, message = "Vui lòng đăng nhập để sửa số lượng" }, JsonRequestBehavior.AllowGet);
                    }
                    //if (!string.IsNullOrEmpty(userAcount))
                    //{
                    //    var dsMbInfo = _iFGWmsService.GetMbInfoGrade(userAcount);
                    //    userWIP = dsMbInfo.grade;
                    //}
                    //if (userWIP != "Admin")
                    //{
                    //    return Json(new { result = false, message = "Tài khoản thuộc quyền Admin mới được chỉnh sửa" }, JsonRequestBehavior.AllowGet);
                    //}
                    generalfg item = new generalfg();
                    item.qty = (int)model.gr_qty;

                    item.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    item.chg_dt = DateTime.Now;



                    _iFGWmsService.UpdateQtyGenneral(item, listId);



                    return Json(new { result = true,message = "Sửa thành công" }, JsonRequestBehavior.AllowGet);

                }
                return Json(new { result = false, message = "Sửa thất bại" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = e }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult updateLotNoBuyer(ListIdGenneralFG model)
        {
            try
            {
                StringBuilder idsList = new StringBuilder();
                if (model.listId != null)
                {
                    foreach (var item in model.listId)
                    {
                        idsList.Append($"{item},");

                    }
                }
                string listId = new InitMethods().RemoveLastComma(idsList);

                if (!string.IsNullOrEmpty(model.lot_no) && model.listId != null)
                {
                    var userAcount = Session["userid"] == null ? null : Session["userid"].ToString();
                  
                    if (string.IsNullOrEmpty(userAcount))
                    {
                        return Json(new { result = false, message = "Vui lòng đăng nhập để sửa số lượng" }, JsonRequestBehavior.AllowGet);
                    }
                   
                    generalfg item = new generalfg();
                    item.lot_no = model.lot_no;

                    item.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    item.chg_dt = DateTime.Now;



                    _iFGWmsService.UpdateLotNoGenneral(item, listId);



                    return Json(new { result = true, message = "Sửa thành công" }, JsonRequestBehavior.AllowGet);

                }
                return Json(new { result = false, message = "Sửa thất bại" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = e }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult InsertTemGeneral1(generalfg generalfg, stamp_detail stamp_detail, List<ModelInsertTemGeneral> InsertTemGeneral)
        {
            var data_update = 0;
            var data_create = 0;
            var data_error = 0;
            var dsProduct = db.d_style_info.ToList();
            if (InsertTemGeneral == null)
            {
                return Json(new { result = false, message = "File excel rỗng" }, JsonRequestBehavior.AllowGet);
            }
            foreach (var item in InsertTemGeneral)
            {
                if (item.BuyerCode == null)
                {
                    data_error++;
                }
                else
                {
                    Int64 list1 = db.generalfgs.Count(x => x.buyer_qr == item.BuyerCode);
                    try
                    {

                        if (list1 == 0)
                        {


                            generalfg.md_cd = item.Model;
                            //generalfg.lot_no = item.LotNo;

                            generalfg.lot_no = item.LotNo.Substring(0, 4) + '-' + item.LotNo.Substring(4, 2) + '-' + item.LotNo.Substring(6, 2);
                            var quantity = Convert.ToInt32(item.Quantity);
                            generalfg.qty = quantity;
                            generalfg.product_code = item.Code;
                            generalfg.sts_cd = "001";
                            generalfg.type = "SAP";
                            generalfg.buyer_qr = item.BuyerCode;
                            generalfg.chg_dt = DateTime.Now;
                            generalfg.reg_dt = DateTime.Now;
                            generalfg.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                            generalfg.reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();

                            db.Entry(generalfg).State = EntityState.Added;
                            Int64 list2 = db.stamp_detail.Count(x => x.buyer_qr == item.BuyerCode);
                            if (list2 == 0)
                            {
                                //LẤY stamp_code
                                var stampCode = "";
                                var stampCode1 = dsProduct.Where(x => x.style_no == item.Code).FirstOrDefault();
                                if (stampCode1 != null)
                                {
                                    stampCode = stampCode1.stamp_code;
                                }
                                var lotDt = "";
                                if (item.LotNo != null && item.LotNo.Length >= 8)
                                {
                                    lotDt = item.LotNo.Substring(0, 4) + "-" + item.LotNo.Substring(4, 2) + "-" + item.LotNo.Substring(6, 2);
                                }
                                stamp_detail.buyer_qr = item.BuyerCode;
                                stamp_detail.stamp_code = stampCode;
                                stamp_detail.product_code = item.Code;
                                stamp_detail.standard_qty = 0;
                                stamp_detail.lot_date = lotDt;
                                stamp_detail.is_sent = "N";
                                stamp_detail.chg_dt = DateTime.Now;
                                stamp_detail.reg_dt = DateTime.Now;
                                stamp_detail.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                                stamp_detail.reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();

                                db.Entry(stamp_detail).State = EntityState.Added;
                            }

                            db.SaveChanges();
                            data_create++;
                        }
                        else
                        {
                            data_update++;
                        }

                    }
                    catch (Exception e)
                    {
                        data_error++;
                    }
                }
            }
            return Json(new
            {
                result = true,
                data_update = data_update,
                data_create = data_create,
                data_error = data_error
            }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ExportFGgeneralToExcel(string productCode, string buyerCode, string recevice_dt_start, string recevice_dt_end)
        {
            if (string.IsNullOrEmpty(productCode))
            {
                productCode = "";
            }
            if (string.IsNullOrEmpty(buyerCode))
            {
                buyerCode = "";
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

            StringBuilder sql = new StringBuilder();
            sql.AppendLine($"SELECT a.*,(SELECT dt_nm FROM comm_dt WHERE comm_dt.mt_cd = 'WHS005' AND comm_dt.dt_cd = a.sts_cd) AS statusName");
            sql.AppendLine($"FROM generalfg as a  where a.product_code LIKE");
            sql.AppendLine($"CONCAT('%', '{productCode}', '%') AND a.buyer_qr LIKE CONCAT('%', '{buyerCode}', '%')");
            sql.AppendLine($"AND a.sts_cd in ('001', '010')   AND case     when('{recevice_dt_start}' <> '00010101' AND a.reg_dt IS not NULL) ");
            sql.AppendLine($"THEN DATE_FORMAT(a.reg_dt, '%Y%m%d') >= STR_TO_DATE('20210401', '%Y%m%d')        WHEN a.reg_dt IS NULL then 1 = 0 ");
            sql.AppendLine($" ELSE '' = ''        end AND     case    when('{recevice_dt_end}' <> '99991231' AND a.reg_dt IS not NULL) ");
            sql.AppendLine($" then DATE_FORMAT(a.reg_dt, '%Y%m%d') <= STR_TO_DATE('{recevice_dt_end}', '%Y%m%d')       when a.reg_dt IS NULL then 1 = 0  ELSE '' = '' end");
                   

            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);

            DataSet ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables[0].TableName = "FG_Inventory";

            using (XLWorkbook wb = new XLWorkbook())
            {
                var ws = wb.AddWorksheet(dt);

                ws.Columns().AdjustToContents();
                ws.Rows().AdjustToContents();

                //ws.Cells("A1").Value = "Lot Code";
                //ws.Cells("B1").Value = "Buyer";
                //ws.Cells("C1").Value = "PO";
                //ws.Cells("D1").Value = "Composite";
                //ws.Cells("E1").Value = "QTy (Roll/EA)";
                //ws.Cells("F1").Value = "Status";
                //ws.Cells("G1").Value = "Recevied Date";
                //ws.Cells("H1").Value = "Product";
                //ws.Cells("I1").Value = "Expiry Date";

                wb.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                wb.Style.Alignment.ShrinkToFit = true;
                wb.Style.Font.Bold = true;

                Response.Clear();
                Response.Buffer = true;
                Response.Charset = "";
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;filename=FGInventory.xlsx");
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


    }
}

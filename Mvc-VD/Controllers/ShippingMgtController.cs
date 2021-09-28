using ClosedXML.Excel;
using Microsoft.AspNet.SignalR.Client;
using Mvc_VD.Models;
using Mvc_VD.Models.Language;
using Mvc_VD.Models.WIP;
using Mvc_VD.Models.WOModel;
using Mvc_VD.Services;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Mvc_VD.Controllers
{
    public class ShippingMgtController : BaseController
    {
        private readonly IWMSService _IWMSService;
        private readonly IWIPService _IWIPService;
        private HubConnection connection = new HubConnection(Extension.GetAppSetting("Realtime"));
        IHubProxy chat;
        //private Entities db = new Entities();  old
        private Entities db;
        string exs = "Lỗi hệ thống!!!";
        string exits = "Dữ liệu đã tồn tại!!!";
        string notexits = "Dữ liệu không tồn tại!!!";
        string ss = "Thành công!!!";
        string used = "Đã được sử dụng!!!";
        string notfind = "Không tìm thấy!!!";
        string scanagain = "Vui lòng Scan Lại!!!";
        string chagain = "Vui lòng chọn  Lại!!!";

        public ShippingMgtController(
           IWMSService IWMSService,
             IWIPService IWIPService,
           IDbFactory DbFactory)
        {
            _IWMSService = IWMSService;
            _IWIPService = IWIPService;

            db = DbFactory.Init();
            chat = connection.CreateHubProxy("shinsungHub");
            connection.Start().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Console.WriteLine("There was an error opening the connection:{0}",
                                      task.Exception.GetBaseException());
                }
                else
                {
                    Console.WriteLine("Connected");
                }

            }).Wait();
        }


        #region N_ShippingPicking Scan



        public ActionResult ShippingPickingScan(string code)
        {
            ViewData["Message"] = code;
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

        public ActionResult GetPickingScan(string sd_no, string sd_nm, string product_cd, string remark)
        {
            var sql = new StringBuilder();
            sql.Append(" SELECT a.*, ")
                .Append(" (select dt_nm  from comm_dt where mt_cd='WHS005' and dt_cd = a.sd_sts_cd) as sts_nm, ")
                .Append("(select lct_nm  from lct_info where lct_cd = a.lct_cd) as lct_nm ")

               .Append(" FROM w_sd_info as a ")
                .Append(" WHERE a.use_yn ='Y' ")
                 .Append("AND ('" + sd_no + "'='' OR  a.sd_no like '%" + sd_no + "%' )")
                 .Append("AND ('" + sd_nm + "'='' OR  a.sd_nm like '%" + sd_nm + "%' )")
                 .Append("AND ('" + product_cd + "'='' OR  a.product_cd like '%" + product_cd + "%' )")
                 .Append("AND ('" + remark + "'='' OR  a.remark like '%" + remark + "%' )")

                .Append(" order by sid desc ");

            return new InitMethods().ConvertDataTableToJsonAndReturn(sql);

        }
        public ActionResult InsertSDInfo(w_sd_info w_sd_info)
        {
            #region Tang tự động
            String sd_no = "SD1";

            var sd_no_last = db.w_sd_info.ToList().LastOrDefault();
            if (sd_no_last != null)
            {
                var sd_noCode = sd_no_last.sd_no;
                sd_no = string.Concat("SD", (int.Parse(sd_noCode.Substring(2)) + 1).ToString());
            }
            #endregion

            string trimmed = String.Concat(w_sd_info.product_cd.Where(c => !Char.IsWhiteSpace(c)));
            w_sd_info.product_cd = trimmed.ToUpper();

            w_sd_info.sd_no = sd_no;
            w_sd_info.alert = 0;
            w_sd_info.lct_cd = "002000000000000000";
            w_sd_info.sd_sts_cd = "000";
            w_sd_info.reg_id = Session["authName"] == null ? null : Session["authName"].ToString();
            w_sd_info.chg_id = Session["authName"] == null ? null : Session["authName"].ToString();

            w_sd_info.use_yn = "Y";
            w_sd_info.del_yn = "N";
            w_sd_info.reg_dt = DateTime.Now;
            w_sd_info.chg_dt = DateTime.Now;

            string sqlQuery = "insert into w_sd_info(product_cd,sd_no,alert,lct_cd,sd_sts_cd,reg_id,chg_id,use_yn,del_yn,reg_dt,chg_dt,sd_nm,remark) " +
                "values(@1,@2,@3,@4,@5,@6,@7,@8,@9,@10,@11,@12,@13)";
            db.Database.ExecuteSqlCommand(sqlQuery,
              new MySqlParameter("@1", w_sd_info.product_cd),
              new MySqlParameter("@2", w_sd_info.sd_no),
              new MySqlParameter("@3", w_sd_info.alert),
              new MySqlParameter("@4", w_sd_info.lct_cd),
              new MySqlParameter("@5", w_sd_info.sd_sts_cd),
              new MySqlParameter("@6", w_sd_info.reg_id),
              new MySqlParameter("@7", w_sd_info.chg_id),
              new MySqlParameter("@8", w_sd_info.use_yn),
              new MySqlParameter("@9", w_sd_info.del_yn),
              new MySqlParameter("@10", w_sd_info.reg_dt),
              new MySqlParameter("@11", w_sd_info.chg_dt),
              new MySqlParameter("@12", w_sd_info.sd_nm),
              new MySqlParameter("@13", w_sd_info.remark)
                );


            //    db.w_sd_info.Add(w_sd_info);
            //      db.SaveChanges();

            var sql = new StringBuilder();
            sql.Append(" SELECT a.*, ")
               .Append(" (select dt_nm  from comm_dt where mt_cd='WHS005' and dt_cd = a.sd_sts_cd) as sts_nm, ")
               .Append("(select lct_nm  from lct_info where lct_cd = a.lct_cd) as lct_nm ")
               .Append(" FROM w_sd_info as a ")
               .Append(" WHERE a.sd_no='" + sd_no + "'  ")
               .Append(" order by sid desc ");

            var ddd = new InitMethods().ConvertDataTableToJsonAndReturn(sql);
            return Json(new { result = true, data = ddd.Data }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult UpdateSDInfo(w_sd_info w_sd_info)
        {
            try
            {
                var KTTT = db.w_sd_info.Find(w_sd_info.sid);
                if (KTTT == null)
                {
                    return Json(new { result = false, message = notexits }, JsonRequestBehavior.AllowGet);
                }
                string trimmed = String.Concat(w_sd_info.product_cd.Where(c => !Char.IsWhiteSpace(c)));
                KTTT.product_cd = trimmed.ToUpper();

                KTTT.sd_nm = w_sd_info.sd_nm;

                KTTT.remark = w_sd_info.remark;
                KTTT.chg_id = Session["authName"] == null ? null : Session["authName"].ToString();
                //KTTT.chg_dt = DateTime.Now;


                db.Entry(KTTT).State = EntityState.Modified;
                db.SaveChanges();


                return Json(new { result = true, data = KTTT }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult DeleteSDInfo(w_sd_info w_sd_info)
        {
            try
            {
                //kiểm tra id đó có tồn tại không.

                //var KTTT = db.w_sd_info.Find(w_sd_info.sid);
                //if (KTTT == null)
                //{
                //    return Json(new { result = false, message = notexits }, JsonRequestBehavior.AllowGet);
                //}
                //if (!(KTTT.sd_sts_cd.Equals("000")))
                //{
                //    return Json(new { result = false, message = "Đã qua công đoạn khác!!" }, JsonRequestBehavior.AllowGet);
                //}

                if (_IWIPService.GetListSd(w_sd_info.sid) == null)
                {
                    return Json(new { result = false, message = notexits }, JsonRequestBehavior.AllowGet);
                }
                //kiểm tra xem sd này bên wip đã nhận chưa nếu nhận rồi là không được xóa

                if (!_IWIPService.CheckMaterialSd(w_sd_info.sd_no))
                {
                    return Json(new { result = false, message = "SD này đã được kho sản xuất nhận rồi " }, JsonRequestBehavior.AllowGet);
                }
                _IWIPService.DeleteSDInfo(w_sd_info.sid);
                _IWIPService.DeleteShippingSDInfo(w_sd_info.sd_no);

                //db.Entry(KTTT).State = EntityState.Deleted;
                //db.SaveChanges();


                return Json(new { result = true, data = w_sd_info.sid, message = ss }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult getdsMaterialDetail(string sd_no, string mt_no)
        {
            try
            {
                IEnumerable<shippingsdmaterial> value = _IWIPService.GetListShippngMaterial(sd_no, mt_no);

                return Json(value, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);
            }
        }
        public async Task<ActionResult> DelMaterialShippng(int id)
        {
            //kiểm tra mã này có trong db không
            var CheckExist = _IWIPService.GetShippingExist(id);
            if (CheckExist == null)
            {
                return Json(new { result = false, message = "Không tìm thấy mã này đễ xóa" }, JsonRequestBehavior.AllowGet);
            }
            //kiểm tra số lượng gửi >= số lượng nhận mới cho xóa.
            // số lượng gửi:
            var DanhSachGui = _IWIPService.GetListShippngMaterial(CheckExist.sd_no, CheckExist.mt_no);
            double SoLuongGui = DanhSachGui.Where(x => x.id != id).Sum(x => x.quantity);
            // số lượng nhan:
            var DanhSachNhan = _IWIPService.GetListWMaterial(CheckExist.sd_no, CheckExist.mt_no);
            double SoLuongNhan = DanhSachNhan.Count();
            //Số lượng gửi ít hơn số lượng nhận nên không được xóa
            if (SoLuongGui - SoLuongNhan < 0)
            {
                return Json(new { result = false, message = "Không xóa được vì đã được nhận ở kho NVL" }, JsonRequestBehavior.AllowGet);
            }
            else//Có thể xóa
            {

                if (_IWIPService.Deleteshippingsdmaterial(id) > 0)
                {
                    //kiểm tra xem nguyên vật liệu này nhận đủ chưa

                    //UPDATE SD ALERT = 0, AND STS = 001 // trở về trạng thái tồn kho
                    //ktra xem còn sd nào trong w_material_tam không, nêu ko có thì update
                    //var kt_sd = db.w_material_info_tam.Count(x => x.sd_no == sd_no && x.mt_sts_cd.Equals("000"));
                    //if (kt_sd == 0)
                    //int kt_sd = _IWIPService.GetWMaterialInfoTamwithCount(sd_no, "000");
                    var kt_sd = _IWIPService.GetListMaterialInfoBySdNo(CheckExist.sd_no).ToList();
                    if (kt_sd.Count() > 0)
                    {
                        if (!kt_sd.Any(x => x.SoluongConLai > 0))
                        {

                            var user = Session["authName"] == null ? null : Session["authName"].ToString();


                            _IWIPService.UpdatedSdInfo(0, "001", user, CheckExist.sd_no, DateTime.Now);



                            await chat.Invoke<string>("Hello", "010").ContinueWith(task => {
                                if (task.IsFaulted)
                                {
                                    Console.WriteLine("There was an error calling send: {0}",
                                                      task.Exception.GetBaseException());
                                }
                                else
                                {
                                    Console.WriteLine(task.Result);
                                }
                            });
                        }
                    }
                    
                    return Json(new { result = true, message = "Xóa thành công" }, JsonRequestBehavior.AllowGet);
                }
            }


            return Json(new { result = false, message = "Không thể xóa mã này" }, JsonRequestBehavior.AllowGet);

        }
        public async Task<ActionResult> updateSoCuon(int id, int Qty)
        {
            try
            {
                //kiểm tra mã này có trong db không
                var CheckExist = _IWIPService.GetShippingExist(id);
                if (CheckExist == null)
                {
                    return Json(new { result = false, message = "Không tìm thấy mã này đễ xóa" }, JsonRequestBehavior.AllowGet);
                }
                if (Qty == 0)
                {
                    return Json(new { result = false, message = "Số lượng bắt buộc lớn hơn 0" }, JsonRequestBehavior.AllowGet);
                }
                // số lượng gửi:
                var DanhSachGui = _IWIPService.GetListShippngMaterial(CheckExist.sd_no, CheckExist.mt_no);
                int SoLuongGui = DanhSachGui.Where(x => x.id != id).Sum(x => x.quantity).ToInt();
                // số lượng nhan:
                var DanhSachNhan = _IWIPService.GetListWMaterial(CheckExist.sd_no, CheckExist.mt_no);
                int SoLuongNhan = DanhSachNhan.Count();
                //Số lượng gửi ít hơn số lượng nhận nên không được update
                if (SoLuongGui + Qty - SoLuongNhan < 0)
                {
                    return Json(new { result = false, message = "Không sửa được vì đã được nhận ở kho NVL" }, JsonRequestBehavior.AllowGet);
                }
                else//Có thể sửa
                {

                    if (_IWIPService.Updateshippingsdmaterial(id, Qty) > 0)
                    {
                        //kiểm tra xem SoluongConLai == 0 là update SD ALERT= 0, status = 001(tỨC TỒN KHO)
                        var user = Session["authName"] == null ? null : Session["authName"].ToString();
                        var kt_sd = _IWIPService.GetListMaterialInfoBySdNo(CheckExist.sd_no).ToList();
                        if (!kt_sd.Any(x => x.SoluongConLai > 0))
                        {
                            _IWIPService.UpdatedSdInfo(0, "001", user, CheckExist.sd_no, DateTime.Now);

                            await chat.Invoke<string>("Hello", "010").ContinueWith(task =>
                            {
                                if (task.IsFaulted)
                                {
                                    Console.WriteLine("There was an error calling send: {0}",
                                                      task.Exception.GetBaseException());
                                }
                                else
                                {
                                    Console.WriteLine(task.Result);
                                }
                            });
                        }
                        //kiểm tra xem SoluongConLai > 1 là update SD ALERT= 1, status = 000(tỨC TỒN KHO)
                        else
                        {
                            _IWIPService.UpdatedSdInfo(1, "000", user, CheckExist.sd_no, DateTime.Now);
                            await chat.Invoke<string>("Hello", "010").ContinueWith(task =>
                            {
                                if (task.IsFaulted)
                                {
                                    Console.WriteLine("There was an error calling send: {0}",
                                                      task.Exception.GetBaseException());
                                }
                                else
                                {
                                    Console.WriteLine(task.Result);
                                }
                            });
                        }
                            return Json(new { result = true, message = "Sửa thành công" }, JsonRequestBehavior.AllowGet);
                    }
                }
                return Json(new { result = false, message ="Không thể sửa"}, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Không thể sửa mã này" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult updateSoMeter(int id, int Qty)
        {
            try
            {
                //kiểm tra mã này có trong db không
                var CheckExist = _IWIPService.GetShippingExist(id);
                if (CheckExist == null)
                {
                    return Json(new { result = false, message = "Không tìm thấy mã này đễ xóa" }, JsonRequestBehavior.AllowGet);
                }
                if (Qty == 0)
                {
                    return Json(new { result = false, message = "Số lượng bắt buộc lớn hơn 0" }, JsonRequestBehavior.AllowGet);
                }
                //// số lượng gửi:
                //var DanhSachGui = _IWIPService.GetListShippngMaterial(CheckExist.sd_no, CheckExist.mt_no);
                //int SoLuongGui = DanhSachGui.Where(x => x.id != id).Sum(x => x.meter).ToInt();
                //// số lượng nhan:
                //var DanhSachNhan = _IWIPService.GetListWMaterial(CheckExist.sd_no, CheckExist.mt_no);
                //int SoLuongNhan = DanhSachNhan.Sum(x=>x.gr_qty).ToInt();
                ////Số lượng gửi ít hơn số lượng nhận nên không được update
                //if (SoLuongGui + Qty - SoLuongNhan < 0)
                //{
                //    return Json(new { result = false, message = "Không sửa được vì đã được nhận ở kho NVL" }, JsonRequestBehavior.AllowGet);
                //}
                //else//Có thể sửa
                //{

                //    if (_IWIPService.UpdateshippingMeter(id, Qty) > 0)
                //    {
                //        return Json(new { result = true, message = "Sửa thành công" }, JsonRequestBehavior.AllowGet);
                //    }
                //}

                if (_IWIPService.UpdateshippingMeter(id, Qty) > 0)
                {
                    return Json(new { result = true, message = "Sửa thành công" }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = "Không thể sửa" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Không thể sửa mã này" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult GetPickingScanMLQR(string ml_cd)
        {
            try
            {
                if (string.IsNullOrEmpty(ml_cd))
                {
                    return Json(new { result = false, message = "Mã ML NO trống vui lòng Scan lại" }, JsonRequestBehavior.AllowGet);
                }

                var kttt_null = db.w_material_info_tam.Where(x => x.mt_cd == ml_cd).SingleOrDefault();
                if (kttt_null == null)
                {
                    return Json(new { result = false, message = notexits }, JsonRequestBehavior.AllowGet);
                }

                if (kttt_null.mt_barcode == null)
                {
                    return Json(new { result = false, message = "Mã này là nguyên vật liệu phụ không có barcode!!!" }, JsonRequestBehavior.AllowGet);
                }
                if (!(string.IsNullOrEmpty(kttt_null.sd_no)))
                {
                    return Json(new { result = false, message = used + " cho " + kttt_null.sd_no }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = true, message = exits, data = kttt_null }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }
        public async Task<ActionResult> InsertMTQRSDList(string data, string sd_no)
        {
            try
            {
                //UPDATE w_material_info_tam  COLUMN SD
                var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var sql2 = new StringBuilder();
                sql2.Append(" UPDATE  w_material_info_tam  ")

                .Append("  SET w_material_info_tam.picking_dt='" + time + "', w_material_info_tam.sd_no = '" + sd_no + "' , w_material_info_tam.lct_cd = '002000000000000000' , w_material_info_tam.from_lct_cd = '002000000000000000' , w_material_info_tam.lct_sts_cd = '000' ")
                .Append(",  w_material_info_tam.dt_of_receipt= IF(w_material_info_tam.dt_of_receipt IS NULL or w_material_info_tam.dt_of_receipt = '', DATE_FORMAT(NOW(), '%Y%m%d'), w_material_info_tam.dt_of_receipt) ")
                .Append(" WHERE w_material_info_tam.wmtid in (" + data + ") and w_material_info_tam.mt_barcode is not null");

                int effect_rows = new Excute_query().Execute_NonQuery(sql2);


                //UPDATE SD ALERT = 1 và sd_sts_cd = '000'
                var sql3 = new StringBuilder();
                sql3.Append(" UPDATE  w_sd_info  ")

                .Append("  SET w_sd_info.alert = 1 , w_sd_info.sd_sts_cd = '000' ")
                .Append(" WHERE w_sd_info.sd_no in ('" + sd_no + "') ");
                int effect_rows2 = new Excute_query().Execute_NonQuery(sql3);


                //Thông báo 		 
                await chat.Invoke<string>("Hello", "010").ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Console.WriteLine("There was an error calling send: {0}",
                                          task.Exception.GetBaseException());
                    }
                    else
                    {
                        Console.WriteLine(task.Result);
                    }
                });
                return Json(new { result = true, message = ss }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }

        }
        #region SD_Info_popup
        public ActionResult PartialView_SD_Info_Popup(string sd_no)
        {
            ViewBag.sd_no = sd_no;

            return PartialView();
        }
        public ActionResult GetPickingScanPP(string sd_no)
        {
            var listdata = (from a in db.w_sd_info

                            where a.sd_no.Equals(sd_no)

                            select new
                            {
                                sd_no = a.sd_no,
                                sd_nm = a.sd_nm,
                                sts_nm = (db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == a.sd_sts_cd).Select(x => x.dt_nm)),

                                lct_nm = (db.lct_info.Where(x => x.lct_cd == a.lct_cd).Select(x => x.lct_nm)),
                                remark = a.remark,

                            }).ToList();

            return Json(new { data = listdata }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetPickingScanPP_Count_MT_no(string sd_no)
        {
            try
            {

                //var sql = new StringBuilder();
                //sql.Append(" SELECT COUNT(mt_cd) AS mt_cnt, abc.mt_no FROM (SELECT  a.mt_no, a.mt_cd  ")
                //    .Append(" FROM w_material_info_tam AS a WHERE a.sd_no ='" + sd_no + "'  AND a.lct_cd LIKE '002%'  ")
                //  .Append(" UNION all  ")


                //    .Append(" SELECT  b.mt_no, b.mt_cd FROM w_material_info AS b  WHERE b.sd_no ='" + sd_no + "' AND b.lct_cd LIKE '002%') AS abc  ")
                //   .Append(" GROUP BY abc.mt_no ");


                //StringBuilder varname1 = new StringBuilder();
                //varname1.Append(" SELECT COUNT(mt_cd) AS cap, max(abc.mt_no) AS  mt_no, max(abc.mt_sts_cd) mt_sts_cd,  \n");
                //varname1.Append("(SELECT COUNT(nhAN.mt_cd) FROM w_material_info AS nhAN WHERE nhAN.mt_no =  max(abc.mt_no) and  nhAN.sd_no ='" + sd_no + "' AND nhAN.lct_cd LIKE '002%' )as nhan, \n");
                //varname1.Append("(SELECT COUNT(dasd.mt_cd) FROM w_material_info AS dasd WHERE dasd.mt_no =  max(abc.mt_no) and  dasd.sd_no ='" + sd_no + "' AND dasd.lct_cd LIKE '002%' AND dasd.mt_sts_cd ='005' )as dasd , \n");
                //varname1.Append(" (SELECT COUNT(dangsd.mt_cd) FROM w_material_info AS dangsd WHERE dangsd.mt_no =  max(abc.mt_no) and  dangsd.sd_no ='" + sd_no + "' AND dangsd.lct_cd LIKE '002%' AND dangsd.mt_sts_cd ='002' )as dangsd,  \n");
                //varname1.Append("   (SELECT COUNT(trave.mt_cd) FROM w_material_info AS trave WHERE trave.mt_no =  max(abc.mt_no) and  trave.sd_no  ='" + sd_no + "' AND trave.lct_cd LIKE '002%' AND trave.mt_sts_cd ='013' ) as trave ");
                //varname1.Append("FROM ( \n");
                //varname1.Append("SELECT a.mt_no, a.mt_cd , a.mt_sts_cd \n");
                //varname1.Append("FROM w_material_info_tam AS a \n");
                //varname1.Append("WHERE a.sd_no ='" + sd_no + "' AND a.lct_cd LIKE '002%' UNION ALL \n");
                //varname1.Append("SELECT b.mt_no, b.mt_cd, b.mt_sts_cd \n");
                //varname1.Append("FROM w_material_info AS b \n");
                //varname1.Append("WHERE b.sd_no ='" + sd_no + "' AND b.lct_cd LIKE '002%') AS abc \n");
                //varname1.Append("GROUP BY abc.mt_no");
                //db.GetPickingScanPP_Count_MT_no(sd_no).ToList()


                //var list2 = new InitMethods().ConvertDataTableToJsonAndReturn(varname1);


                //return Json(new { data = list2.Data }, JsonRequestBehavior.AllowGet);

                StringBuilder sql = new StringBuilder($"CALL GetPickingScanPP_Count_MT_no('{sd_no}');");


                var list2 = new InitMethods().ConvertDataTableToJsonAndReturn(sql);


                return Json(new { data = list2.Data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }

        }

        public ActionResult GetPickingScanListPP(string sd_no)
        {
            try
            {

                //var sql = new StringBuilder();
                //sql.Append(" SELECT t.*, ROW_NUMBER() OVER(ORDER BY (SELECT wmtid)) as id  ")
                //    .Append(" FROM ( ")
                //   .Append(" SELECT ''rece_wip_dt,picking_dt,wmtid , mt_cd,mt_no,lot_no,expiry_dt,dt_of_receipt,expore_dt,gr_qty, sd_no, mt_sts_cd , ")
                //    .Append(" (select dt_nm from comm_dt where mt_cd = 'WHS005' and dt_cd = mt_sts_cd) as sts_nm, '' as po")
                //   .Append(" FROM w_material_info_tam ")
                //   .Append(" where sd_no = '" + sd_no + "' and lct_cd LIKE '002%' ")

                //   .Append(" UNION SELECT rece_wip_dt,picking_dt,ROW_NUMBER() OVER (ORDER BY wmtid ) AS id , mt_cd,mt_no,lot_no,expiry_dt,dt_of_receipt,expore_dt,gr_qty, sd_no , mt_sts_cd , ")
                //   .Append(" (select dt_nm from comm_dt where mt_cd = 'WHS005' and dt_cd = mt_sts_cd) as sts_nm ,")
                //   .Append(" (SELECT at_no FROM w_actual  WHERE w_actual.id_actual = w_material_info.id_actual LIMIT 1) AS po ")
                //   .Append(" FROM w_material_info ")
                //   .Append(" where sd_no = '" + sd_no + "'  and lct_cd LIKE '002%'")
                //   .Append(" ) t ");
                //var result = new InitMethods().ConvertDataTableToJsonAndReturn(sql);
                //



                StringBuilder varname1 = new StringBuilder();
                varname1.Append("SELECT rece_wip_dt,picking_dt,wmtid,  mt_cd,mt_no,lot_no,expiry_dt,dt_of_receipt,expore_dt,gr_qty, sd_no, mt_sts_cd, ( \n");
                varname1.Append("SELECT dt_nm \n");
                varname1.Append("FROM comm_dt \n");
                varname1.Append("WHERE mt_cd = 'WHS005' AND dt_cd = mt_sts_cd) AS sts_nm, w_material_info.lct_cd, \n");
                varname1.Append("  CASE  WHEN w_material_info.ExportCode IS NULL  THEN lct_info.lct_nm ELSE w_material_info.ExportCode \n");
                varname1.Append(" END lct_nm , \n");
                varname1.Append("( \n");
                varname1.Append("SELECT at_no \n");
                varname1.Append("FROM w_actual \n");
                varname1.Append("WHERE w_actual.id_actual = w_material_info.id_actual \n");
                varname1.Append("LIMIT 1) AS po \n");
                varname1.Append("FROM w_material_info \n");
                varname1.Append(" LEFT JOIN lct_info ON w_material_info.lct_cd = lct_info.lct_cd  \n");
                varname1.Append("WHERE sd_no =  '" + sd_no + "' AND w_material_info.lct_cd LIKE '002%' ORDER BY w_material_info.lct_cd ");
                var result = new InitMethods().ConvertDataTableToJsonAndReturn(varname1);
                // var list =  _IWIPService.GetWmaterialWip(sd_no);

                return Json(new { data = result.Data }, JsonRequestBehavior.AllowGet);



            }
            catch (Exception e)
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }

        }
        public ActionResult GetPickingScanPP_Memo(string sd_no)
        {
            var ds = db.w_material_info_memo.Where(x => x.sd_no.Equals(sd_no)).ToList();
            var listdata = (from a in ds
                            select new
                            {
                                id = a.id,
                                md_cd = a.md_cd,
                                style_no = a.style_no,
                                style_nm = a.style_nm,
                                size = a.width + "MM X " + a.spec + "M",
                                mt_cd = a.mt_cd,
                                memo = a.memo,
                                month_excel = a.month_excel,
                                TX = a.TX,
                                total_m = a.total_m,
                                total_m2 = a.total_m2,
                                total_ea = a.total_ea,
                                lot_no = a.lot_no,
                            }).ToList();

            return Json(new { data = listdata }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult PrintSD_LIST(string sd_no)
        {
            ViewData["Message"] = sd_no;

            return View();
        }

        public ActionResult DeleteRecortSd(string mt_cd)
        {
            try
            {
                if (string.IsNullOrEmpty(mt_cd))
                {
                    return Json(new { result = false, message = notexits }, JsonRequestBehavior.AllowGet);
                }

                var kttt = db.w_material_info_tam.Any(x => x.mt_cd == mt_cd && x.mt_sts_cd.Contains("000"));
                if (kttt)
                {
                    var KTTT = db.w_material_info_tam.Where(x => x.mt_cd == mt_cd && x.mt_sts_cd.Contains("000")).FirstOrDefault();
                    if (KTTT == null)
                    {
                        return Json(new { result = false, message = exits }, JsonRequestBehavior.AllowGet);
                    }

                    //insert lại bảng tạm
                    KTTT.sd_no = null;
                    KTTT.lct_cd = null;
                    KTTT.to_lct_cd = null;
                    KTTT.from_lct_cd = null;
                    KTTT.lct_sts_cd = null;
                    KTTT.remark = "Không tìm thấy mã này";
                    db.Entry(KTTT).State = EntityState.Modified;
                    db.SaveChanges();
                    //ktra xem con sd nào bị remain nữa ko, nêu hết rồi thì update status của sd đó


                    if (!db.w_material_info_tam.Any(x => x.mt_sts_cd.Contains("000") && x.sd_no == KTTT.sd_no))
                    {
                        //UPDATE SD ALERT = 0 và sd_sts_cd = '001'
                        var sql3 = new StringBuilder();
                        sql3.Append(" UPDATE  w_sd_info  ")

                        .Append("  SET w_sd_info.alert = 0 , w_sd_info.sd_sts_cd = '001' ")
                        .Append(" WHERE w_sd_info.sd_no in ('" + KTTT.sd_no + "') ");

                        using (var cmd = db.Database.Connection.CreateCommand())
                        {
                            db.Database.Connection.Open();

                            cmd.CommandText = sql3.ToString();
                            var reader = cmd.ExecuteNonQuery();
                            int result1 = Int32.Parse(reader.ToString());
                            db.Database.Connection.Close();
                        }
                    }
                    return Json(new { result = true, message = ss }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { result = false, message = exits }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion
        #region  PartialView_List_ML_NO_Info_Popup
        public ActionResult PartialView_List_ML_NO_Info_Popup(string sd_no)
        {
            ViewBag.sd_no = sd_no;

            return PartialView();
        }
        public JsonResult Get_List_Material_ShippingPickingPage(Pageing paging)
        {
            string mt_no = Request["mt_no"] == null ? "" : Request["mt_no"].Trim();
            string mt_cd = Request["mt_cd"] == null ? "" : Request["mt_cd"].Trim();



            StringBuilder sql = new StringBuilder();

            StringBuilder varname1 = new StringBuilder();
            varname1.Append(" SELECT a.wmtid ,a.mt_cd,a.mt_no,a.lot_no,a.gr_qty ,a.expiry_dt ,a.dt_of_receipt,a.expore_dt  ");
            varname1.Append("FROM w_material_info_tam as a ");
            varname1.Append(" WHERE  a.mt_sts_cd ='000'  AND (a.sd_no IS NULL OR a.sd_no = '') and (a.mt_barcode IS NULL OR a.mt_barcode = '') ");
            varname1.Append(" AND ('" + mt_no + "'='' OR  a.mt_no like '%" + mt_no + "%' ) ");
            varname1.Append(" AND ('" + mt_cd + "'='' OR  a.mt_cd like '%" + mt_cd + "%' ) ");


            //return new InitMethods().ConvertDataTableToJsonAndReturn(varname1);

            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<string>("mt_cd"));
            return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
        }
        public ActionResult Get_List_Material_ShippingPicking(string mt_no = "", string mt_cd = "")
        {
            try
            {
                var data = db.w_material_info_tam.Where(x => x.mt_no.Contains(mt_no) && x.mt_cd.Contains(mt_cd) && string.IsNullOrEmpty(x.sd_no) && x.mt_sts_cd == "000").ToList();
                if (data.Count > 0)
                {
                    var datalist = (from a in data
                                    select new
                                    {
                                        wmtid = a.wmtid,
                                        mt_cd = a.mt_cd,
                                        mt_no = a.mt_no,
                                        lot_no = a.lot_no,
                                        gr_qty = a.gr_qty,
                                        expiry_dt = a.expiry_dt,
                                        dt_of_receipt = a.dt_of_receipt,
                                        expore_dt = a.expore_dt

                                    }
                     ).ToList();

                    return Json(datalist, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = notfind }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }
        public async Task<ActionResult> ShippingPicking_M(string data, string sd_no)
        {
            try
            {
                if (string.IsNullOrEmpty(data))
                {
                    return Json(new { result = false, message = chagain }, JsonRequestBehavior.AllowGet);
                }
                if (string.IsNullOrEmpty(sd_no))
                {
                    return Json(new { result = false, message = chagain + " SD No!!!" }, JsonRequestBehavior.AllowGet);
                }

                string[] temp_id = data.TrimStart('[').TrimEnd(']').Split(',');
                List<int> temp = new List<int>();
                for (int i = 0; i < temp_id.Length; i++)
                {
                    temp.Add(int.Parse(temp_id[i]));
                }

                var kttt_null = db.w_material_info_tam.Any(x => temp.Contains(x.wmtid));
                if (kttt_null == false)
                {
                    return Json(new { result = false, message = exits }, JsonRequestBehavior.AllowGet);
                }

                //UPDATE w_material_info_tam  COLUMN SD
                var sql2 = new StringBuilder();
                sql2.Append(" UPDATE  w_material_info_tam  ")

                .Append("  SET w_material_info_tam.picking_dt='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', w_material_info_tam.sd_no = '" + sd_no + "' , w_material_info_tam.lct_cd = '002000000000000000' , w_material_info_tam.from_lct_cd = '002000000000000000' , w_material_info_tam.lct_sts_cd = '000' ")
                .Append(" WHERE w_material_info_tam.wmtid in (" + data + ") ");

                using (var cmd = db.Database.Connection.CreateCommand())
                {
                    db.Database.Connection.Open();

                    cmd.CommandText = sql2.ToString();
                    var reader = cmd.ExecuteNonQuery();
                    int result1 = Int32.Parse(reader.ToString());
                    db.Database.Connection.Close();
                }


                //UPDATE SD ALERT = 1 và sd_sts_cd = '000'
                var sql3 = new StringBuilder();
                sql3.Append(" UPDATE  w_sd_info  ")

                .Append("  SET w_sd_info.alert = 1 , w_sd_info.sd_sts_cd = '000' ")
                .Append(" WHERE w_sd_info.sd_no in ('" + sd_no + "') ");


                using (var cmd = db.Database.Connection.CreateCommand())
                {
                    db.Database.Connection.Open();

                    cmd.CommandText = sql3.ToString();
                    var reader = cmd.ExecuteNonQuery();
                    int result1 = Int32.Parse(reader.ToString());
                    db.Database.Connection.Close();
                }

                await chat.Invoke<string>("Hello", "010").ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Console.WriteLine("There was an error calling send: {0}",
                                          task.Exception.GetBaseException());
                    }
                    else
                    {
                        Console.WriteLine(task.Result);
                    }
                });

                //get data table 2
                var datalist = (from a in db.w_material_info_tam
                                where temp.Contains(a.wmtid)
                                select new
                                {
                                    wmtid = a.wmtid,
                                    mt_cd = a.mt_cd,
                                    lot_no = a.lot_no,
                                    gr_qty = a.gr_qty,
                                    expiry_dt = a.expiry_dt,
                                    dt_of_receipt = a.dt_of_receipt,
                                    expore_dt = a.expore_dt

                                }
                        ).ToList();

                return Json(new { result = true, message = ss, datalist }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }

        }
        #endregion
        #region  PartialView_Create_List_ML_NO_Info_Popup
        public ActionResult PartialView_Create_List_ML_NO_Info_Popup(string sd_no)
        {
            ViewBag.sd_no = sd_no;

            return PartialView();
        }
        public JsonResult GetInfo_memo(Pageing pageing, string sd_no, string md_cd, string mt_cd, string lot_no, string style_no)
        {
            StringBuilder sql = new StringBuilder($"SELECT a.* FROM w_material_info_memo AS a where a.sd_no='" + sd_no + "'");
            sql.Append(" and ('" + md_cd + "'='' or a.md_cd LIKE '%" + md_cd + "%')");
            sql.Append(" and ('" + mt_cd + "'='' or a.mt_cd LIKE '%" + mt_cd + "%')");
            sql.Append(" and ('" + lot_no + "'='' or a.lot_no LIKE '%" + lot_no + "%')");
            sql.Append(" and ('" + style_no + "'='' or a.style_no LIKE '%" + style_no + "%')");

            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);

            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("id"));
            return new InitMethods().ReturnJsonResultWithPaging(pageing, total, result);

        }

        public JsonResult Upload_ExcelMemory(List<w_material_info_memo> insertmaterial_info_memo, w_material_info_memo w_material_info_memo)
        {
            var data_update = 0;
            var data_create = 0;
            var data_error = 0;
            foreach (var item in insertmaterial_info_memo)
            {
                try
                {
                    w_material_info_memo.TX = (item.TX != null ? item.TX : 0);
                    w_material_info_memo.total_ea = (item.total_ea != null ? item.total_ea : 0);
                    w_material_info_memo.total_m = (item.total_m != null ? item.total_m : 0);
                    w_material_info_memo.total_m2 = (item.total_m2 != null ? item.total_m2 : 0);
                    w_material_info_memo.md_cd = item.md_cd;
                    w_material_info_memo.style_no = item.style_no;
                    w_material_info_memo.style_nm = item.style_nm;
                    w_material_info_memo.mt_cd = item.mt_cd;
                    w_material_info_memo.lot_no = (item.lot_no != null ? item.lot_no.Replace("\r\n", " ") : "");
                    w_material_info_memo.month_excel = item.month_excel;
                    w_material_info_memo.sd_no = item.sd_no;
                    w_material_info_memo.spec = item.spec;
                    w_material_info_memo.spec_unit = item.spec_unit;
                    w_material_info_memo.width = item.width;
                    w_material_info_memo.width_unit = item.width_unit;
                    w_material_info_memo.sts_cd = "001";//stock memo
                    w_material_info_memo.receiving_dt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    w_material_info_memo.use_yn = "Y";
                    w_material_info_memo.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    w_material_info_memo.reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    w_material_info_memo.reg_dt = DateTime.Now;
                    w_material_info_memo.chg_dt = DateTime.Now;
                    db.Entry(w_material_info_memo).State = EntityState.Added;
                    db.SaveChanges();
                    data_create++;
                }
                catch (Exception)
                {

                    data_error++;
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

        public ActionResult InsertCreate_memo(w_material_info_memo w_material_info_memo)
        {
            try
            {
                var find_product = db.d_style_info.Where(x => x.style_no == w_material_info_memo.style_no).ToList();
                if (find_product.Count == 0)
                {
                    return Json(new { result = false, message = notfind + " Product" }, JsonRequestBehavior.AllowGet);

                }

                w_material_info_memo.TX = (w_material_info_memo.TX != null ? w_material_info_memo.TX : 0);
                w_material_info_memo.width = (w_material_info_memo.width != null ? w_material_info_memo.width : 0);
                w_material_info_memo.spec = (w_material_info_memo.spec != null ? w_material_info_memo.spec : 0);

                w_material_info_memo.total_m = (w_material_info_memo.TX * w_material_info_memo.spec);
                w_material_info_memo.total_m2 = (w_material_info_memo.width / 1000

                    * Convert.ToInt32(w_material_info_memo.spec) * w_material_info_memo.TX);
                w_material_info_memo.total_ea = Math.Round(Convert.ToDecimal(Convert.ToDouble(w_material_info_memo.total_m) / 150.8 * 1000 / 1.1 / 1), 0);
                w_material_info_memo.style_nm = find_product.FirstOrDefault().style_nm;
                w_material_info_memo.md_cd = find_product.FirstOrDefault().md_cd;
                w_material_info_memo.sts_cd = "001";
                DateTime dt = DateTime.Now; //Today at 00:00:00
                w_material_info_memo.receiving_dt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                w_material_info_memo.reg_id = Session["authName"] == null ? null : Session["authName"].ToString();
                w_material_info_memo.chg_id = Session["authName"] == null ? null : Session["authName"].ToString();
                w_material_info_memo.width_unit = "MM";
                w_material_info_memo.spec_unit = "M";
                w_material_info_memo.use_yn = "Y";
                w_material_info_memo.reg_dt = DateTime.Now;
                w_material_info_memo.chg_dt = DateTime.Now;

                db.w_material_info_memo.Add(w_material_info_memo);
                db.SaveChanges();
                return Json(new { result = true, data = w_material_info_memo }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }

        }
        public ActionResult UpdateCreate_memo(w_material_info_memo w_material_info_memo)
        {
            try
            {
                var KTTT = db.w_material_info_memo.Find(w_material_info_memo.id);
                if (KTTT == null)
                {
                    return Json(new { result = false, message = exits }, JsonRequestBehavior.AllowGet);
                }
                if (KTTT.style_no != w_material_info_memo.style_no)
                {
                    var find_product = db.d_style_info.Where(x => x.style_no == w_material_info_memo.style_no).ToList();
                    if (find_product.Count == 0)
                    {
                        return Json(new { result = false, message = notfind + " Product" }, JsonRequestBehavior.AllowGet);
                    }
                    KTTT.style_no = find_product.FirstOrDefault().style_no;
                    KTTT.style_nm = find_product.FirstOrDefault().style_nm;
                    KTTT.md_cd = find_product.FirstOrDefault().md_cd;
                }

                KTTT.width = w_material_info_memo.width;
                KTTT.spec = w_material_info_memo.spec;

                KTTT.TX = (w_material_info_memo.TX != null ? w_material_info_memo.TX : 0);
                KTTT.width = (KTTT.width != null ? KTTT.width : 0);
                KTTT.spec = (KTTT.spec != null ? KTTT.spec : 0);

                KTTT.total_m = (KTTT.TX * KTTT.spec);
                KTTT.total_m2 = (KTTT.width / 1000

                    * Convert.ToInt32(KTTT.spec) * KTTT.TX);
                KTTT.total_ea = Math.Round(Convert.ToDecimal(Convert.ToDouble(KTTT.total_m) / 150.8 * 1000 / 1.1 / 1), 0);




                KTTT.mt_cd = w_material_info_memo.mt_cd;
                KTTT.lot_no = w_material_info_memo.lot_no;
                KTTT.memo = w_material_info_memo.memo;
                KTTT.chg_id = Session["authName"] == null ? null : Session["authName"].ToString();
                //KTTT.chg_dt = DateTime.Now;


                db.Entry(KTTT).State = EntityState.Modified;
                db.SaveChanges();


                return Json(new { result = true, data = KTTT }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult DelCreate_memo(w_material_info_memo w_material_info_memo)
        {
            try
            {
                var KTTT = db.w_material_info_memo.Find(w_material_info_memo.id);
                if (KTTT == null)
                {
                    return Json(new { result = false, message = exits }, JsonRequestBehavior.AllowGet);
                }

                db.Entry(KTTT).State = EntityState.Deleted;
                db.SaveChanges();


                return Json(new { result = true, data = KTTT, message = ss }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion
        #endregion

        #region N_API_Shipping_Picking_Scan
        public ActionResult SaveShipping_PickingScan(string sd_no, string ml_no)
        {
            try
            {
                if (string.IsNullOrEmpty(sd_no))
                {
                    return Json(new { result = false, message = chagain + " SD No!!!" }, JsonRequestBehavior.AllowGet);
                }
                if (string.IsNullOrEmpty(ml_no))
                {
                    return Json(new { result = false, message = scanagain + " Lot" }, JsonRequestBehavior.AllowGet);
                }
                var kttt_null = db.w_material_info_tam.Any(x => x.mt_cd == ml_no);
                if (kttt_null == false)
                {
                    return Json(new { result = false, message = exits }, JsonRequestBehavior.AllowGet);
                }
                var KT_SD = db.w_material_info_tam.Any(x => x.mt_cd == ml_no && string.IsNullOrEmpty(x.sd_no) && x.mt_sts_cd == "000");
                if (KT_SD == false)
                {
                    return Json(new { result = false, message = used }, JsonRequestBehavior.AllowGet);
                }
                //UPDATE w_material_info_tam  COLUMN SD
                var sql2 = new StringBuilder();
                sql2.Append(" UPDATE  w_material_info_tam  ")

                .Append("  SET w_material_info_tam.sd_no = '" + sd_no + "' , w_material_info_tam.lct_cd = '002000000000000000' , w_material_info_tam.from_lct_cd = '002000000000000000' , w_material_info_tam.lct_sts_cd = '000' ")
                .Append(" WHERE w_material_info_tam.mt_cd in ('" + ml_no + "') ");

                using (var cmd = db.Database.Connection.CreateCommand())
                {
                    db.Database.Connection.Open();

                    cmd.CommandText = sql2.ToString();
                    var reader = cmd.ExecuteNonQuery();
                    int result1 = Int32.Parse(reader.ToString());
                    db.Database.Connection.Close();
                }

                //UPDATE SD ALERT = 1 và sd_sts_cd = '000'
                var sql3 = new StringBuilder();
                sql3.Append(" UPDATE  w_sd_info  ")

                .Append("  SET w_sd_info.alert = 1 , w_sd_info.sd_sts_cd = '000' ")
                .Append(" WHERE w_sd_info.sd_no in ('" + sd_no + "') ");


                using (var cmd = db.Database.Connection.CreateCommand())
                {
                    db.Database.Connection.Open();

                    cmd.CommandText = sql3.ToString();
                    var reader = cmd.ExecuteNonQuery();
                    int result1 = Int32.Parse(reader.ToString());
                    db.Database.Connection.Close();
                }


                //get data table 1
                var data_sd = (from a in db.w_sd_info
                               where a.sd_no.Equals(sd_no)
                               select new
                               {
                                   sd_no = a.sd_no,
                                   sd_nm = a.sd_nm,
                                   sts_nm = (db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == a.sd_sts_cd).Select(x => x.dt_nm)),
                                   alert = a.alert,
                                   lct_nm = (db.lct_info.Where(x => x.lct_cd == a.lct_cd).Select(x => x.lct_nm)),
                                   remark = a.remark,
                               }
                        ).FirstOrDefault();
                //get data table 2
                var data_material = (from a in db.w_material_info_tam
                                     where a.mt_cd.Equals(ml_no)
                                     select new
                                     {
                                         wmtid = a.wmtid,
                                         mt_cd = a.mt_cd,
                                         lot_no = a.lot_no,
                                         gr_qty = a.gr_qty,
                                         expiry_dt = a.expiry_dt,
                                         dt_of_receipt = a.dt_of_receipt,
                                         expore_dt = a.expore_dt

                                     }
                        ).FirstOrDefault();

                return Json(new { result = true, message = ss, data_sd, data_material }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion
        public ActionResult getadmin(string id)
        {
            var admin = db.mb_info.Where(x => x.userid == id).ToList();
            return Json(admin, JsonRequestBehavior.AllowGet);
        }
        //public JsonResult Test()
        //{

        //        Workbook book = new Workbook();

        //        book.LoadFromFile(“test.xlsx”, ExcelVersion.Version2010);

        //        Worksheet sheet = book.Worksheets[0];



        //        sheet.Copy(sheet.Range[“A1: C1”], sheet.Range[“A9: C9”], true);

        //        sheet.Copy(sheet.Range[“B4: B7”], sheet.Range[“D4: D7”], true);



        //        sheet.Range[“A1: C1”].Clear(ExcelClearOptions.ClearAll);

        //        sheet.Range[“B4: B7”].Clear(ExcelClearOptions.ClearAll);



        //        book.SaveToFile(“result.xlsx”, ExcelVersion.Version2010);

        //        System.Diagnostics.Process.Start(“result.xlsx”);





        //}

        #region PartialView_ListMaterialNoPopup
        public ActionResult PartialView_ListMaterialNoPopup(string sd_no)
        {
            ViewBag.sd_no = sd_no;

            return PartialView();
        }
        [HttpGet]
        public JsonResult Get_ListMaterialNo(Pageing paging)
        {
            string mt_no = Request["mt_no"] == null ? "" : Request["mt_no"].Trim();
            string style_no = Request["style_no"] == null ? "" : Request["style_no"].Trim();
            var data = _IWMSService.GetListMAterialNo(mt_no, style_no);
            var records = data.Count();
            int totalPages = (int)Math.Ceiling((float)records / paging.rows);
            var rowsData = data.Skip((paging.page - 1)).Take(paging.rows);
            return Json(new { total = totalPages, page = paging.page, records = records, rows = rowsData }, JsonRequestBehavior.AllowGet);
        }
        public async Task<ActionResult> insertMaterialShipp(string id, string qty, string meter, string sd_no)
        {
            //insert vào bảng shippingsdmaterial
            try
            {
                if ((id != null) && (qty != null) && (meter != null))
                {
                    var idMaterial = id.TrimStart('[').TrimEnd(']').Split(',');
                    var quantity = qty.TrimStart('[').TrimEnd(']').Split(',');
                    var meters = meter.TrimStart('[').TrimEnd(']').Split(',');
                    bool hasOrder = false;
                    for (int i = 0; i < idMaterial.Length; i++)
                    {
                        var idMaterial1 = int.Parse(idMaterial[i]);
                        var isExistMT = _IWMSService.GetMaterialInfo(idMaterial1);
                        if (isExistMT != null && Convert.ToDouble(quantity[i]) > 0 && Convert.ToDouble(meters[i]) > 0)
                        {
                            shippingsdmaterial item = new shippingsdmaterial();
                            item.sd_no = sd_no;
                            item.mt_no = isExistMT.mt_no.Trim();
                            item.quantity = Convert.ToDouble(quantity[i]);
                            item.meter = Convert.ToDouble(meters[i]);
                            item.reg_id = Session["authName"] == null ? null : Session["authName"].ToString();
                            item.reg_dt = DateTime.Now;
                            hasOrder = true;
                            int b = _IWMSService.InsertToShippingSDMaterial(item);
                        }
                    }
                    if (hasOrder)
                    {
                        //UPDATE SD ALERT = 1 và sd_sts_cd = '000'

                        var user = Session["authName"] == null ? null : Session["authName"].ToString();



                        _IWIPService.UpdatedSdInfo(1, "000", user, sd_no, DateTime.Now);




                        await chat.Invoke<string>("Hello", "010").ContinueWith(task =>
                        {
                            if (task.IsFaulted)
                            {
                                Console.WriteLine("There was an error calling send: {0}",
                                                  task.Exception.GetBaseException());
                            }
                            else
                            {
                                Console.WriteLine(task.Result);
                            }
                        });
                    }
                    return Json(new { result = true, data = sd_no }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { result = false, message = "Vui lòng nhập số lượng để xuất kho" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {

                throw;
            }

        }
        public ActionResult Getshippingsdmaterial(string sd_no)
        {

            //var data = _IWMSService.Getshippingsdmaterial(sd_no);
            var data = _IWIPService.GetListMaterialInfoBySdNo(sd_no);
            return (Json(data, JsonRequestBehavior.AllowGet));

        }

        [HttpPost]
        public async Task<ActionResult> UploadExcel(List<shippingsdmaterial> tempList)
        {
            if (tempList == null)
            {
                return Json(new { flag = false, message = "Chưa chọn File excel" }, JsonRequestBehavior.AllowGet);
            }
            List<shippingsdmaterial> listData = new List<shippingsdmaterial>();

            var sd_no = "";
            if (tempList.Count > 0)
            {

                tempList = tempList.Where(m => m.quantity > 0 && m.meter > 0 && !(string.IsNullOrEmpty(m.mt_no))).ToList();

                //string jsonObject = JsonConvert.SerializeObject(tempList);
                int countSS = 0;
                foreach (var item in tempList)
                {

                    //kieem tra NVL có trong d_material_info khônng/ nếu tồn tại thì insert vào bảng shippingsdmaterial
                    if (!_IWMSService.CheckMaterialInfo(item.mt_no.Trim()))
                    {
                        shippingsdmaterial itemsd = new shippingsdmaterial();
                        item.sd_no = item.sd_no;
                        item.mt_no = item.mt_no.Trim();
                        item.quantity = Convert.ToDouble(item.quantity);
                        item.meter = Convert.ToDouble(item.meter);
                        item.reg_id = Session["authName"] == null ? null : Session["authName"].ToString();
                        item.reg_dt = DateTime.Now;

                        int b = _IWMSService.InsertToShippingSDMaterial(item);
                        if (b > 0)
                        {
                            countSS += 1;
                        }
                        sd_no = item.sd_no;
                    }
                }
                if (countSS > 0)
                {
                    //UPDATE SD ALERT = 1 và sd_sts_cd = '000'

                    var user = Session["authName"] == null ? null : Session["authName"].ToString();

                    _IWIPService.UpdatedSdInfo(1, "000", user, sd_no, DateTime.Now);
                    await chat.Invoke<string>("Hello", "010").ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            Console.WriteLine("There was an error calling send: {0}",
                                              task.Exception.GetBaseException());
                        }
                        else
                        {
                            Console.WriteLine(task.Result);
                        }
                    });
                }
                return Json(new { result = true, data = sd_no, message = "Số liệu được xuất kho là: " + countSS }, JsonRequestBehavior.AllowGet);

            }
            else
            {
                return Json(new { result = false, message = "File excel không hợp lệ." }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region ShowMaterialShipping
        public ActionResult ShowMaterialShipping()
        {
            return SetLanguage("");
        }
        public ActionResult searchShowMaterialShipping(Pageing pageing, string product, string material, string datemonth, string date)
        {

            Dictionary<string, string> list = PagingAndOrderBy(pageing, " ORDER BY  a.product_cd  ");


            IEnumerable<MaterialShipping> Data = _IWIPService.GetListSearchShowMaterialShipping(datemonth, product, material, date);
            //int totalRecords = _IWIPService.TotalRecordsShowMaterialShipping(datemonth, product, material);
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

        }
        public ActionResult getShowMaterialShippingDetail(string style_no, string mt_no, string recei_dt, string datemonth)
        {
            try
            {
                IEnumerable<MaterialShipping> value = _IWIPService.getShowMaterialShippingDetail(style_no, mt_no, recei_dt, datemonth);

                return Json(value, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult GetMemo(Pageing paging, string datemonth, string product, string material, string date)
        {


            Dictionary<string, string> list = PagingAndOrderBy(paging, " ORDER BY  a.product_cd  ");


            IEnumerable<MaterialShippingMemo> Data = _IWIPService.GetListSearchShowMemo(datemonth,product, material, date);
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


            //string MTCode = Request["MTCode"] == null ? "" : Request["MTCode"].Trim();
            //string memoProductCode = Request["memoProductCode"] == null ? "" : Request["memoProductCode"].Trim();
            //StringBuilder sql = new StringBuilder();
            //sql.Append($" SELECT ")
            //    .Append($" MAX(a.mt_cd) AS mt_cd ")
            //    .Append($" , MAX(a.width) AS width ")
            //    .Append($" , MAX(a.spec) AS spec ")
            //    .Append($" , SUM(a.TX) AS total_roll ")
            //    .Append($" , MAX(a.chg_dt) AS chg_dt ")
            //    .Append($" FROM w_material_info_memo a ")
            //    .Append($" WHERE a.mt_cd LIKE '%{MTCode}%' ")
            //    .Append($" AND a.style_no LIKE '%{memoProductCode}%' ")
            //    .Append($" GROUP BY a.width, a.spec,  a.mt_cd ")
            //    .Append($" ; ")
            //    ;
            //DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);

            //int total = dt.Rows.Count;
            //var result = dt.AsEnumerable().OrderByDescending(x => x.Field<DateTime>("chg_dt"));
            //return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
        }
        #region Sử dụng chung

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

        public JsonResult SearchAndPaging(string countSql, string viewSql, int pageIndex, int pageSize)
        {
            int totalRecords = db.Database.SqlQuery<int>(countSql).FirstOrDefault();
            int totalPages = (int)Math.Ceiling((float)totalRecords / (float)pageSize);
            DataTable data = new InitMethods().ReturnDataTableNonConstraints(new StringBuilder(viewSql));
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


        #endregion Sử dụng chung
        #endregion
        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}

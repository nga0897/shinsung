using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using Mvc_VD.Models;
using System.Text;
using Mvc_VD.Classes;

//using OfficeOpenXml;
using System.Web.UI.WebControls;
using System.IO;
using ClosedXML.Excel;
using System.Collections;
using Mvc_VD.Commons;
using Mvc_VD.Services;
using MySql.Data.MySqlClient;
using Mvc_VD.Services.TIMS;
using Mvc_VD.Models.TIMS;
using Mvc_VD.Models.Language;
using System.Threading.Tasks;
using Mvc_VD.Models.FG;

namespace Mvc_VD.Controllers
{
    public class TIMSController : BaseController
    {
        private readonly IEntityService _EntityService;
        private readonly ITIMSService _TIMSService;
        private readonly IWOService _IWOService;
        private readonly IWorkRequestService _WorkRequestServices;
        private readonly Entities db;
        #region Override JsonResult

        public TIMSController(IEntityService EntityService,
                ITIMSService TIMSService,
                IWOService IWOService,
                IDbFactory DbFactory,
                IWorkRequestService WorkRequestServices
                )
        {
            _EntityService = EntityService;
            _TIMSService = TIMSService;
            _IWOService = IWOService;
            _WorkRequestServices = WorkRequestServices;
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
        #region N_Receiving_Scan(TIMS)

        public ActionResult Receving_Scan()
        {
            return SetLanguage("~/Views/TIMS/ReceivingScan/Receving_Scan.cshtml");
           
        }

        #region rd_info_old

        public ActionResult GetRDInfo(string rd_no, string rd_nm)
        {
            var sql = new StringBuilder();
            sql.Append(" SELECT a.* ")
               .Append(" FROM w_rd_info as a ")
                .Append(" WHERE a.use_yn ='Y' ")
                 .Append("AND ('" + rd_no + "'='' OR  a.rd_no like '%" + rd_no + "%' )")
                 .Append("AND ('" + rd_nm + "'='' OR  a.rd_nm like '%" + rd_nm + "%' )")

                .Append(" order by rid desc ");

            return new InitMethods().ConvertDataTableToJsonAndReturn(sql);
        }
        public ActionResult InsertRDInfo(w_rd_info w_rd_info)
        {
            JsonResult data = _TIMSService.InsertRDInfo(w_rd_info);
            return Json(new { result = true, data = data.Data }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult UpdateRDInfo(w_rd_info w_rd_info)
        {
            try
            {
                w_rd_info data = _TIMSService.UpdateRDInfo(w_rd_info);
                if (data == null)
                    return Json(new { result = false, message = Constant.DataExist }, JsonRequestBehavior.AllowGet);
                return Json(new { result = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult DeleteRDInfo(w_rd_info w_rd_info)
        {
            try
            {
                WRdInfo data = _TIMSService.DeleteRDInfo(w_rd_info);
                if (!(data.rd_sts_cd.Equals("000")))
                {
                    return Json(new { result = false, message = Constant.Status }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = true, data = data, message = Constant.Success }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion rd_info_old

        public ActionResult GetPOInfo(string at_no)
        {
            var sql = new StringBuilder();
            sql.Append(" SELECT a.* ")

               .Append(" FROM w_actual_primary as a ")
                 .Append("WHERE ('" + at_no + "'='' OR  a.at_no like '%" + at_no + "%' )")
                .Append(" order by id_actualpr desc ");

            return new InitMethods().ConvertDataTableToJsonAndReturn(sql);
        }

        public JsonResult Get_List_Material_TimsReceiving_PO(string po_no, string product, string input_dt, string bb_no)
        {
            IEnumerable<TimsReceivingScanModel> data = _TIMSService.GetListMaterialTimsReceivingPO(po_no, product, input_dt, bb_no);
            bool check = true;
            if (data.Count() > 0)
                check = true;
            return Json(new { data = data, result = check, message = "" }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetTimsReceiScanMLQR(string bb_no)
        {
            try
            {
                if (string.IsNullOrEmpty(bb_no))
                {
                    return Json(new { result = false, message = "Làm ơn kiểm tra lại đồ đựng rỗng" }, JsonRequestBehavior.AllowGet);
                }
                var kttt_null = db.d_bobbin_lct_hist.Any(x => x.bb_no == bb_no);
                if (kttt_null == false)
                {
                    return Json(new { result = false, message = "Không tìm thấy đồ đựng này!!!" }, JsonRequestBehavior.AllowGet);
                }

                var L_ml_no_bb = db.d_bobbin_lct_hist.Where(x => x.bb_no == bb_no).Select(x => x.mt_cd);

                var count_rowpan = db.w_material_info.Count(a => L_ml_no_bb.Contains(a.mt_cd)
                        && a.lct_cd.StartsWith("002")
                        && (a.mt_sts_cd == "001" || a.mt_sts_cd == "002")
                       && a.gr_qty > 0);

                if (count_rowpan == 0)
                {
                    return Json(new { result = false, message = "Đồ đựng này đã được sử dụng!!!" }, JsonRequestBehavior.AllowGet);
                }

                var dsCommomlct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002");
                var dsCommommt_type_nm = db.comm_dt.Where(x => x.mt_cd == "COM004");
                var dsCommommt_sts_nm = db.comm_dt.Where(x => x.mt_cd == "WHS005");

                var data = (from a in db.w_material_info
                            where L_ml_no_bb.Contains(a.mt_cd)
                            && a.lct_cd.StartsWith("002")
                            && (a.mt_sts_cd == "001" || a.mt_sts_cd == "002")
                           && a.gr_qty > 0
                            select new
                            {
                                wmtid = a.wmtid,
                                mt_cd = a.mt_cd,
                                bb_no = a.bb_no,
                                gr_qty = a.gr_qty,
                                recevice_dt_tims = a.input_dt,

                                from_lct_cd = a.from_lct_cd,
                                from_lct_nm = db.lct_info.Where(x => x.lct_cd == a.lct_cd).Select(x => x.lct_nm),

                                lct_sts_cd = dsCommomlct_sts_cd.Where(x => x.dt_cd == a.lct_sts_cd).Select(x => x.dt_nm),

                                mt_type_nm = dsCommommt_type_nm.Where(x => x.dt_cd == a.mt_type).Select(x => x.dt_nm),
                                mt_type = a.mt_type,

                                mt_sts_cd = a.mt_sts_cd,
                                sts_nm = dsCommommt_sts_nm.Where(x => x.dt_cd == a.mt_sts_cd).Select(x => x.dt_nm),
                                rowpan = count_rowpan,
                            }
                      ).ToList();

                return Json(new { result = true, message = Constant.Success, data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
            
        }

        public ActionResult UpdateMTQR_RDList(string bb_no)
        {
            try
            {
                if (string.IsNullOrEmpty(bb_no))
                {
                    return Json(new { result = false, message = Constant.ScanAgain }, JsonRequestBehavior.AllowGet);
                }

                
                var bobin = _TIMSService.FindOneDBobbinInfo(bb_no);
                if (bobin == null)
                {
                    return Json(new { result = false, message = "Đồ đựng này đã bị xóa khỏi hệ thống!Vui lòng kiểm tra lại!!!" }, JsonRequestBehavior.AllowGet);
                }
                
                var history = _TIMSService.FindOneBobbin_lct_hist(bb_no);
                if (history == null)
                {
                    return Json(new { result = false, message = "Đồ đựng này không tìm thấy!Vui lòng kiểm tra lại!!!" }, JsonRequestBehavior.AllowGet);
                }
                
                var mt_cd = _TIMSService.FindOneMaterialInfoByMTCdBBNo(history.mt_cd, bb_no);
                if (mt_cd == null)
                {
                    return Json(new { result = false, message = "Không tìm thấy hàng chứa trong đồ đựng này!!!" }, JsonRequestBehavior.AllowGet);
                }
                if (mt_cd.lct_cd.StartsWith("006"))
                {
                    return Json(new { result = false, message = "Đồ Đựng này đã được chuyển vào kho TIMS!!!" }, JsonRequestBehavior.AllowGet);
                }
                if (mt_cd.lct_cd.StartsWith("003"))
                {
                    return Json(new { result = false, message = "Đồ Đựng này đã được chuyển vào kho FG!!!" }, JsonRequestBehavior.AllowGet);
                }
                if ((mt_cd.mt_sts_cd == "001" && mt_cd.mt_sts_cd == "002") && mt_cd.lct_cd.StartsWith("002") && mt_cd.gr_qty > 0)
                {
                    return Json(new { result = false, message = "Không tồn tại trong danh sách này!!!" }, JsonRequestBehavior.AllowGet);
                }
                
               
                var data = _TIMSService.FindAllTimsReceivingScanByInBBNo(bb_no).ToList();
                int total = data.Count;
                if (total == 0)
                {
                    return Json(new { result = false, message = "Không tìm thấy đồ đựng này!!!" }, JsonRequestBehavior.AllowGet);
                }
                

                DateTime dt = DateTime.Now;
                string day_now = dt.ToString("yyyyMMdd");
                string full_date = dt.ToString("yyyy-MM-dd HH:mm:ss");

                var user = Session["authName"] == null ? null : Session["authName"].ToString();

                int result = _TIMSService.UpdateMTQR_RDList(full_date, user, bb_no);
                return Json(new { result = true, message = "Thành công!!!", Data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        #region old

        public ActionResult UpdateScan(string mt_barcode)
        {
            string message = "";

            string sqlCount = " SELECT * "
                + " FROM w_material_info AS a "

                + " WHERE a.mt_sts_cd LIKE '3000' and a.mt_cd='" + mt_barcode + "' and cast(a.gr_qty AS INT ) > 0  ";

            ;

            var condition_w_material = db.Database.SqlQuery<w_material_info>(sqlCount).ToList().Count();

            if (condition_w_material == 0)
            {
                message = "Material: " + mt_barcode + " no data";
            };

            if ((condition_w_material != 0))
            {
                w_material_info W_MT = db.w_material_info.Where(x => x.mt_cd == mt_barcode).FirstOrDefault();

                W_MT.mt_sts_cd = "008";

                W_MT.use_yn = "Y";
                W_MT.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                W_MT.recevice_dt_tims = DateTime.Now.ToString("yyyyMMddHHmmss");
                db.Entry(W_MT).State = EntityState.Modified;
                db.SaveChanges();
                var list = (from p in db.w_material_info
                            join s in db.d_material_info on p.mt_no equals s.mt_no
                            join c in db.comm_dt on p.mt_sts_cd equals c.dt_cd
                            join d in db.comm_dt on p.lct_sts_cd equals d.dt_cd
                            where p.mt_barcode == mt_barcode && p.mt_sts_cd == "008" && c.mt_cd == "WHS005"
                            select new
                            {
                                wmtid = p.wmtid,
                                mt_cd = p.mt_cd,
                                mt_no = p.mt_no,
                                mt_nm = s.mt_nm,
                                mt_type = s.mt_type,
                                gr_qty = p.gr_qty,
                                unit_cd = s.unit_cd,
                                mt_sts_cd = (db.comm_dt.Where(item => item.mt_cd == "WHS005" && item.dt_cd == p.mt_sts_cd).Select(x => x.dt_nm)),

                                chg_dt = p.chg_dt,
                            }).FirstOrDefault();

                return Json(new { message = message, data = list }, JsonRequestBehavior.AllowGet);
            }
            return Json(new
            {
                result = false,
                message = message
            }, JsonRequestBehavior.AllowGet);
        }

        #endregion old

        #region RD_Info_popup

        public ActionResult PartialView_RD_Info_Popup(string rd_no)
        {
            ViewBag.rd_no = rd_no;
            return PartialView("~/Views/TIMS/ReceivingScan/PartialView_RD_Info_Popup.cshtml");
        }

        public ActionResult GetTimsReceivingScanPP(string rd_no)
        {
            var listdata = (from a in db.w_rd_info

                            where a.rd_no.Equals(rd_no)

                            select new
                            {
                                rd_no = a.rd_no,
                                rd_nm = a.rd_nm,
                                rd_sts_cd = a.rd_sts_cd,
                                remark = a.remark,
                            }).ToList();

            return Json(new { data = listdata }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetTimsReceivingScanListPP(string rd_no)
        {
            try
            {
                var data1 = (from a in db.w_material_info
                             where a.rd_no.Equals(rd_no)
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

        public ActionResult PrintRD_LIST(string rd_no)
        {
            ViewData["Message"] = rd_no;
            return PartialView("~/Views/TIMS/ReceivingScan/PrintRD_LIST.cshtml");
        }

        #endregion RD_Info_popup

        #region N_PartialView_List_ML_NO_Info_Popup

        public ActionResult PartialView_List_ML_NO_Info_Popup(string rd_no)
        {
            ViewBag.rd_no = rd_no;
            return PartialView("~/Views/TIMS/ReceivingScan/PartialView_List_ML_NO_Info_Popup.cshtml");
        }

        public ActionResult Get_List_Material_TimsReceiving(string mt_no = "", string mt_cd = "", string bb_no = "")
        {
            try
            {
                var KT_List_bb = db.d_bobbin_lct_hist.Select(x => x.bb_no);
                var KT_List_mt_cd = db.d_bobbin_lct_hist.Where(x => KT_List_bb.Contains(x.bb_no)).Select(x => x.mt_cd);

                if (KT_List_bb == null)
                {
                    return Json(new { result = false, message = Constant.NotFound + " Container" }, JsonRequestBehavior.AllowGet);
                }

                var dsCommomlct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002");
                var dsCommommt_type_nm = db.comm_dt.Where(x => x.mt_cd == "COM004");
                var dsCommommt_sts_nm = db.comm_dt.Where(x => x.mt_cd == "WHS005");

                var data1 = (from a in db.w_material_info
                             where KT_List_bb.Contains(a.bb_no)
                                && KT_List_mt_cd.Contains(a.mt_cd)
                                && a.bb_no.Contains(bb_no)
                                && a.mt_cd.Contains(mt_cd)
                                && a.mt_no.Contains(mt_no)
                                && string.IsNullOrEmpty(a.rd_no)
                                && a.lct_cd.StartsWith("002")
                                && (a.mt_sts_cd == "001" || a.mt_sts_cd == "002") && a.gr_qty > 0

                             select new
                             {
                                 wmtid = a.wmtid,
                                 mt_cd = a.mt_cd,
                                 mt_no = a.mt_no,
                                 bb_no = a.bb_no,
                                 gr_qty = a.gr_qty,
                                 recevice_dt_tims = a.input_dt,

                                 from_lct_cd = a.from_lct_cd,
                                 from_lct_nm = db.lct_info.Where(x => x.lct_cd == a.lct_cd).Select(x => x.lct_nm),

                                 lct_sts_cd = dsCommomlct_sts_cd.Where(x => x.dt_cd == a.lct_sts_cd).Select(x => x.dt_nm),

                                 mt_type_nm = dsCommommt_type_nm.Where(x => x.dt_cd == a.mt_type).Select(x => x.dt_nm),

                                 mt_type = a.mt_type,

                                 mt_sts_cd = a.mt_sts_cd,
                                 sts_nm = dsCommommt_sts_nm.Where(x => x.dt_cd == a.mt_sts_cd).Select(x => x.dt_nm),
                             }
                      ).ToList();

                if (data1.Count == 0)
                {
                    return Json(new { result = false, message = Constant.Used }, JsonRequestBehavior.AllowGet);
                }

                return Json(data1, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Receving_Scan_M(string data, string rd_no)
        {
            try
            {
                
                if (string.IsNullOrEmpty(rd_no))
                {
                    return Json(new { result = false, message = Constant.ChooseAgain + " RD No" }, JsonRequestBehavior.AllowGet);
                }
                if (string.IsNullOrEmpty(data))
                {
                    return Json(new { result = false, message = Constant.ChooseAgain + " Lot" }, JsonRequestBehavior.AllowGet);
                }
                string[] temp_id = data.TrimStart('[').TrimEnd(']').Split(',');
                List<int> temp = new List<int>();
                for (int i = 0; i < temp_id.Length; i++)
                {
                    temp.Add(int.Parse(temp_id[i]));
                }

                var kttt_null = db.w_material_info.Any(x => temp.Contains(x.wmtid));
                if (kttt_null == false)
                {
                    return Json(new { result = false, message = Constant.DataExist }, JsonRequestBehavior.AllowGet);
                }
                
                DateTime dt = DateTime.Now;
                string day_now = dt.ToString("yyyyMMdd");
                string full_date = dt.ToString("yyyy-MM-dd HH:mm:ss");
                var user = Session["authName"] == null ? null : Session["authName"].ToString();

                var sql2 = new StringBuilder();
                sql2.Append(" UPDATE  w_material_info  ")

                .Append("  SET w_material_info.rd_no = '" + rd_no + "' , w_material_info.mt_sts_cd = '008', ")
                .Append("   w_material_info.lct_cd = '006000000000000000' , w_material_info.recevice_dt_tims = '" + day_now + "' ,")
                .Append("   w_material_info.to_lct_cd = '006000000000000000' ,w_material_info.chg_id = '" + user + "'  ,w_material_info.chg_dt = '" + full_date + "'  ")

                .Append(" WHERE w_material_info.wmtid in (" + data + ") ");

                using (var cmd = db.Database.Connection.CreateCommand())
                {
                    db.Database.Connection.Open();

                    cmd.CommandText = sql2.ToString();
                    var reader = cmd.ExecuteNonQuery();
                    int result1 = Int32.Parse(reader.ToString());
                    db.Database.Connection.Close();
                }
                var datalist = (from a in db.w_material_info

                                where temp.Contains(a.wmtid)
                                select new
                                {
                                    wmtid = a.wmtid,
                                    mt_cd = a.mt_cd,
                                    bb_no = a.bb_no,
                                    gr_qty = a.gr_qty,
                                    recevice_dt_tims = a.input_dt,

                                    from_lct_cd = a.from_lct_cd,
                                    from_lct_nm = db.lct_info.Where(x => x.lct_cd == a.lct_cd).Select(x => x.lct_nm),

                                    lct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002" && x.dt_cd == a.lct_sts_cd).Select(x => x.dt_nm),

                                    mt_type_nm = db.comm_dt.Where(x => x.mt_cd == "COM004" && x.dt_cd == a.mt_type).Select(x => x.dt_nm),
                                    mt_type = a.mt_type,

                                    mt_sts_cd = a.mt_sts_cd,
                                    sts_nm = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == a.mt_sts_cd).Select(x => x.dt_nm),
                                }
                  ).ToList();

                return Json(new { result = true, message = Constant.Success, datalist }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion N_PartialView_List_ML_NO_Info_Popup

        #endregion N_Receiving_Scan(TIMS)

        #region N_API_Receiving_Scan

        public ActionResult Scan_Receiving_Tims(string bb_no, string rd_no)
        {
            try
            {
                if (string.IsNullOrEmpty(bb_no))
                {
                    return Json(new { result = false, message = Constant.ScanAgain + " Container" }, JsonRequestBehavior.AllowGet);
                }
                if (string.IsNullOrEmpty(rd_no))
                {
                    return Json(new { result = false, message = Constant.ChooseAgain + " RD No!!!" }, JsonRequestBehavior.AllowGet);
                }
                var kttt_null = db.d_bobbin_lct_hist.Any(x => x.bb_no == bb_no);
                if (kttt_null == false)
                {
                    return Json(new { result = false, message = Constant.DataExist }, JsonRequestBehavior.AllowGet);
                }
                var L_ml_no_bb = db.d_bobbin_lct_hist.Where(x => x.bb_no == bb_no).Select(x => x.mt_cd);

                var KT_ML = db.w_material_info.Where(x => L_ml_no_bb.Contains(x.mt_cd) && string.IsNullOrEmpty(x.rd_no) && x.lct_cd.Contains("002") && (x.mt_sts_cd == "001" || x.mt_sts_cd == "002") && x.gr_qty > 0).ToList();
                if (KT_ML.Count == 0)
                {
                    return Json(new { result = false, message = Constant.Used }, JsonRequestBehavior.AllowGet);
                }
                var html = "";

                for (int i = 0; i < KT_ML.Count; i++)
                {
                    html += KT_ML[i].wmtid;
                    if (i != KT_ML.Count - 1)
                    {
                        html += ',';
                    }
                }

                DateTime dt = DateTime.Now;
                string day_now = dt.ToString("yyyyMMdd");
                var user = Session["authName"] == null ? null : Session["authName"].ToString();
                string full_date = dt.ToString("yyyy-MM-dd HH:mm:ss");
                var sql2 = new StringBuilder();
                sql2.Append(" UPDATE  w_material_info  ")

                .Append("  SET w_material_info.rd_no = '" + rd_no + "' , w_material_info.mt_sts_cd = '008', ")
                .Append("   w_material_info.lct_cd = '006000000000000000' , w_material_info.recevice_dt_tims = '" + day_now + "' ,")
                .Append("   w_material_info.to_lct_cd = '006000000000000000' ,w_material_info.chg_id = '" + user + "'  ,w_material_info.chg_dt = '" + full_date + "'  ")

                .Append(" WHERE w_material_info.wmtid in (" + html + ") ");

                using (var cmd = db.Database.Connection.CreateCommand())
                {
                    db.Database.Connection.Open();

                    cmd.CommandText = sql2.ToString();
                    var reader = cmd.ExecuteNonQuery();
                    int result1 = Int32.Parse(reader.ToString());
                    db.Database.Connection.Close();
                }
                var dsml_remail = KT_ML.Select(x => x.mt_cd);

                var data = (from a in db.w_material_info
                            where dsml_remail.Contains(a.mt_cd)
                            select new
                            {
                                wmtid = a.wmtid,
                                mt_cd = a.mt_cd,
                                bb_no = a.bb_no,
                                gr_qty = a.gr_qty,
                                recevice_dt_tims = a.input_dt,

                                from_lct_cd = a.from_lct_cd,
                                from_lct_nm = db.lct_info.Where(x => x.lct_cd == a.lct_cd).Select(x => x.lct_nm),

                                lct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002" && x.dt_cd == a.lct_sts_cd).Select(x => x.dt_nm),

                                mt_type_nm = db.comm_dt.Where(x => x.mt_cd == "COM004" && x.dt_cd == a.mt_type).Select(x => x.dt_nm),
                                mt_type = a.mt_type,

                                mt_sts_cd = a.mt_sts_cd,
                                sts_nm = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == a.mt_sts_cd).Select(x => x.dt_nm),
                            }
                      ).ToList();

                return Json(new { result = true, message = Constant.Success, data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion N_API_Receiving_Scan

        #region WorkRequest

        public ActionResult DeliveryPlan()
        {
            return View();
        }

        public JsonResult search_delivery_detail()
        {
            var style_no = Request["style_no"];
            var dateConvert = new DateTime();
            try
            {
                var start = Request["st_date"];
                var end = Request["end_date"];
                if (DateTime.TryParse(start, out dateConvert))
                {
                    start = dateConvert.ToString("yyyyMMdd");
                }
                if (DateTime.TryParse(end, out dateConvert))
                {
                    end = dateConvert.ToString("yyyyMMdd");
                }
                var data = _WorkRequestServices.Search_Delivery_Detail(style_no, start, end);
                var result = GetJsonPersons(data);
                return Json(result.Data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetWorkRequest(string po_no, string delivery_dt)
        {
            var data = _WorkRequestServices.GetWorkRequest(po_no, delivery_dt);
            var result = GetJsonPersons(data);
            return result;
        }

        #endregion WorkRequest

        #region N_Shipping_Scan(TIMS)

        public ActionResult Shipping_Scan(string code)
        {
            ViewData["Message"] = code;
            return View("~/Views/TIMS/ShippingScan/Shipping_Scan.cshtml");
        }

        public ActionResult GetEXTInfo(string ext_no, string ext_nm)
        {
            var sql = new StringBuilder();
            sql.Append(" SELECT a.* ")

               .Append(" FROM w_ext_info as a ")
                .Append(" WHERE a.use_yn ='Y' ")
                 .Append("AND ('" + ext_no + "'='' OR  a.ext_no like '%" + ext_no + "%' )")
                 .Append("AND ('" + ext_nm + "'='' OR  a.ext_nm like '%" + ext_nm + "%' )")

                .Append(" order by extid desc ");

            return new InitMethods().ConvertDataTableToJsonAndReturn(sql);
        }

        public ActionResult InsertEXTInfo(w_ext_info w_ext_info)
        {
            #region Tang tự động

            String ext_no = "EXT1";

            var ext_no_last = db.w_ext_info.ToList().LastOrDefault();
            if (ext_no_last != null)
            {
                var ext_noCode = ext_no_last.ext_no;
                ext_no = string.Concat("EXT", (int.Parse(ext_noCode.Substring(3)) + 1).ToString());
            }

            #endregion Tang tự động

            w_ext_info.ext_no = ext_no;
            w_ext_info.lct_cd = "006000000000000000";
            w_ext_info.alert = 0;

            DateTime dt = DateTime.Now;
            string day_now = dt.ToString("yyyyMMdd");
            w_ext_info.work_dt = day_now;

            w_ext_info.ext_sts_cd = "000";
            w_ext_info.reg_id = Session["authName"] == null ? null : Session["authName"].ToString();
            w_ext_info.chg_id = Session["authName"] == null ? null : Session["authName"].ToString();

            w_ext_info.use_yn = "Y";

            w_ext_info.reg_dt = DateTime.Now;
            w_ext_info.chg_dt = DateTime.Now;

            db.w_ext_info.Add(w_ext_info);
            db.SaveChanges();

            var sql = new StringBuilder();
            sql.Append(" SELECT a.* ")
               .Append(" FROM w_ext_info as a ")
                .Append(" WHERE a.ext_no='" + ext_no + "'  ")
                .Append(" order by extid desc ");

            var ddd = new InitMethods().ConvertDataTableToJsonAndReturn(sql);
            return Json(new { result = true, data = ddd.Data }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult UpdateEXTInfo(w_ext_info w_ext_info)
        {
            try
            {
                var KTTT = db.w_ext_info.Find(w_ext_info.extid);
                if (KTTT == null)
                {
                    return Json(new { result = false, message = Constant.DataExist }, JsonRequestBehavior.AllowGet);
                }
                KTTT.ext_nm = w_ext_info.ext_nm;
                KTTT.remark = w_ext_info.remark;

                KTTT.chg_id = Session["authName"] == null ? null : Session["authName"].ToString();
                db.Entry(KTTT).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { result = true, data = KTTT }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult DeleteEXTInfo(w_ext_info w_ext_info)
        {
            try
            {
                var KTTT = db.w_ext_info.Find(w_ext_info.extid);
                if (KTTT == null)
                {
                    return Json(new { result = false, message = Constant.DataExist }, JsonRequestBehavior.AllowGet);
                }
                if (!(KTTT.ext_sts_cd.Equals("000")))
                {
                    return Json(new { result = false, message = Constant.Status }, JsonRequestBehavior.AllowGet);
                }
                DateTime dt = DateTime.Now;
                string day_now = dt.ToString("yyyyMMdd");
                string full_date = dt.ToString("yyyy-MM-dd HH:mm:ss");
                var user = Session["authName"] == null ? null : Session["authName"].ToString();
                //UPDATE sd_list

                var sql1 = new StringBuilder();
                sql1.Append(" UPDATE  w_material_info  ")
                    .Append(" SET w_material_info.ext_no = '' , w_material_info.mt_sts_cd = '010',")
                    .Append("  w_material_info.to_lct_cd = '006000000000000000' , w_material_info.to_lct_cd = '003G01000000000000',w_material_info.chg_id = '" + user + "'  ,w_material_info.chg_dt = '" + full_date + "'")
                    .Append(" WHERE w_material_info.ext_no ='" + KTTT.ext_no + "' ");

                using (var cmd = db.Database.Connection.CreateCommand())
                {
                    db.Database.Connection.Open();
                    cmd.CommandText = sql1.ToString();
                    var reader = cmd.ExecuteNonQuery();
                    int result1 = Int32.Parse(reader.ToString());
                    db.Database.Connection.Close();
                }
                db.Entry(KTTT).State = EntityState.Deleted;
                db.SaveChanges();

                return Json(new { result = true, data = KTTT, message = Constant.Success }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetTimsShippingScanMLQR(string buyer_qr)
        {
            try
            {


                if (string.IsNullOrEmpty(buyer_qr))
                {
                    return Json(new { result = false, message = Constant.ScanAgain }, JsonRequestBehavior.AllowGet);
                }

                var data = (from a in db.w_material_info
                            where a.buyer_qr.Equals(buyer_qr)
                             && string.IsNullOrEmpty(a.ext_no)
                             && a.mt_sts_cd.Equals("010")
                            && a.lct_cd.StartsWith("006") && a.gr_qty > 0

                            select new
                            {
                                wmtid = a.wmtid,
                                mt_cd = a.mt_cd,
                                bb_no = a.bb_no,
                                buyer_qr = a.buyer_qr,
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

                if (data.Count == 0)
                {
                    return Json(new { result = false, message = Constant.Used }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { result = true, message = Constant.DataExist, data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult UpdateMTQR_EXTList(string data, string ext_no)
        {
            try
            {
                
                DateTime dt = DateTime.Now;
                string day_now = dt.ToString("yyyyMMdd");
                string full_date = dt.ToString("yyyy-MM-dd HH:mm:ss");
                var user = Session["authName"] == null ? null : Session["authName"].ToString();

                var sql2 = new StringBuilder();
                sql2.Append(" UPDATE  w_material_info  ")

                .Append("  SET w_material_info.ext_no = '" + ext_no + "' , w_material_info.mt_sts_cd = '000', ")
                .Append("   w_material_info.lct_cd = '003G01000000000000' , w_material_info.recevice_dt_tims = '" + day_now + "' ,")
                .Append("   w_material_info.to_lct_cd = '003G01000000000000' ,w_material_info.chg_id = '" + user + "'  ,w_material_info.chg_dt = '" + full_date + "'  ")

                .Append(" WHERE w_material_info.wmtid in (" + data + ") ");

                using (var cmd = db.Database.Connection.CreateCommand())
                {
                    db.Database.Connection.Open();

                    cmd.CommandText = sql2.ToString();
                    var reader = cmd.ExecuteNonQuery();
                    int result1 = Int32.Parse(reader.ToString());
                    db.Database.Connection.Close();
                }

                var sql3 = new StringBuilder();
                sql3.Append(" UPDATE  w_ext_info  ")

                .Append("  SET w_ext_info.alert = 1")
                .Append(" WHERE w_ext_info.ext_no in ('" + ext_no + "') ");

                using (var cmd = db.Database.Connection.CreateCommand())
                {
                    db.Database.Connection.Open();

                    cmd.CommandText = sql3.ToString();
                    var reader = cmd.ExecuteNonQuery();
                    int result1 = Int32.Parse(reader.ToString());
                    db.Database.Connection.Close();
                }

                return Json(new { result = true, message = Constant.Success }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }


        //public JsonResult Get_Status_Bobin(string bb_no)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(bb_no))
        //        {
        //            return Json(new { result = false, message = Constant.ScanAgain }, JsonRequestBehavior.AllowGet);
        //        }
        //        var bb_noo = bb_no.Trim();

        //        var KIEMTRABUYER = _TIMSService.GetOneDBWithBuyer(bb_noo);

        //        if (KIEMTRABUYER > 0)
        //        {
        //            return Json(new { result = false, message = Constant.MappingBuyerSuccess }, JsonRequestBehavior.AllowGet);
        //        }

        //        var ds_bb = _IWOService.GetBobbinInfo(bb_noo);
        //        if (ds_bb == null)
        //        {


        //            return Json(new { result = false, message = Constant.DataExist + " " + Constant.ScanAgain }, JsonRequestBehavior.AllowGet);
        //        }

        //        string sql = @"CALL Pro_GetInfoBobin(@1);";
        //        var data = db.Database.SqlQuery<StatusBobbin>(sql, new MySqlParameter("@1", bb_noo));
        //        return Json(new { result = true, message = Constant.Success, Data = data }, JsonRequestBehavior.AllowGet);
        //    }
        //    catch (Exception)
        //    {
        //        return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
        //    }
        //}
        public JsonResult Get_Status_Bobin(string bb_no)
        {
            try
            {
                if (string.IsNullOrEmpty(bb_no))
                {
                    return Json(new { result = false, message = Constant.ScanAgain, Data = new List<StatusBobbin>() }, JsonRequestBehavior.AllowGet);
                }
                var bb_noo = bb_no.Trim();

                var KIEMTRABUYER = _TIMSService.GetOneDBWithBuyer(bb_noo);

                if (KIEMTRABUYER > 0)
                {
                    return Json(new { result = false, message = Constant.MappingBuyerSuccess, Data = new List<StatusBobbin>() }, JsonRequestBehavior.AllowGet);
                }

                var ds_bb = _IWOService.GetBobbinInfo(bb_noo);
                if (ds_bb == null)
                {


                    return Json(new { result = false, message = Constant.DataExist + " " + Constant.ScanAgain, Data = new List<StatusBobbin>() }, JsonRequestBehavior.AllowGet);
                }

                string sql = @"CALL Pro_GetInfoBobin(@1);";
                var data = db.Database.SqlQuery<StatusBobbin>(sql, new MySqlParameter("@1", bb_noo));
                //string data = "";
                if (data == null)
                {
                    return Json(new { result = false, message = "Dữ liệu đã tồn tại!!! Vui lòng Scan Lại!!!", Data = new List<StatusBobbin>() }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { result = true, message = Constant.Success, Data = data }, JsonRequestBehavior.AllowGet);
                }

            }
            catch (Exception)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }


        public JsonResult Get_Status_Buyer(string buyerCode)
        {
            try
            {
                if (string.IsNullOrEmpty(buyerCode))
                {
                    return Json(new { result = false, message = Constant.ScanAgain }, JsonRequestBehavior.AllowGet);
                }
                var bb_noo = buyerCode.Trim();
                //kt exist

                if (!db.w_material_info.Any(x => x.buyer_qr == buyerCode))
                {
                    return Json(new { result = false, message = "Mã tem gói này chưa được mapping" }, JsonRequestBehavior.AllowGet);
                }

                StringBuilder sql = new StringBuilder($"CALL Pro_GetInfoBuyer('{buyerCode}');");

                var data = new InitMethods().ConvertDataTableToJsonAndReturn(sql);

                return Json(new { result = true, message = Constant.Success, Data = data.Data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
        }

        #region EXT_Info_popup

        public ActionResult PartialView_EXT_Info_Popup(string ext_no)
        {
            ViewBag.ext_no = ext_no;
            return PartialView("~/Views/TIMS/ShippingScan/PartialView_EXT_Info_Popup.cshtml");
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

        #region N_PartialView_GetList_ML_NO_Tims_Shipping_PP

        public ActionResult PartialView_GetList_ML_NO_Tims_Shipping_PP(string ext_no)
        {
            ViewBag.ext_no = ext_no;
            return PartialView("~/Views/TIMS/ShippingScan/PartialView_GetList_ML_NO_Tims_Shipping_PP.cshtml");
        }

        public ActionResult Get_List_Material_TimsShipping(Pageing paging, string mt_no = "", string mt_cd = "", string buyer_qr = "")
        {
            try
            {
                StringBuilder sql = new StringBuilder($"CALL spTIMS_Shipping_GetList_Ml_no('{buyer_qr}','{mt_cd}','{mt_no}');");
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

        public ActionResult TimsShipping_Scan_M(string data, string ext_no)
        {
            try
            {
              
                if (string.IsNullOrEmpty(ext_no))
                {
                    return Json(new { result = false, message = Constant.ChooseAgain + " EXT No" }, JsonRequestBehavior.AllowGet);
                }
                if (string.IsNullOrEmpty(data))
                {
                    return Json(new { result = false, message = Constant.CheckData }, JsonRequestBehavior.AllowGet);
                }

                DateTime dt = DateTime.Now;
                string day_now = dt.ToString("yyyyMMdd");
                string full_date = dt.ToString("yyyy-MM-dd HH:mm:ss");
                var user = Session["authName"] == null ? null : Session["authName"].ToString();

                var sql2 = new StringBuilder();
                sql2.Append(" UPDATE  w_material_info  ")

                .Append("  SET w_material_info.ext_no = '" + ext_no + "' , w_material_info.mt_sts_cd = '000', ")
                .Append("   w_material_info.lct_cd = '003G01000000000000' , w_material_info.recevice_dt_tims = '" + day_now + "' ,")
                .Append("   w_material_info.to_lct_cd = '003G01000000000000' ,w_material_info.chg_id = '" + user + "'  ,w_material_info.chg_dt = '" + full_date + "'  ")

                .Append(" WHERE w_material_info.wmtid in (" + data + ") ");

                using (var cmd = db.Database.Connection.CreateCommand())
                {
                    db.Database.Connection.Open();

                    cmd.CommandText = sql2.ToString();
                    var reader = cmd.ExecuteNonQuery();
                    int result1 = Int32.Parse(reader.ToString());
                    db.Database.Connection.Close();
                }

                var sql3 = new StringBuilder();
                sql3.Append(" UPDATE  w_ext_info  ")

                .Append("  SET w_ext_info.alert = 1")
                .Append(" WHERE w_ext_info.ext_no in ('" + ext_no + "') ");

                using (var cmd = db.Database.Connection.CreateCommand())
                {
                    db.Database.Connection.Open();

                    cmd.CommandText = sql3.ToString();
                    var reader = cmd.ExecuteNonQuery();
                    int result1 = Int32.Parse(reader.ToString());
                    db.Database.Connection.Close();
                }

                string[] temp_id = data.TrimStart('[').TrimEnd(']').Split(',');
                List<int> temp = new List<int>();
                for (int i = 0; i < temp_id.Length; i++)
                {
                    temp.Add(int.Parse(temp_id[i]));
                }

                var datalist = (from a in db.w_material_info
                                where temp.Contains(a.wmtid)
                                select new
                                {
                                    wmtid = a.wmtid,
                                    mt_cd = a.mt_cd,
                                    bb_no = a.bb_no,
                                    buyer_qr = a.buyer_qr,
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

                return Json(new { result = true, message = Constant.Success, datalist }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion N_PartialView_GetList_ML_NO_Tims_Shipping_PP

        #endregion N_Shipping_Scan(TIMS)

        public JsonResult GetJsonPersons(DataTable data)
        {
            var lstPersons = GetTableRows(data);
            var a = Json(lstPersons, JsonRequestBehavior.AllowGet);
            return a;
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

        public ActionResult getLocation(lct_info lct_info)
        {
            var lists = db.lct_info.Where(item => item.lct_cd.StartsWith("003") && item.level_cd == "000").
             OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();
            return Json(lists, JsonRequestBehavior.AllowGet);
        }

        #region TIMS Web

        public ActionResult Web()
        {
            return SetLanguage("~/Views/TIMS/Web/Web.cshtml");
       
        }

        public JsonResult GetProducts(string productNo, string productName, string modelCode)
        {
            var sql = new StringBuilder($"CALL spTIMS_GetProducts('{productNo}','{productName}','{modelCode}');");
            return new InitMethods().ConvertDataTableToJsonAndReturn(sql);
        }

        public JsonResult GetQCs(string qcType, string qcCode, string qcName, string qcExp)
        {
            if (string.IsNullOrEmpty(qcType))
            {
                qcType = "";
            }
            if (string.IsNullOrEmpty(qcCode))
            {
                qcCode = "";
            }
            if (string.IsNullOrEmpty(qcName))
            {
                qcName = "";
            }
            if (string.IsNullOrEmpty(qcExp))
            {
                qcExp = "";
            }
            var list = db.qc_item_mt.Where(x => x.del_yn == "N" && x.item_type != "PC" && x.item_type.Contains(qcType) && x.item_vcd.Contains(qcCode) && x.item_nm.Contains(qcName) && x.item_exp.Contains(qcExp)).OrderByDescending(x => x.item_vcd).ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetTIMSProcesses()
        {
            var data = db.comm_dt.Where(x => x.mt_cd == "COM007" && x.use_yn == "Y"
            && (!x.dt_cd.StartsWith("ROT") && !x.dt_cd.StartsWith("STA"))
            )
                .AsEnumerable()
                .Select(x => new
                {
                    ProcessCode = x.dt_cd,
                    ProcessName = x.dt_nm
                }).ToList();
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetTIMSRolls()
        {
            return Json(new InitMethods().GetCommon("COM032"), JsonRequestBehavior.AllowGet);
        }

        public JsonResult Getdataw_actual_primary(Pageing pageing, string product, string product_name, string model, string at_no, string regstart, string regend)
        {
            string type = Request["type"] == null ? "" : Request["type"];
            var sql = new StringBuilder();
            if (type == "")
            {
                var data = _TIMSService.GetActualPrimary(product, product_name, model, at_no, regstart, regend);
                var records = data.Count();
                int totalPages = (int)Math.Ceiling((float)records / pageing.rows);

                int start = (pageing.page - 1) * pageing.rows;
                var rowsData = data.Skip(start).Take(pageing.rows);
                return Json(new { total = totalPages, page = pageing.page, records = records, rows = rowsData }, JsonRequestBehavior.AllowGet);

            }
            else
            {
                var data = _TIMSService.GetDatawActualPrimarySP(product, product_name, model, at_no, regstart, regend).OrderByDescending(x => x.id_actualpr);
                var records = data.Count();
                int totalPages = (int)Math.Ceiling((float)records / pageing.rows);

                int start = (pageing.page - 1) * pageing.rows;
                var rowsData = data.Skip(start).Take(pageing.rows);
                return Json(new { total = totalPages, page = pageing.page, records = records, rows = rowsData }, JsonRequestBehavior.AllowGet);

            }
        }
        public JsonResult GetFinishPO(Pageing pageing, string product, string product_name, string model, string at_no, string regstart, string regend)
        {
            StringBuilder sql = new StringBuilder($"Call GetAllFinishProducts('{product}','{product_name}','{model}','{at_no}','{regstart}','{regend}','TIMS');");
            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);

            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("id_actualpr"));
            return new InitMethods().ReturnJsonResultWithPaging(pageing, total, result);
        }
        [HttpPost]
        public JsonResult FinishPO(string po)
        {
            using (var db = new Entities())
            {
                var poModified = db.w_actual_primary.Where(x => x.at_no == po).FirstOrDefault();
                poModified.finish_yn = "YT";
                if (ModelState.IsValid)
                {
                    db.Entry(poModified).State = EntityState.Modified;
                    db.SaveChanges();
                    return Json(new { result = true, id = poModified.id_actualpr }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { result = false }, JsonRequestBehavior.AllowGet);
                }
            }
        }
        public JsonResult Getdataw_actual_SX(Pageing pageing, string at_no)
        {
            var data = _TIMSService.GetDataWActualSX(at_no);
            var records = data.Count();
            int totalPages = (int)Math.Ceiling((float)records / pageing.rows);
            var rowsData = data.Skip((pageing.page - 1)).Take(pageing.rows);
            return Json(new { total = totalPages, page = pageing.page, records = records, rows = rowsData }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetTIMSListStaff(Pageing pageing, int? id_actual)
        {
            string staff_id = Request["staff_id"] == null ? "" : Request["staff_id"].Trim();
            string staff_name = Request["staff_name"] == null ? "" : Request["staff_name"].Trim();
            string StartDate = Request["StartDate"] == null ? "" : Request["StartDate"].Trim();
            string EndDate = Request["EndDate"] == null ? "" : Request["EndDate"].Trim();
            var dateConvert = new DateTime();
            if (DateTime.TryParse(StartDate, out dateConvert))
            {
                StartDate = dateConvert.ToString("yyyy/MM/dd");
            }

            if (DateTime.TryParse(EndDate, out dateConvert))
            {
                EndDate = dateConvert.ToString("yyyy/MM/dd");
            }
           
            //var find_id = _TIMSService.FindOneWActual(id_actual);
           
            //var bien_id = (find_id.name == "OQC" ? "id_actual_oqc" : "id_actual");
            //var bien_staff = (find_id.name == "OQC" ? "staff_id_oqc" : "staff_id");
            var data = _TIMSService.GetTIMSListStaff((int?)id_actual, staff_id, staff_name, StartDate, EndDate);
            var records = 0;
            if (data != null)
            {
                records = data.Count();
            }

            int totalPages = (int)Math.Ceiling((float)records / pageing.rows);
            var rowsData = data.Skip((pageing.page - 1)).Take(pageing.rows);
            return Json(new { total = totalPages, page = pageing.page, records = records, rows = rowsData }, JsonRequestBehavior.AllowGet);
        }


        public JsonResult update_actual()
        {
            var danhsach = db.d_pro_unit_staff.ToList();
            foreach (var item in danhsach)
            {
                _TIMSService.UpdateActualStaff(item.id_actual, item.psid);
            }

            return Json("", JsonRequestBehavior.AllowGet);

        }

        public JsonResult Getprocess_staff(int id_actual, int psid)
        {
            try
            {
               
                var find = _TIMSService.FindOneWActual(id_actual);
                if (id_actual == 0)
                {
                    return Json(new { result = false, message = "Vui Lòng Kiểm tra lại" }, JsonRequestBehavior.AllowGet);
                }
               
                var delete = _TIMSService.FindOneDProUnitStaffById(psid);
                int ca = _TIMSService.CheckShift(psid);
                if (ca == 0)
                {
                    return Json(new { result = false, message = Constant.EndShift }, JsonRequestBehavior.AllowGet);
                }
                var type = "";
                if (find != null)
                {
                    type = find.name;
                }
                if (type != "OQC")
                {
                    //if (db.w_material_info.Any(x => x.id_actual == id_actual && x.staff_id == delete.staff_id))
                    //{
                    //    return Json(new { result = false, message = "MT LOT " + Constant.Exist }, JsonRequestBehavior.AllowGet);
                    //}

                    var dataExist = _TIMSService.FindAllWMaterialInfoByStaffId(delete.staff_id, id_actual, delete.start_dt, delete.end_dt);
                    if (dataExist.ToList().Count > 0)
                    {
                        return Json(new { result = false, message = "MT LOT " + Constant.Exist }, JsonRequestBehavior.AllowGet);
                    }
                   

                }
                else
                {
                    var check = _TIMSService.FindAllWMaterialInfoByStaffIdOQC(delete.staff_id, id_actual);
                    if (check.Count() > 0)
                    {
                        return Json(new { result = false, message = "MT LOT " + Constant.Exist }, JsonRequestBehavior.AllowGet);
                    }
                }
               
                if (delete != null)
                {
                    int result = _TIMSService.DeleteDProUnitStaff(delete.id_actual, delete.psid, delete.staff_id);
                    return Json(new { result = true, psid = delete.psid }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { result = false, message = Constant.CannotDelete }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetTIMSActualInfo(Pageing paging, string at_no)
        {
            var data = _TIMSService.GetTIMSActualInfoBySP(at_no);
            var records = data.Count();
            int totalPages = (int)Math.Ceiling((float)records / paging.rows);
            var rowsData = data.Skip((paging.page - 1)).Take(paging.rows);
            return Json(new { total = totalPages, page = paging.page, records = records, rows = rowsData }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult xoa_wactual_con(int id)
        {
            try
            {
                var find_id = db.w_actual.Find(id);
                if (find_id != null)
                {
                    if (find_id.name.StartsWith("OQC"))
                    {
                        if (db.w_material_info.Any(x => x.id_actual_oqc == find_id.id_actual))
                        {
                            return Json(new { result = false, message = "Already Exits MT LOT" }, JsonRequestBehavior.AllowGet);
                        }
                    }
                    else
                    {
                        if (db.w_material_info.Any(x => x.id_actual == find_id.id_actual))
                        {
                            return Json(new { result = false, message = "Already Exits MT LOT" }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    db.Entry(find_id).State = EntityState.Deleted;
                    db.SaveChanges();
                    return Json(new { result = true }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = "Can not Find" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = e }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetTIMSActualDetail(int id_actual, int psid)
        {
            try
            {
                var check_vo = _TIMSService.FindOneWActual(id_actual);
                var check_stff = _TIMSService.FindOneDProUnitStaffById(psid);
                StringBuilder sql = new StringBuilder();

                if (check_vo.name == "OQC")
                {
                    var data1 = _TIMSService.FindDetailActualStaffOQC(check_stff.staff_id, id_actual, check_stff.end_dt, check_stff.start_dt);
                    return Json(new { result = true, data = data1 }, JsonRequestBehavior.AllowGet);
                }

                var data = _TIMSService.GetTIMSActualDetailByStaff(check_stff.staff_id, id_actual, check_stff.start_dt, check_stff.end_dt);
                return Json(new { result = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, data = 0 }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult GetTIMSActualDetailpp(int id_actual, int psid)
        {
            try
            {
                var check_vo = _TIMSService.FindOneWActual(id_actual);
                var check_stff = _TIMSService.FindOneDProUnitStaffById(psid);
                StringBuilder sql = new StringBuilder();

                if (check_vo.name == "OQC")
                {
                    var data1 = _TIMSService.FindDetailActualStaffOQC(check_stff.staff_id, id_actual, check_stff.end_dt, check_stff.start_dt);
                    return Json(new { result = true, data = data1 }, JsonRequestBehavior.AllowGet);
                }

                var data = _TIMSService.GetTIMSActualDetailByStaffpp(check_stff.staff_id, id_actual, check_stff.start_dt, check_stff.end_dt);
                return Json(new { result = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, data = 0 }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult GetTIMSActualDetailpp1(int id_actual, string staff_id, string start_dt, string end_dt)
        {
            try
            {
                var check_vo = _TIMSService.FindOneWActual(id_actual);
                //var check_stff = _TIMSService.FindOneDProUnitStaffById(psid);
                StringBuilder sql = new StringBuilder();

                if (check_vo.name == "OQC")
                {
                    var data1 = _TIMSService.FindDetailActualStaffOQC(staff_id, id_actual, end_dt, start_dt);
                    return Json(new { result = true, data = data1 }, JsonRequestBehavior.AllowGet);
                }

                var data = _TIMSService.GetTIMSActualDetailByStaffpp(staff_id, id_actual, start_dt, end_dt);
                return Json(new { result = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, data = 0 }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult Add_w_actual(string style_no, string at_no, string name, string roll, w_actual w_actual)
        {
            try
            {
                w_actual.date = DateTime.Now.ToString("yyyy-MM-dd");
                var check_data = _TIMSService.FindAllWActualByAtNo(at_no);
                w_actual.level = check_data.Count() + 1;
                w_actual.type = "TIMS";
                w_actual.actual = 0;
                w_actual.at_no = at_no;
                w_actual.product = style_no;
                w_actual.name = name;
                w_actual.don_vi_pr = roll;
                w_actual.defect = 0;
                w_actual.reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                w_actual.chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                w_actual.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                w_actual.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                if (name == "OQC")
                {
                    w_actual.item_vcd = "OC000001A";
                }
                else
                {
                    w_actual.item_vcd = "TI000001A";
                }

                if (ModelState.IsValid)
                {
                    int idInserted = _TIMSService.InsertWActual(w_actual);
                    var vlieu = style_no + "-" + name;
                    if (!(db.d_material_info.Any(x => x.mt_no == vlieu)))
                    {
                        d_material_info b = new d_material_info();
                        b.mt_no = vlieu;
                        b.mt_nm = vlieu;
                        b.mt_type = "CMT";
                        b.reg_dt = DateTime.Now;
                        b.bundle_unit = "EA";
                        b.chg_dt = DateTime.Now;
                        b.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        b.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        b.use_yn = "Y";
                        b.del_yn = "N";

                        db.Entry(b).State = EntityState.Added;
                        db.SaveChanges(); // line that threw exception
                    }
                    StringBuilder sql = new StringBuilder($"CALL spTIMS_GetTIMSActualInfo('{at_no}','','','{w_actual.id_actual}');");
                    var result2 = _TIMSService.GetTIMSActualInfoBySP(at_no);
                    return Json(new { result = true, message = Constant.Success, kq = result2 }, JsonRequestBehavior.AllowGet);
                }
                
                return Json(new { result = false, message = Constant.Duplicate }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetTIMSWorkerMappingList()
        {
            return null;
        }

        public JsonResult GetWorkers(Pageing pageing, string userId, string userName, string positionCode)
        {
            if (string.IsNullOrEmpty(userId))
            {
                userId = "";
            }
            if (string.IsNullOrEmpty(userName))
            {
                userName = "";
            }
            if (string.IsNullOrEmpty(positionCode))
            {
                positionCode = "";
            }
            Dictionary<string, string> list = PagingAndOrderBy(pageing, " ORDER BY MyDerivedTable.userid DESC ");
            String varname1 = "";
            varname1 = varname1 + "SELECT (SELECT dt_nm " + "\n";
            varname1 = varname1 + "        FROM   comm_dt " + "\n";
            varname1 = varname1 + "        WHERE  mt_cd = 'COM018' " + "\n";
            varname1 = varname1 + "               AND dt_cd = a.position_cd) AS PositionName, " + "\n";
            varname1 = varname1 + "       a.userid UserId,ROW_NUMBER() OVER(" + "\n";
            varname1 = varname1 + "ORDER BY a.userid DESC) AS RowNum ," + "\n";
            varname1 = varname1 + "       a.uname UserName, a.position_cd PositionCode, " + "\n";
            varname1 = varname1 + "       a.nick_name, " + "\n";
            varname1 = varname1 + "       (SELECT c.mc_no " + "\n";
            varname1 = varname1 + "        FROM   d_pro_unit_staff AS b " + "\n";
            varname1 = varname1 + "               LEFT JOIN d_pro_unit_mc AS c " + "\n";
            varname1 = varname1 + "                      ON b.id_actual = c.id_actual " + "\n";
            varname1 = varname1 + "        WHERE  a.userid = b.staff_id " + "\n";
            varname1 = varname1 + "               AND c.mc_no IN (SELECT d.mc_no " + "\n";
            varname1 = varname1 + "                               FROM   d_machine_info AS d) " + "\n";
            varname1 = varname1 + "        ORDER  BY c.chg_dt DESC " + "\n";
            varname1 = varname1 + "        LIMIT  1)                         AS mc_no" + "\n";
            varname1 = varname1 + "FROM   mb_info AS a";
            varname1 = varname1 + " Where a.lct_cd='staff' and  ('" + userId + "'='' OR  a.userid like '%" + userId + "%' )";
            varname1 = varname1 + " AND ('" + userName + "'='' OR  a.uname like '%" + userName + "%' )";
            varname1 = varname1 + " AND ('" + positionCode + "'='' OR  a.position_cd like '%" + positionCode + "%' )";

            string countSql = " SELECT COUNT(*) FROM ( " + varname1 + " ) MyDerivedTable ";
            string viewSql = " SELECT * FROM ( " + varname1 + " ) MyDerivedTable "

                + " WHERE MyDerivedTable.RowNum BETWEEN " + list["start"] + " AND " + list["end"] + " "
                + list["orderBy"];
            return SearchAndPaging(countSql, viewSql, int.Parse(list["index"]), int.Parse(list["size"]));
        }

        public JsonResult GetAllWorkerPositions()
        {
            return Json(new InitMethods().GetCommon("COM018"), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetStaffTypes()
        {
            return Json(new InitMethods().GetCommon("COM013"), JsonRequestBehavior.AllowGet);
        }

        //-------------------HẰNG CODE

        public ActionResult Createprocess_unitstaff(string staff_id, string use_yn, d_pro_unit_staff a)
        {
            var start1 = DateTime.Now;
            var dsach_ca = db.w_policy_mt.ToList();
            float timenow = float.Parse(start1.ToString("HH")) + float.Parse(DateTime.Now.ToString("mm")) / 60;
            var end = _IWOService.GetEndDateProcessUnitStaff(timenow);
            var start = DateTime.Now.ToString("yyyyMMddHHmmss");
            var dateConvert = new DateTime();
            if (DateTime.TryParse(end, out dateConvert))
            {
                end = dateConvert.ToString("yyyyMMddHHmmss");
            }
            var CheckUser = _TIMSService.CheckExistUserByUserid(staff_id);
           
            if (!CheckUser)
            {
                return Json(new { result = false, message = "Staff " + Constant.NotFound }, JsonRequestBehavior.AllowGet);
            }
            var item_count = _TIMSService.FindDProUnitStaffByStaffIdIdActual(a.id_actual, staff_id);
            if (item_count.Count() == 0)
            {
                a.start_dt = start;
                a.end_dt = end;
                a.staff_id = staff_id;
                a.use_yn = use_yn;
                a.del_yn = "N";
                a.chg_dt = DateTime.Now;
                a.reg_dt = DateTime.Now;
                a.id_actual = a.id_actual;
                int idReturn = _TIMSService.InsertDProUnitStaff(a);
                var vaule = _TIMSService.FindOneDProUnitStaffReturnByPsid(idReturn);
                return Json(new { result = 0, kq = vaule }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var list = item_count.ToList();
                if (item_count.Count() >= 1)
                {
                    bool duplicate_time = false;
                    foreach (var item in list)
                    {
                        var item_start_dt = item.start_dt;
                        var item_end_dt = item.end_dt;

                        if (DateTime.TryParse(item_start_dt, out dateConvert))
                        {
                            item_start_dt = dateConvert.ToString("yyyyMMddHHmmss");
                        }
                        if (DateTime.TryParse(item_end_dt, out dateConvert))
                        {
                            item_end_dt = dateConvert.ToString("yyyyMMddHHmmss");
                        }
                        bool condition_after = (((Convert.ToInt64(item_start_dt) > Convert.ToInt64(start)) && (Convert.ToInt64(item_start_dt) > Convert.ToInt64(end))));
                        bool condition_before = (((Convert.ToInt64(item_end_dt) < Convert.ToInt64(start)) && (Convert.ToInt64(item_start_dt) < Convert.ToInt64(end))));
                        if (!(condition_before || condition_after))
                        {
                            duplicate_time = true;
                            if (item.id_actual != a.id_actual)
                            {
                                if (ModelState.IsValid)
                                {
                                    return Json(new { result = 3, update = item.psid, start = start, end = end }, JsonRequestBehavior.AllowGet);
                                }
                            }
                        }
                    }
                    if (duplicate_time == false && list.Count > 0)
                    {
                        a.start_dt = start;
                        a.end_dt = end;
                        a.staff_id = staff_id;
                        a.use_yn = use_yn;
                        a.del_yn = "N";
                        a.chg_dt = DateTime.Now;
                        a.reg_dt = DateTime.Now;
                        a.id_actual = a.id_actual;
                        int idReturn = _TIMSService.InsertDProUnitStaff(a);
                        var vaule = _TIMSService.GetDProUnitStaff(idReturn);
                        return Json(new { result = 0, kq = vaule }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        return Json(new { result = 1, staff_id = staff_id }, JsonRequestBehavior.AllowGet);
                    }
                }
                return View();
            }
        }

        public ActionResult Modifyprocess_unitstaff(int psid, string staff_id, string use_yn, d_pro_unit_staff a, string end, string start)
        {
            bool checkUserExist = _TIMSService.CheckExistUserByUserid(staff_id);
          
            if (!checkUserExist)
            {
                return Json(new { result = false, message = "Staff " + Constant.NotFound }, JsonRequestBehavior.AllowGet);
            }
            var dateConvert = new DateTime();
            if (DateTime.TryParse(start, out dateConvert))
            {
                start = dateConvert.ToString("yyyyMMddHHmmss");
            }
            if (DateTime.TryParse(end, out dateConvert))
            {
                end = dateConvert.ToString("yyyyMMddHHmmss");
            }
            DProUnitStaffAPI item1 = _TIMSService.FindOneDProUnitStaffById(psid);

            if (Convert.ToInt64(start) >= Convert.ToInt64(end) || Convert.ToInt64(item1.start_dt) < Convert.ToInt64(start))
            {
                return Json(new { result = 2, staff_id = staff_id }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                // end(giờ muốn cập nhật) nhỏ hơn (item.end_dt) giờ đang tồn tại trong db 
                if (Convert.ToInt64(end) < Convert.ToInt64(item1.end_dt))
                {
                    // lấy giờ mới nhất trong db, nếu end(giờ muốn cập nhật) nhỏ hơn giờ đã tồn tại mã lot trong db thì ko cho
                    //nếu là OQC thì lấy end_production_dt ngược lại lấy reg_dt

                    string sql2 = @"SELECT a.name
                                                     FROM w_actual AS a
                                                     WHERE a.id_actual =@1;";
                    string nameProcess = db.Database.SqlQuery<string>(sql2,
                        new MySqlParameter("1", item1.id_actual)
                        ).FirstOrDefault();

                    string sql = "";
                    if (nameProcess == "OQC")
                    {
                        sql = @"SELECT MAX(a.end_production_dt) AS reg_dt
                                FROM  w_material_info AS a 
                                JOIN d_pro_unit_staff AS b ON a.id_actual_oqc=b.id_actual 
                                WHERE b.psid=@1 AND (a.end_production_dt BETWEEN DATE_FORMAT(@2, '%Y-%m-%d %H:%i:%s') AND  DATE_FORMAT(@3, '%Y-%m-%d %H:%i:%s'))  
                                ;";
                    }
                    else
                    {
                        sql = @"SELECT MAX(a.reg_dt) AS reg_dt
                                FROM  w_material_info AS a 
                                JOIN d_pro_unit_staff AS b ON a.id_actual=b.id_actual 
                                WHERE b.psid=@1 AND (a.reg_dt BETWEEN DATE_FORMAT(@2, '%Y-%m-%d %H:%i:%s') AND  DATE_FORMAT(@3, '%Y-%m-%d %H:%i:%s'))  
                                ;";
                    }

                    string kq = db.Database.SqlQuery<string>(sql,
                        new MySqlParameter("@1", psid),
                        new MySqlParameter("@2", item1.start_dt),
                        new MySqlParameter("@3", item1.end_dt)
                        ).FirstOrDefault();
                    if (!string.IsNullOrEmpty(kq))
                    {
                        if (DateTime.TryParse(kq, out dateConvert))
                        {
                            kq = dateConvert.ToString("yyyyMMddHHmmss");
                        }
                        if (Convert.ToInt64(end) < Convert.ToInt64(kq))
                        {
                            return Json(new { result = 4, message = "Đã tồn tại mã Bobbin" }, JsonRequestBehavior.AllowGet);
                        }
                    }
                }


                var item_count = _TIMSService.FindDProUnitStaffByStaffId(psid, staff_id).ToList();

                if (item_count.Count == 0)
                {

                    
                    item1.uname = _TIMSService.GetUname(staff_id);
                    item1.start_dt = start;
                    item1.end_dt = end;
                    item1.staff_id = staff_id;
                    item1.use_yn = use_yn;
                    item1.del_yn = "N";
                    _TIMSService.UpdateDProUnitStaff(item1);
                    item1.start_dt = item1.start_dt.Substring(0, 4) + "-" + item1.start_dt.Substring(4, 2) + "-" + item1.start_dt.Substring(6, 2) + " " + item1.start_dt.Substring(8, 2) + ":" + item1.start_dt.Substring(10, 2) + ":" + item1.start_dt.Substring(12, 2);
                    item1.end_dt = item1.end_dt.Substring(0, 4) + "-" + item1.end_dt.Substring(4, 2) + "-" + item1.end_dt.Substring(6, 2) + " " + item1.end_dt.Substring(8, 2) + ":" + item1.end_dt.Substring(10, 2) + ":" + item1.end_dt.Substring(12, 2);
                    return Json(new { result = 0, kq = item1 }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var time_cuoi = item_count.Where(x => x.id_actual == item1.id_actual).OrderByDescending(x => x.end_dt).FirstOrDefault()?.start_dt;

                    if (time_cuoi == null)
                    {
                        
                        item1.uname = _TIMSService.GetUname(staff_id);
                        item1.start_dt = start;
                        item1.end_dt = end;
                        item1.staff_id = staff_id;
                        item1.use_yn = use_yn;
                        item1.del_yn = "N";
                        _TIMSService.UpdateDProUnitStaff(item1);
                        item1.start_dt = item1.start_dt.Substring(0, 4) + "-" + item1.start_dt.Substring(4, 2) + "-" + item1.start_dt.Substring(6, 2) + " " + item1.start_dt.Substring(8, 2) + ":" + item1.start_dt.Substring(10, 2) + ":" + item1.start_dt.Substring(12, 2);
                        item1.end_dt = item1.end_dt.Substring(0, 4) + "-" + item1.end_dt.Substring(4, 2) + "-" + item1.end_dt.Substring(6, 2) + " " + item1.end_dt.Substring(8, 2) + ":" + item1.end_dt.Substring(10, 2) + ":" + item1.end_dt.Substring(12, 2);
                        return Json(new { result = 0, kq = item1 }, JsonRequestBehavior.AllowGet);
                    }
                    if (Convert.ToInt64(time_cuoi) > Convert.ToInt64(end))
                    {
                        return Json(new { result = 2, staff_id = staff_id }, JsonRequestBehavior.AllowGet);
                    }
                    var list = item_count.ToList();
                    if (item_count.Count >= 1)
                    {
                        bool duplicate_time = false;
                       
                        var data1 = _TIMSService.CheckExistDuplicateTime(staff_id, start, end, psid, item1.id_actual);
                      
                        if (data1)
                        {
                            duplicate_time = true;
                        }
                        if (duplicate_time == false && list.Count > 0)
                        {
                           
                            DProUnitStaffAPI item = _TIMSService.FindOneDProUnitStaffById(psid);

                            


                            item1.uname = _TIMSService.GetUname(staff_id);
                            item.start_dt = start;
                            item.end_dt = end;
                            item.staff_id = staff_id;
                            item.use_yn = use_yn;
                            item.del_yn = "N";
                            _TIMSService.UpdateDProUnitStaff(item);
                            item.start_dt = item.start_dt.Substring(0, 4) + "-" + item.start_dt.Substring(4, 2) + "-" + item.start_dt.Substring(6, 2) + " " + item.start_dt.Substring(8, 2) + ":" + item.start_dt.Substring(10, 2) + ":" + item.start_dt.Substring(12, 2);
                            item.end_dt = item.end_dt.Substring(0, 4) + "-" + item.end_dt.Substring(4, 2) + "-" + item.end_dt.Substring(6, 2) + " " + item.end_dt.Substring(8, 2) + ":" + item.end_dt.Substring(10, 2) + ":" + item.end_dt.Substring(12, 2);
                            return Json(new { result = 0, kq = item }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            return Json(new { result = 1, staff_id = staff_id }, JsonRequestBehavior.AllowGet);
                        }
                    }
                    return View();
                }
            }
        }

        //-------------------HẰNG CODE - END

        #region ROll Nomal

        public ActionResult searchbobbinPopupold(string bb_no, string bb_nm, string mt_cd, int? id_actual)
        {
            var data = _TIMSService.GetListBoBinPopup(id_actual, bb_no, bb_nm, mt_cd);
            return Json(data, JsonRequestBehavior.AllowGet);
        }
        public ActionResult searchbobbinPopup(Pageing pageing, string bb_no, string bb_nm, string mt_cd, int? id_actual)
        {

            Dictionary<string, string> list = PagingAndOrderBy(pageing, " ORDER BY  MyDerivedTable.mt_cd desc   ");



            int totalRecords = _TIMSService.TotalRecordssearchbobbinPopup(id_actual, bb_no, bb_nm, mt_cd);
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

            IEnumerable<WMaterialnfo> Data = _TIMSService.GetListSearchGetListBoBinPopup(id_actual, bb_no, bb_nm, mt_cd);

           // var data = _TIMSService.GetActualPrimary(product, product_name, model, at_no, regstart, regend);
            var records = totalRecords;
           // int totalPages = (int)Math.Ceiling((float)records / pageing.rows);

            int start = (pageing.page - 1) * pageing.rows;
            var rowsData = Data.Skip(start).Take(pageing.rows);
            //return Json(new { total = totalPages, page = pageing.page, records = records, rows = rowsData }, JsonRequestBehavior.AllowGet);



            var result = new
            {
                total = totalPages,
                page = int.Parse(list["index"]),
                records = totalRecords,
                rows = rowsData
            };
            return Json(result, JsonRequestBehavior.AllowGet);

        }
        public JsonResult InsertMLNoWithSelectedBobin(w_material_info a, w_material_mapping c, int id_actual, string name, string bb_no,
             string staff_id, int psid)
        {
            try
            {
                string style_no = Request["style_no"] == null ? "" : Request["style_no"].Trim();
                var ds_bb = _TIMSService.GetOneDBobbinInfoWithMtCdIsNULL(bb_no);
                if (String.IsNullOrEmpty(staff_id))
                {
                    return Json(new { result = false, message = Constant.CheckData + " Staff" }, JsonRequestBehavior.AllowGet);
                }
                else if (ds_bb == null)
                {
                    return Json(new { result = false, message = "Container" + Constant.DataExist }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    if ((ds_bb.mt_cd != "" && ds_bb.mt_cd != null) && ds_bb != null)
                    {
                        return Json(new { result = false, message = Constant.Used }, JsonRequestBehavior.AllowGet);
                    }
                }
                var check_exit_vo = _TIMSService.FindOneWActual(id_actual);
                if (check_exit_vo == null)
                {
                    return Json(new { result = false, message = "PO " + Constant.DataExist }, JsonRequestBehavior.AllowGet);
                }
                var ca = _TIMSService.CheckShift(psid, id_actual);
                if (ca == 0)   //het ca
                {
                    return Json(new { result = false, message = Constant.EndShift }, JsonRequestBehavior.AllowGet);
                }
                DateTime dt1 = DateTime.Now; //Today at 00:00:00
                string time_now = dt1.ToString("HHmmss");
                string time_now1 = dt1.ToString("yyyyMMdd");
                time_now = time_now + Guid.NewGuid().ToString("N").Substring(0, 3).ToUpper();
                a.mt_type = "CMT";
                a.bb_no = bb_no;
                a.real_qty = 0;
                a.staff_id = staff_id;
                a.mt_no = style_no + "-" + check_exit_vo.name;
                var bien_first = style_no + check_exit_vo.name + time_now1 + time_now;
                a.bbmp_sts_cd = "000";
                a.mt_sts_cd = "002";
                a.lct_cd = "006000000000000000";
                a.from_lct_cd = "006000000000000000";
                a.lct_sts_cd = "101";
                a.id_actual = id_actual;
                a.use_yn = "Y";
                a.sts_update = "composite";
                a.gr_qty = 0;
                a.chg_dt = DateTime.Now;
                a.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                a.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                a.reg_dt = DateTime.Now;
               
                a.input_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                var menuCd = string.Empty;
                var subMenuIdConvert = 0;
                var subMenuId = "";
                var ds = _TIMSService.FindAllMaterialByMtCdLike(bien_first);
                if (ds.Count() == 0)
                {
                    subMenuId = bien_first + time_now + "000000";
                }
                else
                {
                    var bien = "";
                    var ds2_1 = _TIMSService.FindAllMaterialByMtCdLike(bien_first + time_now);
                    if (ds2_1.Count() > 0)
                    {
                        bien = ds2_1.LastOrDefault().mt_cd;
                    }
                    else { bien = ds.LastOrDefault().mt_cd; }

                    var time_sau = time_now;
                    var time_trc = bien.Remove(bien.Length - 6, 6);
                    time_trc = time_trc.Substring(time_trc.Length - 6, 6);
                    if (time_sau != time_trc)
                    {
                        subMenuId = bien_first + time_now + "000000";
                    }
                    else
                    {
                        subMenuId = bien.Substring(bien.Length - 6, 6);
                    }
                }
                int.TryParse(subMenuId, out subMenuIdConvert);
                menuCd = string.Format("{0}{1}", menuCd, autobarcode((subMenuIdConvert + 1)));
                a.mt_cd = bien_first + menuCd;
                a.mt_barcode = bien_first + menuCd;
                a.mt_qrcode = bien_first + menuCd;
                a.date = time_now1;
                a.product = check_exit_vo.product;
                a.at_no = check_exit_vo.at_no;
                
                int idReturn = _TIMSService.InsertWMaterialInfoToBobin(a);
                //mapping mt_cd với bobin
                ds_bb.mt_cd = a.mt_cd;
                ds_bb.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                ds_bb.count_number = 1;
                int count_number = int.Parse(ds_bb.count_number.ToString());
                _TIMSService.UpdateBobbinInfo(ds_bb.chg_id, ds_bb.mt_cd, count_number, ds_bb.bno);
                if (!db.d_bobbin_lct_hist.Any(x => x.bb_no == ds_bb.bb_no && x.mt_cd == ds_bb.mt_cd)) //ko co trong do thi insert
                                                                                                        // if (data.Count() < 0 )
                {
                    DBobbinLctHist d_bobbin_lct_hist = new DBobbinLctHist();
                    d_bobbin_lct_hist.bb_no = ds_bb.bb_no;
                    d_bobbin_lct_hist.bb_nm = ds_bb.bb_nm;
                    d_bobbin_lct_hist.mc_type = ds_bb.mc_type;
                    d_bobbin_lct_hist.mt_cd = ds_bb.mt_cd;
                    d_bobbin_lct_hist.use_yn = "Y";
                    d_bobbin_lct_hist.del_yn = "N";
                    d_bobbin_lct_hist.reg_dt = DateTime.Now;
                    d_bobbin_lct_hist.chg_dt = DateTime.Now;
                    d_bobbin_lct_hist.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    d_bobbin_lct_hist.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    _TIMSService.InsertToBobbinHist(d_bobbin_lct_hist);
                }
                var take = style_no + name;
                var list = _TIMSService.FindOneWMaterialMappingByIdActualAndStaffId(id_actual, staff_id);
                if (list.Count() > 0)
                {
                    var mt_lot_con = list.FirstOrDefault().mt_lot;
                    var list_con = _TIMSService.FindMaterialMappingByMtcdUseYn(mt_lot_con, "Y").ToList();
                    foreach (var item in list_con)
                    {
                        if (item.sts_share == null)
                        {
                            c.mt_lot = a.mt_cd;
                            c.mt_no = item.mt_no;
                            c.mt_cd = item.mt_cd;
                            c.mapping_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                            c.bb_no = item.bb_no;
                            c.remark = item.remark;
                            c.use_yn = item.use_yn;
                            c.del_yn = "N";
                            c.chg_dt = DateTime.Now;
                            c.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                            c.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                            c.reg_dt = DateTime.Now;
                            int idTest = _TIMSService.InsertMaterialMapping(c);
                            var ds2 = _TIMSService.FindWMaterialInfo(c.mt_cd).ToList();

                            if (ds2.Count() == 1)
                            {
                                var id_fo1 = ds2.FirstOrDefault().wmtid;
                                WMaterialnfo update1 = _TIMSService.FindOneWMaterialInfo(id_fo1);
                    
                                //NẾU MÃ ĐẦU VÀO LÀ STA OR ROT THÌ STATUS = 008
                                if (update1.mt_cd.Contains("STA") || update1.mt_cd.Contains("ROT"))
                                {
                                    update1.mt_sts_cd = "008";
                                }
                                else
                                {
                                    update1.mt_sts_cd = "002";
                                }
                                //update1.mt_sts_cd = "002";
                                update1.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                                _TIMSService.UpdateWMaterialInfoById(update1.mt_sts_cd, update1.chg_id, update1.wmtid);
                            }
                        }
                    }
                }
                var ds1 = _TIMSService.FindOneWMaterialInfo(idReturn, a.mt_no, a.bbmp_sts_cd);
                var mt_cd = a.mt_cd.Remove(a.mt_cd.Length - 12, 12);
                int giatri = _TIMSService.SumGroupQtyWMaterialInfo(mt_cd);

                return Json(new { ds1 = ds1, gtri = giatri }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult insertw_material_mping(w_material_mapping a, string bb_no, string mt_cd, int id_actual)
        {
            try
            {
                var qr = _TIMSService.FindWMaterialInfoByMTQRCode(mt_cd);
                
                if (_TIMSService.CheckExistMaterialMappingById(mt_cd))
                {
                    return Json(new { result = false, message = Constant.Other }, JsonRequestBehavior.AllowGet);
                }
                var ds_bb = _TIMSService.FindAllBobbin_lct_histByBBNo(bb_no).ToList();
                if (ds_bb.Count == 0)
                {
                    return Json(new { result = false, message = "Container " + Constant.DataExist }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    int count_erro = 0;
                    for (int i = 0; i < ds_bb.Count; i++)
                    {
                        var ds_bb_for = ds_bb[i];
                        var mt = db.w_material_info.Where(x => x.mt_cd == ds_bb_for.mt_cd &&
                        (x.mt_sts_cd == "008" || x.mt_sts_cd == "002")
                        && x.lct_cd.StartsWith("006")
                        && (x.lct_sts_cd == "101") && (x.gr_qty > 0) && (id_actual != x.id_actual || id_actual == x.id_actual && x.id_actual_oqc != null && x.id_actual_oqc != 0)).ToList();
                        if (qr.Count() == 1 && mt.Count == 1)
                        { //hàm kiểm tra mã có cùng po không
                            var id_lot = qr.FirstOrDefault().id_actual;
                            var id_cd = mt.FirstOrDefault().id_actual;
                            var check_lot = db.w_actual.Find(id_lot);
                            var check_cd = db.w_actual.Find(id_cd);
                            var check_po = "";
                            if (check_lot.at_no == check_cd.at_no)
                            {
                                check_po = "OK";
                            }
                            if (check_po == "")
                            {
                                return Json(new { result = false, message = "Chọn Sai PO! Xin vui lòng chọn lại!" }, JsonRequestBehavior.AllowGet);
                            }
                            a.mapping_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                            a.mt_no = mt.FirstOrDefault().mt_no;
                            if ((!db.w_material_mapping.Any(x => x.mt_no == a.mt_no && x.mt_cd == ds_bb_for.mt_cd && x.mt_lot == mt_cd)) && (mt_cd != ds_bb_for.mt_cd))
                            {
                                a.mt_cd = ds_bb_for.mt_cd;
                                a.mt_lot = mt_cd;
                                a.bb_no = ds_bb_for.bb_no;
                                a.use_yn = "Y";
                                a.bb_no = qr.FirstOrDefault().bb_no;
                                a.remark = qr.FirstOrDefault().remark;
                                a.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                                a.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                                a.reg_dt = DateTime.Now;
                                a.chg_dt = DateTime.Now;
                                int resultMapping = _TIMSService.InsertMaterialMapping(a);
                               
                                var id_fo1 = mt.FirstOrDefault().wmtid;
                                
                                var update1 = _TIMSService.FindOneWMaterialInfo(id_fo1);
                                //update1.mt_sts_cd = "002";
                                update1.id_actual = update1.id_actual;
                                update1.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                                int effectRows = _TIMSService.UpdateWMaterialInfoById(update1.mt_sts_cd, update1.chg_id, update1.id_actual, update1.wmtid);
                            }
                        }
                        else
                        {
                            count_erro++;
                        }
                    }
                    if (count_erro == ds_bb.Count)
                    {
                        return Json(new { result = false, message = "Container " + Constant.NotFound + "hoặc cùng công đoạn" }, JsonRequestBehavior.AllowGet);
                    }
                    var data = _TIMSService.GetDatasMaterialMapping(mt_cd);
                    var result = new { list = data, count_erro = count_erro };
                    return Json(result, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult insertw_materialEA_mping(w_material_mapping a, w_material_info b, string bb_no, int id_actual, string staff_id, int psid)
        {
            using (var context = new Entities())
            {
                
                try
                {
                    
                    var ds_bb = _TIMSService.FindOneDBobbinInfo(bb_no);
                  
                    var ds_mapping = _TIMSService.FindOneBobbin_lct_hist(bb_no);
                    if (String.IsNullOrEmpty(staff_id))
                    {
                        return Json(new { result = false, message = Constant.CheckData + " Staff" }, JsonRequestBehavior.AllowGet);
                    }
                    else if (ds_bb == null)
                    {
                        return Json(new { result = false, message = "Container Không tìm thấy!!!" }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        if (ds_mapping == null)
                        {
                            return Json(new { result = false, message = "Đồ đựng này Trống" }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            //
                            if (String.IsNullOrEmpty(ds_bb.mt_cd))
                            {
                                //update bảng bobin trống từ lỗi sản xuất đưa qua 
                                ds_bb.mt_cd = ds_mapping.mt_cd;
                                ds_bb.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                                _TIMSService.UpdateBobbinInfo(ds_bb.chg_id, ds_bb.mt_cd, ds_bb.bno);
                                
                            }
                        }
                    }


                    //kiểm tra mấy mã lot cũ có đạt tiêu chuẩn k
                    var mt = _TIMSService.FindOneWMaterialInfoLike(ds_mapping.mt_cd, "006", "001");
                    if (mt != null)
                    {
                        var check_thong_bao = mt;
                        if (check_thong_bao.mt_sts_cd != "008" && check_thong_bao.mt_sts_cd != "002")
                        {
                            var trangthai = checktrangthai(check_thong_bao.mt_sts_cd);
                            return Json(new { result = false, message = "Trạng Thái đang là " + trangthai }, JsonRequestBehavior.AllowGet);
                        }
                        if (check_thong_bao.gr_qty == 0)
                        {
                            return Json(new { result = false, message = "Sản Lượng Không có để Scan!!!" }, JsonRequestBehavior.AllowGet);
                        }
                        if (id_actual == check_thong_bao.id_actual)
                        {
                            return Json(new { result = false, message = "Cùng Công Đoạn Vui Lòng Không Scan vào!!!" }, JsonRequestBehavior.AllowGet);
                        }
                    }
                    else
                    {
                        return Json(new { result = false, message = "Không đủ điều kiện để scan" }, JsonRequestBehavior.AllowGet);
                    }
                   
                    var data1 = _TIMSService.CheckShift(psid, id_actual);
                   
                    if (data1 == 0)
                    {
                        return Json(new { result = false, message = Constant.EndShift }, JsonRequestBehavior.AllowGet);
                    }
                   
                    var check_exit_vo = _TIMSService.FindOneWActual(id_actual);
                   
                    if (check_exit_vo == null)
                    {
                        return Json(new { result = false, message = "PO " + Constant.NotFound }, JsonRequestBehavior.AllowGet);
                    }

                    //tạo mã lot mới nhưng vẫn là container đó
                    DateTime dt1 = DateTime.Now; //Today at 00:00:00
                    string time_now = dt1.ToString("HHmmss");
                    string time_now1 = dt1.ToString("yyyyMMdd");
                    time_now = time_now + Guid.NewGuid().ToString("N").Substring(0, 3).ToUpper();
                    b.mt_type = "CMT";
                    b.bb_no = bb_no;
                    b.real_qty = 0;
                    b.staff_id = staff_id;
                    var get_primary = _TIMSService.FindOneWActualPrimaryByAtNo(check_exit_vo.at_no);
                    if (check_exit_vo == null)
                    {
                        return Json(new { result = false, message = "PO " + Constant.NotFound }, JsonRequestBehavior.AllowGet);
                    }
                    b.mt_no = get_primary.product + "-" + check_exit_vo.name;
                    var bien_first = b.mt_no + time_now1 + time_now;
                    b.bbmp_sts_cd = "000";
                    b.mt_sts_cd = "002";
                    b.lct_cd = "006000000000000000";
                    b.from_lct_cd = "006000000000000000";
                    b.lct_sts_cd = "101";
                    b.id_actual = id_actual;
                    b.use_yn = "Y";
                    b.sts_update = "composite";
                    b.gr_qty = 0;
                    b.chg_dt = DateTime.Now;
                    b.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    b.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    b.reg_dt = DateTime.Now;
                    b.at_no = check_exit_vo.at_no;
                    b.product = check_exit_vo.product;
                    DateTime dt = DateTime.Now; //Today at 00:00:00
                    string input_dt = dt.ToString("yyyyMMddHHmmss");
                    b.input_dt = input_dt;

                    var menuCd = string.Empty;
                    var subMenuIdConvert = 0;
                    var subMenuId = "";
                    var ds = _TIMSService.FindAllMaterialByMtCdLike(bien_first);
                    if (ds.Count() == 0 && !ds.Any(x => x.mt_cd.StartsWith(bien_first + time_now)))
                    {
                        subMenuId = bien_first + time_now + "000000";
                    }
                    else
                    {
                        var bien = "";
                        var ds2_1 = ds.Where(x => x.mt_cd.StartsWith(bien_first + time_now)).ToList();
                        if (ds2_1.Count() > 0)
                        {
                            bien = ds2_1.LastOrDefault().mt_cd;
                        }
                        else { bien = ds.LastOrDefault().mt_cd; }

                        var time_sau = time_now;
                        var time_trc = bien.Remove(bien.Length - 6, 6);
                        time_trc = time_trc.Substring(time_trc.Length - 6, 6);
                        if (time_sau != time_trc)
                        {
                            subMenuId = bien_first + time_now + "000000";
                        }
                        else
                        {
                            subMenuId = bien.Substring(bien.Length - 6, 6);
                        }
                    }
                    int.TryParse(subMenuId, out subMenuIdConvert);
                    menuCd = string.Format("{0}{1}", menuCd, autobarcode((subMenuIdConvert + 1)));
                    b.mt_cd = bien_first + menuCd;
                    b.mt_barcode = bien_first + menuCd;
                    b.mt_qrcode = bien_first + menuCd;
                    b.date = time_now1;
                    
                    // nếu k mapping mã nguyên vât liệu sẽ mapping mã bobbin để lấy

                    //hàm kiểm tra mã có cùng po không
                    var id_lot = id_actual;
                   
                    var id_cd = mt.id_actual;
                    var check_lot = db.w_actual.Find(id_lot);
                    var check_cd = db.w_actual.Find(id_cd);
                    var check_po = "";
                    if (check_lot.at_no == check_cd.at_no)
                    {
                        check_po = "OK";
                    }
                    if (check_po == "")
                    {
                        return Json(new { result = false, message = "Chọn Sai PO! Xin vui lòng chọn lại!" }, JsonRequestBehavior.AllowGet);
                    }
                    int idReturn = _TIMSService.InsertWMaterialInfoToBobin(b);


                    a.mapping_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                    a.mt_no = mt.mt_no;
                    // kiểm tra xem đã có mã lot mới và lot cũ đã tồn tại mapping chưa
                    if ((!db.w_material_mapping.Any(x => x.mt_no == a.mt_no && x.mt_cd == ds_mapping.mt_cd && x.mt_lot == b.mt_cd)) && b.mt_cd != ds_mapping.mt_cd)
                    {
                        a.mt_cd = ds_mapping.mt_cd;
                        a.mt_lot = b.mt_cd;
                        a.bb_no = ds_mapping.bb_no;
                        a.use_yn = "Y";
                        a.bb_no = ds_mapping.bb_no;
                        a.remark = "";
                        a.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        a.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();

                        a.reg_dt = DateTime.Now;
                        a.chg_dt = DateTime.Now;
                        int result = _TIMSService.InsertMaterialMapping(a);
                        
                    }
                    
                    var change_mt = mt;
                    change_mt.staff_qty = 0;
                    
                    //NẾU MÃ ĐẦU VÀO LÀ STA OR ROT THÌ STATUS = 008
                    if (change_mt.mt_cd.Contains("STA") || change_mt.mt_cd.Contains("ROT"))
                    {
                        change_mt.mt_sts_cd = "008";
                    }
                    else
                    {
                        change_mt.mt_sts_cd = "002";
                    }
                    //change_mt.mt_sts_cd = "002";
                    int test = _TIMSService.UpdateMaterialStaffQty(change_mt.staff_qty, change_mt.mt_sts_cd, change_mt.wmtid);
                    
                    var history = _TIMSService.FindOneBobbin_lct_hist(bb_no);
                    if (history != null)
                    {
                        history.mt_cd = b.mt_cd;
                        int updatedHistory = _TIMSService.UpdateMtCdBobbinHistInfo(history.mt_cd, history.blno);
                        
                    }
                    else
                    {
                       
                        var d_bobbin_lct_hist = new DBobbinLctHist();
                        //add vào bb_history
                        d_bobbin_lct_hist.bb_no = bb_no;
                        d_bobbin_lct_hist.bb_nm = ds_bb.bb_nm;
                        d_bobbin_lct_hist.mc_type = ds_bb.mc_type;
                        d_bobbin_lct_hist.mt_cd = b.mt_cd;
                        d_bobbin_lct_hist.use_yn = ds_bb.use_yn;
                        d_bobbin_lct_hist.chg_dt = DateTime.Now;
                        d_bobbin_lct_hist.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        d_bobbin_lct_hist.reg_dt = DateTime.Now;
                        d_bobbin_lct_hist.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        _TIMSService.InsertBobbinHist(d_bobbin_lct_hist);
                        
                    }
                    ds_bb.mt_cd = b.mt_cd;
                    ds_bb.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    _TIMSService.UpdateBobbinInfo(ds_bb.chg_id, ds_bb.mt_cd, ds_bb.bno);
                   
                    var ds1 = _TIMSService.FindOneWMaterialInfoMMS007(idReturn);

                    //tra ve enumerable nen phai to list
                    return Json(new { ds1 = ds1 }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception e)
                {
                   
                    return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
                }
             
            }
        }

        public ActionResult Getfacline_qc_PhanLoai(string item_vcd, string mt_lot)
        {
            var sql = new StringBuilder();
            sql.Append(" SELECT b.fqno,b.fq_no,b.check_qty, b.ml_tims,CONCAT(substr(b.work_dt,1,4),'-',substr(b.work_dt,5,2),'-',substr(b.work_dt,7,2),' ',substr(b.work_dt,9,2),':',substr(b.work_dt,11,2) )work_dt,(b.ok_qty),(b.check_qty)-(b.ok_qty) as defect_qty ")
           .Append(" from m_facline_qc as b where b.item_vcd='" + item_vcd + "' AND (b.ml_no='' or b.ml_no IS NULL) and b.ml_tims='" + mt_lot + "'  and fq_no like 'TI%' order by fq_no='TOTAL' desc ,check_qty ,fq_no  ");

            var data = new InitMethods().ReturnDataTableNonConstraints(sql);
            var result = GetJsonPersons(data);
            return result;
        }
        public ActionResult Getfacline_qc(string item_vcd, string mt_cd, string mt_lot)
        {
            var sql = new StringBuilder();
            sql.Append(" SELECT b.fqno,b.fq_no,CONCAT(substr(b.work_dt,1,4),'-',substr(b.work_dt,5,2),'-',substr(b.work_dt,7,2),' ',substr(b.work_dt,9,2),':',substr(b.work_dt,11,2) )work_dt,b.check_qty,(b.ok_qty),(b.check_qty)-(b.ok_qty) as defect_qty ")
            .Append(" from m_facline_qc as b where b.item_vcd='" + item_vcd + "' and b.ml_no='" + mt_cd + "' and b.ml_tims='" + mt_lot + "'  and fq_no like 'TI%' order by fq_no='TOTAL' desc ,check_qty ,fq_no  ");
            var data = new InitMethods().ReturnDataTableNonConstraints(sql);
            var result = GetJsonPersons(data);
            return result;
        }

    
        public ActionResult getmt_date_web_auto(int id_actual, int psid)
        {
            
            var check_vo = _TIMSService.FindOneWActual(id_actual);
            
            var check_stff = _TIMSService.FindOneDProUnitStaffById(psid);
            if (check_stff == null)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
            StringBuilder sql = new StringBuilder();
            if (check_vo.name == "OQC")
            {
                var result = _TIMSService.FindAllWMaterialInfoByStaffIdOQC(check_stff.staff_id, id_actual, check_stff.start_dt, check_stff.end_dt);
                return Json(new { result = true, data = result }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                
                IEnumerable<WMaterialnfo> results = _TIMSService.FindAllWMaterialInfoByStaffId(check_stff.staff_id, id_actual, check_stff.start_dt, check_stff.end_dt);
                return Json(results, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult getmt_date_web_auto_detail(int id_actual, int psid)
        {
            var check_vo = db.w_actual.Find(id_actual);
            var check_stff = db.d_pro_unit_staff.Where(x => x.psid == psid).SingleOrDefault();
            if (check_stff == null)
            {
                return Json(false, JsonRequestBehavior.AllowGet);

            }
            StringBuilder sql = new StringBuilder();
            if (check_vo.name == "OQC")
            {
                sql.Append(" SELECT * FROM w_material_info WHERE mt_sts_cd != '003'");
                sql.Append("  and staff_id_oqc = '" + check_stff.staff_id + "' AND id_actual_oqc = '" + id_actual + "'  AND ((SELECT k.reg_dt FROM w_material_mapping AS k  WHERE k.mt_lot IS NULL AND mt_cd=k.mt_cd LIMIT 1) between DATE_FORMAT('" + check_stff.start_dt + "', '%Y-%m-%d %H:%i:%s') and DATE_FORMAT('" + check_stff.end_dt + "', '%Y-%m-%d %H:%i:%s'))   \n");
            }
            else
            {
                sql.Append(" SELECT * FROM w_material_info WHERE mt_sts_cd != '003'");
                sql.Append("  and staff_id = '" + check_stff.staff_id + "' AND id_actual = '" + id_actual + "'  AND (reg_dt between DATE_FORMAT('" + check_stff.start_dt + "', '%Y-%m-%d %H:%i:%s') and DATE_FORMAT('" + check_stff.end_dt + "', '%Y-%m-%d %H:%i:%s')) and  \n");
                sql.Append(" mt_cd NOT IN (SELECT mt_cd FROM w_material_info WHERE id_actual='" + id_actual + "' AND orgin_mt_cd IS NOT null and mt_sts_cd!='003'  AND mt_cd LIKE CONCAT(orgin_mt_cd,'-MG%')  \n");
                sql.Append("  UNION SELECT orgin_mt_cd FROM w_material_info WHERE orgin_mt_cd IS NOT NULL ");
                sql.Append(" AND id_actual='" + id_actual + "' AND mt_cd LIKE CONCAT(orgin_mt_cd,'-DV%') AND mt_sts_cd!='003' AND staff_id='" + check_stff.staff_id + "')\n");
            }
            var data1 = db.w_material_info.SqlQuery(sql.ToString()).ToList<w_material_info>();

            var data = (from a in data1
                        orderby a.reg_dt descending
                        select new
                        {
                            wmtid = a.wmtid,
                            date = a.date,
                            mt_cd = a.mt_cd,
                            real_qty = a.real_qty,
                            mt_no = a.mt_no,
                            gr_qty = a.gr_qty,
                            bbmp_sts_cd = a.bbmp_sts_cd,
                            mt_qrcode = a.mt_qrcode,
                            lct_cd = a.lct_cd,
                            bb_no = a.bb_no,
                            mt_barcode = a.mt_barcode,
                            chg_dt = a.chg_dt,
                        }).ToList().AsEnumerable().Select(x => new
                        {
                            wmtid = x.wmtid,
                            date = (x.date != null && x.date != "" ? x.date.Substring(0, 4) + "-" + x.date.Substring(4, 2) + "-" + x.date.Substring(6, 2) : ""),
                            mt_cd = x.mt_cd,
                            mt_no = x.mt_no,
                            real_qty = x.real_qty,
                            gr_qty = x.gr_qty,
                            gr_qty1 = x.gr_qty,
                            mt_qrcode = x.mt_qrcode,
                            bb_no = x.bb_no,
                            mt_barcode = x.mt_barcode,
                            chg_dt = x.chg_dt,
                        }).ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Xoa_mt_pp_composite(int id)
        {
            try
            {
                if (id != 0)
                {
                    string sql = @"select * from w_material_info where wmtid = @1";
                    w_material_info a = db.Database.SqlQuery<w_material_info>(sql, new MySqlParameter("@1", id)).FirstOrDefault();
                    //kiem tra chua qua cong doan
                    if (a != null)
                    {
                        bool check = _TIMSService.CheckExistMaterialMappingById(a.mt_cd);
                        
                        if (check)
                        {
                            return Json(new { result = false, message = Constant.Exist + "mapping" }, JsonRequestBehavior.AllowGet);
                        }
                        if (a.gr_qty > 0 || a.real_qty > 0)
                        {
                            return Json(new { result = false, message = Constant.CannotDelete }, JsonRequestBehavior.AllowGet);
                        }

                        //kiểm tra hàng đã kiểm rồi
                        var check_facline = _TIMSService.CheckExistFacline(a.mt_cd);
                        if (check_facline)
                        {
                            return Json(new { result = false, message = Constant.QCPass }, JsonRequestBehavior.AllowGet);
                        }
                        var mt_cd = a.mt_cd.Remove(a.mt_cd.Length - 12, 12);
                        var ds_mapping = db.w_material_mapping.Where(x => x.mt_lot == a.mt_cd).ToList();
                        foreach (var item in ds_mapping)
                        {
                            w_material_mapping b = db.w_material_mapping.Find(item.wmmid);
                            db.Entry(b).State = EntityState.Deleted;
                        }

                        db.Entry(a).State = EntityState.Deleted;
                        var ds = db.w_material_info.Where(x => x.mt_cd.StartsWith(mt_cd)).ToList().Sum(x => x.gr_qty);

                        //xã bobin
                        var find_bb = db.d_bobbin_info.Where(x => x.bb_no == a.bb_no).SingleOrDefault();
                        if (find_bb != null)
                        {
                            find_bb.mt_cd = "";
                            find_bb.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                            db.Entry(find_bb).State = EntityState.Modified;
                        }
                        //xóa history
                        var find_history = db.d_bobbin_lct_hist.Where(x => x.bb_no == a.bb_no && x.mt_cd == a.mt_cd).SingleOrDefault();
                        if (find_history != null)
                        {
                            db.Entry(find_history).State = EntityState.Deleted;
                        }
                        db.SaveChanges(); // line that threw exception
                        return Json(new { ds = ds }, JsonRequestBehavior.AllowGet);
                    }
                }
                return Json(false, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult CancelEA(int id, int psid)
        {
            try
            {
                if (id != 0)
                {
                    //w_material_info a = db.w_material_info.Find(id);
                    w_material_info a = _IWOService.GetWMaterialInfo(id);


                    //kiểm tra coi n đã hết thời gian làm việc chưa
                    if (_TIMSService.CheckStaffShiftForId(a.id_actual, psid) == 0)
                    {
                        return Json(new { result = false, message = Constant.EndShift }, JsonRequestBehavior.AllowGet);
                    }
                    //String varname1 = "";
                    //varname1 = varname1 + "SELECT * FROM d_pro_unit_staff AS a " + "\n";
                    //varname1 = varname1 + "WHERE a.id_actual='" + a.id_actual + "' and a.psid='" + psid + "' AND (NOW() BETWEEN " + "\n";
                    //varname1 = varname1 + "Date_format(a.start_dt, '%Y-%m-%d %H:%i:%s') " + "\n";
                    //varname1 = varname1 + " AND Date_format(a.end_dt, '%Y-%m-%d %H:%i:%s'))";
                    //var data = db.Database.SqlQuery<d_pro_unit_staff>(varname1).ToList().Count();
                    //if (data == 0)
                    //{
                    //    return Json(new { result = false, message = Constant.EndShift }, JsonRequestBehavior.AllowGet);
                    //}

                    //kiểm tra mã đầu ra đã qua công đoạn khác chưa
                    //if (db.w_material_mapping.Any(x => x.mt_cd == a.mt_cd))
                    //{
                    //    return Json(new { result = false, message = Constant.Other }, JsonRequestBehavior.AllowGet);
                    //}
                    var check_exit2 = _TIMSService.FindAllMaterialMappingByMtCd(a.mt_cd);
                    if (check_exit2.Count() > 0)
                    {
                        return Json(new { result = false, message = Constant.Other }, JsonRequestBehavior.AllowGet);
                    }


                    if (a != null)
                    {
                        if (a.gr_qty > 0 || a.real_qty > 0)
                        {
                            return Json(new { result = false, message = Constant.Tested }, JsonRequestBehavior.AllowGet);
                        }
                        //xã bobbin đầu ra
                        var bo_bbnmoi = a.bb_no;
                        //var find_bb = db.d_bobbin_info.Where(x => x.bb_no == a.bb_no).SingleOrDefault();
                        var find_bb = _IWOService.GetBobbinInfo(a.bb_no);
                        if (find_bb == null)
                        {
                            return Json(new { result = false, message = Constant.NotFound + " Container" }, JsonRequestBehavior.AllowGet);
                        }
                        var ds_mapping = db.w_material_mapping.Where(x => x.mt_lot == a.mt_cd).ToList();
                        var lot = "";
                        var bo_bbcu = "";
                        foreach (var item in ds_mapping)
                        {
                            bo_bbcu = item.bb_no;
                            //sửa lại số lượng về cũ và xóa mapping
                            Cancel_mapping(item.wmmid, psid);
                           // Log.Info("TIMS=>CancelEA ()mt_cd " + item.mt_cd + " bobin: " + item.bb_no);


                            //add history trở về nguyên trạng thái ban đầu
                            //add vào bb_history
                            var history = new d_bobbin_lct_hist();
                            if (!db.d_bobbin_lct_hist.Any(x => x.bb_no == find_bb.bb_no && x.mt_cd == item.mt_cd))
                            {
                                lot = item.mt_cd;
                                history.bb_no = find_bb.bb_no;
                                history.bb_nm = find_bb.bb_nm;
                                history.mc_type = find_bb.mc_type;
                                history.mt_cd = item.mt_cd;
                                history.use_yn = find_bb.use_yn;
                                history.chg_dt = DateTime.Now;
                                history.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                                history.reg_dt = DateTime.Now;
                                history.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                                db.Entry(history).State = EntityState.Added;
                               // Log.Info("TIMS=>CancelEA (Add bobin d_bobbin_lct_hist)mt_cd " + history.mt_cd + " bobin: " + find_bb.bb_no);

                            }

                        }

                        find_bb.mt_cd = lot;
                        find_bb.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        db.Entry(find_bb).State = EntityState.Modified;
                       // Log.Info("TIMS=>CancelEA (thay doi d_bobbin_info 1)mt_cd " + find_bb.mt_cd + " bobin: " + find_bb.bb_no);

                        //db.SaveChanges(); // line that threw exception
                        //xóa history
                        var find_history = db.d_bobbin_lct_hist.Where(x => x.bb_no == a.bb_no && x.mt_cd == a.mt_cd).SingleOrDefault();
                        if (find_history != null)
                        {
                            db.Entry(find_history).State = EntityState.Deleted;
                            //db.SaveChanges(); // line that threw exception
                           // Log.Info("TIMS=>CancelEA (xoa bobin d_bobbin_lct_hist 1)mt_cd " + find_history.mt_cd + " bobin: " + find_history.bb_no);

                        }
                        //delete mã đầu ra
                        int DeleteWmaterialOutput = _IWOService.Deletew_material_info(a.wmtid);
                        //db.Entry(a).State = EntityState.Deleted;

                        if (bo_bbcu != bo_bbnmoi)
                        {
                            //xã bobin mới đó
                            find_bb.mt_cd = "";
                            find_bb.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                            db.Entry(find_bb).State = EntityState.Modified;

                           // Log.Info("TIMS=>CancelEA (thay doi d_bobbin_info 2)mt_cd " + find_bb.mt_cd + " bobin: " + find_bb.bb_no);

                            //xóa history_bbmoi
                            var find_historynew = db.d_bobbin_lct_hist.Where(x => x.bb_no == a.bb_no && x.mt_cd == lot).SingleOrDefault();
                            if (find_historynew != null)
                            {
                                db.Entry(find_historynew).State = EntityState.Deleted;
                              //  Log.Info("TIMS=>CancelEA (xoa bobin d_bobbin_lct_hist 2)mt_cd " + find_historynew.mt_cd + " bobin: " + find_historynew.bb_no);
                            }
                        }
                        db.SaveChanges(); // line that threw exception

                        return Json(new { result = true }, JsonRequestBehavior.AllowGet);
                    }
                }
                return Json(new { result = false, message = Constant.CannotCancel }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }
        //public ActionResult CancelEA(int id, int psid)
        //{
        //    try
        //    {
        //        if (id != 0)
        //        {
        //            //w_material_info a = db.w_material_info.Find(id);
        //            w_material_info a = _IWOService.GetWMaterialInfo(id);


        //            //kiểm tra coi n đã hết thời gian làm việc chưa
        //            if (_TIMSService.CheckStaffShiftForId(a.id_actual, psid) == 0)
        //            {
        //                return Json(new { result = false, message = Constant.EndShift }, JsonRequestBehavior.AllowGet);
        //            }
        //            //String varname1 = "";
        //            //varname1 = varname1 + "SELECT * FROM d_pro_unit_staff AS a " + "\n";
        //            //varname1 = varname1 + "WHERE a.id_actual='" + a.id_actual + "' and a.psid='" + psid + "' AND (NOW() BETWEEN " + "\n";
        //            //varname1 = varname1 + "Date_format(a.start_dt, '%Y-%m-%d %H:%i:%s') " + "\n";
        //            //varname1 = varname1 + " AND Date_format(a.end_dt, '%Y-%m-%d %H:%i:%s'))";
        //            //var data = db.Database.SqlQuery<d_pro_unit_staff>(varname1).ToList().Count();
        //            //if (data == 0)
        //            //{
        //            //    return Json(new { result = false, message = Constant.EndShift }, JsonRequestBehavior.AllowGet);
        //            //}
         
        //            //kiểm tra mã đầu ra đã qua công đoạn khác chưa
        //            //if (db.w_material_mapping.Any(x => x.mt_cd == a.mt_cd))
        //            //{
        //            //    return Json(new { result = false, message = Constant.Other }, JsonRequestBehavior.AllowGet);
        //            //}
        //            var check_exit2 = _TIMSService.FindAllMaterialMappingByMtCd(a.mt_cd);
        //            if (check_exit2.Count() > 0)
        //            {
        //                return Json(new { result = false, message = Constant.Other }, JsonRequestBehavior.AllowGet);
        //            }


        //            if (a != null)
        //            {
        //                if (a.gr_qty > 0 || a.real_qty > 0)
        //                {
        //                    return Json(new { result = false, message = Constant.Tested }, JsonRequestBehavior.AllowGet);
        //                }
        //                //xã bobbin đầu ra
        //                var bo_bbnmoi = a.bb_no;
        //                var find_bb = db.d_bobbin_info.Where(x => x.bb_no == a.bb_no).SingleOrDefault();
        //                if (find_bb == null)
        //                {
        //                    return Json(new { result = false, message = Constant.NotFound + " Container" }, JsonRequestBehavior.AllowGet);
        //                }
        //                var ds_mapping = db.w_material_mapping.Where(x => x.mt_lot == a.mt_cd).ToList();
        //                var lot = "";
        //                var bo_bbcu = "";
        //                foreach (var item in ds_mapping)
        //                {
        //                    bo_bbcu = item.bb_no;
        //                    //sửa lại số lượng về cũ và xóa mapping
        //                    Cancel_mapping(item.wmmid, psid);
        //                    Log.Info("TIMS=>CancelEA ()mt_cd " + item.mt_cd + " bobin: " + item.bb_no);


        //                    //add history trở về nguyên trạng thái ban đầu
        //                    //add vào bb_history
        //                    var history = new d_bobbin_lct_hist();
        //                    if (!db.d_bobbin_lct_hist.Any(x => x.bb_no == find_bb.bb_no && x.mt_cd == item.mt_cd))
        //                    {
        //                        lot = item.mt_cd;
        //                        history.bb_no = find_bb.bb_no;
        //                        history.bb_nm = find_bb.bb_nm;
        //                        history.mc_type = find_bb.mc_type;
        //                        history.mt_cd = item.mt_cd;
        //                        history.use_yn = find_bb.use_yn;
        //                        history.chg_dt = DateTime.Now;
        //                        history.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
        //                        history.reg_dt = DateTime.Now;
        //                        history.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
        //                        db.Entry(history).State = EntityState.Added;
        //                        Log.Info("TIMS=>CancelEA (Add bobin d_bobbin_lct_hist)mt_cd " + history.mt_cd + " bobin: " + find_bb.bb_no);

        //                    }
                            
        //                }

        //                find_bb.mt_cd = lot;
        //                find_bb.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
        //                db.Entry(find_bb).State = EntityState.Modified;
        //                Log.Info("TIMS=>CancelEA (thay doi d_bobbin_info 1)mt_cd " + find_bb.mt_cd + " bobin: " + find_bb.bb_no);

        //                //db.SaveChanges(); // line that threw exception
        //                //xóa history
        //                var find_history = db.d_bobbin_lct_hist.Where(x => x.bb_no == a.bb_no && x.mt_cd == a.mt_cd).SingleOrDefault();
        //                if (find_history != null)
        //                {
        //                    db.Entry(find_history).State = EntityState.Deleted;
        //                    //db.SaveChanges(); // line that threw exception
        //                    Log.Info("TIMS=>CancelEA (xoa bobin d_bobbin_lct_hist 1)mt_cd " + find_history.mt_cd + " bobin: " + find_history.bb_no);

        //                }
        //                //delete mã đầu ra
        //                int DeleteWmaterialOutput = _IWOService.Deletew_material_info(a.wmtid);
        //                //db.Entry(a).State = EntityState.Deleted;
                       
        //                if (bo_bbcu != bo_bbnmoi)
        //                {
        //                    //xã bobin mới đó
        //                    find_bb.mt_cd = "";
        //                    find_bb.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
        //                    db.Entry(find_bb).State = EntityState.Modified;
                          
        //                    Log.Info("TIMS=>CancelEA (thay doi d_bobbin_info 2)mt_cd " + find_bb.mt_cd + " bobin: " + find_bb.bb_no);

        //                    //xóa history_bbmoi
        //                    var find_historynew = db.d_bobbin_lct_hist.Where(x => x.bb_no == a.bb_no && x.mt_cd == lot).SingleOrDefault();
        //                    if (find_historynew != null)
        //                    {
        //                        db.Entry(find_historynew).State = EntityState.Deleted;
        //                        Log.Info("TIMS=>CancelEA (xoa bobin d_bobbin_lct_hist 2)mt_cd " + find_historynew.mt_cd + " bobin: " + find_historynew.bb_no);
        //                    }
        //                }
        //                db.SaveChanges(); // line that threw exception

        //                return Json(new { result = true }, JsonRequestBehavior.AllowGet);
        //            }
        //        }
        //        return Json(new { result = false, message = Constant.CannotCancel }, JsonRequestBehavior.AllowGet);
        //    }
        //    catch (Exception e)
        //    {
        //        return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
        //    }
        //}

        public ActionResult ds_mapping_w(string mt_cd)
        {
            try
            {
                StringBuilder varname1 = new StringBuilder();
                varname1.Append("SELECT a.wmmid,a.mt_lot,a.mt_cd,a.sts_share,DATE_FORMAT(a.mapping_dt, '%Y-%m-%d %H:%i:%s') mapping_dt \n");
                varname1.Append(",a.use_yn,a.reg_dt,b.gr_qty,b.mt_no, \n");
                varname1.Append("b.bb_no, \n");
                varname1.Append("(CASE WHEN a.use_yn='N' THEN \n");
                varname1.Append("b.gr_qty ELSE 0 END \n");
                varname1.Append(")Used \n");
                varname1.Append(",(CASE WHEN a.use_yn='N' THEN \n");
                varname1.Append("`Find_Remain`(a.mt_cd,b.orgin_mt_cd,a.reg_dt) ELSE 0 END \n");
                varname1.Append(")Remain \n");
                varname1.Append("FROM w_material_mapping AS a \n");
                varname1.Append("JOIN w_material_info AS b ON a.mt_cd=b.mt_cd \n");
                varname1.Append("WHERE a.mt_lot ='" + mt_cd + "' \n");
                varname1.Append("ORDER BY a.use_yn='Y' DESC,a.reg_dt DESC");
                var data = new InitMethods().ReturnDataTableNonConstraints(varname1);
                var KQ = GetJsonPersons(data);
                return Json(KQ.Data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Console.WriteLine("Disconected");
                return Json(e, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult Finish_back(int wmmid)
        {
            try
            {
                //không sử dụng nữa cho về flase
                if (wmmid > 0)
                {
                    return Json(new { result = false, message = "Chức năng này đã bị hủy" }, JsonRequestBehavior.AllowGet);
                }
                
                var data = _TIMSService.FindOneWMaterialMappingById(wmmid);
                if (data.use_yn == "Y")
                {
                   
                    var ds = _TIMSService.FindOneMaterialInfoByMTLot(data.mt_cd);
                   
                    if (ds != null)
                    {
                        data.use_yn = "N";
                        data.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        _TIMSService.UpdateUseYnMaterialMapping(data.use_yn, data.chg_id, data.wmmid);
                       
                        var id_fo1 = ds.wmtid;
                        
                        w_material_info update1 = _TIMSService.FindOneMaterialInfoById(ds.wmtid);
                        update1.mt_sts_cd = "005";
                        update1.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        _TIMSService.UpdateWMaterialInfoById(update1.mt_sts_cd, update1.chg_id, update1.wmtid);
                      
                        if (update1.bb_no != "" && update1.bb_no != null)
                        {
                            //xã bobin
                           
                            var check_bb = _TIMSService.FindOneDBobbinInfo(update1.bb_no, update1.mt_cd);
                            if (check_bb != null)
                            {
                                //find history để xóa
                               
                                var history = _TIMSService.FindAllBobbin_lct_histByBBNoMTCd(check_bb.bb_no, check_bb.mt_cd).ToList();
                                if (history.Count() > 0)
                                {
                                    foreach (var item in history)
                                    {
                                        
                                        _TIMSService.DeleteBobbinHist(item.blno);
                                       
                                    }
                                }
                                check_bb.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                                check_bb.mt_cd = "";
                                
                                _TIMSService.UpdateBobbinInfo(check_bb.chg_id, check_bb.mt_cd, 0, check_bb.bno);
                            }
                        }
                        var check_finish = _TIMSService.FindAllWMaterialMappingByMtcdUseYn(update1.mt_cd, "Y").ToList();
                        foreach (var item in check_finish)
                        {
                            item.use_yn = "N";
                            item.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                            _TIMSService.UpdateUseYnMaterialMapping(item.use_yn, item.chg_id, item.wmmid);
                           
                        }
                        
                        return Json(new { result = true, message = Constant.EndLotSuccess, use_yn = data.use_yn }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    //kiem tra da mapping voi cong doan khac chua

                    
                    var check_exit2 = _TIMSService.FindAllMaterialMappingByMtCd(data.mt_lot);
                    if (check_exit2.Count() > 0)
                    {
                       
                        return Json(new { result = false, message = Constant.Other }, JsonRequestBehavior.AllowGet);
                    }

                    var ds = db.w_material_info.Where(x => x.mt_cd == data.mt_cd).ToList();
                    if (ds.Any(x => x.mt_cd.StartsWith(x.orgin_mt_cd + "-MG")))
                    {
                        return Json(new { result = false, message = Constant.MergeCodeCannotCancel, use_yn = data.use_yn }, JsonRequestBehavior.AllowGet);
                    }
                    if (ds.Count == 1)
                    {
                        //trở về trạng thái đầu

                        data.use_yn = "Y";
                        data.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        _TIMSService.UpdateUseYnMaterialMapping(data.use_yn, data.chg_id, data.wmmid);
                      

                        var id_fo1 = ds.FirstOrDefault().wmtid;
                        w_material_info update1 = db.w_material_info.Find(id_fo1);
                        update1.mt_sts_cd = "002";
                        update1.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        _TIMSService.UpdateWMaterialInfoById(update1.mt_sts_cd, update1.chg_id, update1.wmtid);
                        
                        if (update1.bb_no != "" && update1.bb_no != null)
                        {
                            //xã bobin
                            
                            var check_bb = _TIMSService.FindOneDBobbinInfo(update1.bb_no);
                            if (check_bb != null)
                            {
                               
                                check_bb.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                                check_bb.mt_cd = update1.mt_cd;
                                
                                _TIMSService.UpdateBobbinInfo(check_bb.chg_id, check_bb.mt_cd, 1, check_bb.bno);
                               
                                //add history đã xóa
                                //nếu n chưa tồn tại
                                var listbobin = _TIMSService.FindAllBobbin_lct_histByBBNoMTCd(check_bb.bb_no, data.mt_cd);
                               
                                if (listbobin.Count()  == 0)
                                {
                                    var history = new DBobbinLctHist();
                                    history.bb_no = check_bb.bb_no;
                                    history.bb_nm = check_bb.bb_nm;
                                    history.mt_cd = data.mt_cd;
                                    history.mc_type = check_bb.mc_type;
                                    history.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                                    history.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                                    history.reg_dt = DateTime.Now;
                                    history.chg_dt = DateTime.Now;
                                    history.del_yn = "N";
                                    history.use_yn = "Y";
                                    _TIMSService.InsertToBobbinHist(history);
                                   
                                }
                            }
                        }
                       
                        return Json(new { result = true, message = Constant.BackSuccess, use_yn = data.use_yn }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = e.Message }, JsonRequestBehavior.AllowGet);
               
            }
        }
        public JsonResult stop_cp(int wmmid, int psid)
        {
            try
            {
                //kiểm tra coi n đã hết thời gian làm việc chưa
                int ca = _TIMSService.CheckShift(psid);
                if (ca == 0)
                {
                    return Json(new { result = false, message = Constant.EndShift }, JsonRequestBehavior.AllowGet);
                }
                var data = _TIMSService.FindOneWMaterialMappingById(wmmid);
                data.sts_share = "N";
                _TIMSService.UpdateWMaterialMappingById(data.sts_share, wmmid);
                return Json(new { result = true, message = Constant.Success }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { result = false, message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult savereturn_lot(int soluong, string mt_cd, string mt_lot)
        {
            try
            {
                var check_exit = db.w_material_info.Where(x => x.mt_cd == mt_cd).SingleOrDefault();
                if (check_exit == null)
                {
                    return Json(new { result = false, message = "MT Code DONT Exits" }, JsonRequestBehavior.AllowGet);
                }
                var check_exit1 = db.w_material_mapping.Where(x => x.mt_lot == mt_lot && x.mt_cd == mt_cd).SingleOrDefault();
                if (check_exit1 == null)
                {
                    return Json(new { result = false, message = "MT LOT DONT Exits" }, JsonRequestBehavior.AllowGet);
                }
                //kiểm tra đã finish chưa
                if (check_exit1.use_yn == "N")
                {
                    return Json(new { result = false, message = "MT LOT Already Finish" }, JsonRequestBehavior.AllowGet);
                }

                if (soluong == 0)
                {
                    return Json(new { result = false, message = "Length is not valid" }, JsonRequestBehavior.AllowGet);
                }

                //kiểm tra số lượng có vượt quá k

                if (soluong > check_exit.gr_qty)
                {
                    return Json(new { result = false, message = "Length is not valid" }, JsonRequestBehavior.AllowGet);
                }

                //tách số lượng
                var soluongcl_mt_cd = check_exit.gr_qty - (soluong);
                //finish
                check_exit1.use_yn = "N";
                check_exit1.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                db.Entry(check_exit1).State = EntityState.Modified;
                db.SaveChanges();
                // chuyển trạng thái về 005 và update số lượng đã sử dụng
                check_exit.mt_sts_cd = "005";
                check_exit.gr_qty = soluongcl_mt_cd;
                check_exit.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                db.Entry(check_exit).State = EntityState.Modified;
                db.SaveChanges(); // line that threw exception
                var count_return = db.w_material_info.Where(x => x.sts_update == "return" && x.orgin_mt_cd == mt_cd).Count();

                StringBuilder varname1 = new StringBuilder();
                varname1.Append("INSERT INTO w_material_info \n");
                varname1.Append("( staff_id,staff_id_oqc,`id_actual`, `mt_type`, `mt_cd`, `mt_no`,  `gr_qty`,  `rd_no`, `sd_no`, `recevice_dt`, `date`, `return_date`, `alert_NG`, `expiry_dt`, `dt_of_receipt`, \n");
                varname1.Append("`expore_dt`, `recevice_dt_tims`, `lot_no`, `mt_barcode`, `mt_qrcode`, `mt_sts_cd`, `bb_no`, `bbmp_sts_cd`, `lct_cd`, `lct_sts_cd`, `from_lct_cd`, `to_lct_cd`, `output_dt`, `input_dt`, `orgin_mt_cd`, `remark`, `sts_update`, `use_yn`, `reg_id`, `reg_dt`, `chg_id`, `chg_dt`,`real_qty`) \n");
                varname1.Append("SELECT  `id_actual`,  `mt_type`, CONCAT(mt_cd,'-','RT" + (count_return + 1) + "'), `mt_no`, " + soluong + ",  `rd_no`, `sd_no`, `recevice_dt`, `date`,'" + DateTime.Now.ToString("yyyyMMddHHmmss") + "', `alert_NG`, `expiry_dt`, `dt_of_receipt`, `expore_dt`, `recevice_dt_tims`, `lot_no`, CONCAT(mt_cd,'-','RT" + (count_return + 1) + "'), CONCAT(mt_cd,'-','RT" + (count_return + 1) + "'),'004', `bb_no`, `bbmp_sts_cd`, `lct_cd`, `lct_sts_cd`, `from_lct_cd`, `to_lct_cd`, `output_dt`, `input_dt`, mt_cd, `remark`,'return', `use_yn`, '" + check_exit.chg_id + "', NOW(), '" + check_exit.chg_id + "', NOW()," + soluong + " \n");
                varname1.Append("FROM w_material_info \n");
                varname1.Append("WHERE mt_cd='" + mt_cd + "';");

                int effect_rows = new Excute_query().Execute_NonQuery(varname1);
                if (check_exit.bb_no != "" && check_exit.bb_no != null)
                {
                    //xã bobin
                    var check_bb = db.d_bobbin_info.Where(x => x.bb_no == check_exit.bb_no && x.mt_cd == check_exit.mt_cd).ToList().SingleOrDefault();
                    if (check_bb != null)
                    {
                        check_bb.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        check_bb.mt_cd = mt_cd + "-" + "RT" + (count_return + 1);
                        db.Entry(check_bb).State = EntityState.Modified;
                        db.SaveChanges(); // line that threw exception
                                          //find history để xóa
                        var history = db.d_bobbin_lct_hist.Where(x => x.bb_no == check_bb.bb_no && x.mt_cd == mt_cd).SingleOrDefault();
                        if (history != null)
                        {
                            history.mt_cd = check_bb.mt_cd;
                            db.Entry(history).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                    }
                }
                //kiem tra co bao nhieu mt_cd da qua cong doan ma chua finish

                var check_finish = db.w_material_mapping.Where(x => x.mt_cd == check_exit.mt_cd && x.use_yn == "Y").ToList();
                foreach (var item in check_finish)
                {
                    item.use_yn = "N";
                    item.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    db.Entry(item).State = EntityState.Modified;
                    db.SaveChanges(); 
                }
                StringBuilder varname2 = new StringBuilder();
                varname2.Append("SELECT a.wmmid,a.mt_lot,a.mt_cd \n");
                varname2.Append(",a.use_yn,a.reg_dt,b.gr_qty,b.mt_no, \n");
                varname2.Append("b.bb_no, \n");
                varname2.Append("(CASE WHEN a.use_yn='N' THEN \n");
                varname2.Append("b.gr_qty ELSE 0 END \n");
                varname2.Append(")Used \n");
                varname2.Append(",(CASE WHEN a.use_yn='N' THEN \n");
                varname2.Append("`Find_Remain`(a.mt_cd,b.orgin_mt_cd,a.reg_dt) ELSE 0 END\n");
                varname2.Append(")Remain \n");
                varname2.Append("FROM w_material_mapping AS a \n");
                varname2.Append("JOIN w_material_info AS b ON a.mt_cd=b.mt_cd \n");
                varname2.Append("WHERE a.wmmid='" + check_exit1.wmmid + "' \n");
                varname2.Append("ORDER BY a.use_yn='Y' DESC,a.reg_dt DESC");
                var data = new DataTable();
                using (var cmd = db.Database.Connection.CreateCommand())
                {
                    db.Database.Connection.Open();
                    cmd.CommandText = varname2.ToString();
                    using (var reader = cmd.ExecuteReader())
                    {
                        data.Load(reader);
                    }
                }
                var KQ = GetJsonPersons(data);
                return Json(new { result = true, message = "Success", kq = KQ.Data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult Cancel_mapping(int wmmid, int psid)
        {
            try
            {
                var check_exits = db.w_material_mapping.Find(wmmid);
                if (check_exits.sts_share == "N")
                {
                    return Json(new { result = false, message = Constant.Divide }, JsonRequestBehavior.AllowGet);
                }
                var find_mt_cd = db.w_material_info.Where(x => x.mt_cd == check_exits.mt_cd).SingleOrDefault();
                if (check_exits == null || find_mt_cd == null)
                {
                    return Json(new { result = false, message = "MT Code " + Constant.NotFound }, JsonRequestBehavior.AllowGet);
                }
                var check_exits_mapping = db.w_material_mapping.Where(x => x.mt_cd == check_exits.mt_lot).ToList();
                if (check_exits_mapping.Count > 0)
                {
                    return Json(new { result = false, message = Constant.Other }, JsonRequestBehavior.AllowGet);
                }
                var mt_lot = check_exits.mt_lot;

                var find_lot = db.w_material_info.Where(x => x.mt_cd == mt_lot).SingleOrDefault();

                //kiểm tra coi n đã hết thời gian làm việc chưa
                String varname1 = "";
                varname1 = varname1 + "SELECT * FROM d_pro_unit_staff AS a " + "\n";
                varname1 = varname1 + "WHERE a.id_actual='" + find_lot.id_actual + "' and a.psid='" + psid + "' AND (NOW() BETWEEN " + "\n";
                varname1 = varname1 + "Date_format(a.start_dt, '%Y-%m-%d %H:%i:%s') " + "\n";
                varname1 = varname1 + " AND Date_format(a.end_dt, '%Y-%m-%d %H:%i:%s'))";
                var data = db.Database.SqlQuery<d_pro_unit_staff>(varname1).ToList().Count();
                if (data == 0)
                {
                    return Json(new { result = false, message = Constant.EndShift }, JsonRequestBehavior.AllowGet);
                }
                //trường hợp cuộn còn dư mang đi qua công đoạn

                var check_mapping1 = db.w_material_mapping.Where(x => x.mt_cd == check_exits.mt_cd && x.mt_lot != check_exits.mt_lot).OrderByDescending(x => x.mapping_dt).ToList();

                foreach (var item in check_mapping1)
                {
                    //so sánh thời gian mapping
                    if (Convert.ToInt64(item.mapping_dt) > Convert.ToInt64(check_exits.mapping_dt))
                    {
                        return Json(new { result = false, message = Constant.Other }, JsonRequestBehavior.AllowGet);
                    }
                }
                var check_exits_return = db.w_material_info.Where(x => x.orgin_mt_cd == check_exits.mt_cd && x.sts_update == "return").ToList();
                if (check_exits_return.Count > 0)
                {
                    return Json(new { result = false, message = Constant.Started }, JsonRequestBehavior.AllowGet);
                }

                //kiểm tra đã check QC chưa và xóa

                var check_qc = db.m_facline_qc.Where(x => x.ml_no == find_mt_cd.mt_cd && x.ml_tims == check_exits.mt_lot && x.fq_no.StartsWith("TI")).ToList();
                var soldacheck = check_qc.Sum(x => x.check_qty);
                var soldaok = check_qc.Sum(x => x.ok_qty);
                foreach (var item in check_qc)
                {
                    var qc_detail = db.m_facline_qc_value.Where(x => x.fq_no == item.fq_no).ToList();
                    foreach (var item1 in qc_detail)
                    {
                        db.Entry(item1).State = EntityState.Deleted;
                    }
                    db.Entry(item).State = EntityState.Deleted;
                }
                //nhâp số lượng NG cũ vào
                var check_NG = db.w_material_info.Where(x => x.mt_cd.StartsWith(find_mt_cd.mt_cd + ("-NG")) && x.lct_cd.StartsWith("006")).SingleOrDefault();
                if (check_NG != null)
                {
                    //xóa NG
                    db.Entry(check_NG).State = EntityState.Deleted;
                }
                find_mt_cd.gr_qty = soldacheck + find_mt_cd.gr_qty;
                //tra về trạng thái cũ
                //update lại số lượng của mt_lot tổng
                if (find_lot != null)
                {
                    if (find_lot.gr_qty > 0)
                    {

                        find_lot.gr_qty = find_lot.gr_qty - soldaok;
                        find_lot.real_qty = find_lot.real_qty - soldaok;
                        find_lot.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                      //  Log.Info("TIMS=>Cancel_mapping" + "update san luong lai mt_cd=" + find_lot.mt_cd + " SL=" + find_lot.gr_qty);
                        db.Entry(find_lot).State = EntityState.Modified;
                    }
                }
                var chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                var checkEA = _TIMSService.CheckEA(find_lot.id_actual);
                if (checkEA.Contains("200"))
                {
                    //kiểm tra nếu là EA thì bobibn đầu ra là bobin đầu vào
                    find_mt_cd.bb_no = check_exits.bb_no;
                  


                    // nếu đầu vào đã add đồ đựng mới thì xả bobbin dạng ea
                    //update table d_bobbin_info
                    _TIMSService.UpdateBobbinInfo(chg_id, find_mt_cd.bb_no, find_mt_cd.mt_cd);
                    //xóa table DeleteBobbinHistory
                    _TIMSService.DeleteBobbinHistory(find_mt_cd.bb_no, find_mt_cd.mt_cd);
                    //kiểm tra nếu là EA thì bobibn đầu ra là bobin đầu vào
                }

                //var sts = (find_mt_cd.sts_update == "" || find_mt_cd == null ? "008" : "002");
                //find_mt_cd.mt_sts_cd = sts;
               
                find_mt_cd.chg_id = chg_id;
                db.Entry(find_mt_cd).State = EntityState.Modified;
                //trả lại trạng thái cũ và xóa khỏi bảng mapping
                db.Entry(check_exits).State = EntityState.Deleted;

                db.SaveChanges();


               
                return Json(new { result = true, message = Constant.CancelSuccess }, JsonRequestBehavior.AllowGet);



            }
            catch (Exception)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult PartialView_View_QC_WEB(string item_vcd)
        {
            //lấy hết tất cả qc_itemcheck_mt
            var qc_itemcheck_mt = new List<qc_itemcheck_mt_Model>();
            var qc_itemcheck_mt2 = new List<qc_itemcheck_mt_Model>();
            qc_itemcheck_mt = db.qc_itemcheck_mt
               .Where(x => x.item_vcd == item_vcd && x.del_yn.Equals("N"))
               .Select(x => new qc_itemcheck_mt_Model
               {
                   qc_itemcheck_mt__check_subject = x.check_subject,
                   qc_itemcheck_mt__check_id = x.check_id,
                   qc_itemcheck_mt__icno = x.icno,
               })
               .ToList();

            foreach (var item in qc_itemcheck_mt)
            {
                var view_qc_Model = new List<view_qc_Model>();

                //lấy hết tất cả qc_itemcheck_dt
                var qc_itemcheck_dt = db.qc_itemcheck_dt
                    .Where(x => x.item_vcd.Equals(item_vcd) && x.check_id.Equals(item.qc_itemcheck_mt__check_id) && (x.del_yn == "N") && (x.defect_yn == "Y")).ToList();
                if (qc_itemcheck_dt.Count > 0)
                {
                    foreach (var item1 in qc_itemcheck_dt)
                    {
                        var view_qc_Model_item = new view_qc_Model();
                        //add check_name
                        view_qc_Model_item.qc_itemcheck_dt__check_name = item1.check_name;
                        view_qc_Model_item.qc_itemcheck_dt__icdno = item1.icdno;

                        //add view_qc_Model
                        view_qc_Model.Add(view_qc_Model_item);
                    }
                }

                item.view_qc_Model = view_qc_Model;
            }
            qc_itemcheck_mt2.AddRange(qc_itemcheck_mt);

            foreach (var ds in qc_itemcheck_mt2)
            {
                if (ds.view_qc_Model.Count == 0)
                {
                    qc_itemcheck_mt.Remove(ds);
                }
            }
            return PartialView(qc_itemcheck_mt);
        }
        public ActionResult Insert_w_product_qc(string item_vcd, int check_qty, int ok_qty, string mt_cd,
            string mt_lot, int? remain, string check_qty_error, int psid, m_facline_qc MFQC, m_facline_qc_value MFQCV)
        {
            #region insert info
            try
            {

                //kiem tra check co dung so luong k
                //kiểm tra coi n đã hết thời gian làm việc chưa
                if (check_qty < ok_qty)
                {
                    return Json(new { result = false, message = "Vui lòng kiểm tra lại sản lượng của bạn nhập !!!" }, JsonRequestBehavior.AllowGet);
                }
                if (remain < 0)
                {
                    return Json(new { result = false, message = "Remain number phải là số dương!" }, JsonRequestBehavior.AllowGet);
                }
                var userAcount = Session["userid"] == null ? null : Session["userid"].ToString();
                var userWIP = "";
                if (!string.IsNullOrEmpty(userAcount))
                {
                    var dsMbInfo = _IWOService.GetMbInfoGrade(userAcount);
                    userWIP = dsMbInfo.grade;
                }

                int dem = _TIMSService.CheckShift(psid);

                if (dem == 0 && userWIP != "Admin")
                {
                    return Json(new { result = false, message = Constant.EndShift }, JsonRequestBehavior.AllowGet);
                }
                ////var check_mtcd = db.w_material_info.Where(x => x.mt_cd == mt_cd).SingleOrDefault(); 
                //string sql_check_mtcd = @"Select * from w_material_info m where m.mt_cd = @1 limit 1";
                //var check_mtcd = db.Database.SqlQuery<w_material_info>(sql_check_mtcd, new MySqlParameter("@1", mt_cd)).FirstOrDefault();
                //var check_mtcd =  _TIMSService.FindWMaterialInfo(mt_cd).FirstOrDefault();
                var check_mtcd = _IWOService.GetWMaterialInfowithmtcd(mt_cd);
                //cmttest2
                if (check_mtcd == null)
                {
                    return Json(new { result = false, message = "MT Code " + Constant.NotFound }, JsonRequestBehavior.AllowGet);
                }
                if (String.IsNullOrEmpty(mt_lot))
                {
                    return Json(new { result = false, message = "Composite " + Constant.NotFound }, JsonRequestBehavior.AllowGet);
                }

                //kiem tra mapping đo chua

                int kqcheckmaping = _TIMSService.CheckMaterialMapping(mt_cd, mt_lot);
                //var check_mapping2 = _TIMSService.FindAllWMaterialMappingByMtLotMtCd(mt_cd, mt_lot).FirstOrDefault();
                var check_mapping2 = _IWOService.Getmaterialmappingreturn(mt_cd, mt_lot);
                if (kqcheckmaping > 0 && userWIP != "Admin")
                {
                    return Json(new { result = false, message = Constant.Other }, JsonRequestBehavior.AllowGet);
                }
                //Duy Close
                /*
                 //var check_mapping1 = db.w_material_mapping.Where(x => x.mt_cd == mt_cd && x.mt_lot != mt_lot).OrderByDescending(x => x.mapping_dt).ToList();
                // var check_mapping2 = db.w_material_mapping.Where(x => x.mt_cd == mt_cd && x.mt_lot == mt_lot).OrderByDescending(x => x.mapping_dt).FirstOrDefault();
                var check_mapping1 =  _TIMSService.FindAllWMaterialMappingByMTCdAndNotMtLot(mt_cd, mt_lot);
                var check_mapping2 = _TIMSService.FindAllWMaterialMappingByMtLotMtCd(mt_cd, mt_lot).FirstOrDefault();
                if (check_mapping2 != null)
                {
                    foreach (var item1 in check_mapping1)
                    {
                        //so sánh thời gian mapping
                        var mapping_hientai = check_mapping2.mapping_dt;
                        if (Convert.ToInt64(item1.mapping_dt) > Convert.ToInt64(mapping_hientai))
                        {
                            return Json(new { result = false, message = Constant.Other }, JsonRequestBehavior.AllowGet);
                        }
                    }
                }*/
                var bien = "";
                // var check_lot = db.w_material_info.Where(x => x.mt_cd == mt_lot).SingleOrDefault();
                //  var check_lot = db.w_material_info.SingleOrDefault(x => x.mt_cd == mt_lot);
                //var check_lot = _TIMSService.FindOneMaterialInfoByMTLot(mt_lot);
                var check_lot = _IWOService.GetWMaterialInfowithmtcd(mt_lot);
                //var check_lot = _TIMSService.FindOneMaterialInfoByMTLot(mt_lot);
                if (check_lot == null && userWIP != "Admin")
                {
                    return Json(new { result = false, message = "Lot không tìm thấy!!!" }, JsonRequestBehavior.AllowGet);
                }
                //kiểm tra lấy PO
                //var check_po = db.w_actual.Where(x => x.id_actual == check_lot.id_actual).SingleOrDefault();
                //var check_po = db.w_actual.SingleOrDefault(x => x.id_actual == check_lot.id_actual);
                //var check_po = _TIMSService.FindOneWActual(check_lot.id_actual);
                var check_po = _IWOService.GetWActual(check_lot.id_actual.Value);
                // var check_pr = db.w_actual_primary.SingleOrDefault(x => x.at_no == check_po.at_no);
                //var check_pr = _TIMSService.FindOneWActualPrimaryByAtNo(check_po.at_no);
                var check_pr = _TIMSService.GetwactualprimaryFratno(check_po.at_no);
                var ca = "";
                var ca_ngay = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd 08:00:00"));
                var ca_dem = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd 20:00:00"));

                //
                //  var check = db.m_facline_qc.Where(x => x.ml_no == mt_cd && x.fq_no.StartsWith("TI")).ToList();
                // var  check_again = check.Where(x => x.ml_tims == mt_lot && x.ml_no == mt_cd).ToList();
                // var check_again = db.m_facline_qc.Where(x => x.ml_no == mt_cd && x.fq_no.StartsWith("TI") && x.ml_tims == mt_lot).ToList();
                //var check_again = _TIMSService.FindAllMFaclineQc(mt_cd, mt_lot);
                var check_again = _TIMSService.Getmfaclineqc(mt_cd, "TI", mt_lot);

                DateTime reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                //String dateString = reg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                //String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                var defec_t = 0;
                var id_actual = check_lot.id_actual;
                var trang_thai = 0;
                if (check_again.Count() > 0)
                {
                    trang_thai = 1;
                    var fq = check_again.SingleOrDefault();
                    // if (db.w_material_mapping.Any(x => x.mt_cd == fq.ml_tims))
                    if (_TIMSService.CheckExistMaterialMappingById(fq.ml_tims) && userWIP != "Admin")
                    {
                        return Json(new { result = false, message = "Lot đã qua công đoạn khác " }, JsonRequestBehavior.AllowGet);
                    }
                    //Duy note:sai logic
                    //if (check_lot.gr_qty == 0)
                    //{
                    //    return Json(new { result = false, message = "Vui lòng kiểm tra sản lượng đầu ra của mã !!! " }, JsonRequestBehavior.AllowGet);
                    //}
                    if (check_lot.mt_sts_cd == "005")
                    {
                        return Json(new { result = false, message = "Mã đầu ra  đã được kết thúc !!! " }, JsonRequestBehavior.AllowGet);
                    }
                    var so_luongng = fq.check_qty - fq.ok_qty;
                    defec_t = Convert.ToInt32(so_luongng);

                    if (ca_ngay <= fq.reg_dt && fq.reg_dt <= ca_dem)
                    {
                        ca = "CN";
                    }
                    else
                    {
                        ca = "CD";
                    }
                    bien = check_po.at_no + "-" + check_pr.product + "-" + ca + "-TIMS-NG";

                    //tru Mã Ng đã tạo ra trc đó
                    //   var ma_NG = db.w_material_info.Where(x => x.mt_cd == bien).SingleOrDefault();
                    //var ma_NG = _TIMSService.FindOneMaterialInfoByMTLot(bien);
                    var ma_NG = _IWOService.GetWMaterialInfowithmtcd(bien);
                    if (ma_NG != null)
                    {
                        ma_NG.gr_qty = (ma_NG.gr_qty - defec_t) + (check_qty - ok_qty);
                        ma_NG.real_qty = (ma_NG.real_qty - defec_t) + (check_qty - ok_qty);
                        _IWOService.UpdateMaterialInfo(ma_NG);
                        //_TIMSService.UpdateMaterialInfoQty(defec_t, check_qty, ok_qty, bien);

                        //wmtid
                        //_TIMSService.UpdateMeterialInfoQty(ma_NG.gr_qty, ma_NG.real_qty);
                        // db.Entry(ma_NG).State = EntityState.Modified;
                        //  db.SaveChanges();
                        // Log.Info("TIMS=>Insert_w_product_qc mt_cd " + ma_NG.mt_cd + " Sodong 3890 " + ma_NG.gr_qty + " " + ma_NG.real_qty);
                    }
                    //if (bien != null && bien != "")
                    //if (!string.IsNullOrEmpty(bien))
                    //{
                    //    string sqlUpdateQty = $@" Update w_material_info set
                    //                               gr_qty = (gr_qty - {defec_t}) + ({check_qty} - {ok_qty}),
                    //                               real_qty = (real_qty - {defec_t}) + ({check_qty} - {ok_qty})
                    //                               where mt_cd = @1 ";
                    //    _TIMSService.UpdateMaterialInfoQty(defec_t, check_qty, ok_qty, bien);
                    //    db.Database.ExecuteSqlCommand(sqlUpdateQty, new MySqlParameter("@1", bien));
                    //}


                    //update bảng chính m_facline_qc
                    fq.check_qty = check_qty;
                    fq.ok_qty = ok_qty;
                    //string sql_update_faclineQty = @"Update m_facline_qc set check_qty= @1 and ok_qty = @2 where fqno = @3";
                    //db.Database.ExecuteSqlCommand(sql_update_faclineQty,
                    //       new MySqlParameter("@1", fq.check_qty),
                    //       new MySqlParameter("@2", fq.ok_qty),
                    //       new MySqlParameter("@3", fq.fqno));
                    _TIMSService.UpdateFaclineQty(fq.check_qty.Value, fq.ok_qty.Value, fq.fqno);

                    //update số lượng đầu ra
                    //check_lot.gr_qty = (check_lot.gr_qty - fq.ok_qty) + ok_qty;
                    //check_lot.real_qty = (check_lot.real_qty - fq.ok_qty) + ok_qty;
                    //_TIMSService.UpdateMaterialInfoQty(check_lot.gr_qty, check_lot.real_qty, check_lot.wmtid);
                    _TIMSService.UpdateContainerOutput(mt_lot, _TIMSService.GetSumQtyFacline(mt_lot));

                    // db.Entry(check_lot).State = EntityState.Modified;
                    // Log.Info("TIMS=>Insert_w_product_qc mt_cd " + check_lot.mt_cd + " Sodong 3900 " + check_lot.gr_qty + " " + check_lot.real_qty);

                    //thay doi san luong dau vao neu nhu co remain
                    //neu co remain
                    var reamain_int = remain;
                    // var bobin_xa = db.d_bobbin_lct_hist.Where(x => x.bb_no == check_mtcd.bb_no).FirstOrDefault();
                    // var find_bb = db.d_bobbin_info.Where(x => x.bb_no == check_mtcd.bb_no).FirstOrDefault();
                    var bobin_xa = _IWOService.GetdbobbinlcthistFrbbno(check_mtcd.bb_no);
                    var find_bb = _IWOService.GetBobbinInfo(check_mtcd.bb_no);

                    // db.Entry(fq).State = EntityState.Modified;
                    if (reamain_int > 0)
                    {
                        //update lại sản lượng của đầu vào nếu có remain
                        //NẾU MÃ ĐẦU VÀO LÀ STA OR ROT THÌ STATUS = 008
                        if (check_mtcd.mt_cd.Contains("STA") || check_mtcd.mt_cd.Contains("ROT"))
                        {
                            check_mtcd.mt_sts_cd = "008";
                        }
                        else
                        {
                            check_mtcd.mt_sts_cd = "002";
                        }
                        check_mtcd.gr_qty = reamain_int;
                        
                        
                        _IWOService.UpdateMaterialInfo(check_mtcd);
                        // _TIMSService.update
                        //db.Entry(check_lot).State = EntityState.Modified;
                        //kiem tra do dung da được sử dụng chưa
                        if (bobin_xa == null)
                        {
                            //insert lại nha 
                            var d_bobbin_lct_hist = new d_bobbin_lct_hist();
                            d_bobbin_lct_hist.bb_no = check_mtcd.bb_no;
                            d_bobbin_lct_hist.mt_cd = check_mtcd.mt_cd;
                            d_bobbin_lct_hist.reg_dt = DateTime.Now;
                            d_bobbin_lct_hist.chg_dt = DateTime.Now;
                            d_bobbin_lct_hist.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                            d_bobbin_lct_hist.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                            d_bobbin_lct_hist.use_yn = "Y";
                            d_bobbin_lct_hist.del_yn = "N";
                            _IWOService.InsertToBobbinLctHistory(d_bobbin_lct_hist);
                            // _TIMSService.InsertBobbinHist(d_bobbin_lct_hist);
                            // db.Entry(d_bobbin_lct_hist).State = EntityState.Added;
                            // Log.Info("TIMS=>Insert_w_product_qc add bobin mt_cd " + d_bobbin_lct_hist.mt_cd + " Sodong 3947 bobin" + d_bobbin_lct_hist.bb_no);
                            //update info
                            find_bb.mt_cd = check_mtcd.mt_cd;
                            find_bb.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                            _IWOService.UpdateBobbinMTCode(find_bb);
                            // _TIMSService.UpdateMtCdBobbinInfo(find_bb.mt_cd, find_bb.bno);
                            // _TIMSService.UpdateBobbinInfo(Session["userid"] == null ? null : Session["userid"].ToString(), find_bb.mt_cd, find_bb.bno);//xem ;lai

                            //   db.Entry(find_bb).State = EntityState.Modified;

                        }
                        //maping đổi về trạng thái 
                        check_mapping2.use_yn = "Y";
                        _TIMSService.UpdateUseYnMaterialMapping(check_mapping2.use_yn, check_mapping2.wmmid);
                        // db.Entry(check_mapping2).State = EntityState.Modified;
                    }
                    else
                    {
                        //xa do dung 
                        //xóa bobin info
                        //update lại sản lượng của đầu vào nếu có remain=0
                        check_mtcd.gr_qty = reamain_int;
                        check_mtcd.mt_sts_cd = "005";
                        _IWOService.UpdateMaterialInfo(check_mtcd);
                        //_TIMSService.UpdateWMaterialInfoInput(check_mtcd.gr_qty, check_mtcd.mt_sts_cd, check_mtcd.wmtid);
                        //update 
                        //db.Entry(check_lot).State = EntityState.Modified;

                        //string sql_update_faclineQty1 = @"Update m_facline_qc set check_qty= @1 and ok_qty = @2 where fqno = @3";
                        //db.Database.ExecuteSqlCommand(sql_update_faclineQty1,
                        //       new MySqlParameter("@1", fq.check_qty),
                        //       new MySqlParameter("@2", fq.ok_qty),
                        //       new MySqlParameter("@3", fq.fqno));

                        //if (bobin_xa.mt_cd == check_mtcd.mt_cd)
                        //{
                        //    db.Entry(bobin_xa).State = EntityState.Deleted;
                        //    find_bb.mt_cd = null;

                        //    db.Entry(find_bb).State = EntityState.Modified;
                        //}
                        check_mapping2.use_yn = "N";
                        int check = _TIMSService.UpdateUseYnMaterialMapping(check_mapping2.use_yn, check_mapping2.wmmid);
                        // db.Entry(check_mapping2).State = EntityState.Modified;
                    }

                    //update_defect.defect = (update_defect.defect - defec_t) + (check_qty - ok_qty);
                    check_po.defect = _TIMSService.GetDefactActual((int)check_po.id_actual);
                    _TIMSService.UpdateDefectActual(check_po.defect, check_po.id_actual);
                    _TIMSService.UpdateActualStaff(id_actual, psid);
                    return Json(new { result = true, remain = reamain_int, MFQC }, JsonRequestBehavior.AllowGet);
                    ////update defect của công đoạn
                    //check_po.defect = (check_po.defect - so_luongng) + (check_qty - ok_qty);
                    //string sql_update_defect = @"Update w_actual set defect= @1 where id_actual = @2";
                    //db.Database.ExecuteSqlCommand(sql_update_defect,
                    //       new MySqlParameter("@1", check_po.defect),
                    //       new MySqlParameter("@2", check_po.id_actual));
                    //int result =_TIMSService.UpdateWActualById(check_po.id_actual, check_po.defect);
                    //    db.Entry(check_po).State = EntityState.Modified;
                    //if (result != 0)
                    //{
                    //    _TIMSService.UpdateActualStaff(id_actual, psid);
                    //    return Json(new { result = true, MFQC }, JsonRequestBehavior.AllowGet);
                    //}
                    //int save = db.SaveChanges();
                    //if (save > 0)
                    //{
                    //update actual cho công nhân
                    //StringBuilder update_actual_wk = new StringBuilder();
                    //update_actual_wk.Append(" CALL `Update_SL_TIMS`('" + id_actual + "', '" + psid + "');CALL `Update_SL_w_actual`(" + id_actual + "); \n");
                    //db.Database.ExecuteSqlCommand(update_actual_wk.ToString());
                    //_TIMSService.UpdateActualStaff(id_actual, psid);
                    //return Json(new { result = true, MFQC }, JsonRequestBehavior.AllowGet);
                    //}

                    //string sql_update_defect = @"Update w_actual set defect= @1 where id_actual = @2";
                    //db.Database.ExecuteSqlCommand(sql_update_defect,
                    //       new MySqlParameter("@1", defect),
                    //       new MySqlParameter("@2", check_po.id_actual));

                    //    db.Entry(check_po).State = EntityState.Modified;
                    //int save = db.SaveChanges();
                    //if (save > 0)
                    //{
                    //update actual cho công nhân
                    //StringBuilder update_actual_wk = new StringBuilder();
                    //update_actual_wk.Append(" CALL `Update_SL_TIMS`('" + id_actual + "', '" + psid + "');CALL `Update_SL_w_actual`(" + id_actual + "); \n");
                    ////int tinhtoan = new Excute_query().Execute_NonQuery(update_actual_wk);
                    //db.Database.ExecuteSqlCommand(update_actual_wk.ToString());
                    //return Json(new { result = true, MFQC }, JsonRequestBehavior.AllowGet);
                    //}

                }
                else
                {
                    //tạo mã PO ca ngày hoặc ca đêm bên mms
                    if (ca_ngay <= DateTime.Now && DateTime.Now <= ca_dem)
                    {
                        ca = "CN";
                    }
                    else
                    {
                        ca = "CD";
                    }
                    bien = check_po.at_no + "-" + check_pr.product + "-" + ca + "-TIMS-NG";
                    //var list = db.m_facline_qc.Where(x => x.fq_no.StartsWith("TI")).OrderBy(x => x.fq_no).ToList();
                    //string list = _TIMSService.GetmfaclineqcSearch("TI000000001");

                    //if (string.IsNullOrEmpty(list))
                    //{
                    //    MFQC.fq_no = "TI000000001";
                    //}
                    //else
                    //{
                    //    //var menuCd = string.Empty;
                    //    //var subMenuIdConvert = 0;
                    //    //var list1 = list;

                    //    //var bien1 = list.fq_no;
                    //    //var subMenuId = bien1.Substring(bien1.Length - 9, 9);
                    //    //int.TryParse(subMenuId, out subMenuIdConvert);
                    //    //menuCd = "TI" + string.Format("{0}{1}", menuCd, CreateFQ((subMenuIdConvert + 1)));
                    //   
                    //}
                    MFQC.fq_no = "TI000000002";
                    MFQC.ml_tims = mt_lot;
                    MFQC.ml_no = mt_cd;
                    MFQC.work_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                    MFQC.reg_dt = reg_dt;
                    MFQC.chg_dt = chg_dt;
                    MFQC.product_cd = check_pr.product;
                    MFQC.shift = ca;
                    MFQC.at_no = check_po.at_no;
                    //var item = db.qc_item_mt.FirstOrDefault(x => x.item_vcd == item_vcd);
                    var item = _TIMSService.Getqcitemmt(item_vcd);
                    MFQC.item_nm = item.item_nm;
                    MFQC.item_exp = item.item_exp;
                    if (ModelState.IsValid)
                    {
                        MFQC.reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                        MFQC.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                        int result = _TIMSService.InsertMFaclineQC(MFQC);
                        //db.Entry(MFQC).State = EntityState.Added;
                        //list.Add(MFQC);

                        //db.SaveChanges();
                        //neu la ma chua check mms
                        if (bien != "")
                        {
                            //StringBuilder varname1 = new StringBuilder();
                            //if (!db.w_material_info.Any(x => x.mt_cd.StartsWith(bien)))
                            if (_IWOService.GetTotalwMaterialInfoDV(bien) <= 0)
                            {
                                // nếu chưa tồn tại thì insert mã vào DB
                                //varname1.Append("INSERT INTO w_material_info \n");
                                //varname1.Append("            (at_no,product,id_actual,mt_type, \n");
                                //varname1.Append("             `mt_cd`,alert_NG, \n");
                                //varname1.Append("             `mt_no`, \n");
                                //varname1.Append("             `gr_qty`, \n");
                                //varname1.Append("             `date`, \n");
                                //varname1.Append("             `expiry_dt`, \n");
                                //varname1.Append("             `dt_of_receipt`, \n");
                                //varname1.Append("             `expore_dt`, \n");
                                //varname1.Append("             `lot_no`, \n");
                                //varname1.Append("             `mt_barcode`, \n");
                                //varname1.Append("             `mt_qrcode`, \n");
                                //varname1.Append("             `mt_sts_cd`, \n");
                                //varname1.Append("             `use_yn`, \n");
                                //varname1.Append("             `reg_id`, \n");
                                //varname1.Append("             `reg_dt`, \n");
                                //varname1.Append("             `chg_id`, \n");
                                //varname1.Append("             `chg_dt`, \n");
                                //varname1.Append("             orgin_mt_cd,lct_cd,real_qty) \n");
                                //varname1.Append("SELECT at_no,product,0,mt_type, \n");
                                //varname1.Append("      '" + bien + "',1, \n");
                                //varname1.Append("       `mt_no`, \n");
                                //varname1.Append("       " + (check_qty - ok_qty) + ", \n");
                                //varname1.Append("       `date`, \n");
                                //varname1.Append("       `expiry_dt`, \n");
                                //varname1.Append("       `dt_of_receipt`, \n");
                                //varname1.Append("       `expore_dt`, \n");
                                //varname1.Append("       `lot_no`, \n");
                                //varname1.Append("       '" + bien + "', \n");
                                //varname1.Append("        '" + bien + "', \n");
                                //varname1.Append("       '003', \n");
                                //varname1.Append("       `use_yn`, \n");
                                //varname1.Append("       '" + MFQC.reg_id + "', \n");
                                //varname1.Append("       Now(), \n");
                                //varname1.Append("       '" + MFQC.reg_id + "', \n");
                                //varname1.Append("       Now(), \n");
                                //varname1.Append("       mt_cd,lct_cd," + (check_qty - ok_qty) + " \n");
                                //varname1.Append("FROM   w_material_info \n");
                                //varname1.Append("WHERE  mt_cd = '" + mt_lot + "'; \n");
                                _TIMSService.InsertMaterialInfo(bien, check_qty, ok_qty, MFQC.reg_id, mt_lot);
                                //Log.Info("TIMS=>Insert_w_product_qc (Add NG) mt_cd " + bien + " SL: " + (check_qty - ok_qty));
                            }
                            else
                            {
                                //nếu tồn tại rồi thì update số lượng của NG PO
                                //varname1.Append("UPDATE w_material_info \n");
                                //varname1.Append("SET gr_qty=(gr_qty+ " + (check_qty - ok_qty) + "),real_qty=(real_qty+ " + (check_qty - ok_qty) + ") \n");
                                //varname1.Append("WHERE mt_cd='" + bien + "';");
                                //db.Database.ExecuteSqlCommand(varname1.ToString());
                                _TIMSService.UpdateNGPO(check_qty, ok_qty, bien);
                                // _TIMSService.UpdateMaterialInfoQty(check_qty - ok_qty, bien);
                                // Log.Info("TIMS=>Insert_w_product_qc (update SL NG) mt_cd " + bien + " SL: " + (check_qty - ok_qty));
                            }

                            //công vào thùng đầu ra
                            //varname1.Append("UPDATE w_material_info   \n");
                            //varname1.Append("SET gr_qty=(gr_qty+ " + ok_qty + "),real_qty=(real_qty+  " + ok_qty + ") \n");
                            //varname1.Append("WHERE mt_cd='" + mt_lot + "';");

                            _TIMSService.UpdateContainerOutput(mt_lot, _TIMSService.GetSumQtyFacline(mt_lot));

                            //phai la mt_lot
                            // Log.Info("TIMS=>Insert_w_product_qc (update SL dau ra) mt_lot " + check_lot.gr_qty + " SL: " + (check_lot.gr_qty - ok_qty));
                            //thay đổi đầu vào=remain
                            //var html = "";
                            //var finish = "";
                            if (remain > 0)
                            {
                                //html = ",mt_sts_cd='002'";
                                // html = "002";
                                //gọi lại hàm nếu là dạng EA thì sẽ k update history lại nếu là dạng roll thì vẫn còn số lượng và bobin đựng của lot nha
                                //var bobin_history = db.d_bobbin_lct_hist.SingleOrDefault(x => x.bb_no == check_mtcd.bb_no);
                                //var bobin_history=_TIMSService.FindOneBobbin_lct_hist(check_mtcd.bb_no);
                                var bobin_history = _IWOService.GetdbobbinlcthistFrbbno(check_mtcd.bb_no);
                                //if (trang_thai == 1 && check_po.don_vi_pr != "200" && (!db.d_bobbin_lct_hist.Any(x => x.bb_no == check_mtcd.bb_no)))
                                if (trang_thai == 1 && check_po.don_vi_pr != "200" && bobin_history != null)
                                {
                                    //insert history lại 
                                    //update bobin
                                    //  var bobin_history = db.d_bobbin_lct_hist.Where(x => x.bb_no == check_mtcd.bb_no).SingleOrDefault();
                                    // var bobin_primary = db.d_bobbin_info.Where(x => x.bb_no == check_mtcd.bb_no).SingleOrDefault();
                                    //DBobbinInfo bobin_primary = _TIMSService.FindOneDBobbinInfo(check_mtcd.bb_no);
                                    d_bobbin_info bobin_primary = _IWOService.GetBobbinInfo(check_mtcd.bb_no);
                                    if (bobin_primary == null)
                                    {
                                        bobin_primary.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                                        bobin_primary.mt_cd = check_mtcd.mt_cd;
                                        _IWOService.UpdateBobbinMTCode(bobin_primary);
                                        //_TIMSService.UpdateBobbinInfo(bobin_primary.chg_id, bobin_primary.mt_cd, bobin_primary.bno);
                                        //  db.Entry(bobin_primary).State = EntityState.Modified;
                                        //  db.SaveChanges(); // line that threw exception
                                        //    Log.Info("TIMS=>Insert_w_product_qc (thay doi d_bobin_info) mt_cd " + check_mtcd.mt_cd + " bobin: " + check_mtcd.bb_no);
                                    }
                                    //string userId = Session["userid"] == null ? null : Session["userid"].ToString();
                                    //string sql_update_bobin_primary = @"Update d_bobbin_info Set chg_id = @1 ,mt_cd =@2
                                    //                                        Where bno = @3 ";
                                    //db.Database.ExecuteSqlCommand(sql_update_bobin_primary, new MySqlParameter("@1", userId),
                                    //                                                       new MySqlParameter("@2", check_mtcd.mt_cd),
                                    //                                                       new MySqlParameter("@3", check_mtcd.bb_no)
                                    //                                                       );
                                    //_TIMSService.UpdateBobbinInfo(userId,check_mtcd.mt_cd, check_mtcd.b);
                                    if (bobin_history == null)
                                    {
                                        //insert data đã xóa trước đó 
                                        //var d_bobbin_lct_hist = new d_bobbin_lct_hist();
                                        var d_bobbin_lct_hist = new DBobbinLctHist();
                                        d_bobbin_lct_hist.bb_no = check_mtcd.bb_no;
                                        d_bobbin_lct_hist.mt_cd = check_mtcd.mt_cd;
                                        d_bobbin_lct_hist.reg_dt = DateTime.Now;
                                        d_bobbin_lct_hist.chg_dt = DateTime.Now;
                                        d_bobbin_lct_hist.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                                        d_bobbin_lct_hist.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                                        d_bobbin_lct_hist.use_yn = "Y";
                                        d_bobbin_lct_hist.del_yn = "N";
                                        _TIMSService.InsertBobbinHist(d_bobbin_lct_hist);
                                        // db.Entry(d_bobbin_lct_hist).State = EntityState.Added;
                                        // db.SaveChanges(); // line that threw exception
                                        // Log.Info("TIMS=>Insert_w_product_qc (add d_bobbin_lct_hist) mt_cd " + check_mtcd.mt_cd + " bobin: " + check_mtcd.bb_no);
                                    }
                                }
                                // html = "002";
                                //finish = "UPDATE w_material_mapping SET use_yn='Y' where mt_lot='" + mt_lot + "' and mt_cd='" + mt_cd + "';";
                                _TIMSService.UpdateMaterialmappinguseyn(mt_lot, mt_cd, "Y");
                                //nếu đầu vào là mã STA hoặc ROT thì status = 008
                                if (mt_cd.Contains("STA") || mt_cd.Contains("ROT"))
                                {
                                    _TIMSService.UpdateGrqtyMaterialInfo(mt_cd, (int)remain, "008");
                                }
                                else
                                {
                                    _TIMSService.UpdateGrqtyMaterialInfo(mt_cd, (int)remain, "002");
                                }
                                
                            }
                            else if (remain == 0)
                            {
                                _TIMSService.UpdateGrqtyMaterialInfo(mt_cd, (int)remain, "005");
                                _TIMSService.UpdateMaterialMappingUseyn(mt_cd, "N");
                            }

                            //_TIMSService.UpdateGrqtyMaterialInfo(mt_cd, (int)remain,"002");

                            int checkgrqty = _TIMSService.CheckGrqtyMaterialInfo(mt_cd, "005");
                            if (checkgrqty > 0)
                            {
                                _TIMSService.DeleteDBobbinLctHistforDevice(mt_cd);
                                _TIMSService.UpdateBobbinInfowithmtcd(mt_cd);
                            }
                            //varname1.Append("UPDATE w_material_info SET gr_qty='" + remain + "'" + html + " \n");
                            //varname1.Append("WHERE mt_cd='" + mt_cd + "';CALL `Finish_Tims`('" + mt_cd + "');CALL `xa_container`('" + mt_cd + "');" + finish + " \n");


                            // int effect_rows1 = _TIMSService.UpdateFinishTIMS(Int32.Parse(remain), html, mt_cd, mt_lot);

                            //    Log.Info("TIMS=>Insert_w_product_qc (xa do dung) mt_cd " + mt_cd + " ket thuc ma mt_cd " + mt_cd + " Thay doi SL=" + remain);
                            //cho nay qua cham ... 
                            //int effect_rows1 = new Excute_query().Execute_NonQuery(varname1);
                            //string sqlUpdate = varname1.ToString();
                            //int effect_rows1 = db.Database.ExecuteSqlCommand(sqlUpdate);

                            //update defect của công đoạn
                            //var update_defect = db.w_actual.Find(id_actual);
                            //var update_defect = _TIMSService.FindOneWActual(id_actual);
                            var update_defect = _IWOService.GetWActual((int)id_actual.Value);
                            //update_defect.defect = (update_defect.defect - defec_t) + (check_qty - ok_qty);
                            update_defect.defect = _TIMSService.GetDefactActual((int)id_actual);
                            _TIMSService.UpdateDefectActual(update_defect.defect, update_defect.id_actual);
                            // db.Entry(update_defect).State = EntityState.Modified;
                            // db.SaveChanges();
                            // var update_defect = _TIMSService.FindOneWActual(id_actual);
                            // var defectNew =(update_defect.defect - defec_t) + (check_qty - ok_qty);
                            //  _TIMSService.UpdateWActualById(id_actual, defectNew);

                            //update actual cho công nhân
                            //StringBuilder update_actual_wk = new StringBuilder();
                            //update_actual_wk.Append(" CALL `Update_SL_TIMS`('" + id_actual + "', '" + psid + "');CALL `Update_SL_w_actual`(" + id_actual + "); \n");
                            _TIMSService.UpdateActualStaff(id_actual, psid);
                            //int tinhtoan = new Excute_query().Execute_NonQuery(update_actual_wk);
                            //db.Database.ExecuteSqlCommand(update_actual_wk.ToString());
                            return Json(new { result = true, remain = remain, MFQC }, JsonRequestBehavior.AllowGet);
                        }
                        return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
                    }
                }
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {

                return Json(new { result = false, message = "Lôĩ hệ thống!!!" }, JsonRequestBehavior.AllowGet);
            }
            #endregion insert info
        }

        public ActionResult update_Ng_qty(string fqno, string defect_qty)
        {
            try
            {
                //update check qty bảng m_facline_qc/ tông defect+ok = check_qty
                if (string.IsNullOrEmpty(fqno))
                {
                    return Json(new { result = false, message = "Id rỗng" }, JsonRequestBehavior.AllowGet);
                }
                if (string.IsNullOrEmpty(defect_qty))
                {
                    return Json(new { result = false, message = "Số Ng rỗng, vui lòng nhập số Ng" }, JsonRequestBehavior.AllowGet);
                }
                var id = int.Parse(fqno);
                var ktExist = db.m_facline_qc.Find(id);
                if (ktExist == null)
                {
                    return Json(new { result = false, message = "Id không tồn tại" }, JsonRequestBehavior.AllowGet);
                }
                var ng_cu = (ktExist.check_qty - ktExist.ok_qty);
                var updateCheckqty = ktExist.ok_qty + int.Parse(defect_qty);
                ktExist.check_qty = updateCheckqty;
                db.Entry(ktExist).State = EntityState.Modified;

                //update bảng w_material_info
                //tim mã PO000000022-LJ63-19611A-CN-TIMS-NG update gr_qty
                //kiểm tra lấy PO
                var ExistId_actual = db.w_material_info.Where(x => x.mt_cd == ktExist.ml_tims).SingleOrDefault();

                var Exist_po = db.w_actual.Where(x => x.id_actual == ExistId_actual.id_actual).SingleOrDefault();
                var check_pr = db.w_actual_primary.Where(x => x.at_no == Exist_po.at_no).SingleOrDefault();
                var ca = "";
                var ca_ngay = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd 08:00:00"));
                var ca_dem = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd 20:00:00"));

                //tạo mã PO ca ngày hoặc ca đêm bên mms
                if (ca_ngay.TimeOfDay <= ExistId_actual.reg_dt.TimeOfDay && ExistId_actual.reg_dt.TimeOfDay <= ca_dem.TimeOfDay)
                {
                    ca = "CN";
                }
                else
                {
                    ca = "CD";
                }
                var bien = Exist_po.at_no + "-" + check_pr.product + "-" + ca + "-TIMS-NG";

                var Exist_MaNg = db.w_material_info.Where(x => x.mt_cd.Contains(bien)).SingleOrDefault();
                if (Exist_MaNg != null)
                {
                    if (Exist_MaNg.gr_qty == Exist_MaNg.real_qty)
                    {
                        Exist_MaNg.real_qty = (Exist_MaNg.real_qty - ng_cu) + int.Parse(defect_qty);

                    }
                    Exist_MaNg.gr_qty = (Exist_MaNg.gr_qty - ng_cu) + int.Parse(defect_qty);

                    db.Entry(Exist_MaNg).State = EntityState.Modified;
                }
                db.SaveChanges();

                update_defect(ktExist.ml_tims);



                var sql = new StringBuilder();

                sql.Append(" SELECT b.fqno,b.fq_no,CONCAT(substr(b.work_dt,1,4),'-',substr(b.work_dt,5,2),'-',substr(b.work_dt,7,2),' ',substr(b.work_dt,9,2),':',substr(b.work_dt,11,2) )work_dt,b.check_qty,(b.ok_qty),(b.check_qty)-(b.ok_qty) as defect_qty ")
                .Append(" from m_facline_qc as b   ")
                .Append(" where b.fqno='" + fqno + "'  ");

                var data = new InitMethods().ConvertDataTableToJsonAndReturn(sql);




                return Json(new { result = true, message = "Sửa thành công", data = data.Data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Lỗi hệ thống" }, JsonRequestBehavior.AllowGet);

            }
        }

        public string update_defect(string mt_cd)
        {
            try
            {
                var danhsach = db.w_material_info.Where(x => x.mt_cd == mt_cd).SingleOrDefault();
                if (danhsach != null)
                {
                    var id_actual = danhsach.id_actual;
                    var staff_id = danhsach.staff_id;
                    var thoigian = danhsach.reg_dt.ToString("yyyy-MM-dd HH:mm:ss");
                    var sql = new StringBuilder();
                    sql.Append("CALL `UPdate_Defect_Tims`('" + id_actual + "', '" + staff_id + "', '" + thoigian + "') ;");
                    var data = new InitMethods().ConvertDataTableToJsonAndReturn(sql);
                    return "ok";
                }

                return "";

            }
            catch (Exception e)
            {
                return "";
            }


        }


        public ActionResult ModifyFaclineQcDetail(string check_qty, string id, int fqno)
        {
            var fq_no = "";
            if ((check_qty != null) && (id != null) && (fqno > 0))
            {
                var list_m_facline_qc = db.m_facline_qc.Find(fqno);
                fq_no = list_m_facline_qc.fq_no;
                var list = new ArrayList();
                var a2 = id.TrimStart('[').TrimEnd(']').Split(',');
                var m1 = check_qty.TrimStart('[').TrimEnd(']').Split(',');

                int sumcheck_qty = 0;
                foreach (var item in m1)
                {
                    sumcheck_qty += Convert.ToInt32(item);
                }
                var defect_qty = list_m_facline_qc.check_qty - list_m_facline_qc.ok_qty;
                if (sumcheck_qty > defect_qty)
                {
                    return Json(new { result = false, message = Constant.OverloadError }, JsonRequestBehavior.AllowGet);
                }

                for (int i = 0; i < a2.Length; i++)
                {
                    var id2 = int.Parse(a2[i]);
                    var find = db.m_facline_qc_value.Find(id2);

                    if (Convert.ToInt32(m1[i]) <= defect_qty)
                    {
                        find.check_qty = Convert.ToInt32(m1[i]);
                        find.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        db.Entry(find).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                    var listdata = (from p in db.m_facline_qc_value
                                    join b in db.qc_itemcheck_mt
                                    on p.check_id equals b.check_id
                                    where p.fqhno == find.fqhno
                                    select new
                                    {
                                        fqhno = p.fqhno,
                                        check_subject = b.check_subject,
                                        check_value = p.check_value,
                                        check_qty = p.check_qty,
                                    }).FirstOrDefault();
                    list.Add(listdata);
                }

                var soluongdefect = (db.m_facline_qc_value.Where(x => x.fq_no == fq_no).ToList().Sum(x => x.check_qty));
                
                list_m_facline_qc.ok_qty = list_m_facline_qc.check_qty - soluongdefect;
                list_m_facline_qc.chg_dt = DateTime.Now;
                list_m_facline_qc.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                db.Entry(list_m_facline_qc).State = EntityState.Modified;
                db.SaveChanges();
                //update w_material_info

                var w_material_info = db.w_material_info.Where(x => x.mt_cd == list_m_facline_qc.ml_no).SingleOrDefault();
                if (w_material_info != null)
                {
                    var check_ng = db.m_facline_qc.Where(x => x.ml_no == list_m_facline_qc.ml_no).ToList();
                    w_material_info.gr_qty = w_material_info.gr_qty - (check_ng.Sum(x => x.check_qty) - check_ng.Sum(x => x.ok_qty));
                    w_material_info.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    db.Entry(w_material_info).State = EntityState.Modified;
                    db.SaveChanges();
                    var w_material_info2 = db.w_material_info.Where(x => x.mt_cd.StartsWith(list_m_facline_qc.ml_no + "-NG") && x.lct_cd.StartsWith("006")).SingleOrDefault();
                    if (w_material_info2 != null)
                    {
                        w_material_info2.gr_qty = (check_ng.Sum(x => x.check_qty) - check_ng.Sum(x => x.ok_qty));
                        w_material_info2.alert_NG = 1;
                        w_material_info2.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                        db.Entry(w_material_info2).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                    //update w_actual
                    var w_actual = db.w_actual.Find(w_material_info.id_actual);
                    w_actual.defect = db.w_material_info.Where(x => x.id_actual == w_material_info.id_actual && x.mt_sts_cd == "003").ToList().Sum(x => x.gr_qty);
                    w_actual.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    db.Entry(w_actual).State = EntityState.Modified;
                    db.SaveChanges();
                }

                var listp_mpo_qc = (from p in db.m_facline_qc

                                    where p.fqno == fqno
                                    select new
                                    {
                                        fqno = p.fqno,
                                        fq_no = p.fq_no,
                                        work_dt = p.work_dt.Substring(0, 4) + "-" + p.work_dt.Substring(4, 2) + "-" + p.work_dt.Substring(6, 2) + " " + p.work_dt.Substring(8, 2) + ":" + p.work_dt.Substring(10, 2),
                                        check_qty = p.check_qty,
                                        ok_qty = p.ok_qty,
                                        defect_qty = p.check_qty - p.ok_qty,
                                    }).FirstOrDefault();
                list.Add(listp_mpo_qc);
                return Json(new { result = true, list = list }, JsonRequestBehavior.AllowGet);
            }
            return View();
        }

        #endregion ROll Nomal

        #region Devide

        public ActionResult searchbobbinPopupDV(string bb_no, string bb_nm, string mt_cd)
        {
            try
            {
                StringBuilder varname1 = new StringBuilder();
                varname1.Append("SELECT a.bno,a.mc_type,a.bb_no,a.mt_cd,a.bb_nm,a.use_yn,a.purpose,a.barcode, \n");
                varname1.Append("a.re_mark,a.count_number,a.del_yn,a.reg_id,a.reg_dt,a.chg_id,a.chg_dt FROM d_bobbin_info AS a \n");
                varname1.Append("WHERE (a.mt_cd=''  OR a.mt_cd IS NULL)  ");
                varname1.Append("and ('" + bb_no + "'='' or a.bb_no LIKE '%" + bb_no + "%')");
                varname1.Append("and ('" + bb_nm + "'='' or a.bb_nm LIKE '%" + bb_nm + "%')");

                var data = db.d_bobbin_info.SqlQuery(varname1.ToString()).ToList();
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Json(e, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult Decevice_sta(string bb_no, int number_dv, int psid)
        {
            try
            {

            
                //check bb_no in history
                
                var check_bb = _TIMSService.FindOneBobbin_lct_hist(bb_no);
                if (check_bb == null)
                {
                    return Json(new { result = false, message = "Container " + Constant.NotFound }, JsonRequestBehavior.AllowGet);
                }

              
                var mt_cd = check_bb.mt_cd;
                if (number_dv == 0)
                {
                    return Json(new { result = false, message = Constant.QuantityInvalid }, JsonRequestBehavior.AllowGet);
                }
               
                var find_mtcd = _TIMSService.CheckWmaterialForDiv(mt_cd);
                if (find_mtcd == null)
                {
                    return Json(new { result = false, message = Constant.FindDK }, JsonRequestBehavior.AllowGet);
                }
                //kiểm tra đã qua công đoạn khác chưa

               
                if (_IWOService.CheckwMaterialMapping(mt_cd) > 0)
                {
                    return Json(new { result = false, message = Constant.Other }, JsonRequestBehavior.AllowGet);
                }
                //kiểm tra coi n đã hết thời gian làm việc chưa
                if (_TIMSService.CheckStaffShiftForId(find_mtcd.id_actual, psid) == 0)
                {
                    return Json(new { result = false, message = Constant.EndShift }, JsonRequestBehavior.AllowGet);
                }

                double sss = Convert.ToDouble(find_mtcd.gr_qty ?? 0) / Convert.ToDouble(number_dv);
                var so_luong = Math.Ceiling(sss);
                var so_cl = so_luong;
                int gr_cl = (int)find_mtcd.gr_qty;

                //update sô lượng về 0 và trang thái về 005
                find_mtcd.mt_sts_cd = "005";
                find_mtcd.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                find_mtcd.gr_qty = 0;
                var orgin_mt_cd = find_mtcd.mt_cd;
                _IWOService.UpdateMaterialInfo(find_mtcd);
                _TIMSService.UpdateBobbinInfowithmtcd(find_mtcd.mt_cd);
                int count = _IWOService.GetTotalwMaterialInfoDV(find_mtcd.mt_cd + "-DV");
                _TIMSService.DeleteBobbinHistoryDevideSta(find_mtcd.mt_cd);
               
                for (int i = 0; i < so_luong; i++)
                {
                    //insert vao so lượng đã chia
                    var sl_chia = number_dv;
                    if (Convert.ToInt32(gr_cl) < Convert.ToInt32(number_dv))
                    {
                        sl_chia = gr_cl;
                    }
                    string mtcddv = find_mtcd.mt_cd + "-DV" + (count + 1);
                    _TIMSService.InsertwmaterialinfoDevideSta(sl_chia, mtcddv, find_mtcd.mt_cd);
                    so_cl = so_cl - 1;
                    gr_cl = gr_cl - sl_chia;
                    count++;
                   
                }
                _TIMSService.UpdateMappingDevideSta(find_mtcd.bb_no, find_mtcd.mt_cd);
                return Json(new { result = true, message = Constant.Success, kq = find_mtcd }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {

                return Json(new { result = false, message = e.Message}, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult destroyDevide(string mt_cd)
        {
            //kiểm tra mã lot này tồn tại không
          
            var check_mt_cd = _IWOService.GetWMaterialInfowithmtcd(mt_cd);
            if (check_mt_cd == null)
            {
                return Json(new { result = false, message = Constant.NotFound }, JsonRequestBehavior.AllowGet);
            }
            //mã này có bằng 0 và trạng thái = 005 chưa và không tồn tại đồ đựng này trong history nhé
            if (check_mt_cd.gr_qty != 0)
            {
                return Json(new { result = false, message = "Sản Lượng Vẫn Còn Không thể trở về" }, JsonRequestBehavior.AllowGet);
            }
            if (check_mt_cd.mt_sts_cd != "005")
            {
                return Json(new { result = false, message = Constant.Status }, JsonRequestBehavior.AllowGet);
            }

           
            if (!_TIMSService.CheckWMaterialMapForDV(mt_cd))
            {
                return Json(new { result = false, message = "Mã Devide đã được chuyển qua công đoạn khác !! " }, JsonRequestBehavior.AllowGet);
            }
            if (!_TIMSService.CheckWMaterialMapForDV(mt_cd))
            {
                return Json(new { result = false, message = Constant.Other }, JsonRequestBehavior.AllowGet);
            }
            //kiểm tra đã gộp chưa
            // xử lí
            StringBuilder varname1 = new StringBuilder();
            int CheckExistMappig = _TIMSService.CheckWMaterialMappingForRedo(mt_cd);
            if (CheckExistMappig > 0)
            {
                return Json(new { result = false, message = "Đã được gộp lot nên không thể trở về !! " }, JsonRequestBehavior.AllowGet);
            }

            IEnumerable<WMaterialnfoDV> dsach = _TIMSService.DSWMaterialDV(mt_cd);

            var ketqua = dsach.Select(x => x.blno);
            var blno = string.Join(",", ketqua);
            var ketqua1 = dsach.Select(x => x.bno);
            var bno = string.Join(",", ketqua1);
            var gr_qty = _TIMSService.SumGrqtyDSWMaterialDV(mt_cd);
           
            _TIMSService.UpdateWMaterialQtyForRedo(gr_qty, check_mt_cd.mt_cd);


            //xoa mã devide
            _TIMSService.DeleteWMaterialQtyForRedo(check_mt_cd.mt_cd);

            //Xóa mã devedie mapping
            _TIMSService.DeleteWMaterialMappingForRedo(check_mt_cd.mt_cd);

            //XÓA BOBIN HISTORY DEVIDE
            if (blno != "")
            {
                _TIMSService.DeleteBoBbinHisForRedo(blno);

            }
            if (bno != "")
            {
                _TIMSService.UpdateBobbinInfoForRedo(bno);

            }
            //khoi phuc bb
            int effect_rows = _TIMSService.CallSPkhoiphucbobin(check_mt_cd.mt_cd, check_mt_cd.bb_no);

            return Json(new { result = true, message = Constant.Success, gr_qty = gr_qty, wmtid = check_mt_cd.wmtid }, JsonRequestBehavior.AllowGet);

        }

        private int ToNumber(string input)
        {
            string StringNumber = "";
            for (int i = 0; i < input.Length; i++)
            {
                if (char.IsDigit(input[i]))
                    StringNumber += input[i];
            }
            if (StringNumber.Length > 0)
                return Convert.ToInt32(StringNumber);
            else
                return 0;
        }

      
        public JsonResult change_gr_dv(string value_new, string value_old, string wmtid, int psid)
        {
            return Json(new { result = false, message = "Đã hủy chức năng này khỏi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Changebb_dvEA(string bb_no, int wmmid)
        {//chức năng thay đổi ở nhưng cuộn EA còn dư
            try
            {
                //kiểm tra bobbin có tồn tại không

                string CheckBobbinExist = _TIMSService.GetBobbinExist(bb_no);

                if (string.IsNullOrEmpty(CheckBobbinExist))
                {
                    return Json(new { result = false, message = "Bobbin này không tồn tại, Mã bobbin bạn vừa quét: " + bb_no }, JsonRequestBehavior.AllowGet);
                }
                string CheckBobbinUsing = _TIMSService.GetBobbinUsing(bb_no);
                //var check_bb_new_his = db.d_bobbin_lct_hist.Where(x => x.bb_no == bb_no).ToList();
                if (!string.IsNullOrEmpty(CheckBobbinUsing))
                {
                    return Json(new { result = false, message = Constant.Used }, JsonRequestBehavior.AllowGet);
                }
                var check_mapping2 = db.w_material_mapping.Find(wmmid);
                var check_mapping1 = db.w_material_mapping.Where(x => x.mt_cd == check_mapping2.mt_cd && x.mt_lot != check_mapping2.mt_lot).OrderByDescending(x => x.mapping_dt).ToList();

                if (check_mapping2 != null)
                {
                    foreach (var item in check_mapping1)
                    {
                        //so sánh thời gian mapping
                        var mapping_hientai = check_mapping2.mapping_dt;
                        if (Convert.ToInt64(item.mapping_dt) > Convert.ToInt64(mapping_hientai))
                        {
                            return Json(new { result = false, message = Constant.Other }, JsonRequestBehavior.AllowGet);
                        }
                    }
                }

                var check = db.w_material_info.Where(x => x.mt_cd == check_mapping2.mt_cd && x.gr_qty > 0).SingleOrDefault();
                if (check == null)
                {
                    return Json(new { result = false, message = "Lot " + Constant.NotFound }, JsonRequestBehavior.AllowGet);
                }
                if (check.mt_sts_cd == "009" || check.mt_sts_cd == "010")
                {
                    return Json(new { result = false, message = Constant.Other }, JsonRequestBehavior.AllowGet);
                }
                if (check.gr_qty == 0)
                {
                    return Json(new { result = false, message = "Đã sử dụng hết" }, JsonRequestBehavior.AllowGet);
                }
              

                StringBuilder thaydoibobin = new StringBuilder();
                thaydoibobin.Append(" CALL `change_bb_EA`('" + bb_no + "', '" + check.wmtid + "', '" + check.bb_no + "', '" + check.mt_cd + "'); \n");
                Log.Info("TIMS=>Changebb_dvEA" +
                    "bobin moi " + bb_no +
                    "mt cd " + check.mt_cd +
                    "bobin cu " + check.bb_no);
                int tinhtoan = new Excute_query().Execute_NonQuery(thaydoibobin);

                return Json(new { result = true, message = Constant.Success }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult Changebb_dv(string bb_no, int wmtid)
        {//chức năng thay đổi ở sau khi chia ra hộp nhỏ
            try
            {
               
                var check = _IWOService.GetWMaterialInfo(wmtid);
                if (check == null)
                {
                    return Json(new { result = false, message = Constant.NotFound + " MT NO" }, JsonRequestBehavior.AllowGet);
                }
                
                if (_IWOService.CheckwMaterialMapping(check.mt_cd) > 0)
                {
                    return Json(new { result = false, message = Constant.Other }, JsonRequestBehavior.AllowGet);
                }
                if (check.mt_sts_cd == "009" || check.mt_sts_cd == "010")
                {
                    return Json(new { result = false, message = Constant.Other }, JsonRequestBehavior.AllowGet);
                }

               
                var check_bb_new = _IWOService.GetdbobbininfoforChangebbdv(bb_no);
                
                bool check_bb_new_his = _IWOService.CheckBobbinHistory(bb_no);
               
                if (!check_bb_new_his)// nếu = false chạy vào đây (mã BObin này chưa tồn tại trong bảng d_bobbin_info)
                {
                    return Json(new { result = false, message = Constant.Used }, JsonRequestBehavior.AllowGet);
                }
                if (check_bb_new == null)
                {
                    return Json(new { result = false, message = Constant.NotFound + " Container" }, JsonRequestBehavior.AllowGet);
                }
                //đã kiểm tra  chưa
                if (!_TIMSService.CheckWMaterialHasNG(check.mt_cd))
                {
                    return Json(new { result = false, message = Constant.Other }, JsonRequestBehavior.AllowGet);
                }

                //đã check bất cứ TQC OQC ANY Chưa
                if (!_TIMSService.CheckFaclineQCHasNG(check.mt_cd) || !_TIMSService.CheckWProductQCHasNG(check.mt_cd))
                
                {
                    return Json(new { result = false, message = Constant.Tested }, JsonRequestBehavior.AllowGet);
                }

                
                int tinhtoan = _TIMSService.ChangeBobinTimsDv(bb_no, wmtid, check.bb_no, check.mt_cd);

                return Json(new { result = true, message = Constant.Success }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion Devide

        #region Process OQC

        public ActionResult GetbobinOqc(Pageing pageing, int id_actual, string at_no)
        {
            var bb_nm = Request["bb_nm"];
            var bb_no = Request["bb_no"];
            var datatt = _TIMSService.Getdbobbinlcthist(id_actual, at_no, bb_nm, bb_no);
            int start = (pageing.page - 1) * pageing.rows;
            int totals = datatt.Count();
            int totalPages = (int)Math.Ceiling((float)totals / pageing.rows);
            IEnumerable<WMaterialInfoTmp> dataactual = datatt.Skip<WMaterialInfoTmp>(start).Take(pageing.rows);
            var jsonReturn = new
            {
                total = totalPages,
                page = pageing.page,
                records = totals,
                rows = dataactual
            };
            return Json(jsonReturn, JsonRequestBehavior.AllowGet);
        }

        public JsonResult getListQR_oqc(string bb_no, int id_actual, string staff_id)
        {
            try
            {
                //kiem tra do dung
               
                var history = _TIMSService.FindOneBobbin_lct_hist(bb_no);
                if (history == null)
                {
                    return Json(new { result = false, message = "Đồ đựng này " + Constant.Used }, JsonRequestBehavior.AllowGet);
                }
                //kiểm tra mã lot đó có không
                
                var find = _IWOService.GetWMaterialInfowithmtcd(history.mt_cd);

                if (find == null)
                {
                    return Json(new { result = false, message = "Đồ đựng này " + Constant.Used }, JsonRequestBehavior.AllowGet);
                }
                if (find.gr_qty == 0)
                {
                    return Json(new { result = false, message = "Sản lượng đang bằng 0 !! Vui lòng kiểm tra lại" }, JsonRequestBehavior.AllowGet);
                }
                if (find.mt_sts_cd.Equals("009"))
                {
                    return Json(new { result = false, message = "Hộp hàng này đã có người khác kiểm tra rồi." + find.staff_id_oqc }, JsonRequestBehavior.AllowGet);
                }
                if (find.mt_sts_cd != "002" && find.mt_sts_cd != "008")
                {
                    var trangthai = checktrangthai(find.mt_sts_cd);
                    return Json(new { result = false, message = "Trạng Thái đang là " + trangthai }, JsonRequestBehavior.AllowGet);
                }
                //kiểm tra xem coi công nhân này còn ca không
                if (_TIMSService.CheckStaffShift(id_actual, staff_id))
                {
                    return Json(new { result = false, message = Constant.EndShift }, JsonRequestBehavior.AllowGet);
                }

                var id_lot = id_actual;
                var id_cd = find.id_actual;
                
                var check_lot = _TIMSService.FindOneWActual(id_lot);

              
                var check_cd = _IWOService.GetWActual((int)id_cd);

                var check_po = "";
                if (check_lot.at_no == check_cd.at_no)
                {
                    check_po = "OK";
                }
                if (check_po == "")
                {
                    return Json(new { result = false, message = "Chọn Sai PO! Xin vui lòng chọn lại!" }, JsonRequestBehavior.AllowGet);
                }
                //kiểm tra trạng thái hiện tại và đã đủ công đoạn chưa
                //kiểm tra bao nhiêu công đoạn
                //IEnumerable<w_actual> process = _TIMSService.GetListWActualForProcess(check_cd.at_no).ToList();

                //truy xuất lot
                IEnumerable<truyxuatlot> txlot;
                txlot = _TIMSService.CheckScanOQC(find.mt_cd, check_cd.at_no);
                //if (!string.IsNullOrEmpty(find.buyer_qr) || find.mt_sts_cd == "010")
                //{

                //    txlot = _TIMSService.Truyxuatlistlot(find.mt_cd, "", find.buyer_qr);
                //}
                //else
                //{
                //    txlot = _TIMSService.Truyxuatlistlot(find.mt_cd, "CP", find.buyer_qr);

                //}
                //kiem tra ton tai đầy đủ process không

                //List<string> khongTonTai = process.Any(x=>x.name == txlot)
               // var khongton_tai = process.Where(item => !txlot.Any(p => p.process_cd == item.name));

                   // process.Where(s => txlot.Any(p => p.process_cd == s.name)).ToList();


                //var khongton_tai = (from c in process
                //                    where txlot
                //                    .Contains(c.name)
                //                    select new { process = c.name }).ToList();
                if (txlot.Count() > 0)
                {
                    var html = "Bạn chưa trải qua những công đoạn này :" + string.Join("<br> ", txlot.Select(x => x.process_cd)); ;
                    return Json(new { result = false, message = html }, JsonRequestBehavior.AllowGet);

                }
                if (find.mt_sts_cd != "009" && find.lct_cd.StartsWith("006"))
                {
                    //update trạng thái và đưa vào võ
                    find.id_actual_oqc = id_actual;
                    find.staff_id_oqc = staff_id;
                    find.mt_sts_cd = "009";
                    find.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    int result = _TIMSService.UpdateWMaterialInfoById(find.id_actual_oqc, find.staff_id_oqc, find.mt_sts_cd, find.chg_id, find.wmtid);
                   
                }
                return Json(new
                {
                    result = true,
                    kq = new
                    {
                        wmtid = find.wmtid,
                        bb_no = find.bb_no,
                        mt_no = find.mt_no,
                        bb_nm = "",
                        mt_cd = find.mt_cd,
                        gr_qty = find.gr_qty,
                        id_actual = find.id_actual,
                        count_ng = 0,
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }
        public string checktrangthai(string mt_sts_cd)
        {
            var check_trangthai = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == mt_sts_cd).ToList();
            var csv = string.Join(", ", check_trangthai.Select(x => x.dt_nm));

            return csv;
        }
       
        public ActionResult PartialView_View_OQC_WEB(string item_vcd)
        {
            //lấy hết tất cả qc_itemcheck_mt
            var qc_itemcheck_mt = new List<qc_itemcheck_mt_Model>();
            var qc_itemcheck_mt2 = new List<qc_itemcheck_mt_Model>();
            qc_itemcheck_mt = db.qc_itemcheck_mt
               .Where(x => x.item_vcd == item_vcd && x.del_yn.Equals("N"))
               .Select(x => new qc_itemcheck_mt_Model
               {
                   qc_itemcheck_mt__check_subject = x.check_subject,
                   qc_itemcheck_mt__check_id = x.check_id,
                   qc_itemcheck_mt__icno = x.icno,
               })
               .ToList();

            foreach (var item in qc_itemcheck_mt)
            {
                var view_qc_Model = new List<view_qc_Model>();

                //lấy hết tất cả qc_itemcheck_dt
                var qc_itemcheck_dt = db.qc_itemcheck_dt
                    .Where(x => x.item_vcd.Equals(item_vcd) && x.check_id.Equals(item.qc_itemcheck_mt__check_id) && (x.del_yn == "N") && (x.defect_yn == "Y")).ToList();
                if (qc_itemcheck_dt.Count > 0)
                {
                    foreach (var item1 in qc_itemcheck_dt)
                    {
                        var view_qc_Model_item = new view_qc_Model();
                        //add check_name
                        view_qc_Model_item.qc_itemcheck_dt__check_name = item1.check_name;
                        view_qc_Model_item.qc_itemcheck_dt__icdno = item1.icdno;

                        //add view_qc_Model
                        view_qc_Model.Add(view_qc_Model_item);
                    }
                }

                item.view_qc_Model = view_qc_Model;
            }
            qc_itemcheck_mt2.AddRange(qc_itemcheck_mt);

            foreach (var ds in qc_itemcheck_mt2)
            {
                if (ds.view_qc_Model.Count == 0)
                {
                    qc_itemcheck_mt.Remove(ds);
                }
            }
            return PartialView(qc_itemcheck_mt);
        }

        

        public ActionResult Getfacline_oqc(string item_vcd, string mt_cd)
        {
            var sql = new StringBuilder();

            sql.Append(" SELECT b.pqno,b.pq_no,CONCAT(substr(b.work_dt,1,4),'-',substr(b.work_dt,5,2),'-',substr(b.work_dt,7,2),' ',substr(b.work_dt,9,2),':',substr(b.work_dt,11,2) )work_dt,b.check_qty,(b.ok_qty),(b.check_qty)-(b.ok_qty) as defect_qty ")
            .Append(" from w_product_qc as b where b.item_vcd='" + item_vcd + "' and b.ml_no='" + mt_cd + "' order by pq_no='TOTAL' desc ,check_qty ,pq_no  ");

            var data = new DataTable();
            using (var cmd = db.Database.Connection.CreateCommand())
            {
                db.Database.Connection.Open();
                cmd.CommandText = sql.ToString();
                using (var reader = cmd.ExecuteReader())
                {
                    data.Load(reader);
                }
            }
            db.Database.Connection.Close();
            var result = GetJsonPersons(data);
            return result;
        }

        public ActionResult Getfacline_oqc_value(string pq_no)
        {
            var list = (from p in db.w_product_qc_value
                        join b in db.qc_itemcheck_mt
                        on p.check_id equals b.check_id
                        where p.pq_no == pq_no
                        select new
                        {
                            pqhno = p.pqhno,
                            check_subject = b.check_subject,
                            check_value = p.check_value,
                            check_qty = p.check_qty,
                        }).ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Getmt_lotOQC(int id_actual, string staff_id)
        {
            IEnumerable<WMaterialInfoOQCAPI> Data = _TIMSService.GetListLotOQCpp(id_actual, staff_id);

            return Json(Data, JsonRequestBehavior.AllowGet);

        }

        public JsonResult Get_OKPO(Pageing pageing, string mt_cd, string mt_no, string bb_no, string product, string at_no, string staff_id)
        {
            try
            {
                StringBuilder sql = new StringBuilder($"SELECT a.wmtid,a.mt_cd,a.mt_no,a.gr_qty,a.bb_no,b.at_no,a.staff_id  FROM w_material_info AS a JOIN w_actual AS b ON a.id_actual=b.id_actual");
                sql.Append(" JOIN w_actual_primary AS c ON b.at_no=c.at_no")
                .Append(" JOIN d_bobbin_lct_hist AS d ON a.mt_cd=d.mt_cd")
                  .Append(" WHERE a.lct_cd LIKE '006%' AND  a.gr_qty>0    ")
                   //.Append(" -- AND b.`type`!='SX' ")
                   .Append("and ('" + at_no + "'='' or b.at_no LIKE '%" + at_no + "%')")
                   .Append("and ('" + staff_id + "'='' or a.staff_id LIKE '%" + staff_id + "%')")
                   .Append("and ('" + mt_cd + "'='' or a.mt_cd LIKE '%" + mt_cd + "%')")
                   .Append("and ('" + product + "'='' or c.product LIKE '%" + product + "%')")
                    .Append("and ('" + mt_no + "'='' or a.mt_no LIKE '%" + mt_no + "%')")
                    .Append("and ('" + bb_no + "'='' or a.bb_no LIKE '%" + bb_no + "%')");
                DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);

                int total = dt.Rows.Count;
                var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("wmtid"));
                return new InitMethods().ReturnJsonResultWithPaging(pageing, total, result);
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public JsonResult Changests_packing(int wmtid, int psid)
        {
            try
            {
                var data = _TIMSService.FindAllMeterialChangestPacking(wmtid);
                var Sucess = 0;
                var Not_Enough = 0;
                var id_actual = 0;
                if (data != null)
                {
                    id_actual = data.id_actual_oqc ?? 0;
                    if (_TIMSService.FindDProUnitStaffChangestsPacking(data.id_actual_oqc, psid) == 0)
                    {
                        return Json(new { result = false, message = Constant.EndShift }, JsonRequestBehavior.AllowGet);
                    }
                    _TIMSService.UpdateWMaterialInfoByIdSingle("010", (Session["userid"] == null) ? "" : Session["userid"].ToString(), DateTime.Now, wmtid);

                    _TIMSService.UpdateactualDprounitstaffChangestspacking(data.gr_qty.HasValue ? data.gr_qty.Value : 0.0, id_actual, psid);

                    int SumActual = _TIMSService.GetSumactualChangestspacking(id_actual);
                    _TIMSService.UpdatewactualChangestspacking(id_actual, SumActual);
                    return Json(new { result = true, message = Constant.Success, Not_Enough, Sucess }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { result = false, message = Constant.CannotPass, Not_Enough, Sucess }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = Constant.ErrorSystem, Not_Enough = Constant.ErrorSystem, Sucess = "0" }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult ChangestspackingWeb(string wmtid, int psid)
         {
            try
             {
                var data = _TIMSService.FindAllMeterialChangestPackingweb(wmtid).ToList();
                var Not_Enough = 0;
                int id_actual = 0;
                if (data.Count() > 0)
                {
                    id_actual = data[0].id_actual_oqc ?? 0;
                    if (_TIMSService.FindDProUnitStaffChangestsPacking(id_actual, psid) == 0)
                    {
                        return Json(new { result = false, message = Constant.EndShift }, JsonRequestBehavior.AllowGet);
                    }
                    _TIMSService.UpdateWMaterialInfoByIdMultiple("010", (Session["userid"] == null) ? "" : Session["userid"].ToString(), DateTime.Now, wmtid);
                    double sanluong = 0.0;
                    foreach (var item in data)
                    {
                        sanluong += item.gr_qty.HasValue ? item.gr_qty.Value : 0.0;
                    }
                    _TIMSService.UpdateactualDprounitstaffChangestspacking(sanluong ,id_actual, psid);
                    int SumActual = _TIMSService.GetSumactualChangestspacking(id_actual);
                    _TIMSService.UpdatewactualChangestspacking(id_actual, SumActual);
                    return Json(new { result = true, message = Constant.Success, Not_Enough }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { result = false, message = Constant.CannotPass, Not_Enough }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = Constant.ErrorSystem, Not_Enough = Constant.ErrorSystem, Sucess = "0" }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult Returnsts_packing(string wmtid)
        {
            try
            {
                //start
                var data = _TIMSService.FindAllMaterialInfoReturn(wmtid).ToList();
                var check_oqc = 0;
               
                var Sucess = 0;
                
                foreach (var item in data)
                {
                    if (!db.w_product_qc.Any(x => x.ml_no == item.mt_cd))
                    {
                        check_oqc++;
                    }
                    //tra ve nguyen
                    item.mt_sts_cd = "008";
                    item.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    int effectRow = _TIMSService.UpdateWMaterialInfoById(item.mt_sts_cd, item.chg_id, item.wmtid);
                    
                    var id_oqc = _TIMSService.FindOneWActual(item.id_actual_oqc);
                    if (id_oqc != null)
                    {
                        id_oqc.defect = id_oqc.defect + item.gr_qty;
                        id_oqc.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                        _TIMSService.UpdateDefectActual(id_oqc.defect, id_oqc.chg_id, id_oqc.id_actual);
                    }
                    Sucess++;
                }
                if (Sucess > 0)
                {
                    return Json(new { result = true, message = Constant.Success }, JsonRequestBehavior.AllowGet);
                }
               
                return Json(new { result = false, message = Constant.CannotPass }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        public void ReleaseContainer(string bobbin, string mt_cd, string mt_sts_cd)
        {
            //kiểm tra xem gr_qty=0 chưa
            if (mt_sts_cd == "005")
            {
                
                _TIMSService.UpdateBobbinInfo(Session["userid"] == null ? null : Session["userid"].ToString(), bobbin, mt_cd);
                _TIMSService.DeleteBobbinHistory(bobbin, mt_cd);
                
            }
           
        }
        public JsonResult Gop_OK(string wmtid, string mt_cd, int soluong, int psid)
        {
            try
            {
               
                WMaterialInfoTmp currentMaterial = _TIMSService.FindOneMaterialInfoByMTLot(mt_cd);
                if (currentMaterial == null)
                {
                    return Json(new { result = false, message = "MT LOT " + Constant.NotFound }, JsonRequestBehavior.AllowGet);
                }
                if (String.IsNullOrEmpty(currentMaterial.bb_no))
                {
                    return Json(new { result = false, message = "Vui Lòng Thêm đồ đựng !!!!" }, JsonRequestBehavior.AllowGet);
                }
                //kiểm tra coi n đã hết thời gian làm việc chưa
                if (_TIMSService.CheckShift(psid, currentMaterial.id_actual.Value) == 0)
                {
                    return Json(new { result = false, message = Constant.EndShift }, JsonRequestBehavior.AllowGet);
                }
               
                IEnumerable<WMaterialInfoTmp> listMaterial = _TIMSService.GetListMaterial(wmtid, mt_cd, currentMaterial.mt_no).ToList();
               
                double ChosenNG = listMaterial.Sum(x => x.gr_qty.Value);
                if (ChosenNG < soluong)
                {
                    return Json(new { result = false, message = Constant.QuantityNGInvalid }, JsonRequestBehavior.AllowGet);
                }
               
                int Count = 0;
                int RemainingNumber = soluong;
                bool Success = false;
                foreach (WMaterialInfoTmp item in listMaterial)
                {
                    if (RemainingNumber <= 0)
                    {
                        break;
                    }
                    if ((item.gr_qty >= soluong) && Count == 0)
                    {
                        ////trừ số lượng còn lại của ng và update
                        item.gr_qty = item.gr_qty - soluong;
                        ////nếu số lượng hết thì cho n đã sử dụng hết
                       
                        if (item.gr_qty == 0)
                        {
                           
                            _TIMSService.UpdateMaterial(item.gr_qty, item.wmtid, "005");
                            ReleaseContainer(item.bb_no, item.mt_cd, "005");
                            
                        }
                        else
                        {
                            _TIMSService.UpdateMaterial(item.gr_qty, item.wmtid);
                        }

                        //add mapping
                        //ADD mã mới
                       
                        int IncrementNumber = _TIMSService.CountMaterialInfo(item.mt_cd + "-MG%") + 1;
                        item.orgin_mt_cd = item.mt_cd;
                        item.mt_cd = item.mt_cd + "-MG" + IncrementNumber;
                        item.gr_qty = 0;
                        item.real_qty = soluong;
                        item.mt_sts_cd = "005";
                        _TIMSService.InsertMergeMaterial(item);
                        if (currentMaterial.mt_cd != item.mt_cd && !_TIMSService.CheckExistMaterialMapping(item.mt_cd, currentMaterial.mt_cd))
                        {
                            w_material_mapping addmapping = new w_material_mapping();
                            addmapping.mt_cd = item.mt_cd;
                            addmapping.mt_lot = currentMaterial.mt_cd;
                            addmapping.mt_no = currentMaterial.mt_no;
                            addmapping.mapping_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                            addmapping.bb_no = currentMaterial.bb_no;
                            addmapping.use_yn = "N";
                            addmapping.del_yn = "N";
                            addmapping.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                            addmapping.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                            addmapping.reg_dt = DateTime.Now;
                            addmapping.chg_dt = DateTime.Now;
                            _TIMSService.InsertMaterialMapping(addmapping);
                           
                            Success = true;
                        }
                        break;
                    }
                    else
                    {
                        var GrQuantity = item.gr_qty;

                        item.gr_qty = (RemainingNumber - item.gr_qty > 0 ? 0 : (-1 * (RemainingNumber - item.gr_qty)));
                       
                        if (item.gr_qty == 0)
                        {
                            _TIMSService.UpdateMaterial(item.gr_qty, item.wmtid, "005");
                            ReleaseContainer(item.bb_no, item.mt_cd, "005");
                        }
                        else
                        {
                            _TIMSService.UpdateMaterial(item.gr_qty, item.wmtid);
                        }
                        // ADD mã mới
                        
                        int IncrementNumber = _TIMSService.CountMaterialInfo(item.mt_cd + "-MG%") + 1;
                        item.orgin_mt_cd = item.mt_cd;
                        item.mt_cd = item.mt_cd + "-MG" + IncrementNumber;
                        item.real_qty = item.gr_qty;
                        item.mt_sts_cd = "005";
                        _TIMSService.InsertMergeMaterial(item);
                       
                        //add mapping
                        //ADD mã mới
                        if (currentMaterial.mt_cd != item.mt_cd && !_TIMSService.CheckExistMaterialMapping(item.mt_cd, currentMaterial.mt_cd))
                        {
                           
                            var addmapping = new w_material_mapping();
                            addmapping.mt_cd = item.mt_cd;
                            addmapping.mt_lot = currentMaterial.mt_cd;
                            addmapping.mt_no = currentMaterial.mt_no;
                            addmapping.mapping_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                            addmapping.bb_no = currentMaterial.bb_no;
                            addmapping.use_yn = "N";
                            addmapping.del_yn = "N";
                            addmapping.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                            addmapping.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                            addmapping.reg_dt = DateTime.Now;
                            addmapping.chg_dt = DateTime.Now;
                            _TIMSService.InsertMaterialMapping(addmapping);
                          
                        }
                        Success = true;
                        RemainingNumber = RemainingNumber - int.Parse(GrQuantity.ToString());
                    }
                    Count++;
                }
                if (Success)
                {
                    currentMaterial.gr_qty = currentMaterial.gr_qty + soluong;
                    _TIMSService.UpdateMaterial(currentMaterial.gr_qty, currentMaterial.wmtid);
                    

                    return Json(new { result = true, message = Constant.Success }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

     

        public JsonResult Get_NGPO(Pageing pageing, int id_actual, string mt_cd, string mt_no, string bb_no, string PO)
        {
            try
            {
                
                WActual FindPO = _TIMSService.FindOneWActual(id_actual);
                if (FindPO == null)
                {
                    return Json(new { result = false, message = "PO " + Constant.NotFound }, JsonRequestBehavior.AllowGet);
                }
                
                WActualPrimary FindPrimary = _TIMSService.FindOneWActualPrimaryByAtNo(FindPO.at_no);
                
                IEnumerable<NGPO> data = _TIMSService.GetNotGoodPO(FindPrimary.product, mt_cd, mt_no, PO);
                var records = data.Count();
                int totalPages = (int)Math.Ceiling((float)records / pageing.rows);
                var rowsData = data.Skip((pageing.page - 1)).Take(pageing.rows);
                return Json(new { total = totalPages, page = pageing.page, records = records, rows = rowsData }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult Gop_NG(string wmtid, string mt_cd, int soluong, int psid)
        {
            try
            {
                var chekck_mtcd = db.w_material_info.Where(x => x.mt_cd == mt_cd).SingleOrDefault();
                if (chekck_mtcd == null)
                {
                    return Json(new { result = false, message = " MT LOT" + Constant.NotFound }, JsonRequestBehavior.AllowGet);
                }


                //kiểm tra coi n đã hết thời gian làm việc chưa

                String varname1 = "";
                varname1 = varname1 + "SELECT * FROM d_pro_unit_staff AS a " + "\n";
                varname1 = varname1 + "WHERE a.id_actual='" + chekck_mtcd.id_actual + "' and a.psid='" + psid + "' AND (NOW() BETWEEN " + "\n";
                varname1 = varname1 + "Date_format(a.start_dt, '%Y-%m-%d %H:%i:%s') " + "\n";
                varname1 = varname1 + " AND Date_format(a.end_dt, '%Y-%m-%d %H:%i:%s'))";
                var data1 = db.Database.SqlQuery<d_pro_unit_staff>(varname1).ToList().Count();
                if (data1 == 0)
                {
                    return Json(new { result = false, message = Constant.EndShift }, JsonRequestBehavior.AllowGet);
                }

                var gr_cu = chekck_mtcd.gr_qty;
                StringBuilder varname2 = new StringBuilder();
                varname2.Append("SELECT * \n");
                varname2.Append(" FROM w_material_info  \n");
                varname2.Append("where wmtid in (" + wmtid + ") and gr_qty>0 and mt_sts_cd='012' order by gr_qty desc \n");
                var data = db.w_material_info.SqlQuery(varname2.ToString()).ToList();
                var so_luong_ngchoose = data.Sum(x => x.gr_qty);

                if (so_luong_ngchoose < soluong)
                {
                    return Json(new { result = false, message = Constant.QuantityNGInvalid }, JsonRequestBehavior.AllowGet);
                }
               
                var dem = 0;
                var so_luongcl = soluong;
                var sucess = 0;
                foreach (var item in data)
                {
                    if (so_luongcl <= 0)
                    {
                        break;
                    }
                    var addmapping = new w_material_mapping();
                    if ((item.gr_qty >= soluong) && dem == 0)
                    {
                        //trừ số lượng còn lại của ng và update
                        item.gr_qty = item.gr_qty - soluong;
                        //nếu số lượng hết thì cho n đã sử dụng hết
                        if (item.gr_qty == 0)
                        {
                            addmapping.mt_cd = item.mt_cd;
                            item.mt_sts_cd = "005";
                        }
                        item.lct_cd = "006000000000000000";
                        db.Entry(item).State = EntityState.Modified;
                        //add mapping
                        if (item.gr_qty > 0)
                        {
                            // ADD mã mới
                            var so_tang = db.w_material_info.Where(x => x.mt_cd.StartsWith(item.mt_cd + "-MG")).ToList().Count() + 1;
                            item.orgin_mt_cd = item.mt_cd;
                            item.mt_cd = item.mt_cd + "-MG" + so_tang;
                            item.mt_sts_cd = "005";
                            item.real_qty = item.gr_qty;
                            db.Entry(item).State = EntityState.Added;
                            addmapping.mt_cd = item.mt_cd;
                        }
                        if (!db.w_material_mapping.Any(x => x.mt_cd == item.mt_cd && x.mt_lot == chekck_mtcd.mt_cd && chekck_mtcd.mt_cd != item.mt_cd))
                        {
                            addmapping.mt_lot = chekck_mtcd.mt_cd;
                            addmapping.mt_no = chekck_mtcd.mt_no;
                            addmapping.mapping_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                            addmapping.bb_no = chekck_mtcd.bb_no;
                            addmapping.use_yn = "N";
                            addmapping.del_yn = "N";
                            addmapping.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                            addmapping.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                            addmapping.reg_dt = DateTime.Now;
                            addmapping.chg_dt = DateTime.Now;
                            db.Entry(addmapping).State = EntityState.Added;
                            sucess++;
                        }
                        break;
                    }
                    else
                    {
                        var so_luongcu = item.gr_qty;

                        item.gr_qty = (so_luongcl - item.gr_qty > 0 ? 0 : (-1 * (so_luongcl - item.gr_qty)));
                        if (item.gr_qty == 0)
                        {
                            item.mt_sts_cd = "005";
                        }
                        item.lct_cd = "006000000000000000";
                        db.Entry(item).State = EntityState.Modified;

                        if (item.gr_qty > 0)
                        {
                            // ADD mã mới
                            var so_tang = db.w_material_info.Where(x => x.mt_cd.StartsWith(item.mt_cd + "-MG")).ToList().Count() + 1;
                            item.orgin_mt_cd = item.mt_cd;
                            item.mt_cd = item.mt_cd + "-MG" + so_tang;
                            item.mt_sts_cd = "005";
                            item.real_qty = item.gr_qty;
                            db.Entry(item).State = EntityState.Added;
                            addmapping.mt_cd = item.mt_cd;
                        }
                        if (!db.w_material_mapping.Any(x => x.mt_cd == item.mt_cd && x.mt_lot == chekck_mtcd.mt_cd && chekck_mtcd.mt_cd != item.mt_cd))
                        {
                            addmapping.mt_lot = chekck_mtcd.mt_cd;
                            addmapping.mt_no = chekck_mtcd.mt_no;
                            addmapping.mapping_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                            addmapping.bb_no = chekck_mtcd.bb_no;
                            addmapping.use_yn = "N";
                            addmapping.del_yn = "N";
                            addmapping.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                            addmapping.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                            addmapping.reg_dt = DateTime.Now;
                            addmapping.chg_dt = DateTime.Now;
                            db.Entry(addmapping).State = EntityState.Added;
                            sucess++;
                        }
                        so_luongcl = so_luongcl - int.Parse(so_luongcu.ToString());
                    }
                    dem++;
                }
                if (sucess > 0)
                {
                    chekck_mtcd.gr_qty = chekck_mtcd.gr_qty + soluong;
                    chekck_mtcd.real_qty = chekck_mtcd.real_qty + soluong;
                    chekck_mtcd.lct_cd = "006000000000000000";
                    db.Entry(chekck_mtcd).State = EntityState.Modified;
                    db.SaveChanges();
                    return Json(new { result = true, message = Constant.Success }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion Process OQC
        #region ViewDetail_ForShift_Worker
        public ActionResult PartialView_dialog_Viewdetaildatetime(int id_actual)
        {
            ViewBag.id_actual = id_actual;

            return PartialView("~/Views/TIMS/Web/PartialView_dialog_Viewdetaildatetime.cshtml");

        }
        public async System.Threading.Tasks.Task<ActionResult> getdetail_actual(int id_actual, string date, string shift, int staff_id = -1)
        {
            try
            {
                var check_vo = _TIMSService.FindOneWActual(id_actual);
                //var check_stff = _TIMSService.FindOneDProUnitStaffById(psid);
                StringBuilder sql = new StringBuilder();

                if (check_vo.name == "OQC")
                {
                    //var data1 = _TIMSService.FindDetailActualStaffAPI2OQC(id_actual, date, shift, staff_id);
                    IEnumerable<WMaterialInfoTIMSAPIRec> data1 = _TIMSService.GetDetailActualAPIOQC(id_actual, date, shift);
                    return Json(data1, JsonRequestBehavior.AllowGet);
                }

                IEnumerable<WMaterialInfoTIMSAPIRec> data = await _TIMSService.GetDetailActualAPIStaffAsync(id_actual, date, shift, staff_id);
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(e, JsonRequestBehavior.AllowGet);
            }
          
        }
        public JsonResult getdetail_actual1(int id_actual, string date, string shift, int staff_id = -1)
        {
            try
            {
                var check_vo = _TIMSService.FindOneWActual(id_actual);
                //var check_stff = _TIMSService.FindOneDProUnitStaffById(psid);
                StringBuilder sql = new StringBuilder();

                if (check_vo.name == "OQC")
                {
                    //var data1 = _TIMSService.FindDetailActualStaffAPI2OQC(id_actual, date, shift, staff_id);
                    IEnumerable<WMaterialInfoTIMSAPIRec> data1 = _TIMSService.GetDetailActualAPIOQC(id_actual, date, shift);
                    return Json(data1, JsonRequestBehavior.AllowGet);
                }

                IEnumerable<WMaterialInfoTIMSAPIRec> data = _TIMSService.GetDetailActualAPI2(id_actual, date, shift, staff_id);
                //IEnumerable<WMaterialInfoTIMSAPI> data = _TIMSService.GetDetailActualAPI(id_actual, date, shift);
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(e, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region ViewDetail_ForRecevingScan
        public ActionResult PartialView_dialog_ViewDetailReceiving(string po)
        {
            ViewBag.po = po;

            return PartialView("~/Views/TIMS/Web/PartialView_dialog_ViewDetailReceiving.cshtml");

        }
        public JsonResult getdetail_RecordReceing(string date , string product, string shift)
        {
            try
            {
                StringBuilder sql = new StringBuilder();
                IEnumerable<WMaterialInfoTIMSAPIReceing> data = _TIMSService.GetDetailActualAPIReceiving(date, product, shift);
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(e, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #endregion TIMS Web

        #region TIMS_MAPPINGBUYER

        public ActionResult Scan_Buyer()
        {
            return SetLanguage("");
        }

        public JsonResult Getmt_mappingOQC(Pageing pageing, string product, string bb_bo, string at_no)
        {
            StringBuilder sql = new StringBuilder($" SELECT a.wmtid, a.at_no,a.bb_no,a.product, a.gr_qty,a.mt_cd,a.buyer_qr, DATE_FORMAT(a.end_production_dt,'%Y-%m-%d %H:%i:%s') input_dt ")
                .Append(" FROM w_material_info as a ")
                .Append(" JOIN d_bobbin_lct_hist as b on a.bb_no=b.bb_no and a.mt_cd=b.mt_cd ")
                .Append(" WHERE a.mt_sts_cd='010' and  a.gr_qty>0  ")
                .Append(" AND (a.buyer_qr IS   NULL OR  a.buyer_qr ='')  ")
                .Append("AND ('" + product + "'='' or a.mt_no LIKE '%" + product + "%')")
                .Append("AND ('" + bb_bo + "'='' or a.bb_no LIKE '%" + bb_bo + "%')")
                .Append("AND ('" + at_no + "'='' or a.at_no LIKE '%" + at_no + "%')");

            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);
            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("wmtid"));
            return new InitMethods().ReturnJsonResultWithPaging(pageing, total, result);
        }

        public JsonResult mapping_buyer(string bb_no, string buyer_qr)
        {
            try
            {
                if (string.IsNullOrEmpty(bb_no))
                {
                    return Json(new { result = false, message = "Vui lòng scan mã Container" }, JsonRequestBehavior.AllowGet);
                }
                if (string.IsNullOrEmpty(buyer_qr))
                {
                    return Json(new { result = false, message = "Vui lòng scan mã Buyer" }, JsonRequestBehavior.AllowGet);
                }
                //check tồn tại buyer này chưa

                
                if (_TIMSService.GetQrcodeBuyer(buyer_qr)>0)
                {
                    return Json(new { result = false, message = "Buyer Qr đã được sử dụng." }, JsonRequestBehavior.AllowGet);
                }
                
                bool checkbuyer = _TIMSService.CheckQRBuyer(bb_no);
                if (!checkbuyer)
                {
                    return Json(new { result = false, message = "Đồ đựng này đã được mapping với buyer!!!!." }, JsonRequestBehavior.AllowGet);

                }
               
                var check_bobin = _TIMSService.FindOneDBobbinInfo(bb_no);
                if (check_bobin == null)
                {
                    return Json(new { result = false, message = "Container " + Constant.NotFound }, JsonRequestBehavior.AllowGet);
                }
                var check_bobin_history = _TIMSService.GetdbobbinlcthistFrbbno(bb_no);
               
                if (check_bobin_history == null)
                {
                    return Json(new { result = false, message = "Container " + Constant.NotFound }, JsonRequestBehavior.AllowGet);
                }

                var sucess = 0;
               
                    var check_mtcd_con = _TIMSService.CheckwmaterialinfoMappingbuyer(check_bobin_history.mt_cd, "010" , "006000000000000000").ToList();
                    if (check_mtcd_con.Count() == 1)
                    {
                        var check_mtcd_1 = check_mtcd_con.SingleOrDefault();
                        //kiểm tra có cùng product không
                        
                        string check_product = _TIMSService.GetProductWactualPrimary(check_bobin_history.mt_cd, "010", "006000000000000000");
                        string trimmed = check_product.Replace(" ","");
                        //if (trimmed == "LP14491031(SSV4902)")
                        //{
                        //        trimmed = "SSV4902";
                        //}
                    // kiểm tra xem product theo quy tắc(0) hay bất quy tắc(1). có nghĩa  là nếu bất quy tắc thì không cần khác product vẫn cho mapping với tem gói
                    var typeProduct = _TIMSService.ChecktypeProduct(check_product);
                    if (typeProduct.Equals("0"))
                    {
                        if (buyer_qr.StartsWith(trimmed.Replace("-", "")))
                        {
                            check_mtcd_1.buyer_qr = buyer_qr;
                            //check_mtcd_1.end_production_dt = DateTime.Now;
                            check_mtcd_1.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();

                            stamp_detail isExisted = _TIMSService.Getstampdetail(buyer_qr);
                            if (isExisted == null)
                            {
                                //ktra stamp_cd
                                var stamp_cd = "";

                                var ktra_stamp_cd = _TIMSService.GetStyleNo(check_product);

                                if (ktra_stamp_cd != null)
                                {
                                    stamp_cd = ktra_stamp_cd.stamp_code;
                                    if (String.IsNullOrEmpty(stamp_cd))
                                    {
                                        return Json(new { result = false, message = "Vui Lòng Chọn kiểu tem cho Product(DMS)!!! " }, JsonRequestBehavior.AllowGet);
                                    }
                                }

                                //lấy ra lot_date bằng cách trừ đi product và 8 kí tự sẽ tìm được mã lot_date
                                var prd = trimmed.Replace(" ", "");
                                var prd1 = trimmed.Replace("-", "");
                                //var startBien = prd1.Length + 8;
                                var timDZIH = buyer_qr.IndexOf("DZIH") + 4; //product+DZIH

                                //CEDMB2976DZIHA1N0L4E001010100
                                //5210000364CSDT1DZIHB2N0L9J049011100

                                //lot_date
                                //product = 5210000364CSDT1;
                                //vendor_code = DZIH
                                //vendor_line = B
                                //label_printer = 2
                                //is_sample = N
                                //PCN = 0
                                // lot_date = L9J
                                //serial_number = 049
                                //machine_line = 01
                                //shift = 1
                                // quantity = 100
                            

                                var vendor_line = buyer_qr.Substring(timDZIH , 1);
                                var label_printer = buyer_qr.Substring(timDZIH + 1, 1);
                                var is_sample = buyer_qr.Substring(timDZIH + 2, 1);
                                var PCN = buyer_qr.Substring(timDZIH + 3, 1);
                                var date = buyer_qr.Substring(timDZIH + 4, 3);
                                var lot_date = DateFormatByShinsungRule(date);

                                var serial_number = buyer_qr.Substring(timDZIH + 7, 3); //gắn 001
                                var machine_line = buyer_qr.Substring(timDZIH + 10, 2); //gắn 01

                                var shift = buyer_qr.Substring(timDZIH + 12, 1);
                                
                       

                                //insert stamp_detail
                                var stamp_detail = new stamp_detail()
                                {
                                    buyer_qr = buyer_qr,
                                    stamp_code = stamp_cd,
                                    product_code = check_product,
                                    vendor_code = "DZIH",
                                    vendor_line = vendor_line,
                                    label_printer = label_printer,
                                    is_sample = is_sample,
                                    pcn = PCN,
                                    lot_date = lot_date,
                                    serial_number = serial_number,
                                    machine_line = machine_line,
                                    shift = shift,
                                    standard_qty = ktra_stamp_cd.pack_amt.HasValue ? ktra_stamp_cd.pack_amt.Value : 0,
                                    reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString(),
                                    chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString(),
                                };

                                _TIMSService.Insertstampdetail(stamp_detail);

                            }
                            _IWOService.UpdateMaterialInfo(check_mtcd_1);
                            sucess++;
                        }
                        else
                        {
                            return Json(new { result = false, message = "Mã tem gói này không thuộc với Product của Container. " }, JsonRequestBehavior.AllowGet);
                        }
                    }
                    else
                    {
                        check_mtcd_1.buyer_qr = buyer_qr;
                        //check_mtcd_1.end_production_dt = DateTime.Now;
                        check_mtcd_1.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();

                        stamp_detail isExisted = _TIMSService.Getstampdetail(buyer_qr);
                        if (isExisted == null)
                        {
                            //ktra stamp_cd
                            var stamp_cd = "";

                            var ktra_stamp_cd = _TIMSService.GetStyleNo(check_product);

                            if (ktra_stamp_cd != null)
                            {
                                stamp_cd = ktra_stamp_cd.stamp_code;
                                if (String.IsNullOrEmpty(stamp_cd))
                                {
                                    return Json(new { result = false, message = "Vui Lòng Chọn kiểu tem cho Product(DMS)!!! " }, JsonRequestBehavior.AllowGet);
                                }
                            }

                            //lấy ra lot_date bằng cách trừ đi product và 8 kí tự sẽ tìm được mã lot_date
                            var prd = trimmed.Replace(" ", "");
                            var prd1 = trimmed.Replace("-", "");
                            var startBien = prd1.Length + 8;


                            //CEDMB2976DZIHA1N0L4E001010100
                            //lot_date
                           

                            //insert stamp_detail
                            var stamp_detail = new stamp_detail()
                            {
                                buyer_qr = buyer_qr,
                                stamp_code = stamp_cd,
                                product_code = check_product,
                                vendor_code = "DZIH",
                                vendor_line = "",
                                label_printer = "1",
                                is_sample = "N",
                                pcn = "0",
                                lot_date = "",
                                serial_number = "1",
                                machine_line = "01",
                                shift = "1",
                                standard_qty = ktra_stamp_cd.pack_amt.HasValue ? ktra_stamp_cd.pack_amt.Value : 0,
                                reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString(),
                                chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString(),
                            };

                            _TIMSService.Insertstampdetail(stamp_detail);

                        }
                        _IWOService.UpdateMaterialInfo(check_mtcd_1);
                        sucess++;
                    }
                        
                    }
                //xóa bobin
                //kiểm tra bobin
                if (check_bobin != null && sucess > 0)
                {
                    _TIMSService.DeleteBobbinHist(check_bobin_history.blno);
                    _TIMSService.Deletedbobbininfo(check_bobin.bno);
                    return Json(new { result = true, message = Constant.Success, data = check_bobin_history,dataWeb = check_mtcd_con }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = "Không thể mapping vì chưa qua công đoạn OQC" }, JsonRequestBehavior.AllowGet);


            }
            catch (Exception e)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
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

        public ActionResult ShippingContainer(string wmtid)
        {
            try
            {
               string user  = Session["userid"] == null ? null : Session["userid"].ToString();
                var userWIP = "";
                if (!string.IsNullOrEmpty(user))
                {
                    var sqlquery = @"SELECT * FROM mb_info WHERE userid=@1 ";
                    var dsMbInfo = db.Database.SqlQuery<mb_info>(sqlquery,
                        new MySqlParameter("1", user)).FirstOrDefault();
               
                    userWIP = dsMbInfo.grade;
                }
                if (userWIP != "Admin")
                {
                    return Json(new { result = false, message = "Tài khoản thuộc quyền Admin mới được chỉnh sửa" }, JsonRequestBehavior.AllowGet);
                }
                int aaa =  _TIMSService.UpdateCompositeShipping(wmtid, user);
                if (aaa == 0)
                {
                    return Json(new { result = false, message = "Không có mã nào được xuất kho" }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = true, message = "Xuất kho thành công" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống" }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion TIMS_MAPPINGBUYER

        #region TIMS_DownReason

        public ActionResult DownReason()
        {
            return SetLanguage("");
        }

        public JsonResult Get_OKReason(Pageing pageing, string mt_lot, string mt_cd, string mt_no, string bb_no, string product)
        {
            try
            {
                
                WMaterialInfoTmp currentMaterial = _TIMSService.FindOneMaterialInfoByMTLot(mt_lot);
                if (currentMaterial == null)
                {
                    return Json(new { result = true, message = Constant.Success, }, JsonRequestBehavior.AllowGet);
                }
               
                WActual findPO = _TIMSService.FindOneWActual(currentMaterial.id_actual);
                if (findPO == null)
                {
                    return Json(new { result = false, message = "PO " + Constant.NotFound }, JsonRequestBehavior.AllowGet);
                }
              
                WActualPrimary findPrimary = _TIMSService.FindOneWActualPrimaryByAtNo(findPO.at_no);
                var data = _TIMSService.GetOKReason(mt_lot, findPrimary.product, findPO.don_vi_pr, mt_cd, product, currentMaterial.mt_no, bb_no);
                var records = data.Count();
                int totalPages = (int)Math.Ceiling((float)records / pageing.rows);
                var rowsData = data.Skip((pageing.page - 1)).Take(pageing.rows);
                return Json(new { total = totalPages, page = pageing.page, records = records, rows = rowsData }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public ActionResult check_update_grty(string mt_cd, int value, string reason)
        {
            //check  không qua công đoạn trước  và chưa kiểm tra PQC
            try
            {
                var data = db.w_material_info.Where(x => x.mt_cd == mt_cd).SingleOrDefault();
                if (data == null)
                {
                    return Json(new { result = false, kq = Constant.NotFound, wmtid = data.wmtid }, JsonRequestBehavior.AllowGet);
                }
                //if (db.w_material_mapping.Any(x => x.mt_cd == mt_cd))
                //{
                //    return Json(new { result = false, kq = Constant.Other, wmtid = data.wmtid }, JsonRequestBehavior.AllowGet);
                //}
                if (value == 0 )
                {
                    return Json(new { result = false, kq = "Không được sửa về 0, sửa về 0 sẽ bị xả", wmtid = data.wmtid }, JsonRequestBehavior.AllowGet);
                }
                //insert bảng giảm có lí do
                var w_material_down = new w_material_down();
                w_material_down.gr_down = value;
                w_material_down.mt_cd = data.mt_cd;
                w_material_down.reason = reason;
                w_material_down.bb_no = data.bb_no;
                w_material_down.sts_now = data.mt_sts_cd;
                w_material_down.gr_qty = data.gr_qty;
                w_material_down.chg_dt = DateTime.Now;
                w_material_down.reg_dt = DateTime.Now;
                w_material_down.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                w_material_down.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                db.Entry(w_material_down).State = EntityState.Added;
                db.SaveChanges(); 

                data.gr_qty = value;
                data.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                data.remark = reason;
                db.Entry(data).State = EntityState.Modified;
                db.SaveChanges(); 

                return Json(new { result = true, wmtid = data.wmtid }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult check_update_grty_api(string container, int value, string reason)
        {
            //check  không qua công đoạn trước  và chưa kiểm tra PQC
            try
            {
                var data = db.d_bobbin_lct_hist.Where(x => x.bb_no == container).ToList();
                if (data.Count == 0)
                {
                    return Json(new { result = false, message = Constant.NotFound + " Container " }, JsonRequestBehavior.AllowGet);
                }
                var mt_cd = data.FirstOrDefault().mt_cd;
                if (db.w_material_mapping.Any(x => x.mt_cd == mt_cd))
                {
                    return Json(new { result = false, message = Constant.Other }, JsonRequestBehavior.AllowGet);
                }
                var check_mt_cd = db.w_material_info.Where(x => x.mt_cd == mt_cd).SingleOrDefault();
                if (check_mt_cd == null)
                {
                    return Json(new { result = false, message = Constant.NotFound }, JsonRequestBehavior.AllowGet);
                }
               
                //insert bảng giảm có lí do

                var w_material_down = new w_material_down();
                w_material_down.gr_down = value;
                w_material_down.mt_cd = check_mt_cd.mt_cd;
                w_material_down.reason = reason;
                w_material_down.bb_no = check_mt_cd.bb_no;
                w_material_down.sts_now = check_mt_cd.mt_sts_cd;
                w_material_down.gr_qty = check_mt_cd.gr_qty;
                w_material_down.chg_dt = DateTime.Now;
                w_material_down.reg_dt = DateTime.Now;
                w_material_down.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                w_material_down.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                db.Entry(w_material_down).State = EntityState.Added;
                db.SaveChanges(); 

                check_mt_cd.gr_qty = value;
                check_mt_cd.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                check_mt_cd.remark = reason;
                db.Entry(check_mt_cd).State = EntityState.Modified;
                db.SaveChanges(); 

                return Json(new { result = true, message = Constant.Success }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetReasonDown(string mt_cd)
        {
            var ds = db.w_material_down.Where(x => x.mt_cd == mt_cd).ToList();
            var danhsach = (from a in ds
                            select new
                            {
                                mt_cd = a.mt_cd,
                                gr_qty = a.gr_qty,
                                gr_down = a.gr_down,
                                reason = a.reason,
                                bb_no = a.bb_no,
                                reg_id = a.reg_id,
                                reg_dt = a.reg_dt.ToString("yyyyy-MM-dd hh:mm:ss"),
                            }
                          ).ToList();
            return Json(new { data = danhsach }, JsonRequestBehavior.AllowGet);
        }

        #endregion TIMS_DownReason

        #region General

        public ActionResult General()
        {
            return SetLanguage("~/Views/TIMS/Inventory/General.cshtml");

        }

        public JsonResult destroyLotCode(int id)
        {
            try
            {
                 string user  = Session["userid"] == null ? null : Session["userid"].ToString();
                var userWIP = "";
                if (!string.IsNullOrEmpty(user))
                {
                    var sqlquery = @"SELECT * FROM mb_info WHERE userid=@1 ";
                    var dsMbInfo = db.Database.SqlQuery<mb_info>(sqlquery,
                        new MySqlParameter("1", user)).FirstOrDefault();
               
                    userWIP = dsMbInfo.grade;
                }
                else
                {
                    return Json(new { result = false, message = "Vui lòng đăng nhập lại, tài khoản bạn không tìm thấy" }, JsonRequestBehavior.AllowGet);
                }
                if (userWIP != "Admin")
                {
                    return Json(new { result = false, message = "Tài khoản thuộc quyền Admin mới được chỉnh sửa" }, JsonRequestBehavior.AllowGet);
                }
                var CheckExistTims = db.w_material_info.Where(x => x.wmtid == id 
                //&& x.lct_cd.Equals("006000000000000000") 
                && (x.mt_sts_cd == "008" ||
                x.mt_sts_cd == "009" || x.mt_sts_cd == "002" || x.mt_sts_cd == "003")).Count();

                //                +Chức năng: Chuyển trạng thái về huy(mt_sts_cd = 011)
                //Điều kiện: lct_cd like '006%' mt_sts_cd in ('008', '009', '002', '003')
                //+ Chức năng trở về trạng thái cũ
               
                if (CheckExistTims > 0)
                {
                    
                    var chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();

                    var data = db.w_material_info.FirstOrDefault(x => x.wmtid == id);
                    var sts_update = data.mt_sts_cd;
                    var mt_cd = data.mt_cd;
                    var bb_no = data.bb_no;

                    //var sql = $"Call sp_update_destroy('{id}','{sts_update}','{mt_cd}','{bb_no}','{chg_id}');";
                    int result =  _TIMSService.UpdateDestroy(id, sts_update, mt_cd, bb_no, chg_id);
                    //var sql = "CALL sp_update_destroy(   ";

                    //sql += " '" + id + "' ,";
                    //sql += " '" + sts_update + "' ,";

                    //sql += " '" + chg_id + "' ,";

                    //sql += " '" + reg_id + "')";

                    //StringBuilder stringBuilder = new StringBuilder(sql);

                    //int result = new Excute_query().Execute_NonQuery(stringBuilder);

                 
                //var sql_insert = new StringBuilder();

                //sql_insert.Append(" SELECT b.id,b.VBobbinCd, ")
                //    .Append("	b.WMaterialCode AS MaterialCode, b.WMaterialNo AS MaterialNo, b.WMaterialLength AS `Length`, b.WMaterialSize AS `Size`, b.WMaterialQty AS `Qty`, b.WMaterialUnit AS `Unit`, b.WMaterialStatusCode AS StatusCode, b.WMaterialStatusName AS StatusName, b.WMaterialReceivedDate AS ReceivedDate ")
                //    .Append(" FROM viewtimsinventorygeneral AS b ")
                //    .Append(" WHERE b.id='" + id + "' ");
                //var list2 = new InitMethods().ConvertDataTableToJsonAndReturn(sql_insert);

                return Json(new { result = true, message = Constant.Success }, JsonRequestBehavior.AllowGet);
                  
                }
                return Json(new { result = false, message ="Mã này không nằm trong các trạng thái được HỦY" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult redoLotCode(int id)
        {
            try
            {
                var check = db.w_material_info.Where(x => x.wmtid == id 
                //&& x.lct_cd.StartsWith("006000000000000000")
                && x.mt_sts_cd == "011").Count();

                //                +Chức năng: Chuyển trạng thái về huy(mt_sts_cd = 011)
                //Điều kiện: lct_cd like '006%' mt_sts_cd in ('008', '009', '002', '003')
                //+ Chức năng trở về trạng thái cũ

                if (check > 0)
                {
                    var chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    var data = db.w_material_info.FirstOrDefault(x => x.wmtid == id);
                    var sts_update = data.sts_update;
                    var bb_no = data.bb_no;
                    var mt_cd = data.mt_cd;

                    int result = _TIMSService.CallSPUpdateRedo(id, sts_update, mt_cd, bb_no, chg_id);


                    return Json(new { result = true, message = Constant.Success }, JsonRequestBehavior.AllowGet);
                    //var sql = "CALL sp_update_redo(   ";

                    //sql += " '" + id + "' ,";
                    //sql += " '" + sts_update + "' ,";
                    //sql += " '" + chg_id + "' ";

                    //sql += ",'" + reg_id + "')";

                    //StringBuilder stringBuilder = new StringBuilder(sql);

                    //int result = new Excute_query().Execute_NonQuery(stringBuilder);

                    //if (result > 0)
                    //{
                    //    var sql_insert = new StringBuilder();

                    //    sql_insert.Append(" SELECT b.id,b.VBobbinCd, ")
                    //        .Append("	b.WMaterialCode AS MaterialCode, b.WMaterialNo AS MaterialNo, b.WMaterialLength AS `Length`, b.WMaterialSize AS `Size`, b.WMaterialQty AS `Qty`, b.WMaterialUnit AS `Unit`, b.WMaterialStatusCode AS StatusCode, b.WMaterialStatusName AS StatusName, b.WMaterialReceivedDate AS ReceivedDate ")
                    //        .Append(" FROM viewtimsinventorygeneral AS b ")
                    //        .Append(" WHERE b.id='" + id + "' ");

                    //    var list2 = new InitMethods().ConvertDataTableToJsonAndReturn(sql_insert);

                    //    var list_data = db.w_material_info.Where(x => x.wmtid == id).ToList().FirstOrDefault();

                    //    if ((db.d_bobbin_lct_hist.Where(x => x.bb_no == bb_no).Count() == 0) && !(string.IsNullOrEmpty(bb_no)))
                    //    {
                    //        var list_data_bobin = db.d_bobbin_info.Where(x => x.bb_no == bb_no).ToList().FirstOrDefault();

                    //        var history = new d_bobbin_lct_hist();

                    //        history.bb_no = list_data.bb_no;
                    //        history.bb_nm = list_data_bobin.bb_nm;
                    //        history.mc_type = list_data_bobin.mc_type;
                    //        history.mt_cd = list_data.mt_cd;
                    //        history.use_yn = "Y";
                    //        history.del_yn = "N";
                    //        history.chg_dt = DateTime.Now;
                    //        history.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    //        history.reg_dt = DateTime.Now;
                    //        history.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    //        db.Entry(history).State = EntityState.Added;
                    //    }



                
                   
                }
                return Json(new { result = false, message = Constant.CannotRedo }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetInventory(string mtCode, string mtNo, string sVBobbinCd, string recDate, int view, string mtNoSpecific, string prd_cd, string recDateStart, string recDateEnd)
        {
            StringBuilder sql = new StringBuilder($"Call spTIMS_InventoryGeneral_Union('{mtCode}','{mtNo}','{sVBobbinCd}','{recDate}','{prd_cd}');");
            List<TIMSInventoryModel> listTotal = new InitMethods().ConvertDataTable<TIMSInventoryModel>(sql);
            List<TIMSInventoryModel> listInfo = new List<TIMSInventoryModel>();
            List<TIMSInventoryModel> listDetailAll = new List<TIMSInventoryModel>();
            List<TIMSInventoryModel> listDetailSpecific = new List<TIMSInventoryModel>();
            string mtNoTemp = "";
            foreach (var item in listTotal)
            {
                if (string.IsNullOrEmpty(item.MaterialCode))
                {
                    listInfo.Add(item);
                }
                else
                {
                    listDetailAll.Add(item);
                }
            }
            if (view == 1 && string.IsNullOrEmpty(mtCode))
            {
                return Json(new { listInfo, mtNoTemp }, JsonRequestBehavior.AllowGet);
            }

            if (view == 1 && (!string.IsNullOrEmpty(mtCode)))
            {
                foreach (var item in listDetailAll)
                {
                    if (item.MaterialCode.Contains(mtCode))
                    {
                        listDetailSpecific.Add(item);
                    }
                }
                if (listDetailSpecific.Count > 0)
                {
                    mtNoTemp = listDetailSpecific[0].MaterialNo;
                }
                return Json(new { listInfo, mtNoTemp }, JsonRequestBehavior.AllowGet);
            }

            //if view = 2
            foreach (var item in listDetailAll)
            {
                if (item.MaterialNo == mtNoSpecific)
                {
                    listDetailSpecific.Add(item);
                }
            }
            return Json(listDetailSpecific, JsonRequestBehavior.AllowGet);
        }

        public JsonResult getInventoryInfo(string mtCode, string sVBobbinCd, string prd_cd, string recDateStart, string recDateEnd, string mtNoSpecific, string status, string po, string productName, string model)
        {
            var listInfo = _TIMSService.GetInventoryInfoGeneral(mtCode, sVBobbinCd, prd_cd, recDateStart,
                recDateEnd, mtNoSpecific, status, po, productName, model);
            return Json(new { listInfo = listInfo }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetInventoryDetail(string prd_cd, string mtCode, string sVBobbinCd, string recDateStart, string recDateEnd, string ProSpecific, string status, string po)
        {
            if (status.Contains("002") && !status.Contains("008"))
            {
                var giatri = " not in ";
                if (status.Contains("HCOQC"))
                {
                    giatri = " in ";
                }
                var sql = new StringBuilder();
                sql.Append(" SELECT b.at_no,	b.Id, b.WMaterialCode AS MaterialCode, b.WMaterialNo AS MaterialNo, b.WMaterialLength AS `Length`, b.WMaterialSize AS `Size`, b.WMaterialQty AS `Qty`, b.WMaterialUnit AS `Unit`, b.WMaterialStatusCode AS StatusCode, b.WMaterialStatusName AS StatusName, b.WMaterialReceivedDate AS ReceivedDate,b.VBobbinCd, b.product_cd,b.buyer_qr ")
                         .Append("FROM viewtimsinventorygeneral AS b")
                         .Append(" WHERE b.product_cd= '" + ProSpecific + "'  ")
                          .Append(" and b.id_actual " + giatri + " (SELECT MAX(a.id_actual) FROM w_actual AS a WHERE a.`type` = 'TIMS'  AND a.name != 'OQC'   ")
                         .Append("   and a.at_no in  (SELECT at_no FROM w_actual_primary  WHERE product = '" + ProSpecific + "' ) GROUP BY a.at_no)  ")
                         .Append(" AND ('" + mtCode + "'='' or b.WMaterialCode LIKE CONCAT('%','" + mtCode + "','%')) ")
                         .Append(" 	AND ('" + sVBobbinCd + "'='' or b.VBobbinCd LIKE CONCAT('%','" + sVBobbinCd + "','%')) ")
                         .Append(" 	AND ('" + po + "'='' OR b.at_no LIKE CONCAT('%','" + po + "','%')) ")
                         .Append("	AND ('" + ProSpecific + "'='' OR b.product_cd LIKE CONCAT('%','" + ProSpecific + "','%')) ")
                         .Append(" AND ('" + recDateStart + "'='' OR DATE_FORMAT(b.WMaterialReceivedDate,'%Y/%m/%d') >= DATE_FORMAT('" + recDateStart + "','%Y/%m/%d')) ")
                         .Append(" 	AND ('" + recDateEnd + "'='' OR DATE_FORMAT(b.WMaterialReceivedDate,'%Y/%m/%d') <= DATE_FORMAT('" + recDateEnd + "','%Y/%m/%d')) ")
                         .Append(" 	and '" + status + "' like concat ('%',b.WMaterialStatusCode,'%') ")

                         .Append(" 	ORDER BY b.at_no asc ");

                var data = new InitMethods().ConvertDataTableToJsonAndReturn(sql);
                return Json(data.Data, JsonRequestBehavior.AllowGet);
            }

            else
            {
                StringBuilder sql = new StringBuilder($"Call spTIMS_InventoryGeneralDetail('{ProSpecific}','{prd_cd}','{mtCode}','{sVBobbinCd}','{recDateStart}','{recDateEnd}','{status}','{po}');");
                List<TIMSInventoryModel> listDetailSpecific = new InitMethods().ConvertDataTable<TIMSInventoryModel>(sql);
                return Json(listDetailSpecific, JsonRequestBehavior.AllowGet);
            }

        }

        public ActionResult PrintExcelFile(string mtCode,string recDateStart, string recDateEnd, string productCode, string productName, string model, string at_no, string status)
        {
            StringBuilder sql = new StringBuilder($"Call spTIMS_InventoryGeneral_Union('{at_no}','{model}','{productCode}','{productName}','{mtCode}','{recDateStart}','{recDateEnd}','{status}');");
            List<TIMSInventoryModelExcel> listTotal = new InitMethods().ConvertDataTable<TIMSInventoryModelExcel>(sql);
            List<TIMSInventoryModelExcel> listOrderBy = new List<TIMSInventoryModelExcel>();
            listOrderBy = listTotal.OrderBy(x => x.product_cd).ThenBy(x => x.mt_cd).ToList();

            DataTable dt = new InitMethods().ConvertListToDataTable(listOrderBy);
            DataSet ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables[0].TableName = "TIMS_Inventory_General";

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
            return View("~/Views/TIMS/Inventory/General.cshtml");
        }
        public ActionResult Search_bbTimsGeneral(string bb_no)
        {
            try
            {
                if (string.IsNullOrEmpty(bb_no))
                {

                    return Json(new { result = false, message = "Vui lòng SCan lại" }, JsonRequestBehavior.AllowGet);
                }
                var kttt = db.w_material_info.Where(x => x.bb_no == bb_no).ToList();
                if (kttt.Count == 0)
                {
                    return Json(new { result = false, message = "Không tìm thấy mã Container" }, JsonRequestBehavior.AllowGet);
                }
                var kt_tims = kttt.Where(x => x.bb_no == bb_no && x.lct_cd.StartsWith("006")).ToList();
                if (kt_tims.Count == 0)//ko co trong phong tims
                {
                    var ktStatus = kttt.Where(x => x.bb_no == bb_no).FirstOrDefault();
                    var lctName = db.lct_info.Where(x => x.lct_cd == ktStatus.lct_cd).FirstOrDefault().lct_nm;
                    var StatusName = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == ktStatus.mt_sts_cd).FirstOrDefault().dt_nm;

                    return Json(new { result = false, message = "Mã này đang ở trong kho" + lctName + "Trạng thái là:" + StatusName }, JsonRequestBehavior.AllowGet);
                }
                var data = kt_tims.Where(x => x.bb_no == bb_no).FirstOrDefault();
                return Json(new { result = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {

                return Json(new { result = false, message = "Không tồn tại" }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion General

        #region PRINT NG (TIMS)

        public ActionResult Print_NG()
        {
            return SetLanguage("~/Views/TIMS/Actual/PrintNG/PrintNG.cshtml");

        }

        public JsonResult GetTIMSMaterialNG(Pageing paging, string code, string at_no, string product)
        {
            StringBuilder sql = new StringBuilder($"CALL spTIMS_GetTIMSMaterialNG('{code}','{at_no}','{product}');");
            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);
            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<String>("buyer_qr"));
            return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
        }

        public JsonResult GetTIMSMaterialOK(Pageing paging, string code, string name, string process, string workerId)
        {
            StringBuilder varname1 = new StringBuilder();
            varname1.Append("SELECT * \n");
            varname1.Append("FROM w_material_info AS a \n");
            varname1.Append("WHERE `a`.`mt_sts_cd` = '012'  AND `a`.`lct_cd` LIKE '006%'");
            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);
            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("wmtid"));
            return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
        }

        public JsonResult Change_OK_NG(string mt_cd, double gr_qty, string reason, string buyer_qr)
        {
            try
            {
                var type = "CP";
                var find_ng = db.w_material_info.Where(x => x.mt_cd == mt_cd && x.mt_sts_cd == "003").SingleOrDefault();
                if (!String.IsNullOrEmpty(buyer_qr))
                {
                    find_ng = db.w_material_info.Where(x => x.buyer_qr == buyer_qr && x.mt_sts_cd == "003").SingleOrDefault();
                    type = "BR";
                }
                if (find_ng == null)
                {
                    return Json(new { result = false, message = " NG" + Constant.NotFound }, JsonRequestBehavior.AllowGet);
                }
                if (find_ng.gr_qty < gr_qty)
                {
                    return Json(new { result = false, message = Constant.QuantityNGInvalid }, JsonRequestBehavior.AllowGet);
                }
                //trừ mã cũ

                //insert bảng giảm có lí do
                var w_material_down = new w_material_down();
                w_material_down.gr_down = find_ng.gr_qty - gr_qty;
                w_material_down.mt_cd = find_ng.mt_cd;
                w_material_down.reason = reason;
                w_material_down.bb_no = find_ng.bb_no;
                w_material_down.sts_now = find_ng.mt_sts_cd;
                w_material_down.gr_qty = find_ng.gr_qty;
                w_material_down.chg_dt = DateTime.Now;
                w_material_down.reg_dt = DateTime.Now;
                w_material_down.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                w_material_down.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                db.Entry(w_material_down).State = EntityState.Added;
                db.SaveChanges(); // line that threw exception
                find_ng.gr_qty = find_ng.gr_qty - gr_qty;
                find_ng.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                if (ModelState.IsValid)
                {
                    db.Entry(find_ng).State = EntityState.Modified;
                    db.SaveChanges();
                    var ok = new w_material_info();
                    if (type == "CP")
                    {
                        //tạo mã ok mới dựa trên mã ng
                        var check = db.w_material_info.Where(x => x.mt_cd.StartsWith(find_ng.mt_cd + "-OK")).ToList();
                        var dem = (check.Count > 0 ? check.Count().ToString() : "");
                        ok.mt_cd = find_ng.mt_cd + "-OK" + dem;
                    }
                    else
                    {
                        // tao ra ma moi
                        var buyer_new = find_ng.buyer_qr + "-BROK";
                        var danhsach = db.w_material_info.Where(x => x.mt_cd.StartsWith(buyer_new)).ToList();
                        buyer_new = danhsach.Count() == 0 ? buyer_new : buyer_new + danhsach.Count();
                        ok.mt_cd = buyer_new;
                    }
                    ok.id_actual = find_ng.id_actual;
                    ok.orgin_mt_cd = find_ng.mt_cd;
                    ok.gr_qty = gr_qty;
                    ok.real_qty = ok.gr_qty;
                    ok.mt_barcode = ok.mt_cd;
                    ok.mt_qrcode = ok.mt_cd;
                    ok.lct_cd = find_ng.lct_cd;
                    ok.mt_type = find_ng.mt_type;
                    ok.mt_no = find_ng.mt_no;
                    ok.mt_sts_cd = "012";
                    ok.use_yn = "Y";
                    ok.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    ok.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    ok.chg_dt = DateTime.Now;
                    ok.reg_dt = DateTime.Now;
                    db.Entry(ok).State = EntityState.Added;
                    db.SaveChanges();
                    Log.Info("TIMS=>Change_OK_NG add mt_cd ok: " + ok.mt_cd + " SLok: " + ok.gr_qty + "NG: " + find_ng.mt_cd + " SLok: " + find_ng.gr_qty);
                    return Json(new { result = true, kq = ok }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult Cancel_NG_OK(string mt_cd)
        {
            try
            {
                //check mã tồn tại không
                var find_lot = db.w_material_info.Where(x => x.mt_cd == mt_cd).SingleOrDefault();
                if (find_lot == null)
                {
                    return Json(new { result = false, message = "LOT " + Constant.NotFound }, JsonRequestBehavior.AllowGet);
                }
                if (find_lot.mt_sts_cd != "012")
                {
                    return Json(new { result = false, message = Constant.Status }, JsonRequestBehavior.AllowGet);
                }
                //kiếm con NG cũ
                var find_lotng = db.w_material_info.Where(x => x.mt_cd == (find_lot.orgin_mt_cd)).SingleOrDefault();
                if (find_lot == null)
                {
                    return Json(new { result = false, message = "LOT " + Constant.NotFound }, JsonRequestBehavior.AllowGet);
                }
                //updtae lại số lượng ng
                find_lotng.gr_qty = find_lotng.gr_qty + find_lot.gr_qty;
                find_lotng.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                db.Entry(find_lotng).State = EntityState.Modified;
                db.SaveChanges();
                Log.Info("TIMS=>Change_OK_NG THAY DOI mt_cd : " + find_lotng.mt_cd + " SL: " + find_lotng.gr_qty);

                //xóa mã ok
                db.Entry(find_lot).State = EntityState.Deleted;
                db.SaveChanges();
                return Json(new { result = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = Constant.ErrorSystem }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult PrintMaterialNG()
        {
            string[] keys = Request.Form.AllKeys;
            StringBuilder listValues = new StringBuilder();

            for (int i = 0; i < keys.Length; i++)
            {
                listValues.Append(Request.Form[keys[i]] + ",");
            }

            ViewData["Message"] = listValues.ToString();
            return View("~/Views/TIMS/Actual/PrintNG/QRPrintNG.cshtml");
        }

        public JsonResult QRMaterialNGPrint(string id)
        {
            if (id != "")
            {
                StringBuilder varname1 = new StringBuilder();
                varname1.Append("( \n");
                varname1.Append("SELECT `a`.`wmtid` AS `Id`,`a`.`mt_cd` AS `WMaterialCode`,`a`.`mt_no` AS `WMaterialNumber`,`b`.`mt_nm` AS `WMaterialName`,`a`.`mt_type` AS `WMaterialType`,`a`.`alert_NG` AS `WAlertNG`,`a`.`mt_sts_cd` AS `WMaterialStatusCode`,`a`.`gr_qty` AS `WMaterialGrQty`, DATE_FORMAT(`a`.`reg_dt`,'%Y-%m-%d') AS `CreatedDate`,`b`.`spec` AS `DMaterialSpec`,`b`.`spec_unit` AS `DMaterialSpecUnit`, IFNULL(`b`.`bundle_unit`,'EA') AS `DMaterialBundleUnit`,`d`.`product` AS `Product`,`c`.`name` AS `Process`, \n");
                varname1.Append(" \n");
                varname1.Append("(a.staff_id) AS `WorkerId`,( \n");
                varname1.Append("SELECT `mb_info`.`uname` \n");
                varname1.Append("FROM `mb_info` \n");
                varname1.Append("WHERE `mb_info`.`userid` = a.staff_id) AS `WorkerName`,( \n");
                varname1.Append("SELECT CASE WHEN `b`.`bundle_unit` = 'Roll' THEN CASE WHEN (`a`.`gr_qty` IS NULL OR `b`.`spec` IS NULL) THEN 0 ELSE `a`.`gr_qty` / `b`.`spec` END ELSE IFNULL(`a`.`gr_qty`,0) END) AS `Qty` \n");
                varname1.Append("FROM (((`w_material_info` `a` \n");
                varname1.Append("LEFT JOIN `d_material_info` `b` ON(`b`.`mt_no` = `a`.`mt_no`)) \n"); ;
                varname1.Append("LEFT JOIN `w_actual` `c` ON(`a`.`id_actual` = `c`.`id_actual`)) \n");
                varname1.Append("LEFT JOIN `w_actual_primary` `d` ON(`c`.`at_no` = `d`.`at_no`)) \n");
                varname1.Append("WHERE `a`.`mt_sts_cd` IN ('003','012') AND `a`.`wmtid` IN (" + id + ")  AND `a`.`lct_cd` LIKE '006%')");
                return new InitMethods().ConvertDataTableToJsonAndReturn(varname1);
            }
            return null;
        }

        public JsonResult CountingTIMSMaterialNG()
        {
            StringBuilder sql = new StringBuilder($"SELECT COUNT(wmtid) as Total FROM w_material_info where alert_NG = '1' and mt_sts_cd = '003' and lct_cd like '006%';");
            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);
            List<string> list = new List<string>(dt.Rows.Count);
            foreach (DataRow row in dt.Rows)
                list.Add(row["Total"].ToString());
            string total = list.Select(x => x).FirstOrDefault();
            return Json(int.Parse(total), JsonRequestBehavior.AllowGet);
        }
        public ActionResult InsertQcPhanLoai(List<Insert_MPO_QC_Model> model, string at_no, string ml_tims, string date_ymd, int gr_qty, string item_vcd, m_facline_qc MFQC, m_facline_qc_value MFQCV)
        {
            #region insert info

            var check = db.m_facline_qc.SingleOrDefault(x => x.ml_tims == ml_tims && x.fq_no.StartsWith("TI"));
            DateTime reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
            String dateString = reg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
            DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
            String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
            var prd_cd = "";
            var ca = "";
            //lấy product

            var ktprd_cd = db.w_actual_primary.Where(x => x.at_no == at_no).FirstOrDefault();
            if (ktprd_cd != null)
            {
                prd_cd = ktprd_cd.product;
            }
            // LẤY sHILF

            if (ml_tims.Contains("-CN"))
            {
                ca = "CN";
            }
            if (ml_tims.Contains("-CD"))
            {
                ca = "CD";
            }

            if (check == null)
            {
                var list = db.m_facline_qc.Where(x => x.fq_no.StartsWith("TI")).OrderBy(x => x.fq_no).ToList();
                if (list.Count == 0)
                {
                    MFQC.fq_no = "TI000000001";    
                }
                else
                {
                    var menuCd = string.Empty;
                    var subMenuIdConvert = 0;
                    var list1 = list.LastOrDefault();

                    var bien1 = list1.fq_no;
                    var subMenuId = bien1.Substring(bien1.Length - 9, 9);
                    int.TryParse(subMenuId, out subMenuIdConvert);
                    menuCd = "TI" + string.Format("{0}{1}", menuCd, CreateFQ((subMenuIdConvert + 1)));
                    MFQC.fq_no = menuCd;
                }
               

                MFQC.check_qty = gr_qty;
                MFQC.ml_tims = ml_tims;
                MFQC.at_no = at_no;
                MFQC.product_cd = prd_cd;
                MFQC.shift = ca;
                MFQC.work_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                MFQC.reg_dt = reg_dt;
                MFQC.chg_dt = chg_dt;
                var item = db.qc_item_mt.FirstOrDefault(x => x.item_vcd == item_vcd);
                MFQC.item_nm = item.item_nm;
                MFQC.item_exp = item.item_exp;
                if (ModelState.IsValid)
                {
                    MFQC.reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    MFQC.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    db.Entry(MFQC).State = EntityState.Added;
                    list.Add(MFQC);
                    db.SaveChanges();

                }
                if (model != null)
                {
                    foreach (var item2 in model)
                    {
                        if (item2.objTr != null)
                        {
                            foreach (var item3 in item2.objTr)
                            {
                                if (item3.input > 0)
                                {
                                    var list_qc_itemcheck_dt = db.qc_itemcheck_dt.Find(Convert.ToInt32(item3.icdno));

                                    MFQCV.fq_no = MFQC.fq_no;
                                    MFQCV.check_value = list_qc_itemcheck_dt.check_name;
                                    MFQCV.check_cd = list_qc_itemcheck_dt.check_cd;
                                    MFQCV.check_qty = Convert.ToInt32(item3.input);
                                    MFQCV.check_id = list_qc_itemcheck_dt.check_id;
                                    MFQCV.date_ymd = date_ymd;
                                    MFQCV.reg_dt = reg_dt;
                                    MFQCV.chg_dt = chg_dt;

                                    MFQCV.reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                                    MFQCV.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                                    _TIMSService.InsertFaclineQCValue(MFQCV, prd_cd, ca, at_no);
                                    //db.Entry(MFQCV).State = EntityState.Added;
                                    //db.SaveChanges();
                                }
                            }
                        }
                    }


                    return Json(new { result = true, MFQC }, JsonRequestBehavior.AllowGet);
                }
            }

            else
            {
                //insert m_facline_qc_value
                if (model != null)
                {
                    foreach (var item2 in model)
                    {
                        if (item2.objTr != null)
                        {
                            foreach (var item3 in item2.objTr)
                            {
                                if (item3.input > 0)
                                {
                                    var list_qc_itemcheck_dt = db.qc_itemcheck_dt.Find(Convert.ToInt32(item3.icdno));

                                    MFQCV.fq_no = check.fq_no;
                                    MFQCV.check_value = list_qc_itemcheck_dt.check_name;
                                    MFQCV.check_cd = list_qc_itemcheck_dt.check_cd;
                                    MFQCV.check_qty = Convert.ToInt32(item3.input);
                                    MFQCV.check_id = list_qc_itemcheck_dt.check_id;
                                    MFQCV.date_ymd = date_ymd;
                                    MFQCV.reg_dt = reg_dt;
                                    MFQCV.chg_dt = chg_dt;

                                    MFQCV.reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                                    MFQCV.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                                    check.reg_dt = DateTime.Now;

                                    //db.Entry(MFQCV).State = EntityState.Added;
                                    _TIMSService.InsertFaclineQCValue(MFQCV, prd_cd, ca, at_no);



                                    db.Entry(check).State = EntityState.Modified;
                                    db.SaveChanges();
                                }
                            }
                        }
                    }

                    //update update check_Qty [m_facline_qc]
                    var soluongdefect = (db.m_facline_qc_value.Where(x => x.fq_no == MFQCV.fq_no).ToList().Sum(x => x.check_qty));

                    var isExist_QC = db.m_facline_qc.Where(x => x.fq_no == MFQCV.fq_no).SingleOrDefault();
                    isExist_QC.check_qty = soluongdefect;
                    isExist_QC.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    db.Entry(isExist_QC).State = EntityState.Modified;

                    db.SaveChanges();

                    return Json(new { result = true, MFQC }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { result = false, message = "Lỗi hệ thống" }, JsonRequestBehavior.AllowGet);

            #endregion insert info
        }


        public ActionResult Getfacline_qc_value(string fq_no)
        {
            var list = (from p in db.m_facline_qc_value
                        join b in db.qc_itemcheck_mt
                        on p.check_id equals b.check_id
                        where p.fq_no == fq_no
                        select new
                        {
                            fqhno = p.fqhno,
                            check_subject = b.check_subject,
                            check_value = p.check_value,
                            check_qty = p.check_qty,
                            date_ymd = p.date_ymd,
                        }).ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetfaclineQCValue(string ProductCode, string datetime, string shift)
        {
            IEnumerable<MFaclineQCValue> data = _TIMSService.GetFaclineQCValue(ProductCode, datetime, shift);
            var jsonReturn = new
            {
                rows = data
            };
            return Json(jsonReturn, JsonRequestBehavior.AllowGet);
        }
        #endregion PRINT NG (TIMS)

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

        public class qc_itemcheck_mt_Model
        {
            public int qc_itemcheck_mt__icno { get; set; }
            public string qc_itemcheck_mt__check_subject { get; set; }
            public string qc_itemcheck_mt__check_id { get; set; }
            public List<view_qc_Model> view_qc_Model { get; set; }
        }

        public class view_qc_Model
        {
            public int qc_itemcheck_dt__icdno { get; set; }
            public string qc_itemcheck_dt__check_name { get; set; }
        }

        public class Insert_MPO_QC_Model
        {
            public int icno { get; set; }
            public List<objTr> objTr { get; set; }
        }

        public class objTr
        {
            public int icdno { get; set; }
            public int input { get; set; }
        }

        private string CreateFQ(int id)
        {
            if (id.ToString().Length < 2)
            {
                return string.Format("00000000{0}", id);
            }

            if ((id.ToString().Length < 3) || (id.ToString().Length == 2))
            {
                return string.Format("0000000{0}", id);
            }

            if ((id.ToString().Length < 4) || (id.ToString().Length == 3))
            {
                return string.Format("000000{0}", id);
            }
            if ((id.ToString().Length < 5) || (id.ToString().Length == 4))
            {
                return string.Format("00000{0}", id);
            }
            if ((id.ToString().Length < 6) || (id.ToString().Length == 5))
            {
                return string.Format("0000{0}", id);
            }
            if ((id.ToString().Length < 7) || (id.ToString().Length == 6))
            {
                return string.Format("000{0}", id);
            }
            if ((id.ToString().Length < 8) || (id.ToString().Length == 7))
            {
                return string.Format("00{0}", id);
            }
            if ((id.ToString().Length < 9) || (id.ToString().Length == 8))
            {
                return string.Format("0{0}", id);
            }
            if ((id.ToString().Length < 10) || (id.ToString().Length == 9))
            {
                return string.Format("{0}", id);
            }
            return string.Empty;
        }

        public string autobarcode(int id)
        {
            if (id.ToString().Length < 2)
            {
                return string.Format("00000{0}", id);
            }

            if ((id.ToString().Length < 3) || (id.ToString().Length == 2))
            {
                return string.Format("0000{0}", id);
            }

            if ((id.ToString().Length < 4) || (id.ToString().Length == 3))
            {
                return string.Format("000{0}", id);
            }

            if ((id.ToString().Length < 5) || (id.ToString().Length == 4))
            {
                return string.Format("00{0}", id);
            }
            if ((id.ToString().Length < 6) || (id.ToString().Length == 5))
            {
                return string.Format("0{0}", id);
            }
            if ((id.ToString().Length < 7) || (id.ToString().Length == 6))
            {
                return string.Format("{0}", id);
            }
            return string.Empty;
        }

        #endregion Sử dụng chung

        #region TIMS_ATM

        public ActionResult TIMS_ATM()
        {
            return View();
        }

        public ActionResult ATM()
        {
            return SetLanguage("~/Views/TIMS/ATM/ATM.cshtml");

        }
        public ActionResult EffATM()
        {
            return SetLanguage("~/Views/TIMS/ATM/EffATM.cshtml");
        }
        public ActionResult searchEffATM(Pageing paging)
        {
            string ProductCode = Request["SearchProductCode"] == null ? "" : Request["SearchProductCode"].Trim();
            string ProductName = Request["SearchProductName"] == null ? "" : Request["SearchProductName"].Trim();
            string PO = Request["SearchPO"] == null ? "" : Request["SearchPO"].Trim();
            string Remark = Request["SearchRemark"] == null ? "" : Request["SearchRemark"].Trim();

            StringBuilder varname1 = new StringBuilder();
            varname1.Append($"CALL HieuSuatATM('{ProductCode}','{ProductName}','{PO}','{Remark}')");

            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<string>("at_no"));
            return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);

        }
        public ActionResult ExportsearchEffATM()
        {
            string ProductCode = Request["SearchProductCode"] == null ? "" : Request["SearchProductCode"].Trim();
            string ProductName = Request["SearchProductName"] == null ? "" : Request["SearchProductName"].Trim();
            string PO = Request["SearchPO"] == null ? "" : Request["SearchPO"].Trim();
            string Remark = Request["SearchRemark"] == null ? "" : Request["SearchRemark"].Trim();

            StringBuilder varname1 = new StringBuilder();
            varname1.Append($"CALL HieuSuatATM('{ProductCode}','{ProductName}','{PO}','{Remark}')");

            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);



            DataSet ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables[0].TableName = "HieusuatATM";

            using (XLWorkbook wb = new XLWorkbook())
            {
                var ws = wb.AddWorksheet(dt);

                ws.Columns().AdjustToContents();
                ws.Rows().AdjustToContents();



                ws.Columns().AdjustToContents();
                ws.Rows().AdjustToContents();

                ws.Cells("A1").Value = "Tuần";
                ws.Cells("B1").Value = "PO NO";
                ws.Cells("C1").Value = "Product Code";
                ws.Cells("D1").Value = "Product Name";
                ws.Cells("E1").Value = "Sản lượng LT";
                ws.Cells("F1").Value = "Số(M) LT";
                ws.Cells("G1").Value = "Mã Nguyên vật liệu";
                ws.Cells("H1").Value = "%Loss";
                ws.Cells("I1").Value = "Hiệu suất sản xuất(%)";
                ws.Cells("J1").Value = "OK SẢN XUẤT";
                ws.Cells("K1").Value = "NG SẢN XUẤT";
                ws.Cells("L1").Value = "Hiệu suất OQC(%)";
                ws.Cells("M1").Value = "OK TP";
                ws.Cells("N1").Value = "NG TP";
                ws.Cells("O1").Value = "Hàng chờ kiểm";


                wb.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                wb.Style.Alignment.ShrinkToFit = true;
                wb.Style.Font.Bold = true;

                Response.Clear();
                Response.Buffer = true;
                Response.Charset = "";
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;filename=HieuSuatATM.xlsx");
                using (MemoryStream MyMemoryStream = new MemoryStream())
                {
                    wb.SaveAs(MyMemoryStream);
                    MyMemoryStream.WriteTo(Response.OutputStream);
                    Response.Flush();
                    Response.End();
                }
            }
            return View();


        }
        public ActionResult getDataAtm()
        {
            var list = (from a in db.w_actual
                        join b in db.w_actual_primary
                        on a.at_no equals b.at_no
                        where a.type.Equals("TIMS")

                        select new
                        {
                            type = b.type,
                            id_actual = a.id_actual,
                            at_no = a.at_no,
                            target = b.target,
                            name = a.name,
                            reg_dt = a.reg_dt,
                            product = b.product
                        }).ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public ActionResult searchAtm(Pageing paging, bool type)
        {
            string product_id = Request["product"] == null ? "" : Request["product"].Trim();
            string name_id = Request["name"] == null ? "" : Request["name"].Trim();
            string at_id = Request["at_no"] == null ? "" : Request["at_no"].Trim();
            string reg_dt = Request["reg_dt"] == null ? "" : Request["reg_dt"].Trim();

            string reg_dt_end = Request["reg_dt_end"] == null ? "" : Request["reg_dt_end"].Trim();

            var dateConvert = new DateTime();
            if (DateTime.TryParse(reg_dt, out dateConvert))
            {
                reg_dt = dateConvert.ToString("yyyy/MM/dd");
            }

            if (DateTime.TryParse(reg_dt_end, out dateConvert))
            {
                reg_dt_end = dateConvert.ToString("yyyy/MM/dd");
            }
            StringBuilder varname1 = new StringBuilder();
            if (type == true)
            {
                varname1.Append(" SET sql_mode = '';SET @@sql_mode = ''; SELECT any_value(`v`.`id_actual`)id_actual, ");
                varname1.Append("  SUM(`v`.`m_lieu`)m_lieu,SUM(`v`.`actual`)actual,MAX(`v`.`product`)product, ");
                varname1.Append("  COUNT(`v`.`reg_dt`) AS `count_day`,MAX(`v`.`product_nm`) product_nm,");
                varname1.Append("  MIN(`v`.`reg_dt`)min_day,MAX(`v`.`reg_dt`)max_day,");
                varname1.Append("  ROUND((((SUM(`v`.`actual`) * any_value(`v`.`need_m`)) / SUM(`v`.`m_lieu`)) * 100),2) AS `HS` ");

                varname1.Append(" from (  ");
                varname1.Append(" SELECT b.id_actual,b.m_lieu,b.actual,b.product,b.reg_dt,b.product_nm,b.need_m from atm_tims as b  ");
                varname1.Append("where ('" + product_id + "'='' OR b.product LIKE '%" + product_id + "%') ");
                varname1.Append("AND ('" + name_id + "'='' OR b.product_nm LIKE '%" + name_id + "%') ");
                varname1.Append("AND ('" + at_id + "'='' OR b.at_no LIKE '%" + at_id + "%') ");
                varname1.Append("AND ('" + reg_dt + "'='' OR DATE_FORMAT(b.reg_dt,'%Y/%m/%d') >= DATE_FORMAT('" + reg_dt + "','%Y/%m/%d') )");
                varname1.Append("AND ('" + reg_dt_end + "'='' OR DATE_FORMAT(b.reg_dt,'%Y/%m/%d') <= DATE_FORMAT('" + reg_dt_end + "','%Y/%m/%d') ))v GROUP BY `v`.`product`");
                DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

                int total = dt.Rows.Count;
                var result = dt.AsEnumerable().OrderByDescending(x => x.Field<string>("product_nm"));
                return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);

            }
            else
            {
                
                varname1.Append("SET sql_mode = ''; SET @@sql_mode = ''; SELECT b.* FROM atm_tims AS b ");
                varname1.Append(" where ('" + product_id + "'='' OR b.product LIKE '%" + product_id + "%') ");
                varname1.Append("AND ('" + name_id + "'='' OR b.product_nm LIKE '%" + name_id + "%') ");
                varname1.Append("AND ('" + at_id + "'='' OR b.at_no LIKE '%" + at_id + "%') ");
                varname1.Append("AND ('" + reg_dt + "'='' OR DATE_FORMAT(b.reg_dt,'%Y/%m/%d') >= DATE_FORMAT('" + reg_dt + "','%Y/%m/%d') )");
                varname1.Append("AND ('" + reg_dt_end + "'='' OR DATE_FORMAT(b.reg_dt,'%Y/%m/%d') <= DATE_FORMAT('" + reg_dt_end + "','%Y/%m/%d') )   GROUP BY b.at_no  ");
                DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

                int total = dt.Rows.Count;
                var result = dt.AsEnumerable().OrderByDescending(x => x.Field<string>("at_no"));
                return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
            }

        }
        public ActionResult TIMSDaily()
        {
            string[] keys = Request.Form.AllKeys;
            if (keys.Length >= 5)
            {
                var value = Request.Form[keys[0]];
                var value2 = Request.Form[keys[1]];
                var value3 = Request.Form[keys[2]];
                var value4 = Request.Form[keys[3]];
                var value5 = Request.Form[keys[4]];
                ViewBag.at_no = (value == "undefined" ? "" : value);
                ViewBag.tieude = (value == "undefined" ? "" : value + "-") + value3;
                ViewBag.id_actual = value2;
                ViewBag.reg_dt = value4;
                ViewBag.reg_dt_end = value5;
            }
            else
            {
                ViewBag.Error = "Không có dữ liệu được gửi lên";
            }
            return View("~/Views/TIMS/ATM/TIMSDaily.cshtml");
        }

        public JsonResult getData(string at_no, string product, string reg_dt, string reg_dt_end)
        {
            try
            {

                var dateConvert = new DateTime();
                if (DateTime.TryParse(reg_dt, out dateConvert))
                {
                    reg_dt = dateConvert.ToString("yyyy/MM/dd");
                }

                if (DateTime.TryParse(reg_dt_end, out dateConvert))
                {
                    reg_dt_end = dateConvert.ToString("yyyy/MM/dd");
                }

                StringBuilder varname1 = new StringBuilder();
                var type = "";
                if (at_no == "")
                {
                    type = "product";
                    varname1.Append(" SET sql_mode = '';SET @@sql_mode = ''; SELECT any_value(`v`.`id_actual`)id_actual, ");
                    varname1.Append("  SUM(`v`.`m_lieu`)m_lieu,SUM(`v`.`actual`)actual,MAX(`v`.`product`)product, ");
                    varname1.Append("  COUNT(`v`.`reg_dt`) AS `count_day`,MAX(`v`.`product_nm`) product_nm,");
                    varname1.Append("  MIN(`v`.`reg_dt`)min_day,MAX(`v`.`reg_dt`)max_day,");
                    varname1.Append("  ROUND((((SUM(`v`.`actual`) * any_value(`v`.`need_m`)) / SUM(`v`.`m_lieu`)) * 100),2) AS `HS` ");

                    varname1.Append(" from (  ");
                    varname1.Append("SELECT b.id_actual,b.m_lieu,b.actual,b.product,b.reg_dt,b.product_nm,b.need_m from atm_tims as b  ");
                    varname1.Append("where ('" + product + "'='' OR b.product LIKE '%" + product + "%') ");
                    varname1.Append("AND ('" + reg_dt + "'='' OR DATE_FORMAT(b.reg_dt,'%Y/%m/%d') >= DATE_FORMAT('" + reg_dt + "','%Y/%m/%d') )");
                    varname1.Append("AND ('" + reg_dt_end + "'='' OR DATE_FORMAT(b.reg_dt,'%Y/%m/%d') <= DATE_FORMAT('" + reg_dt_end + "','%Y/%m/%d') ))v GROUP BY `v`.`product`");
                }
                else
                {
                    type = "at_no";

                    //kiểm tra po đã được chuyển sang w_material_info01 
                    var Checkdata = _TIMSService.CheckPOMove(at_no);
                    if (Checkdata)
                    {
                        varname1.Append(" SET sql_mode = '';SET @@sql_mode = ''; SELECT * from atm_tims01 as b  ");
                        varname1.Append("where ('" + at_no + "'='' OR b.at_no LIKE '%" + at_no + "%') ");
                        varname1.Append("AND ('" + reg_dt + "'='' OR DATE_FORMAT(b.reg_dt,'%Y/%m/%d') >= DATE_FORMAT('" + reg_dt + "','%Y/%m/%d') )");
                        varname1.Append("AND ('" + reg_dt_end + "'='' OR DATE_FORMAT(b.reg_dt,'%Y/%m/%d') <= DATE_FORMAT('" + reg_dt_end + "','%Y/%m/%d') )");
                    }
                    else
                    {
                        varname1.Append(" SET sql_mode = '';SET @@sql_mode = ''; SELECT * from atm_tims as b  ");
                        varname1.Append("where ('" + at_no + "'='' OR b.at_no LIKE '%" + at_no + "%') ");
                        varname1.Append("AND ('" + reg_dt + "'='' OR DATE_FORMAT(b.reg_dt,'%Y/%m/%d') >= DATE_FORMAT('" + reg_dt + "','%Y/%m/%d') )");
                        varname1.Append("AND ('" + reg_dt_end + "'='' OR DATE_FORMAT(b.reg_dt,'%Y/%m/%d') <= DATE_FORMAT('" + reg_dt_end + "','%Y/%m/%d') )");
                    }

                   
                }
                StringBuilder varname2 = new StringBuilder();
                varname2.Append("SELECT * from atm_mms as b  ");
                varname2.Append("where ('" + at_no + "'='' OR b.at_no LIKE '%" + at_no + "%') ");
                varname2.Append("AND ('" + reg_dt + "'='' OR DATE_FORMAT(b.reg_dt,'%Y/%m/%d') >= DATE_FORMAT('" + reg_dt + "','%Y/%m/%d') )");
                varname2.Append("AND ('" + reg_dt_end + "'='' OR DATE_FORMAT(b.reg_dt,'%Y/%m/%d') <= DATE_FORMAT('" + reg_dt_end + "','%Y/%m/%d') )");

                

                var data = new InitMethods().ConvertDataTableToJsonAndReturn(varname1);
                var data1 = new InitMethods().ConvertDataTableToJsonAndReturn(varname2);

                return Json(new { result = true, data = data.Data, data1 = data1.Data, type }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ Thống!" }, JsonRequestBehavior.AllowGet);
            }

        }
        public ActionResult getTMSData(int id, string at_no)
        {
            StringBuilder sql_insert = new StringBuilder();
            sql_insert.Append("SELECT(SELECT SUM(gr_qty) FROM w_material_info WHERE id_actual = '" + id + "') actual ");
            sql_insert.Append(",(SELECT target FROM w_actual_primary WHERE at_no =  '" + at_no + "' LIMIT 1) target, ");
            sql_insert.Append("a.at_no,a.name,a.date,a.defect, ");
            sql_insert.Append("(SELECT product FROM w_actual_primary WHERE at_no =  '" + at_no + "' LIMIT 1) product ");
            sql_insert.Append("FROM w_actual a ");
            sql_insert.Append("WHERE a.id_actual =  '" + id + "' ");

            var list2 = new InitMethods().ConvertDataTableToList<ModelTMSData>(sql_insert).ToList();
            return Json(list2.FirstOrDefault(), JsonRequestBehavior.AllowGet);
        }

        #endregion TIMS_ATM

        #region Return

        public ActionResult PrintQR_Tims()
        {
            return SetLanguage("~/Views/TIMS/PrintQR/PrintQR_Tims.cshtml");
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
            varname1.Append("SELECT a.mt_cd,b.mt_nm, ");
            varname1.Append("CONCAT(SUM(a.gr_qty),ifnull(b.unit_cd,'')) lenght, ");
            varname1.Append("CONCAT(ifnull(b.width,0),'*',ifnull(a.gr_qty,0)) AS size, ");
            varname1.Append("ifnull(b.spec,0) spec,a.mt_no, SUM((case when b.bundle_unit ='Roll' then  ROUND((a.gr_qty/b.spec),2) ELSE a.gr_qty END)) qty, ");
            varname1.Append("b.bundle_unit, a.return_date, ");
            varname1.Append("(SELECT dt_nm FROM comm_dt WHERE comm_dt.dt_cd=a.mt_sts_cd AND comm_dt.mt_cd='WHS005' LIMIT 1) sts_nm , ");
            varname1.Append(" (SELECT w_actual_primary.product FROM w_actual JOIN w_actual_primary ON w_actual.at_no=w_actual_primary.at_no WHERE  a.id_actual = w_actual.id_actual LIMIT 1) product, ");
            varname1.Append("(SELECT name FROM w_actual WHERE a.id_actual=w_actual.id_actual LIMIT 1) AS name ");
            varname1.Append("FROM w_material_info a ");
            varname1.Append("LEFT JOIN d_material_info  b ON a.mt_no=b.mt_no  ");
            varname1.Append("WHERE a.mt_sts_cd='004' AND a.sts_update='Return' and a.lct_cd LIKE '006%'  ");
            varname1.Append(" AND ('" + mt_no + "'='' OR  a.mt_no like '%" + mt_no + "%' ) ");
            varname1.Append(" AND ('" + return_date + "'='' OR DATE_FORMAT(a.return_date,'%Y/%m/%d') = DATE_FORMAT('" + return_date + "','%Y/%m/%d')) ");
            varname1.Append(" AND ('" + recevice_dt_start + "'='' OR DATE_FORMAT(a.recevice_dt,'%Y/%m/%d') >= DATE_FORMAT('" + recevice_dt_start + "','%Y/%m/%d')) ");
            varname1.Append(" AND ('" + recevice_dt_end + "'='' OR DATE_FORMAT(a.recevice_dt,'%Y/%m/%d') <= DATE_FORMAT('" + recevice_dt_end + "','%Y/%m/%d')) ");
            varname1.Append("GROUP BY a.mt_no");

            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<string>("mt_cd"));
            return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
        }

        public JsonResult getPrintQRDetail(Pageing paging)
        {
            int id = Convert.ToInt32(Request["wmtid_DT"] == null ? "" : Request["wmtid_DT"].Trim());
            var mt_no = db.w_material_info.FirstOrDefault(x => x.wmtid == id).mt_no;
            StringBuilder varname1 = new StringBuilder();
            varname1.Append("SELECT a.wmtid,a.mt_cd,b.mt_nm, ");
            varname1.Append("CONCAT(ifnull(a.gr_qty,''),ifnull(b.unit_cd,'')) lenght, ");
            varname1.Append("ifnull(a.gr_qty,'') lenght1, ");
            varname1.Append("CONCAT(ifnull(b.width,0),'*',ifnull(a.gr_qty,0)) AS size, ");
            varname1.Append("ifnull(b.spec,0) spec,a.mt_no,  CONCAT((case when b.bundle_unit ='Roll' then  (a.gr_qty/b.spec) ELSE a.gr_qty END),' ',ifnull(b.bundle_unit,'')) qty, ");
            varname1.Append(" b.bundle_unit, a.return_date, ");
            varname1.Append(" (SELECT dt_nm FROM comm_dt WHERE comm_dt.dt_cd=a.mt_sts_cd AND comm_dt.mt_cd='WHS005') sts_nm , ");
            varname1.Append(" (SELECT w_actual_primary.product FROM w_actual JOIN w_actual_primary ON w_actual.at_no=w_actual_primary.at_no WHERE  a.id_actual = w_actual.id_actual LIMIT 1) product, ");
            varname1.Append("(SELECT name FROM w_actual WHERE a.id_actual=w_actual.id_actual LIMIT 1) AS name ");
            varname1.Append(" FROM w_material_info a LEFT JOIN d_material_info  b ON a.mt_no=b.mt_no ");
            varname1.Append("WHERE a.mt_sts_cd='004' and a.lct_cd LIKE '006%' AND a.sts_update='Return' AND a.mt_no='" + mt_no + "' ");
            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("wmtid"));
            return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
        }

        public JsonResult update_lenght_qty(string wmtid, string lenght_qty, string id1)
        {
            try
            {
                var id = int.Parse(wmtid);

                var flag = db.w_material_info.Where(x => x.wmtid == id).Count();

                if (flag > 0)
                {
                    w_material_info db_w_material = db.w_material_info.Find(id);

                    var gr_qty_tmp = db_w_material.gr_qty;
                    DateTime dt = DateTime.Now;
                    db_w_material.gr_qty = int.Parse(lenght_qty);
                    db.Entry(db_w_material).State = EntityState.Modified;

                    db.SaveChanges();
                    var vaule1 = db.w_material_info.Where(x => x.mt_cd == db_w_material.orgin_mt_cd).ToList();
                    if (vaule1.Count() > 0)
                    {
                        foreach (var item in vaule1)
                        {
                            var vaule2 = vaule1.FirstOrDefault()?.gr_qty;

                            w_material_info db_w_material_rm = db.w_material_info.Find(item.wmtid);
                            db_w_material_rm.gr_qty = vaule2 - (int.Parse(lenght_qty) - gr_qty_tmp);
                            db.Entry(db_w_material_rm).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                    }
                    StringBuilder varname1 = new StringBuilder();
                    varname1.Append("SELECT a.wmtid,a.mt_cd,b.mt_nm, ");
                    varname1.Append("CONCAT(ifnull(a.gr_qty,''),ifnull(b.unit_cd,'')) lenght, ");
                    varname1.Append("ifnull(a.gr_qty,'') lenght1, ");
                    varname1.Append("CONCAT(ifnull(b.width,0),'*',ifnull(a.gr_qty,0)) AS size, ");
                    varname1.Append("ifnull(b.spec,0) spec,a.mt_no,  CONCAT((case when b.bundle_unit ='Roll' then  (a.gr_qty/b.spec) ELSE a.gr_qty END),' ',ifnull(b.bundle_unit,'')) qty, ");
                    varname1.Append("b.bundle_unit, a.return_date, ");
                    varname1.Append(" (SELECT dt_nm FROM comm_dt WHERE comm_dt.dt_cd=a.mt_sts_cd AND comm_dt.mt_cd='WHS005') sts_nm , ");
                    varname1.Append(" (SELECT w_actual_primary.product FROM w_actual JOIN w_actual_primary ON w_actual.at_no=w_actual_primary.at_no WHERE a.id_actual = w_actual.id_actual LIMIT 1) product,  ");
                    varname1.Append(" (SELECT name FROM w_actual WHERE a.id_actual=w_actual.id_actual LIMIT 1) AS name ");
                    varname1.Append(" FROM w_material_info a LEFT JOIN d_material_info b ON a.mt_no=b.mt_no ");
                    varname1.Append("WHERE a.mt_sts_cd='004' and a.lct_cd LIKE '006%' AND a.sts_update='Return' AND a.wmtid='" + wmtid + "' ");

                    var list2 = new InitMethods().ConvertDataTableToList<w_material_model>(varname1);

                    StringBuilder varname = new StringBuilder();
                    varname.Append("SELECT a.wmtid wmtid,a.mt_cd,b.mt_nm, ");
                    varname.Append("CONCAT(SUM(a.gr_qty),ifnull(b.unit_cd,'')) lenght, ");
                    varname.Append("CONCAT(ifnull(b.width,0),'*',ifnull(a.gr_qty,0)) AS size, ");
                    varname.Append("ifnull(b.spec,0) spec,a.mt_no, SUM((case when b.bundle_unit ='Roll' then  ROUND((a.gr_qty/b.spec),2) ELSE a.gr_qty END)) qty, ");
                    varname.Append("b.bundle_unit, a.return_date, ");
                    varname.Append("(SELECT dt_nm FROM comm_dt WHERE comm_dt.dt_cd=a.mt_sts_cd AND comm_dt.mt_cd='WHS005' LIMIT 1) sts_nm , ");
                    varname.Append(" (SELECT w_actual_primary.product FROM w_actual JOIN w_actual_primary ON w_actual.at_no=w_actual_primary.at_no WHERE  a.id_actual = w_actual.id_actual LIMIT 1) product, ");
                    varname.Append("(SELECT name FROM w_actual WHERE a.id_actual=w_actual.id_actual LIMIT 1) AS name ");
                    varname.Append("FROM w_material_info a ");
                    varname.Append("LEFT JOIN d_material_info  b ON a.mt_no=b.mt_no  ");
                    varname.Append("WHERE a.mt_sts_cd='004' AND a.sts_update='Return' and a.lct_cd LIKE '006%' AND a.wmtid='" + id1 + "' ");
                    varname.Append("GROUP BY a.mt_no");

                    var list1 = new InitMethods().ConvertDataTableToJsonAndReturn(varname);
                    return Json(new
                    {
                        result = true,
                        message = Constant.Success,
                        data = list2.ToList(),
                        data1 = list1.Data
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(
                      new
                      {
                          result = false,
                          message = Constant.ErrorSystem,
                      }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(
                        new
                        {
                            result = false,
                            message = Constant.ErrorSystem
                        }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult UpdateProcessIsFinish(int id_actual, bool IsFinished)
        {
            try
            {
              int update =   _TIMSService.UpdateActualIsFinish(id_actual, IsFinished);
                if (update>0)
                {
                    return Json(new { result = true, message = "Sửa thành công" }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message ="Sửa lỗi"}, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, e}, JsonRequestBehavior.AllowGet);
            }
        }    
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
                    db_w_material.mt_sts_cd = "008";
                    db.Entry(db_w_material).State = EntityState.Modified;

                    db.SaveChanges();
                }
                StringBuilder varname1 = new StringBuilder();
                varname1.Append("SELECT a.wmtid,a.mt_cd,b.mt_nm, ");
                varname1.Append("CONCAT(ifnull(a.gr_qty,''),ifnull(b.unit_cd,'')) lenght, ");
                varname1.Append("ifnull(a.gr_qty,'') lenght1, ");
                varname1.Append("CONCAT(ifnull(b.width,0),'*',ifnull(a.gr_qty,0)) AS size, ");
                varname1.Append("ifnull(b.spec,0) spec,a.mt_no,  CONCAT((case when b.bundle_unit ='Roll' then  (a.gr_qty/b.spec) ELSE a.gr_qty END),' ',ifnull(b.bundle_unit,'')) qty, ");
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
                    message = Constant.Success,
                    data = list2.ToList()
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(
                new
                {
                    result = false,
                    message = Constant.ErrorSystem
                }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult btnPrintQR()
        {
            string[] keys = Request.Form.AllKeys;

            var value = "";
            value = Request.Form[keys[0]];

            ViewData["Message"] = value;
            return View("~/Views/TIMS/PrintQR/BtnPrintQr.cshtml");
        }

        public ActionResult qrPrintQr(string mt_no)
        {
            if (mt_no != "")
            {
                StringBuilder varname1 = new StringBuilder();
                varname1.Append("SELECT a.wmtid,a.mt_cd,b.mt_nm, ");
                varname1.Append("CONCAT(ifnull(a.gr_qty,''),ifnull(b.unit_cd,'')) lenght, ");
                varname1.Append("ifnull(a.gr_qty,'') lenght1, ");
                varname1.Append("CONCAT(ifnull(b.width,0),'*',ifnull(a.gr_qty,0)) AS size, ");
                varname1.Append("ifnull(b.spec,0) spec,a.mt_no,  CONCAT((case when b.bundle_unit ='Roll' then  (a.gr_qty/b.spec) ELSE a.gr_qty END),' ',b.bundle_unit) qty, ");
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

        #endregion Return
        #region searchTemGoi


        public JsonResult GetdataTemGoi(Pageing pageing, string product, string printedDate,string shift)
        {


            var data = _IWOService.GetdataTemGoi(product, printedDate, shift);
            var records = data.Count();
            int totalPages = (int)Math.Ceiling((float)records / pageing.rows);
            var rowsData = data.Skip((pageing.page - 1)).Take(pageing.rows);
            return Json(new { total = totalPages, page = pageing.page, records = records, rows = rowsData }, JsonRequestBehavior.AllowGet);
        }

        #endregion
        #region ChangeTemGoi
        public ActionResult PartialView_ViewStatusTemGoiPopup()
        {
            return PartialView();
        }
        public ActionResult ViewStatusTemGoi(string buyerCode)
        {
            try
            {
                //kiểm tra trong bảng stamp_detail có tồn tại không
                var IsExist = _TIMSService.Getstampdetail(buyerCode);
                if (IsExist == null)
                {
                    return Json(new { result = false, message = "Tem gói này không tồn tại." }, JsonRequestBehavior.AllowGet);
                }
                var data = _TIMSService.ViewStatusTemGoi(buyerCode);
               
                if (data != null)
                {
                    //kiểm tra w_material_info(status = 010, lct = '006', buyer_qr: có)
                    if (data.mt_sts_cd.Equals("010") && data.lct_cd.Equals("006000000000000000"))
                    {
                        return Json(new { result = true, data, message = "" }, JsonRequestBehavior.AllowGet);
                    }
                    //kiểm tra mã tem gói đã được đóng thùng chưa
                    var isBoxMapping = _TIMSService.CheckTemGoimappingBox(buyerCode);
                    if (isBoxMapping != null)
                    {
                        return Json(new { result = false, message = "Tem gói này đã được cho vào thùng trong FG." }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        return Json(new { result = true, data, message = "" }, JsonRequestBehavior.AllowGet);
                    }
                }
               
                //kiểm tra xem đã đưa vào Kho FG chưa
               
                return Json(new { result = false, message = "Tem gói này chưa được mapping với Container" }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult ChangStamp(string stampOld, string stampNew, string ProductCode, int wmtid)
        {
            try
            {
                stampOld = stampOld.ToUpper();
                stampNew = stampNew.ToUpper();

                if (string.IsNullOrEmpty(ProductCode))
                {
                    return Json(new { result = false, message = "Product Rỗng" }, JsonRequestBehavior.AllowGet);
                }
                if (string.IsNullOrEmpty(stampOld))
                {
                    return Json(new { result = false, message = "Tem gói rỗng, vui lòng scan lại" }, JsonRequestBehavior.AllowGet);
                }
                if (string.IsNullOrEmpty(stampNew))
                {
                    return Json(new { result = false, message = "Tem gói thay thế rỗng, vui lòng scan lại" }, JsonRequestBehavior.AllowGet);
                }
                if (_TIMSService.GetQrcodeBuyer(stampNew) > 0)
                {
                    return Json(new { result = false, message = "Buyer Qr đã được sử dụng." }, JsonRequestBehavior.AllowGet);
                }
                // nếu tem thay thế chưa mapping với contanier nào thì cho thay thế
                // kiểm tra tem gói đã tồn tại trong stamp_detail. không có thì insert
                string trimmed = ProductCode.Replace(" ", "");
                // kiểm tra xem product theo quy tắc(0) hay bất quy tắc(1). có nghĩa  là nếu bất quy tắc thì không cần khác product vẫn cho mapping với tem gói
                var typeProduct = _TIMSService.ChecktypeProduct(trimmed);
                if (typeProduct.Equals("1"))
                {
                        //var WmaterialStampChange = _TIMSService.ViewStatusTemGoi(stampOld);
                        var item = _TIMSService.FindOneMaterialInfoById(wmtid);

                        //item.wmtid = WmaterialStampChange.wmtid;
                        item.buyer_qr = stampNew;
                        item.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                        item.chg_dt = DateTime.Now;
                        //item.reg_dt = WmaterialStampChange.reg_dt;

                        stamp_detail isExisted = _TIMSService.Getstampdetail(stampNew);
                        if (isExisted == null)
                        {
                            //ktra stamp_cd
                            var stamp_cd = "";

                            var ktra_stamp_cd = _TIMSService.GetStyleNo(ProductCode);

                            if (ktra_stamp_cd != null)
                            {
                                stamp_cd = ktra_stamp_cd.stamp_code;
                                if (String.IsNullOrEmpty(stamp_cd))
                                {
                                    return Json(new { result = false, message = "Vui Lòng Chọn kiểu tem cho Product(DMS)!!! " }, JsonRequestBehavior.AllowGet);
                                }
                            }

                            //lấy ra lot_date bằng cách trừ đi product và 8 kí tự sẽ tìm được mã lot_date
                            var prd = trimmed.Replace(" ", "");
                            var prd1 = trimmed.Replace("-", "");
                 
                            var timDZIH = stampNew.IndexOf("DZIH") + 4; //product+DZIH

                            //CEDMB2976DZIHA1N0L4E001010100
                            //5210000364CSDT1DZIHB2N0L9J049011100

                            //lot_date
                            //product = 5210000364CSDT1;
                            //vendor_code = DZIH
                            //vendor_line = B
                            //label_printer = 2
                            //is_sample = N
                            //PCN = 0
                            // lot_date = L9J
                            //serial_number = 049
                            //machine_line = 01
                            //shift = 1
                            // quantity = 100


                            var vendor_line = stampNew.Substring(timDZIH, 1);
                            var label_printer = stampNew.Substring(timDZIH + 1, 1);
                            var is_sample = stampNew.Substring(timDZIH + 2, 1);
                            var PCN = stampNew.Substring(timDZIH + 3, 1);
                            var date = stampNew.Substring(timDZIH + 4, 3);
                            var lot_date = DateFormatByShinsungRule(date);

                            var serial_number = stampNew.Substring(timDZIH + 7, 3); //gắn 001
                            var machine_line = stampNew.Substring(timDZIH + 10, 2); //gắn 01

                            var shift = stampNew.Substring(timDZIH + 12, 1);


                                // kiểm tra tem gói đã qua FG chưa nếu rồi thì update luôn
                                var isExist = _TIMSService.FindOneBuyerInfoById(stampOld);
                                if (isExist != null)
                                {
                                    if (isExist.sts_cd != "001")
                                    {
                                        return Json(new { result = false, message = "Tem gói này có thể đã đóng thùng hoặc đã giao hàng" }, JsonRequestBehavior.AllowGet);
                                    }

                                    isExist.buyer_qr = stampNew;

                                    isExist.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();

                                    _TIMSService.UpdateBuyerQRGeneral(isExist);
                                }
                                //insert stamp_detail
                        var stamp_detail = new stamp_detail()
                            {
                                buyer_qr = stampNew,
                                stamp_code = stamp_cd,
                                product_code = ProductCode,
                                vendor_code = "DZIH",
                                vendor_line = vendor_line,
                                label_printer = label_printer,
                                is_sample = is_sample,
                                pcn = PCN,
                                lot_date = lot_date,
                                serial_number = serial_number,
                                machine_line = machine_line,
                                shift = shift,
                                standard_qty = ktra_stamp_cd.pack_amt.HasValue ? ktra_stamp_cd.pack_amt.Value : 0,
                                reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString(),
                                chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString(),
                            };


                            _TIMSService.Insertstampdetail(stamp_detail);

                        }

                       
                        _IWOService.UpdateMaterialInfo(item);
                        return Json(new { result = true, message = Constant.Success }, JsonRequestBehavior.AllowGet);
                  
                }

                if (stampNew.StartsWith(trimmed.Replace("-", "")))
                {
                    //var WmaterialStampChange = _TIMSService.ViewStatusTemGoi(stampOld);

                    var item = _TIMSService.FindOneMaterialInfoById(wmtid);

                  
                    //item.wmtid = WmaterialStampChange.wmtid;
                    item.buyer_qr = stampNew;
                    item.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    item.chg_dt = DateTime.Now;
                    //item.reg_dt = WmaterialStampChange.reg_dt;

                    stamp_detail isExisted = _TIMSService.Getstampdetail(stampNew);
                    if (isExisted == null)
                    {
                        //ktra stamp_cd
                        var stamp_cd = "";

                        var ktra_stamp_cd = _TIMSService.GetStyleNo(ProductCode);

                        if (ktra_stamp_cd != null)
                        {
                            stamp_cd = ktra_stamp_cd.stamp_code;
                            if (String.IsNullOrEmpty(stamp_cd))
                            {
                                return Json(new { result = false, message = "Vui Lòng Chọn kiểu tem cho Product(DMS)!!! " }, JsonRequestBehavior.AllowGet);
                            }
                        }



                        //lấy ra lot_date bằng cách trừ đi product và 8 kí tự sẽ tìm được mã lot_date
                        var prd = trimmed.Replace(" ", "");
                        var prd1 = trimmed.Replace("-", "");
                        //var startBien = prd1.Length + 8;


                        ////CEDMB2976DZIHA1N0L4E001010100
                        ////lot_date
                        //var date = stampNew.Substring(startBien, 3);
                        //var lot_date = DateFormatByShinsungRule(date);

                        //var vendor_line = stampNew.Substring(prd1.Length + 4, 1);

                        //var is_sample = stampNew.Substring(prd1.Length + 6, 1);


                        //var shift = stampNew.Substring(stampNew.Length - 4, 1);
                        //var serial_number = stampNew.Substring(startBien + 3, 3); //gắn 001
                        //var machine_line = stampNew.Substring(startBien + 6, 2); //gắn 001
                        var timDZIH = stampNew.IndexOf("DZIH") + 4; //product+DZIH

                        //CEDMB2976DZIHA1N0L4E001010100
                        //5210000364CSDT1DZIHB2N0L9J049011100

                        //lot_date
                        //product = 5210000364CSDT1;
                        //vendor_code = DZIH
                        //vendor_line = B
                        //label_printer = 2
                        //is_sample = N
                        //PCN = 0
                        // lot_date = L9J
                        //serial_number = 049
                        //machine_line = 01
                        //shift = 1
                        // quantity = 100


                        var vendor_line = stampNew.Substring(timDZIH, 1);
                        var label_printer = stampNew.Substring(timDZIH + 1, 1);
                        var is_sample = stampNew.Substring(timDZIH + 2, 1);
                        var PCN = stampNew.Substring(timDZIH + 3, 1);
                        var date = stampNew.Substring(timDZIH + 4, 3);
                        var lot_date = DateFormatByShinsungRule(date);

                        var serial_number = stampNew.Substring(timDZIH + 7, 3); //gắn 001
                        var machine_line = stampNew.Substring(timDZIH + 10, 2); //gắn 01

                        var shift = stampNew.Substring(timDZIH + 12, 1);



                        //insert stamp_detail
                        var stamp_detail = new stamp_detail()
                        {
                            buyer_qr = stampNew,
                            stamp_code = stamp_cd,
                            product_code = ProductCode,
                            vendor_code = "DZIH",
                            vendor_line = vendor_line,
                            label_printer = label_printer,
                            is_sample = is_sample,
                            pcn = PCN,
                            lot_date = lot_date,
                            serial_number = serial_number,
                            machine_line = machine_line,
                            shift = shift,
                            standard_qty = ktra_stamp_cd.pack_amt.HasValue ? ktra_stamp_cd.pack_amt.Value : 0,
                            reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString(),
                            chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString(),
                        };



                        ////lot_date
                        //var date = stampNew.Substring(18, 3);
                        //var lot_date = DateFormatByShinsungRule(date);

                        ////var vendor_line
                        //var vendor_line = stampNew.Substring(14, 1);

                        ////var is_sample
                        //var is_sample = stampNew.Substring(16, 1);

                        ////var is_sample
                        //var serial_number = stampNew.Substring(21, 3);

                        ////insert stamp_detail
                        //var stamp_detail = new stamp_detail()
                        //{
                        //    buyer_qr = stampNew,
                        //    stamp_code = stamp_cd,

                        //    product_code = ProductCode,
                        //    vendor_code = "DZIH",
                        //    vendor_line = vendor_line,
                        //    label_printer = "1",
                        //    is_sample = is_sample,
                        //    pcn = "0",
                        //    lot_date = lot_date,
                        //    serial_number = serial_number,
                        //    machine_line = "01",
                        //    shift = "0",
                        //    standard_qty = ktra_stamp_cd.pack_amt.HasValue ? ktra_stamp_cd.pack_amt.Value : 0
                        //};

                        _TIMSService.Insertstampdetail(stamp_detail);

                    }
                  
                    // kiểm tra tem gói đã qua FG chưa nếu rồi thì update luôn
                    var isExist = _TIMSService.FindOneBuyerInfoById(stampOld);
                    if (isExist != null )
                    {
                        if (isExist.sts_cd != "001")
                        {
                            return Json(new { result = false, message = "Tem gói này không có thể đã đóng thùng hoặc đã giao hàng" }, JsonRequestBehavior.AllowGet);
                        }

                        isExist.buyer_qr = stampNew ;

                        isExist.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                        
                        _TIMSService.UpdateBuyerQRGeneral(isExist);
                    }
                    _IWOService.UpdateMaterialInfo(item);
                    return Json(new { result = true, message = Constant.Success }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { result = false, message = "Mã tem gói này không thuộc với Product của Container" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region RecevingSortingTims
        public ActionResult RecevingSortingTims()
        {
            return SetLanguage("~/Views/Tims/SortingTims/RecevingTims/RecevingSortingTims.cshtml");
        }
        public ActionResult ScanReceivingBuyer(string buyer_qr, string ShippingCode)
        {
            try
            {
                buyer_qr = buyer_qr.Trim();
                //Check input
                if (string.IsNullOrEmpty(ShippingCode))
                {
                    return Json(new { result = false, message =  "Vui lòng chọn một phiếu SF!!!" }, JsonRequestBehavior.AllowGet);
                }
                if (string.IsNullOrEmpty(buyer_qr))
                {
                    return Json(new { result = false, message = "Vui lòng scan một tem gói" }, JsonRequestBehavior.AllowGet);
                }

              

                //KIỂM TRA XEM SD CÓ TỒN TẠI KHÔNG.
                if (_TIMSService.CheckSFinfo(ShippingCode))
                {
                    return Json(new { result = false, message = "SF này không tồn tại" }, JsonRequestBehavior.AllowGet);
                }

                //kiểm tra tem gói có thuộc phiếu xuất đó ko

                var isCheckExistSF = _TIMSService.isCheckExistSF(ShippingCode, buyer_qr);
                if (isCheckExistSF == null )
                {
                    return Json(new { result = false, message = "Không có tem gói được trả về" }, JsonRequestBehavior.AllowGet);
                }
                //Step1: update w_material_info tem gói  lct= 006000000000000000, sts_cd = 015

                var isCheckExistBuyerQRSF = _TIMSService.isCheckExistBuyerQRSF(buyer_qr);
                if (isCheckExistBuyerQRSF == null)
                {
                    return Json(new { result = false, message = "Tem gói này không được mapping với OQC" }, JsonRequestBehavior.AllowGet);
                }
                isCheckExistBuyerQRSF.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                int updateWMaterial = _TIMSService.UpdateBuyerFGWMaterialInfo(isCheckExistBuyerQRSF);

                //Step2: update shippingfgsortingdetail  location= 006000000000000000 

                int updateShippingFGSorting = _TIMSService.updateShippingFGSorting(isCheckExistSF);

                return (Json(new { result = true, data = "" , message = "Thành công"}, JsonRequestBehavior.AllowGet));
                
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = e }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion
        #region ShippingSortingTims
        public ActionResult ShippingSortingTims()
        {
            return SetLanguage("~/Views/Tims/SortingTims/ShippingTims/ShippingSortingTims.cshtml");
        }
        public ActionResult searchShippingSortingTIMS(Pageing pageing, string ShippingCode, string ProductCode, string ProductName, string Description)
        {

            Dictionary<string, string> list = PagingAndOrderBy(pageing, " ORDER BY MyDerivedTable.ExportCode DESC ");



            int totalRecords = _TIMSService.TotalRecordsSearchShippingSortingTims(ShippingCode, ProductCode, ProductName, Description);
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

            IEnumerable<ShippingTIMSSortingModel> Data = _TIMSService.GetListSearchShippingSortingTIMS(ShippingCode, ProductCode, ProductName, Description);

            var result = new
            {
                total = totalPages,
                page = int.Parse(list["index"]),
                records = totalRecords,
                rows = Data
            };
            return Json(result, JsonRequestBehavior.AllowGet);

        }
        public JsonResult InsertShippingTIMSSorting(ShippingTIMSSortingModel item)
        {
            try
            {
                #region Tang tự động
                String ShippingCode = "ST1";

                var Shippinglast = _TIMSService.GetLastShippingTIMSSorting().FirstOrDefault();

                if (Shippinglast != null)
                {
                    ShippingCode = Shippinglast.ShippingCode;
                    ShippingCode = string.Concat("ST", (int.Parse(ShippingCode.Substring(2)) + 1).ToString());
                }
                #endregion
                item.ShippingCode = ShippingCode;
                item.IsFinish = "N";
                item.CreateId = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                item.CreateDate = DateTime.Now;
                item.ChangeId = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                item.ChangeDate = DateTime.Now;

                int id_ShippingFG = _TIMSService.InsertToShippingTIMSSorting(item);
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
        public ActionResult ModifyShippingTIMSSorting(ShippingTIMSSortingModel item)
        {
            try
            {
                item.ChangeId = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                item.ChangeDate = DateTime.Now;

                _TIMSService.ModifyShippingTIMSSorting(item);


                return Json(new { result = true, message = "Thành công!!!", data = item }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Sửa thất bại" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult GetExport_ScanBuyer_TIMS(string BuyerCode)
        {
            try
            {
                //Check input
                if (string.IsNullOrEmpty(BuyerCode))
                {
                    return Json(new { result = false, message = "Mã tem gói trống, Vui lòng Scan lại" }, JsonRequestBehavior.AllowGet);
                }
                BuyerCode = BuyerCode.Trim();
                //Kiểm tra mã TEM GOI có tồn tại ở kho TIMS không
                var IsExistBuyerQr = _TIMSService.CheckIsExistBuyerCode(BuyerCode);
                //kiểm tra xem mã khác 015 là kho khác
                if (IsExistBuyerQr != null)
                {
                    if (!IsExistBuyerQr.mt_sts_cd.Equals("015"))
                    {
                        return Json(new { result = false, message = "Mã Này không được sorting" }, JsonRequestBehavior.AllowGet);
                    }
                    string trangthai = IsExistBuyerQr.mt_sts_cd == "015" ? "Sorting" : "";
                    IsExistBuyerQr.mt_sts_cd = trangthai;
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
        public ActionResult UpdateShippingSortingTIMS(string ShippingCode, string ListId)
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

                var item = new ShippingTIMSSortingDetailModel();
                item.location = "003G01000000000000";

                _TIMSService.UpdateShippingSortingTIMS(item, ListId);

                //insert phiếu xuất và tem gói vào bảng shippingfgsortingdetail
                var UserID = Session["userid"] == null ? null : Session["userid"].ToString();
                _TIMSService.InsertShippingSortingTIMSDetail(ShippingCode, ListId, UserID);


                return Json(new { result = true, message = "Thành công" }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống" }, JsonRequestBehavior.AllowGet);
            }
        }
        #region PartialViewShippingTIMSSortingPP
        public ActionResult PartialViewShippingTIMSSortingPP(string ShippingCode)
        {
            ViewBag.ShippingCode = ShippingCode;
            return PartialView("~/Views/TIMS/SortingTims/ShippingTims/PartialViewShippingTIMSSortingPP.cshtml");
        }
        public ActionResult GetShippingSortingTIMSPP(string ShippingCode)
        {
            var listdata = _TIMSService.GetListSearchShippingSortingTIMSPP(ShippingCode);

            return Json(new { data = listdata }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetListShippingFGSorting(string ShippingCode)
        {
            var listdata = _TIMSService.GetListShippingTIMSSorting(ShippingCode);

            return Json(new { data = listdata }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetShippingScanPP_Countbuyer(string ShippingCode)
        {
            try
            {
                StringBuilder sql = new StringBuilder($"CALL GetShippingTIMSScanPP_Countbuyer('{ShippingCode}');");
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
    }
}
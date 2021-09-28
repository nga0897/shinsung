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
using ClosedXML.Excel;
using Mvc_VD.Classes;
using Mvc_VD.Services;
using MySql.Data.MySqlClient;
using Mvc_VD.Models.Language;

namespace Mvc_VD.Controllers
{
    public class DevManagementController : BaseController
    {
        //old private Entities db = new Entities();
        private Entities db;
        private readonly IDMSService _IDomMS;
        private readonly IWIPService _IWIPService;
        //private readonly ISystemService _iSystemService;
        public DevManagementController(IDbFactory dbFactory, IWIPService IWIPService, IDMSService DomMS)
        {

            db = dbFactory.Init();
            _IDomMS = DomMS;
            _IWIPService = IWIPService;
        }
        //public DevManagementController(IDomMS DomMS, IDbFactory dbFactory)
        //{
        //    _IDomMS = DomMS;
        //    db = dbFactory.Init();
        //}
        string exs = "Lỗi hệ thống!!!";
        string exits = "Dữ liệu đã tồn tại!!!";
        string ss = "Thành công!!!";



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

        #region popupmachine

        public ActionResult PopupMachine()
        {
            return PartialView();
        }

        public ActionResult Automachine(string Prefix)
        {
            //Note : you can bind same list from database
            ArrayList row_data = new ArrayList();

            var list = db.d_machine_info.ToList();
            foreach (var item in list)
            {
                var x = new
                {
                    mc_no = item.mc_no,
                };
                row_data.Add(x);
            };
            //Searching records from list using LINQ query
            var machine = (from N in list
                           where (N.mc_no.Contains(Prefix) || N.mc_no.Contains(Prefix.ToUpper()))
                           select new { N.mc_no });
            if (machine.Count() == 0)
            {
                return View();
            }
            return Json(machine, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetPopupmachine(d_machine_info d_machine_info)
        {
            var vaule = db.d_machine_info.ToList();
            var machineMgtData = new { rows = vaule };
            return Json(machineMgtData, JsonRequestBehavior.AllowGet);
        }

        #endregion popupmachine

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Material
        public JsonResult add_month(string date, int month)
        {
            DateTime SearchDate = DateTime.Parse(date);
            var kq = SearchDate.AddMonths(month).ToString("yyyy-MM-dd");
            return Json(kq, JsonRequestBehavior.AllowGet);

        }
        public ActionResult getpopupsuplier()
        {
            var sql = new StringBuilder();
            sql.Append(" SELECT si.* ")
                .Append("FROM  supplier_info as si ")
                .Append("INNER JOIN comm_dt ON ")
                .Append("( comm_dt.dt_cd= si.bsn_tp )")
                .Append("WHERE comm_dt.mt_cd='COM001' ");
            var data = db.supplier_info.SqlQuery(sql.ToString()).ToList<supplier_info>();
            foreach (var item in data)
            {
                var conndition = "SELECT * FROM comm_dt WHERE dt_cd='" + item.bsn_tp + "'";

                var com = db.comm_dt.SqlQuery(conndition).FirstOrDefault();

                item.bsn_tp = ((com != null) ? com.dt_nm : string.Empty);
            }
            var jsondatacomdt = new
            {
                rows = data
            };
            return Json(jsondatacomdt, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Gettypepopup()
        {
            var data = db.comm_dt.Where(x => x.mt_cd == "COM001").OrderBy(x => x.dt_nm).ToList();
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public JsonResult searchpusupllier()
        {
            // Get Data from ajax function
            var code = Request["codeData"];
            var name = Request["nameData"];
            var bsn_search = Request["bsn_searchData"];
            var sql = new StringBuilder();
            sql.Append(" SELECT si.* ")
                .Append("FROM  supplier_info as si ")
                .Append("INNER JOIN comm_dt ON ")
                .Append("( comm_dt.dt_cd= si.bsn_tp )")
                .Append("WHERE comm_dt.mt_cd='001' ")
                .Append("AND ('" + code + "'='' OR  si.sp_cd like '%" + code + "%' )")
                .Append("AND ('" + name + "'='' OR  si.sp_nm like '%" + name + "%' )")
                .Append("AND ('" + bsn_search + "'='' OR  comm_dt.dt_cd = '" + bsn_search + "')");
            var data = db.supplier_info.SqlQuery(sql.ToString()).ToList<supplier_info>();
            foreach (var item in data)
            {
                var conndition = "SELECT * FROM comm_dt WHERE dt_cd='" + item.bsn_tp + "'";

                var com = db.comm_dt.SqlQuery(conndition).FirstOrDefault();

                item.bsn_tp = ((com != null) ? com.dt_nm : string.Empty);
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetPopupManu()
        {
            var list = db.manufac_info.ToList();
            return Json(new { rows = list }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Material()
        {
            return SetLanguage("");
         
        }

        public ActionResult GetMaterial()
        {
            var vaule = (from a in db.d_material_info
                         orderby a.chg_dt descending
                         select new
                         {
                             mtid = a.mtid,
                             reg_dt = a.reg_dt,
                             area = a.area,
                             area_unit = a.area_unit,
                             mt_type = a.mt_type,
                             mt_no = a.mt_no,
                             mt_nm = a.mt_nm,
                             mf_cd = a.mf_cd,
                             sp_cd = a.sp_cd,
                             width = a.width,
                             width_unit = a.width_unit,
                             spec = a.spec,
                             spec_unit = a.spec_unit,
                             price = a.price,
                             price_unit = a.price_unit,
                             photo_file = a.photo_file,
                             re_mark = a.re_mark,
                             s_lot_no = a.s_lot_no,
                             reg_id = a.reg_id,
                             chg_dt = a.chg_dt,
                             item_vcd = a.item_vcd,
                             item_vcd1 = a.item_vcd,
                             qc_range_cd = a.qc_range_cd,
                             gr_qty = a.gr_qty,
                             unit_cd = a.unit_cd,
                         }).AsEnumerable().Select(x => new
                         {
                             mtid = x.mtid,
                             reg_dt = x.reg_dt,
                             new_spec = ((x.spec != null) ? x.spec + " " + x.spec_unit : string.Empty),
                             new_with = ((x.width != null) ? x.width + " " + x.width_unit : string.Empty),
                             new_price = ((x.price != null) ? x.price + " " + x.price_unit : string.Empty),
                             new_area = ((x.area != null) ? x.area + " " + x.area_unit : string.Empty),
                             area = x.area,
                             area_unit = x.area_unit,
                             mt_type = x.mt_type,
                             mt_no = x.mt_no,
                             mt_nm = x.mt_nm,
                             mf_cd = x.mf_cd,
                             sp_cd = x.sp_cd,
                             width = x.width,
                             width_unit = x.width_unit,
                             spec = x.spec,
                             spec_unit = x.spec_unit,
                             price = x.price,
                             price_unit = x.price_unit,
                             photo_file = x.photo_file,
                             re_mark = x.re_mark,
                             s_lot_no = x.s_lot_no,
                             reg_id = x.reg_id,
                             chg_dt = x.chg_dt,
                             item_vcd = x.item_vcd,
                             item_vcd1 = x.item_vcd,
                             qc_range_cd = x.qc_range_cd,
                             gr_qty = x.gr_qty,
                             unit_cd = x.unit_cd,
                         }).ToList();
            var jsonDataComDt = new
            {
                rows = vaule
            };
            return Json(jsonDataComDt, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Getpp_qc_type_mt()
        {
            var list = db.qc_item_mt.Where(x => x.item_type == "MT" && x.del_yn == "N").ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetMaterial_page()
        {
            var vaule = (from a in db.d_material_info
                         orderby a.chg_dt descending
                         select new
                         {
                             mtid = a.mtid,
                             reg_dt = a.reg_dt,
                             area = a.area,
                             area_unit = a.area_unit,
                             mt_type = a.mt_type,
                             mt_no = a.mt_no,
                             mt_nm = a.mt_nm,
                             mf_cd = a.mf_cd,
                             sp_cd = a.sp_cd,
                             width = a.width,
                             width_unit = a.width_unit,
                             spec = a.spec,
                             spec_unit = a.spec_unit,
                             price = a.price,
                             price_unit = a.price_unit,
                             photo_file = a.photo_file,
                             re_mark = a.re_mark,
                             reg_id = a.reg_id,
                             chg_dt = a.chg_dt,
                         }).AsEnumerable().Select(x => new
                         {
                             mtid = x.mtid,
                             reg_dt = x.reg_dt,
                             new_spec = ((x.spec != null) ? x.spec + " " + x.spec_unit : string.Empty),
                             new_with = ((x.width != null) ? x.width + " " + x.width_unit : string.Empty),
                             new_price = ((x.price != null) ? x.price + " " + x.price_unit : string.Empty),
                             new_area = ((x.area != null) ? x.area + " " + x.area_unit : string.Empty),
                             area = x.area,
                             area_unit = x.area_unit,
                             mt_type = x.mt_type,
                             mt_no = x.mt_no,
                             mt_nm = x.mt_nm,
                             mf_cd = x.mf_cd,
                             sp_cd = x.sp_cd,
                             width = x.width,
                             width_unit = x.width_unit,
                             spec = x.spec,
                             spec_unit = x.spec_unit,
                             price = x.price,
                             price_unit = x.price_unit,
                             photo_file = x.photo_file,
                             re_mark = x.re_mark,
                             reg_id = x.reg_id,
                             chg_dt = x.chg_dt,
                         }).ToList();
            var jsonDataComDt = new
            {
                rows = vaule
            };

            return Json(jsonDataComDt, JsonRequestBehavior.AllowGet);
        }

        private string fotmat_gia(string price)
        {
            try
            {
                var gia = int.Parse(price);
                var giatri = Convert.ToDecimal(gia).ToString("#,###,###,###,###");
                return string.Format(giatri);
            }
            catch (Exception)
            {
                var gia = price;
                var giatri = Convert.ToString(gia);
                return string.Format(giatri);
            }
        }

        public ActionResult GetType_Marterial(comm_mt comm_dt)
        {
            var type = db.comm_dt.ToList().Where(item => item.mt_cd == "COM004").ToList();
            return Json(type, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetWidth_Marterial(comm_mt comm_dt)
        {
            var width = db.comm_dt.ToList().Where(item => item.mt_cd == "COM005").ToList();
            return Json(width, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetSpec_Marterial(comm_mt comm_dt)
        {
            var spec = db.comm_dt.ToList().Where(item => item.mt_cd == "COM006").ToList();
            return Json(spec, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetPrice_Marterial(comm_mt comm_dt)
        {
            var price = db.comm_dt.ToList().Where(item => item.mt_cd == "COM002").ToList();
            return Json(price, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetArea_Marterial(comm_mt comm_dt)
        {
            var price = db.comm_dt.ToList().Where(item => item.mt_cd == "COM011").ToList();
            return Json(price, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetStickness(comm_mt comm_dt)
        {
            var price = db.comm_dt.ToList().Where(item => item.mt_cd == "COM028").ToList();
            return Json(price, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Getprice_least_unit(comm_mt comm_dt)
        {
            var price = db.comm_dt.ToList().Where(item => item.mt_cd == "COM029").ToList();
            return Json(price, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetQCRange_Marterial(comm_mt comm_dt)
        {
            var qc = db.comm_dt.ToList().Where(item => item.mt_cd == "COM017" && item.use_yn == "Y").ToList();
            return Json(qc, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Get_Getbundle(comm_mt comm_dt)
        {
            var qc = db.comm_dt.ToList().Where(item => item.mt_cd == "COM027" && item.use_yn == "Y").ToList();
            return Json(qc, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetQCRange(comm_mt comm_dt)
        {
            var QCRange = db.comm_dt.ToList().Where(item => item.mt_cd == "COM017").ToList();
            return Json(QCRange, JsonRequestBehavior.AllowGet);
        }

        public ActionResult getQC_gs(qc_item_mt qc_item_mt)
        {
            var Qc_gs = db.qc_item_mt.ToList().Where(item => item.item_type == "GS" && item.del_yn == "N").ToList();
            return Json(Qc_gs, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetUnit_qty(comm_mt comm_dt)
        {
            var qc = db.comm_dt.ToList().Where(item => item.mt_cd == "COM025" && item.use_yn == "Y").ToList();
            return Json(qc, JsonRequestBehavior.AllowGet);
        }

        private string CreatemT(int id)
        {
            if (id.ToString().Length < 2)
            {
                return string.Format("0000000{0}", id);
            }

            if ((id.ToString().Length < 3) || (id.ToString().Length == 2))
            {
                return string.Format("000000{0}", id);
            }

            if ((id.ToString().Length < 4) || (id.ToString().Length == 3))
            {
                return string.Format("00000{0}", id);
            }
            if ((id.ToString().Length < 5) || (id.ToString().Length == 4))
            {
                return string.Format("0000{0}", id);
            }
            if ((id.ToString().Length < 6) || (id.ToString().Length == 5))
            {
                return string.Format("000{0}", id);
            }
            if ((id.ToString().Length < 7) || (id.ToString().Length == 6))
            {
                return string.Format("00{0}", id);
            }
            if ((id.ToString().Length < 8) || (id.ToString().Length == 7))
            {
                return string.Format("0{0}", id);
            }
            if ((id.ToString().Length < 9) || (id.ToString().Length == 8))
            {
                return string.Format("{0}", id);
            }
            return string.Empty;
        }

        public JsonResult deleteMaterial(int id)
        {
            try
            {
                int count = db.d_material_info.Where(x => x.mtid == id).ToList().Count();
                var result = new { result = true };
                if (count > 0)
                {
                    d_material_info d_material_info = db.d_material_info.Find(id);

                    var ds_w_material_info = db.w_material_info.Where(x => x.mt_no == d_material_info.mt_no).ToList();
                    db.Entry(d_material_info).State = EntityState.Deleted;
                    db.SaveChanges();
                    return Json(id, JsonRequestBehavior.AllowGet);
                }
                result = new { result = false };
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult insertmaterial(d_material_info d_material_info, string sts_create)
        {
            try
            {
                if (d_material_info.mt_type == "PMT")
                {
                    d_material_info.mt_no = "C" + d_material_info.mt_no + "-" + d_material_info.width;
                }
                var menuCd1 = string.Empty;
                var sublog1 = new d_material_info();
                var list1 = db.d_material_info.Where(x => x.mt_no == d_material_info.mt_no).ToList();
                if (list1.Count == 0)
                {
                    {
                        var profile = Request.Files;
                        string imgname = string.Empty;
                        string ImageName = string.Empty;

                        if (System.Web.HttpContext.Current.Request.Files.AllKeys.Any())
                        {
                            var logo1 = System.Web.HttpContext.Current.Request.Files["file"];
                            if (logo1.ContentLength > 0)
                            {
                                var profileName = Path.GetFileName(logo1.FileName);
                                var ext = Path.GetExtension(logo1.FileName);

                                ImageName = profileName;
                                var comPath = Server.MapPath("~/Images/MarterialImg/") + ImageName;

                                logo1.SaveAs(comPath);
                                d_material_info.photo_file = ImageName;
                            }
                        }

                        d_material_info.reg_dt = DateTime.Now;
                        d_material_info.chg_dt = DateTime.Now;
                        d_material_info.del_yn = "N";
                        d_material_info.use_yn = "Y";
                        d_material_info.reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                        d_material_info.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                        if (ModelState.IsValid) //check what type of extension
                        {
                            db.Entry(d_material_info).State = EntityState.Added;
                            db.SaveChanges(); // line that threw exception
                            var vaule = (from a in db.d_material_info
                                         where a.mtid == d_material_info.mtid
                                         select new
                                         {
                                             mtid = a.mtid,
                                             area = a.area,
                                             barcode = a.barcode,
                                             area_unit = a.area_unit,
                                             mt_type = a.mt_type,
                                             mt_no = a.mt_no,
                                             mt_nm = a.mt_nm,
                                             mf_cd = a.mf_cd,
                                             sp_cd = a.sp_cd,
                                             item_vcd = a.item_vcd,
                                             qc_range_cd = a.qc_range_cd,
                                             width = a.width,
                                             width_unit = a.width_unit,
                                             spec = a.spec,
                                             spec_unit = a.spec_unit,
                                             price = a.price,
                                             unit_cd = a.unit_cd,
                                             gr_qty = a.gr_qty,
                                             price_unit = a.price_unit,
                                             photo_file = a.photo_file,
                                             re_mark = a.re_mark,
                                             s_lot_no = a.s_lot_no,
                                             bundle_qty = a.bundle_qty,
                                             bundle_unit = a.bundle_unit,
                                             mt_no_origin = a.mt_no_origin,
                                             thick = a.thick,
                                             thick_unit = a.thick_unit,
                                             stick = a.stick,
                                             stick_unit = a.stick_unit,
                                             tot_price = a.tot_price,
                                             price_least_unit = a.price_least_unit,
                                             mt_cd = a.mt_cd,
                                             reg_id = a.reg_id,
                                             reg_dt = a.reg_dt,
                                             chg_id = a.chg_id,
                                             chg_dt = a.chg_dt,
                                             consumable = a.consum_yn
                                         }).AsEnumerable().Select(x => new
                                         {
                                             mtid = x.mtid,
                                             mt_cd = x.mt_cd,
                                             barcode = x.barcode,
                                             mt_type_nm = (db.comm_dt.Where(k => k.mt_cd == "COM004" && k.dt_cd == x.mt_type).Select(k => k.dt_nm)),
                                             new_price = ((x.price != null) ? x.price + " " + x.width_unit : string.Empty),
                                             new_spec = ((x.spec != null) ? x.spec : string.Empty),
                                             new_with = ((x.width != null) ? x.width : string.Empty),
                                             area_all = ((x.area != null) ? x.area : string.Empty),
                                             tot_price_new = ((x.tot_price != null) ? x.tot_price : string.Empty),
                                             stick_new = ((x.stick != null) ? x.stick : string.Empty),
                                             thick_new = ((x.thick != null) ? x.thick : string.Empty),
                                             bundle_qty = x.bundle_qty,
                                             thick = x.thick,
                                             thick_unit = x.thick_unit,
                                             stick = x.stick,
                                             stick_unit = x.stick_unit,
                                             tot_price = x.tot_price,
                                             price_least_unit = x.price_least_unit,
                                             area = x.area,
                                             area_unit = x.area_unit,
                                             item_vcd = x.item_vcd,
                                             item_nm = ((x.item_vcd != null || x.item_vcd != "") ? db.qc_item_mt.Where(m => m.item_vcd == x.item_vcd).Select(m => m.item_nm) : null),
                                             qc_range_cd = x.qc_range_cd,
                                             qc_range_cd_nm = ((x.qc_range_cd != null || x.qc_range_cd != "") ? db.comm_dt.Where(m => m.mt_cd == "COM017" && m.dt_cd == x.qc_range_cd && m.use_yn == "Y").Select(m => m.dt_nm) : null),
                                             bundle_unit_nm = ((x.bundle_unit != null || x.bundle_unit != "") ? db.comm_dt.Where(m => m.mt_cd == "COM027" && m.dt_cd == x.bundle_unit && m.use_yn == "Y").Select(m => m.dt_nm) : null),
                                             mt_type = x.mt_type,
                                             bundle_unit = x.bundle_unit,
                                             mt_no = x.mt_no,
                                             mt_nm = x.mt_nm,
                                             unit_cd = x.unit_cd,
                                             mf_cd = x.mf_cd,
                                             sp_cd = x.sp_cd,
                                             mt_no_origin = x.mt_no_origin,
                                             s_lot_no = x.s_lot_no,
                                             width = x.width,
                                             gr_qty = x.gr_qty,
                                             width_unit = x.width_unit,
                                             spec = x.spec,
                                             spec_unit = x.spec_unit,
                                             price = x.price,
                                             price_unit = x.price_unit,
                                             photo_file = x.photo_file,
                                             re_mark = x.re_mark,
                                             reg_id = x.reg_id,
                                             reg_dt = x.reg_dt,
                                             chg_id = x.chg_id,
                                             chg_dt = x.chg_dt,
                                             consumable = x.consumable
                                         }).FirstOrDefault();
                            return Json(new { result = true, type = "", kq = vaule }, JsonRequestBehavior.AllowGet);
                        }
                    }
                }
                return Json(new { result = false, type = "", kq = "MT Code  already exists" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, kq = e }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult updatematerial(int mtid, string gr_qty, string barcode, string mt_type, string mt_no, string mt_nm, string width, string width_unit, string spec, string spec_unit, string price, string price_unit, string photo_file, string re_mark, string sp_cd, string mf_cd, string file, string area, string area_unit, string s_lot_no, string item_vcd, string qc_range_cd, string unit_cd, string bundle_unit, string mt_no_origin, string stick, string stick_unit, string tot_price, string price_least_unit, string mt_cd, string thick, string thick_unit, string m_consumable, string bundle_qty)
        {
            var listcount = db.d_material_info.Count(x => x.mtid == mtid);
            d_material_info d_material_info = db.d_material_info.Find(mtid);
            d_material_info.bundle_qty = bundle_qty;
            d_material_info.sp_cd = sp_cd;
            d_material_info.mf_cd = mf_cd;
            d_material_info.barcode = barcode;

            //d_material_info.chg_dt = DateTime.Now;
            d_material_info.del_yn = "N";
            d_material_info.use_yn = "Y";
            d_material_info.mt_type = mt_type;
            d_material_info.mt_nm = mt_nm;
            d_material_info.gr_qty = gr_qty;
            d_material_info.unit_cd = unit_cd;
            d_material_info.width = width;
            d_material_info.width_unit = width_unit;
            d_material_info.spec = spec;
            d_material_info.spec_unit = spec_unit;
            d_material_info.re_mark = re_mark;
            d_material_info.price = price;
            d_material_info.price_unit = price_unit;
            d_material_info.area = area;
            d_material_info.area_unit = area_unit;
            d_material_info.s_lot_no = s_lot_no;
            d_material_info.qc_range_cd = qc_range_cd;
            d_material_info.item_vcd = item_vcd;
            d_material_info.bundle_unit = bundle_unit;
            d_material_info.mt_no_origin = mt_no_origin;
            d_material_info.tot_price = tot_price;
            d_material_info.stick_unit = stick_unit;
            d_material_info.stick = stick;
            d_material_info.price_least_unit = price_least_unit;
            d_material_info.mt_cd = mt_cd;
            d_material_info.thick = thick;
            d_material_info.thick_unit = thick_unit;
            d_material_info.consum_yn = m_consumable;

            var profile = Request.Files;
            string imgname = string.Empty;
            string ImageName = string.Empty;

            d_material_info.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
            if (System.Web.HttpContext.Current.Request.Files.AllKeys.Any())
            {
                var logo1 = System.Web.HttpContext.Current.Request.Files["file"];
                if (logo1.ContentLength > 0)
                {
                    var profileName = Path.GetFileName(logo1.FileName);
                    var ext = Path.GetExtension(logo1.FileName);

                    ImageName = profileName;
                    var comPath = Server.MapPath("~/Images/MarterialImg/") + ImageName;

                    logo1.SaveAs(comPath);
                    d_material_info.photo_file = ImageName;
                }
            }

            if (ModelState.IsValid)
            {
                db.Entry(d_material_info).State = EntityState.Modified;
                db.SaveChanges();
                var vaule = (from a in db.d_material_info
                             where a.mtid == d_material_info.mtid
                             select new
                             {
                                 mtid = a.mtid,
                                 area = a.area,
                                 barcode = a.barcode,
                                 area_unit = a.area_unit,
                                 bundle_qty = a.bundle_qty,
                                 mt_type = a.mt_type,
                                 mt_no = a.mt_no,
                                 mt_cd = a.mt_cd,
                                 mt_nm = a.mt_nm,
                                 mf_cd = a.mf_cd,
                                 sp_cd = a.sp_cd,
                                 width = a.width,
                                 unit_cd = a.unit_cd,
                                 width_unit = a.width_unit,
                                 item_vcd = a.item_vcd,
                                 qc_range_cd = a.qc_range_cd,
                                 spec = a.spec,
                                 spec_unit = a.spec_unit,
                                 s_lot_no = a.s_lot_no,
                                 gr_qty = a.gr_qty,
                                 price = a.price,
                                 price_unit = a.price_unit,
                                 photo_file = a.photo_file,
                                 re_mark = a.re_mark,
                                 bundle_unit = a.bundle_unit,
                                 mt_no_origin = a.mt_no_origin,
                                 thick = a.thick,
                                 thick_unit = a.thick_unit,
                                 stick = a.stick,
                                 stick_unit = a.stick_unit,
                                 tot_price = a.tot_price,
                                 price_least_unit = a.price_least_unit,
                                 reg_id = a.reg_id,
                                 reg_dt = a.reg_dt,
                                 chg_id = a.chg_id,
                                 chg_dt = a.chg_dt,
                                 consumable = a.consum_yn
                             }).AsEnumerable().Select(x => new
                             {
                                 mtid = x.mtid,
                                 mt_cd = x.mt_cd,
                                 mt_type_nm = (db.comm_dt.Where(k => k.mt_cd == "COM004" && k.dt_cd == x.mt_type).Select(k => k.dt_nm)),
                                 bundle_qty = x.bundle_qty,
                                 new_price = ((x.price != null) ? x.price : string.Empty),
                                 new_spec = ((x.spec != null) ? x.spec : string.Empty),
                                 new_with = ((x.width != null) ? x.width : string.Empty),
                                 area_all = ((x.area != null) ? x.area : string.Empty),
                                 tot_price_new = ((x.tot_price != null) ? x.tot_price : string.Empty),
                                 stick_new = ((x.stick != null) ? x.stick : string.Empty),
                                 thick_new = ((x.thick != null) ? x.thick : string.Empty),
                                 barcode = x.barcode,
                                 thick = x.thick,
                                 thick_unit = x.thick_unit,
                                 stick = x.stick,
                                 stick_unit = x.stick_unit,
                                 tot_price = x.tot_price,
                                 price_least_unit = x.price_least_unit,
                                 area = x.area,
                                 area_unit = x.area_unit,
                                 item_vcd = x.item_vcd,
                                 item_nm = ((x.item_vcd != null || x.item_vcd != "") ? db.qc_item_mt.Where(m => m.item_vcd == x.item_vcd).Select(m => m.item_nm) : null),
                                 qc_range_cd = x.qc_range_cd,
                                 qc_range_cd_nm = ((x.qc_range_cd != null || x.qc_range_cd != "") ? db.comm_dt.Where(m => m.mt_cd == "COM017" && m.dt_cd == x.qc_range_cd && m.use_yn == "Y").Select(m => m.dt_nm) : null),
                                 bundle_unit_nm = ((x.bundle_unit != null || x.bundle_unit != "") ? db.comm_dt.Where(m => m.mt_cd == "COM027" && m.dt_cd == x.bundle_unit && m.use_yn == "Y").Select(m => m.dt_nm) : null),
                                 mt_type = x.mt_type,
                                 bundle_unit = x.bundle_unit,
                                 mt_no = x.mt_no,
                                 mt_nm = x.mt_nm,
                                 unit_cd = x.unit_cd,
                                 mf_cd = x.mf_cd,
                                 sp_cd = x.sp_cd,
                                 mt_no_origin = x.mt_no_origin,
                                 s_lot_no = x.s_lot_no,
                                 width = x.width,
                                 gr_qty = x.gr_qty,
                                 width_unit = x.width_unit,
                                 spec = x.spec,
                                 spec_unit = x.spec_unit,
                                 price = x.price,
                                 price_unit = x.price_unit,
                                 photo_file = x.photo_file,
                                 re_mark = x.re_mark,
                                 reg_id = x.reg_id,
                                 reg_dt = x.reg_dt,
                                 chg_dt = x.chg_dt,
                                 chg_id = x.chg_id,
                                 consumable = x.consumable
                             }).ToList();
                return Json(vaule, JsonRequestBehavior.AllowGet);
            }
            return View("Material");
        }

        public JsonResult searchmaterial()
        {
            // Get Data from ajax function
            var name = Request["nameData"];
            var type = Request["typeData"];
            var code = Request["codeData"];
            var start = Request.QueryString["start1Data"];
            var end = Request.QueryString["end1Data"];
            var dateConvert = new DateTime();
            if (DateTime.TryParse(end, out dateConvert))
            {
                end = dateConvert.ToString("yyyy/MM/dd");
            }

            if (DateTime.TryParse(start, out dateConvert))
            {
                start = dateConvert.ToString("yyyy/MM/dd");
            }

            var sql = new StringBuilder();
            if ((end != "") && (start != ""))
            {
                sql.Append(" SELECT dmi.* ")
                    .Append("FROM d_material_info  as dmi ")
                    .Append("Where ('" + name + "'='' OR  dmi.mt_nm like '%" + name + "%' )")
                    .Append("AND ('" + type + "'='' OR  dmi.mt_type like '%" + type + "%' )")
                    .Append("AND ('" + code + "'='' OR  dmi.mt_no like '%" + code + "%')")
                    .Append("AND ('" + start + "'='' OR DATE_FORMAT(dmi.reg_dt,'%Y/%m/%d') >= DATE_FORMAT('" + start + "','%Y/%m/%d'))")
                    .Append("AND ('" + end + "'='' OR DATE_FORMAT(dmi.reg_dt,'%Y/%m/%d') <= DATE_FORMAT('" + end + "','%Y/%m/%d')) and dmi.del_yn='N'");
            }
            else
            {
                sql.Append(" SELECT dmi.* ")
                    .Append("FROM d_material_info  as dmi ")
                    .Append("Where ('" + name + "'='' OR  dmi.mt_nm like '%" + name + "%' )")
                    .Append("AND ('" + type + "'='' OR  dmi.mt_type like '%" + type + "%' )")
                    .Append("AND ('" + code + "'='' OR  dmi.mt_no like '%" + code + "%')")
                    .Append("AND ('" + start + "'='' OR DATE_FORMAT(dmi.reg_dt,'%Y/%m/%d') = DATE_FORMAT('" + start + "','%Y/%m/%d'))")
                    .Append("AND ('" + end + "'='' OR DATE_FORMAT(dmi.reg_dt,'%Y/%m/%d') = DATE_FORMAT('" + end + "','%Y/%m/%d')) and dmi.del_yn='N'");
            }

            var data = db.d_material_info.SqlQuery(sql.ToString()).ToList<d_material_info>();
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public JsonResult searchmaterial_2(Pageing pageing, string type, string name, string code, string start, string end, string sp)
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
                + " WHERE a.del_yn='N' and a.mt_type!='CMT'  "
                + " AND ('" + name + "'='' OR a.mt_nm LIKE '%" + name + "%')"
                + " AND ('" + type + "'='' OR a.mt_type LIKE '%" + type + "%')"
                + " AND ('" + code + "'='' OR a.mt_no LIKE '%" + code + "%')"
                + " AND ('" + sp + "'='' OR a.sp_cd LIKE '%" + sp + "%')"


            + " AND ('" + start + "'='' OR DATE_FORMAT(a.reg_dt,'%Y/%m/%d') >= DATE_FORMAT('" + start + "','%Y/%m/%d')) "
                + " AND ('" + end + "'='' OR DATE_FORMAT(a.reg_dt,'%Y/%m/%d') <= DATE_FORMAT('" + end + "','%Y/%m/%d')) "
                ;
            string countSql = " SELECT COUNT(*) FROM ( " + tempSql + " ) a ";
            string viewSql = " SELECT * FROM ( " + tempSql + " ) a WHERE a.RowNum BETWEEN " + list["start"] + " AND " + list["end"] + list["orderBy"];
            return SearchAndPaging(countSql, viewSql, int.Parse(list["index"]), int.Parse(list["size"]));
        }
        public JsonResult searchmaterial_replace(Pageing pageing, string type, string name, string code, string start, string end, string sp, string MaterialPrarent)
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
                + " WHERE a.del_yn='N' and a.mt_type!='CMT'  and a.mt_no !=  '" + MaterialPrarent + "' "
                + " AND ('" + name + "'='' OR a.mt_nm LIKE '%" + name + "%')"
                + " AND ('" + type + "'='' OR a.mt_type LIKE '%" + type + "%')"
                + " AND ('" + code + "'='' OR a.mt_no LIKE '%" + code + "%')"
                + " AND ('" + sp + "'='' OR a.sp_cd LIKE '%" + sp + "%')"


            + " AND ('" + start + "'='' OR DATE_FORMAT(a.reg_dt,'%Y/%m/%d') >= DATE_FORMAT('" + start + "','%Y/%m/%d')) "
                + " AND ('" + end + "'='' OR DATE_FORMAT(a.reg_dt,'%Y/%m/%d') <= DATE_FORMAT('" + end + "','%Y/%m/%d')) "
                ;
            string countSql = " SELECT COUNT(*) FROM ( " + tempSql + " ) a ";
            string viewSql = " SELECT * FROM ( " + tempSql + " ) a WHERE a.RowNum BETWEEN " + list["start"] + " AND " + list["end"] + list["orderBy"];
            return SearchAndPaging(countSql, viewSql, int.Parse(list["index"]), int.Parse(list["size"]));
        }

        public ActionResult ExportToExcel(string type, string name, string code, string start, string end, string sp)
        {
            // Get Data from ajax function
            //var grid = new GridView();
            var dateConvert = new DateTime();

            if (DateTime.TryParse(end, out dateConvert))
            {
                end = dateConvert.ToString("yyyy/MM/dd");
            }

            if (DateTime.TryParse(start, out dateConvert))
            {
                start = dateConvert.ToString("yyyy/MM/dd");
            }

            string sql = "SELECT * FROM d_material_info AS a "
                + "WHERE a.del_yn='N' and a.mt_type !='CMT' "
                + "AND (a.mt_nm LIKE '%" + name + "%') "
                + "AND (a.mt_type LIKE '%" + type + "%') "
                + "AND (a.mt_no LIKE '%" + code + "%') "
                //+ "AND (a.sp_cd LIKE '%" + sp + "%') "
                + "AND ('" + start + "'='' OR DATE_FORMAT(a.reg_dt,'%Y/%m/%d') >= DATE_FORMAT('" + start + "','%Y/%m/%d')) "
                + "AND ('" + end + "'='' OR DATE_FORMAT(a.reg_dt,'%Y/%m/%d') <= DATE_FORMAT('" + end + "','%Y/%m/%d')) "
                + " ORDER BY a.mt_no DESC "
                ;

            var data = db.Database.SqlQuery<d_material_info>(sql);

            var values = data.ToList().AsEnumerable().Select(x => new
            {
                ID = x.mtid,
                Type = x.mt_type,
                Barcode = x.barcode,
                MT_NO = x.mt_no,
                Name = x.mt_nm,
                //Group_Qty = x.gr_qty,
                Unit = x.unit_cd,
                Bundle = x.bundle_qty,
                Bundle_Unit = ((x.bundle_unit != null || x.bundle_unit != "") ? db.comm_dt.Where(m => m.mt_cd == "COM027" && m.dt_cd == x.bundle_unit && m.use_yn == "Y").Select(m => m.dt_nm) : null),
                Supplier = x.sp_cd,
                Manufacturer = x.mf_cd,
                Width = ((x.width != null) ? x.width : string.Empty),
                Length = ((x.spec != null) ? x.spec : string.Empty),
                Area = ((x.area != null) ? x.area : string.Empty),
                area_unit = x.area_unit,
                //Price = ((x.price != null) ? x.price : string.Empty),
                //Total_Price = ((x.tot_price != null) ? x.tot_price : string.Empty),
                //Stickness = ((x.stick != null) ? x.stick : string.Empty),
                //Thickness = ((x.thick != null) ? x.thick : string.Empty),
                //Description = x.re_mark,
                //QC_Code = x.item_vcd,
                //QCName = ((x.item_vcd != null || x.item_vcd != "") ? db.qc_item_mt.Where(m => m.item_vcd == x.item_vcd).Select(m => m.item_nm) : null),
                //QC_Range = ((x.qc_range_cd != null || x.qc_range_cd != "") ? db.comm_dt.Where(m => m.mt_cd == "COM017" && m.dt_cd == x.qc_range_cd && m.use_yn == "Y").Select(m => m.dt_nm) : null),
                //Origin_MT_NO = x.mt_no_origin,
                Photo = x.photo_file,
                Create_Date = x.reg_dt,
                Create_User = x.reg_id,
                Change_Name = x.chg_id,
                Change_Date = x.chg_dt
            }).ToArray();

            String[] labelList = new string[19] { "ID", "Type", "Barcode", "MT NO", "Name", "Unit", "Bundle Qty", "Bundle Unit", "Supplier", "Manufacturer", "Width (mm)", "Length (M)", "Area(m²)", "area unit", "Price", "Create Date", "Create User", "Change Name", "Change Date" };

            Response.ClearContent();

            Response.AddHeader("content-disposition", "attachment;filename=MaterialManagement.xls");

            Response.AddHeader("Content-Type", "application/vnd.ms-excel");

            WriteHtmlTable(values, Response.Output, labelList);

            Response.End();

            return View("Material");
        }

        public ActionResult ExportToHTML(string type, string name, string code, string start, string end)
        {
            // Get Data from ajax function
            //var grid = new GridView();
            var dateConvert = new DateTime();

            if (DateTime.TryParse(end, out dateConvert))
            {
                end = dateConvert.ToString("yyyy/MM/dd");
            }

            if (DateTime.TryParse(start, out dateConvert))
            {
                start = dateConvert.ToString("yyyy/MM/dd");
            }

            string sql = "SELECT * FROM d_material_info AS a "
                + "WHERE a.del_yn='N'  "
                + "AND (a.mt_nm LIKE '%" + name + "%') "
                + "AND (a.mt_type LIKE '%" + type + "%') "
                + "AND (a.mt_no LIKE '%" + code + "%') "
                //+ "AND (a.mt_cd LIKE '%" + mt_cd + "%') "
                + "AND ('" + start + "'='' OR DATE_FORMAT(a.reg_dt,'%Y/%m/%d') >= DATE_FORMAT('" + start + "','%Y/%m/%d')) "
                + "AND ('" + end + "'='' OR DATE_FORMAT(a.reg_dt,'%Y/%m/%d') <= DATE_FORMAT('" + end + "','%Y/%m/%d')) "
                + " ORDER BY a.mt_no DESC "
                ;

            var data = db.Database.SqlQuery<d_material_info>(sql);

            var values = data.ToList().AsEnumerable().Select(x => new
            {
                ID = x.mtid,
                Type = x.mt_type,
                MT_NO = x.mt_no,
                MT_Code = x.mt_cd,
                LOT_Code = x.s_lot_no,
                Name = x.mt_nm,
                Group_Qty = x.gr_qty,
                Unit = x.unit_cd,
                Bundle_Unit = ((x.bundle_unit != null || x.bundle_unit != "") ? db.comm_dt.Where(m => m.mt_cd == "COM027" && m.dt_cd == x.bundle_unit && m.use_yn == "Y").Select(m => m.dt_nm) : null),
                Supplier = x.sp_cd,
                Manufacturer = x.mf_cd,
                Width = ((x.width != null) ? x.width : string.Empty),
                Length = ((x.spec != null) ? x.spec : string.Empty),
                Area = ((x.area != null) ? x.area : string.Empty),
                area_unit = x.area_unit,
                Price = ((x.price != null) ? x.price : string.Empty),
                Total_Price = ((x.tot_price != null) ? x.tot_price : string.Empty),
                Stickness = ((x.stick != null) ? x.stick : string.Empty),
                Thickness = ((x.thick != null) ? x.thick : string.Empty),
                Description = x.re_mark,
                QC_Code = x.item_vcd,
                QCName = ((x.item_vcd != null || x.item_vcd != "") ? db.qc_item_mt.Where(m => m.item_vcd == x.item_vcd).Select(m => m.item_nm) : null),
                QC_Range = ((x.qc_range_cd != null || x.qc_range_cd != "") ? db.comm_dt.Where(m => m.mt_cd == "COM017" && m.dt_cd == x.qc_range_cd && m.use_yn == "Y").Select(m => m.dt_nm) : null),
                Origin_MT_NO = x.mt_no_origin,
                Photo = x.photo_file,
                Create_Date = x.reg_dt,
                Create_User = x.reg_id,
                Change_Name = x.chg_id,
                Change_Date = x.chg_dt
            }).ToArray();

            String[] labelList = new string[29] { "ID", "Type", "MT NO", "MT Code ", "Lot Code", "Name", "Group Qty", "Unit", "Bundle Unit", "Supplier", "Manufacturer", "Width (mm)", "Length (M)", "Area(m²)", "area unit", "Price", "Total Price ($)", "Stickness(g)", "Thickness(µm)", "Description", "QC Code", "QC Name", "QC Range", "Origin MT NO", "Photo", "Create Date", "Create User", "Change Name", "Change Date" };

            Response.ClearContent();

            Response.AddHeader("content-disposition", "attachment;filename=MaterialManagement.htm");

            Response.AddHeader("Content-Type", "text/html");

            WriteHtmlTable(values, Response.Output, labelList);

            Response.End();

            return View("Material");
        }

        public JsonResult insertMaterialTemp(d_material_info d_material_info, List<ModelInsertMaterialTemp> insertmaterial_tmp)
        {
            var data_update = 0;
            var data_create = 0;
            var data_error = 0;
            foreach (var item in insertmaterial_tmp)
            {
                if (item.MT_No == null)
                {
                    data_error++;
                }
                else
                {
                    var mt_no = item.MT_No.ToString().Trim();
                    if (item.Type == "Cutting Material")
                    {
                        mt_no = "C" + mt_no + "-" + item.Width;
                    }
                    var mt_type = "";
                    switch (item.Type.ToString().Trim())
                    {
                        case "Cutting Material":
                            mt_type = "PMT";
                            break;

                        case "Main-Sub Material":
                            mt_type = "MMT";
                            break;

                        case "Adhesive Materials":
                            mt_type = "AM";
                            break;

                        default:
                            mt_type = "CMT";
                            break;
                    }

                    Int64 list1 = db.d_material_info.Count(x => x.mt_no == mt_no);
                    try
                    {
                        if (!db.supplier_info.Any(x => x.sp_cd == item.sp_cd))
                        {
                            data_error++;
                        }
                        else
                        {
                            if (list1 == 0)
                            {
                                d_material_info.del_yn = "N";
                                d_material_info.barcode = item.Barcode;
                                d_material_info.use_yn = "Y";
                                d_material_info.mt_type = mt_type;
                                d_material_info.mt_no = mt_no;
                                d_material_info.bundle_qty = item.bundle_qty;
                                d_material_info.mt_nm = item.Name;
                                d_material_info.sp_cd = item.sp_cd;
                                d_material_info.bundle_unit = item.bundle_unit;
                                d_material_info.width = item.Width;
                                d_material_info.width_unit = "mm";
                                d_material_info.spec = item.Length;
                                d_material_info.spec_unit = "M";
                                d_material_info.chg_dt = DateTime.Now;
                                d_material_info.reg_dt = DateTime.Now;
                                d_material_info.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                                d_material_info.reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                                //lenghth để có thể save được
                                d_material_info.consum_yn = "N";
                                d_material_info.thick_unit = "µm";
                                d_material_info.stick_unit = "g";
                                if (ModelState.IsValid)
                                {
                                    db.Entry(d_material_info).State = EntityState.Added;
                                    db.SaveChanges();
                                    data_create++;
                                }
                            }
                            else
                            {
                                data_update++;
                            }
                        }
                    }
                    catch (Exception)
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

        #endregion Material

        #region lot in material

        public ActionResult update_gr_wmt(double gr_qty, string id)
        {
            try
            {
                if (gr_qty != 0 && !(string.IsNullOrEmpty(id)) )
                {
                    //w_material_info_tam a = db.w_material_info_tam.Find(id);
                    //a.gr_qty = gr_qty;
                    //a.real_qty = gr_qty;
                    //a.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    ////a.chg_dt = DateTime.Now;
                    //db.Entry(a).State = EntityState.Modified;
                    //db.SaveChanges();
                    //return Json(new { result = true, kq = a }, JsonRequestBehavior.AllowGet);


                    w_material_info_tam item = new w_material_info_tam();
                    item.gr_qty = gr_qty;

                    item.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    item.chg_dt = DateTime.Now;
                    _IDomMS.UpdateQtyWMaterailTam(item, id);

                    return Json(new { result = true }, JsonRequestBehavior.AllowGet);

                   
                }
                return Json(new { result = false, kq = "Can not update" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, kq = e }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult update_gr_wmt_select(double gr_qty, string id)
        {
            try
            {
                if (gr_qty != 0 && id != null)
                {
                    w_material_info_tam item = new w_material_info_tam();
                    item.gr_qty = gr_qty;
                   
                    item.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    item.chg_dt = DateTime.Now;


                    //var succeess = 0;
                    //var a2 = id.TrimStart('[').TrimEnd(']').Split(',');
                    //var danhsach = new ArrayList();
                    //for (int i = 0; i < a2.Length; i++)
                    //{
                    //    var id2 = int.Parse(a2[i]);
                    //    w_material_info_tam a = db.w_material_info_tam.Find(id2);
                    //    a.gr_qty = gr_qty;
                    //    a.real_qty = gr_qty;
                    //    a.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    //    //a.chg_dt = DateTime.Now;
                    //    db.Entry(a).State = EntityState.Modified;
                    //    db.SaveChanges();
                    //    danhsach.Add(a);
                    //    succeess++;
                    //}

                    _IDomMS.UpdateQtyWMaterailTam(item, id);

                  
                 
                    return Json(new { result = true }, JsonRequestBehavior.AllowGet);
                   
                }
                return Json(new { result = false, kq = "Can not update" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, kq = e }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult getdatalist3_Supplier_QR_Management(string mt_no)
        {
            var danhsach = db.w_material_info_tam.Where(x => x.mt_cd.StartsWith(mt_no + "-CP-")).ToList();
            return Json(danhsach, JsonRequestBehavior.AllowGet);
            ;
        }

        public ActionResult pl_insertw_material(string mt_no, string barcode, double gr_qty, string expore_dt, string dt_of_receipt, string exp_input_dt, string lot_no, int send_qty, string mt_type, int bundle_qty, string bundle_unit)
        {
            try
            {
                if (mt_type == "CMT")
                {
                    return Json(new { result = false, kq = "Type Composite Can not Create" }, JsonRequestBehavior.AllowGet);
                }
                DateTime dt1 = DateTime.Now; //Today at 00:00:00
                string time_now = dt1.ToString("HHmmss");
                string date = dt1.ToString("yyyyMMdd").Remove(0, 2);
                StringBuilder varname1 = new StringBuilder();
                var userID = (Session["userid"] == null ? null : Session["userid"].ToString());
                expore_dt = expore_dt.Replace("-", "");
                dt_of_receipt = dt_of_receipt.Replace("-", "");
                exp_input_dt = exp_input_dt.Replace("-", "");
                gr_qty = (bundle_unit == "EA" ? bundle_qty : gr_qty);

                w_material_info_tam item = new w_material_info_tam();
                item.mt_no = mt_no;
                item.gr_qty = gr_qty;
                item.expore_dt = expore_dt;
                item.dt_of_receipt = dt_of_receipt;
                item.expiry_dt = exp_input_dt;
                item.lot_no = lot_no;
                item.mt_type = mt_type;
                item.reg_id = userID;

                 if(_IDomMS.InsertToWmaterialTam(item,barcode, send_qty, date, time_now) > 0)
                {
                  
                    IEnumerable<WMaterialTam> list=  _IDomMS.GetListTam(mt_no, date, time_now, send_qty);
                    return Json(new { result = true, data = list, message = "Tạo mã thành công"}, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { result = false, kq = "Không thể lưu trữ mã NVL này!" }, JsonRequestBehavior.AllowGet);
                }



                //varname1.Append("CALL `insert_lot_tam`('" + mt_no + "','" + gr_qty + "', '" + expore_dt + "', '" + dt_of_receipt + "', '" + exp_input_dt + "', '" + lot_no + "', '" + send_qty + "', '" + time_now1 + "', '" + time_now + "', '" + mt_type + "', '" + userID + "','" + barcode + "') \n");


                //var data = db.w_material_info_tam.SqlQuery(varname1.ToString()).ToList();

                
            }
            catch (Exception e)
            {
                return Json(new { result = false, kq = "Can not Create" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult PrintQRSupplier()
        {
            string[] keys = Request.Form.AllKeys;
            ViewData["Message"] = Request.Form[keys[0]];
            var id_mt = Request.Form[keys[5]];
            //find info 1 nguyen vat lieu
            var id = Convert.ToInt32(id_mt);
            db.Database.Connection.Open();
            string sql = "select * from d_material_info where mtid = @1 ";
            var find = db.Database.SqlQuery<d_material_info>(sql, new MySqlParameter("1", id)).FirstOrDefault();

            //  var find = db.d_material_info.Find(id);
            //
            ViewData["mt_nm"] = find.mt_nm;
            ViewData["barcode"] = find.barcode;
            ViewData["width"] = Request.Form[keys[1]];
            ViewData["spec"] = Request.Form[keys[3]];
            ViewData["bundle_qty"] = Request.Form[keys[2]];
            ViewData["bundle_unit"] = Request.Form[keys[4]];
            return View();
        }
        public ActionResult QRbarcodeSupplie(string wmtid)
        {
            var sql = new StringBuilder();
            sql.Append(" SELECT  a.* ")
              .Append("FROM  w_material_info_tam as a ")
               .Append("where a.wmtid in (" + wmtid + ") ");

            var data = db.w_material_info_tam.SqlQuery(sql.ToString()).ToList();
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        #endregion lot in material

        #region MachineMgt

        public ActionResult MachineMgt()
        {
            HttpCookie cookie = HttpContext.Request.Cookies["language"];
            if (cookie != null)
            {
                ViewBag.language = cookie.Value;
            }
            return View();
        }
     
        public ActionResult getMachineMgtData(d_machine_info d_machine_info)
        {
            try
            {
                var value = db.d_machine_info.OrderBy(x => x.mc_no).ToList();

                var machineMgtData = new { rows = value };
                return Json(machineMgtData, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string automc_no(int id)
        {
            if (id.ToString().Length < 2)
            {
                return string.Format("000000{0}", id);
            }

            if ((id.ToString().Length < 3) || (id.ToString().Length == 2))
            {
                return string.Format("00000{0}", id);
            }

            if ((id.ToString().Length < 4) || (id.ToString().Length == 3))
            {
                return string.Format("0000{0}", id);
            }

            if ((id.ToString().Length < 5) || (id.ToString().Length == 4))
            {
                return string.Format("000{0}", id);
            }
            if ((id.ToString().Length < 6) || (id.ToString().Length == 5))
            {
                return string.Format("00{0}", id);
            }
            if ((id.ToString().Length < 7) || (id.ToString().Length == 6))
            {
                return string.Format("0{0}", id);
            }
            if ((id.ToString().Length < 8) || (id.ToString().Length == 7))
            {
                return string.Format("{0}", id);
            }
            return string.Empty;
        }

        public ActionResult insertMachineMgt(d_machine_info d_machine_info, string mc_type, string mc_no, string purpose, string mc_nm, string re_mark)
        {
            //var mc_no_tmp = mc_type + mc_no;

            int count = db.d_machine_info.Where(item => item.mc_no == mc_no).ToList().Count();

            if (count > 0)
            {
                return Json(new { result = false, message = exits }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                DateTime reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                String dateString = reg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                d_machine_info.mc_type = mc_type;
                d_machine_info.mc_no = mc_no;
                d_machine_info.purpose = purpose;
                d_machine_info.mc_nm = mc_nm;
                d_machine_info.re_mark = re_mark;
                d_machine_info.reg_dt = reg_dt;
                d_machine_info.chg_dt = chg_dt;
                d_machine_info.barcode = mc_no;
                d_machine_info.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                d_machine_info.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();

                db.d_machine_info.Add(d_machine_info);
                db.SaveChanges();
                return Json(new { result = true, d_machine_info }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult updatetMachineMgt(int mno, string mc_type, string mc_no, string purpose, string mc_nm, string re_mark)
        {
            try
            {
                d_machine_info d_machine_info = db.d_machine_info.Find(mno);
                var countdb = db.d_machine_info.Count(x => x.mno == mno);
                if (countdb != 0)
                {
                    DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                    String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                    d_machine_info.mc_type = mc_type;
                    d_machine_info.mc_no = mc_no;
                    d_machine_info.purpose = purpose;
                    d_machine_info.mc_nm = mc_nm;
                    d_machine_info.re_mark = re_mark;
                    //d_machine_info.chg_dt = chg_dt;
                    d_machine_info.barcode = mc_no;
                    d_machine_info.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();

                    if (ModelState.IsValid)
                    {
                        db.Entry(d_machine_info).State = EntityState.Modified;
                        db.SaveChanges(); // line that threw exception
                        return Json(new { result = countdb }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    return Json(new { result = countdb }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = countdb }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public JsonResult deleteMachineMgt(int mno)
        {
            int count = db.d_machine_info.Where(x => x.mno == mno).ToList().Count();
            var result = new { result = true };
            if (count > 0)
            {
                d_machine_info d_machine_info = db.d_machine_info.Find(mno);
                db.d_machine_info.Remove(d_machine_info);
                db.SaveChanges();
                return Json(new { result = count }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { result = count }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult searchMachineMgt()
        {
            try
            {
                // Get Data from ajax function
                string mc_type_search = Request.QueryString["mc_type"];
                string mc_no_search = Request.QueryString["mc_no"];
                string mc_nm_search = Request.QueryString["mc_nm"];
                string start_search = Request.QueryString["start"];
                string end_search = Request.QueryString["end"];

                var dateConvert = new DateTime();
                if (DateTime.TryParse(end_search, out dateConvert))
                {
                    end_search = dateConvert.ToString("yyyy/MM/dd");
                }

                if (DateTime.TryParse(start_search, out dateConvert))
                {
                    start_search = dateConvert.ToString("yyyy/MM/dd");
                }

                StringBuilder sql = new StringBuilder();
                sql.Append(" SELECT a.* ")
                    .Append("FROM  d_machine_info as a ")
         .Append("Where ('" + mc_type_search + "'='' OR a.mc_type like '%" + mc_type_search + "%' )")
         .Append("AND ('" + mc_no_search + "'='' OR a.mc_no like '%" + mc_no_search + "%' )")
         .Append("AND ('" + mc_nm_search + "'='' OR a.mc_nm like '%" + mc_nm_search + "%')")
         .Append("AND ('" + start_search + "'='' OR DATE_FORMAT(a.reg_dt,'%Y/%m/%d') >= DATE_FORMAT('" + start_search + "','%Y/%m/%d'))")
         .Append("AND ('" + end_search + "'='' OR DATE_FORMAT(a.reg_dt,'%Y/%m/%d') <= DATE_FORMAT('" + end_search + "','%Y/%m/%d'))")
         .Append("ORDER BY a.chg_dt desc ");

                //var data = new DataTable();
                //using (var cmd = db.Database.Connection.CreateCommand())
                //{
                //    db.Database.Connection.Open();
                //    cmd.CommandText = sql.ToString();
                //    using (var reader = cmd.ExecuteReader())
                //    {
                //        data.Load(reader);
                //    }
                //}
                //db.Database.Connection.Close();
                //var result = GetJsonPersons3(data);
                return new InitMethods().ConvertDataTableToJsonAndReturn(sql);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public JsonResult getType_MachineMgt(comm_dt comm_dt)
        {
            try
            {
                var type = db.comm_dt.Where(x => x.mt_cd == "COM007").ToList();
                return Json(type, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public ActionResult _PrintQRMachine(string id)
        {
            ViewData["Message"] = id;
            return View();
        }

        public ActionResult PrintMultiMachineQRCode(string id)
        {
            var multiIDs = id.TrimStart('[').TrimEnd(']').Split(',');
            var row_data = new List<d_machine_info>();
            for (int i = 0; i < multiIDs.Length; i++)
            {
                var id2 = int.Parse(multiIDs[i]);
                var sql = new StringBuilder();
                sql.Append(" SELECT a.* ")
                   .Append(" from d_machine_info as a")
                   .Append(" where a.mno ='" + id2 + "'");

                var data = db.d_machine_info.SqlQuery(sql.ToString()).ToList<d_machine_info>();
                row_data.AddRange(data);
            }

            return Json(row_data, JsonRequestBehavior.AllowGet);
        }

        public JsonResult insertMachineExcel(d_machine_info d_machine_info, List<ModelInsertMachineExcel> ModelInsertMachineExcel)
        {
            var data_update = 0;
            var data_create = 0;
            var data_error = 0;
            var list = new ArrayList();
            if ((ModelInsertMachineExcel == null) && (ModelInsertMachineExcel?.Any() != true))
            {
                return Json(new { result = false, message = "Không thế lấy giữ liệu từ tệp excel hoặc tệp tải lên không có giữ liệu" }, JsonRequestBehavior.AllowGet);
            }

            if (ModelInsertMachineExcel.Count == 0)
            {
                return Json(new { result = false, message = "Không thế lấy giữ liệu từ tệp excel hoặc tệp tải lên không có giữ liệu" }, JsonRequestBehavior.AllowGet);
            };

            foreach (var item in ModelInsertMachineExcel)
            {
                //var style_no = item.style_no.ToString();
                var mc_type = string.IsNullOrEmpty(item.mc_type) ? "" : item.mc_type.ToString();
                var mc_no = item.mc_no;

                if (string.IsNullOrEmpty(item.mc_no))
                {
                    data_error++;
                    continue;
                    //return Json(new
                    //{
                    //    result = false,
                    //    message = "No QR Code has been selected !"
                    //}, JsonRequestBehavior.AllowGet);
                }

                var mc_nm = string.IsNullOrEmpty(item.mc_nm) ? "" : item.mc_nm.ToString();
                var purpose = string.IsNullOrEmpty(item.purpose) ? "" : item.purpose.ToString();

                //6,20,50,200

                int countList = db.d_machine_info.Count(x => x.mc_no == mc_no);
                try
                {
                    if (countList == 0)
                    {
                        mc_type = @"" + mc_type.Replace("\"", "") + "";
                        mc_no = @"" + mc_no.Replace("\"", "") + "";

                        mc_nm = @"" + mc_nm.Replace("\"", "") + "";

                        purpose = @"" + purpose.Replace("\"", "") + "";
                        var chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                        var reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();

                        var sql = "CALL sp_insertMachine_Excel(   ";

                        sql += "\"" + mc_type + "\" ,";
                        sql += "\"" + mc_no + "\", ";
                        sql += "\"" + mc_nm + "\" ,";
                        sql += "\"" + purpose + "\" ,";

                        sql += " '" + chg_id + "' ";

                        sql += ",'" + reg_id + "')";

                        StringBuilder stringBuilder = new StringBuilder(sql);

                        int result = new Excute_query().Execute_NonQuery(stringBuilder);

                        if (result > 0)
                        {
                            data_create++;

                            var sql_insert = new StringBuilder();

                            sql_insert.Append(" SELECT mno,mc_type,mc_no,mc_nm,purpose,re_mark,reg_id,chg_id, ")
                                .Append("chg_dt,reg_dt,'INSERT' AS STATUS FROM d_machine_info")
                                //.Append(" STR_TO_DATE(chg_dt,'%Y-%m-%d %H:%i:%s') chg_dt,STR_TO_DATE(reg_dt,'%Y-%m-%d %H:%i:%s') reg_dt, 'INSERT' AS STATUS FROM d_machine_info")
                                .Append(" WHERE mc_no='" + mc_no + "' ");

                            var list2 = new InitMethods().ConvertDataTableToList<ModelReturnMachineExcel>(sql_insert);
                            //StringBuilder updatemodel = new StringBuilder(" INSERT INTO d_model_info(md_cd, md_nm, reg_dt, chg_dt, use_yn, del_yn) SELECT md_cd, md_cd, NOW(), NOW(),'Y','N' FROM view_product_notduplication; ");
                            //new Excute_query().Execute_NonQuery(updatemodel);
                            list.Add(list2);
                        }
                    }
                    else
                    {
                        data_update++;
                    }
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
                data = list,
                data_error = data_error
            }, JsonRequestBehavior.AllowGet);
        }

        #endregion MachineMgt

        #region Mold

        public ActionResult MoldMgt()
        {
            return SetLanguage("");
        }
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //var lang = HttpContext.Request.Cookies["language"].Value;
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
        //public ActionResult MoldMgtData()
        //{
        //    var List = db.d_mold_info.OrderByDescending(x => x.chg_dt).ToList();
        //    var jsonData = new
        //    {
        //        rows = List
        //    };
        //    return Json(jsonData, JsonRequestBehavior.AllowGet);
        //}

        public ActionResult MoldMgtData(Pageing pageing)
        {
            var md_no = Request["md_no"];
            var md_nm = Request["md_nm"];
            //int pageIndex = pageing.page;
            //int pageSize = pageing.rows;
            //int start_r = pageing.page;
            //if (start_r > 1) {
            //    start_r = ((pageIndex - 1) * pageSize);
            //}
            //int end_r = (pageIndex * pageSize);

            //string order_by = "";
            //if (pageing.sidx != null) {
            //    order_by = " ORDER BY " + pageing.sidx + " " + pageing.sord;
            //}

            //if (pageing.sidx == null) {
            //    order_by = " ORDER BY MyDerivedTable.mdno DESC ";
            //}
            Dictionary<string, string> list = PagingAndOrderBy(pageing, " ORDER BY MyDerivedTable.mdno DESC ");
            string tempSql = " SELECT ROW_NUMBER() OVER (ORDER BY a.mdno DESC) AS RowNum, a.*  "
                + " FROM d_mold_info a  "
                + " WHERE ('" + md_no + "'='' OR  a.md_no LIKE '%" + md_no + "%' ) "
                + " AND ('" + md_nm + "'='' OR  a.md_nm LIKE '%" + md_nm + "%') "
                ;
            string countSql = " SELECT COUNT(*) FROM ( " + tempSql + " ) MyDerivedTable ";
            string viewSql = " SELECT * FROM ( " + tempSql + " ) MyDerivedTable "
                + " WHERE MyDerivedTable.RowNum BETWEEN " + list["start"] + " AND " + list["end"] + " "
                + list["orderBy"];
            return SearchAndPaging(countSql, viewSql, int.Parse(list["index"]), int.Parse(list["size"]));
            //var data = new DataTable();
            //using (var cmd = db.Database.Connection.CreateCommand()) {
            //    db.Database.Connection.Open();
            //    cmd.CommandText = viewSql;
            //    using (var reader = cmd.ExecuteReader()) {
            //        data.Load(reader);
            //    }
            //}

            //int totalRecords = db.Database.SqlQuery<int>(countSql).FirstOrDefault();

            //var values = ConvertDataTableToJson(data);

            //int totalPages = (int)Math.Ceiling((float)totalRecords / (float)pageSize);

            //var result = new {
            //    total = totalPages,
            //    page = pageIndex,
            //    records = totalRecords,
            //    rows = values.Data,
            //};

            //return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult InsertMold(d_mold_info d_mold_info, string md_no, string md_nm, string purpose, string re_mark)
        {
            int countlist = db.d_mold_info.Count(x => x.md_no == md_no);
            var bb_no_tmp = string.Empty;

            var listlast = db.d_mold_info.ToList().LastOrDefault();

            if (countlist == 0)
            {
                DateTime reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                String dateString = reg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");

                DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                d_mold_info.md_no = md_no;
                d_mold_info.md_nm = md_nm;
                d_mold_info.barcode = md_no;
                d_mold_info.purpose = purpose;
                d_mold_info.re_mark = re_mark;
                d_mold_info.reg_dt = reg_dt;
                d_mold_info.chg_dt = chg_dt;
                d_mold_info.reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                d_mold_info.del_yn = "N";

                d_mold_info.use_yn = "Y";

                if (ModelState.IsValid)
                {
                    db.d_mold_info.Add(d_mold_info);
                    db.SaveChanges();
                    return Json(new { result = countlist, kq = d_mold_info }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { result = countlist }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult UpdateMold(int mdno, string md_no, string md_nm, string purpose, string re_mark)
        {
            var countdMold = db.d_mold_info.Count(x => x.mdno == mdno);
            if (countdMold > 0)
            {
                var countdMoldItem = db.d_mold_info.FirstOrDefault(x => x.mdno == mdno);
                countdMoldItem.md_nm = md_nm;
                countdMoldItem.purpose = purpose;
                countdMoldItem.re_mark = re_mark;
                countdMoldItem.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                db.SaveChanges();
                return Json(new { result = countdMold, kq = countdMoldItem }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { result = countdMold }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult deleteMold(int mdno)
        {
            var countMold = db.d_mold_info.Count(x => x.mdno == mdno);
            if (countMold > 0)
            {
                var Molddata = db.d_mold_info.FirstOrDefault(x => x.mdno == mdno);
                db.d_mold_info.Remove(Molddata);
                db.SaveChanges();
                return Json(new { result = countMold }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { result = countMold }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ExportToExcelMold(string md_no, string md_nm)
        {
            string tempSql = " SELECT  a.*  "
                + " FROM d_mold_info a  "
                + " WHERE ('" + md_no + "'='' OR  a.md_no LIKE '%" + md_no + "%' ) "
                + " AND ('" + md_nm + "'='' OR  a.md_nm LIKE '%" + md_nm + "%') "
                + " ORDER BY a.mdno DESC "
                ;

            var data = db.Database.SqlQuery<d_mold_info>(tempSql);

            var records = data.ToList().AsEnumerable().Select(x => new
            {
                md_no = x.md_no,
                md_nm = x.md_nm,
                purpose = x.purpose,
                barcode = x.barcode,
                re_mark = x.re_mark,
                reg_id = x.reg_id,
                reg_dt = x.reg_dt,
                chg_id = x.chg_id,
                chg_dt = x.chg_dt
            }).ToArray();

            Response.ClearContent();
            Response.AddHeader("content-disposition", "attachment;filename=MoldManagement.xls");
            Response.AddHeader("Content-Type", "application/vnd.ms-excel");
            WriteMoldHtmlTable(records, Response.Output);
            Response.End();
            return View("MoldMgt");
        }

        //public JsonResult searchMold() {
        //    // Get Data from ajax function
        //    string s_md_no = Request.QueryString["s_md_no"];
        //    string s_md_nm = Request.QueryString["s_md_nm"];

        //    // Get data from model mb_info
        //    var MoldMgtData_List = db.d_mold_info.ToList();
        //    ArrayList row_data = new ArrayList();
        //    if (s_md_no == "") {
        //        if (s_md_nm == "") {
        //            int id_data = 1;
        //            foreach (var item in MoldMgtData_List) {
        //                var row_data_iteam = new {
        //                    mdno = item.mdno,
        //                    md_no = item.md_no,
        //                    md_nm = item.md_nm,
        //                    purpose = item.purpose,
        //                    re_mark = item.re_mark,
        //                    reg_dt = item.reg_dt,
        //                    chg_dt = item.chg_dt,

        //                };
        //                row_data.Add(row_data_iteam);
        //                id_data++;

        //            };
        //        }
        //        else {
        //            int id_data = 1;
        //            foreach (var item in MoldMgtData_List) {
        //                if (s_md_nm == item.md_nm || s_md_nm.ToLower() == item.md_nm.ToLower()) {
        //                    var row_data_iteam = new {
        //                        mdno = item.mdno,
        //                        md_no = item.md_no,
        //                        md_nm = item.md_nm,
        //                        purpose = item.purpose,
        //                        re_mark = item.re_mark,
        //                        reg_dt = item.reg_dt,
        //                        chg_dt = item.chg_dt,

        //                    };
        //                    row_data.Add(row_data_iteam);
        //                }
        //                id_data++;
        //            };
        //        }

        //    }
        //    else {
        //        int id_data = 1;
        //        foreach (var item in MoldMgtData_List) {
        //            if (s_md_no == item.md_no || s_md_no.ToLower() == item.md_no.ToLower()) {
        //                var row_data_iteam = new {
        //                    mdno = item.mdno,
        //                    md_no = item.md_no,
        //                    md_nm = item.md_nm,
        //                    purpose = item.purpose,
        //                    re_mark = item.re_mark,
        //                    reg_dt = item.reg_dt,
        //                    chg_dt = item.chg_dt,

        //                };
        //                row_data.Add(row_data_iteam);
        //            }
        //            id_data++;
        //        };

        //    }

        //    return Json(row_data, JsonRequestBehavior.AllowGet);
        //}

        #endregion Mold

        #region Develop

        public ActionResult DevelopCommon()
        {
            return SetLanguage("");
        }

        public ActionResult getDevelopCommon(comm_mt comm_mt)
        {
            try
            {
                var vaule = (from comm_mt_ in db.comm_mt
                             where comm_mt_.div_cd == "DEV"
                             select new
                             {
                                 mt_id = comm_mt_.mt_id,
                                 mt_cd = comm_mt_.mt_cd,
                                 mt_nm = comm_mt_.mt_nm,
                                 mt_exp = comm_mt_.mt_exp,
                                 use_yn = comm_mt_.use_yn,
                             }).ToList();
                var developCommonData = new
                {
                    rows = vaule
                };
                return Json(developCommonData, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public ActionResult getDevCommDtData(comm_dt comm_dt, comm_mt comm_mt, string mt_cd)
        {
            try
            {
                var vaule = (from comm_dt_ in db.comm_dt
                             join comm_mt_ in db.comm_mt
                                 on comm_dt_.mt_cd equals comm_mt_.mt_cd
                             where comm_dt_.mt_cd == mt_cd
                             select new
                             {
                                 cdid = comm_dt_.cdid,
                                 mt_cd = comm_mt_.mt_cd,
                                 mt_nm = comm_mt_.mt_nm,
                                 dt_cd = comm_dt_.dt_cd,
                                 dt_nm = comm_dt_.dt_nm,
                                 dt_exp = comm_dt_.dt_exp,
                                 dt_order = comm_dt_.dt_order,
                                 use_yn = comm_dt_.use_yn,
                             }).ToList();
                var devCommDtData = new
                {
                    rows = vaule
                };
                return Json(devCommDtData, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public ActionResult insertDevCommDt(comm_dt comm_dt, string dt_cd, string dt_nm, string dt_exp, string use_yn, string mt_cd, int dt_order)
        {
            try
            {
                int count = (from commdt in db.comm_dt where commdt.dt_cd == dt_cd && commdt.mt_cd == mt_cd select commdt).Count();  // count: 2
                if (count > 0)
                {
                    return View(comm_dt);
                }
                else
                {
                    DateTime reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                    String dateString = reg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");

                    DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                    String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");

                    comm_dt.mt_cd = mt_cd;
                    comm_dt.dt_cd = dt_cd;
                    comm_dt.dt_nm = dt_nm;
                    comm_dt.dt_exp = dt_exp;
                    comm_dt.dt_order = dt_order;
                    comm_dt.use_yn = use_yn;
                    comm_dt.reg_dt = reg_dt;
                    comm_dt.chg_dt = chg_dt;
                    comm_dt.del_yn = "N";
                    if (ModelState.IsValid)
                    {
                        db.Entry(comm_dt).State = EntityState.Added;
                        db.SaveChanges(); // line that threw exception
                        var result = new { result = true };
                        return Json(result, JsonRequestBehavior.AllowGet);
                    }
                    return View(comm_dt);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public JsonResult insertDevelopCommon(comm_mt comm_mt, string mt_cd, string mt_nm, string mt_exp, string use_yn)
        {
            try
            {
                var comm_mt_list = db.comm_mt.Where(item => item.div_cd == "DEV").ToList();
                ArrayList row_data = new ArrayList();
                int id_data = 0;
                foreach (var item in comm_mt_list)
                {
                    var row_data_iteam = new
                    {
                        //id = Convert.ToString(id_data),
                        mt_cd = item.mt_cd,
                        mt_nm = item.mt_nm,
                        mt_exp = item.mt_exp,
                        use_yn = item.use_yn,
                        reg_id = item.reg_id,
                        reg_dt = Convert.ToString(item.reg_dt),
                        chg_id = item.chg_id,
                        chg_dt = Convert.ToString(item.chg_dt),
                    };
                    row_data.Add(row_data_iteam);
                    id_data++;
                };
                if (id_data == 0)
                {
                    mt_cd = "DEV001";
                }
                else
                {
                    id_data++;
                    if (id_data.ToString().Length == 1)
                    {
                        mt_cd = "DEV00" + id_data.ToString();
                    }
                    if (id_data.ToString().Length == 2)
                    {
                        mt_cd = "DEV0" + id_data.ToString();
                    }
                    if (id_data.ToString().Length == 3)
                    {
                        mt_cd = "DEV" + id_data.ToString();
                    }
                }
                id_data++;
                if (db.comm_mt.Any(item => ((item.mt_cd == mt_cd))) == false)
                {
                    comm_mt.mt_cd = mt_cd;
                }
                else
                {
                    if (id_data.ToString().Length == 1)
                    {
                        mt_cd = "DEV00" + id_data.ToString();
                    }
                    if (id_data.ToString().Length == 2)
                    {
                        mt_cd = "DEV0" + id_data.ToString();
                    }
                    if (id_data.ToString().Length == 3)
                    {
                        mt_cd = "DEV" + id_data.ToString();
                    }
                }
                DateTime reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                String dateString = reg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");

                DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");

                comm_mt.mt_cd = mt_cd;
                comm_mt.div_cd = "DEV";
                comm_mt.mt_nm = mt_nm;
                comm_mt.mt_exp = mt_exp;
                comm_mt.use_yn = use_yn;
                comm_mt.reg_dt = reg_dt;
                comm_mt.chg_dt = chg_dt;
                if (ModelState.IsValid)
                {
                    db.comm_mt.Add(comm_mt);
                    db.SaveChanges();
                }
                return Json(row_data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public ActionResult updateDevCommDt(int cdid, string dt_cd, string dt_nm, string dt_exp, string use_yn, string mt_cd, int dt_order)
        {
            try
            {
                comm_dt comm_dt = db.comm_dt.Find(cdid);

                DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                comm_dt.mt_cd = mt_cd;
                comm_dt.dt_cd = dt_cd;
                comm_dt.dt_nm = dt_nm;
                comm_dt.dt_exp = dt_exp;
                comm_dt.dt_order = dt_order;
                comm_dt.use_yn = use_yn;
                //comm_dt.reg_dt = reg_dt;
                //comm_dt.chg_dt = chg_dt;
                if (ModelState.IsValid)
                {
                    db.Entry(comm_dt).State = EntityState.Modified;
                    db.SaveChanges(); // line that threw exception
                    var result = new { result = true };
                    return Json(result, JsonRequestBehavior.AllowGet);
                }
                return View(comm_dt);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public ActionResult updateDevelopCommon(int mt_id, string mt_cd, string mt_nm, string mt_exp, string use_yn)
        {
            try
            {
                comm_mt comm_mt = db.comm_mt.Find(mt_id);

                DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");

                comm_mt.mt_cd = mt_cd;
                comm_mt.mt_nm = mt_nm;
                comm_mt.mt_exp = mt_exp;
                comm_mt.use_yn = use_yn;
                //comm_mt.reg_dt = reg_dt;
                //comm_mt.chg_dt = chg_dt;
                if (ModelState.IsValid)
                {
                    db.Entry(comm_mt).State = EntityState.Modified;
                    db.SaveChanges(); // line that threw exception

                    var result = new { result = true };
                    return Json(result, JsonRequestBehavior.AllowGet);
                }
                return View(comm_mt);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        public JsonResult deleteDevelopCommon(string Developcommonmaincode)
        {
            comm_mt common_data = new comm_mt();
            var result = db.comm_mt.Count(x => x.mt_cd == Developcommonmaincode);
            if (result > 0)
            {
                var common = db.comm_mt.FirstOrDefault(x => x.mt_cd == Developcommonmaincode);
                db.comm_mt.Remove(common);
                db.SaveChanges();

                var countcommondt = db.comm_dt.Count(x => x.mt_cd == Developcommonmaincode);
                for (int i = 1; i <= countcommondt; i++)
                {
                    var commondt = db.comm_dt.FirstOrDefault(x => x.mt_cd == Developcommonmaincode);
                    db.comm_dt.Remove(commondt);
                    db.SaveChanges();
                };
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult deleteDevelopCommondt(string Developcommondt_manincode, string Developcommondt_dtcd)
        {
            var countcommondt = db.comm_dt.Count(x => x.mt_cd == Developcommondt_manincode && x.dt_cd == Developcommondt_dtcd);
            if (countcommondt > 0)
            {
                var commondt = db.comm_dt.FirstOrDefault(x => x.mt_cd == Developcommondt_manincode && x.dt_cd == Developcommondt_dtcd);
                db.comm_dt.Remove(commondt);
                db.SaveChanges();
            }
            return Json(countcommondt, JsonRequestBehavior.AllowGet);
        }

        #endregion Develop

        #region ProductMgt

        public ActionResult ProductMgt()
        {
            return SetLanguage("");
        }
        public JsonResult GetProductForBuyer(string product)
        {
            //if (string.IsNullOrEmpty(product))
            //{
            //    product = "";
            //}
            //var sql = new StringBuilder();
            //sql.Append($" SELECT  * ")
            //    .Append($" FROM  d_style_info as s ")
            //    .Append($" WHERE  s.style_no like  '%{product}%' ")
            //    .Append($" AND ( s.stamp_code IS NOT NULL  && s.stamp_code <> '' ) ")
            //    .Append($" order by s.sid desc  ");
            //return new InitMethods().JsonResultAndMessageFromQuery(sql, "");


            // Get Data from ajax function
            var code = Request["style_no"];
            var code_nm = Request["style_nm"];
            var modecode = Request["md_cd"];
            var projectname = Request.QueryString["prj_nm"];
            var start = Request.QueryString["start"];
            var end = Request.QueryString["end"];
            var dateConvert = new DateTime();
            if (DateTime.TryParse(end, out dateConvert))
            {
                end = dateConvert.ToString("yyyy/MM/dd");
            }

            if (DateTime.TryParse(start, out dateConvert))
            {
                start = dateConvert.ToString("yyyy/MM/dd");
            }
            var sql = new StringBuilder();

            sql.Append(" SELECT (SELECT dt_nm FROM comm_dt WHERE a.part_nm = dt_cd && mt_cd = 'DEV003') AS part_name,a.* ")
            .Append("FROM  d_style_info as a ")
            .Append("Where ('" + code + "'='' OR  a.style_no like '%" + code + "%' )")
            .Append(" AND ( a.stamp_code IS NOT NULL  && a.stamp_code <> '' ) ")
            .Append("AND ('" + code_nm + "'='' OR  a.style_nm like '%" + code_nm + "%' )")
            .Append("AND ('" + modecode + "'='' OR  a.md_cd like '%" + modecode + "%' )")
            .Append("AND ('" + projectname + "'='' OR  a.prj_nm like '%" + projectname + "%' )")
            .Append("AND ('" + start + "'='' OR DATE_FORMAT(a.reg_dt,'%Y/%m/%d') >= DATE_FORMAT('" + start + "','%Y/%m/%d'))")
            .Append("AND ('" + end + "'='' OR DATE_FORMAT(a.reg_dt,'%Y/%m/%d') <= DATE_FORMAT('" + end + "','%Y/%m/%d'))")
            .Append("ORDER BY a.chg_dt desc ");

            return new InitMethods().ConvertDataTableToJsonAndstring(sql.ToString());

        }
        public JsonResult GetProduct(string product)
        {
            if (string.IsNullOrEmpty(product))
            {
                product = "";
            }
            var sql = new StringBuilder();
            sql.Append($" SELECT  s.style_no, s.md_cd, s.pack_amt, s.stamp_code")
                .Append($" FROM  d_style_info as s ")
                .Append($" WHERE  s.style_no like  '%{product}%' ")
                .Append($" AND ( s.stamp_code IS NOT NULL  && s.stamp_code <> '' ) ")
                .Append($" order by s.sid desc  ");
            return new InitMethods().JsonResultAndMessageFromQuery(sql, "");
        }

        public JsonResult GetStyleMgt()
        {
            var sql = new StringBuilder();
            sql.Append(@"SELECT s.sid,s.ssver, s.reg_id,s.chg_id,s.reg_dt,s.chg_dt,s.stamp_code,m.stamp_name,
                s.style_no, s.style_nm, s.md_cd, s.prj_nm, s.pack_amt,s.expiry_month,s.expiry,s.part_nm ,s.drawingname, s.loss  , s.Description,
                productType
                FROM d_style_info AS s 
                LEFT JOIN stamp_master AS m ON s.stamp_code = m.stamp_code
               -- LEFT JOIN comm_dt AS c ON s.part_nm =  c.dt_cd AND c.mt_cd ='DEV003'
                ORDER BY s.sid desc ");
            return new InitMethods().ConvertDataTableToJsonAndstring(sql.ToString());
        }

        public ActionResult CreateStyleMgt(d_style_info d_style_info, string style_no )
        {
            string trimmed = String.Concat(style_no.Where(c => !Char.IsWhiteSpace(c)));
            var countdb = db.d_style_info.Count(x => x.style_no == trimmed);
            if (countdb == 0)
            {
                d_style_info.style_no = trimmed.ToUpper();
                d_style_info.use_yn = "Y";
                d_style_info.del_yn = "N";
                d_style_info.reg_dt = DateTime.Now;
                d_style_info.chg_dt = DateTime.Now;
                if (string.IsNullOrEmpty(d_style_info.expiry_month))
                {
                    d_style_info.expiry_month = "0";
                }
               

                d_style_info.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();

                int id =   _IDomMS.InsertToStyleInfo(d_style_info);
                d_style_info.sid = id;
                return Json(new { result = true, kq = d_style_info }, JsonRequestBehavior.AllowGet);
               
            }
            return Json(new { result = false, message = "Đã tồn tại!!!" }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ModifyStyleMgt(int sid, string style_no, string style_nm, string md_cd, string ssver, string part_nm, string prj_nm, string standard, string cust_rev, string order_num, int pack_amt, string cav, string bom_type, string tds_no, string use_yn, string del_yn, string item_vcd, string qc_range, string stamp_code, string expiry_month, string expiry, string drawingname, string loss, string Description
            ,string productType
            )
        {
            //if (!string.IsNullOrEmpty(expiry_month))
            //{
            //    if (!Int32.TryParse(expiry_month, out int j))
            //    {
            //        return Json(new { result = false, message = "Số tháng hết hạn (Nhập số, không nhập chữ)." }, JsonRequestBehavior.AllowGet);
            //    }
            //}
            try
            {
                int countdb = db.d_style_info.Count(x => x.style_no == style_no);
                if (countdb > 0)
                {
                    d_style_info d_style_info = db.d_style_info.Find(sid);
                    d_style_info.style_no = style_no.ToUpper();
                    d_style_info.style_nm = style_nm;
                    d_style_info.md_cd = md_cd;
                    d_style_info.ssver = ssver;
                    d_style_info.prj_nm = prj_nm;
                    d_style_info.part_nm = part_nm;
                    d_style_info.standard = standard;
                    d_style_info.cust_rev = cust_rev;
                    d_style_info.order_num = order_num;
                    d_style_info.pack_amt = pack_amt;
                    d_style_info.cav = cav;
                    d_style_info.bom_type = bom_type;
                    d_style_info.tds_no = tds_no;
                    d_style_info.item_vcd = item_vcd;
                    d_style_info.qc_range_cd = qc_range;
                    d_style_info.drawingname = drawingname;
                    d_style_info.loss = loss;
                    d_style_info.Description = Description;
                    d_style_info.productType = productType;
                    d_style_info.use_yn = "Y";
                    d_style_info.del_yn = "N";
                    d_style_info.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    d_style_info.chg_dt = DateTime.Now;

                    if (!string.IsNullOrEmpty(stamp_code))
                    {
                        d_style_info.stamp_code = stamp_code.Trim();
                    }
                    if (!string.IsNullOrEmpty(expiry_month))
                    {
                        d_style_info.expiry_month = expiry_month;
                    }
                    else
                    {
                        d_style_info.expiry_month = "0";
                    }
                    if (!string.IsNullOrEmpty(expiry))
                    {
                        d_style_info.expiry = expiry;
                    }
                    else
                    {
                        d_style_info.expiry = "";
                    }


                   _IDomMS.UpdateToStyleInfo(d_style_info);
                    //if (ModelState.IsValid)
                    //{
                    //    db.Entry(d_style_info).State = EntityState.Modified;
                    //    db.SaveChanges();
                    //    return Json(d_style_info, JsonRequestBehavior.AllowGet);
                    //}
                    return Json(new { result = true, message = "Sửa thành công", d_style_info }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {

                return Json(new { result = false,message = "Lỗi hệ thống" }, JsonRequestBehavior.AllowGet);
            }
            
        }

        public JsonResult searchStyle()
        {
            // Get Data from ajax function
            var code = Request["style_no"];
            var code_nm = Request["style_nm"];
            var modecode = Request["md_cd"];
            var projectname = Request.QueryString["prj_nm"];
            var start = Request.QueryString["start"];
            var end = Request.QueryString["end"];
            var dateConvert = new DateTime();
            if (DateTime.TryParse(end, out dateConvert))
            {
                end = dateConvert.ToString("yyyy/MM/dd");
            }

            if (DateTime.TryParse(start, out dateConvert))
            {
                start = dateConvert.ToString("yyyy/MM/dd");
            }
            var sql = new StringBuilder();

            sql.Append(" SELECT s.sid,s.ssver, s.loss, s.reg_id,s.chg_id,s.reg_dt,s.chg_dt,s.stamp_code,m.stamp_name,s.part_nm ,")
             .Append("   s.style_no, s.style_nm, s.md_cd, s.prj_nm, s.pack_amt, s.expiry_month, s.expiry, drawingname, s.Description, productType")
            .Append(" FROM  d_style_info as s ")
            .Append(" LEFT JOIN stamp_master AS m ON s.stamp_code = m.stamp_code ")
            //.Append(" LEFT JOIN comm_dt AS c ON s.part_nm = c.dt_cd AND c.mt_cd = 'DEV003'")
            .Append(" Where ('" + code + "'='' OR  s.style_no like '%" + code + "%' )")
            .Append(" AND ('" + code_nm + "'='' OR  s.style_nm like '%" + code_nm + "%' )")
            .Append(" AND ('" + modecode + "'='' OR  s.md_cd like '%" + modecode + "%' )")
            .Append(" AND ('" + projectname + "'='' OR  s.prj_nm like '%" + projectname + "%' )")
            .Append(" AND ('" + start + "'='' OR DATE_FORMAT(s.reg_dt,'%Y/%m/%d') >= DATE_FORMAT('" + start + "','%Y/%m/%d'))")
            .Append(" AND ('" + end + "'='' OR DATE_FORMAT(s.reg_dt,'%Y/%m/%d') <= DATE_FORMAT('" + end + "','%Y/%m/%d'))")
            .Append(" ORDER BY s.chg_dt desc ");

            return new InitMethods().ConvertDataTableToJsonAndstring(sql.ToString());
        }

        public JsonResult searchModeCode()
        {
            // Get Data from ajax function
            var code = Request["md_cd"];
            var modecode = Request["md_nm"];

            var sql = new StringBuilder();
            sql.Append(" SELECT *")
           .Append("FROM  d_model_info as a ")
           .Append("Where ('" + code + "'='' OR  a.md_cd like '%" + code + "%' )")
           .Append("AND ('" + modecode + "'='' OR  a.md_nm like '%" + modecode + "%' )");
            return new InitMethods().ConvertDataTableToJsonAndstring(sql.ToString());
        }

        public ActionResult deleteStyle(d_style_info d_style_info, int sid)
        {
            var countdb = db.d_style_info.Count(x => x.sid == sid);
            if (countdb != 0)
            {
                var product_data = db.d_style_info.FirstOrDefault(x => x.sid == sid);
                db.d_style_info.Remove(product_data);
                db.SaveChanges();
                return Json(new { result = countdb }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { result = countdb }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetModelcode()
        {
            var modelcode = db.d_model_info.ToList();
            return Json(new { rows = modelcode }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetSSVer(comm_mt comm_dt)
        {
            var ssver = db.comm_dt.ToList().Where(item => item.mt_cd == "DEV002").ToList();
            return Json(ssver, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetParkNm(comm_mt comm_dt)
        {
            var parknm = db.comm_dt.ToList().Where(item => item.mt_cd == "DEV003").ToList();
            return Json(parknm, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetStandard(comm_mt comm_dt)
        {
            var standard = db.comm_dt.ToList().Where(item => item.mt_cd == "DEV004").ToList();
            return Json(standard, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetCustRev(comm_mt comm_dt)
        {
            var custrev = db.comm_dt.ToList().Where(item => item.mt_cd == "DEV005").ToList();
            return Json(custrev, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetOrder(comm_mt comm_dt)
        {
            var order = db.comm_dt.ToList().Where(item => item.mt_cd == "DEV006").ToList();
            return Json(order, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetType(comm_mt comm_dt)
        {
            var type = db.comm_dt.ToList().Where(item => item.mt_cd == "COM004" && item.use_yn == "Y").ToList();
            return Json(type, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetTdsno(comm_mt comm_dt)
        {
            var tdsno = db.comm_dt.ToList().Where(item => item.mt_cd == "DEV008").ToList();
            return Json(tdsno, JsonRequestBehavior.AllowGet);
        }

        //End Style management

        public JsonResult insertProductExcel(d_style_info d_style_info, List<ModelInsertProductExcel> ModelInsertProductExcel)
        {
            var data_update = 0;
            var data_create = 0;
            var data_error = 0;
            var list = new ArrayList();
            if ((ModelInsertProductExcel == null) && (ModelInsertProductExcel?.Any() != true))
            {
                return Json(new { result = false, message = "Không thế lấy dữ liệu từ tệp excel hoặc tệp tải lên không có dữ liệu" }, JsonRequestBehavior.AllowGet);
            }

            if (ModelInsertProductExcel.Count == 0)
            {
                return Json(new { result = false, message = "Không thế lấy dữ liệu từ tệp excel hoặc tệp tải lên không có dữ liệu" }, JsonRequestBehavior.AllowGet);
            };

            foreach (var item in ModelInsertProductExcel)
            {
                var style_no = item.style_no;

                if (string.IsNullOrEmpty(item.style_no))
                {
                    data_error++;
                    continue;
                }

                var style_nm = string.IsNullOrEmpty(item.style_nm) ? "" : item.style_nm.ToString();
                //var list_d_model_info = db.d_model_info.ToList();

                //foreach (var comm in list_d_model_info)
                //{
                //    if (item.md_cd.Trim() == comm.md_cd.Trim())
                //    {
                //        item.md_cd = comm.md_cd;
                //        break;
                //    }
                //};

                var md_cd = string.IsNullOrEmpty(item.md_cd) ? "" : item.md_cd.ToString();

                //item.md_cd.ToString();
                var prj_nm = string.IsNullOrEmpty(item.prj_nm) ? "" : item.prj_nm.ToString();

                var stamp_code = string.IsNullOrEmpty(item.stamp_code) ? "" : item.stamp_code.ToString();

                var pack_amt = item.pack_amt;
                string expiry = "";
                string expiry_month = "";

                var hsd = string.IsNullOrEmpty(item.expiry) ? "" : item.expiry.ToString();
                var isNumeric = int.TryParse(hsd, out int n);
                if (isNumeric)
                {
                    expiry_month = hsd;
                }
                else
                {
                    expiry = hsd;
                }
                int countlist = db.d_style_info.Count(x => x.style_no == style_no);
                try
                {
                    if (countlist == 0)
                    {
                        style_no = @"" + style_no.Replace("\"", "") + "";
                        style_nm = @"" + style_nm.Replace("\"", "") + "";

                        md_cd = @"" + md_cd.Replace("\"", "") + "";

                        prj_nm = @"" + prj_nm.Replace("\"", "") + "";

                        //pack_amt = @"" + pack_amt.Replace("\"", "") + "";

                        var chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                        var reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();

                        var sql = "CALL sp_insertproduct_excel(   ";

                        sql += "\"" + style_no + "\" ,";
                        sql += "\"" + style_nm + "\", ";
                        sql += "\"" + md_cd + "\" ,";
                        sql += "\"" + prj_nm + "\" ,";
                        sql += " '" + pack_amt + "' ";

                        sql += ",'" + chg_id + "' ";

                        sql += ",'" + reg_id + "'";
                        sql += ",'" + expiry_month + "'";
                        sql += ",'" + expiry + "'";
                        sql += ",'" + stamp_code + "')";
                        StringBuilder stringBuilder = new StringBuilder(sql);

                        int result = new Excute_query().Execute_NonQuery(stringBuilder);

                        if (result > 0)
                        {
                            data_create++;

                            var sql_insert = new StringBuilder();

                            sql_insert.Append(" SELECT s.sid,s.style_no,s.style_nm,s.md_cd,s.prj_nm,s.pack_amt,s.chg_id,CONVERT(s.chg_dt,DATETIME) chg_dt,s.reg_id,CONVERT(s.reg_dt,DATETIME) reg_dt, 'INSERT' AS STATUS ")
                                .Append(" FROM  d_style_info as s ")
                                .Append(" left join comm_dt as m on s.qc_range_cd = m.dt_cd and m.mt_cd = 'COM017' WHERE s.style_no='" + style_no + "' ")
                                .Append(" order by s.sid desc  ");
                            var list2 = new InitMethods().ConvertDataTableToList<ModelReturnProductExcel>(sql_insert);
                            StringBuilder updatemodel = new StringBuilder(" INSERT INTO d_model_info(md_cd, md_nm, reg_dt, chg_dt, use_yn, del_yn) SELECT md_cd, md_cd, NOW(), NOW(),'Y','N' FROM view_product_notduplication; ");
                            new Excute_query().Execute_NonQuery(updatemodel);
                            list.Add(list2);
                        }
                    }
                    else
                    {
                        data_update++;

                        //style_no = @"" + style_no.Replace("\"", "") + "";
                        //style_nm = @"" + style_nm.Replace("\"", "") + "";

                        //md_cd = @"" + md_cd.Replace("\"", "") + "";
                        //prj_nm = @"" + prj_nm.Replace("\"", "") + "";

                        //var chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();

                        //var sql = "CALL sp_updateproduct_excel(   ";

                        //sql += "\"" + style_no + "\" ,";
                        //sql += "\"" + style_nm + "\", ";
                        //sql += "\"" + md_cd + "\" ,";
                        //sql += "\"" + prj_nm + "\" ,";
                        //sql += " '" + pack_amt + "' ";
                        //sql += ",'" + chg_id + "')";
                        //StringBuilder stringBuilder = new StringBuilder(sql);

                        //int result = new Excute_query().Execute_NonQuery(stringBuilder);

                        //if (result > 0)
                        //{
                        //    data_update++;

                        //    var sql_update = new StringBuilder();

                        //    sql_update.Append(" SELECT s.sid,s.style_no,s.style_nm,s.md_cd,s.prj_nm,s.pack_amt,s.chg_id,CONVERT(s.chg_dt,DATETIME) chg_dt,s.reg_id,CONVERT(s.reg_dt,DATETIME) reg_dt, 'UPDATE' AS STATUS ")
                        //        .Append(" FROM  d_style_info as s ")
                        //        .Append(" left join comm_dt as m on s.qc_range_cd = m.dt_cd and m.mt_cd = 'COM017' WHERE s.style_no='" + style_no + "' ")
                        //        .Append(" order by s.sid desc  ");
                        //    var list2 = new InitMethods().ConvertDataTableToList<ModelReturnProductExcel>(sql_update);

                        //    list.Add(list2);

                        //}
                    }
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
                data = list,
                data_error = data_error
            }, JsonRequestBehavior.AllowGet);
        }

        #endregion ProductMgt

        #region ModelManagement

        public ActionResult ModelManagement()
        {
            return SetLanguage("");
        }

        public ActionResult getModelMgt()
        {
            var vaule = db.d_model_info.OrderByDescending(x => x.chg_dt).ToList();
            return Json(new { rows = vaule }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult insertModelMgt(d_model_info dminfo, string md_cd, string md_nm, string use_yn)
        {
            var countdb = db.d_model_info.Count(x => x.md_cd == md_cd);
            if (countdb == 0)
            {
                DateTime reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                dminfo.md_cd = md_cd;
                dminfo.md_nm = md_nm;
                dminfo.use_yn = use_yn;
                dminfo.del_yn = "N";
                dminfo.reg_dt = reg_dt;
                dminfo.chg_dt = chg_dt;
                if (ModelState.IsValid)
                {
                    db.d_model_info.Add(dminfo);
                    db.SaveChanges();
                    return Json(new { result = true, data = dminfo }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
            return View();
        }

        public ActionResult updateModelMgt(int mdid, string md_cd, string md_nm, string use_yn)
        {
            var countdb = db.d_model_info.Count(x => x.md_cd == md_cd);
            if (countdb != 0)
            {
                d_model_info dminfo = db.d_model_info.Find(mdid);

                DateTime chg_dt = DateTime.Now;

                dminfo.md_cd = md_cd;
                dminfo.md_nm = md_nm;
                dminfo.use_yn = use_yn;
                dminfo.del_yn = "N";
                //dminfo.chg_dt = chg_dt;
                if (ModelState.IsValid)
                {
                    db.Entry(dminfo).State = EntityState.Modified;
                    db.SaveChanges();
                    var datalist = db.d_model_info.Where(x => x.mdid == mdid).ToList();
                    return Json(new { result = true, data = datalist }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
            return View();
        }

        public JsonResult insertModelExcel(d_model_info d_model_info, List<ModelInsertModelExcel> insertModel_Excel)
        {
            if ((insertModel_Excel == null) && (insertModel_Excel?.Any() != true))
            {
                return Json(new { result = false, message = "Không thế lấy giữ liệu từ tệp excel hoặc tệp tải lên không có giữ liệu" }, JsonRequestBehavior.AllowGet);
            }

            if (insertModel_Excel.Count == 0)
            {
                return Json(new { result = false, message = "Không thế lấy giữ liệu từ tệp excel hoặc tệp tải lên không có giữ liệu" }, JsonRequestBehavior.AllowGet);
            };
            var data_update = 0;
            var data_create = 0;
            var data_error = 0;
            var list = new ArrayList();
            foreach (var item in insertModel_Excel)
            {
                var md_cd = item.code;

                if (string.IsNullOrEmpty(md_cd))
                {
                    data_error++;
                    continue;
                }
                var md_nm = string.IsNullOrEmpty(item.name) ? "" : item.name.ToString();

                int list1 = db.d_model_info.Count(x => x.md_cd == md_cd);
                try
                {
                    if (list1 == 0)
                    {
                        DateTime reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                        DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                        md_cd = @"" + md_cd.Replace("\"", "") + "";

                        md_nm = @"" + md_nm.Replace("\"", "") + "";
                        var chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                        var reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();

                        var sql = "CALL sp_insertmodel_excel(   ";

                        sql += "\"" + md_cd + "\" ,";
                        sql += "\"" + md_nm + "\"";

                        sql += ",'" + chg_id + "' ";

                        sql += ",'" + reg_id + "')";
                        StringBuilder stringBuilder = new StringBuilder(sql);

                        int result = new Excute_query().Execute_NonQuery(stringBuilder);

                        if (result > 0)
                        {
                            data_create++;

                            var sql_insert = new StringBuilder();

                            sql_insert.Append(" SELECT a.mdid,a.md_cd,a.md_nm,a.use_yn,a.reg_id,CONVERT(a.reg_dt,DATETIME) reg_dt,a.chg_id,CONVERT(a.chg_dt,DATETIME) chg_dt,'INSERT' AS STATUS ")
         .Append(" FROM d_model_info a ")
          .Append(" WHERE a.md_cd='" + md_cd + "' ")
          .Append(" ORDER BY a.chg_dt ");

                            var list2 = new InitMethods().ConvertDataTableToList<ModelReturnModelExcel>(sql_insert);

                            list.Add(list2);
                        }

                        //using (var cmd = db.Database.Connection.CreateCommand())
                        //{
                        //    //string sql = " UPDATE els SET tbstatus='" + status + "' WHERE tagno='" + tagno + "' ";

                        //    db.Database.Connection.Open();
                        //    cmd.CommandText = sql.ToString();
                        //    cmd.ExecuteNonQuery();
                        //    db.Database.Connection.Close();
                        //    data_create++;
                        //}
                    }
                    else
                    {
                        data_update++;
                        //DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                        //md_nm = @"" + md_nm.Replace("\"", "") + "";
                        //var chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                        ////var reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();

                        //var sql = "CALL sp_updatemodel_excel(   ";

                        //sql += "\"" + md_cd + "\" ,";
                        //sql += "\"" + md_nm + "\"";

                        ////sql += ",'" + chg_id + "' ";

                        //sql += ",'" + chg_id + "')";
                        //StringBuilder stringBuilder = new StringBuilder(sql);

                        //int result = new Excute_query().Execute_NonQuery(stringBuilder);

                        //if (result > 0)
                        //{
                        //    data_update++;
                        //    var sql_update = new StringBuilder();

                        //    sql_update.Append(" SELECT a.mdid,a.md_cd,a.md_nm,a.use_yn,a.reg_id,CONVERT(a.reg_dt,DATETIME) reg_dt,a.chg_id,CONVERT(a.chg_dt,DATETIME) chg_dt,'UPDATE' AS STATUS ")
                        //        .Append(" FROM d_model_info a ")
                        //        .Append(" WHERE a.md_cd='" + md_cd + "' ")
                        //        .Append(" ORDER BY a.chg_dt ");

                        //    var list2 = new InitMethods().ConvertDataTableToList<ModelReturnModelExcel>(sql_update);

                        //    list.Add(list2);
                        //}
                    }
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
                data_error = data_error,
                data = list
            }, JsonRequestBehavior.AllowGet);
        }

        public class ModelMgt
        {
            public int mdid { get; set; }
            public string md_cd { get; set; }
            public string md_nm { get; set; }
            public string use_yn { get; set; }
            public string del_yn { get; set; }
            public string reg_id { get; set; }
            public string reg_dt { get; set; }
            public string chg_id { get; set; }
            public string chg_dt { get; set; }
            public string RowNum { get; set; }
        }

        public JsonResult searchModelMgt(Pageing pageing, string md_cd, string md_nm)
        {
            var order = "";
            if (pageing.sidx != null)
            {
                order = "order by " + pageing.sidx + " " + pageing.sord;
            }

            var sql = new StringBuilder();
            sql.Append(" SELECT a.* ")
                .Append("FROM d_model_info as a ")
                 .Append("WHERE ('" + md_cd + "'='' OR a.md_cd like '%" + md_cd + "%' )")
                 .Append("AND ('" + md_nm + "'='' OR a.md_nm like '%" + md_nm + "%' )")
                 .Append(" ORDER BY a.chg_dt DESC ");
            var data = db.d_model_info.SqlQuery(sql.ToString()).ToList<d_model_info>();

            var count = data.Count();
            int pageIndex = pageing.page;
            int pageSize = pageing.rows;
            int startRow = (pageIndex * pageSize) + 1;
            int totalRecords = count;
            var gtri = (int)Math.Ceiling((float)totalRecords / (float)pageSize);
            int totalPages = (gtri < -1 ? 1 : gtri);
            var result = new
            {
                total = totalPages,
                page = pageIndex,
                records = totalRecords,
                rows = data.ToArray().ToPagedList(pageIndex, pageSize),
            };
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult deleteModel(int mdid)
        {
            var count_data = db.d_model_info.Count(x => x.mdid == mdid);
            d_model_info dminfo = db.d_model_info.Find(mdid);

            if (count_data != 0)
            {
                db.d_model_info.Remove(dminfo);
                db.SaveChanges();
                return Json(new { result = true, mdid = mdid }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
            //return View();
        }

        public ActionResult ExportToExcelModel()
        {
            var varname1 = new StringBuilder();
            //StringBuilder  = new StringBuilder();
            varname1.Append(" SELECT a.md_cd 'Model Code',a.md_nm 'Model Name',a.use_yn 'Use Y/N',a.reg_id 'Create User',a.reg_dt 'Create Date',a.chg_id 'Change User',a.chg_dt 'Change Date'  ")
       .Append("FROM d_model_info as a  ")
        .Append(" ORDER BY a.chg_dt DESC ");

            var data = new DataTable();

            using (var cmd = db.Database.Connection.CreateCommand())
            {
                db.Database.Connection.Open();
                cmd.CommandText = varname1.ToString();
                using (var reader = cmd.ExecuteReader())
                {
                    //data.Load(reader);
                    //index = data.Rows.Count;
                    //var index=data.Count

                    try
                    {
                        //data.Load(reader);
                        DataTable DTSchema = reader.GetSchemaTable();
                        //DataTable DT = new DataTable();
                        if (DTSchema != null)
                            if (DTSchema.Rows.Count > 0)
                                for (int i = 0; i < DTSchema.Rows.Count; i++)
                                {
                                    //Create new column for each row in schema table
                                    //Set properties that are causing errors and add it to our datatable
                                    //Rows in schema table are filled with information of columns in our actual table
                                    DataColumn Col = new DataColumn(DTSchema.Rows[i]["ColumnName"].ToString(), (Type)DTSchema.Rows[i]["DataType"]);
                                    Col.AllowDBNull = true;
                                    Col.Unique = false;
                                    Col.AutoIncrement = false;
                                    data.Columns.Add(Col);
                                }
                        while (reader.Read())
                        {
                            //Read data and fill it to our datatable
                            DataRow Row = data.NewRow();
                            for (int i = 0; i < data.Columns.Count; i++)
                            {
                                Row[i] = reader[i];
                            }
                            data.Rows.Add(Row);
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
                db.Database.Connection.Close();
            }

            DataSet ds2 = new DataSet();

            ds2.Tables.Add(data);

            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(ds2);
                wb.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wb.Style.Alignment.ShrinkToFit = true;
                wb.Style.Font.Bold = true;
                Response.Clear();
                Response.Buffer = true;
                Response.Charset = "";
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;filename= ModelMGT.xlsx");
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

        public ActionResult get_Model_html(string ModelCode, string ModelName)
        {
            //     var varname1 = new StringBuilder();
            //     //StringBuilder  = new StringBuilder();
            //     varname1.Append(" SELECT a.md_cd 'Model_Code',a.md_nm 'Model_Name',a.use_yn 'Use_Y/N',a.reg_id 'Create_User',a.reg_dt 'Create_Date',a.chg_id 'Change_User',a.chg_dt 'Change_Date'  ")
            //.Append("FROM d_model_info as a  ")
            // .Append(" ORDER BY a.chg_dt DESC ");

            //     var data = new Excute_query().get_data_from_data_base(varname1);
            //     var result = GetJsonPersons(data);

            //     return Json(result.Data, JsonRequestBehavior.AllowGet);

            var data = _IWIPService.GetListModel(ModelCode, ModelName);
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        #endregion ModelManagement

        #region StaffMgt

        //Begin Layout
        public ActionResult StaffMgt()
        {
            HttpCookie cookie = HttpContext.Request.Cookies["language"];
            if (cookie != null)
            {
                ViewBag.language = cookie.Value;
            }

            return View();
        }

        public ActionResult getgrade()
        {
            var list = db.author_info.ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public ActionResult getdepartment()
        {
            var depart_type = db.department_info.ToList();
            return Json(depart_type, JsonRequestBehavior.AllowGet);
        }

        public ActionResult getposition()
        {
            var position_type = db.comm_dt.ToList().Where(item => item.mt_cd == "COM018").ToList();
            return Json(position_type, JsonRequestBehavior.AllowGet);
        }

        public ActionResult getgender()
        {
            var position_type = db.comm_dt.ToList().Where(item => item.mt_cd == "COM019").ToList();
            return Json(position_type, JsonRequestBehavior.AllowGet);
        }

        public JsonResult userInfoData()
        {
            try
            {
                var mb_info_list = db.mb_info.Where(x => x.lct_cd == "user" || x.grade == "SUPPLIER").OrderByDescending(x => x.chg_dt).ToList();
                return Json(new { rows = mb_info_list }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public JsonResult userInfoData_staff()
        {
            try
            {
                var mb_info_list = db.mb_info.Where(x => x.lct_cd == "staff").OrderByDescending(x => x.chg_dt).ToList();
                return Json(new { rows = mb_info_list }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public JsonResult insertstaff(string userid, string uname, string position, string userCode, string birthDay, string gender, string department, string joinDay)
        {
            Dictionary<string, string> checkNull = new Dictionary<string, string>();
            checkNull.Add("userid", userid);
            checkNull.Add("uname", uname);
            checkNull.Add("position", position);
            checkNull.Add("userCode", userCode);
            checkNull.Add("birthDay", birthDay);
            checkNull.Add("gender", gender);
            checkNull.Add("department", department);
            checkNull.Add("joinDay", joinDay);

            foreach (var item in checkNull)
            {
                if (string.IsNullOrEmpty(item.Value))
                {
                    return Json(new { flag = false, message = "Điền đầy đủ thông tin để tiếp tục!." }, JsonRequestBehavior.AllowGet);
                }
            }

            mb_info mb_info = new mb_info();
            var result = db.mb_info.Count(x => x.userid == userid);
            if ((result == 0) && (userid != ""))
            {
                mb_info.userid = userid;
                mb_info.uname = uname;
                mb_info.gender = gender;
                mb_info.lct_cd = "staff";
                mb_info.barcode = userCode;
                mb_info.position_cd = position;
                mb_info.birth_dt = birthDay.Replace("-", "");
                mb_info.depart_cd = department;
                mb_info.join_dt = joinDay.Replace("-", "");
                mb_info.del_yn = "N";
                mb_info.reg_dt = DateTime.Now;
                mb_info.chg_dt = DateTime.Now;
                mb_info.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                mb_info.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                db.mb_info.Add(mb_info);
                db.SaveChanges();
                return Json(new { result = 0, userid = mb_info.userid }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { result, userid = mb_info.userid }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult modifyStaff(string userid, string uname, string position, string userCode, string birthDay, string gender, string department, string joinDay)
        {
            Dictionary<string, string> checkNull = new Dictionary<string, string>();
            checkNull.Add("userid", userid);
            checkNull.Add("uname", uname);
            checkNull.Add("position", position);
            checkNull.Add("userCode", userCode);
            checkNull.Add("birthDay", birthDay);
            checkNull.Add("gender", gender);
            checkNull.Add("department", department);
            checkNull.Add("joinDay", joinDay);

            foreach (var item in checkNull)
            {
                if (string.IsNullOrEmpty(item.Value))
                {
                    return Json(new { flag = false, message = "Điền đầy đủ thông tin để tiếp tục!." }, JsonRequestBehavior.AllowGet);
                }
            }

            var result = db.mb_info.Count(x => x.userid == userid);
            if ((result != 0) && (userid != ""))
            {
                var mb_info = db.mb_info.FirstOrDefault(x => x.userid == userid);
                mb_info.uname = uname;
                mb_info.gender = gender;
                mb_info.lct_cd = "staff";
                mb_info.barcode = userCode;
                mb_info.position_cd = position;
                mb_info.birth_dt = birthDay.Replace("-", "");
                mb_info.depart_cd = department;
                mb_info.join_dt = joinDay.Replace("-", "");
                mb_info.del_yn = "N";
                //mb_info.chg_dt = DateTime.Now;
                mb_info.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();

                db.Entry(mb_info).State = EntityState.Modified;
                db.SaveChanges();

                int CountMbAuthor = db.mb_author_info.Count(x => x.userid == userid);
                if (CountMbAuthor > 0)
                {
                    var mb_author_info = db.mb_author_info.FirstOrDefault(x => x.userid == userid);
                    db.mb_author_info.Remove(mb_author_info);
                    db.SaveChanges();
                }
                return Json(new { result = 1, userid = userid }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { result = 1, userid = userid }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult insertUser(string userid, string uname, string upw, string nick_name, string cel_nb, string e_mail, string memo, string grade, string scr_yn, string mail_yn, string joinday, string birthday, string gender, string department, string position
            )
        {
            mb_info mb_info = new mb_info();
            var result = db.mb_info.Count(x => x.userid == userid);
            if ((result == 0) && (userid != "") && (upw != ""))
            {
                mb_info.userid = userid;
                mb_info.uname = uname;
                mb_info.nick_name = nick_name;
                mb_info.upw = upw;
                mb_info.grade = grade;
                mb_info.cel_nb = cel_nb;
                mb_info.e_mail = e_mail;
                mb_info.mail_yn = mail_yn;
                mb_info.ltacc_dt = DateTime.Now;
                mb_info.mbout_dt = DateTime.Now;
                mb_info.session_limit = DateTime.Now;
                mb_info.memo = memo;
                mb_info.mbjoin_dt = DateTime.Now;
                mb_info.scr_yn = scr_yn;
                mb_info.reg_dt = DateTime.Now;
                mb_info.chg_dt = DateTime.Now;
                mb_info.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                mb_info.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                if (joinday.Length > 8) { joinday = (Convert.ToDateTime(joinday)).ToString("yyyyMMdd"); } else { joinday = Convert.ToString(joinday); }
                mb_info.join_dt = joinday;
                if (birthday.Length > 8) { birthday = (Convert.ToDateTime(birthday)).ToString("yyyyMMdd"); } else { birthday = Convert.ToString(birthday); }
                mb_info.birth_dt = birthday;

                mb_info.gender = gender;
                mb_info.depart_cd = department;
                mb_info.position_cd = position;
                mb_info.lct_cd = "user";
                db.mb_info.Add(mb_info);
                db.SaveChanges();

                mb_author_info mb_author_info = new mb_author_info();
                mb_author_info.userid = userid;
                mb_author_info.at_cd = db.author_info.FirstOrDefault(x => x.at_nm == grade).at_cd;
                mb_author_info.reg_dt = DateTime.Now;
                mb_author_info.chg_dt = DateTime.Now;
                db.mb_author_info.Add(mb_author_info);
                db.SaveChanges();

                return Json(new { result = 0, userid = mb_info.userid }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { result = 0, userid = mb_info.userid }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult modifyUser(string userid, string uname, string upw, string nick_name, string cel_nb, string e_mail, string memo, string grade, string scr_yn, string mail_yn, string joinday, string birthday, string gender, string department, string position
            )
        {
            var result = db.mb_info.Count(x => x.userid == userid);
            if ((result != 0) && (userid != "") && (upw != ""))
            {
                var mb_info = db.mb_info.FirstOrDefault(x => x.userid == userid);
                var at_cd = db.author_info.FirstOrDefault(x => x.at_nm == grade).at_cd;
                mb_info.uname = uname;
                mb_info.nick_name = nick_name;
                mb_info.upw = upw;
                mb_info.grade = grade;
                mb_info.cel_nb = cel_nb;
                mb_info.e_mail = e_mail;
                mb_info.mail_yn = mail_yn;
                mb_info.memo = memo;
                mb_info.scr_yn = scr_yn;
                if (joinday.Length > 8) { joinday = (Convert.ToDateTime(joinday)).ToString("yyyyMMdd"); } else { joinday = Convert.ToString(joinday); }
                mb_info.join_dt = joinday;
                if (birthday.Length > 8) { birthday = (Convert.ToDateTime(birthday)).ToString("yyyyMMdd"); } else { birthday = Convert.ToString(birthday); }
                mb_info.birth_dt = birthday;

                mb_info.gender = gender;
                mb_info.depart_cd = department;
                mb_info.position_cd = position;

                db.SaveChanges();

                int CountMbAuthor = db.mb_author_info.Count(x => x.userid == userid);
                if (CountMbAuthor > 0)
                {
                    var mb_author_info = db.mb_author_info.FirstOrDefault(x => x.userid == userid);
                    db.mb_author_info.Remove(mb_author_info);
                    db.SaveChanges();
                }
                var insert_mb_author_info = new mb_author_info();
                insert_mb_author_info.userid = userid;
                insert_mb_author_info.at_cd = at_cd;
                insert_mb_author_info.reg_dt = DateTime.Now;
                insert_mb_author_info.chg_dt = DateTime.Now;
                db.mb_author_info.Add(insert_mb_author_info);
                db.SaveChanges();
                return Json(new { result = 1, userid = userid }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { result = 1, userid = userid }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult deleteUser(string userid)
        {
            var result = db.mb_info.Count(x => x.userid == userid);
            if (result > 0)
            {
                var user = db.mb_info.Find(userid);
                db.mb_info.Remove(user);
                db.SaveChanges();
            }

            var resultauth = db.mb_author_info.Count(x => x.userid == userid);
            if (resultauth > 0)
            {
                var userauth = db.mb_author_info.FirstOrDefault(x => x.userid == userid);
                db.mb_author_info.Remove(userauth);
                db.SaveChanges();
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public JsonResult searchUser(string searchType, string keywordInput, string department, string position)
        {
            var sql = new StringBuilder();
            switch (searchType)
            {
                case "userid":
                    sql.Append(" SELECT *")
                    .Append("FROM  mb_info as a ")
                    .Append("Where ('" + keywordInput + "'='' OR  a.userid like '%" + keywordInput + "%' )")
                    .Append("AND ('" + department + "'='' OR  a.depart_cd like '%" + department + "%' )")
                    .Append("AND ('" + position + "'='' OR  a.position_cd like '%" + position + "%' )")
                    .Append("ORDER BY a.chg_dt desc ");
                    break;

                case "uname":
                    sql.Append(" SELECT *")
                    .Append("FROM  mb_info as a ")
                    .Append("Where ('" + keywordInput + "'='' OR  a.uname like '%" + keywordInput + "%' )")
                    .Append("AND ('" + department + "'='' OR  a.depart_cd like '%" + department + "%' )")
                    .Append("AND ('" + position + "'='' OR  a.position_cd like '%" + position + "%' )")
                    .Append("ORDER BY a.chg_dt desc ");
                    break;

                case "nick_name":
                    sql.Append(" SELECT *")
                    .Append("FROM  mb_info as a ")
                    .Append("Where ('" + keywordInput + "'='' OR  a.nick_name like '%" + keywordInput + "%' )")
                    .Append("AND ('" + department + "'='' OR  a.depart_cd like '%" + department + "%' )")
                    .Append("AND ('" + position + "'='' OR  a.position_cd like '%" + position + "%' )")
                    .Append("ORDER BY a.chg_dt desc ");
                    break;
            }

            return new InitMethods().JsonResultAndMessageFromQuery(sql, "");
        }

        public JsonResult SearchStaff(string searchType, string keywordInput, string position)
        {
            if (string.IsNullOrEmpty(searchType))
            {
                searchType = "";
            }
            else
            {
                searchType = searchType.Trim();
            }
            if (string.IsNullOrEmpty(keywordInput))
            {
                keywordInput = "";
            }
            else
            {
                keywordInput = keywordInput.Trim();
            }
            if (string.IsNullOrEmpty(position))
            {
                position = "";
            }
            else
            {
                position = position.Trim();
            }
            StringBuilder sql = new StringBuilder($"CALL spDevManagement_SearchStaff('{searchType}','{keywordInput}','{position}');");
            return new InitMethods().JsonResultAndMessageFromQuery(sql, "");
        }

        #endregion StaffMgt

        #region TrayBox

        public ActionResult TrayBoxMgt()
        {
            HttpCookie cookie = HttpContext.Request.Cookies["language"];
            if (cookie != null)
            {
                ViewBag.language = cookie.Value;
            }
            return View();
        }

        public ActionResult getTrayBox()
        {
            var values = db.d_bobbin_info.OrderByDescending(x => x.bb_no).ToList();
            var data = new { rows = values };
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult insertautoTrayBox(string qty_ge, string mc_type)
        {
            try
            {
                DateTime dateTime = DateTime.Now;
                //string CurrentDateTime = dateTime.ToString("yyyyMMddHHmmss");
                string CurrentDateTime = dateTime.ToString("yyMMddHHmmss");
                //var bb_tmp =;

                //var del_yn = "N";
                //var mc_type = "TRB";
                //var bb_no = string.Empty;
                var menuCd = string.Empty;
                int qty = Convert.ToInt32(qty_ge);
                var list = new ArrayList();
                //var item = String.Empty;
                //var sb = (new StringBuilder()).Append("'");

                var bb_no_tmp = string.Empty;

                for (int i = 0; i < qty; i++)
                {
                    //int countlist = db.d_bobbin_info.Where(x => x.mc_type == "TRB").ToList().Count();
                    //if (countlist == 0)
                    //{
                    //    bb_no_tmp = "000001";
                    //}

                    //else
                    //{
                    //    var listlast = db.d_bobbin_info.Where(x => x.mc_type == "TRB").OrderBy(item => item.bb_no).ToList().LastOrDefault().bb_no;
                    //    //var bien = listlast.po_no;
                    //    var subbbno = new Excute_query().Right(listlast, 6);

                    //    //listlast.Substring(listlast.Length - 6, 6);
                    //    //listlast.ebd
                    //    int.TryParse(subbbno, out subMenuIdConvert);
                    //    bb_no_tmp = string.Format("{0}", new Excute_query().autobarcode((subMenuIdConvert + 1)));
                    //}
                    //var number = StringExtensions.FormatNumberInt(0011);
                    //bb_no_tmp = string.Format("{0}", new Excute_query().autobarcode((i + 1)));
                    bb_no_tmp = String.Format("{0, 0:D3}", i + 1);
                    string type = CheckType(mc_type);
                    var bb_cd = MergeCode(CurrentDateTime, bb_no_tmp, type);

                    var chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    var reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();

                    //var sql = " CALL sp_insert_TrayBox( ";

                    //sql += " '" + bb_cd + "' ,";
                    //sql += " '" + chg_id + "' ";

                    //sql += ",'" + reg_id + "' )";
                    //StringBuilder stringBuilder = new StringBuilder(sql);

                    var bb_nm = new Excute_query().StartEndIndex(bb_cd, 0, bb_cd.LastIndexOf("-"));

                    var sql = " CALL sp_insert_TrayBox('" + bb_cd + "' , '" + chg_id + "' ,'" + reg_id + "','" + mc_type + "','" + bb_nm + "' )";
                    StringBuilder stringBuilder = new StringBuilder(sql);

                    int result = new Excute_query().Execute_NonQuery(stringBuilder);
                    //var checkflag = new InitMethods().ReturnJsonResultFromQuery(stringBuilder);
                    //checkflag.Data;
                    if (result > 0)
                    {
                        var sql_insert = new StringBuilder();

                        sql_insert.Append(" SELECT d.bb_no, d.bno, d.mc_type, d.mt_cd, d.bb_nm, d.purpose, d.barcode, d.re_mark ")
                            .Append(" FROM d_bobbin_info d ")
                            .Append(" WHERE d.bb_no='" + bb_cd + "' ")
                            .Append(" ORDER BY d.bno DESC ");
                        var list2 = new InitMethods().ConvertDataTableToList<ModelReturnTrayBox>(sql_insert);

                        list.Add(list2);
                        //list.Add(bb_cd);

                        //sb.Append(bb_cd).Append("','");
                    }
                }

                #region MyRegion

                //int index = sb.ToString().LastIndexOf(",");
                //string bb_list = sb.ToString().Substring(0, index);

                //var sql_insert = new StringBuilder();

                //sql_insert.Append(" SELECT d.bb_no, d.bno, d.mc_type, d.mt_cd, d.bb_nm, d.purpose, d.barcode, d.re_mark ")
                //    .Append(" FROM d_bobbin_info d ")
                //    .Append(" WHERE d.bb_no IN (" + bb_list + " )")
                //    .Append(" ORDER BY d.bno DESC ");
                //var list2 = new InitMethods().ConvertDataTableToList<ModelReturnTrayBox>(sql_insert);

                #endregion MyRegion

                return Json(new
                {
                    result = true,
                    data = list,
                    message = "Thành công !",
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

        //public string Right(string str, int length)
        //{
        //    return str.Substring(str.Length - length, length);
        //}

        public string MergeCode(string CurrentDateTime, string menuCd, string type)
        {
            return string.Concat("AUTO-" + type + "-" + CurrentDateTime + menuCd);
        }

        public string CheckType(string type)
        {
            if (type == "BOB")
            {
                type = "BOB";
            }
            else
            {
                type = "TRAY";
            }
            return type;
        }

        public ActionResult insertTrayBox(d_bobbin_info dbobbin, string bb_no, string bb_nm, string purpose, string description)
        {
            try
            {
                int count = db.d_bobbin_info.Where(item => item.bb_no == bb_no).ToList().Count();
                var result = new { result = count };
                if (count > 0)
                {
                    return View(dbobbin);
                }
                else
                {
                    DateTime chg_dt = DateTime.Now;
                    DateTime reg_dt = DateTime.Now;
                    dbobbin.reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    dbobbin.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    dbobbin.chg_dt = chg_dt;
                    dbobbin.reg_dt = reg_dt;
                    dbobbin.bb_no = bb_no;
                    dbobbin.bb_nm = bb_nm;
                    dbobbin.purpose = purpose;
                    dbobbin.re_mark = description;
                    dbobbin.barcode = bb_no;

                    dbobbin.del_yn = "N";
                    dbobbin.mc_type = "TRB";

                    db.Entry(dbobbin).State = EntityState.Added;
                    db.SaveChanges(); // line that threw exception

                    return Json(dbobbin, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public ActionResult updateTrayBox(int bno, string bb_nm, string purpose, string description)
        {
            //var count_ml_cd = db.m_layout_mt.Count(x => x.ml_cd == m_ml_cd);
            try
            {
                d_bobbin_info dbox4 = db.d_bobbin_info.Find(bno);
                DateTime chg_dt = DateTime.Now;

                //dbox4.chg_dt = chg_dt;
                dbox4.bb_nm = bb_nm;
                dbox4.re_mark = description;

                dbox4.purpose = purpose;
                dbox4.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                //dbox4.mc_type = "TRB";

                if (ModelState.IsValid)
                {
                    db.Entry(dbox4).State = EntityState.Modified;
                    db.SaveChanges(); // line that threw exception
                    return Json(dbox4, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Json(new { result = false }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult searchTrayBox(Pageing pageing)
        {
            //var values = db.d_bobbin_info.ToList();
            string bb_no = Request["bb_no"];
            string bb_nm = Request["bb_nm"];
            string tempSql = " SELECT d.bb_no, d.bno, d.mc_type, d.mt_cd, d.bb_nm, d.purpose, d.barcode, d.re_mark,(SELECT b.at_no FROM w_material_info AS a "
           + "JOIN w_actual AS b ON a.id_actual=b.id_actual WHERE a.mt_cd=d.mt_cd LIMIT 1)at_no "
            + " FROM d_bobbin_info d "
                + " WHERE d.bb_no LIKE '%AUTO%' and ('" + bb_no + "'='' OR d.bb_no LIKE '%" + bb_no + "%') "
                + " AND ('" + bb_nm + "'='' OR d.bb_nm LIKE '%" + bb_nm + "%') ";
            StringBuilder varname1 = new StringBuilder(tempSql);
            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);
            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("bno"));
            return new InitMethods().ReturnJsonResultWithPaging(pageing, total, result);

        }

        public JsonResult GetJsonPersons4(DataTable data)
        {
            var lstPersons = GetTableRows(data);
            return Json(lstPersons, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetJsonPersons5(DataTable data)
        {
            var lstPersons = GetTableRows(data);
            return Json(lstPersons, JsonRequestBehavior.AllowGet);
        }

        public ActionResult _Print()
        {
            string[] keys = Request.Form.AllKeys;

            var value = "";
            value = Request.Form[keys[0]];

            ViewData["Message"] = value;
            return View();
        }

        public ActionResult _Print2(string id)
        {
            var a2 = id.TrimStart('[').TrimEnd(']').Split(',');
            var row_data = new List<d_bobbin_info>();
            for (int i = 0; i < a2.Length; i++)
            {
                var id2 = int.Parse(a2[i]);
                var sql = new StringBuilder();
                sql.Append(" SELECT a.* ")
                           .Append(" from d_bobbin_info as a")
                                                      .Append(" where a.bno ='" + id2 + "'");

                var data = db.d_bobbin_info.SqlQuery(sql.ToString()).ToList<d_bobbin_info>();
                row_data.AddRange(data);
            }
            return Json(row_data, JsonRequestBehavior.AllowGet);
        }

        #endregion TrayBox

        #region Print QR

        public ActionResult _printQRUser(string id)
        {
            ViewData["Message"] = id;
            return View();
        }

        public ActionResult PrintMultiUserQRCode(string id)
        {
            var multiIDs = id.TrimStart('[').TrimEnd(']').Split(',');
            var row_data = new List<mb_info>();
            for (int i = 0; i < multiIDs.Length; i++)
            {
                var eachId = multiIDs[i];
                var sql = new StringBuilder();
                sql.Append(" SELECT a.* ")
                   .Append(" from mb_info as a")
                   .Append(" where a.userid ='" + eachId + "'");

                var data = db.mb_info.SqlQuery(sql.ToString()).ToList<mb_info>();
                row_data.AddRange(data);
            }

            return Json(row_data, JsonRequestBehavior.AllowGet);
        }

        #endregion Print QR

        #region Routing

        public ActionResult Rounting()
        {
            return SetLanguage("");
        }

        public JsonResult GetTIMSProcesses()
        {
            var data = db.comm_dt.Where(x => x.mt_cd == "COM007" && x.use_yn == "Y")
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

        public JsonResult Get_Rounting(Pageing pageing, string product, string process_code)
        {
            StringBuilder sql = new StringBuilder($"Select a.*,(select dt_nm from comm_dt where mt_cd='COM032' and dt_cd=a.don_vi_pr)don_vi_prnm,(select dt_nm from comm_dt where mt_cd='COM007' and dt_cd=a.name)name_pr from d_rounting_info as a where a.style_no='" + product + "'  and  a.process_code='" + process_code + "' ");
            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);
            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderBy(x => x.Field<string>("type")).ThenBy(x => x.Field<int>("level"));
            return new InitMethods().ReturnJsonResultWithPaging(pageing, total, result);
        }
        public JsonResult Create_Rounting(DRoutingInfo d_rounting_info)
        {
            try
            {
                //kiem tra co product do chua
                if (!db.d_style_info.Any(x => x.style_no == d_rounting_info.style_no))
                {
                    return Json(new { result = false, message = "Product không tìm thấy!" }, JsonRequestBehavior.AllowGet);
                }

                //tao cong doan

                //type
                d_rounting_info.type = (d_rounting_info.name.StartsWith("ROT") || d_rounting_info.name.StartsWith("STA") ? "SX" : "TIMS");
                //item_vcd
                if (d_rounting_info.name.StartsWith("OQC"))
                {
                    d_rounting_info.item_vcd = "OC000001A";
                }
                else if (d_rounting_info.type == "SX")
                {
                    d_rounting_info.item_vcd = "PC00004A";
                }
                else
                {
                    d_rounting_info.item_vcd = "TI000001A";
                }
                //level
                //var check_data = db.d_rounting_info.Where(x => x.style_no == d_rounting_info.style_no).ToList();
                var check_data = _IDomMS.CheckLevelProcess(d_rounting_info.style_no, d_rounting_info.process_code);
                var check_con = check_data.Where(x => x.name == d_rounting_info.name).ToList();
                if (check_con.Count() > 0)
                {
                    d_rounting_info.level = check_con.FirstOrDefault().level;
                }
                else
                {
                    d_rounting_info.level = (check_data.Count() + 1);
                }
                d_rounting_info.description = d_rounting_info.description;
                d_rounting_info.reg_dt = DateTime.Now;
                d_rounting_info.chg_dt = DateTime.Now;
                d_rounting_info.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                d_rounting_info.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();

                if (ModelState.IsValid)
                {
                    //db.Entry(d_rounting_info).State = EntityState.Added;
                    //db.SaveChanges();

                    if (d_rounting_info.isFinish == "Y")
                    {
                        _IDomMS.UpdateIsFinish(d_rounting_info.style_no, d_rounting_info.process_code);
                    }

                   int idr = _IDomMS.InsertToDRoutingInfo(d_rounting_info);
                    var donvi = db.comm_dt.Where(x => x.mt_cd == "COM032" && x.dt_cd == d_rounting_info.don_vi_pr).SingleOrDefault();
                    var name = db.comm_dt.Where(x => x.mt_cd == "COM007" && x.dt_cd == d_rounting_info.name).SingleOrDefault();
                    var item = new
                    {
                        idr = idr,
                        style_no = d_rounting_info.style_no,
                        name = d_rounting_info.name,
                        description = d_rounting_info.description,
                        level = d_rounting_info.level,
                        don_vi_pr = d_rounting_info.don_vi_pr,
                        type = d_rounting_info.type,
                        item_vcd = d_rounting_info.item_vcd,
                        reg_dt = d_rounting_info.reg_dt,
                        reg_id = d_rounting_info.reg_id,
                        chg_id = d_rounting_info.chg_id,
                        chg_dt = d_rounting_info.chg_dt,
                        isFinish = d_rounting_info.isFinish,
                        don_vi_prnm = (donvi != null ? donvi.dt_nm : ""),
                        name_pr = (name != null ? name.dt_nm : ""),
                    };

                    //tra ve view
                    return Json(new { result = true, kq = item }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult Modify_Rounting(int idr, string style_no, string name, string don_vi_pr, string description, string isFinish, string process_code)
        {
            try
            {
                var find_id = db.d_rounting_info.Find(idr);
                if (find_id == null)
                {
                    return Json(new { result = false, message = "Product này không có công đoạn!!" }, JsonRequestBehavior.AllowGet);
                }
              
                find_id.name = name;
                find_id.don_vi_pr = don_vi_pr;
                find_id.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                //type
                find_id.type = (find_id.name.StartsWith("ROT") || find_id.name.StartsWith("STA") ? "SX" : "TIMS");
                //item_vcd
                if (find_id.name.StartsWith("OQC"))
                {
                    find_id.item_vcd = "OC000001A";
                }
                else if (find_id.type == "SX")
                {
                    find_id.item_vcd = "PC00004A";
                }
                else
                {
                    find_id.item_vcd = "TI000001A";
                }
                if (ModelState.IsValid)
                {
                    if (isFinish == "Y")
                    {
                        _IDomMS.UpdateIsFinish(find_id.style_no, process_code);
                    }
                    _IDomMS.UpdateDRoutingInfo(find_id, description, isFinish);
                    //db.Entry(find_id).State = EntityState.Modified;
                    //db.SaveChanges();
                    var donvi = db.comm_dt.Where(x => x.mt_cd == "COM032" && x.dt_cd == find_id.don_vi_pr).SingleOrDefault();
                    var name1 = db.comm_dt.Where(x => x.mt_cd == "COM007" && x.dt_cd == find_id.name).SingleOrDefault();
                    var item = new
                    {
                        idr = find_id.idr,
                        style_no = find_id.style_no,
                        description = description,
                        name = find_id.name,
                        level = find_id.level,
                        don_vi_pr = find_id.don_vi_pr,
                        type = find_id.type,
                        item_vcd = find_id.item_vcd,
                        reg_dt = find_id.reg_dt,
                        reg_id = find_id.reg_id,
                        chg_id = find_id.chg_id,
                        chg_dt = find_id.chg_dt,
                        isFinish = isFinish,
                        don_vi_prnm = (donvi != null ? donvi.dt_nm : ""),
                        name_pr = (name1 != null ? name1.dt_nm : ""),
                    };

                    //tra ve view
                    return Json(new { result = true, kq = item }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult Get_ProductMaterial(Pageing pageing, string product, string name, string process_code)
        {
            StringBuilder sql = new StringBuilder($"Select a.*,(select mt_nm from d_material_info where mt_no=a.mt_no) mt_nm from product_material as a where a.style_no='" + product + "' and a.name='" + name + "' AND a.process_code ='" + process_code + "' order by a.id asc");
            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);
            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<string>("mt_no"));
            return new InitMethods().ReturnJsonResultWithPaging(pageing, total, result);
        }
        public JsonResult Deleteprocess(int idr)
        {
            try
            {
                var find_id = db.d_rounting_info.Find(idr);
                if (find_id == null)
                {
                    return Json(new { result = false, message = "Không tìm thấy công đoạn này" }, JsonRequestBehavior.AllowGet);
                }

                string checkExist = @"SELECT EXISTS(SELECT id FROM product_material  WHERE style_no=@1 and name = @2);";
                int kq = db.Database.SqlQuery<int>(checkExist,
                    new MySqlParameter("@1", find_id.style_no),
                     new MySqlParameter("@2", find_id.name)
                    ).FirstOrDefault();
                if (kq > 0)
                {
                    return Json(new { result = false, message = "Công đoạn này đang chứa liệu,Hãy xóa liệu trước." }, JsonRequestBehavior.AllowGet);
                }


                db.Entry(find_id).State = EntityState.Deleted;
                db.SaveChanges();
                return Json(new { result = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult CreateProductMaterial([System.Web.Http.FromBody] ProductMaterailModel model)
        {
            if (!ModelState.IsValid)
                return null;
            try
            {
                string checkExist = @"SELECT EXISTS(SELECT id FROM product_material  WHERE style_no=@1 and name = @2 AND mt_no = @3  AND process_code = @4);";
                int kq = db.Database.SqlQuery<int>(checkExist,
                    new MySqlParameter("@1", model.style_no),
                     new MySqlParameter("@2", model.name),
                     new MySqlParameter("@3", model.mt_no),
                     new MySqlParameter("4", model.process_code)
                    ).FirstOrDefault();
                if (kq > 0)
                {
                    return Json(new { result = false, message = "Nguyên vật liệu " + exits }, JsonRequestBehavior.AllowGet);
                }
                ProductMaterailModel item = new ProductMaterailModel();

                item.style_no = model.style_no;
                item.process_code = model.process_code;
                item.level = model.level;
                item.name = model.name;
                item.mt_no = model.mt_no;
                item.cav = model.cav;
                item.buocdap = model.buocdap;
                item.need_time = model.need_time;
                item.need_m = (model.buocdap) / 1000 / model.cav * model.need_time;
                item.reg_dt = DateTime.Now;
                item.chg_dt = DateTime.Now;
                item.isActive = model.isActive;

                item.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                item.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();

                if (model.isActive == "Y")
                {
                    _IDomMS.UpdateNVLDeTinhHieuSuat(model.style_no);
                }
           
                int idbominfo = _IDomMS.InsertToProductMaterial(item);
                item.id = idbominfo;
                item.mt_nm = db.d_material_info.Where(x => x.mt_no == item.mt_no).FirstOrDefault().mt_nm;
               
                return Json(new { result = true, message = ss, data = item }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {

                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult ModifyProductMaterial(ProductMaterailModel table)
        {
            try
            {
                //kiểm tra trong nguyên vật liệu đã có tồn tại không
                var ProductMaterial = _IDomMS.GetProductMaterial(table.id);
                if (ProductMaterial == null)
                {
                    return Json(new { result = false, message = exits }, JsonRequestBehavior.AllowGet);
                }
                string checkExist = @"SELECT EXISTS(SELECT id FROM product_material  
                        WHERE style_no=@1 AND level = @2  AND mt_no = @3 AND  id != @4
                                );";
                int kq = db.Database.SqlQuery<int>(checkExist,
                    new MySqlParameter("1", ProductMaterial.style_no),
                     new MySqlParameter("2", ProductMaterial.level),
                      new MySqlParameter("3", table.mt_no),
                      new MySqlParameter("4", table.id)
                    ).FirstOrDefault();
                if (kq > 0)
                {
                    return Json(new { result = false, message = "Công đoạn này đã Đăng kí NGUYÊN VẬT LIỆU này. Hãy chọn một NGUYÊN VẬT LIỆU khác" }, JsonRequestBehavior.AllowGet);
                }

                ProductMaterial.cav = table.cav;
                ProductMaterial.isActive = table.isActive;
                ProductMaterial.need_time = table.need_time;
                ProductMaterial.buocdap = table.buocdap;
                ProductMaterial.need_m = (table.buocdap / 1000 / table.cav * table.need_time);
                ProductMaterial.mt_no = table.mt_no;
                ProductMaterial.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
               
                if (table.isActive == "Y")
                {
                    _IDomMS.UpdateNVLDeTinhHieuSuat(ProductMaterial.style_no);
                }
                _IDomMS.UpdateToProductMaterial(ProductMaterial);
                ProductMaterial.mt_nm = db.d_material_info.Where(x => x.mt_no == table.mt_no).FirstOrDefault().mt_nm;
                return Json(new { result = true, message = ss, ProductMaterial }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }

        }
        public ActionResult DeleteProductMaterial(int id)
        {
            try
            {
                //kiểm tra trong nguyên vật liệu đã có tồn tại không
                var ProductMaterial = _IDomMS.GetProductMaterial(id);
                if (ProductMaterial == null)
                {
                    return Json(new { result = false, message = exits }, JsonRequestBehavior.AllowGet);
                }
                _IDomMS.DeleteProductMaterialForId(id);

                //xóa detail
                _IDomMS.DeleteProductMaterialDetail(ProductMaterial.style_no, ProductMaterial.level, ProductMaterial.mt_no);
                return Json(new { result = true, id }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {

                return Json(new { result = false, message="Lỗi hệ thống" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult AddMaterialchild([System.Web.Http.FromBody] ProductMaterialDetailModel model)
        {
            if (!ModelState.IsValid)
                return null;
            try
            {
                //kierm tra product trong bảng bom_info
                var checkMaterialExist = _IDomMS.CheckMaterialExist(model.ProductCode,model.level, model.MaterialPrarent, model.process_code);
                if (checkMaterialExist == null)
                {
                    return Json(new { result = false, message = "Vui lòng chọn NVL để tạo liệu thay thế" }, JsonRequestBehavior.AllowGet);
                }
                //insert material lại
                for (int i = 0; i < model.ListMaterial.Length; i++)
                {
                    var MaterialChild = new ProductMaterialDetailModel();
                    MaterialChild.level = model.level;
                    MaterialChild.name = model.name;
                    MaterialChild.ProductCode = model.ProductCode;
                    MaterialChild.process_code = model.process_code;
                    MaterialChild.MaterialPrarent = model.MaterialPrarent;
                    MaterialChild.MaterialNo = model.ListMaterial[i];
                    MaterialChild.CreateDate = DateTime.Now;
                    MaterialChild.ChangeDate = DateTime.Now;
                    MaterialChild.CreateId = Session["userid"] == null ? null : Session["userid"].ToString();
                    MaterialChild.ChangeId = Session["userid"] == null ? null : Session["userid"].ToString();

                    _IDomMS.InsertToMaterialChild(MaterialChild);
                }

                IEnumerable<ProductMaterialDetailModel> value = _IDomMS.GetListmaterialChild(model.ProductCode,model.name, model.MaterialPrarent, model.process_code);
                return Json(new { result = true, message = "", data = value }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {

                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult getMaterialChild(string ProductCode, string name, string MaterialPrarent, string process_code)
        {
            try
            {
                IEnumerable<ProductMaterialDetailModel> value = _IDomMS.GetListmaterialChild(ProductCode, name, MaterialPrarent, process_code);
                return Json(value, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult DeleteMaterialChild(int id)
        {
            if (_IDomMS.DeleteMaterialChild(id) > 0)
            {
                return Json(new { result = true, message = "Xóa thành công" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { result = false, message = "Không thể xóa mã này" }, JsonRequestBehavior.AllowGet);

        }
        public JsonResult Get_StyleProcess(Pageing pageing, string product)
        {

            string data = @"SELECT a.* FROM product_routing AS a WHERE a.style_no = @1 ;";
            IEnumerable<ProductProcess> data1 = db.Database.SqlQuery<ProductProcess>(data,
                new MySqlParameter("1", product));
            int totalRecords = data1.ToList().Count;

            Dictionary<string, string> list = PagingAndOrderBy(pageing, " ORDER BY  MyDerivedTable.style_no desc   ");
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

            int start = (pageing.page - 1) * pageing.rows;
            var rowsData = data1.Skip(start).Take(pageing.rows);

            var result = new
            {
                total = totalPages,
                page = int.Parse(list["index"]),
                records = totalRecords,
                rows = rowsData
            };
            return Json(result, JsonRequestBehavior.AllowGet);

        }
        public ActionResult CreateProductProcess([System.Web.Http.FromBody] ProductProcess model)
        {
            if (!ModelState.IsValid)
                return null;
            try
            {
                string checkProcessCode = @"SELECT max(process_code)process_code  FROM product_routing  WHERE style_no=@1;";
                string resultProcess = db.Database.SqlQuery<string>(checkProcessCode,
                    new MySqlParameter("@1", model.style_no),
                     new MySqlParameter("@2", model.process_code)
                    ).FirstOrDefault();
                ProductProcess item = new ProductProcess();
                if (string.IsNullOrEmpty(resultProcess))
                {
                    item.process_code = 1;
                }

                else
                {
                    item.process_code = resultProcess.ToInt() + 1;
                }
                item.style_no = model.style_no;
                item.process_name = model.process_name;
                item.description = model.description;
                item.IsApply = model.IsApply;
                item.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                item.reg_dt = DateTime.Now;


                if (model.IsApply == "Y")
                {
                    _IDomMS.UpdateProcessToApply(model.style_no);
                }

                int idProcessinfo = _IDomMS.InsertToProductProcess(item);
                item.id = idProcessinfo;

                return Json(new { result = true, message = ss, data = item }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {

                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult ModifyProductProcess(ProductProcess model)
        {
            try
            {
                //kiểm tra có tồn tại không
                var ProductProcess = _IDomMS.GetProductProcess(model.id);
                if (ProductProcess == null)
                {
                    return Json(new { result = false, message = exits }, JsonRequestBehavior.AllowGet);
                }


                ProductProcess.process_name = model.process_name;
                ProductProcess.description = model.description;
                ProductProcess.IsApply = model.IsApply;

                ProductProcess.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                ProductProcess.chg_dt = DateTime.Now;

                if (model.IsApply == "Y")
                {
                    _IDomMS.UpdateProcessToApply(ProductProcess.style_no);
                }
                _IDomMS.UpdateToProductProcess(ProductProcess);

                return Json(new { result = true, message = ss, data = ProductProcess }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }

        }
        #endregion Routing
        #region Resource
        public ActionResult Resource()
        {
            HttpCookie cookie = HttpContext.Request.Cookies["language"];
            if (cookie != null)
            {
                ViewBag.language = cookie.Value;
            }
            return View();
        }
        public ActionResult PartialView_Resource()
        {
            var month = (Request["month"] != null ? Request["month"] : "0");
            var product = (Request["product"] != null ? Request["product"] : "");
            var po_no = (Request["po_no"] != null ? Request["po_no"] : "");
            var type_kind = (Request["type_kind"] != null ? Request["type_kind"] : "");
            var value_code = (Request["value_code"] != null ? Request["value_code"] : "");
            var value_nm = (Request["value_nm"] != null ? Request["value_nm"] : "");
            var start = (Request["start"] != null ? Request["start"] : "");
            var end = (Request["end"] != null ? Request["end"] : "");
            var mc_type = ((Request["mc_type"] != null && Request["mc_type"] != "undefined") ? Request["mc_type"] : "");
            var time_now = DateTime.Now.AddMonths(int.Parse(month));

            var startOfMonth = new DateTime(time_now.Year, time_now.Month, 1);
            var first = startOfMonth.AddMonths(1);
            var last = first.AddDays(-1);

            var date_start = startOfMonth.ToString("yyyyMMdd");
            var date_end = last.ToString("yyyyMMdd");

            String varname1 = "";
            String varname2 = "";
            String varname3 = "";
            if (type_kind == "mc")
            {
                var List = new ArrayList();
                ViewBag.List = List;
                ViewBag.name = "Machine";
                varname1 = varname1 + "select (a.mc_nm),(a.mc_no),('Machine')kind, " + "\n";
                var count_day = (last - startOfMonth).TotalDays;
                for (int i = 0; i < (count_day + 1); i++)
                {
                    var bien = startOfMonth.AddDays(i);
                    var bien_doi = bien.ToString("dd");
                    List.Add(bien_doi);
                    var text = bien.ToString("yyyyMMdd");
                    varname1 = varname1 + "(case when " + "\n";
                    varname1 = varname1 + "	 (select count(k.pmid) from d_pro_unit_mc as k " + "\n";
                    varname1 = varname1 + "			where '" + text + "' between (Date_format(k.start_dt,'%Y%m%d')) and (Date_format(k.end_dt,'%Y%m%d')) and k.mc_no=a.mc_no)>0 " + "\n";
                    varname1 = varname1 + "		 then 	'bg-warning' else 'bg-white' end)day_" + (i + 1) + " , " + "\n";
                    varname1 = varname1 + "(	SELECT Group_concat(DISTINCT (Concat('PO', Cast(Substr(m.at_no, 3, 11)AS UNSIGNED), ' ' , m.name, ' Start: ', Concat(Substring(n.start_dt, 1, 4), '-', " + "\n";
                    varname1 = varname1 + "       Substring(n.start_dt, 5, 2), '-', Substring(n.start_dt, 7, 2), ' ', Substring(n.start_dt, 9, 2), ':', Substring(n.start_dt, 11, 2), ':', " + "\n";
                    varname1 = varname1 + "       Substring(n.start_dt, 13, 2)),  ' End: ', Concat(Substring(n.end_dt, 1, 4), '-',  Substring(n.end_dt, 5, 2), '-', " + "\n";
                    varname1 = varname1 + "       Substring(n.end_dt, 7, 2), ' ', Substring(n.end_dt, 9, 2), ':',  Substring(n.end_dt, 11, 2), ':', Substring(n.end_dt, 13, 2)) ) )  SEPARATOR ',') " + "\n";
                    varname1 = varname1 + "       cv " + "\n";
                    varname1 = varname1 + "FROM   d_pro_unit_mc AS n " + "\n";
                    varname1 = varname1 + "       JOIN w_actual AS m " + "\n";
                    varname1 = varname1 + "         ON n.id_actual = m.id_actual " + "\n";
                    varname1 = varname1 + "WHERE  n.mc_no = a.mc_no " + "\n";
                    varname1 = varname1 + "       AND '" + text + "' BETWEEN ( Date_format(n.start_dt, '%Y%m%d') ) AND ( " + "\n";
                    varname1 = varname1 + "                              Date_format(n.end_dt, '%Y%m%d') ) ) po" + (i + 1) + ",";
                }
                varname1 = varname1 + " (a.mno) id " + "\n";
                varname1 = varname1 + "from d_machine_info as a " + "\n";
                //where
                varname1 = varname1 + " Where ('" + value_code + "'='' OR  a.mc_no  like '%" + value_code + "%' )";
                varname1 = varname1 + " and ('" + value_nm + "'='' OR  a.mc_nm  like '%" + value_nm + "%' )";
                if (po_no != "" || product != "")
                {
                    varname1 = varname1 + " and a.mc_no in (select v.mc_no from d_pro_unit_mc as v ";
                    varname1 = varname1 + " join w_actual as n on v.id_actual=n.id_actual ";
                    varname1 = varname1 + " join w_actual_primary as l on n.at_no =l.at_no ";
                    varname1 = varname1 + " Where ('" + po_no + "'='' OR  n.at_no  like '%" + po_no + "%' )";
                    varname1 = varname1 + " and ('" + product + "'='' OR l.product  like '%" + product + "%' ))";
                }
                varname1 = varname1 + " and ('" + mc_type + "'='' OR  a.mc_type  like '%" + mc_type + "%' ) group by a.mc_no";

            }
            if (type_kind == "wk")
            {
                var List = new ArrayList();
                ViewBag.List = List;
                ViewBag.name = "Worker";
                varname2 = varname2 + "select (a.uname) mc_nm, " + "\n";
                varname2 = varname2 + " (a.userid) mc_no,('Worker')kind," + "\n";
                var count_day = (last - startOfMonth).TotalDays;
                for (int i = 0; i < (count_day + 1); i++)
                {
                    var bien = startOfMonth.AddDays(i);
                    var bien_doi = bien.ToString("dd");
                    List.Add(bien_doi);
                    var text = bien.ToString("yyyyMMdd");
                    varname2 = varname2 + "(case when " + "\n";
                    varname2 = varname2 + "	 (select count(k.psid) from d_pro_unit_staff as k " + "\n";
                    varname2 = varname2 + "			where '" + text + "' between (Date_format(k.start_dt,'%Y%m%d')) and (Date_format(k.end_dt,'%Y%m%d')) and  k.staff_id=a.userid)>0 " + "\n";
                    varname2 = varname2 + "		 then 	'bg-warning' else 'bg-white' end)day_" + (i + 1) + " , " + "\n";
                    varname2 = varname2 + "(	SELECT Group_concat(DISTINCT (Concat('PO', Cast(Substr(m.at_no, 3, 11)AS UNSIGNED), ' ' , m.name, ' Start: ', Concat(Substring(n.start_dt, 1, 4), '-', " + "\n";
                    varname2 = varname2 + "       Substring(n.start_dt, 5, 2), '-', Substring(n.start_dt, 7, 2), ' ', Substring(n.start_dt, 9, 2), ':', Substring(n.start_dt, 11, 2), ':', " + "\n";
                    varname2 = varname2 + "       Substring(n.start_dt, 13, 2)),  ' End: ', Concat(Substring(n.end_dt, 1, 4), '-',  Substring(n.end_dt, 5, 2), '-', " + "\n";
                    varname2 = varname2 + "       Substring(n.end_dt, 7, 2), ' ', Substring(n.end_dt, 9, 2), ':',  Substring(n.end_dt, 11, 2), ':', Substring(n.end_dt, 13, 2)) ) )  SEPARATOR ',') " + "\n";
                    varname2 = varname2 + "       cv " + "\n";
                    varname2 = varname2 + "FROM   d_pro_unit_staff AS n " + "\n";
                    varname2 = varname2 + "       JOIN w_actual AS m " + "\n";
                    varname2 = varname2 + "         ON n.id_actual = m.id_actual " + "\n";
                    varname2 = varname2 + "WHERE  n.staff_id = a.userid " + "\n";
                    varname2 = varname2 + "       AND '" + text + "' BETWEEN ( Date_format(n.start_dt, '%Y%m%d') ) AND ( " + "\n";
                    varname2 = varname2 + "                              Date_format(n.end_dt, '%Y%m%d') ) ) po" + (i + 1) + ",";
                }
                varname2 = varname2 + " (a.userid) id " + "\n";
                varname2 = varname2 + "from mb_info as a " + "\n";
                varname2 = varname2 + " Where a.lct_cd='staff' and ('" + value_code + "'='' OR  a.userid  like '%" + value_code + "%' )";
                varname2 = varname2 + " and ('" + value_nm + "'='' OR  a.uname  like '%" + value_nm + "%' ) ";
                if (po_no != "" || product != "")
                {
                    varname2 = varname2 + " and a.userid in (select v.staff_id from d_pro_unit_staff as v ";
                    varname2 = varname2 + " join w_actual as n on v.id_actual=n.id_actual ";
                    varname2 = varname2 + " join w_actual_primary as l on n.at_no =l.at_no ";
                    varname2 = varname2 + " Where ('" + po_no + "'='' OR  n.at_no  like '%" + po_no + "%' )";
                    varname2 = varname2 + " and ('" + product + "'='' OR l.product  like '%" + product + "%' ))";
                }
                varname2 = varname2 + " and ('" + mc_type + "'='' OR  a.position_cd  like '%" + mc_type + "%' ) group by a.userid";

            }
            if (type_kind == "mold")
            {

                ViewBag.name = "Mold";
                var List = new ArrayList();
                ViewBag.List = List;
                varname3 = varname3 + "select (a.md_nm) mc_nm,(a.md_no) mc_no,('Mold')kind, " + "\n";
                var count_day = (last - startOfMonth).TotalDays;
                for (int i = 0; i < (count_day + 1); i++)
                {
                    var bien = startOfMonth.AddDays(i);
                    var bien_doi = bien.ToString("dd");
                    List.Add(bien_doi);
                    var text = bien.ToString("yyyyMMdd");
                    varname3 = varname3 + "(case when " + "\n";
                    varname3 = varname3 + "	 (select count(k.pmid) from d_pro_unit_mc as k " + "\n";
                    varname3 = varname3 + "			where '" + text + "' between (Date_format(k.start_dt,'%Y%m%d')) and (Date_format(k.end_dt,'%Y%m%d')) and k.mc_no=a.md_no)>0 " + "\n";
                    varname3 = varname3 + "		 then 	'bg-warning' else 'bg-white' end)day_" + (i + 1) + " , " + "\n";
                    varname3 = varname3 + "(	SELECT Group_concat(DISTINCT (Concat('PO', Cast(Substr(m.at_no, 3, 11)AS UNSIGNED), ' ' , m.name, ' Start: ', Concat(Substring(n.start_dt, 1, 4), '-', " + "\n";
                    varname3 = varname3 + "       Substring(n.start_dt, 5, 2), '-', Substring(n.start_dt, 7, 2), ' ', Substring(n.start_dt, 9, 2), ':', Substring(n.start_dt, 11, 2), ':', " + "\n";
                    varname3 = varname3 + "       Substring(n.start_dt, 13, 2)),  ' End: ', Concat(Substring(n.end_dt, 1, 4), '-',  Substring(n.end_dt, 5, 2), '-', " + "\n";
                    varname3 = varname3 + "       Substring(n.end_dt, 7, 2), ' ', Substring(n.end_dt, 9, 2), ':',  Substring(n.end_dt, 11, 2), ':', Substring(n.end_dt, 13, 2)) ) )  SEPARATOR ',') " + "\n";
                    varname3 = varname3 + "       cv " + "\n";
                    varname3 = varname3 + "FROM   d_pro_unit_mc AS n " + "\n";
                    varname3 = varname3 + "       JOIN w_actual AS m " + "\n";
                    varname3 = varname3 + "         ON n.id_actual = m.id_actual " + "\n";
                    varname3 = varname3 + "WHERE  n.mc_no = a.md_no " + "\n";
                    varname3 = varname3 + "       AND '" + text + "' BETWEEN ( Date_format(n.start_dt, '%Y%m%d') ) AND ( " + "\n";
                    varname3 = varname3 + "                              Date_format(n.end_dt, '%Y%m%d') ) ) po" + (i + 1) + ",";
                }
                varname3 = varname3 + " ANY_VALUE(a.mdno) id " + "\n";
                varname3 = varname3 + "from d_mold_info as a " + "\n";

                //where
                varname3 = varname3 + " Where ('" + value_code + "'='' OR  a.md_no  like '%" + value_code + "%' )";
                if (po_no != "" || product != "")
                {
                    varname3 = varname3 + " and a.md_no in (select v.md_no from d_pro_unit_mc as v ";
                    varname3 = varname3 + " join w_actual as n on v.id_actual=n.id_actual ";
                    varname3 = varname3 + " join w_actual_primary as l on n.at_no =l.at_no ";
                    varname3 = varname3 + " Where ('" + po_no + "'='' OR  n.at_no  like '%" + po_no + "%' )";
                    varname3 = varname3 + " and ('" + product + "'='' OR l.product  like '%" + product + "%' ))";
                }
                varname3 = varname3 + " and ('" + value_nm + "'='' OR  a.md_nm  like '%" + value_nm + "%' ) group by a.md_no";

            }
            var varname_all = varname1 + "   " + varname2 + "   " + varname3;

            var data = db.Database.SqlQuery<model_daymc_wk_mold>(varname_all).ToList();



            ViewBag.start = date_start;
            ViewBag.end = date_end;
            ViewBag.month = time_now.ToString("MMMM");

            return PartialView(data);

        }
        public class ProductActivition
        {
            public List<modelTableMAchine> ProductActivitionFaile { get; set; }
           
        }
        public class modelTableMAchine
        {
            public string id { get; set; }
            public string date { get; set; }
            public string mc_no { get; set; }
            public string mc_nm { get; set; }
            public string at_no { get; set; }
            public string productCode { get; set; }
            public string start_dt { get; set; }
            public string end_dt { get; set; }
         

        }
        public class model_daymc_wk_mold
        {
            public string id { get; set; }
            public string fo_no { get; set; }
            public string kind { get; set; }
            public string mc_no { get; set; }
            public string mc_nm { get; set; }
            public string mc_type { get; set; }
            public string date { get; set; }
            public string day_1 { get; set; }
            public string day_2 { get; set; }
            public string day_3 { get; set; }
            public string day_4 { get; set; }
            public string day_5 { get; set; }
            public string day_6 { get; set; }
            public string day_7 { get; set; }
            public string day_8 { get; set; }
            public string day_9 { get; set; }
            public string day_10 { get; set; }
            public string day_11 { get; set; }
            public string day_12 { get; set; }
            public string day_13 { get; set; }
            public string day_14 { get; set; }
            public string day_15 { get; set; }
            public string day_16 { get; set; }
            public string day_17 { get; set; }
            public string day_18 { get; set; }
            public string day_19 { get; set; }
            public string day_20 { get; set; }
            public string day_21 { get; set; }
            public string day_22 { get; set; }
            public string day_23 { get; set; }
            public string day_24 { get; set; }
            public string day_25 { get; set; }
            public string day_26 { get; set; }
            public string day_27 { get; set; }
            public string day_28 { get; set; }
            public string day_29 { get; set; }
            public string day_30 { get; set; }
            public string day_31 { get; set; }



            public string po1 { get; set; }
            public string po2 { get; set; }
            public string po3 { get; set; }
            public string po4 { get; set; }
            public string po5 { get; set; }
            public string po6 { get; set; }
            public string po7 { get; set; }
            public string po8 { get; set; }
            public string po9 { get; set; }
            public string po10 { get; set; }
            public string po11 { get; set; }
            public string po12 { get; set; }
            public string po13 { get; set; }
            public string po14 { get; set; }
            public string po15 { get; set; }
            public string po16 { get; set; }
            public string po17 { get; set; }
            public string po18 { get; set; }
            public string po19 { get; set; }
            public string po20 { get; set; }
            public string po21 { get; set; }
            public string po22 { get; set; }
            public string po23 { get; set; }
            public string po24 { get; set; }
            public string po25 { get; set; }
            public string po26 { get; set; }
            public string po27 { get; set; }
            public string po28 { get; set; }
            public string po29 { get; set; }
            public string po30 { get; set; }
            public string po31 { get; set; }

        }
        //public ActionResult GetWoMc_wk_mold(string start_dt, string end_dt, string value, string sts)
        //{
        //    String varname1 = "";
        //    //start_dt = Convert.ToDateTime(start_dt).ToString("yyyyMMddHHmmss");
        //    //end_dt = Convert.ToDateTime(end_dt).ToString("yyyyMMddHHmmss");

        //    if (sts == "mc" || sts == "mold")
        //    {
        //        varname1 = varname1 + "SELECT " + "\n";
        //        varname1 = varname1 + "       a.pmid  id, " + "\n";
        //        varname1 = varname1 + "      (case when (select count(mc_nm) from d_machine_info where a.mc_no=mc_no) =1 then (select mc_nm from d_machine_info where a.mc_no=mc_no)  " + "\n";
        //        varname1 = varname1 + "      else (select md_nm from d_mold_info where a.mc_no=md_no) end)  mc_nm, " + "\n";
        //        varname1 = varname1 + "        (CONCAT(substr(a.fo_no, 1, 1), CONVERT(substr(a.fo_no, 2, 11),INT))) as fo_no, " + "\n";
        //        varname1 = varname1 + "       (CONCAT(substr(b.bom_no, 1, 1), CONVERT(substr(b.bom_no, 2, 11),INT),substr(b.bom_no, 12, 1))) as bom_no, " + "\n";
        //        varname1 = varname1 + "       Date_format(a.start_dt, '%Y-%m-%d %H:%i') start_dt, " + "\n";
        //        varname1 = varname1 + "       Date_format(a.end_dt, '%Y-%m-%d %H:%i')   end_dt, " + "\n";
        //        varname1 = varname1 + "        a.mc_no, " + "\n";
        //        varname1 = varname1 + "       (SELECT style_no  FROM   d_bom_info  WHERE  bom_no = b.bom_no)     product, " + "\n";
        //        varname1 = varname1 + "       b.line_no  " + "\n";
        //        varname1 = varname1 + "FROM   d_pro_unit_mc AS a " + "\n";
        //        varname1 = varname1 + "       JOIN s_order_factory_info AS b " + "\n";
        //        varname1 = varname1 + "         ON a.fo_no = b.fo_no " + "\n";
        //        varname1 = varname1 + "WHERE  a.mc_no = '" + value + "' " + "\n";
        //        varname1 = varname1 + " and ('" + start_dt + "'='' OR DATE_FORMAT(a.start_dt,'%Y%m%d') >= DATE_FORMAT('" + start_dt + "','%Y%m%d'))";
        //        varname1 = varname1 + "AND ('" + end_dt + "'='' OR DATE_FORMAT(a.end_dt,'%Y%m%d') <= DATE_FORMAT('" + end_dt + "','%Y%m%d')) ";

        //    }
        //    if (sts == "wk")
        //    {

        //        varname1 = varname1 + "SELECT a.staff_id mc_no,(select uname from mb_info where userid= a.staff_id)mc_nm, " + "\n";
        //        varname1 = varname1 + "       a.psid  id, " + "\n";
        //        varname1 = varname1 + "       Date_format(a.start_dt, '%Y-%m-%d %H:%i') start_dt, " + "\n";
        //        varname1 = varname1 + "       Date_format(a.end_dt, '%Y-%m-%d %H:%i')   end_dt, " + "\n";
        //        varname1 = varname1 + "       (CONCAT(substr(a.fo_no, 1, 1), CONVERT(substr(a.fo_no, 2, 11),INT))) as fo_no," + "\n";
        //        varname1 = varname1 + "       (CONCAT(substr(b.bom_no, 1, 1), CONVERT(substr(b.bom_no, 2, 11),INT),substr(b.bom_no, 12, 1))) as bom_no, " + "\n";
        //        varname1 = varname1 + "       (SELECT style_no  FROM   d_bom_info  WHERE  bom_no = b.bom_no)     product, " + "\n";
        //        varname1 = varname1 + "       (SELECT color FROM   d_pro_unit  WHERE  a.prounit_cd = prounit_cd)  backgroundColor " + "\n";
        //        varname1 = varname1 + "FROM   d_pro_unit_staff AS a " + "\n";
        //        varname1 = varname1 + "       JOIN s_order_factory_info AS b " + "\n";
        //        varname1 = varname1 + "         ON a.fo_no = b.fo_no " + "\n";
        //        varname1 = varname1 + "WHERE  a.staff_id = '" + value + "'";
        //        varname1 = varname1 + " and ('" + start_dt + "'='' OR DATE_FORMAT(a.start_dt,'%Y/%m/%d') >= DATE_FORMAT('" + start_dt + "','%Y/%m/%d'))";
        //        varname1 = varname1 + "AND ('" + end_dt + "'='' OR DATE_FORMAT(a.end_dt,'%Y/%m/%d') <= DATE_FORMAT('" + end_dt + "','%Y/%m/%d')) ";


        //    }

        //    var model = db.Database.SqlQuery<getdata_mc_wk_mold>(varname1).ToList();
        //    return Json(new { data = model }, JsonRequestBehavior.AllowGet);

        //}
        public ActionResult GetPopupmachine_resource()
        {
            var mc_type = (Request["mc_type"] != null ? Request["mc_type"] : "");
            var mc_no = (Request["mc_no"] != null ? Request["mc_no"] : "");
            var mc_nm = (Request["mc_nm"] != null ? Request["mc_nm"] : "");
            var list = db.d_machine_info.Where(x => (mc_type == "" || x.mc_type == mc_type) && (mc_no == "" || x.mc_no.Contains(mc_no)) && (mc_nm == "" || x.mc_nm.Contains(mc_nm))).ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetPopupstaff_resource()
        {
            var userid = (Request["userid"] != null ? Request["userid"] : "");
            var uname = (Request["uname"] != null ? Request["uname"] : "");
            var position_cd = (Request["position_cd"] != null ? Request["position_cd"] : "");
            var list = db.mb_info.Where(x => (userid == "" || x.userid.Contains(userid)) && (uname == "" || x.uname.Contains(uname)) && (position_cd == "" || x.position_cd.Contains(position_cd))).Select(a => new
            {
                position_cd = db.comm_dt.Where(x => x.dt_cd == a.position_cd && x.mt_cd == "COM018").Select(x => x.dt_nm),
                userid = a.userid,
                uname = a.uname,
            }).ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetPopupmold_resource(Pageing pageing)
        {
            var md_no = Request["md_no"];
            var md_nm = Request["md_nm"];

            Dictionary<string, string> list = PagingAndOrderBy(pageing, " ORDER BY MyDerivedTable.mdno DESC ");
            String tempSql = "";
            tempSql = tempSql + "SELECT ROW_NUMBER() OVER ( " + "\n";
            tempSql = tempSql + "ORDER BY mdno DESC) AS RowNum " + "\n";
            tempSql = tempSql + ", a.* " + "\n";
            tempSql = tempSql + "FROM d_mold_info a " + "\n";
            tempSql = tempSql + "WHERE (''='' OR a.md_no LIKE '%%') AND (''='' OR a.md_nm LIKE '%%') ";
            tempSql = tempSql + " and ('" + md_no + "'='' OR   a.md_no like'%" + md_no + "%')";
            tempSql = tempSql + " and ('" + md_nm + "'='' OR   a.md_nm like'%" + md_nm + "%')  ";

            string countSql = " SELECT COUNT(*) FROM ( " + tempSql + " ) MyDerivedTable ";
            string viewSql = " SELECT * FROM ( " + tempSql + " ) MyDerivedTable "

                + " WHERE MyDerivedTable.RowNum BETWEEN " + list["start"] + " AND " + list["end"] + " "
                + list["orderBy"];
            return SearchAndPaging(countSql, viewSql, int.Parse(list["index"]), int.Parse(list["size"]));

        }
        #endregion
        public JsonResult GetJsonPersons(DataTable data)
        {
            var lstPersons = new { rows = GetTableRows(data) };
            return Json(lstPersons, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetJsonPersons1(DataTable data)
        {
            var lstPersons = new { rows = GetTableRows(data) };
            return Json(lstPersons, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetJsonPersons3(DataTable data)
        {
            var lstPersons = new { rows = GetTableRows(data) };
            return Json(lstPersons, JsonRequestBehavior.AllowGet);
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

        #region BobbinMgt

        public ActionResult BobbinMgt()
        {
            HttpCookie cookie = HttpContext.Request.Cookies["language"];
            if (cookie != null)
            {
                ViewBag.language = cookie.Value;
            }

            return View();
        }

        public ActionResult getBobbinMgtData(d_bobbin_info d_bobbin_info)
        {
            var value = db.d_bobbin_info.Where(x => (!x.bb_no.StartsWith("AUTO-BOB")) && (!x.bb_no.StartsWith("AUTO-TRAY")) && (x.mc_type == "BOB" || x.mc_type == "TRB")).OrderByDescending(x => x.chg_dt).ToList();
            var bobbinMgtData = new { rows = value };
            return Json(bobbinMgtData, JsonRequestBehavior.AllowGet);
        }

        public string autobb_no(int id)
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

        public ActionResult insertBobbinMgt(d_bobbin_info d_bobbin_info, string bb_no, string purpose, string bb_nm, string re_mark, string mc_type)
        {
            int count = db.d_bobbin_info.Where(item => item.bb_no == bb_no).ToList().Count();
            if (count == 0)
            {
                d_bobbin_info.reg_dt = DateTime.Now;
                d_bobbin_info.chg_dt = DateTime.Now;
                d_bobbin_info.bb_no = bb_no;
                d_bobbin_info.purpose = purpose;
                d_bobbin_info.bb_nm = bb_nm;
                d_bobbin_info.re_mark = re_mark;
                d_bobbin_info.barcode = bb_no;
                d_bobbin_info.mc_type = mc_type;
                d_bobbin_info.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                d_bobbin_info.reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();

                if (ModelState.IsValid)
                {
                    db.d_bobbin_info.Add(d_bobbin_info);
                    db.SaveChanges();
                    return Json(new { result = count }, JsonRequestBehavior.AllowGet);
                }
            }
            return Json(new { result = count }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult updateBobbinMgt(int bno, string bb_no, string purpose, string bb_nm, string re_mark, string mc_type)
        {
            try
            {
                int count = db.d_bobbin_info.Where(item => item.bb_no == bb_no).ToList().Count();
                if (count > 0)
                {
                    d_bobbin_info d_bobbin_info = db.d_bobbin_info.Find(bno);
                    //d_bobbin_info.chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                    d_bobbin_info.purpose = purpose;
                    d_bobbin_info.bb_nm = bb_nm;
                    d_bobbin_info.re_mark = re_mark;
                    d_bobbin_info.mc_type = mc_type;
                    d_bobbin_info.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    //d_bobbin_info.reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();

                    if (ModelState.IsValid)
                    {
                        db.Entry(d_bobbin_info).State = EntityState.Modified;
                        db.SaveChanges(); // line that threw exception
                        return Json(new { result = count }, JsonRequestBehavior.AllowGet);
                    }
                }

                return Json(new { result = count }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public JsonResult deleteBobbinMgt(int bno)
        {
            int count = db.d_bobbin_info.Where(x => x.bno == bno).ToList().Count();
            var result = new { result = true };
            if (count > 0)
            {
                d_bobbin_info d_bobbin_info = db.d_bobbin_info.Find(bno);
                db.d_bobbin_info.Remove(d_bobbin_info);
                db.SaveChanges();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            result = new { result = false };
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public JsonResult searchBobbinMgt()
        {
            try
            {
                // Get Data from ajax function
                string bb_no_search = Request.QueryString["s_bb_no"];
                string bb_nm_search = Request.QueryString["s_bb_nm"];
                var sql = new StringBuilder();
                sql.Append(" SELECT si.* ")
                    .Append("FROM d_bobbin_info as si ")
                     .Append("WHERE ('" + bb_no_search + "'='' OR   si.bb_no like '" + bb_no_search + "' )")
                     .Append("AND ('" + bb_nm_search + "'='' OR   si.bb_nm like '" + bb_nm_search + "' )")
                           .Append("ORDER BY si.chg_dt desc ");
                var data = db.d_bobbin_info.SqlQuery(sql.ToString()).ToList<d_bobbin_info>();
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public JsonResult searchBobbinMgt_TSD()
        {
            try
            {
                // Get Data from ajax function
                string bb_no_search = Request.QueryString["s_bb_no"];
                string bb_nm_search = Request.QueryString["s_bb_nm"];
                StringBuilder varname1 = new StringBuilder();
                varname1.Append("SELECT si.*,(SELECT b.at_no FROM w_material_info AS a \n");
                varname1.Append("JOIN w_actual AS b ON a.id_actual=b.id_actual WHERE a.mt_cd=si.mt_cd LIMIT 1)at_no \n");
                varname1.Append("FROM d_bobbin_info AS si \n");
                varname1.Append("WHERE  si.bb_no NOT LIKE '%AUTO%' and ('%" + bb_no_search + "%'='' OR   si.bb_no like '%" + bb_no_search + "%' )");
                varname1.Append("AND ('%" + bb_nm_search + "%'='' OR   si.bb_nm like '%" + bb_nm_search + "%' )");
                varname1.Append("ORDER BY si.bb_no asc ");
                return new InitMethods().ConvertDataTableToJsonAndReturn(varname1);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public ActionResult _Bobbin(string id)
        {
            ViewData["Message"] = id;
            return View();
        }

        public ActionResult PrintMultiBobbinQRCode(string id)
        {
            var multiIDs = id.TrimStart('[').TrimEnd(']').Split(',');
            var row_data = new List<d_bobbin_info>();
            for (int i = 0; i < multiIDs.Length; i++)
            {
                var id2 = int.Parse(multiIDs[i]);
                var sql = new StringBuilder();
                sql.Append(" SELECT a.* ")
                   .Append(" from d_bobbin_info as a")
                   .Append(" where a.bno ='" + id2 + "'");

                var data = db.d_bobbin_info.SqlQuery(sql.ToString()).ToList<d_bobbin_info>();
                row_data.AddRange(data);
            }

            return Json(row_data, JsonRequestBehavior.AllowGet);
        }

        #endregion BobbinMgt

        #region Bominfo
        public ActionResult BomMgt()
        {
            return SetLanguage("");
           
        }

        public ActionResult GetBom(Pageing Pageing)
        {
            try
            {

                string bom_no = Request["bom_no"] == null ? "" : Request["bom_no"].Trim();
                string product = Request["product"] == null ? "" : Request["product"].Trim();
                string product_nm = Request["product_nm"] == null ? "" : Request["product_nm"].Trim();
                string md_cd = Request["md_cd"] == null ? "" : Request["md_cd"].Trim();
                string mt_no = Request["mt_no"] == null ? "" : Request["mt_no"].Trim();
                string mt_nm = Request["mt_no"] == null ? "" : Request["mt_nm"].Trim();

                StringBuilder varname1 = new StringBuilder();
                varname1.Append("SELECT MAX(a.bid) AS bid,max(b.md_cd) AS md_cd,max(b.style_nm ) AS style_nm,MAX(b.reg_dt) AS reg_dt, MAX(b.reg_id) AS reg_id  , MAX(b.style_no) AS style_no, max(a.IsApply) as IsApply  \n");
             
                varname1.Append(" FROM d_bom_info AS a \n");
                varname1.Append(" JOIN d_style_info AS b ON a.style_no=b.style_no ");
                varname1.Append("Where ('" + bom_no + "'='' OR a.bom_no LIKE '%" + bom_no + "%') ");
                varname1.Append("and ('" + product + "'='' OR a.style_no LIKE '%" + product + "%') ");
                varname1.Append("and ('" + product_nm + "'='' OR b.style_nm LIKE '%" + product_nm + "%') ");
                varname1.Append("and ('" + md_cd + "'='' OR b.md_cd LIKE '%" + md_cd + "%') ");
                varname1.Append("and ('" + mt_no + "'='' OR a.mt_no LIKE '%" + mt_no + "%') ");
                varname1.Append("and ('" + mt_nm + "'='' OR (SELECT mt_nm FROM d_material_info WHERE mt_no=a.mt_no) LIKE '%" + mt_nm + "%')  GROUP BY a.style_no ");

                DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

                int total = dt.Rows.Count;
                var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("bid"));
                return new InitMethods().ReturnJsonResultWithPaging(Pageing, total, result);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult getBomdsMaterial(Pageing Pageing, string style_no)
        {
            try
            {

                string bom_no = Request["bom_no"] == null ? "" : Request["bom_no"].Trim();
                string product = Request["product"] == null ? "" : Request["product"].Trim();
                string product_nm = Request["product_nm"] == null ? "" : Request["product_nm"].Trim();
                string md_cd = Request["md_cd"] == null ? "" : Request["md_cd"].Trim();
                string mt_no = Request["mt_no"] == null ? "" : Request["mt_no"].Trim();
                string mt_nm = Request["mt_no"] == null ? "" : Request["mt_nm"].Trim();

           

                StringBuilder varname1 = new StringBuilder();
                varname1.Append("SELECT a.*, \n");
                varname1.Append("(SELECT mt_nm FROM d_material_info WHERE mt_no=a.mt_no) mt_nm, \n");
                varname1.Append("b.md_cd,b.style_nm \n");
                varname1.Append(" FROM d_bom_info AS a \n");
                varname1.Append(" JOIN d_style_info AS b ON a.style_no=b.style_no ");
                varname1.Append("Where ('" + bom_no + "'='' OR a.bom_no LIKE '%" + bom_no + "%') ");
                varname1.Append("and a.style_no = '" + style_no + "' ");
                varname1.Append("and ('" + product + "'='' OR a.style_no LIKE '%" + product + "%') ");
                varname1.Append("and ('" + product_nm + "'='' OR b.style_nm LIKE '%" + product_nm + "%') ");
                varname1.Append("and ('" + md_cd + "'='' OR b.md_cd LIKE '%" + md_cd + "%') ");
                varname1.Append("and ('" + mt_no + "'='' OR a.mt_no LIKE '%" + mt_no + "%') ");
                varname1.Append("and ('" + mt_nm + "'='' OR (SELECT mt_nm FROM d_material_info WHERE mt_no=a.mt_no) LIKE '%" + mt_nm + "%')  ");

                DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

                int total = dt.Rows.Count;
                var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("bid"));
                return new InitMethods().ReturnJsonResultWithPaging(Pageing, total, result);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult getBomMaterial(int id)
        {
            try
            {
                IEnumerable<MaterialBomModel> value = _IDomMS.GetListmaterialbom(id);

                return Json(value, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult getBomdsMaterialCap3(string style_no, string mt_no)
        {
            try
            {
                IEnumerable<CreateBomMaterialModel> value = _IDomMS.GetListmaterialbomcap3(style_no, mt_no);

                return Json(value, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult CreateBomManagement([System.Web.Http.FromBody] CreateBomMgtModel model)
        {
            if (!ModelState.IsValid)
                return null;
            try
            {
                string checkExist = @"SELECT EXISTS(SELECT bid FROM d_bom_info  WHERE style_no=@1 AND mt_no = @2);";
                int kq = db.Database.SqlQuery<int>(checkExist,
                    new MySqlParameter("@1", model.ProductCode),
                     new MySqlParameter("@2", model.materialNo)
                    ).FirstOrDefault();
                if (kq > 0)
                {
                    return Json(new { result = false, message = "Product " + exits }, JsonRequestBehavior.AllowGet);
                }
                //string checkExist = "SELECT EXISTS(SELECT * FROM d_bom_info WHERE ProductCode='" + model.ProductCode + "')";
                // var check = db.Database.SqlQuery<bool>(checkExist).FirstOrDefault();
                //if (check)
                //{
                //    return Json(new { result = false, message = "Product " + exits }, JsonRequestBehavior.AllowGet);
                //}
                d_bom_info Bom = new d_bom_info();
                Bom.bom_no = Guid.NewGuid().ToString();
                Bom.style_no = model.ProductCode;
                Bom.mt_no = model.materialNo;
                Bom.cav = model.cavit;
                Bom.buocdap = Decimal.ToDouble(model.buocdap);
                Bom.need_time = model.need_time;
                Bom.need_m = (Decimal.ToDouble(model.buocdap) / 1000 / model.cavit * model.need_time);
                Bom.reg_dt = DateTime.Now;
                Bom.chg_dt = DateTime.Now;
                Bom.del_yn = "N";
                Bom.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                Bom.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();

                if (model.isActive)
                {
                    _IDomMS.UpdateBomDeTinhHieuSuat(model.ProductCode);
                }
                int idBOMInfo = _IDomMS.InsertToBomInfo(Bom, model.isActive);
               
                //db.d_bom_info.Add(Bom);
                //db.SaveChanges();//Lưu lên DB để lấy BOMID
                //string sql = "";
                //for (int i = 0; i < model.ListMaterial.Length; i++)
                //{
                //    if (i == 0)
                //        sql += "INSERT INTO MaterialBOM(`MaterialID`,`BOMID`,`CreateOn`,`ProductCode`) Values(" + model.ListMaterial[i] + "," + idBOMInfo + ",NOW(),'" + model.ProductCode + "')";
                //    else
                //        sql += ",(" + model.ListMaterial[i] + "," + idBOMInfo + ",NOW(),'" + model.ProductCode + "')";
                //}
                //int Result = db.Database.ExecuteSqlCommand(sql);

                var BOMInfo = _IDomMS.GetListBom(idBOMInfo);

                return Json(new { result = true, message = ss, data = BOMInfo }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {

                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }
        //public ActionResult UpdateMaterialDeTinhHieuSuat(int BOMID, int MaterialBOMID)
        public ActionResult UpdateMaterialDeTinhHieuSuat(int BOMID, int MaterialBOMID)
        {
            _IDomMS.UpdateMaterialBomDeTinhHieuSuat(BOMID, MaterialBOMID);


            return Json(new { result = true, BOMID }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult UpdateProductDeApply(int bid, string IsApply)
        {
            _IDomMS.UpdateProductDeApply(bid, IsApply);


            return Json(new { result = true, bid , IsApply }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ADdBomManagement([System.Web.Http.FromBody] CreateBomMaterialModel model)
        {
            if (!ModelState.IsValid)
                return null;
            try
            {
                //kierm tra product trong bảng bom_info
                var checkBOmExist = _IDomMS.CheckBomExist(model.ProductCode,model.MaterialPrarent);
                if (checkBOmExist == null)
                {
                    return Json(new { result = false, message = "Product này không tồn tại" }, JsonRequestBehavior.AllowGet);
                }
               
                //insert material lại
              
                for (int i = 0; i < model.ListMaterial.Length; i++)
                {
                    //if (i == 0)
                    //    sql += "INSERT  INTO MaterialBOM(`MaterialID`,`BOMID`,`CreateOn`,`ProductCode`) Values(" + model.ListMaterial[i] + "," + checkBOmExist.bid + ",NOW(),'" + model.ProductCode + "')";
                    //else
                    //    sql += ",(" + model.ListMaterial[i] + "," + checkBOmExist.bid + ",NOW(),'" + model.ProductCode + "')";

                    var MaterialBOM = new CreateBomMaterialModel();
                    MaterialBOM.ProductCode = model.ProductCode;
                    MaterialBOM.MaterialPrarent = model.MaterialPrarent;
                    MaterialBOM.MaterialNo = model.ListMaterial[i];
                    MaterialBOM.CreateDate = DateTime.Now;
                    MaterialBOM.ChangeDate = DateTime.Now;
                    MaterialBOM.CreateId = Session["userid"] == null ? null : Session["userid"].ToString();
                    MaterialBOM.ChangeId = Session["userid"] == null ? null : Session["userid"].ToString();

                   _IDomMS.InsertToMaterialBom(MaterialBOM);
                }
                //int Result = db.Database.ExecuteSqlCommand(sql);

                
                IEnumerable<CreateBomMaterialModel> value = _IDomMS.GetListmaterialbomcap3(model.ProductCode, model.MaterialPrarent );
                return Json(new { result = true, message = "", data = value }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {

                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult DeleteMaterialBomManagement(int? id)
        {
            _IDomMS.DeleteMaterialBomForId(id);


            return Json(new { result = true, id }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult CreateBomMgt(d_bom_info table)
        {
            try
            {
                //kiểm tra trong bom đã có tồn tại nguyên vật liệu này chưa
                if (db.d_bom_info.Any(x => x.style_no == table.style_no))
                {
                    return Json(new { result = false, message = "Product" + exits }, JsonRequestBehavior.AllowGet);
                }
                if (db.d_bom_info.Any(x => x.bom_no == table.bom_no && x.mt_no == table.mt_no))
                {
                    return Json(new { result = false, message = "MT Code " + exits }, JsonRequestBehavior.AllowGet);
                }
                //check tồn tại mt_no này không 
                var ch_mt_no = db.d_material_info.Where(x => x.mt_no == table.mt_no).SingleOrDefault();
                if (ch_mt_no == null)
                {
                    return Json(new { result = false, message = "Material " + exits }, JsonRequestBehavior.AllowGet);
                }
                //check tồn tại product này không 
                var pr = db.d_style_info.Where(x => x.style_no == table.style_no).SingleOrDefault();
                if (pr == null)
                {
                    return Json(new { result = false, message = "Product " + exits }, JsonRequestBehavior.AllowGet);
                }
                if (table.bom_no == "" || table.bom_no == null)
                {
                    var menuCd = string.Empty;
                    var subMenuIdConvert = 0;
                    var ds = db.d_bom_info.Count();
                    if (ds == 0)
                    {
                        table.bom_no = "B0000000001";
                    }
                    else
                    {
                        var list = db.d_bom_info.OrderByDescending(x => x.bom_no).FirstOrDefault();
                        var subMenuId = list.bom_no.Substring(list.bom_no.Length - 10, 10);
                        int.TryParse(subMenuId, out subMenuIdConvert);
                        menuCd = "B" + string.Format("{0}{1}", menuCd, CreateId((subMenuIdConvert + 1)));
                        table.bom_no = menuCd;
                    }
                }

                table.need_m = (table.buocdap / 1000 / table.cav * table.need_time);
                table.mt_no = ch_mt_no.mt_no;
                table.style_no = pr.style_no;
                table.reg_dt = DateTime.Now;
                table.chg_dt = DateTime.Now;
                table.del_yn = "N";
                table.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                table.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                if (ModelState.IsValid)
                {
                    db.Entry(table).State = EntityState.Added;
                    db.SaveChanges();
                }
                var data = new
                {
                    bid = table.bid,
                    bom_no = table.bom_no,
                    mt_no = table.mt_no,
                    mt_nm = ch_mt_no.mt_nm,
                    style_no = table.style_no,
                    style_nm = pr.style_nm,
                    md_cd = pr.md_cd,
                    need_time = table.need_time,
                    cav = table.cav,
                    need_m = table.need_m,
                    buocdap = table.buocdap,
                    reg_dt = table.reg_dt,
                };
                return Json(new { result = true, message = ss, data }, JsonRequestBehavior.AllowGet);


            }
            catch (Exception e)
            {

                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }

        }
        public JsonResult ModifyBomMgt(d_bom_info table, int? isActive)
        {
            try
            {
                //kiểm tra trong bom đã có tồn tại không
                var modify = db.d_bom_info.Find(table.bid);
                if (modify == null)
                {
                    return Json(new { result = false, message = exits }, JsonRequestBehavior.AllowGet);
                }
                //var ch_mt_no = db.d_material_info.Where(x => x.mt_no == table.mt_no).SingleOrDefault();
                //if (ch_mt_no == null)
                //{
                //    return Json(new { result = false, message = "Material " + exits }, JsonRequestBehavior.AllowGet);
                //}
                //check tồn tại product này không 
                //var pr = db.d_style_info.Where(x => x.style_no == table.style_no).SingleOrDefault();
                //if (pr == null)
                //{
                //    return Json(new { result = false, message = "Product " + exits }, JsonRequestBehavior.AllowGet);
                //}

                string checkExist = @"SELECT EXISTS(SELECT bid FROM d_bom_info  WHERE style_no=@1 AND mt_no = @2 and bid != @3 );";
                int kq = db.Database.SqlQuery<int>(checkExist,
                    new MySqlParameter("@1", table.style_no),
                     new MySqlParameter("@2", table.mt_no),
                     new MySqlParameter("@3", table.bid)
                    ).FirstOrDefault();
                if (kq > 0)
                {
                    return Json(new { result = false, message = "Product " + exits }, JsonRequestBehavior.AllowGet);
                }

                modify.cav = table.cav;
                modify.need_time = table.need_time;
                modify.buocdap = table.buocdap;
                modify.need_m = (table.buocdap / 1000 / table.cav * table.need_time);
                modify.mt_no = table.mt_no;
                modify.style_no = table.style_no;
                modify.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();

                if (isActive == 1)
                {
                    _IDomMS.UpdateBomDeTinhHieuSuat(table.style_no);
                }
                _IDomMS.UpdateBomInfo(modify, isActive);

                //if (ModelState.IsValid)
                //{
                //    db.Entry(modify).State = EntityState.Modified;
                //    db.SaveChanges();
                //}
                //var data = new
                //{
                //    bid = modify.bid,
                //    bom_no = modify.bom_no,
                //    mt_no = modify.mt_no,
                //    //mt_nm = ch_mt_no.mt_nm,
                //    style_no = modify.style_no,
                //    //style_nm = table.style_nm,
                //    //md_cd = table.md_cd,
                //    need_time = modify.need_time,
                //    cav = modify.cav,
                //    need_m = modify.need_m,
                //    buocdap = modify.buocdap,
                //    reg_dt = modify.reg_dt,
                //};
                var data = _IDomMS.GetListBom(modify.bid);
                return Json(new { result = true, message = ss, data }, JsonRequestBehavior.AllowGet);


            }
            catch (Exception e)
            {

                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }

        }
        public JsonResult deletebom(string style_no)
        {
            try
            {
                //var notfind = 0;
                //var success = 0;
                //var data = id.TrimStart('[').TrimEnd(']').Split(',');
                //var list_id = new ArrayList();
                //for (int i = 0; i < data.Length; i++)
                //{
                //    var bid = int.Parse(data[i]);
                //    var check_bom = db.d_bom_info.Find(bid);
                //    if (check_bom == null)
                //    {
                //        notfind++;
                //    }
                //    else
                //    {
                //        if (ModelState.IsValid)
                //        {
                //            db.Entry(check_bom).State = EntityState.Deleted;
                //            db.SaveChanges();
                //            list_id.Add(check_bom.bid);
                //        }

                //        success++;
                //    }

                //}
                //if (success > 0)
                //{
                //    return Json(new { result = true, message = ss, notfind, success, list_id }, JsonRequestBehavior.AllowGet);

                //}
                var abc = _IDomMS.DeleteBomInfo(style_no);
               // var abcd = _IDomMS.DeleteAaLlMaterialBom(id);
                if (abc > 0)
                {
                    return Json(new { result = true, message = "Xóa thành công" }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = exits }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult Upload_BOM(d_bom_info table, List<d_bom_info> insertmaterial_tmp)
        {
            try
            {
                var count = 0;
                var kiemtra = 0;
                var style = 0;
                var mt_cd = 0;
                var exit_style = 0;
                var cav = 0;
                var buocdap = 0;
                var need_time = 0;
                var bom = "";
                var danhsach = new ArrayList();
                foreach (var item in insertmaterial_tmp)
                {
                    if (item.cav == null)
                    {
                        kiemtra++;
                        cav++;
                    }
                    if (item.buocdap == null)
                    {
                        kiemtra++;
                        buocdap++;
                    }
                    if (item.need_time == null)
                    {
                        kiemtra++;
                        need_time++;
                    }
                    //kiểm tra trong bom đã có tồn tại nguyên vật liệu này chưa
                    if (db.d_bom_info.Any(x => x.style_no == item.style_no) && count == 0)
                    {
                        kiemtra++;
                        style++;
                        //return Json(new { result = false, message = "Product" + exits }, JsonRequestBehavior.AllowGet);
                    }
                    if (db.d_bom_info.Any(x => x.bom_no == item.bom_no && x.mt_no == item.mt_no))
                    {
                        kiemtra++;
                        mt_cd++;
                        //return Json(new { result = false, message = "MT Code " + exits }, JsonRequestBehavior.AllowGet);
                    }
                    //check tồn tại mt_no này không 
                    var ch_mt_no = db.d_material_info.Where(x => x.mt_no == item.mt_no).SingleOrDefault();
                    if (ch_mt_no == null)
                    {
                        kiemtra++;
                        mt_cd++;
                        //return Json(new { result = false, message = "Material " + exits }, JsonRequestBehavior.AllowGet);
                    }
                    //check tồn tại product này không 
                    var pr = db.d_style_info.Where(x => x.style_no == item.style_no).SingleOrDefault();
                    if (pr == null)
                    {
                        kiemtra++;
                        exit_style++;
                        //return Json(new { result = false, message = "Product " + exits }, JsonRequestBehavior.AllowGet);
                    }
                    if (kiemtra == 0)
                    {
                        if ((item.bom_no == "" || item.bom_no == null) && bom == "")
                        {
                            var menuCd = string.Empty;
                            var subMenuIdConvert = 0;
                            var ds = db.d_bom_info.Count();
                            if (ds == 0)
                            {
                                table.bom_no = "B0000000001";
                            }
                            else
                            {
                                var list = db.d_bom_info.OrderByDescending(x => x.bom_no).FirstOrDefault();
                                var subMenuId = list.bom_no.Substring(list.bom_no.Length - 10, 10);
                                int.TryParse(subMenuId, out subMenuIdConvert);
                                menuCd = "B" + string.Format("{0}{1}", menuCd, CreateId((subMenuIdConvert + 1)));
                                table.bom_no = menuCd;
                            }
                            bom = table.bom_no;
                        }
                        else
                        {
                            table.bom_no = bom;
                        }
                        table.buocdap = item.buocdap;
                        table.cav = item.cav;
                        table.need_time = item.need_time;
                        var vvv = Math.Round(Convert.ToDecimal((item.buocdap / 1000 / item.cav * item.need_time)), 2);
                        table.need_m = Convert.ToDouble(vvv);
                        table.mt_no = ch_mt_no.mt_no;
                        table.style_no = pr.style_no;
                        table.reg_dt = DateTime.Now;
                        table.chg_dt = DateTime.Now;
                        table.del_yn = "N";
                        table.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        table.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        if (ModelState.IsValid)
                        {
                            db.Entry(table).State = EntityState.Added;
                            db.SaveChanges();

                            var data = new
                            {
                                bid = table.bid,
                                bom_no = table.bom_no,
                                mt_no = table.mt_no,
                                mt_nm = ch_mt_no.mt_nm,
                                style_no = table.style_no,
                                style_nm = pr.style_nm,
                                md_cd = pr.md_cd,
                                need_time = table.need_time,
                                cav = table.cav,
                                need_m = table.need_m,
                                buocdap = table.buocdap,
                                reg_dt = table.reg_dt,
                            };
                            danhsach.Add(data);
                        }
                    }
                    count++;
                }
                if (kiemtra == 0 && danhsach.Count > 0)
                {
                    return Json(new
                    {
                        result = true,
                        danhsach = danhsach
                    }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = exits, style, mt_cd, exit_style, cav, need_time, buocdap }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {

                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }


        }
        public ActionResult DelMaterialBomManagement(int bid)
        {
           
            if (_IDomMS.DelMaterial(bid) > 0)
            {
                return Json(new { result = true, data= bid, message ="Xóa thành công"}, JsonRequestBehavior.AllowGet);
            }
            return Json(new { result = false,message ="Không thể xóa mã này"}, JsonRequestBehavior.AllowGet);

        }
        public ActionResult DelMaterialBom(int Id)
        {

            if (_IDomMS.DelMaterialBom(Id) > 0)
            {
                return Json(new { result = true, message = "Xóa thành công" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { result = false, message = "Không thể xóa mã này" }, JsonRequestBehavior.AllowGet);

        }
        private string CreateId(int id)
        {
            if (id.ToString().Length < 2)
            {
                return string.Format("000000000{0}", id);
            }

            if ((id.ToString().Length < 3) || (id.ToString().Length == 2))
            {
                return string.Format("00000000{0}", id);
            }

            if ((id.ToString().Length < 4) || (id.ToString().Length == 3))
            {
                return string.Format("0000000{0}", id);
            }

            if ((id.ToString().Length < 5) || (id.ToString().Length == 4))
            {
                return string.Format("000000{0}", id);
            }
            if ((id.ToString().Length < 6) || (id.ToString().Length == 5))
            {
                return string.Format("00000{0}", id);
            }
            if ((id.ToString().Length < 7) || (id.ToString().Length == 6))
            {
                return string.Format("0000{0}", id);
            }
            if ((id.ToString().Length < 8) || (id.ToString().Length == 7))
            {
                return string.Format("000{0}", id);
            }
            if ((id.ToString().Length < 9) || (id.ToString().Length == 8))
            {
                return string.Format("00{0}", id);
            }
            if ((id.ToString().Length < 10) || (id.ToString().Length == 9))
            {
                return string.Format("0{0}", id);
            }

            if ((id.ToString().Length < 11) || (id.ToString().Length == 10))
            {
                return string.Format("{0}", id);
            }
            return string.Empty;
        }
        #endregion

        #region Page Staus
        public ActionResult Status()
        {
            HttpCookie cookie = HttpContext.Request.Cookies["language"];
            if (cookie != null)
            {
                ViewBag.language = cookie.Value;
            }

            return View();
        }
        public JsonResult Detailcontainer_composite(string bb_no, string mt_no, string mt_cd)
        {
            try
            {
                if (!String.IsNullOrEmpty(bb_no))
                {
                    //kiem tra no có ở history không
                    var trangthai = "";
                    var history_bb = db.d_bobbin_lct_hist.Where(x => x.bb_no == bb_no).ToList();
                    if (history_bb.Count == 1)
                    {
                        //kiểm tra n có ở primary bb không
                        //nếu mã bobin rỗng thì update nó
                        var bobin_cha = db.d_bobbin_info.Where(x => x.bb_no == bb_no).SingleOrDefault();
                        if (bobin_cha != null)
                        {
                            if (String.IsNullOrEmpty(bobin_cha.mt_cd))
                            {
                                //update
                                bobin_cha.mt_cd = history_bb.FirstOrDefault().mt_cd;
                                db.Entry(bobin_cha).State = EntityState.Modified;
                                db.SaveChanges(); // line that threw exception
                            }
                        }
                        //kiểm tra mã đó đang ở đâu nhỉ
                        //ĐANG Ở ĐÂU
                        var mt_cd_bb = history_bb.FirstOrDefault().mt_cd;
                        var primary = db.w_material_info.Where(x => x.mt_cd == mt_cd_bb).SingleOrDefault();
                        //nếu trong bảng tạm không có kiểm tra bảng chính tồn tại không
                        var lct_cd = "";
                        if (primary != null)
                        {
                            var mt_sts_cd = primary.mt_sts_cd;
                            trangthai = checktrangthai(mt_sts_cd);
                            lct_cd = (primary.lct_cd.StartsWith("002") ? "WIP" : "TIMS");
                            var process = "";
                            var poroduct = "";
                            var po = "";
                            var ketluan = "";
                            var time_mapping = "";
                            var cong_nhan = "";
                            var bobin = (primary.bb_no != null ? primary.bb_no : "");
                            if (primary.id_actual != 0 && primary.id_actual != null)
                            {
                                var ds = db.w_material_mapping.Where(x => x.mt_cd == mt_cd_bb).ToList().OrderByDescending(x => x.reg_dt).FirstOrDefault();
                                var actual = db.w_actual.Where(x => x.id_actual == primary.id_actual).SingleOrDefault();
                                var bobin_mapping = "";

                                if (ds != null)
                                {
                                    var mt = ds.mt_lot;
                                    var check_mt_mappng = db.w_material_info.Where(x => x.mt_cd == mt).SingleOrDefault();
                                    var actual2 = db.w_actual.Where(x => x.id_actual == check_mt_mappng.id_actual).SingleOrDefault();
                                    bobin_mapping = " Và mapping với container này :" + check_mt_mappng.bb_no;
                                    process = checkcondoan(actual2.name);
                                    cong_nhan = check_mt_mappng.staff_id;
                                    time_mapping = Convert.ToDateTime(ds.reg_dt.ToString()).ToString("dd-MM-yyyy HH:mm:ss");
                                    ketluan = "Đã Được Mapping ở công đoạn này: " + process + " Với Đồ Đựng Này : " + check_mt_mappng.bb_no;
                                }
                                else
                                {
                                    if (lct_cd.Equals("WIP"))
                                    {

                                        if (primary.input_dt.Length == 14)
                                        {
                                            time_mapping = primary.input_dt.Substring(0, 4) + "-" + primary.input_dt.Substring(4, 2) + "-" + primary.input_dt.Substring(6, 2) + " " + primary.input_dt.Substring(8, 2) + ":" + primary.input_dt.Substring(10, 2) + ":" + primary.input_dt.Substring(12, 2);
                                        }


                                        //time_mapping = Convert.ToDateTime(primary.input_dt.ToString()).ToString("dd-MM-yyyy HH:mm:ss");
                                    }
                                    if (primary.lct_cd.StartsWith("003") || primary.lct_cd.StartsWith("004"))
                                    {
                                        lct_cd = "FG";
                                    }
                                    process = checkcondoan(actual.name);
                                    ketluan = "Đang ở công đoạn này: " + process;
                                    cong_nhan = primary.staff_id;

                                }

                                po = actual.at_no;
                                var actual_primary = db.w_actual_primary.Where(x => x.at_no == po).SingleOrDefault();
                                poroduct = (actual_primary != null ? actual_primary.product : "");
                                //công đoạn 
                                if (primary.input_dt.Length == 14)
                                {
                                    time_mapping = primary.input_dt.Substring(0, 4) + "-" + primary.input_dt.Substring(4, 2) + "-" + primary.input_dt.Substring(6, 2) + " " + primary.input_dt.Substring(8, 2) + ":" + primary.input_dt.Substring(10, 2) + ":" + primary.input_dt.Substring(12, 2);
                                }

                            }
                            else
                            {
                                ketluan = "Vẫn chưa được mapping";
                            }
                            var data = new
                            {
                                SD = "",
                                bobin = bobin,
                                trangthai = trangthai,
                                lct_cd = lct_cd,
                                process = process,
                                product = poroduct,
                                po = po,
                                time_mapping = time_mapping,
                                sanluong = primary.gr_qty,
                                cong_nhan = cong_nhan,
                                ketluan = ketluan,
                            };
                            return Json(new { result = true, data, number = 4 }, JsonRequestBehavior.AllowGet);
                        }

                    }
                    else if (history_bb.Count > 1)
                    {
                        //nếu nhiều hơn 1 thì n đang đựng nhiều mã kiểm tra lại
                        var csv = string.Join(", ", history_bb.Select(x => x.mt_cd));
                        trangthai = "Đồ đựng này đựng nhiều mã hơn 1 :" + csv;
                        return Json(new { result = true, number = 5, trangthai }, JsonRequestBehavior.AllowGet);

                    }
                    else
                    {
                        //nếu không có thì kiểm tra xem lần cuối bb này đựng cái mã gì 
                        //kêu ngta kiểm tra mã cũ này xem tại sao bị xã 
                        //var checkr_his = db.history_container.Where(x => x.bb_no == bb_no).OrderByDescending(x => x.blhis).ToList();
                        trangthai = "Bobbin này đang rỗng";
                        return Json(new { result = true, number = 6, trangthai }, JsonRequestBehavior.AllowGet);

                    }
                }
                if (!String.IsNullOrEmpty(mt_cd))
                {
                    //ĐANG Ở ĐÂU
                    var primary = db.w_material_info.Where(x => x.mt_cd == mt_cd).SingleOrDefault();
                    //nếu trong bảng tạm không có kiểm tra bảng chính tồn tại không
                    var trangthai = "";
                    var lct_cd = "";
                    if (primary != null)
                    {
                        var mt_sts_cd = primary.mt_sts_cd;
                        trangthai = checktrangthai(mt_sts_cd);
                        lct_cd = (primary.lct_cd.StartsWith("002") ? "WIP" : "TIMS");
                        var process = "";
                        var poroduct = "";
                        var po = "";
                        var ketluan = "";
                        var time_mapping = "";
                        var cong_nhan = "";

                        var bobin = (primary.bb_no != null ? primary.bb_no : "");
                        if (primary.id_actual != 0 && primary.id_actual != null)
                        {
                            var ds = db.w_material_mapping.Where(x => x.mt_cd == mt_cd).ToList().OrderByDescending(x => x.reg_dt).FirstOrDefault();
                            var actual = db.w_actual.Where(x => x.id_actual == primary.id_actual).SingleOrDefault();
                            var bobin_mapping = "";

                            if (ds != null)
                            {
                                var mt = ds.mt_lot;
                                var check_mt_mappng = db.w_material_info.Where(x => x.mt_cd == mt).SingleOrDefault();
                                var actual2 = db.w_actual.Where(x => x.id_actual == check_mt_mappng.id_actual).SingleOrDefault();
                                bobin_mapping = " Và mapping với container này :" + check_mt_mappng.bb_no;
                                process = checkcondoan(actual2.name);
                                time_mapping = Convert.ToDateTime(ds.reg_dt.ToString()).ToString("dd-MM-yyyy HH:mm:ss");
                                cong_nhan = check_mt_mappng.staff_id;

                                ketluan = "Đã Được Mapping ở công đoạn này: " + process + " Với Đồ Đựng Này : " + check_mt_mappng.bb_no;
                            }
                            else
                            {
                                if (primary.lct_cd.StartsWith("003") || primary.lct_cd.StartsWith("004"))
                                {
                                    lct_cd = "FG";
                                }
                                process = checkcondoan(actual.name);
                                ketluan = "Đang ở công đoạn này: " + process;
                                cong_nhan = primary.staff_id;
                            }

                            po = actual.at_no;
                            var actual_primary = db.w_actual_primary.Where(x => x.at_no == po).SingleOrDefault();
                            poroduct = (actual_primary != null ? actual_primary.product : "");
                            //công đoạn 
                        }
                        else
                        {
                            ketluan = "Vẫn chưa được mapping";
                        }
                        var data = new
                        {
                            SD = "",
                            bobin = bobin,
                            trangthai = trangthai,
                            lct_cd = lct_cd,
                            process = process,
                            product = poroduct,
                            po = po,
                            cong_nhan = cong_nhan,
                            ketluan = ketluan,
                            sanluong = primary.gr_qty,
                            time_mapping = time_mapping,

                        };
                        return Json(new { result = true, data, number = 3 }, JsonRequestBehavior.AllowGet);
                    }
                }
                if (!String.IsNullOrEmpty(mt_no))
                {
                    var SD = "";
                    var trangthai = "";
                    var lct_cd = "Chưa Scan vào kHO WIP";
                    //kiểm tra mã này có trong bảng tạm không
                    var tam = db.w_material_info_tam.Where(x => x.mt_cd == mt_no).SingleOrDefault();
                    if (tam != null)
                    {
                        //nếu có kiểm tra xem nó đã có SD chưa 
                        SD = (tam.sd_no == null ? "" : tam.sd_no);
                        var ketluan = (SD == "" && tam.remark != "Không tìm thấy mã này" ? " Chưa scan mã này vào SD ở Picking Scan" : "Đã được đưa vào SD:" + SD + " Và chưa được Receiving Scan(WIP)");
                        lct_cd = (SD != "" ? "  WIP" : "");
                        //nếu chưa có sd kiểm tra xem n đã xóa lần nào chưa remark(Không tìm thấy mã này) 
                        trangthai = (tam.remark == "Không tìm thấy mã này" ? "Đã từng xóa mã này khỏi danh SD ở Picking Scan" : checktrangthai(tam.mt_sts_cd) + lct_cd);
                        var data = new
                        {
                            SD = SD,
                            trangthai = trangthai,
                            lct_cd = lct_cd,
                            process = "",
                            product = "",
                            po = "",
                            ketluan = "Sản lượng: " +tam.gr_qty,
                            sanluong = "",
                            time_mapping = "",
                        };
                        return Json(new { result = true, datamachine = data, number = 1 }, JsonRequestBehavior.AllowGet);
                    }
                    var primary = db.w_material_info.Where(x => x.mt_cd == mt_no).SingleOrDefault();
                    //nếu trong bảng tạm không có kiểm tra bảng chính tồn tại không
                    if (primary != null)
                    {
                        //nếu NVL xuất ra máy thì view trạng thái ở máy

                        var IsWmaterial = _IWIPService.CheckWMaterialInfo(mt_no);
                        if (!string.IsNullOrEmpty(IsWmaterial.ExportCode) && !string.IsNullOrEmpty(IsWmaterial.LoctionMachine))
                        {
                            trangthai = checktrangthai(IsWmaterial.mt_sts_cd);
                            var ds = db.w_material_mapping.Where(x => x.mt_cd == primary.mt_cd).OrderByDescending(x => x.reg_dt).FirstOrDefault();
                         var    time_mapping2 = "";
                            if (ds != null)
                            {
                                if (ds.mapping_dt != null && ds.mapping_dt.Length == 14 )
                                {
                                    var time_mapping1 = ds.mapping_dt.Substring(0, 4) + "-" + ds.mapping_dt.Substring(4, 2) + "-" + ds.mapping_dt.Substring(6, 2) + " " + ds.mapping_dt.Substring(8, 2) + ":" + ds.mapping_dt.Substring(10, 2)
                                        + ":" + ds.mapping_dt.Substring(12, 2);
                                    time_mapping2 = time_mapping1;
                                }
                                
                            }
                            //lấy ra cái máy mà liệu đó chạy
                            var machineCode = IsWmaterial.machine_id == null ? "" : IsWmaterial.machine_id;
                            var datamachine = new
                            {
                                SD = IsWmaterial.ExportCode,
                                trangthai = trangthai,
                                lct_cd = "Đang ở Máy",
                                process = "",
                                product = IsWmaterial.product == "" ? "" : IsWmaterial.product,
                                po = IsWmaterial.at_no == "" ? "" : IsWmaterial.at_no,
                                ketluan = "Đã được xuất ra máy " + machineCode,
                                sanluong = primary.gr_qty,
                                time_mapping = time_mapping2,
                            };
                            return Json(new { result = true, datamachine, number = 10 }, JsonRequestBehavior.AllowGet);
                        }


                        SD = primary.sd_no;
                        var mt_sts_cd = primary.mt_sts_cd;


                        trangthai = checktrangthai(mt_sts_cd);
                        var locationWip = primary.lct_cd;
                         var vitri = checkvitri(locationWip);
                        lct_cd = (primary.lct_cd.StartsWith("002") ? "WIP" : "TIMS");
                        var process = "";
                        var poroduct = "";
                        var po = "";
                        var ketluan = "";
                        var time_mapping = "";
                        if (primary.id_actual != 0 && primary.id_actual != null)
                        {
                            var actual = db.w_actual.Where(x => x.id_actual == primary.id_actual).SingleOrDefault();
                            po = actual.at_no;
                            var actual_primary = db.w_actual_primary.Where(x => x.at_no == po).SingleOrDefault();
                            poroduct = (actual_primary != null ? actual_primary.product : "");
                            //công đoạn 
                            process = checkcondoan(actual.name);
                            ketluan = "Đã Được Mapping ở công đoạn này: " + process;
                            var ds = db.w_material_mapping.Where(x => x.mt_cd == mt_no).ToList().OrderByDescending(x => x.reg_dt).FirstOrDefault();
                            if (ds != null)
                            {
                                time_mapping = Convert.ToDateTime(ds.reg_dt.ToString()).ToString("dd-MM-yyyy HH:mm:ss");
                            }
                           
                        }
                        else
                        {
                            ketluan = "Vẫn chưa được mapping";
                        }

                        var data = new
                        {
                            SD = SD,
                            trangthai = trangthai,
                            lct_cd = vitri,
                            process = process,
                            product = poroduct,
                            po = po,
                            vitri = vitri,
                            ketluan = ketluan,
                            sanluong = primary.gr_qty,
                            time_mapping = time_mapping,
                        };
                        return Json(new { result = true, datamachine = data, number = 2 }, JsonRequestBehavior.AllowGet);
                    }
                }
                return Json(new { result = false, message = "Mã này không tồn tại trong hệ thống." }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {

                return Json(new { result = false, message = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
            }
        }
        public string checktrangthai(string mt_sts_cd)
        {
            var check_trangthai = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == mt_sts_cd).ToList();
            var csv = string.Join(", ", check_trangthai.Select(x => x.dt_nm));

            return csv;
        }
        public string checkvitri(string lct_Cd)
        {
            var checkvitri = db.lct_info.Where(x => x.lct_cd == lct_Cd).ToList();
            var csv = string.Join(", ", checkvitri.Select(x => x.lct_nm));
            return csv;
        }
        public string checkcondoan(string name)
        {
            var check_trangthai = db.comm_dt.Where(x => x.mt_cd == "COM007" && x.dt_cd == name).ToList();
            var csv = string.Join(", ", check_trangthai.Select(x => x.dt_nm));

            return csv;
        }
        #endregion

        private void WriteHtmlTable<T>(IEnumerable<T> data, TextWriter output, String[] labelList)
        {
            //Writes markup characters and text to an ASP.NET server control output stream. This class provides formatting capabilities that ASP.NET server controls use when rendering markup to clients.
            using (StringWriter sw = new StringWriter())
            {
                using (HtmlTextWriter htw = new HtmlTextWriter(sw))
                {
                    //  Create a form to contain the List
                    Table table = new Table();
                    TableRow row = new TableRow();
                    PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(T));

                    // table header from IEnumerable
                    //foreach (PropertyDescriptor prop in props) {
                    //    TableHeaderCell hcell = new TableHeaderCell();
                    //    hcell.Text = prop.Name;
                    //    hcell.BackColor = System.Drawing.Color.Yellow;
                    //    row.Cells.Add(hcell);
                    //}

                    foreach (String label in labelList)
                    {
                        TableHeaderCell hcell = new TableHeaderCell();
                        hcell.Text = label;
                        //hcell.BackColor = System.Drawing.Color.Yellow;
                        hcell.Font.Bold = true;
                        row.Cells.Add(hcell);
                        row.BorderStyle = BorderStyle.Solid;
                    }
                    table.Rows.Add(row);

                    //  add each of the data item to the table
                    foreach (T item in data)
                    {
                        row = new TableRow();
                        foreach (PropertyDescriptor prop in props)
                        {
                            TableCell cell = new TableCell();
                            cell.Text = prop.Converter.ConvertToString(prop.GetValue(item));
                            //cell.BorderStyle = BorderStyle.Solid;
                            row.Cells.Add(cell);
                            row.BorderStyle = BorderStyle.Solid;
                        }
                        table.Rows.Add(row);
                    }

                    //  render the table into the htmlwriter
                    table.RenderControl(htw);

                    //  render the htmlwriter into the response
                    output.Write(sw.ToString());
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        public JsonResult ConvertDataTableToJson(DataTable data)
        {
            return Json(GetTableRows(data), JsonRequestBehavior.AllowGet);
        }

        public JsonResult SearchAndPaging(string countSql, string viewSql, int pageIndex, int pageSize)
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
                    try
                    {
                        //data.Load(reader);
                        DataTable DTSchema = reader.GetSchemaTable();
                        //DataTable DT = new DataTable();
                        if (DTSchema != null)
                            if (DTSchema.Rows.Count > 0)
                                for (int i = 0; i < DTSchema.Rows.Count; i++)
                                {
                                    //Create new column for each row in schema table
                                    //Set properties that are causing errors and add it to our datatable
                                    //Rows in schema table are filled with information of columns in our actual table
                                    DataColumn Col = new DataColumn(DTSchema.Rows[i]["ColumnName"].ToString(), (Type)DTSchema.Rows[i]["DataType"]);
                                    Col.AllowDBNull = true;
                                    Col.Unique = false;
                                    Col.AutoIncrement = false;
                                    data.Columns.Add(Col);
                                }
                        while (reader.Read())
                        {
                            //Read data and fill it to our datatable
                            DataRow Row = data.NewRow();
                            for (int i = 0; i < data.Columns.Count; i++)
                            {
                                Row[i] = reader[i];
                            }
                            data.Rows.Add(Row);
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
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

        private void WriteMoldHtmlTable<T>(IEnumerable<T> data, TextWriter output)
        {
            //Writes markup characters and text to an ASP.NET server control output stream. This class provides formatting capabilities that ASP.NET server controls use when rendering markup to clients.
            using (StringWriter sw = new StringWriter())
            {
                using (HtmlTextWriter htw = new HtmlTextWriter(sw))
                {
                    //  Create a form to contain the List
                    Table table = new Table();

                    TableRow row = new TableRow();

                    PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(T));

                    foreach (PropertyDescriptor prop in props)
                    {
                        TableHeaderCell hcell = new TableHeaderCell();
                        hcell.Text = prop.Name;
                        hcell.BackColor = System.Drawing.Color.Yellow;
                        row.Cells.Add(hcell);
                    }

                    table.Rows.Add(row);

                    //  add each of the data item to the table
                    foreach (T item in data)
                    {
                        row = new TableRow();
                        foreach (PropertyDescriptor prop in props)
                        {
                            TableCell cell = new TableCell();
                            cell.Text = prop.Converter.ConvertToString(prop.GetValue(item));
                            cell.BorderStyle = BorderStyle.Solid;
                            row.Cells.Add(cell);
                        }
                        table.Rows.Add(row);
                    }

                    //  render the table into the htmlwriter
                    table.RenderControl(htw);

                    //  render the htmlwriter into the response
                    output.Write(sw.ToString());
                }
            }
        }

        #region Insert Staff By Upload Excel File

        [HttpPost]
        public JsonResult InsertStaffByExcelFile(List<StaffModel> modelList)
        {
            if (modelList.Count == 0)
            {
                return Json(new { flag = false, message = "Không thể lấy dữ liệu từ tệp excel Hoặc tệp excel tải lên không có dữ liệu" }, JsonRequestBehavior.AllowGet);
            }
            HashSet<string> hashSet = new HashSet<string>();
            bool isDuplicated = false;
            string duplicatedUserid = "";
            foreach (var item in modelList)
            {
                if (!hashSet.Add(item.Id))
                {
                    isDuplicated = true;
                    duplicatedUserid = item.Id;
                    break;
                }
            }
            if (isDuplicated)
            {
                return Json(new { duplicatedUserid, flag = false, message = $"Tệp Excel trùng ID: {duplicatedUserid}" }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                var listPositionCode = new InitMethods().GetCommon("COM018");
                var listDepartmentCode = new InitMethods().GetDepartment();
                StringBuilder userIdListTemp = new StringBuilder();
                foreach (var item in modelList)
                {
                    //get position code for each staff
                    foreach (var comm in listPositionCode)
                    {
                        if (item.PositionName.Trim() == comm.Name.Trim())
                        {
                            item.PositionCode = comm.Code;
                            break;
                        }
                    }

                    //get department code for each staff
                    foreach (var dept in listDepartmentCode)
                    {
                        if (item.DepartmentName.Trim() == dept.Name.Trim())
                        {
                            item.DepartmentCode = dept.Code;
                            break;
                        }
                    }

                    if (item.Gender == "Nữ")
                    {
                        item.Gender = "W";
                    }
                    else { item.Gender = "M"; }

                    userIdListTemp.Append($"'{item.Id}',");
                }
                int index = userIdListTemp.ToString().LastIndexOf(",");
                string userIdList = userIdListTemp.ToString().Substring(0, index);
                StringBuilder deleteSql = new StringBuilder($"DELETE FROM mb_info WHERE userid in ({userIdList});");
                db.Database.ExecuteSqlCommand(deleteSql.ToString());

                // NEED 30s TO INSERT 1000 ROWS
                //foreach (var item in modelList)
                //{
                //    mb_info record = new mb_info();
                //    record.userid = item.Id;
                //    record.uname = item.Name;
                //    record.gender = item.Gender;
                //    record.lct_cd = item.LocationCode;
                //    record.barcode = item.BarCode;
                //    record.position_cd = item.PositionCode;
                //    record.birth_dt = item.BirthDate;
                //    record.depart_cd = item.DepartmentCode;
                //    record.join_dt = item.JoinDate;
                //    record.del_yn = "N";
                //    record.reg_id = Session["userid"] == null ? "" : Session["userid"].ToString();
                //    record.reg_dt = DateTime.Now;
                //    record.chg_id = Session["userid"] == null ? "" : Session["userid"].ToString();
                //    record.chg_dt = DateTime.Now;
                //    //db.mb_info.Add(record);
                //}
                //db.SaveChanges();

                // END

                //// NEED 1s TO INSERT 1000 ROWS
                var userInsert = Session["userid"] == null ? "" : Session["userid"].ToString();
                string dateInsert = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                StringBuilder insertSql = new StringBuilder();
                insertSql.Append($" insert into mb_info(userid, uname, gender, lct_cd, barcode, position_cd, birth_dt, depart_cd, join_dt, reg_id, reg_dt, chg_id, chg_dt) values ");
                foreach (var item in modelList)
                {
                    insertSql.Append($" ('{item.Id}', '{item.Name}', '{item.Gender}', '{item.LocationCode}', '{item.BarCode}', '{item.PositionCode}', '{item.BirthDate}', '{item.DepartmentCode}', '{item.JoinDate}', '{userInsert}', '{dateInsert}', '{userInsert}', '{dateInsert}'), ");
                }

                int removeLastComma = insertSql.ToString().LastIndexOf(",");
                string executeInsertSql = insertSql.ToString().Substring(0, removeLastComma);

                db.Database.ExecuteSqlCommand(executeInsertSql);
                //// END

                StringBuilder selectInsertSql = new StringBuilder($"SELECT a.userid, a.uname, a.position_cd, a.reg_id, a.reg_dt, a.chg_id, a.chg_dt FROM mb_info a WHERE a.userid in ({userIdList})");
                return new InitMethods().JsonResultAndMessageFromQuery(selectInsertSql, "Create new staffs successfully.");
            }
            catch (Exception)
            {
                return Json(new { flag = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetAllDepartments()
        {
            return Json(new InitMethods().GetDepartment(), JsonRequestBehavior.AllowGet);
        }

        #endregion Insert Staff By Upload Excel File


        #region RescoureMgt
        public ActionResult ResourceMgt()
        {
            return SetLanguage("");
        }
          public ActionResult PartialView_ResourceMgt()
        {

            var month = (Request["month"] != null ? Request["month"] : "0");
            var productCode = (Request["productCode"] != null ? Request["productCode"] : "");
            var po_no = (Request["po_no"] != null ? Request["po_no"] : "");
            var machineCode = (Request["machineCode"] != "" ? Request["machineCode"] : "SX3-RO-10K-04");
           
            var time_now = DateTime.Now.AddMonths(int.Parse(month));

            var startOfMonth = new DateTime(time_now.Year, time_now.Month, 1);
            var first = startOfMonth.AddMonths(1);
            var last = first.AddDays(-1);

            var date_start = startOfMonth.ToString("yyyy-MM-dd");
            var date_end = last.ToString("yyyy-MM-dd");

            var result = new ProductActivition();
            var varname_all = " SELECT n.id_actual, n.mc_no, DATE_FORMAT(n.reg_dt, '%Y-%m-%d') AS date,m.at_no	,m.product, ";
            varname_all = varname_all + "  DATE_FORMAT(n.start_dt,'%Y-%m-%d %H:%i:%s') AS start_dt,  ";
            varname_all = varname_all + "  DATE_FORMAT(n.end_dt,'%Y-%m-%d %H:%i:%s') AS end_dt ";

            varname_all = varname_all + "  FROM d_pro_unit_mc AS n ";
            varname_all = varname_all + "  JOIN w_actual AS m ON n.id_actual = m.id_actual ";
            varname_all = varname_all + " WHERE (DATE_FORMAT(n.reg_dt, '%Y-%m-%d') >=  '"+ date_start + "'";
            varname_all = varname_all + " AND DATE_FORMAT(n.reg_dt, '%Y-%m-%d') <=  '"+ date_end + "')";
            varname_all = varname_all + " AND ('" + machineCode + "'='' OR  n.mc_no like '%" + machineCode + "%' )  ";
            varname_all = varname_all + " AND ('" + po_no + "'='' OR  m.at_no like '%" + po_no + "%' )  ";
            varname_all = varname_all + " AND ('" + productCode + "'='' OR  m.product like '%" + productCode + "%' )  ";

          
            result.ProductActivitionFaile = db.Database.SqlQuery<modelTableMAchine>(varname_all).ToList();


            var monthReport =  DateTime.Now.ToString("yyyy-MM-dd").Substring(0, 7) ;
            ViewBag.start = date_start;
            ViewBag.end = date_end;
            ViewBag.MonthReport = monthReport;

            return PartialView(result);

        }
        public ActionResult PartialView_dialog_Viewdetail(string at_no, string mc_no)
        {
            ViewBag.at_no = at_no;
            ViewBag.mc_no = mc_no;

            string sqlquery = @"CALL tableMachineDatetime(@1,@2);";
            var result = db.Database.SqlQuery<TableMachineDatetime>(sqlquery,
                new MySqlParameter("@1", at_no),
                new MySqlParameter("@2", mc_no)
                );



            return PartialView(result);
        }
        public ActionResult GetDataChart(string at_no, string mc_no)
        {
           

            string sqlquery = @"CALL tableMachineDatetime(@1,@2);";
            var result = db.Database.SqlQuery<TableMachineDatetime>(sqlquery,
                new MySqlParameter("@1", at_no),
                new MySqlParameter("@2", mc_no)
                ).ToList();



            return Json(new { result = true, data = result },JsonRequestBehavior.AllowGet);
        }

        #endregion
    }

    public class TrayBoxModel
    {
        public string RowNum { get; set; }
        public string bb_no { get; set; }
        public string bno { get; set; }
        public string mc_type { get; set; }
        public string mt_cd { get; set; }
        public string bb_nm { get; set; }
        public string purpose { get; set; }
        public string barcode { get; set; }
        public string re_mark { get; set; }
    }

    public class ModelInsertMaterialTemp
    {
        public string Type { get; set; }
        public string Barcode { get; set; }
        public string MT_No { get; set; }
        public string Name { get; set; }
        public string sp_cd { get; set; }
        public string bundle_qty { get; set; }
        public string bundle_unit { get; set; }
        public string Width { get; set; }
        public string Length { get; set; }
    }

    public class ModelInsertModelExcel
    {
        public string code { get; set; }
        public string name { get; set; }
    }

    public class ModelInsertProductExcel
    {
        public string style_no { get; set; }
        public string style_nm { get; set; }
        public string md_cd { get; set; }
        public string prj_nm { get; set; }
        public string pack_amt { get; set; }
        public string stamp_code { get; set; }
        public string expiry { get; set; }
    }

    public class ModelInsertMachineExcel
    {
        public string mc_type { get; set; }
        public string mc_no { get; set; }
        public string mc_nm { get; set; }
        public string purpose { get; set; }
    }

    public class ModelReturnProductExcel
    {
        public string sid { get; set; }
        public string style_no { get; set; }
        public string style_nm { get; set; }
        public string md_cd { get; set; }
        public string prj_nm { get; set; }
        public string pack_amt { get; set; }

        public string reg_id { get; set; }
        public string reg_dt { get; set; }
        public string chg_id { get; set; }
        public string chg_dt { get; set; }
        public string STATUS { get; set; }
        public string stamp_code { get; set; }
        public string expiry { get; set; }
        public string expiry_month { get; set; }
    }

    public class ModelReturnMachineExcel
    {
        public string mno { get; set; }
        public string mc_type { get; set; }
        public string mc_no { get; set; }
        public string mc_nm { get; set; }
        public string purpose { get; set; }
        public string re_mark { get; set; }
        public string reg_id { get; set; }
        public string chg_id { get; set; }
        public string reg_dt { get; set; }

        public string chg_dt { get; set; }
        public string STATUS { get; set; }
    }

    public class ModelReturnModelExcel
    {
        public string mdid { get; set; }
        public string md_cd { get; set; }
        public string md_nm { get; set; }
        public string use_yn { get; set; }
        public string reg_id { get; set; }
        public string reg_dt { get; set; }

        public string chg_id { get; set; }
        public string chg_dt { get; set; }
        public string STATUS { get; set; }
    }

    public class ModelReturnTrayBox
    {
        public string bb_no { get; set; }
        public string bno { get; set; }
        public string mc_type { get; set; }
        public string mt_cd { get; set; }
        public string bb_nm { get; set; }
        public string purpose { get; set; }

        public string barcode { get; set; }
        public string re_mark { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Mvc_VD.Models;
using System.Text;
using System.Globalization;
using System.Collections;
using Mvc_VD.Models.Language;

namespace Mvc_VD.Controllers
{
    public class StandardMgtWHController : Controller
    {
        private Entities db = new Entities();


        #region WMS COMMON
        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        public ActionResult WarehouseCommon()
        {
            HttpCookie cookie = HttpContext.Request.Cookies["language"];
            if (cookie != null)
            {
                ViewBag.language = cookie.Value;
            }
            return View(db.comm_mt.ToList());

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
        public ActionResult getWHSCommon(comm_mt comm_mt)
        {
            try
            {
                var vaule = db.comm_mt.Where(item => item.div_cd == "WHS" && item.mt_cd.StartsWith("WHS")).ToList();
                var data = new { rows = vaule };
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public string automtcd(int id)
        {
            if (id.ToString().Length < 2)
            {
                return string.Format("00{0}", id);
            }

            if ((id.ToString().Length < 3) || (id.ToString().Length == 2))
            {
                return string.Format("0{0}", id);
            }

            if ((id.ToString().Length < 4) || (id.ToString().Length == 3))
            {
                return string.Format("{0}", id);
            }
            return string.Empty;
        }

        public ActionResult insertWHSCommon(comm_mt comm_mt, string mt_cd, string mt_nm, string mt_exp, string use_yn)
        {
            try
            {

                int countlist = db.comm_mt.Where(item => item.div_cd == "WHS").ToList().Count();
                var mt_cd_tmp = string.Empty;
                var submtcdconvert = 0;
                var listlast = db.comm_mt.Where(item => item.div_cd == "WHS").OrderBy(item => item.mt_cd).ToList().LastOrDefault();
                if (countlist == 0)
                {
                    mt_cd = "WHS001";
                }
                else
                {
                    var bien = listlast.mt_cd;
                    var submtcd = bien.Substring(bien.Length - 3, 3);
                    int.TryParse(submtcd, out submtcdconvert);

                    mt_cd_tmp = "WHS" + string.Format("{0}{1}", mt_cd_tmp, automtcd((submtcdconvert + 1)));
                    mt_cd = mt_cd_tmp;
                }

                DateTime reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                String dateString = reg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");

                DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");

                comm_mt.mt_cd = mt_cd;
                comm_mt.div_cd = "WHS";
                comm_mt.mt_nm = mt_nm;
                comm_mt.mt_exp = mt_exp;
                comm_mt.use_yn = use_yn;
                comm_mt.reg_dt = reg_dt;
                comm_mt.chg_dt = chg_dt;
                if (ModelState.IsValid)
                {

                    db.comm_mt.Add(comm_mt);
                    db.SaveChanges();
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
        public ActionResult updatetWHSCommon(int mt_id, string mt_cd, string mt_nm, string mt_exp, string use_yn)
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
        public JsonResult deleteWHSCommon(int mt_id)
        {



            int count = db.comm_mt.Where(x => x.mt_id == mt_id).ToList().Count();
            var result = new { result = true };
            if (count > 0)
            {
                comm_mt wr = db.comm_mt.Find(mt_id);
                db.comm_mt.Remove(wr);
                db.SaveChanges();

                var vaule2 = db.comm_dt.Where(y => y.mt_cd == wr.mt_cd).ToList();
                foreach (var item in vaule2)
                {
                    comm_dt wr2 = db.comm_dt.Find(item.cdid);
                    db.comm_dt.Remove(wr2);
                    db.SaveChanges();
                }
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            result = new { result = false };
            return Json(result, JsonRequestBehavior.AllowGet);

            //if (count > 0)
            //{
            //    s_order_ship_master sm = db.s_order_ship_master.Find(id);
            //    db.s_order_ship_master.Remove(sm);
            //    db.SaveChanges();

            //    var vaule4 = db.s_order_ship_info.Where(item => item.sm_no == sm.sm_no).ToList();
            //    foreach (var item in vaule4)
            //    {
            //        s_order_ship_info wr = db.s_order_ship_info.Find(item.sno);
            //        db.s_order_ship_info.Remove(wr);
            //        db.SaveChanges();
            //    }


            //    return Json(result, JsonRequestBehavior.AllowGet);
            //}

        }


        public JsonResult searchWHSCommon()
        {
            string mt_cd = Request.QueryString["mt_cd"];
            string mt_nm = Request.QueryString["mt_nm"];
            string mt_exp = Request.QueryString["mt_exp"];
            var sql = new StringBuilder();
            sql.Append(" SELECT mt.* ")
                .Append("FROM  comm_mt as mt ")
                 .Append("WHERE ('" + mt_cd + "'='' OR  mt.mt_cd like '" + mt_cd + "' )")
                 .Append("AND ('" + mt_nm + "'='' OR  mt.mt_nm like '%" + mt_nm + "%' )")
                 .Append("AND ('" + mt_exp + "'='' OR   mt.mt_exp like '%" + mt_exp + "%' )")
                 .Append("AND mt.div_cd='WHS' ");

            var data = db.comm_mt.SqlQuery(sql.ToString()).ToList<comm_mt>();


            return Json(data, JsonRequestBehavior.AllowGet);
        }


        public ActionResult getWHSCommonDtData(comm_dt comm_dt, comm_mt comm_mt, string mt_cd)
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


        public ActionResult insertWHSCommonDt(comm_dt comm_dt, string dt_cd, string dt_nm, string dt_exp, string use_yn, string mt_cd, int dt_order)
        {
            try
            {
                int count = db.comm_dt.Where(item => item.dt_cd == dt_cd && item.mt_cd == mt_cd).ToList().Count();

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

        public ActionResult updateWHSCommonDt(int cdid, string dt_nm, string dt_exp, string use_yn, int dt_order)
        {
            try
            {
                comm_dt comm_dt = db.comm_dt.Find(cdid);

                DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");

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
            //        mt_cd: $('#m_mt_cd').val(),
            //cdid: $('#dm_cdid').val(),

        [HttpPost]
        public JsonResult deleteWHSCommonDt(string mt_cd, int cdid)
        {

            var countWHSCommonDt = db.comm_dt.Count(x => x.mt_cd == mt_cd && x.cdid == cdid);
            if (countWHSCommonDt > 0)
            {
                var countWHSCommonDtData = db.comm_dt.FirstOrDefault(x => x.mt_cd == mt_cd && x.cdid == cdid);
                db.comm_dt.Remove(countWHSCommonDtData);
                db.SaveChanges();
            }
            var result = new { result = countWHSCommonDt };
            return Json(result, JsonRequestBehavior.AllowGet);


        }


        #endregion


        #region Warehouse Location(M)
        public ActionResult PrintWahouse(string id)
        {
            ViewData["Message"] = id;
            return View();
        }
        public ActionResult QRbarcodewwh(string id)
        {
            if (id != "")
            {
                var sql = new StringBuilder();
                sql.Append(" SELECT * ")
                    .Append("FROM  lct_info as a ")
                     .Append("where a.lctno in (" + id + ")");
                var data = db.lct_info.SqlQuery(sql.ToString()).ToList<lct_info>();
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            return View();

        }
        public ActionResult WarehouseLocation()
        {
            return View();
        }
        // location                
        public ActionResult locMgt()
        {
            return View();
        }
        public ActionResult GetWHlocMgt()
        {
            var vaule = db.lct_info.Where(item => item.index_cd.StartsWith("W")).OrderBy
              (item => item.lct_cd).ToList();
            return Json(new { rows = vaule }, JsonRequestBehavior.AllowGet);

        }
        public ActionResult insertlocMgt(lct_info WHL, string lct_nm, string re_mark, int lctno, string use_yn, string real_use_yn, string mv_yn, string nt_yn)
        {
            WHL.mv_yn = mv_yn;
            WHL.nt_yn = nt_yn;
            WHL.use_yn = use_yn;
            WHL.real_use_yn = real_use_yn;
            WHL.sf_yn = WHL.sf_yn;
            if (WHL.nt_yn == null)
            {
                WHL.nt_yn = "N";
            }
            if (WHL.sf_yn == null)
            {
                WHL.sf_yn = "N";
            }
            if (WHL.mv_yn == null)
            {
                WHL.mv_yn = "N";
            }

            string[] listtring = { "0", "A", "B", "C", "D", "E", "F", "G", "H","I","J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
            WHL.lct_nm = lct_nm;
            WHL.order_no = 1;
            WHL.re_mark = re_mark;
            string root_yn = Request.QueryString["c_root_yn"];
            var list = db.lct_info.Where(item => item.index_cd.StartsWith("W")).
                  OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();
            var upLevel = db.lct_info.FirstOrDefault(item => item.lctno == lctno);
            if (list.Count == 0)
            {
                WHL.index_cd = "W01";
                WHL.lct_cd = "001W01000000000000";
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
                    menuCd = string.Format("{0}{1}", menuCd, CreateId((subMenuIdConvert + 1)));


                    WHL.index_cd = "W" + menuCd;
                    WHL.lct_cd = "001" + WHL.index_cd + "000000000000";
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
                            Whcd = string.Format("{0}{1}000000", Whcd, "0" + CreateId((subMenuIdConvert + 1)));
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
                            Whcd = string.Format("{0}{1}", Whcd, "0" + CreateId((subMenuIdConvert + 1)));
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
                            Whcd = string.Format("{0}{1}", Whcd, "W" + CreateId((subMenuIdConvert + 1)) + "000000000000");

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
            WHL.reg_id = Session["userid"].ToString();
            WHL.chg_id = Session["userid"].ToString();

            if (WHL.lct_cd.Contains("001W0100A"))
            {
                WHL.real_use_yn = "N";

            }
            if (ModelState.IsValid)
            {
                db.Entry(WHL).State = EntityState.Added;
                db.SaveChanges();
                return Json(WHL, JsonRequestBehavior.AllowGet); ;
            }

            return View(WHL);
        }
        public ActionResult updatewhlocMgt(string mv_yn, string lct_nm, string lct_cd, int lctno, string sf_yn, string nt_yn ,string use_yn, string real_use_yn, string re_mark)
        {
            var count_b = db.lct_info.Count(x => x.lctno == lctno);
            if (count_b != 0)
            {
                if (sf_yn == null)
                {
                    sf_yn = "N";
                }
                if (mv_yn == null)
                {
                    mv_yn = "N";
                }
                if (nt_yn == null)
                {
                    nt_yn = "N";
                }
                lct_info b = db.lct_info.Find(lctno);
                b.lct_nm = lct_nm;
                b.use_yn = use_yn;
                b.real_use_yn = real_use_yn;
                b.mn_full = b.lct_nm;
                b.re_mark = re_mark;
                b.sf_yn = sf_yn;
                b.nt_yn = nt_yn;
                b.mv_yn = mv_yn;
                b.chg_id = Session["userid"].ToString();
                b.chg_id = Session["userid"].ToString();
                DateTime chg_dt = DateTime.Now;
                String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                b.chg_dt = chg_dt;
                db.SaveChanges();
                return Json(new { result = count_b, lct_cd = b.lct_cd }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { result = count_b, lct_cd = db.lct_info.FirstOrDefault(x => x.lctno == lctno).lct_cd }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult deleteWHlocMgt(int lctno)
        {
            lct_info lct_info = db.lct_info.Find(lctno);
            if (lct_info != null)
            {
                db.lct_info.Remove(lct_info);
                db.SaveChanges();
                var result = new { result = 1 };
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var result = new { result = 0 };
                return Json(result, JsonRequestBehavior.AllowGet);
            }


        }

        public JsonResult SearchWhLocation()
        {

            // Get Data from ajax function
            var W = Request["whouse"];
            var A = Request["aisle"];
            var B = Request["bay"];

            var sql = new StringBuilder();
            sql.Append(" SELECT * ")
            .Append("FROM  lct_info as a ")
            .Append("Where a.index_cd like 'W%' and ('" + A + "'='' OR  (SUBSTRING(a.lct_cd,9,1)) like '%" + A + "%' )")
            .Append("AND ('" + B + "'='' OR  (SUBSTRING(a.lct_cd,11, 2)) like '%" + B + "%' )")
            .Append("AND ('" + W + "'='' OR  a.index_cd like '%" + W + "%')");

            var data = db.lct_info.SqlQuery(sql.ToString()).ToList<lct_info>();
            return Json(data, JsonRequestBehavior.AllowGet); ;
        }
        public ActionResult Warehouse(lct_info lct_info)
        {
               var sql = new StringBuilder();
               sql.Append(" SELECT * ")
               .Append("FROM  lct_info as a ")
               .Append("Where a.lct_cd like '001%' GROUP  by a.index_cd");
            var data = db.lct_info.SqlQuery(sql.ToString()).ToList<lct_info>();
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Aisle(lct_info lct_info)
        {
            var lists = db.lct_info.Where(item => item.index_cd.StartsWith("W")).
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

        public ActionResult Bay(lct_info lct_info)
        {
            var lists = db.lct_info.Where(item => item.index_cd.StartsWith("W")).
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
        //// GET: /system/
        private string CreateId(int id)
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
        #endregion




  


    }
}

using System;
using System.Collections.Generic;
using System.Collections;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using System.Text;
using Mvc_VD.Models;
using System.Globalization;
using System.Web;
using Mvc_VD.Models.Language;

namespace Mvc_VD.Controllers
{
    public class StandardMgtWOController : BaseController
    {
        private Entities db = new Entities();

 

     

   


        #region WorkPolicyInfo
        public ActionResult WorkPolicyInfo()
        {
            return SetLanguage("");
           
        }

        public ActionResult getWorkPolicyInfo(w_policy_mt w_policy_mt)
        {
            var data = db.w_policy_mt.ToList();
            var wpmt = new { rows = data };
            return Json(wpmt, JsonRequestBehavior.AllowGet);

        }
        public string autoPono(int id)
        {
            if (id.ToString().Length < 2)
            {
                return string.Format("0000{0}", id);
            }

            if ((id.ToString().Length < 3) || (id.ToString().Length == 2))
            {
                return string.Format("000{0}", id);
            }

            if ((id.ToString().Length < 4) || (id.ToString().Length == 3))
            {
                return string.Format("00{0}", id);
            }

            if ((id.ToString().Length < 5) || (id.ToString().Length == 4))
            {
                return string.Format("0{0}", id);
            }
            if ((id.ToString().Length < 6) || (id.ToString().Length == 5))
            {
                return string.Format("{0}", id);
            }
            return string.Empty;
        }

        public ActionResult insertWorkPolicyInfo(w_policy_mt wpmt, string work_start, string work_end, string work_hour,
            string policy_name, string use_yn, string re_mark, string lunch_start, string lunch_end, string dinner_start, string dinner_end)
        {

            int countlist = db.w_policy_mt.ToList().Count();
            var code_tmp = string.Empty;
            //string code;
            var subcodeconvert = 0;



            if (countlist == 0)
            {
                code_tmp = "P00001";
            }
            else
            {
                var listlast = db.w_policy_mt.OrderBy(item => item.policy_code).ToList().LastOrDefault().policy_code;
                var subbbno = listlast.Substring(listlast.Length - 5, 5);
                int.TryParse(subbbno, out subcodeconvert);


                code_tmp = "P" + string.Format("{0}{1}", code_tmp, autoPono((subcodeconvert + 1)));

            }


            wpmt.policy_code = code_tmp;
            wpmt.policy_name = policy_name;
            wpmt.work_starttime = work_start;
            wpmt.work_endtime = work_end;

            wpmt.lunch_start_time = lunch_start;
            wpmt.lunch_end_time = lunch_end;
            wpmt.dinner_start_time = dinner_start;
            wpmt.dinner_end_time = dinner_end;




            wpmt.work_hour = Convert.ToDecimal(work_hour);
            wpmt.use_yn = use_yn;
            wpmt.re_mark = re_mark;
            DateTime reg_dt = DateTime.Now;
            DateTime chg_dt = DateTime.Now;

            wpmt.reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
            wpmt.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
            wpmt.reg_dt = reg_dt;
            wpmt.chg_dt = chg_dt;
            wpmt.policy_start_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
            wpmt.policy_end_dt = "99991231235959";
            wpmt.last_yn = "Y";
            if (ModelState.IsValid)
            {
                db.w_policy_mt.Add(wpmt);
                db.SaveChanges();
                var result = new { result = true };
                return Json(wpmt, JsonRequestBehavior.AllowGet);
            }
            return View(wpmt);
        }



        public JsonResult deletetWorkPolicyInfo(int id)
        {


            //s_order_info s_order_info = db.s_order_info.Find(id);


            int count = db.w_policy_mt.Where(x => x.wid == id).ToList().Count();


            var result = new { result = true };
            if (count > 0)
            {
                w_policy_mt wpmt = db.w_policy_mt.Find(id);
                db.w_policy_mt.Remove(wpmt);

                db.SaveChanges();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            result = new { result = false };
            return Json(result, JsonRequestBehavior.AllowGet);




        }
        public ActionResult updateWorkPolicyInfo(int wid, string work_start, string work_end, string work_hour,
            string policy_name, string use_yn, string re_mark, string lunch_start, string lunch_end, string dinner_start, string dinner_end)
        {
            w_policy_mt wpmt = db.w_policy_mt.Find(wid);
            wpmt.policy_name = policy_name;
            wpmt.work_starttime = work_start;
            wpmt.work_endtime = work_end;

            wpmt.lunch_start_time = lunch_start;
            wpmt.lunch_end_time = lunch_end;
            wpmt.dinner_start_time = dinner_start;
            wpmt.dinner_end_time = dinner_end;

            wpmt.work_hour = Convert.ToDecimal(work_hour);
            DateTime chg_dt = DateTime.Now;
            //wpmt.chg_dt = chg_dt;
            wpmt.policy_end_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
            wpmt.last_yn = "N";
            wpmt.use_yn = use_yn;
            wpmt.re_mark = re_mark;
            wpmt.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
            if (ModelState.IsValid)
            {
                db.Entry(wpmt).State = EntityState.Modified;
                db.SaveChanges();
                var result = new { result = true };
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            return View(wpmt);
        }
        #endregion


        #region NoticeManagement
        public ActionResult NoticeManagement()
        {
            return View();
        }

        public ActionResult getnoticeMgt(m_board m_board)
        {
            var data = db.m_board.ToList();
            return Json(new { rows = data }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult NoticeCreate()
        {
            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult insertnotice(m_board m_board)
        {
            if (ModelState.IsValid)
            {
                m_board.chg_dt = DateTime.Now;
                m_board.reg_dt = DateTime.Now;
                m_board.reg_id = Session["userid"].ToString();
                m_board.chg_id = Session["userid"].ToString();
                m_board.div_cd = "A";
                db.m_board.Add(m_board);
                db.SaveChanges();
                return RedirectToAction("NoticeManagement");
            }

            return View(m_board);
        }
        public ActionResult NoticeView(int mno)
        {
            m_board m_board = db.m_board.Find(mno);
            if (m_board == null)
            {
                return HttpNotFound();
            }
            return View(m_board);
        }


        public ActionResult NoticeModify(int mno)
        {
            m_board m_board = db.m_board.Find(mno);
            if (m_board == null)
            {
                return HttpNotFound();
            }
            return View(m_board);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult updateNotice(int mno, string content, string title)
        {
            m_board m_board = db.m_board.Find(mno);
            m_board.title = title;
            m_board.content = content;
            //DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
            //String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
            m_board.chg_id = Session["userid"].ToString();
            //m_board.chg_dt = chg_dt;
            m_board.div_cd = "A";
            if (ModelState.IsValid)
            {
                db.Entry(m_board).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("NoticeManagement");
            }
            return View(m_board);
        }
        #endregion

        #region WOCommon(M)
        public ActionResult WOCommon()
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
        public ActionResult getWHSCommon()
        {
            var vaule = db.comm_mt.Where(item => item.div_cd == "MMS" && item.mt_cd.StartsWith("MMS")).ToList();
            var data = new { rows = vaule };
            return Json(data, JsonRequestBehavior.AllowGet);

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

                int countlist = db.comm_mt.Where(item => item.div_cd == "MMS").ToList().Count();
                var mt_cd_tmp = string.Empty;
                var submtcdconvert = 0;
                var listlast = db.comm_mt.Where(item => item.div_cd == "MMS").OrderBy(item => item.mt_cd).ToList().LastOrDefault();
                if (countlist == 0)
                {
                    mt_cd = "MMS001";
                }
                else
                {
                    var bien = listlast.mt_cd;
                    var submtcd = bien.Substring(bien.Length - 3, 3);
                    int.TryParse(submtcd, out submtcdconvert);

                    mt_cd_tmp = "MMS" + string.Format("{0}{1}", mt_cd_tmp, automtcd((submtcdconvert + 1)));
                    mt_cd = mt_cd_tmp;
                }

                DateTime reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                String dateString = reg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");

                DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");

                comm_mt.mt_cd = mt_cd;
                comm_mt.div_cd = "MMS";
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
                    return Json(comm_mt, JsonRequestBehavior.AllowGet);
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
                    return Json(comm_mt, JsonRequestBehavior.AllowGet);
                }
                return View(comm_mt);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        //public JsonResult deleteWHSCommon(int mt_id)
        //{
        //    try
        //    {
        //        int count = db.comm_mt.Where(x => x.mt_id == mt_id).ToList().Count();

        //        var vaule = db.comm_mt.Where(x => x.mt_id == mt_id).ToList().FirstOrDefault();

        //        int count2 = db.comm_dt.Where(x => x.mt_cd == vaule.mt_cd).ToList().Count();

        //        var vaule2 = db.comm_dt.Where(x => x.mt_cd == vaule.mt_cd).ToList();

        //        var result = new { result = true };
        //        if (count > 0)
        //        {
        //            comm_mt comm_mt = db.comm_mt.Find(mt_id);
        //            db.comm_mt.Remove(comm_mt);
        //            db.SaveChanges();
        //            db.SaveChanges();
        //            if (count2 > 0)
        //            {
        //                foreach (var item in vaule2)
        //                {
        //                    comm_dt comm_dt = db.comm_dt.Find(item.cdid);
        //                    db.comm_dt.Remove(comm_dt);
        //                    db.SaveChanges();
        //                }
        //            }
        //            return Json(result, JsonRequestBehavior.AllowGet);
        //        }
        //        result = new { result = false };
        //        return Json(result, JsonRequestBehavior.AllowGet);
        //    }
        //    catch (Exception ex)
        //    {

        //        throw ex;
        //    }




        //}

        public JsonResult searchWHSCommon()
        {
            string mt_cd = Request.QueryString["mt_cd"];
            string mt_nm = Request.QueryString["mt_nm"];
            string mt_exp = Request.QueryString["mt_exp"];
            var sql = new StringBuilder();
            sql.Append(" SELECT mt.* ")
                .Append("FROM  comm_mt as mt ")
                 .Append("WHERE ('" + mt_cd + "'='' OR  mt.mt_cd like '%" + mt_cd + "%' )")
                 .Append("AND ('" + mt_nm + "'='' OR  mt.mt_nm like '%" + mt_nm + "%' )")
                 .Append("AND ('" + mt_exp + "'='' OR   mt.mt_exp like '%" + mt_exp + "%' )")
                 .Append("AND mt.div_cd='MMS' ");

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

        public JsonResult deleteWHSCommonDt(int cdid)
        {

            int count = db.comm_dt.Where(x => x.cdid == cdid).ToList().Count();

            var result = new { result = true };

            if (count > 0)
            {
                comm_dt comm_dt = db.comm_dt.Find(cdid);
                db.comm_dt.Remove(comm_dt);
                db.SaveChanges();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            result = new { result = false };
            return Json(result, JsonRequestBehavior.AllowGet);


        }

        #endregion

        #region Factory_Location
        public ActionResult Factory_Location()
        {
            return SetLanguage("");
        }

        public ActionResult PrintFactory(string id)
        {
            ViewData["Message"] = id;
            return View();
        }
        public ActionResult QRbarcodefactory(string id)
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

        public ActionResult GetWHfactory_wo()
        {
            var vaule = db.lct_info.Where(item => item.lct_cd.StartsWith("002")).OrderBy
                (item => item.lct_cd).ToList();
            return Json(new { rows = vaule }, JsonRequestBehavior.AllowGet);

        }
        public JsonResult get_lct_001(string lct_cd)
        {
            var list = db.lct_info.Where(x => x.lct_cd.StartsWith("002") && x.level_cd == "001" && x.lct_cd != lct_cd).ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        public ActionResult insertfactory_lct(lct_info WHL, string lct_nm, string re_mark, int lctno, string use_yn, string mv_yn, string wp_yn, string ft_yn, string nt_yn, string root_yn)
        {

            WHL.mv_yn = mv_yn;
            WHL.wp_yn = wp_yn;
            WHL.ft_yn = ft_yn;
            WHL.nt_yn = nt_yn;
            if (nt_yn == null)
            {
                WHL.nt_yn = "N";
            }
            if (wp_yn == null)
            {
                WHL.wp_yn = "N";
            }
            if (ft_yn == null)
            {
                WHL.ft_yn = "N";
            }
            if (mv_yn == null)
            {
                WHL.mv_yn = "N";
            }
            string[] listtring = { "0", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

            WHL.lct_nm = lct_nm;
            WHL.use_yn = use_yn;
            WHL.order_no = 1;
            WHL.re_mark = re_mark;
            var list = db.lct_info.Where(item => item.lct_cd.StartsWith("002")).
               OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();


            var menuCd = string.Empty;
            var subMenuIdConvert = 0;
            var sublog = new lct_info();
            if (root_yn == "1")
            {
                var ds = list.Where(x => x.level_cd == "000").OrderByDescending(item => item.reg_dt).ToList();
                if (ds.Count() == 0)
                {
                    WHL.lct_cd = "002001000000000000";
                    WHL.level_cd = "000";
                    WHL.index_cd = "F1";
                    WHL.shelf_cd = "0";
                    WHL.lct_bar_cd = "F1";
                    WHL.up_lct_cd = "002001000000000000";
                    WHL.mn_full = lct_nm;
                }
                else
                {
                    sublog = ds.FirstOrDefault();
                    menuCd = "002";
                    WHL.level_cd = "000";
                    WHL.shelf_cd = "0";

                    if (sublog != null)
                    {
                        var subMenuId = sublog.lct_cd.Substring(3, 3);
                        int.TryParse(subMenuId, out subMenuIdConvert);
                    }
                    var giatri = CreateId_F(subMenuIdConvert + 1);
                    menuCd = string.Format("{0}{1}000000000000", menuCd, giatri);
                    WHL.lct_cd = menuCd;
                    WHL.index_cd = "F" + int.Parse(giatri);
                    WHL.up_lct_cd = menuCd;
                    WHL.mn_full = lct_nm;
                    WHL.lct_bar_cd = WHL.index_cd;
                }
            }
            else
            {
                var upLevel = db.lct_info.FirstOrDefault(item => item.lctno == lctno);
                WHL.up_lct_cd = upLevel.lct_cd;
                var subnew = "";
             
                switch (upLevel.level_cd)
                {
                    case "000":
                        menuCd = upLevel.lct_cd.Substring(0, 6);
                        WHL.level_cd = "001";
                        sublog = list.Where(item => item.up_lct_cd == upLevel.lct_cd).OrderByDescending(item => item.reg_dt).FirstOrDefault();
                        if (sublog != null)
                        {
                            var subMenuId = sublog.lct_cd.Substring(6, 3);
                            int.TryParse(subMenuId, out subMenuIdConvert);
                        }
                        var giatri = CreateId_F(subMenuIdConvert + 1);
                        menuCd = string.Format("{0}{1}000000000", menuCd, giatri);
                        WHL.lct_cd = menuCd;
                        WHL.shelf_cd = (int.Parse(giatri)).ToString();
                        WHL.index_cd = upLevel.index_cd;
                        WHL.mn_full = list.FirstOrDefault().mn_full + ">" + WHL.lct_nm;
                        WHL.lct_bar_cd = WHL.index_cd + "-" + WHL.shelf_cd;
                        break;
                    case "001":
                        menuCd = upLevel.lct_cd.Substring(0, 9);
                        WHL.level_cd = "002";
                        var test = Request.QueryString["mn_x"].Trim();
                        sublog = (Request.QueryString["mn_x"].Trim() == null || Request.QueryString["mn_x"].Trim() == "" ?
                         list.Where(item => item.up_lct_cd == upLevel.lct_cd).OrderByDescending(item => item.reg_dt).FirstOrDefault() :
                         list.Where(item => item.up_lct_cd == Request.QueryString["mn_x"].Trim()).OrderByDescending(item => item.reg_dt).FirstOrDefault());
                        if (sublog != null)
                        {
                            subnew = sublog.lct_cd.Substring(11, 1);
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
                        menuCd = string.Format("{0}{1}", menuCd, "00" + subnew + "000000");

                        WHL.lct_cd = menuCd;
                        var trc = menuCd.Substring(3, 3);
                        WHL.index_cd = "F" + int.Parse(trc);
                        WHL.shelf_cd = subnew;
                        WHL.lct_bar_cd = upLevel.lct_bar_cd + "-" + WHL.shelf_cd;
                        WHL.mn_full = upLevel.mn_full + ">" + WHL.lct_nm;
                        break;
                    case "002":
                        menuCd = upLevel.lct_cd.Substring(0, 12);
                        WHL.level_cd = "003";
                        sublog = list.Where(item => item.up_lct_cd == upLevel.lct_cd).OrderByDescending(item => item.reg_dt).FirstOrDefault();
                        if (sublog != null)
                        {
                            var subMenuId = sublog.lct_cd.Substring(12, 3);
                            int.TryParse(subMenuId, out subMenuIdConvert);
                        }
                        var giatri1 = CreateId_F(subMenuIdConvert + 1);
                        menuCd = string.Format("{0}{1}", menuCd, giatri1) + "000";
                        WHL.lct_cd = menuCd;
                        WHL.index_cd = upLevel.index_cd;
                        WHL.shelf_cd = (int.Parse(giatri1)).ToString();
                        WHL.lct_bar_cd = upLevel.lct_bar_cd + WHL.shelf_cd;
                        WHL.mn_full = upLevel.mn_full + ">" + WHL.lct_nm;
                        break;
                }
            }

            DateTime reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
            String dateString = reg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");

            DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
            String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
            WHL.reg_dt = reg_dt;
            WHL.chg_dt = chg_dt;
            if (ModelState.IsValid)
            {
                db.Entry(WHL).State = EntityState.Added;
                db.SaveChanges();
                return Json(WHL, JsonRequestBehavior.AllowGet);
            }
            return View();
        }


        public ActionResult updatefactory_lct(string lct_nm, string re_mark, int lctno, string use_yn, string mv_yn, string wp_yn, string ft_yn, string nt_yn)
        {
            lct_info lct_info = db.lct_info.Find(lctno);
            lct_info.mv_yn = mv_yn;
            lct_info.use_yn = use_yn;
            lct_info.wp_yn = wp_yn;
            lct_info.ft_yn = ft_yn;
            lct_info.nt_yn = nt_yn;
            if (mv_yn == null) { lct_info.mv_yn = "N"; }
            if (nt_yn == null) { lct_info.nt_yn = "N"; }
            if (wp_yn == null)
            {
                lct_info.wp_yn = "N";
            }
            if (ft_yn == null)
            {
                lct_info.ft_yn = "N";
            }
            var upLevel = db.lct_info.Where(x => x.lct_cd == lct_info.up_lct_cd).ToList();
            if (upLevel.Count > 0)
            {

                var mn_trc = upLevel.FirstOrDefault().mn_full;
                lct_info.mn_full = mn_trc + ">" + lct_nm;
            }


            lct_info.lct_cd = lct_info.lct_cd;
            lct_info.re_mark = re_mark;
            lct_info.lct_nm = lct_nm;
            DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
            String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
            //lct_info.chg_dt = chg_dt;
            lct_info.chg_id = Session["userid"].ToString();

            if (ModelState.IsValid)
            {
                db.Entry(lct_info).State = EntityState.Modified;
                db.SaveChanges();
                return Json(lct_info, JsonRequestBehavior.AllowGet);
            }
            return View(lct_info);
        }
        public ActionResult deleteFactoryLocMgt(int lctno)
        {
            var item_count = db.lct_info.Count(x => x.lctno == lctno);
            if (item_count != 0)
            {
                var item = db.lct_info.FirstOrDefault(x => x.lctno == lctno);
                db.lct_info.Remove(item);
                db.SaveChanges();
            }
            return Json(new { result = item_count }, JsonRequestBehavior.AllowGet);
        }
        private string CreateId_F(int id)
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
        public ActionResult Autolct_cd(string Prefix)
        {
            //Note : you can bind same list from database  
            ArrayList row_data = new ArrayList();


            //Searching records from list using LINQ query  
            var vaule = db.lct_info.Where(item => item.lct_cd.StartsWith("002")).
              OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();

            var style = (from N in vaule
                         where (N.lct_cd.Contains(Prefix) || N.lct_cd.Contains(Prefix.ToUpper()))
                         select new { N.lct_cd });
            if (style.Count() == 0)
            {
                return View();
            }
            return Json(style, JsonRequestBehavior.AllowGet);
        }
        public ActionResult SearchFactory_Location(string lct_cd = null, string locationName = null)
        {
            var Data = db.lct_info.Where(item => (item.lct_cd.StartsWith("002")

           &&  item.lct_cd.Contains(lct_cd) 
           //&& item.lct_nm.Contains(locationName)

            )).OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();

           
            return Json(Data, JsonRequestBehavior.AllowGet);

        }
        #endregion

    
  

 
        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Mvc_VD.Models;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using System.Net.Http;
using Mvc_VD.Models.Language;

namespace Mvc_VD.Controllers
{
    public class systemController : BaseController
    {
        private Entities db = new Entities();
        string exs = "Lỗi hệ thống!!!";
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

        #region User Mgt
        //Begin Layout
        public ActionResult Index()
        {
            //ViewBag.userid = Session["userid"];
            //ViewBag.authData = Session["authData"] + "@";
            //ViewBag.authList = Session["authList"];
            return View();
        }

        public ActionResult userMgt()
        {
            return SetLanguage("");
        }

        public ActionResult searchpp_staff(string uname)
        {
            var sql = new StringBuilder();
            sql.Append(" SELECT * ")
                .Append("FROM  mb_info as a ")
                .Append("LEFT JOIN comm_dt as b ON ")
                .Append("( a.position_cd = b.dt_cd )")
                .Append("Where ('" + uname + "'='' OR  a.uname like '%" + uname + "%' )");

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
            var result = GetJsonPersons_search(data);
            return result;
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

        public JsonResult GetJsonPersons_search(DataTable data)
        {
            var lstPersons = GetTableRows(data);
            return Json(lstPersons, JsonRequestBehavior.AllowGet);
        }

        public ActionResult getgrade()
        {
            var list = db.author_info.ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult userInfoData()
        {
            try
            {
                var mb_info_list = db.mb_info.ToList();
                var jsonData = new
                {
                    rows = mb_info_list
                };
                return Json(jsonData, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        public JsonResult insertUser(string userid, string uname, string upw, string nick_name, string cel_nb, string e_mail, string memo, string grade, string scr_yn, string mail_yn)
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
                db.mb_info.Add(mb_info);
                db.SaveChanges();

                mb_author_info mb_author_info = new mb_author_info();
                mb_author_info.userid = userid;
                mb_author_info.at_cd = db.author_info.FirstOrDefault(x => x.at_nm == grade).at_cd;
                mb_author_info.reg_dt = DateTime.Now;
                mb_author_info.chg_dt = DateTime.Now;
                db.mb_author_info.Add(mb_author_info);
                db.SaveChanges();

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult modifyUser(string userid, string uname, string upw, string nick_name, string cel_nb, string e_mail, string memo, string grade, string scr_yn, string mail_yn)
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
                mb_info.chg_dt = DateTime.Now;
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
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(result, JsonRequestBehavior.AllowGet);
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
        #endregion

        #region CommonMgt
        public ActionResult Common()
        {
            try
            {
                HttpCookie cookie = HttpContext.Request.Cookies["language"];
                if (cookie != null)
                {
                    ViewBag.language = cookie.Value;
                }

                return View(db.comm_mt.ToList());
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        [HttpPost]
        public ActionResult Create(comm_mt comm_mt)
        {
            if (ModelState.IsValid)
            {
                db.comm_mt.Add(comm_mt);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(comm_mt);
        }

        [HttpPost]
        public ActionResult Edit(comm_mt comm_mt)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    db.Entry(comm_mt).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                return View(comm_mt);
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public ActionResult GetJqGridDataCommDt(comm_dt comm_dt, comm_mt comm_mt, string mt_cd)
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
                                 chg_dt = comm_dt_.chg_dt
                             }).OrderByDescending(x => x.chg_dt).ToList();
                var jsonDataComDt = new
                {
                    rows = vaule
                };
                return Json(jsonDataComDt, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public ActionResult insertCommonDetail(comm_dt comm_dt, string dc_mt_cd, string dc_dt_cd, string dc_dt_nm, string dc_dt_exp, int dc_dt_order, string dc_use_yn)
        {
            DateTime reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
            String dateString = reg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
            int countCommon = db.comm_dt.Count(x => x.dt_cd == dc_dt_cd && x.mt_cd == dc_mt_cd);
            if (countCommon == 0)
            {
                DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                comm_dt.mt_cd = dc_mt_cd;
                comm_dt.dt_cd = dc_dt_cd;
                comm_dt.dt_nm = dc_dt_nm;
                comm_dt.dt_exp = dc_dt_exp;
                comm_dt.dt_order = dc_dt_order;
                comm_dt.use_yn = dc_use_yn;
                comm_dt.reg_dt = reg_dt;
                comm_dt.chg_dt = chg_dt;
                comm_dt.del_yn = "N";
                db.comm_dt.Add(comm_dt);
                db.SaveChanges();
                return Json(new { result = countCommon, mt_cd = dc_mt_cd, dt_cd = dc_dt_cd }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { result = countCommon, mt_cd = dc_mt_cd, dt_cd = dc_dt_cd }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult updateCommonDetail(string dm_mt_cd, string dm_dt_cd, string dm_dt_nm, string dm_dt_exp, int dm_dt_order, string dm_use_yn, int dm_cdid)
        {
            int countcommondetail = db.comm_dt.Count(x => x.cdid == dm_cdid);
            if (countcommondetail > 0)
            {
                var datacommondetail = db.comm_dt.FirstOrDefault(x => x.cdid == dm_cdid);
                datacommondetail.mt_cd = dm_mt_cd;
                datacommondetail.dt_cd = dm_dt_cd;
                datacommondetail.dt_nm = dm_dt_nm;
                datacommondetail.dt_exp = dm_dt_exp;
                datacommondetail.dt_order = dm_dt_order;
                datacommondetail.use_yn = dm_use_yn;
                db.SaveChanges();
                return Json(new { result = countcommondetail, mt_cd = dm_mt_cd, dt_cd = dm_dt_cd }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { result = countcommondetail, mt_cd = dm_mt_cd, dt_cd = dm_dt_cd }, JsonRequestBehavior.AllowGet);


        }

        public ActionResult GetJqGridData()
        {
            try
            {
                var vaule = (from comm_mt_ in db.comm_mt
                             orderby comm_mt_.mt_cd
                             where comm_mt_.mt_cd.StartsWith("COM")
                             select new
                             {
                                 mt_id = comm_mt_.mt_id,
                                 mt_cd = comm_mt_.mt_cd,
                                 mt_nm = comm_mt_.mt_nm,
                                 mt_exp = comm_mt_.mt_exp,
                                 use_yn = comm_mt_.use_yn,
                                 chg_dt = comm_mt_.chg_dt
                             }).OrderByDescending(x => x.mt_cd).ToList();
                var commmtData = new
                {

                    rows = vaule
                };
                return Json(commmtData, JsonRequestBehavior.AllowGet);
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


        public string automtcd2(int id)
        {
            if (id.ToString().Length < 2)
            {
                return string.Format("000{0}", id);
            }

            if ((id.ToString().Length < 3) || (id.ToString().Length == 2))
            {
                return string.Format("00{0}", id);
            }

            if ((id.ToString().Length < 4) || (id.ToString().Length == 3))
            {
                return string.Format("0{0}", id);
            }
            if ((id.ToString().Length < 5) || (id.ToString().Length == 4))
            {
                return string.Format("{0}", id);
            }
            return string.Empty;
        }

        public ActionResult insertCommon(comm_mt comm_mt, string mt_cd, string mt_nm, string mt_exp, string use_yn, string div_cd)
        {
            var countname = db.comm_mt.Count(x => x.mt_nm == mt_nm);
            if (countname == 0)
            {
                try
                {
                    int countlist = db.comm_mt.Where(item => item.mt_cd.StartsWith("COM")).ToList().Count();
                    var mt_cd_tmp = string.Empty;
                    var submtcdconvert = 0;

                    var listlast = db.comm_mt.Where(item => item.mt_cd.StartsWith("COM")).OrderBy(item => item.mt_cd).ToList().LastOrDefault();

                    if (countlist == 0)
                    {
                        mt_cd = div_cd + "COM001";
                    }
                    else
                    {
                        var bien = listlast.mt_cd;
                        var submtcd = bien.Substring(bien.Length - 3, 3);
                        int.TryParse(submtcd, out submtcdconvert);

                        mt_cd_tmp = "COM" + string.Format("{0}{1}", mt_cd_tmp, automtcd((submtcdconvert + 1)));
                        mt_cd = mt_cd_tmp;
                    }

                    DateTime reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                    String dateString = reg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");

                    DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                    String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");

                    comm_mt.mt_cd = mt_cd;
                    comm_mt.mt_nm = mt_nm;
                    comm_mt.mt_exp = mt_exp;
                    comm_mt.use_yn = use_yn;
                    comm_mt.reg_dt = reg_dt;
                    comm_mt.chg_dt = chg_dt;
                    if (ModelState.IsValid)
                    {

                        db.comm_mt.Add(comm_mt);
                        db.SaveChanges();
                        return Json(new { result = countname, mt_cd = mt_cd }, JsonRequestBehavior.AllowGet);

                    }

                }
                catch (Exception ex)
                {

                    throw ex;
                }
            }
            return Json(new { result = countname, mt_cd = mt_cd }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult updateCommon(int mt_id, string mt_cd, string mt_nm, string mt_exp, string use_yn)
        {
            var count_mt_cd = db.comm_mt.Count(x => x.mt_cd == mt_cd);
            if (count_mt_cd != 0)
            {
                try
                {
                    comm_mt comm_mt = db.comm_mt.Find(mt_id);
                    DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                    String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                    comm_mt.mt_nm = mt_nm;
                    comm_mt.mt_nm = mt_nm;
                    comm_mt.mt_exp = mt_exp;
                    comm_mt.use_yn = use_yn;
                    //comm_mt.reg_dt = reg_dt;
                    //comm_mt.chg_dt = chg_dt;
                    if (ModelState.IsValid)
                    {
                        db.Entry(comm_mt).State = EntityState.Modified;
                        db.SaveChanges(); // line that threw exception
                        return Json(new { result = count_mt_cd, mt_cd = mt_cd }, JsonRequestBehavior.AllowGet);
                    }
                }
                catch (Exception ex)
                {

                    throw ex;
                }
            }
            return Json(new { result = count_mt_cd, mt_cd = mt_cd }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult deleteCommon(string commonmaincode)
        {

            comm_mt common_data = new comm_mt();
            var result = db.comm_mt.Count(x => x.mt_cd == commonmaincode);
            if (result > 0)
            {
                var common = db.comm_mt.FirstOrDefault(x => x.mt_cd == commonmaincode);
                db.comm_mt.Remove(common);
                db.SaveChanges();

                var countcommondt = db.comm_dt.Count(x => x.mt_cd == commonmaincode);
                for (int i = 1; i <= countcommondt; i++)
                {
                    var commondt = db.comm_dt.FirstOrDefault(x => x.mt_cd == commonmaincode);
                    db.comm_dt.Remove(commondt);
                    db.SaveChanges();
                };
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult deleteCommon_dt(string commondt_manincode, string commondt_dtcd)
        {
            var countcommondt = db.comm_dt.Count(x => x.mt_cd == commondt_manincode && x.dt_cd == commondt_dtcd);
            if (countcommondt > 0)
            {
                var commondt = db.comm_dt.FirstOrDefault(x => x.mt_cd == commondt_manincode && x.dt_cd == commondt_dtcd);
                db.comm_dt.Remove(commondt);
                db.SaveChanges();
            }
            return Json(countcommondt, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region MenuMgt
        public ActionResult menuMgt()
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
        //get giữ liệu _cho db menu_info MenuMgt

        public ActionResult GetDataMenu()
        {
            string mn_cd = (Request.QueryString["mn_cd"] == null ? "" : Request.QueryString["mn_cd"]);
            string mn_nm = (Request.QueryString["mn_nm"] == null ? "" : Request.QueryString["mn_nm"]);
            string full_nm = (Request.QueryString["full_nm"] == null ? "" : Request.QueryString["full_nm"]);

            var menu_info = db.menu_info
                   .Where(x => (mn_cd == "" || x.mn_cd.Contains(mn_cd))
                   && (mn_nm == "" || x.mn_nm.Contains(mn_nm))
                    && (full_nm == "" || x.mn_full.Contains(full_nm))
                   )
                   .ToList().OrderBy(x => x.mn_cd);
            return Json(new { rows = menu_info }, JsonRequestBehavior.AllowGet);
        }

        //Creat giữ liệu đưa vào bảng menu

        public ActionResult insertMenu(menu_info WHL, string root_yn,string url_link,int mnno)
        {
            var list = db.menu_info.OrderBy(item => item.mn_cd).ThenBy(item => item.level_cd).ToList();

            var menuCd = string.Empty;
            var subMenuIdConvert = 0;
            var sublog = new menu_info();
            if (root_yn == "1")
            {
                var ds = list.Where(x => x.level_cd == "000").OrderByDescending(item => item.reg_dt).ToList();
                if (ds.Count() == 0)
                {
                    WHL.mn_cd = "001000000000";
                    WHL.level_cd = "000";
                    WHL.up_mn_cd = "001000000000";
                    WHL.mn_full = WHL.mn_nm;
                    WHL.order_no =0;
                }
                else
                {
                    sublog = ds.FirstOrDefault();
                    menuCd = "002";
                    WHL.level_cd = "000";
                    if (sublog != null)
                    {
                        var subMenuId = sublog.mn_cd.Substring(0, 3);
                        int.TryParse(subMenuId, out subMenuIdConvert);
                    }
                    var giatri = CreateId(subMenuIdConvert + 1);
                    menuCd = string.Format("{0}{1}000000000", menuCd, giatri);
                    WHL.mn_cd = menuCd;
                    WHL.up_mn_cd = menuCd;
                    WHL.mn_full = WHL.mn_nm;
                    WHL.order_no = 1;
                }
            }
            else
            {
                var upLevel = list.FirstOrDefault(item => item.mnno == mnno);
                WHL.up_mn_cd = upLevel.mn_cd;
                
                switch (upLevel.level_cd)
                {
                    case "000":
                        menuCd = upLevel.mn_cd.Substring(0,3);
                        WHL.level_cd = "001";
                        sublog = list.Where(item => item.up_mn_cd == upLevel.mn_cd).OrderByDescending(item => item.reg_dt).FirstOrDefault();
                        if (sublog != null)
                        {
                            var subMenuId = sublog.mn_cd.Substring(3, 3);
                            int.TryParse(subMenuId, out subMenuIdConvert);
                        }
                        var giatri = CreateId(subMenuIdConvert + 1);
                        menuCd = string.Format("{0}{1}000000", menuCd, giatri);
                        WHL.mn_cd = menuCd;
                        WHL.mn_full = upLevel.mn_full + ">" + WHL.mn_nm;
                        break;
                    case "001":
                        menuCd = upLevel.mn_cd.Substring(0, 6);
                        WHL.level_cd = "002";
                        sublog = list.Where(item => item.up_mn_cd == upLevel.mn_cd).OrderByDescending(item => item.reg_dt).FirstOrDefault();
                        if (sublog != null)
                        {
                            var subMenuId = sublog.mn_cd.Substring(6, 3);
                            int.TryParse(subMenuId, out subMenuIdConvert);
                        }
                        giatri = CreateId(subMenuIdConvert + 1);
                        menuCd = string.Format("{0}{1}000", menuCd, giatri);
                        WHL.mn_cd = menuCd;
                        WHL.mn_full = upLevel.mn_full + ">" + WHL.mn_nm;
                        break;
                }
            }

            DateTime reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
            String dateString = reg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");

            DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
            String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
            WHL.reg_dt = reg_dt;
            WHL.chg_dt = chg_dt;
            WHL.sub_yn = "N";
            if (ModelState.IsValid)
            {
                db.Entry(WHL).State = EntityState.Added;
                db.SaveChanges();
                return Json(WHL, JsonRequestBehavior.AllowGet);
            }
            return View();
        }




        private string CreateId(int id)
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
        public ActionResult updateMenuMgt(menu_info WHL)
        {
            var countMenuFinal = db.menu_info.Where(x => x.mnno == WHL.mnno).ToList();
            if (countMenuFinal.Count() > 0)
            {
                var menu_info = countMenuFinal.FirstOrDefault();
                StringBuilder varname1 = new StringBuilder();
                varname1.Append("call menuinfo_afterupdate('" + WHL.mn_nm + "','"+ WHL.url_link + "' \n");
                varname1.Append(",'" + WHL.use_yn + "','"+ menu_info.re_mark + "','"+ WHL.mnno + "' ,'" + menu_info.mn_cd + "','" + menu_info.level_cd + "','" + menu_info.mn_nm + "'  ); \n");
            
                var effecrow = new Excute_query().Execute_NonQuery(varname1);

                return Json(true, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }

        }

        public JsonResult deleteMenu(int mnno)
        {
            var menudata = db.menu_info.Where(x => x.mnno == mnno).SingleOrDefault();
            if (menudata!=null)
            {
                db.menu_info.Remove(menudata);
                db.SaveChanges();
            }
            return Json(menudata, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region AuthorityMgt
        public ActionResult authMgt()
        {
            return SetLanguage("");
        }
        public JsonResult getRole() {
            var list = db.comm_dt.Where(x => x.mt_cd == "MMS014").ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        public ActionResult memberAuthMgt()
        {
            return View();
        } // GET: /System/authMgt

        public ActionResult GetAuthor()
        {
            var author = (from a in db.author_info
                          select new
                          {
                              atno = a.atno,
                              at_cd = a.at_cd,
                              at_nm = a.at_nm,
                              role = a.role,
                              role_nm = db.comm_dt.Where(x => x.mt_cd == "MMS014" && x.dt_cd == a.role).Select(x => x.dt_nm),
                              use_yn = a.use_yn,
                              reg_id = a.reg_id,
                              chg_id = a.chg_id,
                              chg_dt = a.chg_dt,
                              re_mark = a.re_mark,
                          }).ToList();
            var jsonauthor = new
            {

                rows = author
            };
            return Json(jsonauthor, JsonRequestBehavior.AllowGet);

        }

        [HttpPost]
        public ActionResult saveAuthMbInfo(string selRowIdMbJSON)
        {
            JavaScriptSerializer jsdata = new JavaScriptSerializer();
            var obj = jsdata.Deserialize<dynamic>(selRowIdMbJSON);
            string at_cd = obj["at_cd"];
            string at_nm = obj["at_nm"];
            var lengdata = obj["selRowIdUserArr"].Length;

            int CountRows = db.mb_author_info.Count();
            for (var i = 0; i <= lengdata - 1; i++)
            {
                string userid = obj["selRowIdUserArr"][i];
                for (var j = 1; j <= CountRows; j++)
                {
                    int resultCount = db.mb_author_info.Count(x => x.userid == userid);
                    if (resultCount > 0)
                    {
                        var CountRowsIteam = db.mb_author_info.FirstOrDefault(x => x.userid == userid);
                        db.mb_author_info.Remove(CountRowsIteam);
                        db.SaveChanges();
                    }
                }
                var mb_info = db.mb_info.FirstOrDefault(x => x.userid == userid);
                mb_info.grade = db.author_info.FirstOrDefault(x => x.at_cd == at_cd).at_nm;
                db.SaveChanges();
            }
            mb_author_info mb_author_info = new mb_author_info();
            for (var i = 0; i <= lengdata - 1; i++)
            {
                string userid = obj["selRowIdUserArr"][i];
                mb_author_info.at_cd = at_cd;
                mb_author_info.userid = userid;
                mb_author_info.reg_dt = DateTime.Now;
                mb_author_info.chg_dt = DateTime.Now;
                db.mb_author_info.Add(mb_author_info);
                db.SaveChanges();

            };
            var result = new { result = lengdata };
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetAuMember()
        {
            try
            {
                var mb_info_list = db.mb_info.Where(x=>x.lct_cd!= "staff").ToList();
                var jsonData = new
                {
                    rows = mb_info_list
                };
                return Json(jsonData, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public JsonResult search_staff_auth()
        {

            // Get Data from ajax function
            var userid = Request["userid"];
            var uname = Request.QueryString["uname"];

            var sql = new StringBuilder();
            sql.Append("SELECT * ")
                .Append("FROM mb_info as a  ")
                .Append("WHERE ('" + userid + "'='' OR a.userid like '%" + userid + "%' ) ")
                .Append("AND ('" + uname + "'='' OR  a.uname like '%" + uname + "%' )")
                .Append(" order by a.uname");
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
            var result = GetJsonPersons_search(data);
            return result;
        }

        public JsonResult getAuthorMbData(string at_cd)
        {
            try
            {
                List<string> userid = new List<string>();
                var mb_author_info = db.mb_author_info.Where(x => x.at_cd == at_cd);
                foreach (var item in mb_author_info)
                {
                    userid.Add(item.userid);
                }

                return Json(userid, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public JsonResult GetAuthorMenu()
        {
            try
            {
                var mn_cd = (Request.QueryString["mn_cd"] == null ? "" : Request.QueryString["mn_cd"].Trim());
                var mn_full = (Request.QueryString["mn_full"] == null ? "" : Request.QueryString["mn_full"].Trim());
                var menu_info_list = db.menu_info.Where(x =>
               (mn_cd == "" || x.mn_cd.Contains(mn_cd))
               &&
                (mn_full == "" || x.mn_full.Contains(mn_full))
                && x.use_yn=="Y" && (x.url_link != "") && (x.mn_cd != "011001001000")
                ).OrderBy(x => x.mn_cd).ToList();
                var SS = Session["userid"].ToString();
                if (SS != "root")
                {
                    var authMenuItem = menu_info_list.FirstOrDefault(x => x.url_link == "/system/menuMgt");
                    menu_info_list.Remove(authMenuItem);
                }

                var jsonData = new
                {
                    rows = menu_info_list
                };
                return Json(jsonData, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public JsonResult getAuthorMenuData(string at_cd)
        {
            try
            {
                List<string> mn_cd = new List<string>();
                var author_menu_list = db.author_menu_info.Where(x => x.at_cd == at_cd);
                foreach (var item in author_menu_list)
                {
                    mn_cd.Add(item.mn_cd);
                }

                return Json(mn_cd, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public ActionResult delAuthMemberInfo(mb_author_info mb_author_info)
        {
            var at_cd_Id = mb_author_info.at_cd;

            var AuthorMemberInfos = db.mb_author_info.Where(item => item.at_cd == at_cd_Id).ToList();
            if (ModelState.IsValid)
            {
                foreach (var item in AuthorMemberInfos)
                {
                    db.Entry(item).State = EntityState.Deleted;
                }
                db.SaveChanges();
            }
            var jsonDataComDt = new
            {
                rows = AuthorMemberInfos
            };
            return Json(jsonDataComDt, JsonRequestBehavior.AllowGet);
        }//End Delete

        public ActionResult saveAuthMemberInfo(author_info author_info, mb_info mb_info, mb_author_info mb_author_info, string userid, string at_cd, string re_mark, string use_yn)
        {
            var userId = mb_info.userid;
            var authorcd = author_info.at_cd;

            mb_author_info.reg_dt = DateTime.Now;
            mb_author_info.chg_dt = DateTime.Now;

            var a = db.mb_author_info.Any(item => ((item.userid == userId) && (item.at_cd == authorcd)));
            if (db.mb_author_info.Any(item => ((item.userid == userId) && (item.at_cd == authorcd))) == false)
            {
                //INSERT INTO mb_author_info (userid, lct_cd)
                var InsertMbLct = (new mb_author_info()
                {
                    userid = mb_info.userid,
                    at_cd = author_info.at_cd,
                    re_mark = author_info.re_mark,
                    use_yn = author_info.use_yn,
                    reg_dt = mb_author_info.reg_dt,
                    chg_dt = mb_author_info.chg_dt,

                });

                if (ModelState.IsValid)
                {
                    db.Entry(InsertMbLct).State = EntityState.Added;
                    db.SaveChanges();
                }
                var jsonDataComDt = new
                {
                    rows = InsertMbLct
                };
                return Json(jsonDataComDt, JsonRequestBehavior.AllowGet);
            }
            else
                return View("GetAuMember");
        }

        [HttpPost]
        public ActionResult saveAuthMenuInfo_addHome()
        {

            try
            {
                string at_cd = Request.QueryString["at_cd"];
                string mn_id = Request.QueryString["mn_id"];
                string bo_check = Request.QueryString["bo_check"];
               var kq = mn_id + "," + "011001001000";
                var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                StringBuilder varname1 = new StringBuilder();
                varname1.Append("INSERT INTO author_menu_info (at_cd, mn_cd, mn_nm,url_link,st_yn,ct_yn,mt_yn,del_yn,reg_dt,chg_dt) \n");
                varname1.Append("SELECT '" + at_cd + "', mn_cd, mn_nm,url_link,'Y','N','N','N','" + time + "','" + time + "' \n");
                varname1.Append("FROM   menu_info \n");
                varname1.Append("WHERE \n");
                varname1.Append("mn_cd not in (select mn_cd from author_menu_info where at_cd='" + at_cd + "' and mn_cd in (" + kq + ")) \n");
                varname1.Append("and mn_cd in (" + kq + ");");
                if (bo_check != null && bo_check != "")
                {
                    varname1.Append("DELETE FROM author_menu_info \n");
                    varname1.Append("WHERE mn_cd in ("+ bo_check + ") and at_cd='"+ at_cd + "'");
                }
                var effecrow = new Excute_query().Execute_NonQuery(varname1);
                var result = new { result = true, message = ss };
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {

                var result = new { result = false, message = exs };
                return Json(result, JsonRequestBehavior.AllowGet);
            }
          
        }
        public ActionResult CreateAuthor(author_info author_info, string at_cd, string at_nm)
        {
            var menuCd = string.Empty;
            var subMenuIdConvert = 0;
            var sublog = new author_info();
            var m = db.author_info.ToList().Count;
            var list = db.author_info.ToList().LastOrDefault();
            if (list == null)
            {
                author_info.at_cd = "001";
            }
            else
            {
                var bien = list.at_cd;
                var subMenuId = bien.Substring(bien.Length - 3, 3);
                int.TryParse(subMenuId, out subMenuIdConvert);
                menuCd = string.Format("{0}{1}", menuCd, CreateId((subMenuIdConvert + 1)));
                author_info.at_cd = menuCd;

            }
            author_info.chg_dt = DateTime.Now;

            var CountAuth = db.author_info.Count(x => x.at_nm == at_nm);
            if (CountAuth == 0)
            {
                db.author_info.Add(author_info);
                db.SaveChanges();
            }

          var result = (from a in db.author_info
                        where a.at_cd == author_info.at_cd
                        select new
                        {
                            atno = a.atno,
                            at_cd = a.at_cd,
                            at_nm = a.at_nm,
                            role = a.role,
                            role_nm = db.comm_dt.Where(x => x.mt_cd == "MMS014" && x.dt_cd == a.role).Select(x => x.dt_nm),
                            use_yn = a.use_yn,
                            reg_id = a.reg_id,
                            chg_id = a.chg_id,
                            chg_dt = a.chg_dt,
                            re_mark = a.re_mark,
                        }).ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult EditAuthor(string at_cd, string at_nm, string re_mark, string use_yn,string role)
        {

            var result = db.author_info.Count(x => x.at_cd == at_cd);
            if (result > 0)
            {
                author_info author_info = db.author_info.FirstOrDefault(x => x.at_cd == at_cd);
                author_info.re_mark = re_mark;
                author_info.use_yn = use_yn;
                author_info.at_nm = at_nm;
                author_info.role = role;
                DateTime reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                String dateString = reg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                author_info.chg_dt = chg_dt;
                db.SaveChanges();
                var author = (from a in db.author_info
                              where a.atno == author_info.atno
                              select new
                              {
                                  atno = a.atno,
                                  at_cd = a.at_cd,
                                  at_nm = a.at_nm,
                                  role = a.role,
                                  role_nm = db.comm_dt.Where(x => x.mt_cd == "MMS014" && x.dt_cd == a.role).Select(x => x.dt_nm),
                                  use_yn = a.use_yn,
                                  reg_id = a.reg_id,
                                  chg_id = a.chg_id,
                                  chg_dt = a.chg_dt,
                                  re_mark = a.re_mark,
                              }).ToList();
                return Json(author, JsonRequestBehavior.AllowGet);

            }
            else
            {
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult deleteAuthor(string at_cd)
        {
            var countdataAuthor = db.author_info.Count(x => x.at_cd == at_cd);
            if (countdataAuthor > 0)
            {
                var dataAuthor_record = db.author_info.FirstOrDefault(x => x.at_cd == at_cd);
                db.author_info.Remove(dataAuthor_record);
                db.SaveChanges();
            }
            var countdataAuthorMenu = db.author_menu_info.Count(x => x.at_cd == at_cd);
            if (countdataAuthorMenu > 0)
            {
                for (var i = 0; i <= countdataAuthorMenu - 1; i++)
                {
                    var dataAuthor_record = db.author_menu_info.FirstOrDefault(x => x.at_cd == at_cd);
                    db.author_menu_info.Remove(dataAuthor_record);
                    db.SaveChanges();
                }

            }
            var countdataAuthorUser = db.mb_author_info.Count(x => x.at_cd == at_cd);
            if (countdataAuthorUser > 0)
            {
                for (var i = 0; i <= countdataAuthorUser - 1; i++)
                {
                    var dataAuthor_record = db.mb_author_info.FirstOrDefault(x => x.at_cd == at_cd);
                    db.mb_author_info.Remove(dataAuthor_record);
                    db.SaveChanges();
                }

            }
            return Json(countdataAuthor, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region UserAuthorityMgt
        public ActionResult GetMemberInfo()
        {
            var aumember = db.mb_info.ToList();
            var jsonaumember = new
            {

                rows = aumember
            };
            return Json(jsonaumember, JsonRequestBehavior.AllowGet);

        }  //End list Member info

        public ActionResult GetMemberLct(string selected, string userid = "")
        {
            var getLct = string.IsNullOrEmpty(userid) ? null : db.lct_info.ToList();
            var memberLocaltionInfos = db.mb_lct_info.Where(item => item.userid == userid).ToList();
            if (getLct != null)
            {
                foreach (var item in getLct)
                {
                    var c = memberLocaltionInfos.Any(mbItem => mbItem.lct_cd == item.lct_cd);
                    if (c == true)
                    {
                        item.selected = "true";
                    }
                    else
                    {
                        item.selected = "false";
                    }
                }
            }
            var jsonDataLct = new
            {
                rows = getLct
            };

            return Json(jsonDataLct, JsonRequestBehavior.AllowGet);
        }
        //End list2 Member & Location Authority Connection

        public ActionResult delMemberLocationInfo(mb_lct_info mb_lct_info, string userid)
        {
            var userId = mb_lct_info.userid;

            var AuthorMenuInfos = db.mb_lct_info.Where(item => item.userid == userId).ToList();
            if (ModelState.IsValid)
            {
                foreach (var item in AuthorMenuInfos)
                {
                    db.Entry(item).State = EntityState.Deleted;
                }
                db.SaveChanges();
            }
            var jsonDataComDt = new
            {
                rows = AuthorMenuInfos
            };
            return Json(jsonDataComDt, JsonRequestBehavior.AllowGet);
        }
        //End Delete 

        public ActionResult saveMemberLocationInfo(mb_info mb_info, lct_info lct_info, mb_lct_info mb_lct_info, int lctno, string userid, string lct_cd, string re_mark, string use_yn)
        {
            var userId = mb_info.userid;
            var localtionCd = lct_info.lct_cd;

            mb_lct_info.reg_dt = DateTime.Now;
            mb_lct_info.chg_dt = DateTime.Now;
            var a = db.mb_lct_info.Any(item => ((item.userid == userId) && (item.lct_cd == localtionCd)));
            //if (db.mb_lct_info.Any(item => ((item.userid == userId) && (item.lct_cd == localtionCd))) == false)
            //{
            //INSERT INTO mb_lct_info (userid, lct_cd)
            var InsertMbLct = (new mb_lct_info()
            {
                userid = mb_info.userid,
                lct_cd = lct_info.lct_cd,
                re_mark = lct_info.re_mark,
                use_yn = lct_info.use_yn,
                reg_dt = mb_lct_info.reg_dt,
                chg_dt = mb_lct_info.chg_dt,

            });

            if (ModelState.IsValid)
            {
                db.Entry(InsertMbLct).State = EntityState.Added;
                db.SaveChanges();
            }
            var jsonDataComDt = new
            {
                rows = InsertMbLct
            };
            return Json(jsonDataComDt, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region location
        public ActionResult locMgt()
        {
            return View();
        }
        public ActionResult Getlocation()
        {

            var vaule = db.lct_info.Where(item => !(item.lct_cd.StartsWith("002")) && !(item.lct_cd.StartsWith("001")) && !(item.lct_cd.StartsWith("004")) && !(item.lct_cd.StartsWith("003")) && !(item.lct_cd.StartsWith("005"))).OrderBy
               (item => item.lct_cd).ToList();
            return Json(new { rows = vaule }, JsonRequestBehavior.AllowGet);

        }



        public ActionResult insertlocMgt(lct_info lct_info, string lct_nm, string mv_yn, string root_yn, string re_mark, int lctno, string use_yn)
        {
            lct_info.lct_nm = lct_nm;
            lct_info.order_no = 1;
            lct_info.mv_yn = mv_yn;
            lct_info.re_mark = re_mark;
            var list = db.lct_info.Where(item => !(item.lct_cd.StartsWith("002")) && !(item.lct_cd.StartsWith("001")) && !(item.lct_cd.StartsWith("004")) && !(item.lct_cd.StartsWith("003")) && !(item.lct_cd.StartsWith("005"))).
               OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();
            var upLevel1 = list.FirstOrDefault(item => item.lctno == lctno);
            if (root_yn == "1")
            {
                //if (root_yn == null)
                //{
                //    lct_info.lctno = 1;
                //    lct_info.lct_cd = "001000000000000000";
                //    lct_info.level_cd = "000";
                //    lct_info.up_lct_cd = "000000000000000000";
                //}
                //lct_info.up_lct_cd = "000000000000000000";
                lct_info.level_cd = "000";
                var menuCd1 = string.Empty;
                var subMenuIdConvert = 0;
                var listY = db.lct_info.Where(item => item.level_cd == "000").ToList().OrderByDescending(item => item.lct_cd).FirstOrDefault();

                if (listY != null)
                {
                    var subMenuId = listY.lct_cd.Substring(0, 3);
                    int.TryParse(subMenuId, out subMenuIdConvert);//tra
                    if (subMenuIdConvert == 4 || subMenuIdConvert == 2 || subMenuIdConvert == 1)
                    {
                        subMenuIdConvert = subMenuIdConvert + 1;
                    }
                }


                menuCd1 = string.Format("{0}{1}000000000000000", menuCd1, CreateId((subMenuIdConvert + 1)));
                lct_info.lct_cd = menuCd1;
                lct_info.mn_full = lct_nm;
                //lct_info.index_cd = "F1";
            }

            else
            {
                if (upLevel1 != null)
                {
                    lct_info.up_lct_cd = upLevel1.lct_cd;
                    var menuCd = string.Empty;
                    var subMenuIdConvert = 0;

                    var sublog = new lct_info();
                    switch (upLevel1.level_cd)
                    {
                        case "000":
                            menuCd = upLevel1.lct_cd.Substring(0, 3);
                            lct_info.level_cd = "001";
                            sublog = db.lct_info.ToList().Where(item => item.up_lct_cd == upLevel1.lct_cd).OrderByDescending(item => item.reg_dt).FirstOrDefault();

                            if (sublog != null)
                            {
                                var subMenuId = sublog.lct_cd.Substring(3, 3);
                                int.TryParse(subMenuId, out subMenuIdConvert);
                            }
                            menuCd = string.Format("{0}{1}000000000000", menuCd, CreateId((subMenuIdConvert + 1)));
                            lct_info.lct_cd = menuCd;
                            lct_info.mn_full = upLevel1.mn_full + ">" + lct_info.lct_nm;
                            var x = CreateId(subMenuIdConvert + 1);
                            //lct_info.index_cd = "F" + x.ToString();
                            break;

                        case "001":
                            menuCd = upLevel1.lct_cd.Substring(0, 6);
                            lct_info.level_cd = "002";
                            sublog = db.lct_info.ToList().Where(item => item.up_lct_cd == upLevel1.lct_cd).OrderByDescending(item => item.reg_dt).FirstOrDefault();

                            if (sublog != null)
                            {
                                var subMenuId = sublog.lct_cd.Substring(6, 3);
                                int.TryParse(subMenuId, out subMenuIdConvert);
                            }
                            menuCd = string.Format("{0}{1}000000000", menuCd, CreateId((subMenuIdConvert + 1)));
                            lct_info.lct_cd = menuCd;
                            lct_info.mn_full = upLevel1.mn_full + ">" + lct_info.lct_nm;
                            x = CreateId(subMenuIdConvert + 1);
                            //lct_info.index_cd = "F" + x.ToString();
                            break;
                        case "002":
                            menuCd = upLevel1.lct_cd.Substring(0, 9);
                            lct_info.level_cd = "003";
                            sublog = db.lct_info.ToList().Where(item => item.up_lct_cd == upLevel1.lct_cd).OrderByDescending(item => item.reg_dt).FirstOrDefault();

                            if (sublog != null)
                            {
                                var subMenuId = sublog.lct_cd.Substring(9, 3);
                                int.TryParse(subMenuId, out subMenuIdConvert);
                            }
                            menuCd = string.Format("{0}{1}000000", menuCd, CreateId((subMenuIdConvert + 1)));
                            lct_info.lct_cd = menuCd;
                            lct_info.mn_full = upLevel1.mn_full + ">" + lct_info.lct_nm;
                            x = CreateId(subMenuIdConvert + 1);
                            //lct_info.index_cd = "F" + x.ToString();
                            break;
                        case "003":
                            menuCd = upLevel1.lct_cd.Substring(0, 12);
                            lct_info.level_cd = "004";
                            sublog = db.lct_info.ToList().Where(item => item.up_lct_cd == upLevel1.lct_cd).OrderByDescending(item => item.reg_dt).FirstOrDefault();

                            if (sublog != null)
                            {
                                var subMenuId = sublog.lct_cd.Substring(12, 3);
                                int.TryParse(subMenuId, out subMenuIdConvert);
                            }
                            menuCd = string.Format("{0}{1}000", menuCd, CreateId((subMenuIdConvert + 1)));
                            lct_info.lct_cd = menuCd;
                            lct_info.mn_full = upLevel1.mn_full + ">" + lct_info.lct_nm;
                            x = CreateId(subMenuIdConvert + 1);
                            //lct_info.index_cd = "F" + x.ToString();
                            break;
                        default:
                            menuCd = upLevel1.lct_cd.Substring(0, 15);
                            if (sublog != null)
                            {
                                var subMenuId = sublog.lct_cd.Substring(15, 3);
                                int.TryParse(subMenuId, out subMenuIdConvert);
                            }
                            menuCd = string.Format("{0}{1}000", menuCd, CreateId((subMenuIdConvert + 1)));
                            lct_info.lct_cd = menuCd;
                            lct_info.mn_full = upLevel1.mn_full + ">" + lct_info.lct_nm;
                            x = CreateId(subMenuIdConvert + 1);
                            //lct_info.index_cd = "F" + x.ToString();
                            break;
                    }
                }

            }


            DateTime reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
            String dateString = reg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");

            DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
            String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
            lct_info.reg_dt = reg_dt;
            lct_info.chg_dt = chg_dt;
            lct_info.reg_id = Session["userid"].ToString();
            lct_info.chg_id = Session["userid"].ToString();
            lct_info.use_yn = use_yn;

            if (ModelState.IsValid)
            {
                db.Entry(lct_info).State = EntityState.Added;
                db.SaveChanges();
                return Json(lct_info, JsonRequestBehavior.AllowGet);
            }
            return View();
        }
        public ActionResult Searchloc_mgt(string lct_cd, string lct_nm)
        {
            var list = db.lct_info.Where(item => (item.lct_nm == "" || item.lct_nm.Contains(lct_nm)) && !(item.lct_cd.StartsWith("002")) && !(item.lct_cd.StartsWith("001")) && !(item.lct_cd.StartsWith("004")) && !(item.lct_cd.StartsWith("003")) && !(item.lct_cd.StartsWith("005")) && (lct_cd == "" || item.lct_cd.Contains(lct_cd))).
                  OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        protected string RenderRazorViewToString(string viewName, object model)
        {
            if (model != null)
            {
                ViewData.Model = model;
            }
            using (StringWriter sw = new StringWriter())
            {
                ViewEngineResult viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                ViewContext viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);

                return sw.GetStringBuilder().ToString();
            }
        }


        public ActionResult updatelocMgt(string lct_nm, string mv_yn, string re_mark, int lctno, string use_yn)
        {
            // Search user = userid 
            lct_info lct_info = db.lct_info.Find(lctno);
            lct_info.use_yn = use_yn;
            lct_info.mv_yn = mv_yn;
            var name_cu = lct_info.lct_nm.Length;
            var chuoi_full_name = lct_info.mn_full;
            var md = chuoi_full_name.Remove(chuoi_full_name.Length - name_cu);

            var full_name_new = md + lct_nm;
            lct_info.mn_full = full_name_new;
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
                return Json(lct_info, JsonRequestBehavior.AllowGet); ;
            }
            return View();
        }
        public ActionResult deletelocMgt(int lctno)
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
        #endregion

        #region NoticeMgt
        public ActionResult noticeRead(int bno)
        {
            notice_board notice_board = db.notice_board.Find(bno);
            if (notice_board == null)
            {
                return HttpNotFound();
            }
            return View(notice_board);
        }

        public ActionResult noticeInfoData()
        {
            return View();
        }

        public ActionResult Getnotice()
        {

            var list = (from a in db.notice_board
                            //where a.div_cd == "A"
                        select new
                        {
                            bno = a.bno,
                            mn_cd = a.mn_cd,
                            title = a.title,
                            reg_id = a.reg_id,
                            reg_dt = a.reg_dt,
                            chg_dt = a.chg_dt,
                            chg_id = a.chg_id,
                        }).OrderByDescending(x => x.bno).ToList();
            return Json(new { rows = list }, JsonRequestBehavior.AllowGet);

        }

        public JsonResult searchnoticeinfodata()
        {

            // Get Data from ajax function
            string searchType1 = Request.QueryString["searchType1Data"];
            string keywordInput1 = Request.QueryString["keywordInput1Data"];
            string start = Request.QueryString["start1Data"];
            string end = Request.QueryString["end1Data"];
            var html = "";
            if (searchType1 == "title") { html += "AND ('" + keywordInput1 + "'='' OR a.title like '%" + keywordInput1 + "%' )"; }
            if (searchType1 == "content") { html += " AND ('" + keywordInput1 + "'='' OR a.content like '%" + keywordInput1 + "%' )"; }
            if (searchType1 == "create_name") { html += " AND ('" + keywordInput1 + "'='' OR a.reg_id like '%" + keywordInput1 + "%' )"; }
            // Get data

            var sql = new StringBuilder();
            sql.Append(" SELECT * ")
            .Append("FROM  notice_board as a ")
            .Append("Where a.mn_cd is null and ('" + start + "'='' OR DATE_FORMAT(a.reg_dt,'%Y/%m/%d') >= DATE_FORMAT('" + start + "','%Y/%m/%d'))")
            .Append("AND ('" + end + "'='' OR DATE_FORMAT(a.reg_dt,'%Y/%m/%d') <= DATE_FORMAT('" + end + "','%Y/%m/%d'))  " + html + "");
            var data = db.notice_board.SqlQuery(sql.ToString()).ToList<notice_board>().AsEnumerable().Select(x => new
            {
                bno = x.bno,
                title = x.title,
                //content = item.content,
                reg_id = x.reg_id,
                reg_dt = Convert.ToString(x.reg_dt),
                chg_id = x.chg_id,
                chg_dt = Convert.ToString(x.chg_dt),
            }).ToList();
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult noticeRegister()
        {
            return View();
        }

        public ActionResult noticeModify(int bno)
        {
            notice_board notice_board = db.notice_board.Find(bno);
            if (notice_board == null)
            {
                return HttpNotFound();
            }
            return View(notice_board);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult updatenotice(int bno, string content, string title)
        {
            notice_board notice_board = db.notice_board.Find(bno);
            notice_board.title = title;
            notice_board.content = content;
            DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
            String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
            //notice_board.chg_dt = chg_dt;
            if (ModelState.IsValid)
            {
                db.Entry(notice_board).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("noticeInfoData");
            }
            return View(notice_board);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult insertnotice(notice_board notice_board)
        {
            if (ModelState.IsValid)
            {
                notice_board.chg_dt = DateTime.Now;
                notice_board.reg_dt = DateTime.Now;
                notice_board.reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                notice_board.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                notice_board.div_cd = "A";

                db.notice_board.Add(notice_board);
                db.SaveChanges();
                return RedirectToAction("noticeInfoData");
            }

            return View(notice_board);
        }

        #endregion

        #region Manu_Management
        public ActionResult Manual()
        {
            return SetLanguage("");
        }

        public ActionResult Getnotice_menu()
        {
            var list = (from a in db.notice_board
                        where a.mn_cd != null
                        select new
                        {
                            bno = a.bno,
                            mn_cd = a.mn_cd,
                            lng_cd = ((a.lng_cd == "EN") ? "English" : "Vietnamese"),
                            title = a.title,
                            reg_id = a.reg_id,
                            reg_dt = a.reg_dt,
                            chg_dt = a.chg_dt,
                            chg_id = a.chg_id,
                        }).ToList();
            return Json(new { rows = list }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ManualCreate()
        {
            return View();
        }

        public ActionResult insertnotice_menucheck(string mn_cd)
        {
            var ds = db.notice_board.Count(x => x.mn_cd == mn_cd);

            return Json(new { result = ds }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult check_exist_manual(string mn_cd, string lng_cd)
        {

            var ds = db.notice_board.Count(x => x.mn_cd == mn_cd && x.lng_cd == lng_cd);

            return Json(new { result = ds }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult insertnotice_manu(notice_board notice_board)
        {
            notice_board.div_cd = "B";
            notice_board.chg_dt = DateTime.Now;
            notice_board.reg_dt = DateTime.Now;
            notice_board.chg_id = Session["userid"].ToString();
            db.notice_board.Add(notice_board);
            db.SaveChanges();
            return RedirectToAction("Manual");
        }


        [HttpPost]
        [ValidateInput(false)]
        public ActionResult updatemanu(notice_board notice_board)
        {
            var item_count = db.notice_board.Count(x => x.mn_cd == notice_board.mn_cd && x.lng_cd == notice_board.lng_cd);
            if (item_count != 0)
            {
                var item = db.notice_board.FirstOrDefault(x => x.mn_cd == notice_board.mn_cd && x.lng_cd == notice_board.lng_cd);
                item.title = notice_board.title;
                item.mn_cd = notice_board.mn_cd;
                item.content = notice_board.content;
                DateTime chg_dt = DateTime.Now;
                item.chg_dt = DateTime.Now;
                item.chg_id = Session["userid"].ToString();
                db.SaveChanges();
                return RedirectToAction("Manual");
            }
            else
            {
                notice_board.div_cd = "B";
                notice_board.chg_dt = DateTime.Now;
                notice_board.reg_dt = DateTime.Now;
                notice_board.chg_id = Session["userid"].ToString();
                db.notice_board.Add(notice_board);
                db.SaveChanges();
                return RedirectToAction("Manual");
            }

        }

        public ActionResult Searchmanu(string title, string mn_cd, string lng_cd)
        {
            if (lng_cd == "All")
            {
                var ds = (from a in db.notice_board
                          where a.mn_cd != null && a.title.Contains(title) && a.mn_cd.Contains(mn_cd)
                          orderby a.chg_dt descending
                          select new
                          {
                              bno = a.bno,
                              lng_cd = ((a.lng_cd == "EN") ? "English" : "Vietnamese"),
                              mn_cd = a.mn_cd,
                              title = a.title,
                              reg_id = a.reg_id,
                              reg_dt = a.reg_dt,
                              chg_dt = a.chg_dt,
                              chg_id = a.chg_id,
                          }).ToList();
                return Json(ds, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var ds = (from a in db.notice_board
                          where a.mn_cd != null && a.lng_cd == lng_cd && a.title.Contains(title) && a.mn_cd.Contains(mn_cd)
                          orderby a.chg_dt descending
                          select new
                          {
                              bno = a.bno,
                              lng_cd = ((a.lng_cd == "EN") ? "English" : "Vietnamese"),
                              mn_cd = a.mn_cd,
                              title = a.title,
                              reg_id = a.reg_id,
                              reg_dt = a.reg_dt,
                              chg_dt = a.chg_dt,
                              chg_id = a.chg_id,
                          }).ToList();
                return Json(ds, JsonRequestBehavior.AllowGet);
            }


        }

        public ActionResult viewdetaillangue(int bno, string lng_cd)
        {
            notice_board a = db.notice_board.Find(bno);
            var notice_board = db.notice_board.Where(item => item.mn_cd == a.mn_cd && item.lng_cd == lng_cd).ToList();
            return Json(notice_board, JsonRequestBehavior.AllowGet);
        }

        public ActionResult viewdetailnotice(int bno)
        {

            var notice_board = db.notice_board.FirstOrDefault(item => item.bno == bno);
            return Json(new { mn_cd = notice_board.mn_cd, lng_cd = notice_board.lng_cd, title = notice_board.title, content = notice_board.content }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult viewdetail_notice(int bno)
        {

            var notice_board = db.notice_board.FirstOrDefault(item => item.bno == bno);
            return Json(notice_board, JsonRequestBehavior.AllowGet);
        }

        public ActionResult viewdetailmanual(string menu_nm, string lang_cd)
        {
            notice_board notice_board = new notice_board();
            var mn_cd = (db.menu_info.FirstOrDefault(x => x.mn_full == menu_nm) == null) ? "" : db.menu_info.FirstOrDefault(x => x.mn_full == menu_nm).mn_cd;
            notice_board = db.notice_board.FirstOrDefault(item => item.mn_cd == mn_cd && ((item.mn_cd != null) || (item.mn_cd != "")) && (item.lng_cd == lang_cd));
            return Json(notice_board, JsonRequestBehavior.AllowGet);
        }

        public ActionResult viewdetaildetailmanu(string mn_cd)
        {
            var notice_board = db.notice_board.Where(item => item.mn_cd == mn_cd).ToList();
            return Json(notice_board, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult deleteManual(int bno)
        {

            var count_item = db.notice_board.Count(x => x.bno == bno);
            if (count_item > 0)
            {
                var manual_item = db.notice_board.FirstOrDefault(x => x.bno == bno);
                db.notice_board.Remove(manual_item);
                db.SaveChanges();
            }

            return Json(new { result = count_item }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult updatemanu2(int bno, string content, string title)
        {
            notice_board notice_board = db.notice_board.Find(bno);
            notice_board.title = title;
            notice_board.content = content;
            DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
            String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
            //notice_board.chg_dt = chg_dt;
            if (ModelState.IsValid)
            {
                db.Entry(notice_board).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("noticeInfoData");
            }
            return View(notice_board);
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        public int keywordInput1 { get; set; }
    }
}
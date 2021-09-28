using System;
using System.Collections.Generic;
using System.Collections;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Mvc_VD.Models;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using System.Net.Http;
using System.Xml;
using Mvc_VD.Classes;
using System.Security.Cryptography;
using MySql.Data.MySqlClient;
using Mvc_VD.Services;

namespace Mvc_VD.Controllers
{
    public class HomeController : Controller
    {
        private Entities db = new Entities();
        private string KeyEncrypt = "AutoCompany@2021!@#$%^&*()_+";
        private readonly IUserService _iuser;
        private readonly IHomeService _ihomeService;

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            GetSetCookieLanguage();
            var lang = HttpContext.Request.Cookies.AllKeys.Contains("language")
                  ? HttpContext.Request.Cookies["language"].Value
                  : "en";
            var router = this.ControllerContext.RouteData.Values.Values.ElementAt(0).ToString();
            var result = _ihomeService.GetLanguage(lang, router);
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
        #region DashBoard
        public HomeController(IUserService iuser, IHomeService ihomeService)
        {
            _iuser = iuser;
            _ihomeService = ihomeService;
        }
        public ActionResult DashBoard()
        {
            HttpCookie cookie = HttpContext.Request.Cookies["language"];
            if (cookie != null)
            {
                ViewBag.language = cookie.Value;
            }
            return View();
        }

        public ActionResult getnoticeMgt()
        {
            var list = (from a in db.notice_board
                        where a.div_cd == "A"
                        select new
                        {
                            bno = a.bno,
                            mn_cd = a.mn_cd,
                            title = a.title,
                            reg_id = a.reg_id,
                            reg_dt = a.reg_dt,
                            chg_dt = a.chg_dt,
                            chg_id = a.chg_id,
                        }).OrderByDescending(x => x.bno).Take(10).ToList();
            return Json(new { rows = list }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult getMessageAll(mb_message mbm)
        {
            DateTime parsed;
            var data = db.mb_message.ToList().Select(x => new
            {
                tid = x.tid,
                message = x.message,
                reg_id = x.reg_id ?? "admin",
                reg_dt = DateTime.TryParse(x.reg_dt.ToString(), out parsed) ? parsed.ToString("yyyy-MM-dd HH:mm:ss") : " ",
            }).OrderByDescending(x => x.reg_dt).ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult insertMessage(mb_message mbm, string message)
        {
            mbm.message = message;

            DateTime reg_dt = DateTime.Now;
            mbm.reg_dt = reg_dt;
            mbm.reg_id = Session["userid"].ToString();
            mbm.del_yn = "N";

            if (ModelState.IsValid)
            {
                db.mb_message.Add(mbm);
                db.SaveChanges();
                DateTime parsed;
                var data = db.mb_message.ToList().Select(x => new
                {
                    message = x.message,
                    reg_id = x.reg_id ?? "Admin",
                    reg_dt = DateTime.TryParse(x.reg_dt.ToString(), out parsed) ? parsed.ToString("yyyy-MM-dd HH:mm:ss") : " ",
                }).OrderByDescending(x => x.reg_dt).ToList();

                //var data2 = data.Skip(data.Count() - 10).Take(10);
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            return View(mbm);
        }

        public ActionResult getNotice(int bno)
        {
            var data = db.notice_board.FirstOrDefault(x => x.bno == bno);
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        //-------------------------------

        #endregion DashBoard

        #region Session

        public ActionResult Index()
        {
            if (HttpContext.Request.Cookies["user"] != null && HttpContext.Request.Cookies["password"] != null)
            {
                try
                {
                    string cookieUser = HttpContext.Request.Cookies.Get("user").Value;
                    string cookiePassword = HttpContext.Request.Cookies.Get("password").Value;
                    return Login(cookieUser, cookiePassword, "on");
                }
                catch (Exception ex)
                {
                }
            }
            return View();
        }

        public ActionResult Registry()
        {
            return View();
        }

        public ActionResult ResultRegistry()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Registry([System.Web.Http.FromBody] string userid, [System.Web.Http.FromBody] string password, [System.Web.Http.FromBody] string password2, [System.Web.Http.FromBody] string uname, [System.Web.Http.FromBody] string cel_nb, [System.Web.Http.FromBody] string e_mail)
        {
            ViewBag.Message = "Registry";
            mb_info model_user = new mb_info();
            var result = db.mb_info.Count(x => x.userid == userid);

            if (password == password2)
            {
                if ((result == 0) && (userid != "") && (password != ""))
                {
                    model_user.userid = userid;
                    model_user.uname = uname;
                    model_user.nick_name = "";
                    model_user.upw = password;
                    model_user.grade = "";
                    model_user.tel_nb = "";
                    model_user.cel_nb = cel_nb;
                    model_user.e_mail = e_mail;
                    model_user.sms_yn = "";
                    model_user.mail_yn = "";
                    model_user.join_ip = "";
                    model_user.join_domain = "";
                    model_user.ltacc_dt = DateTime.Now;
                    model_user.ltacc_domain = "";
                    model_user.mbout_dt = DateTime.Now;
                    model_user.mbout_yn = "";
                    model_user.accblock_yn = "";
                    model_user.session_key = "";
                    model_user.session_limit = DateTime.Now;
                    model_user.memo = "";
                    model_user.del_yn = "";
                    model_user.check_yn = "";
                    model_user.rem_me = "";
                    model_user.mbjoin_dt = DateTime.Now;
                    model_user.scr_yn = "";
                    model_user.log_ip = "";
                    model_user.lct_cd = "";
                    model_user.reg_id = "";
                    model_user.reg_dt = DateTime.Now;
                    model_user.chg_id = "";
                    model_user.chg_dt = DateTime.Now;
                    model_user.re_mark = "";
                    db.mb_info.Add(model_user);
                    db.SaveChanges();
                    ViewBag.Result = "Registry is success";
                }
                else
                {
                    ViewBag.Result = "Registry is fail";
                }
            }
            else
            {
                ViewBag.Result = "Registry is fail : password unmatch";
            }

            return View("ResultRegistry");
        }

        [HttpPost]
        public ActionResult Login([System.Web.Http.FromBody] string userid, [System.Web.Http.FromBody] string password, [System.Web.Http.FromBody] string remmemberme)
        {

            string check_userid = userid;
            string check_password = password;
            string check_remmemberme = remmemberme;

            string checkroot_userid = "";
            string checkroot_password = "";
            if (string.IsNullOrEmpty(check_userid) || string.IsNullOrEmpty(check_password))
            {
                return RedirectToAction("Index", "Home");
            }
            //string path = Server.MapPath("/") + "InfoProject.xml";
            //DataSet dataSet = new DataSet();
            //DataTable dt = new DataTable();
            //dataSet.ReadXml(path);
            //dt = dataSet.Tables["rootuser"];
            //int i = 0;
            //foreach (DataRow dr in dt.Rows)
            //{
            //    checkroot_userid = dr["username"].ToString();
            //    checkroot_password = dr["password"].ToString();
            //    i++;
            //}

            if ((checkroot_userid == userid) && (checkroot_password == password))
            {
                #region Remember Password
                if (remmemberme == "on")
                {

                    //Xóa Cookie Lưu Thông Tin User cũ
                    HttpContext.Response.Cookies.Remove("user");
                    //Tạo Cookie Lưu Thông Tin mới
                    HttpCookie cookie = new HttpCookie("user");
                    cookie.Value = userid;
                    cookie.Expires = DateTime.Now.AddYears(1);
                    //Set Cookie Lưu thông Tin User
                    HttpContext.Response.SetCookie(cookie);
                    //Xóa Cookie Lưu Thông Tin User cũ
                    HttpContext.Response.Cookies.Remove("password");
                    //Tạo Cookie Lưu Thông Tin mới
                    cookie = new HttpCookie("password");
                    cookie.Value = password;
                    cookie.Expires = DateTime.Now.AddYears(1);
                    //Set Cookie Lưu thông Tin User
                    HttpContext.Response.SetCookie(cookie);
                }
                #endregion
                var MenuList = db.menu_info.Where(x => x.use_yn == "Y").OrderBy(x => x.mn_cd).ToList();
                Session["authorize"] = "logined";
                Session["userid"] = checkroot_userid;
                Session["authName"] = "Root";
                Session["authList"] = MenuList;
                Session["author_cha"] = MenuList;
                Session["role"] = "";
                return RedirectToAction("Dashboard", "Home");
            }
            else
            {
                //duy close 
                /*var result = db.mb_info.Count(x => x.userid == check_userid && x.upw == check_password);
   
                

                if (result > 0)
                {*/
                string result = _iuser.CheckLoginUser(check_userid, check_password);
                if (!string.IsNullOrEmpty(result))
                {
                    //duy close
                    /*int authDataCount = db.mb_author_info.Count(x => x.userid == check_userid);
                    if (authDataCount > 0)
                    {
                        var list = db.mb_author_info.Where(x => x.userid == check_userid).FirstOrDefault();
                        string authData = list.at_cd;*/
                    string authData = _iuser.GetAuthData(result);
                    GetSetCookieUserLogin(authData);
                    if (!string.IsNullOrEmpty(authData))
                    {
                        //if (db.author_info.Where(x => x.at_cd == authData).FirstOrDefault().role == "RL001")
                        BuyerLogin buyerLogin = _iuser.GetRoleFromAuthData(authData);
                        if (buyerLogin != null && buyerLogin.role == "RL001")
                        {
                            Session["role"] = "RL001";
                        }
                        else
                        {
                            Session["role"] = "";
                        }
                        //String varname2 = "";
                        //varname2 = varname2 + "CALL `Menu_Author`('" + authData + "') ";
                        //var data1 = db.Database.SqlQuery<menu>(varname2).ToList();
                        //IEnumerable<menu> listMenu = _iuser.GetListMenuByAuthData(authData);
                        IEnumerable<menu> listMenu = _iuser.GetListMenuByAuthData(authData, GetSetCookieLanguage().Value);
                        var check_wms = listMenu.Where(x => x.mn_cd == "012000000000" || x.mn_cd == "002000000000" || x.mn_cd == "009000000000" || x.mn_cd == "010000000000");
                        Session["wms"] = string.Join(",", check_wms.Select(x => x.mn_cd));
                        Session["authorize"] = "logined";
                        Session["userid"] = check_userid;
                        //Session["authName"] = db.author_info.FirstOrDefault(x => x.at_cd == authData).at_nm;
                        Session["authName"] = buyerLogin.at_nm;
                        Session["authList"] = listMenu.ToList();
                        Session["author_cha"] = listMenu.ToList();
                        //GetSetCookieUserLogin(authData);
                        if (authData == "015")
                        {
                            return RedirectToAction("Supplier_QR_Management", "Supplier");
                        }
                        //Session.Timeout = 500000;
                    }
                    return RedirectToAction("Dashboard", "Home");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
        }
        public HttpCookie GetSetCookieUserLogin(string usercode)
        {
            HttpCookie UserCookie = null;
            if (string.IsNullOrEmpty(usercode) == false)
            {
                if (HttpContext.Request.Cookies.AllKeys.Contains("usercode"))
                {
                    HttpContext.Request.Cookies.Remove("usercode");
                }
                UserCookie = new HttpCookie("usercode");
                DateTime now = DateTime.Now;
                UserCookie.Value = usercode;
                UserCookie.Expires = now.AddDays(1);
                UserCookie.HttpOnly = false;


                Response.Cookies.Add(UserCookie);
            }
            else
            {
                UserCookie = HttpContext.Request.Cookies.Get("usercode");
                Response.Cookies.Add(UserCookie);
            }
            return UserCookie;
        }
        public HttpCookie GetSetCookieLanguage()
        {
            HttpCookie languageCookie = null;
            if (HttpContext.Request.Cookies.Get("language") == null)
            {
                languageCookie = new HttpCookie("language");
                DateTime now = DateTime.Now;
                languageCookie.Value = Commons.language.en;
                languageCookie.Expires = now.AddDays(1);
                languageCookie.Secure = false;
                Response.Cookies.Add(languageCookie);
            }
            else
            {
                languageCookie = HttpContext.Request.Cookies.Get("language");
                Response.Cookies.Add(languageCookie);
            }
            return languageCookie;
        }
        public ActionResult checkFirstLogin()
        {
            var loginFirst = new { loginFirst = Session["loginFirst"] };
            if (Session["loginFirst"] == null)
            {
                Session["loginFirst"] = "N";
            }
            return Json(loginFirst, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Logout()
        {
            HttpCookie userInfo = new HttpCookie("userInfo");
            if (HttpContext.Request.Cookies["user"] != null && HttpContext.Request.Cookies["password"] != null)
            {
                Response.Cookies["user"].Expires = DateTime.Now.AddDays(-1);
                Response.Cookies["password"].Expires = DateTime.Now.AddDays(-1);
            }
            userInfo.Expires = DateTime.Now.AddHours(1);
            Response.Cookies.Add(userInfo);
            HttpContext.Request.Cookies.Clear();
            return RedirectToAction("Index", "Home");
        }

        public ActionResult GetNumNote(string mn_cd)
        {
            try
            {
                //note for WMS
                if (mn_cd == "013")
                {
                    var abc = db.w_sd_info.Count(x => x.alert == 2);

                    if (abc > 0)
                    {
                        return Json(new { result = true, data = abc }, JsonRequestBehavior.AllowGet);
                    }
                }
                //note for WIP
                if (mn_cd == "010")
                {
                    var abc = db.w_sd_info.Count(x => x.alert == 1);

                    if (abc > 0)
                    {
                        return Json(new { result = true, data = abc }, JsonRequestBehavior.AllowGet);
                    }
                }
                //note for FG
                if (mn_cd == "009")
                {
                    var abc = db.w_ext_info.Count(x => x.alert == 1);

                    if (abc > 0)
                    {
                        return Json(new { result = true, data = abc }, JsonRequestBehavior.AllowGet);
                    }
                }
                //note for TIMS
                if (mn_cd == "012")
                {
                    var abc = db.w_ext_info.Count(x => x.alert == 2);

                    if (abc > 0)
                    {
                        return Json(new { result = true, data = abc }, JsonRequestBehavior.AllowGet);
                    }
                }
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetNumNote_detail(string mn_cd)
        {
            try
            {
                //note for WIP
                if (mn_cd == "010")
                {
                    var data = (from a in db.w_sd_info
                                where a.alert == 1
                                orderby a.sd_no descending
                                select new
                                {
                                    id = a.sid,
                                    code = a.sd_no,
                                    name = a.sd_nm,
                                    link = "/wipwms/Receving_Scan_Wip",
                                    chg_dt = a.chg_dt
                                }
                        ).OrderByDescending(x => x.chg_dt).ToList();

                    if (data.Count > 0)
                    {
                        return Json(new { result = true, data }, JsonRequestBehavior.AllowGet);
                    }
                }
                //note for WMS
                if (mn_cd == "013")
                {
                    var data = (from a in db.w_sd_info
                                where a.alert == 2
                                orderby a.sd_no descending
                                select new
                                {
                                    id = a.sid,
                                    code = a.sd_no,
                                    name = a.sd_nm,
                                    link = "/ShippingMgt/ShippingPickingScan"
                                }
                        ).ToList();

                    if (data.Count > 0)
                    {
                        return Json(new { result = true, data }, JsonRequestBehavior.AllowGet);
                    }
                }
                //note for FG
                if (mn_cd == "009")
                {
                    var data = (from a in db.w_ext_info
                                where a.alert == 1
                                orderby a.ext_no descending
                                select new
                                {
                                    id = a.extid,
                                    code = a.ext_no,
                                    name = a.ext_nm,
                                    link = "/fgwms/Receving_Scan"
                                }
                        ).ToList();

                    if (data.Count > 0)
                    {
                        return Json(new { result = true, data }, JsonRequestBehavior.AllowGet);
                    }
                }
                //note for TIMS
                if (mn_cd == "012")
                {
                    var data = (from a in db.w_ext_info
                                where a.alert == 2
                                orderby a.ext_no descending
                                select new
                                {
                                    id = a.extid,
                                    code = a.ext_no,
                                    name = a.ext_nm,
                                    link = "/TIMS/Shipping_Scan"
                                }
                        ).ToList();

                    if (data.Count > 0)
                    {
                        return Json(new { result = true, data }, JsonRequestBehavior.AllowGet);
                    }
                }
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion Session

        #region Menu

        //public ActionResult GetSessionMenuData()
        //{
        //    try
        //    {
        //        if (Session["check_root"].ToString() == "ok")
        //        {
        //            var SessionData1 = new { userid = Session["userid"], authName = Session["authName"], authList = Session["authList"], authList_cha = Session["authList"], ds_author = Session["authList"], ds_KQ = 0 };
        //            return Json(SessionData1, JsonRequestBehavior.AllowGet);
        //        }

        //        var url = Request["url"];
        //        String varname1 = "";
        //        varname1 = varname1 + "SELECT c.* " + "\n";
        //        varname1 = varname1 + "FROM   author_menu_info AS a " + "\n";
        //        varname1 = varname1 + "       JOIN mb_author_info AS b " + "\n";
        //        varname1 = varname1 + "    on a.at_cd=b.at_cd and b.userid='" + Session["userid"] + "' " + "\n";
        //        varname1 = varname1 + "       join author_info as c on b.at_cd=c.at_cd  ";
        //        varname1 = varname1 + "       WHERE a.url_link = '" + url + "'";
        //        var data = db.Database.SqlQuery<author_info>(varname1).ToList();
        //        if (data.Count() == 1)
        //        {
        //            var role = data.FirstOrDefault().role;
        //            var at_cd = data.FirstOrDefault().at_cd;
        //            String varname2 = "";
        //            varname2 = varname2 + "select b.mn_cd,b.mn_nm,b.up_mn_cd,b.url_link from author_menu_info as a " + "\n";
        //            varname2 = varname2 + "join menu_info as b on concat(substring(a.mn_cd,1,3),'000000000') = b.mn_cd " + "\n";
        //            varname2 = varname2 + "where a.at_cd='"+ at_cd + "' " + "\n";
        //            varname2 = varname2 + "group by  substring(a.mn_cd,1,3) " + "\n";
        //            varname2 = varname2 + "union " + "\n";
        //            varname2 = varname2 + "select b.mn_cd,b.mn_nm,b.up_mn_cd,b.url_link from author_menu_info as a " + "\n";
        //            varname2 = varname2 + "join menu_info as b on concat(substring(a.mn_cd,1,6),'000000') = b.mn_cd " + "\n";
        //            varname2 = varname2 + "where a.at_cd='"+ at_cd + "' " + "\n";
        //            varname2 = varname2 + "group by b.mn_cd";
        //            var data1 = db.Database.SqlQuery<menu>(varname2).ToList();
        //            if (role == "RL001")
        //            {
        //                var ds_author = db.author_action.Where(x => x.url_link == url).ToList();
        //                var SessionData = new { userid = Session["userid"], authName = Session["authName"], authList = Session["authList"], authList_cha = data1, ds_author = ds_author };
        //                return Json(SessionData, JsonRequestBehavior.AllowGet);
        //            }
        //            var SessionData1 = new { userid = Session["userid"], authName = Session["authName"], authList = Session["authList"], authList_cha = data1,ds_author = 0, ds_KQ = 0 };
        //            return Json(SessionData1, JsonRequestBehavior.AllowGet);
        //        }

        //        return RedirectToAction("Index", "Home");
        //    }
        //    catch (Exception e)
        //    {
        //        return RedirectToAction("Index", "Home");
        //    }
        //}
        public ActionResult GetSessionMenuData()
        {
            try
            {
                var result = _iuser.GetListMenuByAuthData(GetSetCookieUserLogin("").Value, GetSetCookieLanguage().Value);
                Session["author_cha"] = result;
                Session["authlist"] = result;
                var url = Request["url"];
                if (Session["authName"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                var bien = (Session["authName"].ToString() == "Root" ? "true" : Session["wms"]);
                if (Session["role"].ToString() == "RL001")
                {
                    var ds_role = db.author_action.Where(x => x.url_link == url).ToList();
                    var SessionData = new { userid = Session["userid"], authName = Session["authName"], authList = Session["authList"], authList_cha = Session["authList"], ds_author = Session["authList"], ds_role = ds_role, wms = bien };
                    return Json(SessionData, JsonRequestBehavior.AllowGet);
                }

                var SessionData1 = new { userid = Session["userid"], authName = Session["authName"], authList = Session["authList"], authList_cha = Session["authList"], ds_author = Session["authList"], ds_role = 0, wms = bien };
                return Json(SessionData1, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return RedirectToAction("Index", "Home");
            }
        }

        #endregion Menu

        #region API_LOGIN

        public ActionResult API_Login(string user, string password, string type)
        {
            //string check_userid = user;
            //string check_password = password;

            //String varname1 = "";
            //varname1 = varname1 + "select d.* from " + "\n";
            //varname1 = varname1 + "mb_info as a join " + "\n";
            //varname1 = varname1 + "mb_author_info as b " + "\n";
            //varname1 = varname1 + "on a.userid=b.userid and a.userid='" + check_userid + "' and a.upw='" + check_password + "' " + "\n";
            //varname1 = varname1 + "join author_menu_info as c " + "\n";
            //varname1 = varname1 + "join menu_info as d on c.mn_cd=d.mn_cd and c.at_cd=b.at_cd " + "\n";
            //varname1 = varname1 + "where d.mn_full like '%" + type + "%'";
            //var result = db.Database.SqlQuery<menu_info>(varname1).ToList();

            string sqlquery = @"SELECT COUNT(a.userid)
            FROM mb_info a
            join mb_author_info b ON a.userid =b.userid
            JOIN author_menu_info as c ON c.at_cd = b.at_cd
            JOIN menu_info d on c.mn_cd=d.mn_cd and c.at_cd=b.at_cd
            WHERE a.userid = @1 AND a.upw =@2 AND d.mn_full LIKE @3";

            int result = db.Database.SqlQuery<int>(sqlquery, new MySqlParameter("@1", user), new MySqlParameter("@2", password), new MySqlParameter("@3", "%" + type + "%")).FirstOrDefault();

            if (result > 0)
            {
                var a = new { result = true };
                return Json(a, JsonRequestBehavior.AllowGet); ;
            }
            else
            {
                var b = new { result = false };
                return Json(b, JsonRequestBehavior.AllowGet); ;
            }
        }

        #endregion API_LOGIN

        #region API_APP_Version
        public ActionResult API_APP_Version(int phienban, string name_app)
        {

            //var get_ds_new = db.version_app.Where(x => x.type == name_app).OrderByDescending(x => x.version).ToList();
            bool kqres = false;
            string messres = "";
            bool vernew = false;
            var get_ds_new = _ihomeService.GetVersionApp(name_app);
            if (get_ds_new != null)
            {
                kqres = true;
                if (get_ds_new.version > phienban)
                {
                    vernew = true;
                    messres = " Đang tồn tại phiên bản mới, vui lòng cập nhật để tiếp tục!";
                    //return Json(new { result = true, message = "Đang tồn tại phiên bản mới bạn có muốn update nó không?", kq = get_ds_new }, JsonRequestBehavior.AllowGet);
                }

            }
            else
            {
                messres = "Hiện tại APP chưa tồn tại!";
                //return Json(new { result = false, message = "Không tồn tại phiên bản nào!!" }, JsonRequestBehavior.AllowGet);
            }
            var jsonReturn = new
            {
                result = kqres,
                versionnew = vernew,
                message = messres,
                data = get_ds_new
            };
            return Json(jsonReturn, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Add_Version(int phienban, string name_file, string name_app, version_app version_app)
        {
            var get_ds_new = db.version_app.Where(x => x.type == name_app).OrderBy(x => x.version).ToList();
            if (get_ds_new.Count < 2)
            {
                version_app.version = phienban;
                version_app.type = name_app;
                db.Entry(version_app).State = EntityState.Added;
                db.SaveChanges();
            }
            else
            {
                //xóa cái đầu tiên 
                var xoa = get_ds_new.FirstOrDefault();
                db.Entry(xoa).State = EntityState.Deleted;
                db.SaveChanges();


                //insert cái tiếp theo
                version_app.version = phienban;
                version_app.type = name_app;
                db.Entry(version_app).State = EntityState.Added;
                db.SaveChanges();
            }
            return Json(new { result = true }, JsonRequestBehavior.AllowGet);
        }



        #endregion
        #region EncryptData

        /// <summary>
        /// Hàm mã hóa chuỗi với key
        /// </summary>
        /// <param name="source"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Encrypt(string source, string key)
        {
            using (TripleDESCryptoServiceProvider tripleDESCryptoService = new TripleDESCryptoServiceProvider())
            {
                using (MD5CryptoServiceProvider hashMD5Provider = new MD5CryptoServiceProvider())
                {
                    byte[] byteHash = hashMD5Provider.ComputeHash(Encoding.UTF8.GetBytes(key));
                    tripleDESCryptoService.Key = byteHash;
                    tripleDESCryptoService.Mode = CipherMode.ECB;
                    byte[] data = Encoding.UTF8.GetBytes(source);
                    return Convert.ToBase64String(tripleDESCryptoService.CreateEncryptor().TransformFinalBlock(data, 0, data.Length));
                }
            }
        }
        /// <summary>
        /// Hàm giải mã chuỗi với key
        /// </summary>
        /// <param name="encrypt"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Decrypt(string encrypt, string key)
        {
            using (TripleDESCryptoServiceProvider tripleDESCryptoService = new TripleDESCryptoServiceProvider())
            {
                using (MD5CryptoServiceProvider hashMD5Provider = new MD5CryptoServiceProvider())
                {
                    byte[] byteHash = hashMD5Provider.ComputeHash(Encoding.UTF8.GetBytes(key));
                    tripleDESCryptoService.Key = byteHash;
                    tripleDESCryptoService.Mode = CipherMode.ECB;
                    byte[] data = Convert.FromBase64String(encrypt);
                    byte[] test = tripleDESCryptoService.CreateDecryptor().TransformFinalBlock(data, 0, data.Length);
                    return Encoding.UTF8.GetString(test);
                }
            }
        }
        public string Base64(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/').PadRight(4 * ((s.Length + 3) / 4), '=');
            return s;
        }
        #endregion
    }
}
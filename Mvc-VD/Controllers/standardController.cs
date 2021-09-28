using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Mvc_VD.Models;
using System.Collections;
using System.Text;
using System.IO;
using Mvc_VD.Models.Language;

namespace Mvc_VD.Controllers
{
    public class standardController : BaseController
    {
        private Entities db = new Entities();

        //... Supplier INFO
        #region Supplier
        public JsonResult deletesupllier( int id ) {
            int count = db.supplier_info.Where(x => x.spno == id).ToList().Count();
            var result = new { result = true };
            if (count > 0) {
                supplier_info supplier_info = db.supplier_info.Find(id);
                db.supplier_info.Remove(supplier_info);
                db.SaveChanges();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            result = new { result = false };
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public ActionResult supplierInfo()
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
        public ActionResult a() {
            return View();
        }

        public JsonResult GetAllCountries() {

            var data = db.comm_dt.Where(x => x.mt_cd == "COM001" && x.use_yn == "Y").OrderBy(x => x.dt_nm).ToList();
            return Json(data, JsonRequestBehavior.AllowGet);

        }
        public ActionResult selectSupplierInfo() {
            var vaule = (from a in db.supplier_info
                         orderby a.chg_dt descending
                         select new {
                             spno = a.spno,
                             sp_cd = a.sp_cd,
                             sp_nm = a.sp_nm,
                             bsn_tp = a.bsn_tp,
                             changer_id = a.changer_id,
                             phone_nb = a.phone_nb,
                             cell_nb = a.cell_nb,
                             fax_nb = a.fax_nb,
                             e_mail = a.e_mail,
                             web_site = a.web_site,
                             address = a.address,
                             re_mark = a.re_mark,
                             bsn_tp1 = (db.comm_dt.Where(x => x.dt_cd == a.bsn_tp && x.mt_cd == "COM001").Select(x => x.dt_nm)),
                         }).ToList();
            var jsonDataComDt = new { rows = vaule };
            return Json(jsonDataComDt, JsonRequestBehavior.AllowGet);

        }

        public ActionResult insertSupplierInfo( supplier_info supplier_info, int spno, string sp_cd, string phone_nb, string e_mail, string web_site, string address, string re_mark, string fax_nb, string cell_nb, string changer_id, string bsn_tp ) {
            var a = db.supplier_info.Count(x => x.sp_cd == sp_cd);
            var result = new { result = a };
            if (a == 0) {
                supplier_info.bsn_tp = bsn_tp;
                supplier_info.changer_id = changer_id;
                supplier_info.cell_nb = cell_nb;
                supplier_info.fax_nb = fax_nb;
                supplier_info.spno = spno;
                supplier_info.sp_cd = sp_cd.ToUpper();
                supplier_info.phone_nb = phone_nb;
                supplier_info.e_mail = e_mail;
                supplier_info.web_site = web_site;
                supplier_info.address = address;
                supplier_info.re_mark = re_mark;
                supplier_info.use_yn = "Y";
                supplier_info.del_yn = "N";
                supplier_info.changer_id = Session["authName"] == null ? null : Session["authName"].ToString();
                supplier_info.reg_id = Session["authName"] == null ? null : Session["authName"].ToString();
                supplier_info.chg_id = Session["authName"] == null ? null : Session["authName"].ToString();

                DateTime reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                String dateString = reg_dt.ToString("yyyy-MM-dd HH:mm:ss");

                DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss");
                supplier_info.reg_dt = reg_dt;
                supplier_info.chg_dt = chg_dt;

                db.Entry(supplier_info).State = EntityState.Added;
                db.SaveChanges();

                //insert user
                mb_info mb_info = new mb_info();
                var ds_user = db.mb_info.Count(x => x.userid == sp_cd);
                if ((ds_user == 0))
                {
                    mb_info.userid = sp_cd.ToUpper();
                    mb_info.uname = supplier_info.sp_nm;
                    mb_info.nick_name = supplier_info.sp_nm;
                    mb_info.upw = sp_cd.ToUpper();
                    mb_info.grade = "SUPPLIER";
                    mb_info.tel_nb = supplier_info.phone_nb;
                    mb_info.e_mail = supplier_info.e_mail;
                    mb_info.cel_nb = supplier_info.cell_nb;
                    
                    mb_info.mail_yn = "N";
                    mb_info.ltacc_dt = DateTime.Now;
                    mb_info.mbout_dt = DateTime.Now;
                    mb_info.session_limit = DateTime.Now;
                
                    mb_info.mbjoin_dt = DateTime.Now;
        
                    mb_info.reg_dt = DateTime.Now;
                    mb_info.chg_dt = DateTime.Now;
                    db.mb_info.Add(mb_info);
                    db.SaveChanges();

                    mb_author_info mb_author_info = new mb_author_info();
                    mb_author_info.userid = sp_cd.ToLower();
                    mb_author_info.at_cd = db.author_info.Where(x => x.at_nm == mb_info.grade).FirstOrDefault().at_cd == null ? "" : db.author_info.Where(x => x.at_nm == mb_info.grade).FirstOrDefault().at_cd;
                    mb_author_info.reg_dt = DateTime.Now;
                    mb_author_info.chg_dt = DateTime.Now;
                    db.mb_author_info.Add(mb_author_info);
                    db.SaveChanges();
                   }
                var vaule = (from m in db.supplier_info
                             orderby m.chg_dt descending
                             where m.spno == supplier_info.spno
                             select new {
                                 spno = m.spno,
                                 sp_cd = m.sp_cd,
                                 sp_nm = m.sp_nm,
                                 bsn_tp = m.bsn_tp,
                                 changer_id = m.changer_id,
                                 phone_nb = m.phone_nb,
                                 cell_nb = m.cell_nb,
                                 fax_nb = m.fax_nb,
                                 e_mail = m.e_mail,
                                 web_site = m.web_site,
                                 address = m.address,
                                 re_mark = m.re_mark,
                                 bsn_tp1 = (db.comm_dt.Where(x => x.dt_cd == m.bsn_tp && x.mt_cd == "COM001").Select(x => x.dt_nm)),
                             }).FirstOrDefault();

                return Json(new { result = true, vaule }, JsonRequestBehavior.AllowGet);
            };
            return Json(new { result = false }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult updateSupplierInfo( supplier_info supplier_info, int spno, string sp_cd, string phone_nb, string e_mail, string web_site, string address, string re_mark, string chg_id, string fax_nb, string cell_nb, string changer_id, string bsn_tp ) {
            supplier_info.bsn_tp = bsn_tp;
            supplier_info.changer_id = changer_id;
            supplier_info.cell_nb = cell_nb;
            supplier_info.fax_nb = fax_nb;
            supplier_info.spno = spno;
            supplier_info.sp_cd = sp_cd;
            supplier_info.phone_nb = phone_nb;
            supplier_info.e_mail = e_mail;
            supplier_info.web_site = web_site;
            supplier_info.address = address;
            supplier_info.re_mark = re_mark;
            supplier_info.use_yn = "Y";
            supplier_info.del_yn = "N";

            //if (changer_id == "")
            //{
            //    var username = Session["authName"];
            //    supplier_info.changer_id = username.ToString();
            //}
            supplier_info.changer_id = Session["authName"] == null ? null : Session["authName"].ToString();
            supplier_info.chg_id = Session["authName"] == null ? null : Session["authName"].ToString();

            DateTime reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
            String dateString = reg_dt.ToString("yyyy-MM-dd HH:mm:ss");
            supplier_info.reg_dt = reg_dt;

            DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
            String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss");

            //supplier_info.chg_dt = chg_dt;

            if (ModelState.IsValid) {
                db.Entry(supplier_info).State = EntityState.Modified;
                db.SaveChanges();
                var value = (from m in db.supplier_info
                             orderby m.chg_dt descending
                             where m.spno == supplier_info.spno
                             select new {
                                 spno = m.spno,
                                 sp_cd = m.sp_cd,
                                 sp_nm = m.sp_nm,
                                 bsn_tp = m.bsn_tp,
                                 changer_id = m.changer_id,
                                 phone_nb = m.phone_nb,
                                 cell_nb = m.cell_nb,
                                 fax_nb = m.fax_nb,
                                 e_mail = m.e_mail,
                                 web_site = m.web_site,
                                 address = m.address,
                                 re_mark = m.re_mark,
                                 bsn_tp1 = (db.comm_dt.Where(x => x.dt_cd == m.bsn_tp && x.mt_cd == "COM001").Select(x => x.dt_nm)),
                             }).FirstOrDefault();

                return Json(new { result = true, value }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { result = false }, JsonRequestBehavior.AllowGet);


        }

        [HttpPost]
        public JsonResult deleteSupplier( int spno ) {

            int count = db.supplier_info.Count(x => x.spno == spno);
            var result = new { result = count };
            if (count > 0) {
                var supplier_data = db.supplier_info.FirstOrDefault(x => x.spno == spno);
                db.supplier_info.Remove(supplier_data);
                db.SaveChanges();
            }
            return Json(result, JsonRequestBehavior.AllowGet);


        }

        public JsonResult SearchsupplierInfo() {

            var code = Request["codeData"];
            var name = Request["nameData"];
            var bsn_search = Request["bsn_searchData"];
            var sql = new StringBuilder();
            sql.Append(" SELECT si.* ")
                .Append("FROM  supplier_info as si ")
                .Append("INNER JOIN comm_dt ON ")
                .Append("( comm_dt.dt_cd= si.bsn_tp )")
                .Append("WHERE comm_dt.mt_cd='COM001' ")
                .Append("AND ('" + code + "'='' OR  si.sp_cd like '%" + code + "%' )")
                .Append("AND ('" + name + "'='' OR  si.sp_nm like '%" + name + "%' )")
                .Append("AND ('" + bsn_search + "'='' OR  comm_dt.dt_cd = '" + bsn_search + "')");
            var data = db.supplier_info.SqlQuery(sql.ToString()).OrderByDescending(x => x.chg_dt).ToList<supplier_info>();
            foreach (var item in data) {
                var conndition = "SELECT * FROM comm_dt WHERE dt_cd='" + item.bsn_tp + "'";

                var com = db.comm_dt.SqlQuery(conndition).FirstOrDefault();

                item.bsn_tp = ((com != null) ? com.dt_nm : string.Empty);
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Buyer
        public ActionResult buyerInfo() 
        {
            return SetLanguage("");
        }
        // GET: /standarinfo/buyerInfo
        public JsonResult GetBuyerInfo() {
            var buyer_list = db.buyer_info.OrderByDescending(x => x.chg_dt).Select(x => new {
                byno = x.byno,
                buyer_cd = x.buyer_cd,
                buyer_nm = x.buyer_nm,
                manager_nm = x.manager_nm,
                ceo_nm = x.ceo_nm,
                brd_nm = x.brd_nm,
                logo = x.logo,
                logoShow = x.logo,
                phone_nb = x.phone_nb,
                cell_nb = x.cell_nb,
                fax_nb = x.fax_nb,
                e_mail = x.e_mail,
                address = x.address,
                web_site = x.web_site,
                re_mark = x.re_mark,
                use_yn = x.use_yn,
                del_yn = x.del_yn,
                reg_id = x.reg_id,
                reg_dt = x.reg_dt,
                chg_id = x.chg_id,
                chg_dt = x.chg_dt
            }).ToList();
            return Json(new { rows = buyer_list }, JsonRequestBehavior.AllowGet);
        }  // GET: /standarinfo/GetBuyerInfo

        [HttpPost]
        public ActionResult InsertBuyer( buyer_info buyer_info, string buyer_cd, HttpPostedFileBase file ) {

            buyer_info.reg_dt = DateTime.Now;
            buyer_info.chg_dt = DateTime.Now;
            buyer_info.del_yn = "N";
            buyer_info.buyer_cd = buyer_cd.ToUpper();

            var profile = Request.Files;
            string imgname = string.Empty;
            string ImageName = string.Empty;

            var check_data = db.buyer_info.Count(x => x.buyer_cd == buyer_cd);
            var result = new { result = check_data };
            if (check_data == 0) {
                if (System.Web.HttpContext.Current.Request.Files.AllKeys.Any()) {
                    var logo1 = System.Web.HttpContext.Current.Request.Files["file"];
                    if (logo1.ContentLength > 0) {
                        var profileName = Path.GetFileName(logo1.FileName);
                        var ext = Path.GetExtension(logo1.FileName);

                        ImageName = profileName;
                        var comPath = Server.MapPath("~/Images/BuyerImg/") + ImageName;

                        logo1.SaveAs(comPath);
                        buyer_info.logo = ImageName;
                    }
                }

                if (ModelState.IsValid) //check what type of extension  
                    {
                    db.Entry(buyer_info).State = EntityState.Added;
                    db.SaveChanges(); // line that threw exception
                    return Json(result, JsonRequestBehavior.AllowGet);
                }
            }
            else {
                return Json(result, JsonRequestBehavior.AllowGet);
            }

            return View("GetBuyerInfo");
        }//insert buyer_info

        public ActionResult Editbuyer( int byno, string buyer_cd, string buyer_nm, string brd_nm, string web_site, string cell_nb, string address, string phone_nb, string e_mail, string fax_nb, string use_yn, string re_mark, string logo ) {
            buyer_info buyer_info = db.buyer_info.Find(byno);
            var countBuyer = db.buyer_info.Count(x => x.buyer_cd == buyer_cd);

            buyer_info.buyer_cd = buyer_cd;
            buyer_info.buyer_nm = buyer_nm;
            buyer_info.brd_nm = brd_nm;
            buyer_info.web_site = web_site;
            buyer_info.cell_nb = cell_nb;
            buyer_info.address = address;
            buyer_info.phone_nb = phone_nb;
            buyer_info.e_mail = e_mail;
            buyer_info.fax_nb = fax_nb;
            buyer_info.use_yn = use_yn;
            buyer_info.re_mark = re_mark;
            //buyer_info.chg_dt = DateTime.Now;
            buyer_info.del_yn = "N";

            var profile = Request.Files;
            string imgname = string.Empty;
            string ImageName = string.Empty;

            if (System.Web.HttpContext.Current.Request.Files.AllKeys.Any()) {
                var logo1 = System.Web.HttpContext.Current.Request.Files["file"];
                if (logo1.ContentLength > 0) {
                    var profileName = Path.GetFileName(logo1.FileName);
                    var ext = Path.GetExtension(logo1.FileName);

                    ImageName = profileName;
                    var comPath = Server.MapPath("~/Images/BuyerImg/") + ImageName;

                    logo1.SaveAs(comPath);
                    buyer_info.logo = ImageName;
                }
            }

            if (ModelState.IsValid) //check what type of extension  
                {
                db.Entry(buyer_info).State = EntityState.Modified;
                db.SaveChanges(); // line that threw exception
                var result = new { result = countBuyer, imgname = ImageName };
                return Json(result, JsonRequestBehavior.AllowGet);

            }

            return View("GetBuyerInfo");
        }//update buyer_info

        public JsonResult searchBuyer( string buyer_cd, string buyer_nm ) {

            string buyer_cd_search = buyer_cd;
            string buyer_nm_search = buyer_nm;
            var sql = new StringBuilder();
            sql.Append("SELECT si.* ")
               .Append("FROM  buyer_info as si ")
               .Append("WHERE ('" + buyer_cd_search + "'='' OR   si.buyer_cd like '" + buyer_cd_search + "' ) ")
               .Append("AND ('" + buyer_nm_search + "'='' OR   si.buyer_nm like '" + buyer_nm_search + "' )");
            var data = db.buyer_info.SqlQuery(sql.ToString()).ToList();
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult deleteBuyer( int m_byno ) {

            int count = db.buyer_info.Where(x => x.byno == m_byno).ToList().Count();
            var result = new { result = count };
            if (count > 0) {
                var buyer_info_data = db.buyer_info.Find(m_byno);
                db.buyer_info.Remove(buyer_info_data);
                db.SaveChanges();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            result = new { result = count };
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region BussinessType

        public ActionResult BusinessTypeInfor()
        {
            try {
                return SetLanguage("");
            }
            catch (Exception ex) {

                throw ex;
            }

        }

        public ActionResult getBusInforData( comm_dt comm_dt, comm_mt comm_mt, string mt_cd ) {
            try {

                var vaule = (from comm_dt_ in db.comm_dt
                             join comm_mt_ in db.comm_mt
                             on comm_dt_.mt_cd equals comm_mt_.mt_cd
                             where comm_mt_.mt_cd == mt_cd
                             select new {
                                 cdid = comm_dt_.cdid,
                                 dt_cd = comm_dt_.dt_cd,
                                 dt_nm = comm_dt_.dt_nm,
                                 dt_exp = comm_dt_.dt_exp,
                                 use_yn = comm_dt_.use_yn,
                                 chg_dt = comm_dt_.chg_dt
                             }).OrderByDescending(x => x.chg_dt).ToList();
                var businessTypeInforData = new {

                    rows = vaule
                };
                return Json(businessTypeInforData, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex) {

                throw ex;
            }

        }

        public ActionResult insertBusInfor( comm_dt comm_dt, string dt_cd, string dt_nm, string use_yn, string dt_exp ) {
            try {

                int count = db.comm_dt.Where(item => item.dt_cd == dt_cd && item.mt_cd == "COM001").ToList().Count();
                var result = new { result = count };
                if (count > 0) {
                    return View(comm_dt);
                }
                else {
                    DateTime reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                    String dateString = reg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                    DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format 
                    String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                    //String dpno = Int32.TryParse(Convert.ToString(Request.Form["dpno"]));

                    //String reg_dt = DateTime.Now.ToString().Trim();
                    comm_dt.dt_cd = dt_cd.ToUpper();
                    comm_dt.dt_nm = dt_nm;
                    comm_dt.use_yn = use_yn;
                    comm_dt.dt_exp = dt_exp;
                    comm_dt.reg_dt = reg_dt;
                    comm_dt.chg_dt = chg_dt;
                    comm_dt.del_yn = "N";
                    comm_dt.dt_order = 1;
                    comm_dt.mt_cd = "COM001";
                    if (ModelState.IsValid) {
                        db.Entry(comm_dt).State = EntityState.Added;
                        db.SaveChanges(); // line that threw exception 

                        return Json(result, JsonRequestBehavior.AllowGet);
                    }

                    return Json(result, JsonRequestBehavior.AllowGet);
                }
            }


            catch (Exception ex) {

                throw ex;
            }

        }

        public ActionResult updateBusInfo( int cdid, string dt_cd, string dt_nm, string use_yn, string dt_exp ) {
            try {
                comm_dt comm_dt = db.comm_dt.Find(cdid);
                var count = db.comm_dt.Where(item => item.dt_cd == dt_cd && item.mt_cd == "COM001").ToList().Count();
                var result = new { result = count };
                DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                comm_dt.dt_cd = dt_cd.ToUpper();
                comm_dt.dt_nm = dt_nm;
                comm_dt.use_yn = use_yn;
                comm_dt.dt_exp = dt_exp;
                //comm_dt.chg_dt = chg_dt;
                if (ModelState.IsValid) {
                    db.Entry(comm_dt).State = EntityState.Modified;
                    db.SaveChanges(); // line that threw exception

                    return Json(result, JsonRequestBehavior.AllowGet);
                }
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex) {

                throw ex;
            }

        }

        public JsonResult searchBusInfo( string dt_cd, string dt_nm ) {

            string dt_cd_search = dt_cd;
            string dt_nm_search = dt_nm;
            var sql = new StringBuilder();
            sql.Append("SELECT si.* ")
               .Append("FROM  comm_dt as si ")
               .Append("WHERE si.mt_cd='COM001' ")
               .Append("AND ('" + dt_cd_search + "'='' OR   si.dt_cd like '" + dt_cd_search + "' )")
               .Append("AND ('" + dt_nm_search + "'='' OR   si.dt_nm like '" + dt_nm_search + "' )");
            var data = db.comm_dt.SqlQuery(sql.ToString()).ToList<comm_dt>();
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult deleteBus( int cdid ) {

            int count = db.comm_dt.Where(x => x.cdid == cdid).ToList().Count();
            var result = new { result = true };
            if (count > 0) {
                comm_dt comm_dt = db.comm_dt.Find(cdid);
                db.comm_dt.Remove(comm_dt);
                db.SaveChanges();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            result = new { result = false };
            return Json(result, JsonRequestBehavior.AllowGet);


        }

        #endregion

        #region Manufact
        public ActionResult ManufacInfo() {
            return SetLanguage("");
        }



        public ActionResult getManufacInfoData() {
            var mfInfo = db.manufac_info.OrderByDescending(x => x.chg_dt).ToList<manufac_info>();
            return Json(new { rows = mfInfo }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult insertManufacInfo( manufac_info manufac_info, string mf_cd, HttpPostedFileBase file ) {
            manufac_info.reg_dt = DateTime.Now;
            manufac_info.chg_dt = DateTime.Now;
            manufac_info.del_yn = "N";
            manufac_info.mf_cd = mf_cd.ToUpper();

            var profile = Request.Files;
            string imgname = string.Empty;
            string ImageName = string.Empty;
            var countManufactInfo = db.manufac_info.Count(x => x.mf_cd == mf_cd.ToUpper());
            if (countManufactInfo == 0) {
                if (System.Web.HttpContext.Current.Request.Files.AllKeys.Any()) {
                    var logo1 = System.Web.HttpContext.Current.Request.Files["file"];
                    if (logo1.ContentLength > 0) {
                        var profileName = Path.GetFileName(logo1.FileName);
                        var ext = Path.GetExtension(logo1.FileName);

                        ImageName = profileName;
                        var comPath = Server.MapPath("~/Images/ManufactImg/") + ImageName;

                        logo1.SaveAs(comPath);


                        manufac_info.logo = ImageName;
                    }
                }
                else
                    manufac_info.logo = "logo.png";

                if (ModelState.IsValid) //check what type of extension  
                    {
                    db.Entry(manufac_info).State = EntityState.Added;
                    db.SaveChanges(); // line that threw exception
                    var result = new { result = countManufactInfo };
                    return Json(result, JsonRequestBehavior.AllowGet);
                }
            }
            else {
                var result = new { result = countManufactInfo };
                return Json(result, JsonRequestBehavior.AllowGet);
            }

            return View(manufac_info);
        }


        public ActionResult updateManufacInfo( int mfno, string mf_cd, string mf_nm, string brd_nm, string web_site, string address,
            string phone_nb, string re_mark, string logo ) {
            var profile = Request.Files;
            string imgname = string.Empty;
            string ImageName = string.Empty;
            var countManufact = db.manufac_info.Count(x => x.mf_cd == mf_cd);
            var manufac_info = db.manufac_info.FirstOrDefault(x => x.mf_cd == mf_cd);
            if (countManufact > 0) {
                manufac_info.mf_nm = mf_nm;
                manufac_info.brd_nm = brd_nm;
                manufac_info.web_site = web_site;
                manufac_info.address = address;
                manufac_info.phone_nb = phone_nb;
                manufac_info.re_mark = re_mark;
                //manufac_info.chg_dt = DateTime.Now;



                if (System.Web.HttpContext.Current.Request.Files.AllKeys.Any()) {
                    var logo1 = System.Web.HttpContext.Current.Request.Files["file"];
                    if (logo1.ContentLength > 0) {
                        var profileName = Path.GetFileName(logo1.FileName);
                        var ext = Path.GetExtension(logo1.FileName);

                        ImageName = profileName;
                        var comPath = Server.MapPath("~/Images/ManufactImg/") + ImageName;

                        logo1.SaveAs(comPath);
                        manufac_info.logo = ImageName;
                    }
                    else
                        manufac_info.logo = Server.MapPath("/Images/ManufactImg/") + "logo.png";
                }
                else {
                    manufac_info.logo = logo;
                }

                if (ModelState.IsValid) //check what type of extension  
                    {
                    db.Entry(manufac_info).State = EntityState.Modified;
                    db.SaveChanges(); // line that threw exception
                    var result = new { result = countManufact, imgname = ImageName };
                    return Json(result, JsonRequestBehavior.AllowGet);

                }
            }
            else {
                var result = new { result = countManufact, imgname = ImageName };
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            return View(manufac_info);
        }


        public JsonResult searchManufacInfo() {
            // Get Data from ajax function

            string code = Request.QueryString["mf_cd"];
            string name = Request.QueryString["mf_nm"];
            string brd_nm = Request.QueryString["brd_nm"];
            var sql = new StringBuilder();
            sql.Append(" SELECT mfi.* ")
                .Append("FROM  manufac_info as mfi ")
                 .Append("WHERE ('" + code + "'='' OR   mfi.mf_cd like '%" + code + "%' )")
                 .Append("AND ('" + name + "'='' OR   mfi.mf_nm like '%" + name + "%' )")
                 .Append("AND ('" + brd_nm + "'='' OR   mfi.brd_nm like '%" + brd_nm + "%' )")
                             .Append("ORDER BY mfi.chg_dt DESC ");
            var data = db.manufac_info.SqlQuery(sql.ToString()).ToList<manufac_info>();


            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult deleteManufac( int mfno ) {
            var manufacdata = db.manufac_info.Count(x => x.mfno == mfno);
            if (manufacdata > 0) {
                var manufac_record = db.manufac_info.FirstOrDefault(x => x.mfno == mfno);
                db.manufac_info.Remove(manufac_record);
                db.SaveChanges();
            }
            return Json(manufacdata, JsonRequestBehavior.AllowGet);
        }
        #endregion

        protected override void Dispose( bool disposing ) {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
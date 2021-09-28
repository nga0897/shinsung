using Mvc_VD.Classes;
using Mvc_VD.Commons;
using Mvc_VD.Models;
using Mvc_VD.Models.TIMS;
using Mvc_VD.Models.WOModel;
using Mvc_VD.Services;
using MySql.Data.MySqlClient;
using PagedList;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using WActual = Mvc_VD.Models.WOModel.WActual;

namespace Mvc_VD.Controllers
{
    public class ActualWOController : BaseController
    {
        private Entities db;
        string ec = "Ca này đã kết thúc!";

        private readonly IWOService _iWOService;
        private readonly ITIMSService _iTIMSService;
       
        public ActualWOController(IWOService iWOService, ITIMSService iTIMSService, IDbFactory dbFactory)
        {
            db = dbFactory.Init();
            _iWOService = iWOService;
            _iTIMSService = iTIMSService;
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
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var lang = HttpContext.Request.Cookies.AllKeys.Contains("language")
                ? HttpContext.Request.Cookies["language"].Value
                : "en";
            var router = this.ControllerContext.RouteData.Values.Values.ElementAt(0).ToString();
            var result = _iWOService.GetLanguage(lang, router);
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
        #region Web
        public ActionResult Web()
        {
            return SetLanguage("");
            //HttpCookie cookie = HttpContext.Request.Cookies["language"];
            //if (cookie != null)
            //{
            //    ViewBag.language = cookie.Value;
            //}
            //return View();
        }


        public JsonResult GetActual_Processes()
        {
            var data = db.comm_dt.Where(x => x.mt_cd == "COM007" && x.use_yn == "Y" && (x.dt_cd.StartsWith("ROT") || x.dt_cd.StartsWith("STA")))
                .AsEnumerable()
                .Select(x => new
                {
                    ProcessCode = x.dt_cd,
                    ProcessName = x.dt_nm
                }).ToList();
            return Json(data, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Add_w_actual_primary(w_actual_primary w_actual_primary)
        {
            try
            {
                //kiem tra product co ton tai khong
                if (!db.d_style_info.Any(x => x.style_no == w_actual_primary.product))
                {
                    return Json(new { result = false, kq = "Product Code Dont Exits" }, JsonRequestBehavior.AllowGet);
                }
                var check = db.w_actual_primary.ToList();
                String dateChString = DateTime.Now.ToString("yyyyMMdd");

                var bien_1 = "PO" + dateChString;
                var check_con = check.Where(x => x.at_no.StartsWith(bien_1)).ToList();
                if (check_con.Count == 0)
                {
                    w_actual_primary.at_no = "PO" + dateChString + "-001";
                }
                else
                {
                    var menuCd = string.Empty;
                    var subMenuIdConvert = 0;
                    var list1 = check_con.OrderBy(x => x.at_no).LastOrDefault();
                    var bien1 = list1.at_no;
                    var subMenuId = bien1.Substring(bien1.Length - 3, 3);
                    int.TryParse(subMenuId, out subMenuIdConvert);
                    menuCd = bien_1 + "-" + string.Format("{0}{1}", menuCd, CreatePO((subMenuIdConvert + 1)));
                    w_actual_primary.at_no = menuCd;
                }
                w_actual_primary.finish_yn = "N";
                w_actual_primary.reg_dt = DateTime.Now;
                w_actual_primary.chg_dt = DateTime.Now;
                w_actual_primary.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                w_actual_primary.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();

                if (ModelState.IsValid)
                {
                    db.Entry(w_actual_primary).State = EntityState.Added;
                    db.SaveChanges();
                    //add công đoạn
                    //var danhsach = db.d_rounting_info.Where(x => x.style_no == w_actual_primary.product).ToList();
                    var danhsach = _iWOService.Getrouting(w_actual_primary.product, w_actual_primary.process_code).ToList();
                    foreach (var item in danhsach)
                    {
                        var w_actual = new w_actual();
                        w_actual.description = item.description;
                        w_actual.at_no = w_actual_primary.at_no;
                        w_actual.actual = 0;
                        w_actual.type = item.type;
                        w_actual.product = w_actual_primary.product;
                        w_actual.name = item.name;
                        w_actual.don_vi_pr = item.don_vi_pr;
                        w_actual.level = item.level;
                        w_actual.item_vcd = item.item_vcd;
                        w_actual.date = DateTime.Now.ToString("yyyy-MM-dd");
                        w_actual.defect = 0;
                        bool isF = false;
                        if (item.isFinish == "Y")
                        {
                            isF = true;
                        }
                       
                        w_actual.IsFinished = isF;
                        w_actual.reg_dt = DateTime.Now;
                        w_actual.chg_dt = DateTime.Now;
                        w_actual.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        w_actual.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();

                        //db.Entry(w_actual).State = EntityState.Added;
                        //db.SaveChanges();
                        _iWOService.InsertWActualInfo(w_actual);
                    }

                    IEnumerable<DatawActualPrimary> aaa = _iWOService.GetProcessingActual(w_actual_primary.at_no);

                    return Json(new { result = true, kq = aaa.FirstOrDefault() }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, kq = "Can not Create   " }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, kq = e }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult GetProductProcesses(string productCode)
        {

            string data = @"SELECT a.process_code, a.process_name FROM product_routing AS a WHERE a.style_no = @1  ORDER BY a.IsApply DESC  ;";
            IEnumerable<ProductProcess> data1 = db.Database.SqlQuery<ProductProcess>(data,
                new MySqlParameter("1", productCode));

            return Json(data1, JsonRequestBehavior.AllowGet);
        }
        private string CreatePO(int id)
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
        public JsonResult Add_w_actual(string style_no, string at_no, string name, w_actual w_actual)
        {
            try
            {

                w_actual.date = DateTime.Now.ToString("yyyy-MM-dd");
                var check_data = _iWOService.GetActual(at_no);
                w_actual.type = "SX";
                w_actual.at_no = at_no;
                w_actual.actual = 0;
                w_actual.name = name;
                w_actual.defect = 0;
                var check_con = check_data.Where(x => x.name == name).ToList();
                if (check_con.Count() > 0)
                {
                    w_actual.level = check_con.FirstOrDefault().level;
                }
                else
                {
                    w_actual.level = (check_data.Count() + 1);
                }
                w_actual.reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                w_actual.chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
                w_actual.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                w_actual.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                w_actual.item_vcd = "PC00004A";
               
                w_actual.product = style_no;
                if (ModelState.IsValid)
                {
                  int iddata =  _iWOService.InsertWActualInfo(w_actual);
                    w_actual.id_actual = iddata;
                    //check tồn tại nguyên vật liệu composite



                    var vlieu = style_no + "-" + name;

                    var isMaterialExist = _iWOService.IsMaterialInfoExist(vlieu);

                    if (!isMaterialExist)
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

                        _iWOService.InsertDMaterial(b);

                    }
                    return Json(new { result = true, message = "Thành công!!!", kq = w_actual }, JsonRequestBehavior.AllowGet);
                }
                
                return Json(new { result = false, message = "Trùng" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult updateDescriptionWActual(WActual item)
        {
          
            try
            {
                //kiểm tra mã này có trong db không
                var ISCheckExist = _iWOService.GetWActual(item.id_actual);
                if (ISCheckExist == null)
                {
                    return Json(new { result = false, message = "Không tìm thấy mã này để sửa nội dung." }, JsonRequestBehavior.AllowGet);
                }
                if (string.IsNullOrEmpty(item.description))
                {
                    return Json(new { result = false, message = "Vui lòng nhập nội dung cần thay đổi." }, JsonRequestBehavior.AllowGet);
                }
                _iWOService.UpdateWActual(item.id_actual, item.description);

                return Json(new { result = true, message = "Sửa thành công" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Không thể sửa mã này" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult updateDescriptionWMaterialInfo(int? wmtid, string description)
        {

            try
            {
                //kiểm tra mã này có trong db không
                var ISCheckExist = _iWOService.GetWMaterialInfo((int)wmtid);
                if (ISCheckExist == null)
                {
                    return Json(new { result = false, message = "Không tìm thấy mã này để sửa nội dung." }, JsonRequestBehavior.AllowGet);
                }
                if (string.IsNullOrEmpty(description))
                {
                    return Json(new { result = false, message = "Vui lòng nhập nội dung cần thay đổi." }, JsonRequestBehavior.AllowGet);
                }
                _iWOService.UpdateWMaterialInfoDescription((int)wmtid, description);

                return Json(new { result = true, message = "Sửa thành công" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Không thể sửa mã này" }, JsonRequestBehavior.AllowGet);
            }
        }
        //public JsonResult Modify_w_actualprimary(w_actual_primary w_actual_primary)
        //{
        //    try
        //    {
        //        var find = db.w_actual_primary.Find(w_actual_primary.id_actualpr);
        //        find.target = w_actual_primary.target;
        //        find.remark = w_actual_primary.remark;
        //        find.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
        //        if (ModelState.IsValid)
        //        {
        //            db.Entry(find).State = EntityState.Modified;
        //            db.SaveChanges(); // line that threw exception
        //            return Json(new { result = true, kq = find }, JsonRequestBehavior.AllowGet);
        //        }
        //        return Json(new { result = false, kq = "Không thể chỉnh sửa đổi!!" }, JsonRequestBehavior.AllowGet);
        //    }
        //    catch (Exception e)
        //    {
        //        return Json(new { result = false, kq = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
        //    }
        //}
        public JsonResult Modify_w_actualprimary(w_actual_primary w_actual_primary)
        {
            try
            {
                // var find = db.w_actual_primary.Find(w_actual_primary.id_actualpr);
                var find = _iWOService.GetPoInfo(w_actual_primary.id_actualpr);
                if (find != null)
                {

                    //kiểm tra xem các công đoạn của PO đó đã mapping chưa nếu chưa thì cho update process lại
                    // TRUE CAN'T DELETE/FALSE CAN DELETE
                    if (_iWOService.CheckDeletePO(find.at_no))
                    {



                    }
                    else
                    {
                        //nếu công đoạn hiện tại giống như công đoạn đang update thì không cần sửa

                        if (find.process_code != w_actual_primary.process_code)
                        {
                            //delete các công đoạn 
                            _iWOService.DeleteMachine(find.at_no);
                            _iWOService.DeleteStaff(find.at_no);
                            _iWOService.DeleteProcess(find.at_no);

                            //Insert các công đoạn mới được chọn

                            var danhsach = _iWOService.Getrouting(find.product, w_actual_primary.process_code).ToList();
                            foreach (var item in danhsach)
                            {
                                var w_actual = new w_actual();
                                w_actual.description = item.description;
                                w_actual.at_no = find.at_no;
                                w_actual.actual = 0;
                                w_actual.type = item.type;
                                w_actual.product = find.product;
                                w_actual.name = item.name;
                                w_actual.don_vi_pr = item.don_vi_pr;
                                w_actual.level = item.level;
                                w_actual.item_vcd = item.item_vcd;
                                w_actual.date = DateTime.Now.ToString("yyyy-MM-dd");
                                w_actual.defect = 0;
                                bool isF = false;
                                if (item.isFinish == "Y")
                                {
                                    isF = true;
                                }

                                w_actual.IsFinished = isF;
                                w_actual.reg_dt = DateTime.Now;
                                w_actual.chg_dt = DateTime.Now;
                                w_actual.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                                w_actual.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();

                                _iWOService.InsertWActualInfo(w_actual);
                            }
                        }

                    }



                    find.target = w_actual_primary.target.ToString();
                    find.remark = w_actual_primary.remark;
                    find.process_code = w_actual_primary.process_code;
                    find.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    _iWOService.UpdateWPrimaryPO(find);
                    return Json(new { result = true, kq = find }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { result = false, kq = "Không thể chỉnh sửa đổi!!" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, kq = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult Getdataw_actual_primary(Pageing pageing, string product, string product_name,string model, string at_no, string regstart,string regend)
        {
            string type = Request["type"] == null ? "" : Request["type"];
            var sql = new StringBuilder();

            if (string.IsNullOrEmpty(type))
            {
                int start = (pageing.page - 1) * pageing.rows;
                int end = (pageing.page - 1) * pageing.rows + pageing.rows;
                IEnumerable<DatawActualPrimary> aaa = _iWOService.GetDatawActualPrimary(product, product_name, model, at_no, regstart, regend, start, end).OrderByDescending(x => x.id_actualpr);
                
                int totals = aaa.Count();
                int totalPages = (int)Math.Ceiling((float)totals / pageing.rows);
                IEnumerable<DatawActualPrimary> dataactual = aaa.Skip<DatawActualPrimary>(start).Take(pageing.rows);
                var jsonReturn = new
                {
                    total = totalPages,
                    page = pageing.page,
                    records = totals,
                    rows = dataactual
                };
                return Json(jsonReturn, JsonRequestBehavior.AllowGet);
            }
            else
            {
                
                IEnumerable<DatawActualPrimary> dataactuals = _iWOService.GetDatawActualPrimaryFromSP(product, product_name, model, at_no, regstart, regend, "MMS").OrderByDescending(x => x.id_actualpr);
                int start = (pageing.page - 1) * pageing.rows;
                int totals = dataactuals.Count();
                int totalPages = (int)Math.Ceiling((float)totals / pageing.rows);
                IEnumerable<DatawActualPrimary> dataactual = dataactuals.Skip<DatawActualPrimary>(start).Take(pageing.rows);
                var jsonReturn = new
                {
                    total = totalPages,
                    page = pageing.page,
                    records = totals,
                    rows = dataactual
                };
                return Json(jsonReturn, JsonRequestBehavior.AllowGet);
            }

        }

 
        public JsonResult GetFinishPO(Pageing pageing, string product, string product_name, string model, string at_no, string regstart, string regend)
        {
            StringBuilder sql = new StringBuilder($"Call GetAllFinishProducts('{product}','{product_name}','{model}','{at_no}','{regstart}','{regend}','MMS');");
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
                poModified.finish_yn = "Y";
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
        public JsonResult Redo_Finish(int id_actualpr)
        {
            using (var db = new Entities())
            {
                var poModified = db.w_actual_primary.Where(x => x.id_actualpr == id_actualpr).FirstOrDefault();
                poModified.finish_yn = "N";
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
        public int ProcessCalculating(string po)
        {
            StringBuilder sql = new StringBuilder($"Call spActualWO_ProcessCalculating('{po}');");
            List<CalculatingProcessModel> list = new InitMethods().ConvertDataTable<CalculatingProcessModel>(sql);
            int result = 0;
            foreach (var item in list)
            {
                if (int.Parse(item.actual) > 0)
                {
                    result++;
                }
            }
            return result;
        }

        public class CalculatingProcessModel
        {
            public string process_count { get; set; }
            public string actual { get; set; }
        }

        public JsonResult Getdataw_actual(Pageing paging, string at_no)
        {
           
            IEnumerable<DatawActual> dataactuals = _iWOService.GetDatawActual(at_no);
            int start = (paging.page - 1) * paging.rows;
          
            int totals = dataactuals.Count();
            int totalPages = (int)Math.Ceiling((float)totals / paging.rows);
            IEnumerable<DatawActual> dataactual = dataactuals.Skip<DatawActual>(start).Take(paging.rows).OrderBy(x => x.level);
            var jsonReturn = new
            {
                total = totalPages,
                page = paging.page,
                records = totals,
                rows = dataactual
            };
            return Json(jsonReturn, JsonRequestBehavior.AllowGet);
          
        }

        public JsonResult xoa_wactual_con(int id)
        {
            try
            {
                var find_id = db.w_actual.Find(id);
                if (find_id != null)
                {
                    if (db.w_material_info.Any(x => x.id_actual == find_id.id_actual))
                    {
                        return Json(new { result = false, message = "PO này đã được sử dụng!" }, JsonRequestBehavior.AllowGet);
                    }
                    db.Entry(find_id).State = EntityState.Deleted;
                    db.SaveChanges();
                    return Json(new { result = true }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = "Không thể tìm thấy!" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult getdetail_actual(int id, string date, string shift)
        {
            try
            {
                //KIEM TRA ID TON TAIJ KHONG
                IEnumerable<WMaterialInfoAPI> data = _iWOService.GetDetailActualAPI(id, "CMT", date, shift);
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(e, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult Deletew_actual_primary(int id)
        {
            try
            {
                var find_id = db.w_actual_primary.Find(id);
                if (find_id != null)
                {
                    if (_iWOService.CheckDeletePO(find_id.at_no))
                    {
                        return Json(new { result = false, kq = "Đã tồn tại các công đoạn đang thực hiện!!" }, JsonRequestBehavior.AllowGet);
                    }
                    _iWOService.DeleteMachine(find_id.at_no);
                    _iWOService.DeleteStaff(find_id.at_no);
                    _iWOService.DeleteProcess(find_id.at_no);
                    _iWOService.DeletePO(find_id.at_no);
                   return Json(new { result = true }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, kq = "Không tìm thấy" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, kq = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);
            }
        }

        #region Mold

        public ActionResult MoldMgtData(Pageing pageing)
        {
            var md_no = Request["md_no"];
            var md_nm = Request["md_nm"];

            Dictionary<string, string> list = PagingAndOrderBy(pageing, " ORDER BY MyDerivedTable.mdno DESC ");
            String tempSql = "";
            tempSql = tempSql + "SELECT (CASE  WHEN b.mc_no IS NOT NULL THEN 'mapping'  ELSE 'Unmapping' END) su_dung ,ROW_NUMBER() OVER ( " + "\n";
            tempSql = tempSql + "ORDER BY a.mdno DESC) AS RowNum " + "\n";
            tempSql = tempSql + ", a.* " + "\n";
            tempSql = tempSql + "FROM d_mold_info a " + "\n";
            tempSql = tempSql + "LEFT JOIN d_pro_unit_mc AS b ON a.md_no=b.mc_no " + "\n";
            tempSql = tempSql + "WHERE (''='' OR a.md_no LIKE '%%') AND (''='' OR a.md_nm LIKE '%%') ";
            tempSql = tempSql + " and ('" + md_no + "'='' OR   a.md_no like'%" + md_no + "%')";
            tempSql = tempSql + " and ('" + md_nm + "'='' OR   a.md_nm like'%" + md_nm + "%')  GROUP BY a.mdno ORDER BY su_dung='Unmapping' DESC ";

            string countSql = " SELECT COUNT(*) FROM ( " + tempSql + " ) MyDerivedTable ";
            string viewSql = " SELECT * FROM ( " + tempSql + " ) MyDerivedTable "

                + " WHERE MyDerivedTable.RowNum BETWEEN " + list["start"] + " AND " + list["end"] + " "
                + list["orderBy"];
            return SearchAndPaging(countSql, viewSql, int.Parse(list["index"]), int.Parse(list["size"]));
        }

        public ActionResult Getprocess_moldunit(int id_actual)
        {
            String varname1 = "";
            varname1 = varname1 + "SELECT  concat(substring(start_dt,1,4),'-', substring(start_dt,5,2),'-', substring(start_dt,7,2),' ', substring(start_dt,9,2),':', substring(start_dt,11,2),':', substring(start_dt,13,2)) start_dt, " + "\n";
            varname1 = varname1 + "         concat(substring(end_dt,1,4),'-', substring(end_dt,5,2),'-', substring(end_dt,7,2),' ', substring(end_dt,9,2),':', substring(end_dt,11,2),':', substring(end_dt,13,2)) end_dt, " + "\n";
            varname1 = varname1 + "  remark, mc_no, use_yn,pmid " + "\n";
            varname1 = varname1 + "FROM d_pro_unit_mc " + "\n";
            varname1 = varname1 + "WHERE id_actual = '" + id_actual + "' and mc_no IN ( " + "\n";
            varname1 = varname1 + "SELECT md_no " + "\n";
            varname1 = varname1 + "FROM d_mold_info)";

            var result = new InitMethods().ConvertDataTableToJsonAndstring(varname1);

            return result;
        }

        public ActionResult Createprocessmachine_unit(string mc_no, string use_yn, string remark, d_pro_unit_mc a)
        {
            try
            {
                var start1 = DateTime.Now;
                
                float timenow = float.Parse(start1.ToString("HH")) + float.Parse(DateTime.Now.ToString("mm")) / 60;
                var end = "";
                var check = "";
                end = _iWOService.GetEndDateProcessUnitStaff(timenow);
                
                var start = DateTime.Now.ToString("yyyyMMddHHmmss");
                var dateConvert = new DateTime();
                if (DateTime.TryParse(start, out dateConvert))
                {
                    start = dateConvert.ToString("yyyyMMddHHmmss");
                }
                if (DateTime.TryParse(end, out dateConvert))
                {
                    end = dateConvert.ToString("yyyyMMddHHmmss");
                }
                if (Convert.ToInt64(start) >= Convert.ToInt64(end))
                {
                    return Json(new { result = 2, mc_no = mc_no }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    string status = _iWOService.GetStatusdprounitmc(mc_no, (int)a.id_actual, start, end);
                    if (status == "0")
                    {
                        a.start_dt = start;
                        a.end_dt = end;
                        a.mc_no = mc_no;
                        a.use_yn = use_yn;
                        a.remark = remark;
                        a.del_yn = "N";
                        a.chg_dt = DateTime.Now;
                        a.reg_dt = DateTime.Now;
                        int pmid = _iWOService.InsertProUnitMC(a);
                        a.start_dt = UTL.ConvertToDatetimeString(a.start_dt);
                        a.end_dt = UTL.ConvertToDatetimeString(a.end_dt);
                        a.pmid = pmid;
                       
                        return Json(new { result = 0, a }, JsonRequestBehavior.AllowGet);

                    }
                    else if (status == "1")
                    {
                        return Json(new { result = 1, mc_no = mc_no }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        int pmid = db.Database.SqlQuery<int>(status, new MySqlParameter("@1", mc_no),
                         new MySqlParameter("@2", a.id_actual),
                          new MySqlParameter("@3", start),
                           new MySqlParameter("@4", end)).FirstOrDefault();
                        return Json(new { result = 3, update = pmid, start = start, end = end }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception)
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Createprocessmachine_duplicate(string mc_no, string use_yn, string remark, int id_update, string start, string end, d_pro_unit_mc a)
        {
            
            d_pro_unit_mc dprounitmc = _iWOService.Getdprounitmc(id_update);
            if (dprounitmc != null)
            {
                dprounitmc.end_dt = start;
                _iWOService.Updatedprounitmc(dprounitmc);
               
                a.start_dt = start;
                a.end_dt = end;
                a.mc_no = mc_no;
                a.use_yn = use_yn;
                a.remark = remark;
                a.del_yn = "N";
                a.chg_dt = DateTime.Now;
                a.reg_dt = DateTime.Now;
                int pmid = _iWOService.InsertProUnitMC(a);
              
                a.start_dt = a.start_dt.Substring(0, 4) + "-" + a.start_dt.Substring(4, 2) + "-" + a.start_dt.Substring(6, 2) + " " + a.start_dt.Substring(8, 2) + ":" + a.start_dt.Substring(10, 2) + ":" + a.start_dt.Substring(12, 2);
                a.end_dt = a.end_dt.Substring(0, 4) + "-" + a.end_dt.Substring(4, 2) + "-" + a.end_dt.Substring(6, 2) + " " + a.end_dt.Substring(8, 2) + ":" + a.end_dt.Substring(10, 2) + ":" + a.end_dt.Substring(12, 2);
              
                return Json(a, JsonRequestBehavior.AllowGet);
            }
            return Json(new { result = false }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Modifyprocessmachine_unit(string mc_no, int pmid, string use_yn, d_pro_unit_mc a, string end, string start)
        {
            var check_mc = db.d_machine_info.Where(x => x.mc_no == mc_no).ToList().Count();
            var check_md = db.d_mold_info.Where(x => x.md_no == mc_no).ToList().Count();
            var tong = check_mc + check_md;
            if (tong == 0)
            {
                return Json(new { result = 4, message = "Không tìm thấy mã" }, JsonRequestBehavior.AllowGet);
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
            if (Convert.ToInt64(start) >= Convert.ToInt64(end))
            {
                return Json(new { result = 2, mc_no = mc_no }, JsonRequestBehavior.AllowGet);
            }
            else
            {
               
                int ttcount = _iWOService.Gettotalcountdprounitmc(mc_no, (int)a.id_actual, pmid);
                if (ttcount > 0)
                {
                    int status = _iWOService.GetStatusdprounitmcupdate(mc_no, (int)a.id_actual, pmid, start, end);
                    if (status == 0)
                    {
                        d_pro_unit_mc item = db.d_pro_unit_mc.Find(pmid);
                        item.start_dt = start;
                        item.end_dt = end;
                        item.mc_no = mc_no;
                        item.use_yn = use_yn;
                        item.del_yn = "N";
                        _iWOService.Updatedprounitmc(item);
                        var value = _iWOService.Getdprounitmc(pmid);
                        return Json(new { result = 0, value }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        return Json(new { result = 1, mc_no = mc_no }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    d_pro_unit_mc item = db.d_pro_unit_mc.Find(pmid);
                    item.start_dt = start;
                    item.end_dt = end;
                    item.mc_no = mc_no;
                    item.use_yn = use_yn;
                    item.del_yn = "N";
                    item.remark = a.remark;
                    item.chg_dt = DateTime.Now;
                    _iWOService.Updatedprounitmc(item);
                    item.start_dt = item.start_dt.Substring(0, 4) + "-" + item.start_dt.Substring(4, 2) + "-" + item.start_dt.Substring(6, 2) + " " + item.start_dt.Substring(8, 2) + ":" + item.start_dt.Substring(10, 2) + ":" + item.start_dt.Substring(12, 2);
                    item.end_dt = item.end_dt.Substring(0, 4) + "-" + item.end_dt.Substring(4, 2) + "-" + item.end_dt.Substring(6, 2) + " " + item.end_dt.Substring(8, 2) + ":" + item.end_dt.Substring(10, 2) + ":" + item.end_dt.Substring(12, 2);
                    return Json(new { result = 0, item }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        public ActionResult DeleteMold_mc_wk_actual(string id, string sts)
        {
            if (!string.IsNullOrEmpty(id))
            {
                string sqldel = "";
                StringBuilder varname1 = new StringBuilder();

                var time_now = DateTime.Now.ToString("yyyyMMddHHmmss");
                if (sts == "wk")
                {

                    string sql = @"SELECT COUNT(a.id_actual) FROM  w_material_info AS a 
                                JOIN d_pro_unit_staff AS b ON a.id_actual=b.id_actual 
                                WHERE b.psid=@1 AND (a.reg_dt BETWEEN DATE_FORMAT(b.start_dt, '%Y-%m-%d %H:%i:%s') AND  DATE_FORMAT(b.end_dt, '%Y-%m-%d %H:%i:%s'))  
                                ;";
                    int kq = db.Database.SqlQuery<int>(sql, new MySqlParameter("@1", id)).FirstOrDefault();

                    if (kq > 0)
                    {
                        return Json(new { result = false, message = "Lot Đã tồn tại!!!" }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        sqldel = @"DELETE FROM d_pro_unit_staff WHERE psid in (@1) and @2<= end_dt";
                    }
                }
                else if (sts == "mc" || sts == "mold")
                {
                    string sql = @"SELECT COUNT(a.id_actual) FROM  w_material_info AS a 
                                JOIN d_pro_unit_mc AS b ON a.id_actual=b.id_actual 
                                WHERE b.pmid=@1 AND (a.reg_dt BETWEEN DATE_FORMAT(b.start_dt, '%Y-%m-%d %H:%i:%s') AND  DATE_FORMAT(b.end_dt, '%Y-%m-%d %H:%i:%s'))  
                                ;";
                    int kq = db.Database.SqlQuery<int>(sql, new MySqlParameter("@1", id)).FirstOrDefault();
                    if (kq > 0)
                    {
                        return Json(new { result = false, message = "Lot Đã tồn tại!!!" }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        sqldel = @"DELETE FROM d_pro_unit_mc WHERE pmid in (@1) and @2<= end_dt";
                    }

                }
                string del = db.Database.ExecuteSqlCommand(sqldel, new MySqlParameter("@1", id), new MySqlParameter("@2", time_now)).ToString();
                if (del == "1")
                {
                    return Json(new { result = true, message = "Xóa Thành Công!" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { result = false, message = "Xóa thất bại vui lòng check lại!" }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                return Json(new { result = false, message = "Vui lòng chọn mã cần xóa!" }, JsonRequestBehavior.AllowGet);
            }

        }

        #endregion Mold

        #region Staff
        public class het_ca
        { public int dem { get; set; } }
        public JsonResult Check_New_worker_Machine(int id_actual)
        {
            var mess = "";

            var dem = 0;
            if (!db.d_pro_unit_mc.Any(x => x.id_actual == id_actual))
            {
                dem++;
                mess = "Thêm máy mới ";
            }

            if (!db.d_pro_unit_staff.Any(x => x.id_actual == id_actual))
            {
                dem++;
                mess += "Thêm công nhân mới";
            }
            mess += " Vào Công Đoạn Này !!!";

            if (dem > 0)
            {
                return Json(new { result = false, message = mess }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { result = true }, JsonRequestBehavior.AllowGet);


        }
        public ActionResult get_staff()
        {
            var mcsatf = _iWOService.GetListCommDT("COM013", "Y");
            return Json(mcsatf, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Getprocess_staff(int id_actual)
        {
           
            IEnumerable<ProcessUnitStaff> value = _iWOService.GetListProcessStaff(id_actual);
            return Json(value, JsonRequestBehavior.AllowGet);
        }

        public ActionResult search_staff_pp(Pageing pageing,string position_cd,string uname,string userid)
        {
            Dictionary<string, string> list = PagingAndOrderBy(pageing, " ORDER BY MyDerivedTable.userid DESC ");
            int totalRecords = _iWOService.TotalRecordsSearchStaffpp(userid, uname, position_cd);
            int pagesize = int.Parse(list["size"]);
            int totalPages = 0;
            try
            {
                totalPages = (int)Math.Ceiling((float)totalRecords / (float)pagesize);
            }
            catch(Exception e)
            {
                totalPages = totalRecords / pagesize;
            }
            
            IEnumerable<StaffPP> Data = _iWOService.GetListSearchStaffpp(userid, uname, position_cd, list["start"], list["end"], list["orderBy"]);
            var ss = Data.ToList();
            var result = new
            {
                total = totalPages,
                page = int.Parse(list["index"]),
                records = totalRecords,
                rows = Data
            };
            return Json(result, JsonRequestBehavior.AllowGet);

        }

        public ActionResult Getposition_staff(comm_mt comm_dt)
        {
            var position = db.comm_dt.ToList().Where(item => item.mt_cd == "COM018" && item.use_yn == "Y").ToList();
            return Json(position, JsonRequestBehavior.AllowGet);
        }

        public ActionResult get_mc_type()
        {
            var mc_type = db.comm_dt.ToList().Where(item => item.mt_cd == "COM007").ToList();
            return Json(mc_type, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Createprocess_unitstaff(string staff_tp, string staff_id, string use_yn, d_pro_unit_staff a)
        {
            try
            {
                var start1 = DateTime.Now; //DateTime.ParseExact
                float timenow = float.Parse(start1.ToString("HH")) + float.Parse(DateTime.Now.ToString("mm")) / 60;
                var starttesst = DateTime.Now.ToString("mm");
                var dsach_ca = db.w_policy_mt.ToList();
                var end = "";
                end = _iWOService.GetEndDateProcessUnitStaff(timenow);
                //function check ca
                var start = DateTime.Now.ToString("yyyyMMddHHmmss");
                var dateConvert = new DateTime();
                if (DateTime.TryParse(end, out dateConvert))
                {
                    end = dateConvert.ToString("yyyyMMddHHmmss");
                }
                
                string status = _iWOService.CheckExistProcessUnitStaff(staff_id, (int)a.id_actual, start, end);

                if (status == null )
                {
                    a.start_dt = start;
                    a.end_dt = end;
                    a.staff_id = staff_id;
                    a.staff_tp = staff_tp;
                    a.use_yn = use_yn;
                    a.del_yn = "N";
                    a.chg_dt = DateTime.Now;
                    a.reg_dt = DateTime.Now;
                    int psid = _iWOService.InsertDProUnitStaff(a);
                    ProcessUnitStaff value = _iWOService.GetDProUnitStaff(psid);
                    return Json(new { result = true, kq = value, message = "Tạo thành công" }, JsonRequestBehavior.AllowGet);
                }
              
                else
                {
                    return Json(new { result = false, kq = new ProcessUnitStaff(),  message = "Công nhân này đang làm việc trong công đoạn này" }, JsonRequestBehavior.AllowGet);
                   
                }
            }
            catch (Exception ex)
            {
                return Json(new { result = false, kq = new ProcessUnitStaff(), message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult Modifyprocess_unitstaffold(int psid, string staff_id, string use_yn, d_pro_unit_staff a, string end, string start)
        {
            if (!db.mb_info.Any(x => x.userid == staff_id))
            {
                return Json(new { result = 4, message = "Không tìm thấy nhân viên này" }, JsonRequestBehavior.AllowGet);
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
            if (Convert.ToInt64(start) >= Convert.ToInt64(end))
            {
                return Json(new { result = 2, staff_id = staff_id }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                int totalcount = _iWOService.GettotalcountProcessUnitStaff(staff_id, psid);
                if (totalcount > 0)
                {
                    string sqlquery = @"SELECT COUNT(*) FROM d_pro_unit_staff AS a
                        WHERE a.staff_id = @1
                        AND DATE_FORMAT(a.start_dt,'%Y-%m-%d %H:%i:%s') >= DATE_FORMAT( @2, '%Y-%m-%d %H:%i:%s') 
                        AND DATE_FORMAT(a.end_dt,'%Y-%m-%d %H:%i:%s') <= DATE_FORMAT(@3, '%Y-%m-%d %H:%i:%s') AND a.psid !=@4";
                    int kqcheckdup = db.Database.SqlQuery<int>(sqlquery, new MySqlParameter("@1", staff_id),
                        new MySqlParameter("@2", start), new MySqlParameter("@3", end), new MySqlParameter("@4", psid)).FirstOrDefault();
                    if (kqcheckdup > 0)
                    {
                        return Json(new { result = 1, staff_id = staff_id }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        d_pro_unit_staff item = db.d_pro_unit_staff.Find(psid);
                        item.start_dt = start;
                        item.end_dt = end;
                        item.staff_id = staff_id;
                        item.use_yn = use_yn;
                        item.del_yn = "N";
                        item.chg_dt = DateTime.Now;
                        _iWOService.Updatedprounitstaff(item);
                        item.start_dt = item.start_dt.Substring(0, 4) + "-" + item.start_dt.Substring(4, 2) + "-" + item.start_dt.Substring(6, 2) + " " + item.start_dt.Substring(8, 2) + ":" + item.start_dt.Substring(10, 2) + ":" + item.start_dt.Substring(12, 2);
                        item.end_dt = item.end_dt.Substring(0, 4) + "-" + item.end_dt.Substring(4, 2) + "-" + item.end_dt.Substring(6, 2) + " " + item.end_dt.Substring(8, 2) + ":" + item.end_dt.Substring(10, 2) + ":" + item.end_dt.Substring(12, 2);
                        return Json(new { result = 0, kq = item }, JsonRequestBehavior.AllowGet);

                    }
                }
                else
                {
                    d_pro_unit_staff item = db.d_pro_unit_staff.Find(psid);
                    item.start_dt = start;
                    item.end_dt = end;
                    item.staff_id = staff_id;
                    item.use_yn = use_yn;
                    item.del_yn = "N";
                    _iWOService.Updatedprounitstaff(item);
                    item.start_dt = item.start_dt.Substring(0, 4) + "-" + item.start_dt.Substring(4, 2) + "-" + item.start_dt.Substring(6, 2) + " " + item.start_dt.Substring(8, 2) + ":" + item.start_dt.Substring(10, 2) + ":" + item.start_dt.Substring(12, 2);
                    item.end_dt = item.end_dt.Substring(0, 4) + "-" + item.end_dt.Substring(4, 2) + "-" + item.end_dt.Substring(6, 2) + " " + item.end_dt.Substring(8, 2) + ":" + item.end_dt.Substring(10, 2) + ":" + item.end_dt.Substring(12, 2);
                    return Json(new { result = 0, kq = item }, JsonRequestBehavior.AllowGet);
                }
            }
        }
        public ActionResult Modifyprocess_unitstaff(int psid, string staff_id, string use_yn, d_pro_unit_staff a, string end, string start)
        {


            //lấy id_staff_id 
            d_pro_unit_staff item = db.d_pro_unit_staff.Find(psid);
            if (item == null )
            {
                return Json(new { result = 4, message = "Không có hàng nào được chọn" }, JsonRequestBehavior.AllowGet);
            }
            // kiểm tra xem id công nhân mà chọn update có khác với công nhân đưa vào không, nếu khác là return
            if (item.staff_id != staff_id)
            {
                return Json(new { result = 4, message = "Bạn không được sửa mã công nhân chỉ được sửa Thời gian của công nhân." }, JsonRequestBehavior.AllowGet);
            }
            staff_id = item.staff_id;

            if (!db.mb_info.Any(x => x.userid == staff_id))
            {
                return Json(new { result = 4, message = "Không tìm thấy nhân viên này" }, JsonRequestBehavior.AllowGet);
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
            if (Convert.ToInt64(start) >= Convert.ToInt64(end))
            {
                return Json(new { result = 2, staff_id = staff_id }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                //kiểm tra xem end time nhỏ hơn end-time cũ thì vào kiểm tra thêm end-time phải = hoặc lớn hơn cuộn bobbin đã đc scan

                
              
                // end(giờ muốn cập nhật) nhỏ hơn (item.end_dt) giờ đang tồn tại trong db 
                if (Convert.ToInt64(end) < Convert.ToInt64(item.end_dt) || Convert.ToInt64(start) > Convert.ToInt64(item.end_dt))
                {
                    // lấy giờ mới nhất trong db, nếu end(giờ muốn cập nhật) nhỏ hơn giờ đã tồn tại mã lot trong db thì ko cho
                    string sql = @"SELECT MAX(a.reg_dt) AS reg_dt
                                FROM  w_material_info AS a 
                                JOIN d_pro_unit_staff AS b ON a.id_actual=b.id_actual 
                                WHERE b.psid=@1 AND (a.reg_dt BETWEEN DATE_FORMAT(@2, '%Y-%m-%d %H:%i:%s') AND  DATE_FORMAT(@3, '%Y-%m-%d %H:%i:%s'))  
                                ;";
                    string kq = db.Database.SqlQuery<string>(sql,
                        new MySqlParameter("@1", psid),
                        new MySqlParameter("@2", item.start_dt),
                        new MySqlParameter("@3", item.end_dt)
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
                        if (Convert.ToInt64(start) > Convert.ToInt64(kq))
                        {
                            return Json(new { result = 4, message = "Đã tồn tại mã Bobbin" }, JsonRequestBehavior.AllowGet);
                        }
                    }
                }


                item.start_dt = start;
                item.end_dt = end;
                item.staff_id = staff_id;
                item.use_yn = use_yn;
                item.del_yn = "N";
                item.chg_dt = DateTime.Now;
                _iWOService.Updatedprounitstaff(item);
                item.start_dt = item.start_dt.Substring(0, 4) + "-" + item.start_dt.Substring(4, 2) + "-" + item.start_dt.Substring(6, 2) + " " + item.start_dt.Substring(8, 2) + ":" + item.start_dt.Substring(10, 2) + ":" + item.start_dt.Substring(12, 2);
                item.end_dt = item.end_dt.Substring(0, 4) + "-" + item.end_dt.Substring(4, 2) + "-" + item.end_dt.Substring(6, 2) + " " + item.end_dt.Substring(8, 2) + ":" + item.end_dt.Substring(10, 2) + ":" + item.end_dt.Substring(12, 2);
                return Json(new { result = 0, kq = item }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult Modifyprocess_unitstaff2(int psid, string staff_id, string use_yn, d_pro_unit_staff a, string end, string start)
        {
            if (!db.mb_info.Any(x => x.userid == staff_id))
            {
                return Json(new { result = 4, message = "Không tìm thấy nhân viên này" }, JsonRequestBehavior.AllowGet);
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
            if (Convert.ToInt64(start) >= Convert.ToInt64(end))
            {
                return Json(new { result = 2, staff_id = staff_id }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                d_pro_unit_staff item = db.d_pro_unit_staff.Find(psid);
                item.start_dt = start;
                item.end_dt = end;
                item.staff_id = staff_id;
                item.use_yn = use_yn;
                item.del_yn = "N";
                item.chg_dt = DateTime.Now;
                _iWOService.Updatedprounitstaff(item);
                item.start_dt = item.start_dt.Substring(0, 4) + "-" + item.start_dt.Substring(4, 2) + "-" + item.start_dt.Substring(6, 2) + " " + item.start_dt.Substring(8, 2) + ":" + item.start_dt.Substring(10, 2) + ":" + item.start_dt.Substring(12, 2);
                item.end_dt = item.end_dt.Substring(0, 4) + "-" + item.end_dt.Substring(4, 2) + "-" + item.end_dt.Substring(6, 2) + " " + item.end_dt.Substring(8, 2) + ":" + item.end_dt.Substring(10, 2) + ":" + item.end_dt.Substring(12, 2);
                return Json(new { result = 0, kq = item }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult Createprocessstaff_duplicate(string staff_id, string use_yn, int id_update, string start, string end, d_pro_unit_staff a)
        {
           
            var id = _iWOService.GetListDProUnitStaff(id_update);
            if (id != null)
            {
                id.end_dt = start;
                db.Entry(id).State = EntityState.Modified;
                db.SaveChanges();
                a.start_dt = start;
                a.end_dt = end;
                a.staff_id = staff_id;
                a.staff_tp = "";
                a.use_yn = use_yn;
                a.del_yn = "N";
                a.chg_dt = DateTime.Now;
                a.reg_dt = DateTime.Now;
                
                int psid = _iWOService.InsertDProUnitStaff(a);
                var value = _iWOService.GetDProUnitStaff(psid);
                return Json(value, JsonRequestBehavior.AllowGet);
            }
            return Json(false, JsonRequestBehavior.AllowGet);
        }

        #endregion Staff

        #region Machine

        public ActionResult searchpopupmachine(Pageing pageing,string mc_type,string mc_no,string mc_nm)
        {
            
            IEnumerable<DMachineInfoAPI> datapopupmachine = _iWOService.GetPopupMachine(mc_type, mc_no, mc_nm);
            
            int totals = datapopupmachine.Count();
            int totalPages = (int)Math.Ceiling((float)totals / pageing.rows);
            IEnumerable<DMachineInfoAPI> data = datapopupmachine.Skip<DMachineInfoAPI>(((pageing.page - 1) * (pageing.rows))).Take(pageing.rows);
            var result = new
            {
                total = totalPages,
                page = pageing.page,
                records = totals,
                rows = data
            };
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Getprocess_machineunit(int id_actual)
        {
            IEnumerable<ProcessMachineunit> data = _iWOService.Getlistdprounitmcfromidactual(id_actual);
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        #endregion Machine

        #region get tổng hợp

        public JsonResult Getinfo_mc_wk_mold(int id_actual)
        {
            bool check = false;
            IEnumerable<wo_info_mc_wk_mold> data = _iWOService.Getwoinfomcwkmold(id_actual);
            if (data.Count() > 0)
                check = true;
            return Json(new { data = data, result = check, message = "" }, JsonRequestBehavior.AllowGet);
        }

        #endregion get tổng hợp

        #region QC

        public ActionResult Getqc_item_pp()
        {
            var list = db.qc_item_mt.Where(x => x.del_yn == "N" && x.item_type == "PC").OrderByDescending(x => x.item_vcd).ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public ActionResult searchqc_item_pp(string item_type, string item_cd, string item_nm, string item_exp)
        {
            var list = db.qc_item_mt.Where(x => (x.item_type == "PC" || x.item_type == "TI" || x.item_type == "GS") && (x.del_yn == "N") && (item_nm == "" || x.item_nm.Contains(item_nm)) && (item_exp == "" || x.item_exp.Contains(item_exp)) && (item_cd == "" || x.item_vcd.Contains(item_cd)) && (item_type == "" || x.item_type.Contains(item_type))).ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetWproductQc(string item_vcd, int olddno)
        {
            var sql = new StringBuilder();

            sql.Append(" SELECT '0' as pqno ,'TOTAL' as pq_no,'' as work_dt,sum(a.check_qty)  as check_qty, sum(a.ok_qty) as ok_qty, (sum(a.check_qty)-sum(a.ok_qty))  as defect_qty ")
            .Append("   FROM w_product_qc as a  ")
            .Append(" where a.item_vcd='" + item_vcd + "' and a.olddno='" + olddno + "' ")
            .Append(" Union All SELECT b.pqno,b.pq_no,CONCAT(substr(b.work_dt,1,4),'-',substr(b.work_dt,5,2),'-',substr(b.work_dt,7,2),' ',substr(b.work_dt,9,2),':',substr(b.work_dt,11,2) ),b.check_qty,(b.ok_qty),(b.check_qty)-(b.ok_qty) as defect_qty  ")
            .Append(" from w_product_qc as b where b.item_vcd='" + item_vcd + "' and b.olddno='" + olddno + "' order by pq_no='TOTAL' desc ,pq_no desc,check_qty  ");

            return new InitMethods().ConvertDataTableToJsonAndReturn(sql);
        }

        #region qc API

        public ActionResult Popup_Qc_Check_API(string item_vcd)
        {
            var ds_item_vcd = db.qc_itemcheck_mt.Where(x => x.item_vcd == item_vcd && x.del_yn.Equals("N")).ToList();
            if (ds_item_vcd.Count > 0)
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

                return Json(new { result = true, qc_itemcheck_mt }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { result = false }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Update_m_facline_qc_value(string check_qty, string fqhno, int fqno)
        {
            if ((check_qty == null) && (fqhno == null))
            {
                var result1 = new { result = false, massege = "Check_qty || fqhno is null!!" };
                return Json(result1, JsonRequestBehavior.AllowGet);
            }

            var list = new ArrayList();
            var a2 = fqhno.TrimStart('[').TrimEnd(']').Split(',');
            var m1 = check_qty.TrimStart('[').TrimEnd(']').Split(',');
            var list_m_facline_qc = db.m_facline_qc.Find(fqno);
            bool tam = false;

            for (int i = 0; i < a2.Length; i++)
            {
                var defect_qty = list_m_facline_qc.check_qty - list_m_facline_qc.ok_qty;
                var id2 = int.Parse(a2[i]);
                var find = db.m_facline_qc_value.Find(id2);

                if (find != null && (Convert.ToInt32(m1[i]) <= defect_qty))
                {
                    tam = true;

                    find.check_qty = Convert.ToInt32(m1[i]);
                    find.chg_id = Session["authName"] == null ? null : Session["authName"].ToString();
                    db.Entry(find).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }

            if (tam)
            {
                var result = new { result = true };
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var result = new { result = false, massege = "Data no exist !!" };
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }


        #endregion qc API

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

        public ActionResult Insert_w_product_qc(string name, string style_no, string item_vcd, int check_qty
            , int ok_qty, int ng_qty, string mt_cd, string date, int id_actual,
           m_facline_qc MFQC)
        {
            #region insert info
            try
            {
                var check = db.m_facline_qc.Where(x => x.ml_no == mt_cd && x.fq_no.StartsWith("FQ")).ToList();
                var bien = "";
                var check_mt_cd = db.w_material_info.Where(x => x.mt_cd == mt_cd).SingleOrDefault();
                if (check_mt_cd == null)
                {
                    return Json(new { result = false, message = "Lot không tìm thấy!!!" }, JsonRequestBehavior.AllowGet);
                }
                //kiểm tra lấy PO
                var check_po = db.w_actual.Where(x => x.id_actual == check_mt_cd.id_actual).SingleOrDefault();
                var check_pr = db.w_actual_primary.Where(x => x.at_no == check_po.at_no).SingleOrDefault();
                var ca = "";
                var ca_ngay = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd 08:00:00"));
                var ca_dem = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd 20:00:00"));
                var ca_dem_hs = ca_ngay.AddDays(1);
                //tạo mã PO ca ngày hoặc ca đêm bên mms 
                if (ca_ngay <= DateTime.Now && DateTime.Now <= ca_dem)
                {
                    ca = "CN";
                }
                else
                {
                    ca = "CD";
                }
                bien = check_po.at_no + "-" + check_pr.product + "-" + ca + "-MMS-NG";
                int defec_t = 0;
                if (check.Count > 0)
                {

                    var fq = check.SingleOrDefault();
                    if (db.w_material_mapping.Any(x => x.mt_cd == fq.ml_no))
                    {
                        return Json(new { result = false, message = "Lot đã qua công đoạn khác " }, JsonRequestBehavior.AllowGet);
                    }
                    var so_luongng = fq.check_qty - fq.ok_qty;
                    defec_t = Convert.ToInt32(so_luongng.ToString());

                    //xoa bảng chính
                    StringBuilder varname1 = new StringBuilder();
                    varname1.Append("DELETE FROM m_facline_qc \n");
                    varname1.Append("WHERE fq_no='" + fq.fq_no + "';");

                    //tru Mã Ng đã tạo ra trc đó

                    varname1.Append("UPDATE w_material_info \n");
                    varname1.Append("SET gr_qty=gr_qty-" + so_luongng + ",real_qty=real_qty-" + so_luongng + " \n");
                    varname1.Append("WHERE mt_cd='" + bien + "';");


                    //update số lượng về ban đầu
                    varname1.Append("UPDATE w_material_info \n");
                    varname1.Append("SET gr_qty=gr_qty+" + so_luongng + " \n");
                    varname1.Append("WHERE mt_cd='" + fq.ml_no + "';");

                    //update defect của công đoạn 

                    varname1.Append("UPDATE w_actual \n");
                    varname1.Append("SET defect=defect-" + so_luongng + " \n");
                    varname1.Append("WHERE id_actual='" + id_actual + "';");
                    int effect_rows = new Excute_query().Execute_NonQuery(varname1);

                }

                //insert bảng m_facline_qc
                var list = db.m_facline_qc.Where(x => x.fq_no.StartsWith("FQ")).OrderBy(x => x.fq_no).ToList();
                if (list.Count == 0)
                {
                    MFQC.fq_no = "FQ000000001";
                }
                else
                {
                    var menuCd = string.Empty;
                    var subMenuIdConvert = 0;
                    var list1 = list.LastOrDefault();

                    var bien1 = list1.fq_no;
                    var subMenuId = bien1.Substring(bien1.Length - 9, 9);
                    int.TryParse(subMenuId, out subMenuIdConvert);
                    menuCd = "FQ" + string.Format("{0}{1}", menuCd, CreateFQ((subMenuIdConvert + 1)));
                    MFQC.fq_no = menuCd;
                }
                MFQC.check_qty = check_qty;
                MFQC.ok_qty = ok_qty;
                MFQC.ml_no = mt_cd;
                MFQC.work_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                MFQC.reg_dt = DateTime.Now;
                MFQC.chg_dt = DateTime.Now;
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
                //neu la ma chua check mms

                if (bien != "")
                {

                    StringBuilder varname1 = new StringBuilder();

                    if (!db.w_material_info.Any(x => x.mt_cd.StartsWith(bien)))
                    {
                        //nếu chưa tồn tại thì insert mã vào DB
                        varname1.Append("INSERT INTO w_material_info \n");
                        varname1.Append("            (staff_id,id_actual,mt_type, \n");
                        varname1.Append("             `mt_cd`,alert_NG, \n");
                        varname1.Append("             `mt_no`, \n");
                        varname1.Append("             `gr_qty`, \n");
                        varname1.Append("             `date`, \n");
                        varname1.Append("             `expiry_dt`, \n");
                        varname1.Append("             `dt_of_receipt`, \n");
                        varname1.Append("             `expore_dt`, \n");
                        varname1.Append("             `lot_no`, \n");
                        varname1.Append("             `mt_barcode`, \n");
                        varname1.Append("             `mt_qrcode`, \n");
                        varname1.Append("             `mt_sts_cd`, \n");
                        varname1.Append("             `use_yn`, \n");
                        varname1.Append("             `reg_id`, \n");
                        varname1.Append("             `reg_dt`, \n");
                        varname1.Append("             `chg_id`, \n");
                        varname1.Append("             `chg_dt`, \n");
                        varname1.Append("             orgin_mt_cd,lct_cd,real_qty) \n");
                        varname1.Append("SELECT staff_id,0,mt_type, \n");
                        varname1.Append("      '" + bien + "',1, \n");
                        varname1.Append("       `mt_no`, \n");
                        varname1.Append("       " + (check_qty - ok_qty) + ", \n");
                        varname1.Append("       `date`, \n");
                        varname1.Append("       `expiry_dt`, \n");
                        varname1.Append("       `dt_of_receipt`, \n");
                        varname1.Append("       `expore_dt`, \n");
                        varname1.Append("       `lot_no`, \n");
                        varname1.Append("       '" + bien + "', \n");
                        varname1.Append("        '" + bien + "', \n");
                        varname1.Append("       '003', \n");
                        varname1.Append("       `use_yn`, \n");
                        varname1.Append("       '" + MFQC.reg_id + "', \n");
                        varname1.Append("       Now(), \n");
                        varname1.Append("       '" + MFQC.reg_id + "', \n");
                        varname1.Append("       Now(), \n");
                        varname1.Append("       mt_cd,lct_cd," + (check_qty - ok_qty) + " \n");
                        varname1.Append("FROM   w_material_info \n");
                        varname1.Append("WHERE  mt_cd = '" + mt_cd + "'; \n");
                    }
                    else
                    {
                        //nếu tồn tại rồi thì update số lượng của NG PO 
                        varname1.Append("UPDATE w_material_info \n");
                        varname1.Append("SET gr_qty=(gr_qty+ " + (check_qty - ok_qty) + "),real_qty=(real_qty+ " + (check_qty - ok_qty) + ") \n");
                        varname1.Append("WHERE mt_cd='" + bien + "';");
                    }
                    //mã gốc trừ NG 

                    varname1.Append("UPDATE w_material_info \n");
                    varname1.Append("SET gr_qty=(gr_qty- " + (check_qty - ok_qty) + ") \n");
                    varname1.Append("WHERE mt_cd='" + mt_cd + "';");

                    int effect_rows1 = new Excute_query().Execute_NonQuery(varname1);

                    //update defect của công đoạn 
                    var update_defect = db.w_actual.Find(id_actual);
                    update_defect.defect = (update_defect.defect - defec_t) + (check_qty - ok_qty);
                    db.Entry(update_defect).State = EntityState.Modified;
                    db.SaveChanges();
                    StringBuilder update_actual_pr = new StringBuilder();
                    update_actual_pr.Append(" CALL `Update_SL_w_actual`(" + id_actual + ")  \n");
                    int tinhtoan = new Excute_query().Execute_NonQuery(update_actual_pr);
                    return Json(new { result = true, MFQC }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);
            }
            #endregion insert info
        }
        public ActionResult Insert_FaclineQc_API(m_facline_qc MFQC, string check_qty, string check_qty_error, string ok_qty, string item_vcd, string mt_cd)
        {
            try
            {
                var ng_qty = check_qty_error;

                var check = db.m_facline_qc.Where(x => x.ml_no == mt_cd && x.fq_no.StartsWith("FQ")).ToList();
                var bien = "";
                var check_mt_cd = db.w_material_info.Where(x => x.mt_cd == mt_cd).SingleOrDefault();
                if (check_mt_cd == null)
                {
                    return Json(new { result = false, message = "Lot không tìm thấy!!!" }, JsonRequestBehavior.AllowGet);
                }
                var id_actual = check_mt_cd.id_actual;
                //kiểm tra lấy PO
                var check_po = db.w_actual.Where(x => x.id_actual == check_mt_cd.id_actual).SingleOrDefault();
                var check_pr = db.w_actual_primary.Where(x => x.at_no == check_po.at_no).SingleOrDefault();
                var ca = "";
                var ca_ngay = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd 08:00:00"));
                var ca_dem = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd 20:00:00"));
                var ca_dem_hs = ca_ngay.AddDays(1);
                //tạo mã PO ca ngày hoặc ca đêm bên mms 
                if (ca_ngay <= DateTime.Now && DateTime.Now <= ca_dem)
                {
                    ca = "CN";
                }
                if (ca_dem <= DateTime.Now && DateTime.Now <= ca_dem_hs)
                {
                    ca = "CD";
                }
                bien = check_po.at_no + "-" + check_pr.product + "-" + ca + "-MMS-NG";
                int defec_t = 0;
                if (check.Count > 0)
                {

                    var fq = check.SingleOrDefault();
                    if (db.w_material_mapping.Any(x => x.mt_cd == fq.ml_no))
                    {
                        return Json(new { result = false, message = "Lot đã qua công đoạn khác " }, JsonRequestBehavior.AllowGet);
                    }
                    var so_luongng = fq.check_qty - fq.ok_qty;
                    defec_t = Convert.ToInt32(so_luongng.ToString());

                    //xoa bảng chính
                    StringBuilder varname1 = new StringBuilder();
                    varname1.Append("DELETE FROM m_facline_qc \n");
                    varname1.Append("WHERE fq_no='" + fq.fq_no + "';");

                    //tru Mã Ng đã tạo ra trc đó

                    varname1.Append("UPDATE w_material_info \n");
                    varname1.Append("SET gr_qty=gr_qty-" + so_luongng + ",real_qty=real_qty-" + so_luongng + " \n");
                    varname1.Append("WHERE mt_cd='" + bien + "';");


                    //update số lượng về ban đầu
                    varname1.Append("UPDATE w_material_info \n");
                    varname1.Append("SET gr_qty=gr_qty+" + so_luongng + " \n");
                    varname1.Append("WHERE mt_cd='" + fq.ml_no + "';");

                    //update defect của công đoạn 

                    varname1.Append("UPDATE w_actual \n");
                    varname1.Append("SET defect=defect-" + so_luongng + " \n");
                    varname1.Append("WHERE id_actual='" + id_actual + "';");
                    int effect_rows = new Excute_query().Execute_NonQuery(varname1);

                }

                //insert bảng m_facline_qc
                var list = db.m_facline_qc.Where(x => x.fq_no.StartsWith("FQ")).OrderBy(x => x.fq_no).ToList();
                if (list.Count == 0)
                {
                    MFQC.fq_no = "FQ000000001";
                }
                else
                {
                    var menuCd = string.Empty;
                    var subMenuIdConvert = 0;
                    var list1 = list.LastOrDefault();

                    var bien1 = list1.fq_no;
                    var subMenuId = bien1.Substring(bien1.Length - 9, 9);
                    int.TryParse(subMenuId, out subMenuIdConvert);
                    menuCd = "FQ" + string.Format("{0}{1}", menuCd, CreateFQ((subMenuIdConvert + 1)));
                    MFQC.fq_no = menuCd;
                }
                MFQC.check_qty = int.Parse(check_qty);
                MFQC.ok_qty = int.Parse(ok_qty);
                MFQC.ml_no = mt_cd;
                MFQC.work_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                MFQC.reg_dt = DateTime.Now;
                MFQC.chg_dt = DateTime.Now;
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
                //neu la ma chua check mms

                if (bien != "")
                {

                    StringBuilder varname1 = new StringBuilder();

                    if (!db.w_material_info.Any(x => x.mt_cd.StartsWith(bien)))
                    {
                        //nếu chưa tồn tại thì insert mã vào DB
                        varname1.Append("INSERT INTO w_material_info \n");
                        varname1.Append("            (staff_id,id_actual,mt_type, \n");
                        varname1.Append("             `mt_cd`,alert_NG, \n");
                        varname1.Append("             `mt_no`, \n");
                        varname1.Append("             `gr_qty`, \n");
                        varname1.Append("             `date`, \n");
                        varname1.Append("             `expiry_dt`, \n");
                        varname1.Append("             `dt_of_receipt`, \n");
                        varname1.Append("             `expore_dt`, \n");
                        varname1.Append("             `lot_no`, \n");
                        varname1.Append("             `mt_barcode`, \n");
                        varname1.Append("             `mt_qrcode`, \n");
                        varname1.Append("             `mt_sts_cd`, \n");
                        varname1.Append("             `use_yn`, \n");
                        varname1.Append("             `reg_id`, \n");
                        varname1.Append("             `reg_dt`, \n");
                        varname1.Append("             `chg_id`, \n");
                        varname1.Append("             `chg_dt`, \n");
                        varname1.Append("             orgin_mt_cd,lct_cd,real_qty) \n");
                        varname1.Append("SELECT staff_id,0,mt_type, \n");
                        varname1.Append("      '" + bien + "',1, \n");
                        varname1.Append("       `mt_no`, \n");
                        varname1.Append("       " + (int.Parse(check_qty) - int.Parse(ok_qty)) + ", \n");
                        varname1.Append("       `date`, \n");
                        varname1.Append("       `expiry_dt`, \n");
                        varname1.Append("       `dt_of_receipt`, \n");
                        varname1.Append("       `expore_dt`, \n");
                        varname1.Append("       `lot_no`, \n");
                        varname1.Append("       '" + bien + "', \n");
                        varname1.Append("        '" + bien + "', \n");
                        varname1.Append("       '003', \n");
                        varname1.Append("       `use_yn`, \n");
                        varname1.Append("       '" + MFQC.reg_id + "', \n");
                        varname1.Append("       Now(), \n");
                        varname1.Append("       '" + MFQC.reg_id + "', \n");
                        varname1.Append("       Now(), \n");
                        varname1.Append("       mt_cd,lct_cd," + (int.Parse(check_qty) - int.Parse(ok_qty)) + " \n");
                        varname1.Append("FROM   w_material_info \n");
                        varname1.Append("WHERE  mt_cd = '" + mt_cd + "'; \n");
                    }
                    else
                    {
                        //nếu tồn tại rồi thì update số lượng của NG PO 
                        varname1.Append("UPDATE w_material_info \n");
                        varname1.Append("SET gr_qty=(gr_qty+ " + (int.Parse(check_qty) - int.Parse(ok_qty)) + "),real_qty=(real_qty+ " + (int.Parse(check_qty) - int.Parse(ok_qty)) + ") \n");
                        varname1.Append("WHERE mt_cd='" + bien + "';");
                    }
                    //mã gốc trừ NG 

                    varname1.Append("UPDATE w_material_info \n");
                    varname1.Append("SET gr_qty=(gr_qty- " + (int.Parse(check_qty) - int.Parse(ok_qty)) + ") \n");
                    varname1.Append("WHERE mt_cd='" + mt_cd + "';");

                    int effect_rows1 = new Excute_query().Execute_NonQuery(varname1);

                    //update defect của công đoạn 
                    var update_defect = db.w_actual.Find(id_actual);
                    update_defect.defect = (update_defect.defect - defec_t) + (int.Parse(check_qty) - int.Parse(ok_qty));
                    db.Entry(update_defect).State = EntityState.Modified;
                    db.SaveChanges();
                    return Json(new { result = true, MFQC }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);  
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);
            }
            #region insert info

            #endregion insert info
        }
        public JsonResult change_gr_dv(string value_new, string value_old, string wmtid)
        {
            try
            {
                var a = value_new.TrimStart('[').TrimEnd(']').Split(',');
                var b = value_old.TrimStart('[').TrimEnd(']').Split(',');
                var c = wmtid.TrimStart('[').TrimEnd(']').Split(',');
                var so_luongcu = 0;
                var so_luongmoi = 0;
                for (int i = 0; i < c.Length; i++)
                {
                    var id = int.Parse(c[i]);
                    var check_exits = db.w_material_info.Find(id);
                    if (check_exits == null)
                    {
                        return Json(new { result = false, message = "Không tìm thấy mã lot" }, JsonRequestBehavior.AllowGet);
                    }
                    //kiểm tra đã trai qua công đoạn chưa
                    if (db.w_material_mapping.Any(x => x.mt_cd == check_exits.mt_cd))
                    {
                        return Json(new { result = false, message = "Mã này đã qua công đoạn khác" }, JsonRequestBehavior.AllowGet);
                    }
                    //đã kiểm tra  chưa
                    if (db.w_material_info.Any(x => x.mt_cd == (check_exits.mt_cd + "-NG")))
                    {
                        return Json(new { result = false, message = "Mã này đã qua công đoạn khác" }, JsonRequestBehavior.AllowGet);
                    }
                    //đã check bất cứ TQC OQC ANY Chưa
                    if (db.m_facline_qc.Any(x => x.ml_no == check_exits.mt_cd) || db.w_product_qc.Any(x => x.ml_no == check_exits.mt_cd))
                    {
                        return Json(new { result = false, message = "Mã này đã được kiểm tra ở công đoạn khác" }, JsonRequestBehavior.AllowGet);
                    }
                    String varname2 = "";
                    varname2 = varname2 + "SELECT * FROM d_pro_unit_staff AS a " + "\n";
                    varname2 = varname2 + "WHERE a.id_actual='" + check_exits.id_actual + "' AND (NOW() BETWEEN " + "\n";
                    varname2 = varname2 + "Date_format(a.start_dt, '%Y-%m-%d %H:%i:%s') " + "\n";
                    varname2 = varname2 + " AND Date_format(a.end_dt, '%Y-%m-%d %H:%i:%s'))";
                    var data1 = db.Database.SqlQuery<d_pro_unit_staff>(varname2).ToList().Count();
                    if (data1 == 0)
                    {
                        return Json(new { result = false, message = ec }, JsonRequestBehavior.AllowGet);
                    }

                    var value_cu = b[i];
                    var value_moi = a[i];
                    //
                    so_luongcu = so_luongcu + int.Parse(value_cu);
                    so_luongmoi = so_luongmoi + int.Parse(value_moi);
                }
                if (so_luongcu == so_luongmoi)
                {
                    for (int i = 0; i < a.Length; i++)
                    {
                        var id_modify = int.Parse(c[i]);
                        var get = db.w_material_info.Find(id_modify);
                        get.gr_qty = int.Parse(a[i]);
                        get.real_qty = int.Parse(a[i]);
                        get.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        db.Entry(get).State = EntityState.Modified;
                        db.SaveChanges(); // line that threw exception
                    }
                    return Json(new { result = true, message = "Thành công!!!" }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = "Số lượng chia không đúng" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult Changebb_dv(string bb_no, int wmtid)
        {
            try
            {
               
                var check = _iWOService.GetWMaterialInfo(wmtid);
                if (check == null)
                {
                    return Json(new { result = false, message = "Không tìm thấy mã lot này" }, JsonRequestBehavior.AllowGet);
                }
               
                if (_iWOService.CheckwMaterialMapping(check.mt_cd) > 0)
                {
                    return Json(new { result = false, message = "Mã lot này đã được chuyển qua công đoạn khác" }, JsonRequestBehavior.AllowGet);
                }
                if (check.mt_sts_cd == "009" || check.mt_sts_cd == "010")
                {
                    return Json(new { result = false, message = "Mã lot này đã được chuyển qua công đoạn OQC" }, JsonRequestBehavior.AllowGet);
                }

                
                var check_bb_new = _iWOService.GetdbobbininfoforChangebbdv(bb_no);
                bool check_bb_new_his = _iWOService.CheckBobbinHistory(bb_no);
               
                if (!check_bb_new_his)
                {
                    return Json(new { result = false, message = "Đồ đựng đã được sử dụng" }, JsonRequestBehavior.AllowGet);
                }
                if (check_bb_new == null)
                {
                    return Json(new { result = false, message = "Không tìm thấy đồ đựng này" }, JsonRequestBehavior.AllowGet);
                }
                //đã kiểm tra  chưa
               
                if (_iWOService.GettwmaterialinforwithNG(check.mt_cd + "-NG") > 0)
                {
                    return Json(new { result = false, message = "Mã lot này đã được chuyển qua công đoạn OQC" }, JsonRequestBehavior.AllowGet);
                }
                //đã check bất cứ TQC OQC ANY Chưa
                
                if (_iWOService.Checkwmfaclineqc(check.mt_cd) > 0 || _iWOService.Gettwproductqc(check.mt_cd) > 0)
                {
                    return Json(new { result = false, message = "Mã lot này đã được kiểm tra" }, JsonRequestBehavior.AllowGet);
                }
                
                var check_bb_histoty = _iWOService.GetdbobbinlcthistFrbbnoamtcd(check.bb_no, check.mt_cd);
                if (check_bb_histoty != null)
                {
                    //change bobin in history bobin
                    check_bb_histoty.bb_no = bb_no;
                    check_bb_histoty.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    _iWOService.Updatedbobbinlcthist(check_bb_histoty);
                    
                }
                else
                {
                    //add vào bb_history
                    var history = new d_bobbin_lct_hist();
                    history.bb_no = bb_no;
                    history.bb_nm = check_bb_new.bb_nm;
                    history.mc_type = check_bb_new.mc_type;
                    history.mt_cd = check.mt_cd;
                    history.use_yn = "Y";
                    history.del_yn = "N";
                    history.chg_dt = DateTime.Now;
                    history.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    history.reg_dt = DateTime.Now;
                    history.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    _iWOService.InsertToBobbinLctHistory(history);
                    
                }
                //xã bobin cũ
               
                var check_bb = _iWOService.GetdbobbininfoforChangebbdv(check.bb_no);
                if (check_bb != null)
                {
                    check_bb.mt_cd = "";
                    check_bb.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    _iWOService.UpdateBobbinMTCode(check_bb);
                }
                else
                {
                    //add bobin mới
                    check_bb_new.mt_cd = check.mt_cd;
                    check_bb_new.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    _iWOService.UpdateBobbinMTCode(check_bb_new);
                }



                //thay đổi bobin trên w_material_info
                check.bb_no = bb_no;
                check.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                _iWOService.UpdateMaterialInfo(check);
                return Json(new { result = true, message = "Thành công!!" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Getfacline_qc(string item_vcd, string mt_cd)
        {
            var sql = new StringBuilder();
            sql.Append(" SELECT b.fqno,b.fq_no,b.check_qty, b.ml_no,CONCAT(substr(b.work_dt,1,4),'-',substr(b.work_dt,5,2),'-',substr(b.work_dt,7,2),' ',substr(b.work_dt,9,2),':',substr(b.work_dt,11,2) )work_dt,(b.ok_qty),(b.check_qty)-(b.ok_qty) as defect_qty ")
            .Append(" from m_facline_qc as b where b.item_vcd='" + item_vcd + "' and b.ml_no='" + mt_cd + "' and fq_no like 'FQ%' order by fq_no='TOTAL' desc ,check_qty ,fq_no  ");

            return new InitMethods().ConvertDataTableToJsonAndReturn(sql);
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
                    return Json(new { result = false, message = "Số lượng Lỗi đã vượt quá lần trước" }, JsonRequestBehavior.AllowGet);
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
                //update list_m_facline_qc
                list_m_facline_qc.ok_qty = list_m_facline_qc.check_qty - soluongdefect;
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
                    var w_material_info2 = db.w_material_info.Where(x => x.mt_cd == (list_m_facline_qc.ml_no + "-NG")).SingleOrDefault();
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

        #endregion QC

        #region mapping

        #region container

        //public ActionResult searchbobbinPopup(string bb_no, string bb_nm, string mt_cd, int id_actual)
        //{
        //    StringBuilder varname1 = new StringBuilder();
        //    varname1.Append("SELECT  concat(b.blno,'-0') bno,b.mc_type,b.bb_no,b.mt_cd,b.bb_nm,b.use_yn,('')purpose,('')barcode, \n");
        //    varname1.Append("('')re_mark,(0)count_number,('N')del_yn,b.reg_id,b.reg_dt,b.chg_id,b.chg_dt \n");
        //    varname1.Append(" FROM d_bobbin_lct_hist AS b \n");
        //    varname1.Append("JOIN w_material_info AS c ON b.mt_cd=c.mt_cd \n");
        //    varname1.Append("JOIN w_actual AS d ON c.id_actual=d.id_actual \n");
        //    varname1.Append("WHERE c.id_actual!= " + id_actual + " and c.lct_cd like '002%' and c.gr_qty>0 \n");
        //    varname1.Append(" and d.at_no=(select at_no from w_actual where id_actual='" + id_actual + "' limit 1) ");
        //    varname1.Append("and ('" + bb_no + "'='' or b.bb_no LIKE '%" + bb_no + "%')");
        //    varname1.Append("and ('" + bb_nm + "'='' or b.bb_nm LIKE '%" + bb_nm + "%')");
        //    varname1.Append("and ('" + mt_cd + "'='' or b.mt_cd LIKE '%" + mt_cd + "%')");
        //    varname1.Append("UNION \n");
        //    varname1.Append("SELECT a.bno,a.mc_type,a.bb_no,a.mt_cd,a.bb_nm,a.use_yn,a.purpose,a.barcode, \n");
        //    varname1.Append("a.re_mark,a.count_number,a.del_yn,a.reg_id,a.reg_dt,a.chg_id,a.chg_dt FROM d_bobbin_info AS a \n");
        //    varname1.Append("WHERE (a.mt_cd=''  OR a.mt_cd IS NULL)  ");
        //    varname1.Append("and ('" + bb_no + "'='' or a.bb_no LIKE '%" + bb_no + "%')");
        //    varname1.Append("and ('" + bb_nm + "'='' or a.bb_nm LIKE '%" + bb_nm + "%')");
        //    varname1.Append("and ('" + mt_cd + "'='' or a.mt_cd LIKE '%" + mt_cd + "%')");

        //    return new InitMethods().ConvertDataTableToJsonAndReturn(varname1);
        //}
        public ActionResult searchbobbinPopup(Pageing pageing, string bb_no, string bb_nm, string mt_cd, int? id_actual)
        {

            Dictionary<string, string> list = PagingAndOrderBy(pageing, " ORDER BY  MyDerivedTable.mt_cd desc   ");



            IEnumerable<BobbinPoup> Data = _iWOService.GetListSearchGetListBoBinPopup(id_actual, bb_no, bb_nm, mt_cd);
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

           
            var records = totalRecords;

            int start = (pageing.page - 1) * pageing.rows;
            var rowsData = Data.Skip(start).Take(pageing.rows);


            var result = new
            {
                total = totalPages,
                page = int.Parse(list["index"]),
                records = totalRecords,
                rows = rowsData
            };
            return Json(result, JsonRequestBehavior.AllowGet);

        }
        public ActionResult searchbobbinPopupDV(string bb_no, string bb_nm, string mt_cd)
        {
            try
            {
                
                var data = _iWOService.Searchdbobbininfo(bb_no, bb_nm);
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Json(e, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion container

        public ActionResult AAA ()
        {
            string bien = Request["style_no"] == null ? "" : Request["style_no"].Trim();

            return null;

        }
        public JsonResult insertw_material(w_material_info a, w_material_mapping c, int id_actual, string name, string bb_no)
        {
            try
            {
                string style_no = Request["style_no"] == null ? "" : Request["style_no"].Trim();
               
                d_bobbin_info currentBobbin = _iWOService.GetBobbinInfo(bb_no);
                if (currentBobbin == null)
                {
                    return Json(new { result = false, message = "Đồ đựng này không tồn tại!" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                   
                    //check dồ đựng 
                    if (!_iWOService.CheckBobbinHistory(bb_no))
                    {
                        return Json(new { result = false, message = "Đồ đựng này đã được sử dụng" }, JsonRequestBehavior.AllowGet);
                    }
                }
               
                if (_iWOService.CheckStaffShift(id_actual))
                {
                    return Json(new { result = false, message = ec }, JsonRequestBehavior.AllowGet);
                }
                
                if (_iWOService.CheckMachineShift(id_actual))
                {
                    return Json(new { result = false, message = "Máy hết ca!Gia hạn hoặc tạo thêm máy!!" }, JsonRequestBehavior.AllowGet);
                }

                var check_exit_vo = db.w_actual.Find(id_actual);
                if (check_exit_vo == null)
                {
                    return Json(new { result = false, message = "Không tìm thấy Mã PO với mã lot này" }, JsonRequestBehavior.AllowGet);
                }

                string GetStaffOfProcess = string.Join(",", _iWOService.GetStaffOfProcess(id_actual));
                string GetMachineProcess = string.Join(",", _iWOService.GetMachineProcess(id_actual));

                

                DateTime dateTime = DateTime.Now; //Today at 00:00:00
                string Hour = dateTime.ToString("HHmmss");
                string Date = dateTime.ToString("yyyyMMdd");
                string FulldateTime = dateTime.ToString("yyyyMMddHHmmss");
                string MtNo = check_exit_vo.product + "-" + check_exit_vo.name;


                string CreateUser = Session["userid"] == null ? null : Session["userid"].ToString();

                string MTCode = MtNo + FulldateTime;
                int CountMTCode = _iWOService.CountDataMtCodeWmaterialInfo(MTCode) + 1;
                string MtCodeAuto = UTL.ConvertToString(CountMTCode, 6);
                string FullMtCode = MTCode + UTL.ConvertToString(CountMTCode, 6);
                //bắt đầu insert vào bảng w_material_info
                a.product = check_exit_vo.product;
                a.at_no = check_exit_vo.at_no;

                a.mt_type = "CMT";
                a.bb_no = bb_no;
                a.mt_no = MtNo;
               
                a.bbmp_sts_cd = "000";
                a.mt_sts_cd = "002";
                a.lct_cd = "002002000000000000";
                a.from_lct_cd = "002002000000000000";
                a.lct_sts_cd = "101";
                a.id_actual = id_actual;
                a.use_yn = "Y";
                a.real_qty = 0;
                a.sts_update = "composite";
                a.gr_qty = 0;
                a.chg_dt = DateTime.Now;
                a.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                a.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();

                a.reg_dt = DateTime.Now;

                DateTime dt = DateTime.Now; //Today at 00:00:00
                string input_dt = dt.ToString("yyyyMMddHHmmss");
                a.input_dt = input_dt;
                a.mt_cd = FullMtCode;
                a.mt_barcode = FullMtCode;
                a.mt_qrcode = FullMtCode;
                a.date = Date;
                a.staff_id = GetStaffOfProcess;
                a.machine_id = GetMachineProcess;
                int idWMaterialInfo = _iWOService.InsertToWmaterialInfo(a);
                w_material_info wmaterialinfo = _iWOService.GetWMaterialInfo(idWMaterialInfo);
                
                //mapping mt_cd với bobin

                currentBobbin.mt_cd = wmaterialinfo.mt_cd;
                //ds_bb.chg_dt = DateTime.Now;
                currentBobbin.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                _iWOService.UpdateBobbinMTCode(currentBobbin);
               

                //check lai function InsertToBobbinLctHistory
                var d_bobbin_lct_hist = new d_bobbin_lct_hist();
                d_bobbin_lct_hist.bb_no = currentBobbin.bb_no;
                d_bobbin_lct_hist.bb_nm = currentBobbin.bb_nm;
                d_bobbin_lct_hist.mc_type = currentBobbin.mc_type;
                d_bobbin_lct_hist.mt_cd = wmaterialinfo.mt_cd;
                d_bobbin_lct_hist.use_yn = "Y";
                d_bobbin_lct_hist.del_yn = "N";
                d_bobbin_lct_hist.reg_dt = DateTime.Now;
                d_bobbin_lct_hist.chg_dt = DateTime.Now;
                d_bobbin_lct_hist.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                d_bobbin_lct_hist.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                int currentBobbinInfo = _iWOService.InsertToBobbinLctHistory(d_bobbin_lct_hist);
                //var take = style_no + name;
                var mt_lot_con = _iWOService.Getmtlot(id_actual);
                if (!string.IsNullOrEmpty(mt_lot_con)) 
                {
                    
                    _iWOService.InsertMaterialMpping(wmaterialinfo.mt_cd, Session["userid"] == null ? null : Session["userid"].ToString(), mt_lot_con);
                    _iWOService.UpdateMaterialInfofroMpping(Session["userid"] == null ? null : Session["userid"].ToString(), wmaterialinfo.staff_id, GetMachineProcess, "CMT", wmaterialinfo.mt_cd);
                     
                    _iWOService.UpdateMaterialInffrMpping(Session["userid"] == null ? null : Session["userid"].ToString(), "CMT", wmaterialinfo.mt_cd);
                   
                }
               
                w_material_info_wo ds1 = _iWOService.Getwmaterialinfowo(wmaterialinfo.mt_no, wmaterialinfo.bbmp_sts_cd, wmaterialinfo.mt_cd, idWMaterialInfo);
               
                var mt_cd = MTCode.Remove(MTCode.Length - 12, 12);
               
                int gtri = _iWOService.Getsumgrqty(mt_cd);
               
                return Json(new { ds1 = ds1, gtri = gtri }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Json(new { result = false, message = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);
            }
        }
        public string check_staff(int id_actual)
        {
            String varname1 = "";
            varname1 = varname1 + "SELECT a.staff_id " + "\n";
            varname1 = varname1 + "FROM   d_pro_unit_staff AS a " + "\n";
            varname1 = varname1 + "WHERE  a.id_actual = '" + id_actual + "' " + "\n";
            varname1 = varname1 + "       AND Now() BETWEEN Date_format(a.start_dt, '%Y-%m-%d %H:%i:%s') AND " + "\n";
            varname1 = varname1 + "                             Date_format(a.end_dt, '%Y-%m-%d %H:%i:%s')";
            var data = db.Database.SqlQuery<staff>(varname1).ToList();

            var ketqua = data.Select(x => x.staff_id);
            var csv = string.Join(", ", ketqua);

            return csv;
        }
        public class staff
        {
            public string staff_id { get; set; }
        }
        public string machine_sx(int id_actual)
        {
            String varname1 = "";
            varname1 = varname1 + "SELECT a.mc_no " + "\n";
            varname1 = varname1 + "FROM d_pro_unit_mc AS a " + "\n";
            varname1 = varname1 + "JOIN d_machine_info AS b ON a.mc_no=b.mc_no " + "\n";
            varname1 = varname1 + "WHERE a.id_actual='" + id_actual + "' " + "\n";
            varname1 = varname1 + "AND NOW() BETWEEN DATE_FORMAT(a.start_dt,'%Y-%m-%d %H:%i:%s') AND DATE_FORMAT(a.end_dt,'%Y-%m-%d %H:%i:%s')";
            var data = db.Database.SqlQuery<machine>(varname1).ToList();

            var ketqua = data.Select(x => x.mc_no);
            var csv = string.Join(", ", ketqua);

            return csv;
        }
        public class machine
        {
            public string mc_no { get; set; }
        }
        #region table1

        //xoa container
        public ActionResult Xoa_mt_pp_composite(int id)
        {
            //w_material_info aa = db.w_material_info.Find(id);
            w_material_info a = _iWOService.GetWMaterialInfo(id);
            if (id != 0 && a != null)
            {
                //kiêm tra hết ca chưa 
                //String varname1 = "";
                //varname1 = varname1 + "SELECT * FROM d_pro_unit_staff AS a " + "\n";
                //varname1 = varname1 + "WHERE a.id_actual='" + a.id_actual + "' and '" + a.staff_id + "' like concat('%',a.staff_id,'%')  AND (NOW() BETWEEN " + "\n";
                //varname1 = varname1 + "Date_format(a.start_dt, '%Y-%m-%d %H:%i:%s') " + "\n";
                //varname1 = varname1 + " AND Date_format(a.end_dt, '%Y-%m-%d %H:%i:%s'))";
                //string sqlcheckca = @"SELECT COUNT(*) FROM d_pro_unit_staff AS a 
                //WHERE a.id_actual=@1 and @2 like concat('%',a.staff_id,'%')  AND (NOW() BETWEEN 
                //Date_format(a.start_dt, '%Y-%m-%d %H:%i:%s') 
                //    AND Date_format(a.end_dt, '%Y-%m-%d %H:%i:%s'))";
                
                //sửa được
                int data1 = _iWOService.CheckHetCaMT((int)a.id_actual, a.staff_id);
                //var data1 = db.Database.SqlQuery<d_pro_unit_staff>(varname1).ToList().Count();

                if (data1 == 0)
                {
                    return Json(new { result = false, message = ec }, JsonRequestBehavior.AllowGet);
                }
                //kiem tra chua qua cong doan
                //if (db.w_material_mapping.Any(x => x.mt_cd == a.mt_cd))
                if (_iWOService.CheckwMaterialMapping(a.mt_cd) > 0)
                {
                    return Json(new { result = false }, JsonRequestBehavior.AllowGet);
                }
                //if (a != null)
                //{
                if (a.gr_qty > 0 || a.real_qty > 0)
                {
                    return Json(new { result = false , message = "Đồ đựng này đã được nhập sản lượng, Bạn không thể xóa nó!!!" }, JsonRequestBehavior.AllowGet);
                }
                int countmapping = _iWOService.Statusmappingfrommtlot(a.mt_cd);
                //var ds_mapping = db.w_material_mapping.Where(x => x.mt_lot == a.mt_cd).ToList();
                //if (ds_mapping.Any(x => x.use_yn == "N"))
                if (countmapping > 0)
                {
                    return Json(new { result = false, message = "Đã Tồn tại mã Kết thúc!! Không thể Xóa!" }, JsonRequestBehavior.AllowGet);
                }
                var ds_mapping = _iWOService.GetWMaterialMappingfrommtlot(a.mt_cd).ToList();
                //var ds_mapping = dsmapping.ToList();
                foreach (var item in ds_mapping)
                {
                    //w_material_mapping b = new w_material_mapping();
                    //try
                    //{
                    //    b = db.w_material_mapping.Find(item.wmmid);
                    //}
                    //catch(Exception e)
                    //{
                    //   w_material_mapping b = new w_material_mapping();
                    //}
                    w_material_mapping b = _iWOService.Getw_material_mappingfromwmmid(item.wmmid);


                    //kiểm trả về trạng thái cho nguyên vật liệu
                    //var check_mt = db.w_material_info.Where(x => x.mt_cd == b.mt_cd && x.mt_type != "CMT").SingleOrDefault();
                    w_material_info check_mt = _iWOService.Getw_material_infoforDelPP(b.mt_cd, "CMT");
                    if (check_mt != null)
                    {

                        //update active về bằng 0
                        //if (!db.w_material_mapping.Any(x => x.mt_cd == b.mt_cd && x.mt_lot != b.mt_lot))
                        if (_iWOService.GetcountwmaterialmappingFrmtcdamtlot(b.mt_cd, b.mt_lot) <= 0)
                        {
                            check_mt.mt_sts_cd = "001";
                            check_mt.id_actual = 0;
                            check_mt.at_no = null;
                            check_mt.product = null;
                            check_mt.staff_id = null;
                            check_mt.machine_id = null;
                        }
                        else
                        {
                            check_mt.mt_sts_cd = "002";
                        }
                        _iWOService.UpdateMaterialInfo(check_mt);
                        //db.Entry(check_mt).State = EntityState.Modified;
                        //db.SaveChanges(); // line that threw exception
                      
                    }
                    //else
                    //{
                    //    _iWOService.Deletew_material_info(check_mt.wmtid);
                    //}
                    //xóa bảng w_material_mapping
                    _iWOService.Deletewmaterialmapping(b);

                    //Log.Info("MMS=>Xoa_mt_pp_composite (Xoa mapping)" + b.mt_cd + "Lot: " + b.mt_cd);
                    //db.Entry(b).State = EntityState.Deleted;
                    //db.SaveChanges(); // line that threw exception

                    //xã bobin
                    //var find_bb1 = db.d_bobbin_info.Where(x => x.bb_no == b.bb_no).SingleOrDefault();
                    d_bobbin_info find_bb1 = _iWOService.GetBobbinInfo(b.bb_no);
                     if (find_bb1 != null)
                     {
                        find_bb1.mt_cd = null;
                        find_bb1.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        _iWOService.UpdateBobbinMTCode(find_bb1);
                        //db.SaveChanges(); // line that threw exception
                     }
                    //xóa histor
                    var find_history1 = db.d_bobbin_lct_hist.Where(x => x.bb_no == b.bb_no && x.mt_cd == b.mt_cd).SingleOrDefault();
                    if (find_history1 != null)
                    {
                        db.Entry(find_history1).State = EntityState.Deleted;
                        db.SaveChanges(); // line that threw exception
                        Log.Info("MMS=>Xoa_mt_pp_composite (Xoa BOBIN HISTORY1)mt_cd " + find_history1.mt_cd + "bobin: " + find_history1.bb_no);

                    }
                }

                db.Entry(a).State = EntityState.Deleted;
                db.SaveChanges(); // line that threw exception
                //xã bobin
                var find_bb = db.d_bobbin_info.Where(x => x.bb_no == a.bb_no).SingleOrDefault();
                if (find_bb != null)
                {
                    Log.Info("MMS=>Xoa_mt_pp_composite (thay doi mt_cd trống)" + find_bb.mt_cd);

                    find_bb.mt_cd = null;
                    find_bb.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    db.Entry(find_bb).State = EntityState.Modified;
                    db.SaveChanges(); // line that threw exception

                }
                //xóa histor
                //var find_history = db.d_bobbin_lct_hist.Where(x => x.bb_no == a.bb_no && x.mt_cd == a.mt_cd).SingleOrDefault();
                var find_history = _iWOService.GetdbobbinlcthistFrbbnoamtcd(a.bb_no, a.mt_cd);
                if (find_history != null)
                {
                    Log.Info("MMS=>Xoa_mt_pp_composite (Xoa BOBIN HISTORY2)mt_cd " + find_history.mt_cd + "bobin: " + find_history.bb_no);
                    _iWOService.Deletedbobbinlcthist(find_history.blno);
                    //db.Entry(find_history).State = EntityState.Deleted;
                    //db.SaveChanges(); // line that threw exception
                    return Json(new { result = true }, JsonRequestBehavior.AllowGet);

                }

                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
                //}
            }
            return Json(false, JsonRequestBehavior.AllowGet);
        }

        public ActionResult getmt_date_web(Pageing Pageing, int id_actual)
        {
            try
            {
               

                IEnumerable<MtDateWebWO> datas = _iWOService.GetMtDateWebs(id_actual).OrderByDescending(x => x.wmtid);
                int totalcounts = datas.Count();
                IEnumerable<MtDateWebWO> data = datas.Skip((Pageing.page - 1) * Pageing.rows).Take(Pageing.rows);
                //var aa = data.ToList();
                int totalPages = (int)Math.Ceiling((float)totalcounts / Pageing.rows);
                var jsonReturn = new
                {
                    total = totalPages,
                    page = Pageing.page,
                    records = totalcounts,
                    rows = data
                };
                return Json(jsonReturn, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult check_update_grty(string mt_cd, int wmtid, int value)
        {
            //check  không qua công đoạn trước  và chưa kiểm tra PQC
            try
            {

                var userAcount = Session["userid"] == null ? null : Session["userid"].ToString();
                var userWIP = "";
                if (!string.IsNullOrEmpty(userAcount))
                {
                    var dsMbInfo = _iWOService.GetMbInfoGrade(userAcount);
                    userWIP = dsMbInfo.grade;
                }
              
                
                if (_iWOService.CheckwMaterialMapping(mt_cd) > 0 && userWIP != "Admin" ) 
                {
                    return Json(new { result = false, message = "Đã qua công đoạn khác", wmtid = wmtid }, JsonRequestBehavior.AllowGet);
                }
               
                if (_iWOService.Checkwmfaclineqc(mt_cd) > 0 && userWIP != "Admin")
                {
                    return Json(new { result = false, message = "Đã kiểm tra PQC", wmtid = wmtid }, JsonRequestBehavior.AllowGet);
                }

               
                w_material_info data = _iWOService.GetWMaterialInfo(wmtid);
                
                int data1 = _iWOService.CheckHetCaMT((int)data.id_actual, data.staff_id);
                if (data1 == 0 && userWIP != "Admin")
                {
                    return Json(new { result = false, message = ec }, JsonRequestBehavior.AllowGet);
                }
                //kiểm tra có check apply theo Công đoạn không

                var checkYorN =  _iWOService.IsCheckApply(data.at_no);

                if (checkYorN != null )
                {
                    //kiểm tra đã scan NVL và BTP chưa
                    //nếu là level= 1 thì chỉ cần kiểm tra NVL thôi, còn level 2 trở lên là phải có BTP
                    // Lấy ra danh sách cần scan NVL cho công đoạn này

                    //lấy ra danh sách đã scan NVL không tồn tại trong NVL đã đăng kí thì return false
                    var DsProcess = _iWOService.GetWActual((int)data.id_actual);
                    var dsDaMapping = _iWOService.GetWMaterialMappingNVL(DsProcess.product, mt_cd, "PMT", DsProcess.name).ToList();
                    if (dsDaMapping.Count > 0)
                    {
                        return Json(new { result = false, message = "Bạn scan thiếu NVL" }, JsonRequestBehavior.AllowGet);
                    }
                    //kiểm tra nếu level >1 kiểm tra thêm scan đủ BTP CHƯA
                    if (DsProcess.level  > 1)
                    {
                        //LẤY TÊN BTP CỦA CÔNG ĐOẠN TRƯỚC NÓ
                        var TenBTP = _iWOService.NameBTP(data.at_no, DsProcess.level);
                        if (TenBTP != null)
                        {
                            var mt_no = TenBTP.product + "-" + TenBTP.NameProcess;
                            var isBTPExistInMapping = _iWOService.IsBTPExistByMapping(mt_cd, mt_no);

                            if (!isBTPExistInMapping)
                            {
                                return Json(new { result = false, message = "Đồ đựng này chưa scan BÁN THÀNH PHẨM"}, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }
                }

                data.gr_qty = value;
                data.real_qty = data.gr_qty;
                data.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                _iWOService.UpdateMaterialInfo(data);
                var data_ds = _iWOService.GetUpdateGrty(data.wmtid);
                int tinhtoan = _iWOService.CallSPUpdateSLwactual((int)data.id_actual);

                return Json(new { result = true, message="Thành Công", kq = data_ds, wmtid = wmtid }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false,message="Lỗi hệ thống", kq = "", wmtid = wmtid }, JsonRequestBehavior.AllowGet);
            }
        }

        #region popupmaterial_mapping

        public ActionResult GetType_Marterial(comm_mt comm_dt)
        {
            var type = db.comm_dt.ToList().Where(item => item.mt_cd == "COM004").ToList();
            return Json(type, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Popup_composite_mt(Pageing Pageing, string type, string mt_no, string mt_cd, int id_actual)
        {
            // Get Data from ajax function
            var order = "";
            if (Pageing.sidx != null)
            {
                order = "order by " + Pageing.sidx + " " + Pageing.sord;
            }
            String varname1 = "";
            varname1 = varname1 + "SELECT a.* FROM w_material_info AS a " + "\n";
            varname1 = varname1 + "WHERE a.mt_sts_cd ='001'  and (a.ExportCode IS NOT NULL AND a.ExportCode != '') ";
            //varname1 = varname1 + "WHERE (a.mt_sts_cd ='001' or a.mt_sts_cd ='002' )";

            varname1 = varname1 + "AND ('" + type + "'='' OR  a.mt_type   like '%" + type + "%' ) " + "\n";
            varname1 = varname1 + "AND ('" + mt_no + "'='' OR  a.mt_no   like '%" + mt_no + "%' ) " + "\n";
            varname1 = varname1 + "AND  ('" + mt_cd + "'='' OR  a.mt_cd   like '%" + mt_cd + "%' ) " + "\n";
            varname1 = varname1 + "AND a.lct_sts_cd ='101' and (a.id_actual IS NULL OR a.id_actual !='" + id_actual + "' )  and a.gr_qty>0 and a.mt_type!='CMT' ";

            var data = db.w_material_info.SqlQuery(varname1.ToString()).ToList<w_material_info>();
            var count = data.Count();
            int pageIndex = Pageing.page;
            int pageSize = Pageing.rows;
            int startRow = (pageIndex * pageSize) + 1;
            int totalRecords = count;
            int totalPages = (int)Math.Ceiling((float)totalRecords / (float)pageSize);
            var result = new
            {
                total = totalPages,
                page = pageIndex,
                records = count,
                rows = data.ToArray().ToPagedList(pageIndex, pageSize)
            };
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        #endregion popupmaterial_mapping

        #endregion table1

        #region table2

        public ActionResult ds_mapping_w(string mt_cd)
        {
            IEnumerable<DataMappingW> data = _iWOService.GetDataMappingW(mt_cd);
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult insertw_material_mping(w_material_mapping a, string bb_no, string mt_cd, string mt_mapping, int id_actual)
        {
            try
            {

                //mt_cd: mã lot đầu ra
                //mt_mapping: mã mt_cd đầu vào
                if (_iWOService.CheckMachineShift(id_actual))
                {
                    return Json(new { result = false, message = "Máy hết ca!Gia hạn hoặc tạo thêm máy!!" }, JsonRequestBehavior.AllowGet);
                }

                //Nếu đầu ra là bb là cái cũ nhất thì khong cho mapping
                //lấy được giá trị nếu là true thì return(tức db bằng rông or null )
                bool IsMaxBobbin = _iWOService.CountMaxBobbin(mt_cd, id_actual);
                if (IsMaxBobbin)
                {
                    return Json(new { result = false, message = "Bobbin này đã được cũ rồi vui lòng chọn Bobbin mới nhất." }, JsonRequestBehavior.AllowGet);
                }
               
                w_material_info qr = _iWOService.GetWMaterialInfowithmtcd(mt_cd);
                if (qr == null)
                {
                    return Json(new { result = false, message = "Vui Lòng Chọn Đồ Đựng Đầu Ra!!" }, JsonRequestBehavior.AllowGet);
                }
                
                if (_iWOService.CheckwMaterialMapping(mt_cd) > 0)
                {
                    return Json(new { result = false, message = "Mã lot này đã được chuyển qua công đoạn khác!!" }, JsonRequestBehavior.AllowGet);
                }
                if (String.IsNullOrEmpty(mt_mapping))
                {
                    // nếu k mapping mã nguyên vât liệu sẽ mapping mã bobbin để lấy

                    d_bobbin_info ds_bb = _iWOService.GetBobbinInfo(bb_no);
                    if (ds_bb == null)
                    {
                        return Json(new { result = false, message = "Đồ Đựng này không tồn tại trên hệ thống!!" }, JsonRequestBehavior.AllowGet);
                    }
                    mt_mapping = ds_bb.mt_cd;

                    d_bobbin_lct_hist ds_bb_his = _iWOService.GetdbobbinlcthistFrbbno(bb_no);
                    if (ds_bb_his == null)
                    {
                        return Json(new { result = false, message = "Đồ Đựng này không tồn tại trên hệ thống!!" }, JsonRequestBehavior.AllowGet);
                    }
                    mt_mapping = ds_bb_his.mt_cd;




                    if (!string.IsNullOrEmpty(mt_mapping))
                    {
                        // mapping bobin
                        ds_bb.count_number = 1;
                        ds_bb.mt_cd = ds_bb_his.mt_cd;
                        ds_bb.mt_cd = mt_mapping;
                        ds_bb.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        _iWOService.UpdateBobbinMTCode(ds_bb);

                    }
                    else
                    {
                        return Json(new { result = false, message = "Vui Lòng Chọn Đồ Đựng Đầu Ra!!" }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    //kiểm tra xem mã nvl có được đưa ra máy chưa, nếu cột ExportCode,LoctionMachine có dữ liệu thì tiếp tục, ngược lại return false
                    var NVLTaiMay = _iWOService.CheckWMaterialInfo(mt_mapping);
                    if (NVLTaiMay == null)
                    {
                        return Json(new { result = false, message = "NVL này chưa được nhập kho sản xuất!!  Mã: " + mt_mapping }, JsonRequestBehavior.AllowGet);
                    }
                    if (string.IsNullOrEmpty(NVLTaiMay.LoctionMachine))
                    {
                        return Json(new { result = false, message = "NVL này chưa được xuất ra Máy!!! Mã: " + mt_mapping }, JsonRequestBehavior.AllowGet);
                    }

                }
                //kiểm tra mã này đủ tiêu chuẩn để mapping không
             
                w_material_info mt = _iWOService.GetWMaterialInfowithmtcd(mt_mapping);

                if (mt != null)
                {
                    //kiểm tra xem Product này chọn Y/N
                 
                    //lấy product
                 var Dsproduct=  _iWOService.GetWActual(id_actual);
                 var checkYorN =  _iWOService.IsCheckApply(Dsproduct.at_no);

                    //var checkYorN = _iWOService.CheckBom(Dsproduct.product);
                    //tức là product này tồn tại apply =Y và nó là nguyên vật liệu  
                    // nếu chọn N không cần kiểm tra
                    if (checkYorN != null && mt.mt_type.Equals("PMT"))
                    {
                        //Nếu là Y thì kiểm tra NVL có thuộc NVL trong product đó không

                        //version kiểm tra NVL theo Bom
                        //var isMaterialExistInbom = _iWOService.IsMaterialInfoExistByBom(Dsproduct.product, mt.mt_no);

                        //if (!isMaterialExistInbom)
                        //{
                        //    return Json(new { result = false, message = "NVL này chưa được đăng kí trong Product " + Dsproduct.product }, JsonRequestBehavior.AllowGet);
                        //}
                        //version kiểm tra NVL theo product của từng công đoạn(Routing)

                        var isMaterialExistInProcess = _iWOService.IsMaterialInfoExistByProcess(Dsproduct.product, Dsproduct.name, mt.mt_no);

                        if (!isMaterialExistInProcess)
                        {
                            return Json(new { result = false, message = "NVL này chưa được đăng kí ở tại Product: " + Dsproduct.product +" Công đoạn:  "+ Dsproduct.name}, JsonRequestBehavior.AllowGet);
                        }
                    }

                    if (mt.mt_sts_cd != "002" && mt.mt_sts_cd != "001")
                   
                    {
                       
                        var check_trangthai = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == mt.mt_sts_cd).ToList();
                        var trangthai = string.Join(", ", check_trangthai.Select(x => x.dt_nm));

                       
                        return Json(new { result = false, message = "Trạng Thái mã này đang là: " + trangthai }, JsonRequestBehavior.AllowGet);
                    }
                    int check_exits_mapping = _iWOService.GetcountwmaterialmappingFrmtcdamtlot(mt_mapping, mt_cd);
                    if (check_exits_mapping > 0)
                    {
                        return Json(new { result = false, message = "Mã này đang được sử dụng ở công đoạn khác!!! " }, JsonRequestBehavior.AllowGet);
                    }
                    if (mt.gr_qty == 0)
                    {
                        return Json(new { result = false, message = "Sản Lượng Không có để Scan!!!" }, JsonRequestBehavior.AllowGet);
                    }
                    if (id_actual == mt.id_actual)
                    {
                        return Json(new { result = false, message = "Cùng Công Đoạn Vui Lòng Không Scan vào!!!" }, JsonRequestBehavior.AllowGet);
                    }

                    if (!mt.lct_cd.StartsWith("002"))
                    {
                        var VITRI = checkvitri(mt.lct_cd);
                        return Json(new { result = false, message = "Mã Này Đang ở KHO " + VITRI }, JsonRequestBehavior.AllowGet);
                    }

                }
                else
                {
                    return Json(new { result = false, message = "NVL này chưa được nhập kho sản xuất!!! Mã: " + mt_mapping }, JsonRequestBehavior.AllowGet);
                }





                if (qr != null && mt != null)
                {
                    var id_lot = qr.id_actual;
                    var staff = qr.staff_id;
                    //kiêm tra hết ca chưa 
                   
                    int data = _iWOService.CheckHetCaMT((int)id_lot, staff);
                    if (data < 1)
                    {
                        return Json(new { result = false, message = ec }, JsonRequestBehavior.AllowGet);
                    }

                    var id_cd = mt;
                    
                    var check_lot = _iWOService.GetWActual((int)id_lot);
                    var check_po = "";
                    if (mt.mt_type == "CMT")
                    {
                        if (check_lot.at_no == id_cd.at_no)
                        {
                            check_po = "OK";
                        }
                        if (check_po == "")
                        {
                            return Json(new { result = false, message = "Chọn Sai PO! Xin vui lòng chọn lại!" }, JsonRequestBehavior.AllowGet);
                        }
                    }
                    else
                    {

                    }
                    a.mt_no = mt.mt_no;
                   
                    if (_iWOService.Getwmaterialmappingformapp(a.mt_no, mt_mapping, mt_cd) == 0)
                    {
                        if (mt_cd == mt_mapping)
                        {
                            return Json(new { result = false, message = "Làm ơn kiểm tra lại mã lot này" }, JsonRequestBehavior.AllowGet);
                        }

                        a.mt_cd = mt_mapping;
                        a.mt_lot = mt_cd;
                        a.bb_no = bb_no;
                        a.use_yn = "Y";
                        a.bb_no = qr.bb_no;
                        a.remark = qr.remark;
                        a.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        a.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();

                        a.reg_dt = DateTime.Now;
                        a.chg_dt = DateTime.Now;
                        a.mapping_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                        int wmmid = _iWOService.InsertwMaterialMapping(a);
                      
                        var id_fo1 = mt.wmtid;
                       
                        w_material_info update1 = _iWOService.GetWMaterialInfo(id_fo1);
                        if (update1.mt_type != "CMT")
                        {
                            update1.id_actual = id_actual;
                            var staff_sx = check_staff(id_actual);
                            var machinesx = machine_sx(id_actual);
                            update1.product = check_lot.product;
                            update1.at_no = check_lot.at_no;
                            update1.staff_id = staff_sx;
                            update1.machine_id = machinesx;
                        }


                        update1.mt_sts_cd = "002";
                        update1.id_actual = update1.id_actual;
                        update1.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        _iWOService.UpdateMaterialInfo(update1);
                       
                        bool checkres = false;
                        IEnumerable<SaveReturn> datares = _iWOService.GetSaveReturn(wmmid);
                        if (datares.Count() > 0)
                            checkres = true;
                        return Json(new { data = datares, result = checkres, message = "" }, JsonRequestBehavior.AllowGet);
                      
                    }
                    else
                    {
                        return Json(new { result = false, message = "Dữ liệu đã được mapping hoặc Đang chờ vào tồn kho" }, JsonRequestBehavior.AllowGet);
                    }
                }
                return Json(new { result = false, message = "Không tồn tại !Làm ơn kiểm tra lại" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult getBomdsMaterial(string style_no, string at_no)
        {
            try
            {
                IEnumerable<WActualBom> value = _iWOService.GetListmaterialbom(style_no, at_no);

                return Json(value, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult getBomdsMaterialDetail(string product, string at_no, string mt_no,string shift_dt,string shift_name)
        {
            try
            {
                IEnumerable<WActualBom> value = _iWOService.GetListmaterialbomdetail(product, at_no, mt_no, shift_dt, shift_name);

                return Json(value, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult getBomdsMaterialDetailReplace(string product, string at_no, string mt_no, string shift_dt, string shift_name)
        {
            try
            {
                IEnumerable<WActualBom> value = _iWOService.GetListmaterialbomdetailReplace(product, at_no, mt_no, shift_dt, shift_name);

                return Json(value, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult LayDanhsachLieuThaythe(string ProductCode, string at_no, string mt_no)
        {
            try
            {
                IEnumerable<WActualBom> value = _iWOService.GetListLieuThaythe(ProductCode, at_no, mt_no);

                return Json(value, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult UpdateProductDeApply(int id_actualpr, string IsApply)
        {
            _iWOService.UpdateProductDeApply(id_actualpr, IsApply);


            return Json(new { result = true, id_actualpr, IsApply }, JsonRequestBehavior.AllowGet);
        }
        public string checkvitri(string lct_Cd)
        {
            var checkvitri = db.lct_info.Where(x => x.lct_cd == lct_Cd).ToList();
            var csv = string.Join(", ", checkvitri.Select(x => x.lct_nm));
            return csv;
        }
        public string checktrangthai(string mt_sts_cd)
        {
            var check_trangthai = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == mt_sts_cd).ToList();
            var csv = string.Join(", ", check_trangthai.Select(x => x.dt_nm));
            return csv;
        }
        public JsonResult Finish_back(int wmmid)
        {
            try
            {
               
                var data = _iWOService.Getw_material_mappingfromwmmid(wmmid);
                if (data == null)
                {
                    return Json(new { result = false, message = "Marterial không tồn tại!" }, JsonRequestBehavior.AllowGet);
                }

                #region CHECK BOBIN
                //Nếu đầu ra là bb là cái cũ nhất thì khong cho mapping
                //lấy được giá trị nếu là true thì return(tức db bằng rông or null )
                int id_actual = Convert.ToInt32(_iWOService.GetWMaterialInfowithmtcd(data.mt_lot).id_actual);
                bool IsMaxBobbin = _iWOService.CountMaxBobbin(data.mt_lot, id_actual);
                if (IsMaxBobbin)
                {
                    return Json(new { result = false, message = "Bobbin này không phải là bobbin mới nhất." }, JsonRequestBehavior.AllowGet);
                }

                #endregion

                //Nếu NVL NÀY ĐÃ BỊ RETURN THÌ KHÔNG ĐƯỢC FINISH

                if (!_iWOService.CheckMaterialRetun(data.mt_cd))
                {
                    return Json(new { result = false, message = "Marterial đã được trả về kho rồI, thao tác này không được thực hiện" }, JsonRequestBehavior.AllowGet);
                }
                var userAcount = Session["userid"] == null ? null : Session["userid"].ToString();
                var userWIP = "";
                if (!string.IsNullOrEmpty(userAcount))
                {
                  var dsMbInfo =   _iWOService.GetMbInfoGrade(userAcount);
                    userWIP = dsMbInfo.grade;
                }
              
                //int checklotmapping1 = _iWOService.CheckwmaterialmappingFinish(data.mt_cd, data.mt_lot, data.mapping_dt);
                //int checklotmapping2 = _iWOService.CheckwmaterialmappingFinish(data.mt_lot, data.mt_cd,data.mapping_dt);
                //if ( userWIP != "Admin" && (checklotmapping1 > 0 || checklotmapping2 > 0))
                //{
                //    return Json(new { result = false, message = "Mã Code này đã được kế thừa qua Lot khác!!!" }, JsonRequestBehavior.AllowGet);
                //}
                if (data.use_yn == "Y")
                {
                    var ds = _iWOService.GetWMaterialInfowithmtcd(data.mt_cd);
                    
                    var ds_2 = _iWOService.GetWMaterialInfowithmtcd(data.mt_lot);


                    if (ds != null)
                    {
                        if (ds_2 != null)
                        {
                           
                            int data2 = _iWOService.Checkdprounitstafffinishback((int)ds_2.id_actual, ds_2.staff_id);
                            if (data2 == 0 && userWIP != "Admin")
                            {
                                return Json(new { result = false, message = ec }, JsonRequestBehavior.AllowGet);
                            }
                        }


                        data.use_yn = "N";
                        data.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        _iWOService.UpdateWMaterialMapping(data);
                       
                        var id_fo1 = ds.wmtid;
                       
                        w_material_info update1 = _iWOService.GetWMaterialInfo(id_fo1);
                        update1.mt_sts_cd = "005";
                        update1.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        _iWOService.UpdateMaterialInfo(update1);
                        
                        _iWOService.UpdatedBobbinInfoforDevice(update1.bb_no, update1.mt_cd, Session["userid"] == null ? null : Session["userid"].ToString());
                        _iWOService.DeleteDBobbinLctHistforDevice(update1.bb_no, update1.mt_cd);
                       
                        return Json(new { result = true, message = "Kết thúc mã thành công!!!", use_yn = data.use_yn }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { result = false, message = "Không tìm thấy mã này" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    #region CHECK BOBIN
                    //Nếu đầu ra là bb là cái cũ nhất thì khong cho mapping
                    //lấy được giá trị nếu là true thì return(tức db bằng rông or null )
                    int id_actual1 = Convert.ToInt32(_iWOService.GetWMaterialInfowithmtcd(data.mt_lot).id_actual);
                    bool IsMaxBobbin1 = _iWOService.CountMaxBobbin(data.mt_lot, id_actual);
                    if (IsMaxBobbin)
                    {
                        return Json(new { result = false, message = "Bobbin này không phải là bobbin mới nhất." }, JsonRequestBehavior.AllowGet);
                    }

                    #endregion
                    //kiem tra da mapping voi cong doan khac chua

                    //int check_exit2 = _iWOService.CheckwMaterialMapping(data.mt_lot);
                    //if (check_exit2 > 0 && userWIP != "admin" )
                    //{
                    //    return Json(new { result = false, message = "Mã Lot này đã được chuyển qua công đoạn khác!" }, JsonRequestBehavior.AllowGet);
                    //}
                    //trở về trạng thái đầu
                    //kiểm tra trạng thái có đủ điều kiện để trở về không
                    data.use_yn = "Y";
                    data.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    _iWOService.UpdateWMaterialMapping(data);
                   
                    var ds = _iWOService.GetWMaterialInfowithmtcd(data.mt_cd);
                    if (ds != null)
                    {
                        var id_fo1 = ds.wmtid;
                        w_material_info update1 = db.w_material_info.Find(id_fo1);
                        update1.mt_sts_cd = "002";
                        update1.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        _iWOService.UpdateMaterialInfo(update1);
                        if (update1.bb_no != "" && update1.bb_no != null)
                        {
                            //xã bobin
                            
                            d_bobbin_info check_bb = _iWOService.GetBobbinInfo(update1.bb_no);
                            if (check_bb != null)
                            {
                                check_bb.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                                check_bb.mt_cd = update1.mt_cd;
                                check_bb.count_number = 1;
                                _iWOService.UpdateBobbinMTCode(check_bb);
                               
                                // //add history đã xóa
                                d_bobbin_lct_hist history = new d_bobbin_lct_hist();
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
                                _iWOService.InsertToBobbinLctHistory(history);
                            }
                        }
                        return Json(new { result = true, message = "Trở về trạng thái cũ thành công", use_yn = data.use_yn }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { result = false, message = "Không tìm thấy mã này" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult savereturn_lot(int soluong, string mt_cd, string mt_lot)
        {
            try
            {
                //mt_cd:mã đầu vào
                //mt_lot:mã đầu ra
                
                w_material_info check_exit = _iWOService.GetWMaterialInfowithmtcd(mt_cd);
                if (check_exit == null)
                {
                    return Json(new { result = false, message = "Mã này không tìm thấy!!" }, JsonRequestBehavior.AllowGet);
                }
                //nếu mt_type = "CMT" thì ko cho return
                if (check_exit.mt_type == "CMT")
                {
                    return Json(new { result = false, message = "Mã này là bán thành phẩm, không được trả về." }, JsonRequestBehavior.AllowGet);
                }
                
                var check_exit1 = _iWOService.Getmaterialmappingreturn(mt_cd, mt_lot);
                if (check_exit1 == null)
                {
                    return Json(new { result = false, message = "Mã này không tìm thấy!!" }, JsonRequestBehavior.AllowGet);
                }
                //kiểm tra đã finish chưa
                if (check_exit1.use_yn == "N")
                {
                    return Json(new { result = false, message = "Mã này đã kết thúc rồi!" }, JsonRequestBehavior.AllowGet);
                }
                if (soluong == 0)
                {
                    return Json(new { result = false, message = "Nhập số lượng trả về phải lớn hơn 0" }, JsonRequestBehavior.AllowGet);
                }
                //kiểm tra số lượng có vượt quá k

                if (soluong >= check_exit.gr_qty)
                {
                    return Json(new { result = false, message = "Số lượng trả về phải ít hơn số lượng tồn tại!" }, JsonRequestBehavior.AllowGet);
                }
              
                //Nếu đầu ra là nguyên vật liệu là cái mới nhất thì cho return
                //lấy được giá trị nếu là true thì return(tức db bằng rông or null )
                bool IsMaxMapping = _iWOService.CountMaxMapping(mt_cd, mt_lot);
                if (IsMaxMapping)
                {
                    return Json(new { result = false, message = "Mã này đã được kế thừa rồi, không thể trả về!!!" }, JsonRequestBehavior.AllowGet);
                }
                //tách số lượng
                var soluongcl_mt_cd = check_exit.gr_qty - (soluong);
                //finish
                check_exit1.use_yn = "N";
                check_exit1.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                _iWOService.UpdateWMaterialMapping(check_exit1);
               
                // chuyển trạng thái về 005 và update số lượng đã sử dụng
                check_exit.mt_sts_cd = "005";
                check_exit.gr_qty = soluongcl_mt_cd;
                check_exit.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                _iWOService.UpdateMaterialInfo(check_exit);
                
                //tạo mã mới cho số lượng còn dư và có trạng thái là return
                //mt_sts_cd=004,sts_update='return',soluong=input,orgin=mt_cd
                int count_return = _iWOService.GetwmaterialinfoforDelPP(mt_cd, "return");
                string sqlin = @"INSERT INTO w_material_info(at_no,product,mt_type,mt_cd,mt_no,gr_qty,rd_no,sd_no,recevice_dt,
                    date,return_date,alert_NG,expiry_dt,dt_of_receipt,expore_dt,recevice_dt_tims,lot_no,mt_barcode,
                    mt_qrcode,mt_sts_cd,bb_no,bbmp_sts_cd,lct_cd,lct_sts_cd,from_lct_cd,to_lct_cd,output_dt,input_dt,
                    orgin_mt_cd,remark,sts_update,use_yn,reg_id,reg_dt,chg_id,chg_dt,real_qty,ExportCode,LoctionMachine,ShippingToMachineDatetime,rece_wip_dt  ) 
                SELECT NULL,NULL,mt_type,CONCAT( mt_cd,'-', @1 ),mt_no,@2,rd_no,sd_no,recevice_dt,date,@3,alert_NG,
                expiry_dt,dt_of_receipt,expore_dt,recevice_dt_tims,lot_no,CONCAT( mt_cd, '-', @1 ),CONCAT( mt_cd, '-', @1 ),'004',bb_no,bbmp_sts_cd,
                lct_cd,lct_sts_cd,from_lct_cd,to_lct_cd,output_dt,input_dt,mt_cd,remark,'return',use_yn,@4,NOW(),@4,NOW(),@2 ,ExportCode,LoctionMachine,ShippingToMachineDatetime,rece_wip_dt
                FROM
	                w_material_info 
                WHERE
	                mt_cd = @5;";
                db.Database.ExecuteSqlCommand(sqlin, new MySqlParameter("@1", "RT" + (count_return + 1)), new MySqlParameter("@2", soluong)
                    , new MySqlParameter("@3", DateTime.Now.ToString("yyyyMMddHHmmss")), new MySqlParameter("@4", check_exit.chg_id),
                    new MySqlParameter("@5", mt_cd));
              
                if (check_exit.bb_no != "" && check_exit.bb_no != null)
                {
                    //xã bobin
                    
                    var check_bb = _iWOService.GetBobbinInfoReturn(check_exit.bb_no, check_exit.mt_cd);
                    if (check_bb != null)
                    {
                        check_bb.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        check_bb.mt_cd = mt_cd + "-" + "RT" + (count_return + 1);
                        
                        _iWOService.UpdateBobbinMTCode(check_bb);
                       

                        //find history để xóa
                        
                        var history = _iWOService.GetdbobbinlcthistFrbbnoamtcd(check_bb.bb_no, mt_cd);
                        if (history != null)
                        {
                            history.mt_cd = check_bb.mt_cd;
                            _iWOService.Updatedbobbinlcthist(history);
                          
                        }
                    }
                }

                //kiem tra co bao nhieu mt_cd da qua cong doan ma chua finish

               
                _iWOService.UpdatewmaterialMappingmtcduseyn(check_exit.mt_cd, "Y", "N", Session["userid"] == null ? null : Session["userid"].ToString());
               
                IEnumerable<SaveReturn> data = _iWOService.GetSaveReturn(check_exit1.wmmid);
                bool checkc = false;
                if (data.Count() > 0)
                    checkc = true;
                return Json(new { data = data, result = checkc, message = "" }, JsonRequestBehavior.AllowGet);
                
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
            }
        }


        public JsonResult Cancel_mapping(int wmmid)
        {
            try
            {
               
                var check_exits = _iWOService.Getw_material_mappingfromwmmid(wmmid);
                if (check_exits == null)
                {
                    return Json(new { result = false, message = "Mã này không tìm thấy!!" }, JsonRequestBehavior.AllowGet);
                }
                
                var find_mt_cd = _iWOService.GetWMaterialInfowithmtcd(check_exits.mt_cd);
                if (find_mt_cd == null)
                {
                    return Json(new { result = false, message = "Mã này không tìm thấy!!" }, JsonRequestBehavior.AllowGet);
                }
                if (find_mt_cd.mt_sts_cd.Equals("005"))
                {
                    return Json(new { result = false, message = "Mã này đã kết thúc rồi, bạn không thể hủy nó!!" }, JsonRequestBehavior.AllowGet);
                }

     
               
                if (_iWOService.CheckwMaterialMapping(check_exits.mt_lot) > 0)
                {
                    return Json(new { result = false, message = "Mã này đã được kết thúc hoặc đã qua công đoạn khác!!!" }, JsonRequestBehavior.AllowGet);
                }
                
                int check_exits_mapping = _iWOService.GetcountwmaterialmappingFrmtcdamtlot(check_exits.mt_cd, check_exits.mt_lot);
                if (check_exits_mapping > 0)
                {
                    return Json(new { result = false, message = "Mã này đang được sử dụng!!! " }, JsonRequestBehavior.AllowGet);
                }
              
                int check_exits_return = _iWOService.Checkexitsreturn(check_exits.mt_cd, "return");
                if (check_exits_return > 0)
                {
                    return Json(new { result = false, message = "Mã này đã được bắt đầu sản xuất!!" }, JsonRequestBehavior.AllowGet);
                }
              


                //kiêm tra hết ca chưa 
                if (find_mt_cd.mt_type != "CMT")
                {
                   
                    var find_lot = _iWOService.GetWMaterialInfowithmtcd(check_exits.mt_lot);

                   
                    int data = _iWOService.Checkdprounitstafffinishback((int)find_lot.id_actual, find_lot.staff_id);
                    if (data == 0)
                    {
                        return Json(new { result = false, message = ec }, JsonRequestBehavior.AllowGet);
                    }
                }
                //tra về trạng thái cũ

                //kiểm trả về trạng thái cho nguyên vật liệu
                var sts = "002";
               
                if (_iWOService.CheckwMaterialMapping(find_mt_cd.mt_cd) > 0 && find_mt_cd.mt_type != "CMT")
                {
                    sts = "001";
                }
                //neu nhu la nguyen vat lieu thi xoa id_actual 

                find_mt_cd.id_actual = find_mt_cd.mt_type != "CMT" ? 0 : find_mt_cd.id_actual;
                find_mt_cd.at_no = find_mt_cd.mt_type != "CMT" ? null : find_mt_cd.at_no;
                find_mt_cd.product = find_mt_cd.mt_type != "CMT" ? null : find_mt_cd.product;
                find_mt_cd.staff_id = find_mt_cd.mt_type != "CMT" ? null : find_mt_cd.staff_id;
                find_mt_cd.machine_id = find_mt_cd.mt_type != "CMT" ? null : find_mt_cd.machine_id;

                find_mt_cd.mt_sts_cd = sts;
                find_mt_cd.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                _iWOService.UpdateMaterialInfo(find_mt_cd);
                

                //trả lại trạng thái cũ và xóa khỏi bảng mapping
                _iWOService.Deletewmaterialmapping(check_exits);
               
                return Json(new { result = true, message = "Trở về thành công" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult PartialView_dialog_Viewdetaildatetime(int id_actual)
        {
            ViewBag.id_actual = id_actual;
            return PartialView();
        }
        #endregion table2

        #endregion mapping

        #region Device

        public ActionResult getmt_date_web_auto(int id_actual)
        {
            var data = (from a in db.w_material_info
                        where a.id_actual == id_actual && a.mt_type == "CMT" &&
                        a.orgin_mt_cd == null
                        orderby a.reg_dt descending
                        select new
                        {
                            wmtid = a.wmtid,
                            date = a.date,
                            mt_cd = a.mt_cd,
                            mt_no = a.mt_no,
                            gr_qty = a.gr_qty,
                            bbmp_sts_cd = db.comm_dt.Where(x => x.mt_cd == "MMS007" && x.dt_cd == a.bbmp_sts_cd).Select(x => x.dt_nm),
                            mt_qrcode = a.mt_qrcode,
                            lct_cd = db.lct_info.Where(item => item.lct_nm.StartsWith("FAC") && item.lct_cd == a.lct_cd).FirstOrDefault().lct_nm,
                            bb_no = a.bb_no,
                            mt_barcode = a.mt_barcode,
                            chg_dt = a.chg_dt,
                            count_table2 = (db.w_material_mapping.Where(item => item.mt_lot == a.mt_cd).Count()),
                        }).ToList().AsEnumerable().Select(x => new
                        {
                            wmtid = x.wmtid,
                            date = x.date.Substring(0, 4) + "-" + x.date.Substring(4, 2) + "-" + x.date.Substring(6, 2),
                            mt_cd = x.mt_cd,
                            mt_no = x.mt_no,
                            gr_qty = x.gr_qty,
                            gr_qty1 = x.gr_qty,
                            bbmp_sts_cd = x.bbmp_sts_cd,
                            mt_qrcode = x.mt_qrcode,
                            lct_cd = x.lct_cd,
                            bb_no = x.bb_no,
                            mt_barcode = x.mt_barcode,
                            chg_dt = x.chg_dt,
                            sl_tru_ng = (db.w_material_info.Where(m => m.orgin_mt_cd == x.mt_cd && (m.mt_cd.StartsWith(x.mt_cd + "-RT") || m.mt_cd.StartsWith(x.mt_cd + "-DV"))).Sum(m => m.gr_qty)),
                            count_table2 = (db.w_material_mapping.Where(item => item.mt_lot == x.mt_cd).Count()),
                        }).ToList();
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Decevice_sta(string mt_cd, string number_dv)
        {
            try
            {
               
                w_material_info find_mtcd = _iWOService.GetmaterialinfoforDevice(mt_cd, "composite", "002", "101");
               
                if (find_mtcd == null)
                {
                    return Json(new { result = false, message = "Mã này không tìm thấy hoặc mã  này không đủ điều kiện để chia ra!!!" }, JsonRequestBehavior.AllowGet);
                }
                if (String.IsNullOrEmpty(number_dv))
                {
                    return Json(new { result = false, message = "Số lượng chia rỗng!!!" }, JsonRequestBehavior.AllowGet);
                }

                
                int data1 = _iWOService.GetDProUnitStaffforDevice((int)find_mtcd.id_actual);
                if (data1 == 0)
                {
                    return Json(new { result = false, message = ec }, JsonRequestBehavior.AllowGet);
                }

                // INSert DATA
                double sss = Convert.ToDouble(find_mtcd.gr_qty ?? 0) / Convert.ToDouble(number_dv);
                var so_luong = Math.Ceiling(sss);
                var so_cl = so_luong;
                var gr_cl = find_mtcd.gr_qty;

                //update sô lượng về 0 và trang thái về 005
                find_mtcd.mt_sts_cd = "005";
                find_mtcd.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                find_mtcd.chg_dt = DateTime.Now;
                find_mtcd.gr_qty = 0;
                var orgin_mt_cd = find_mtcd.mt_cd;
                
                _iWOService.UpdateMaterialInfo(find_mtcd);


             
                _iWOService.UpdatedBobbinInfoforDevice(find_mtcd.bb_no, find_mtcd.mt_cd, Session["userid"] == null ? null : Session["userid"].ToString());
                _iWOService.DeleteDBobbinLctHistforDevice(find_mtcd.bb_no, find_mtcd.mt_cd);
                int count = _iWOService.GetTotalwMaterialInfoDV(mt_cd + "-DV");
                
                for (int i = 0; i < so_luong; i++)
                {
                    //insert vao so lượng đã chia
                    var sl_chia = number_dv;
                    if (Convert.ToInt32(gr_cl) < Convert.ToInt32(number_dv))
                    {
                        sl_chia = gr_cl.ToString();
                    }
                    find_mtcd.reg_dt = DateTime.Now;
                    find_mtcd.mt_sts_cd = "002";
                    find_mtcd.orgin_mt_cd = orgin_mt_cd;
                    find_mtcd.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    find_mtcd.chg_dt = DateTime.Now;
                    find_mtcd.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                   
                    find_mtcd.mt_cd = mt_cd + "-DV" + (count + 1);
                    find_mtcd.product = find_mtcd.product;
                    find_mtcd.at_no = find_mtcd.at_no;
                    find_mtcd.mt_barcode = find_mtcd.mt_cd;
                    find_mtcd.mt_qrcode = find_mtcd.mt_cd;
                    find_mtcd.gr_qty = Convert.ToInt32(sl_chia);
                    find_mtcd.real_qty = Convert.ToInt32(sl_chia);
                    find_mtcd.bb_no = "";
                    so_cl = so_cl - 1;
                    gr_cl = gr_cl - find_mtcd.gr_qty;
                    

                    int b = _iWOService.InsertToWmaterialInfo(find_mtcd);
                    
                    //add vào mapping
                    var c = new w_material_mapping();
                    c.mt_lot = find_mtcd.mt_cd;
                    c.mt_no = find_mtcd.mt_no;
                    c.mt_cd = mt_cd;
                    c.mapping_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                    c.bb_no = find_mtcd.bb_no;
                    c.remark = find_mtcd.remark;
                    c.use_yn = "N";
                    c.chg_dt = DateTime.Now;

                    c.reg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    c.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    c.reg_dt = DateTime.Now;
                    int a = _iWOService.InsertwMaterialMapping(c);
                    count++;

                   
                }

              
                return Json(new { result = true, message = "Thành công!!!", kq = find_mtcd }, JsonRequestBehavior.AllowGet);
            }
            catch(Exception e) 
            {
                return Json(new { result = true, message = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
            }
            
            
        }
        public JsonResult DestroyBobbinMgt(string bobin, string mt_cd)
        {
            try
            {
                var kq = Detroy_Container(bobin, mt_cd);
                if (kq == "ok")
                {
                    return Json(new { result = true, message = "Thành công!!!" }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = kq + " Không thể xả!!!" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = true, message = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
            }


        }
        public string Detroy_Container(string bobin, string mt_cd)
        {
            //kiểm tra xem gr_qty=0 chưa
            var check_mt_cd = db.w_material_info.Where(x => x.mt_cd == mt_cd).SingleOrDefault();

            if (check_mt_cd.mt_cd != null)
            {
                var kho = (check_mt_cd.lct_cd == "002000000000000000" ? "MMS" : "TIMS");
                var bobin_primary = db.d_bobbin_info.Where(x => x.bb_no == bobin && x.mt_cd == mt_cd).SingleOrDefault();
                var bobin_history = db.d_bobbin_lct_hist.Where(x => x.bb_no == bobin && x.mt_cd == mt_cd).SingleOrDefault();
                var mtCode = db.w_material_info.Where(x => x.bb_no == bobin && x.mt_cd == mt_cd).SingleOrDefault();
                if (mtCode != null)
                {
                    mtCode.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    mtCode.mt_sts_cd = "005";
                    db.Entry(mtCode).State = EntityState.Modified;
                    db.SaveChanges(); // line that threw exception
                }
                if (bobin_primary != null)
                {
                    bobin_primary.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                    bobin_primary.mt_cd = null;
                    db.Entry(bobin_primary).State = EntityState.Modified;
                    db.SaveChanges(); // line that threw exception
                }
                if (bobin_history != null)
                {
                    db.Entry(bobin_history).State = EntityState.Deleted;
                    db.SaveChanges(); // line that threw exception

                }
                else
                {
                    return "Không tìm thấy đồ đựng này!!";
                }
                return "ok";

            }
            return "";
        }
        public JsonResult update_actual_mms()
        {
            var danhsach = db.w_actual.ToList();
            foreach (var item in danhsach)
            {
                //update actual cho công nhân
                StringBuilder update_actual_pr = new StringBuilder();
                update_actual_pr.Append(" CALL `Update_SL_w_actual`(" + item.id_actual + ")  \n");
                int tinhtoan = new Excute_query().Execute_NonQuery(update_actual_pr);

            }

            return Json("", JsonRequestBehavior.AllowGet);

        }
        public string Xa_Container(string bobin, string mt_cd, double gr_qty)
        {
            //kiểm tra xem gr_qty=0 chưa
            var check_mt_cd = db.w_material_info.Where(x => x.mt_cd == mt_cd).SingleOrDefault();

            if (gr_qty == 0 && check_mt_cd.mt_cd != null)
            {
                var a = db.d_bobbin_info.Where(x => x.bb_no == bobin && x.mt_cd == mt_cd).ToList();
                if (a.Count() > 0)
                {
                    foreach (var item in a)
                    {
                        item.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                        item.mt_cd = null;
                        db.Entry(item).State = EntityState.Modified;
                        db.SaveChanges(); // line that threw exception
                    }

                }
                //xã bobin
                var b = db.d_bobbin_lct_hist.Where(x => x.bb_no == bobin && x.mt_cd == mt_cd).ToList();
                if (b.Count > 0)
                {
                    foreach (var item in b)
                    {
                        db.Entry(item).State = EntityState.Deleted;
                        db.SaveChanges(); // line that threw exception
                    }
                }
                return "ok";

            }
            return "";
        }
        public ActionResult ds_mapping_sta(string mt_cd)
        {
            
            IEnumerable<DSMappingSTA> data = _iWOService.GetDSMappingSta(mt_cd + "-DV%");
           
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public JsonResult destroyDevide(string mt_cd)
        {
            var check_mt_cd = _iWOService.GetWMaterialInfowithmtcd(mt_cd);
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
           
            if (!_iTIMSService.CheckWMaterialMapForDV(mt_cd))
            {
                return Json(new { result = false, message = "Mã Devide đã được chuyển qua công đoạn khác !! " }, JsonRequestBehavior.AllowGet);
            }
            
            if (!_iTIMSService.CheckWMaterialMapForDV(mt_cd))
            {
                return Json(new { result = false, message = Constant.Other }, JsonRequestBehavior.AllowGet);
            }
            //kiểm tra đã gộp chưa
            // xử lí
            StringBuilder varname1 = new StringBuilder();
           
            int CheckExistMappig = _iTIMSService.CheckWMaterialMappingForRedo(mt_cd);
            if (CheckExistMappig > 0)
            {
                return Json(new { result = false, message = "Đã được gộp lot nên không thể trở về !! " }, JsonRequestBehavior.AllowGet);
            }
            
            IEnumerable<WMaterialnfoDV> dsach = _iTIMSService.DSWMaterialDV(mt_cd);
            var ketqua = dsach.Select(x => x.blno);
            var blno = string.Join(",", ketqua);
            var ketqua1 = dsach.Select(x => x.bno);
            var bno = string.Join(",", ketqua1);
            var gr_qty = _iTIMSService.SumGrqtyDSWMaterialDV(mt_cd);
           
            _iTIMSService.UpdateWMaterialQtyForRedo(gr_qty, check_mt_cd.mt_cd);
            //xoa mã devide
            
            _iTIMSService.DeleteWMaterialQtyForRedo(check_mt_cd.mt_cd);
            //Xóa mã devedie mapping
           
            _iTIMSService.DeleteWMaterialMappingForRedo(check_mt_cd.mt_cd);
            //XÓA BOBIN HISTORY DEVIDE
            if (blno != "")
            {
                
                _iTIMSService.DeleteBoBbinHisForRedo(blno);

            }
            if (bno != "")
            {
                //UPDATE BOBIN  DEVIDE
               
                _iTIMSService.UpdateBobbinInfoForRedo(bno);
            }
            //khoi phuc bb
           
            int effect_rows = _iTIMSService.CallSPkhoiphucbobin(check_mt_cd.mt_cd, check_mt_cd.bb_no);
            return Json(new { result = true, message = Constant.Success, gr_qty = gr_qty, wmtid = check_mt_cd.wmtid }, JsonRequestBehavior.AllowGet);
        }
        #endregion Device

        #endregion Web

        #region SỬ DỤNG CHUNG

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

        public JsonResult GetJsonPersons(DataTable data)
        {
            var lstPersons = GetTableRows(data);
            return Json(lstPersons, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ConvertDataTableToJson(DataTable data)
        {
            return Json(GetTableRows(data), JsonRequestBehavior.AllowGet);
        }

        public List<Dictionary<string, object>> GetTableRows(DataTable data)
        {
            var lstRows = new List<Dictionary<string,
             object>>();
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
            var data = new DataTable();
            using (var cmd = db.Database.Connection.CreateCommand())
            {
                db.Database.Connection.Open();
                cmd.CommandText = viewSql;
                using (var reader = cmd.ExecuteReader())
                {
                    try
                    {
                       
                        DataTable DTSchema = reader.GetSchemaTable();
                        
                        if (DTSchema != null)
                            if (DTSchema.Rows.Count > 0)
                                for (int i = 0; i < DTSchema.Rows.Count; i++)
                                {
                                   
                                    DataColumn Col = new DataColumn(DTSchema.Rows[i]["ColumnName"].ToString(), (Type)DTSchema.Rows[i]["DataType"]);
                                    Col.AllowDBNull = true;
                                    Col.Unique = false;
                                    Col.AutoIncrement = false;
                                    data.Columns.Add(Col);
                                }
                        while (reader.Read())
                        {
                            
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

        #endregion SỬ DỤNG CHUNG

        #region Print NG

        public ActionResult Print_NG()
        {
            return SetLanguage("~/Views/ActualWO/PrintNG/PrintNG.cshtml");
            //HttpCookie cookie = HttpContext.Request.Cookies["language"];
            //if (cookie != null)
            //{
            //    ViewBag.language = cookie.Value;
            //}
            //return View();
        }

        public JsonResult GetMaterialNG(Pageing paging, string code, string at_no, string product)
        {
            StringBuilder sql = new StringBuilder($"CALL spActualWO_GetMaterialNG('{code}','{at_no}','{product}');");
            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);
            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("Id"));
            return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
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
            return View("~/Views/ActualWO/PrintNG/QRPrintNG.cshtml");
        }

        public JsonResult QRMaterialNGPrint(string id)
        {
            if (id != "")
            {
                StringBuilder sql = new StringBuilder($"SELECT * FROM view_printng WHERE Id IN ({id}); ");
                return new InitMethods().ConvertDataTableToJsonAndReturn(sql);
            }
            return null;
        }

        public JsonResult CountingMaterialNG()
        {
            StringBuilder sql = new StringBuilder($"SELECT COUNT(wmtid) as Total FROM w_material_info where alert_NG = '1' and mt_sts_cd = '003' and lct_cd like '002%';");
            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);
            List<string> list = new List<string>(dt.Rows.Count);
            foreach (DataRow row in dt.Rows)
                list.Add(row["Total"].ToString());
            string total = list.Select(x => x).FirstOrDefault();
            return Json(int.Parse(total), JsonRequestBehavior.AllowGet);
        }

        public JsonResult Change_OK_NG(string mt_cd, double gr_qty, string reason)
        {
            try
            {
                var find_ng = db.w_material_info.Where(x => x.mt_cd == mt_cd && x.mt_sts_cd == "003").SingleOrDefault();
                if (find_ng == null)
                {
                    return Json(new { result = false, message = "Không tìm thấy mã này" }, JsonRequestBehavior.AllowGet);
                }
                if (find_ng.gr_qty < gr_qty)
                {
                    return Json(new { result = false, message = "Số lượng này lớn hơn số lượng tồn tại!!" }, JsonRequestBehavior.AllowGet);
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
                    //tạo mã ok mới dựa trên mã ng
                    var check = db.w_material_info.Where(x => x.mt_cd.StartsWith(find_ng.mt_cd + "-OK")).ToList();
                    var dem = (check.Count > 0 ? check.Count().ToString() : "");
                    var ok = new w_material_info();
                    ok.id_actual = find_ng.id_actual;
                    ok.orgin_mt_cd = find_ng.mt_cd;
                    ok.gr_qty = gr_qty;
                    ok.real_qty = ok.gr_qty;
                    ok.mt_cd = find_ng.mt_cd + "-OK" + dem;
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
                    return Json(new { result = true, kq = ok }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!" }, JsonRequestBehavior.AllowGet);
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
                    return Json(new { result = false, message = "Không tìm thấy mã này" }, JsonRequestBehavior.AllowGet);
                }
                if (find_lot.mt_sts_cd != "012")
                {
                    return Json(new { result = false, message = "Trạng thái đã thay đổi!!" }, JsonRequestBehavior.AllowGet);
                }
                //kiếm con NG cũ
                var find_lotng = db.w_material_info.Where(x => x.mt_cd == (find_lot.orgin_mt_cd)).SingleOrDefault();
                if (find_lot == null)
                {
                    return Json(new { result = false, message = "Không tìm thấy mã này" }, JsonRequestBehavior.AllowGet);
                }
                //updtae lại số lượng ng
                find_lotng.gr_qty = find_lotng.gr_qty + find_lot.gr_qty;
                find_lotng.chg_id = Session["userid"] == null ? null : Session["userid"].ToString();
                db.Entry(find_lotng).State = EntityState.Modified;
                db.SaveChanges();
                //xóa mã ok
                db.Entry(find_lot).State = EntityState.Deleted;
                db.SaveChanges();
                return Json(new { result = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ thống!!!" }, JsonRequestBehavior.AllowGet);
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

        public JsonResult GetTIMSMaterialOK(Pageing paging, string code, string name, string process, string workerId)
        {
            StringBuilder varname1 = new StringBuilder();
            varname1.Append("SELECT * \n");
            varname1.Append("FROM w_material_info AS a \n");
            varname1.Append("WHERE `a`.`mt_sts_cd` = '012'  AND `a`.`lct_cd` LIKE '002%'");
            DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);
            int total = dt.Rows.Count;
            var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("wmtid"));
            return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
        }
        public ActionResult PartialView_Phan_loai_QC(string item_vcd)
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
            return PartialView("~/Views/ActualWO/PrintNG/PartialView_Phan_loai_QC.cshtml", qc_itemcheck_mt);

        }
        public ActionResult InsertQcPhanLoai(List<Insert_MPO_QC_Model> model, string ml_no, string date_ymd, int gr_qty, string item_vcd, m_facline_qc MFQC, m_facline_qc_value MFQCV)
        {
            #region insert info

            var check = db.m_facline_qc.Where(x => x.ml_no == ml_no && x.fq_no.StartsWith("FQ")).SingleOrDefault();
            DateTime reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
            String dateString = reg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
            DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
            String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
            if (check == null)//Nếu mã ml_no đã tồn tại trong bảng m_facline_qc rồi thì update
            {
                var list = db.m_facline_qc.Where(x => x.fq_no.StartsWith("FQ")).OrderBy(x => x.fq_no).ToList();
                if (list.Count == 0)
                {
                    MFQC.fq_no = "FQ000000001";
                }
                else
                {
                    var menuCd = string.Empty;
                    var subMenuIdConvert = 0;
                    var list1 = list.LastOrDefault();

                    var bien1 = list1.fq_no;
                    var subMenuId = bien1.Substring(bien1.Length - 9, 9);
                    int.TryParse(subMenuId, out subMenuIdConvert);
                    menuCd = "FQ" + string.Format("{0}{1}", menuCd, CreateFQ((subMenuIdConvert + 1)));
                    MFQC.fq_no = menuCd;
                }

                MFQC.check_qty = gr_qty;
                MFQC.ml_no = ml_no;
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

                                    db.Entry(MFQCV).State = EntityState.Added;
                                    db.SaveChanges();
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

                                    MFQCV.reg_dt = reg_dt;
                                    MFQCV.chg_dt = chg_dt;

                                    MFQCV.reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                                    MFQCV.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();

                                    db.Entry(MFQCV).State = EntityState.Added;
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

        public ActionResult Delete_Qc_valuePhanLoai(int id)
        {
            try
            {
                if (id > 0)
                {
                    var isExist_QCValue = db.m_facline_qc_value.Find(id);
                    if (isExist_QCValue == null)
                    {
                        return Json(new { result = false, message = "Không tìm thấy dữ liệu" }, JsonRequestBehavior.AllowGet);
                    }
                    db.Entry(isExist_QCValue).State = EntityState.Deleted;
                    db.SaveChanges();

                    var soluongdefect = (db.m_facline_qc_value.Where(x => x.fq_no == isExist_QCValue.fq_no).ToList().Sum(x => x.check_qty));

                    //update check_Qty [m_facline_qc]
                    var isExist_QC = db.m_facline_qc.Where(x => x.fq_no == isExist_QCValue.fq_no).SingleOrDefault();
                    isExist_QC.check_qty = soluongdefect;
                    isExist_QC.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    db.Entry(isExist_QC).State = EntityState.Modified;
                    db.SaveChanges();


                    var listp_mpo_qc = (from p in db.m_facline_qc

                                        where p.fqno == isExist_QC.fqno
                                        select new
                                        {
                                            fqno = p.fqno,
                                            fq_no = p.fq_no,
                                            check_qty = p.check_qty,
                                            ml_no = p.ml_no,

                                        }).FirstOrDefault();

                    return Json(new { result = true, data = listp_mpo_qc }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { result = false, message = "Id rỗng" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = "Lỗi hệ thống" }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion Print NG

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        #region ATM

        public ActionResult ATM()
        {
            HttpCookie cookie = HttpContext.Request.Cookies["language"];
            if (cookie != null)
            {
                ViewBag.language = cookie.Value;
            }
            return View();
        }

        public ActionResult GetData_atm(Pageing pageing)
        {


            var list = (from a in db.w_actual
                        join b in db.w_actual_primary
                        on a.at_no equals b.at_no
                        where a.type.Equals("SX")

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
                varname1.Append("SELECT any_value(`v`.`id_actual`)id_actual, ");
                varname1.Append("  SUM(`v`.`m_lieu`)m_lieu,SUM(`v`.`actual`)actual,MAX(`v`.`product`)product, ");
                varname1.Append("  COUNT(`v`.`reg_dt`) AS `count_day`,MAX(`v`.`product_nm`) product_nm,");
                varname1.Append("  MIN(`v`.`reg_dt`)min_day,MAX(`v`.`reg_dt`)max_day,");
                varname1.Append("  ROUND((((SUM(`v`.`actual`) * any_value(`v`.`need_m`)) / SUM(`v`.`m_lieu`)) * 100),2) AS `HS` ");

                varname1.Append(" from (  ");
                varname1.Append("SELECT b.id_actual,b.m_lieu,b.actual,b.product,b.reg_dt,b.product_nm,b.need_m from atm_mms as b  ");
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
                varname1.Append("SELECT * from atm_mms as b  ");
                varname1.Append("where ('" + product_id + "'='' OR b.product LIKE '%" + product_id + "%') ");
                varname1.Append("AND ('" + name_id + "'='' OR b.product_nm LIKE '%" + name_id + "%') ");
                varname1.Append("AND ('" + at_id + "'='' OR b.at_no LIKE '%" + at_id + "%') ");
                varname1.Append("AND ('" + reg_dt + "'='' OR DATE_FORMAT(b.reg_dt,'%Y/%m/%d') >= DATE_FORMAT('" + reg_dt + "','%Y/%m/%d') )");
                varname1.Append("AND ('" + reg_dt_end + "'='' OR DATE_FORMAT(b.reg_dt,'%Y/%m/%d') <= DATE_FORMAT('" + reg_dt_end + "','%Y/%m/%d') )");
                DataTable dt = new InitMethods().ReturnDataTableNonConstraints(varname1);

                int total = dt.Rows.Count;
                var result = dt.AsEnumerable().OrderByDescending(x => x.Field<string>("at_no"));
                return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
            }
            return Json(new { result = false, message = "Lỗi hệ Thống!" }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult Daily()
        {
            string[] keys = Request.Form.AllKeys;
            var value = Request.Form[keys[0]];
            var value2 = Request.Form[keys[1]];
            var value3 = Request.Form[keys[2]];
            var value4 = Request.Form[keys[3]];
            var value5 = Request.Form[keys[4]];
            ViewBag.at_no = (value == "undefined" ? "" : value);
            ViewBag.tieude = (value == "undefined" ? "" : value + "-") + value3;
            ViewBag.id_actual = value2;
            ViewBag.product = value3;
            ViewBag.reg_dt = value4;
            ViewBag.reg_dt_end = value5;
            return View("~/Views/ActualWO/Daily.cshtml");
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
                    varname1.Append("SELECT any_value(`v`.`id_actual`)id_actual, ");
                    varname1.Append("  SUM(`v`.`m_lieu`)m_lieu,SUM(`v`.`actual`)actual,MAX(`v`.`product`)product, ");
                    varname1.Append("  COUNT(`v`.`reg_dt`) AS `count_day`,MAX(`v`.`product_nm`) product_nm,");
                    varname1.Append("  MIN(`v`.`reg_dt`)min_day,MAX(`v`.`reg_dt`)max_day,");
                    varname1.Append("  ROUND((((SUM(`v`.`actual`) * any_value(`v`.`need_m`)) / SUM(`v`.`m_lieu`)) * 100),2) AS `HS` ");

                    varname1.Append(" from (  ");
                    varname1.Append("SELECT b.id_actual,b.m_lieu,b.actual,b.product,b.reg_dt,b.product_nm,b.need_m from atm_mms as b  ");
                    varname1.Append("where ('" + product + "'='' OR b.product LIKE '%" + product + "%') ");
                    varname1.Append("AND ('" + reg_dt + "'='' OR DATE_FORMAT(b.reg_dt,'%Y/%m/%d') >= DATE_FORMAT('" + reg_dt + "','%Y/%m/%d') )");
                    varname1.Append("AND ('" + reg_dt_end + "'='' OR DATE_FORMAT(b.reg_dt,'%Y/%m/%d') <= DATE_FORMAT('" + reg_dt_end + "','%Y/%m/%d') ))v GROUP BY `v`.`product`");
                }
                else
                {
                    type = "at_no";
                    varname1.Append("SELECT * from atm_mms as b  ");
                    varname1.Append("where ('" + at_no + "'='' OR b.at_no LIKE '%" + at_no + "%') ");
                    varname1.Append("AND ('" + reg_dt + "'='' OR DATE_FORMAT(b.reg_dt,'%Y/%m/%d') >= DATE_FORMAT('" + reg_dt + "','%Y/%m/%d') )");
                    varname1.Append("AND ('" + reg_dt_end + "'='' OR DATE_FORMAT(b.reg_dt,'%Y/%m/%d') <= DATE_FORMAT('" + reg_dt_end + "','%Y/%m/%d') )");

                }
                var data = new Excute_query().get_data_from_data_base(varname1);
                var result = GetJsonPersons(data);

                return Json(new { result = true, data = result.Data, type }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = "Lỗi hệ Thống!" }, JsonRequestBehavior.AllowGet);
            }

        }

        private class ModelTMSData
        {
            public string actual { get; set; }
            public string target { get; set; }
            public string at_no { get; set; }
            public string name { get; set; }
            public string date { get; set; }
            public string defect { get; set; }

            public string product { get; set; }
        }

        public ActionResult getActualData(int id_actual, string at_no)
        {
            StringBuilder sql_insert = new StringBuilder();
            sql_insert.Append("SELECT (SELECT SUM(gr_qty) FROM w_material_info WHERE id_actual = '" + id_actual + "') actual ");
            sql_insert.Append(",(SELECT target FROM w_actual_primary WHERE at_no =  '" + at_no + "' LIMIT 1) target, ");
            sql_insert.Append("a.at_no,a.name,a.date,a.defect, ");
            sql_insert.Append("(SELECT product FROM w_actual_primary WHERE at_no =  '" + at_no + "' LIMIT 1) product ");
            sql_insert.Append("FROM w_actual a ");
            sql_insert.Append("WHERE a.id_actual =  '" + id_actual + "' ");

            var list2 = new InitMethods().ConvertDataTableToList<ModelTMSData>(sql_insert).ToList();

           
            return Json(list2.FirstOrDefault(), JsonRequestBehavior.AllowGet);
            
        }
        public ActionResult PartialView_dialog_ViewDetailMaterial(string product, string at_no)
        {
            ViewBag.product = product;
            ViewBag.at_no = at_no;
            return PartialView();
        }
        #endregion ATM
        #region ExportToMachineScan
        public ActionResult ExportToMachineScan()
        {
            HttpCookie cookie = HttpContext.Request.Cookies["language"];
            if (cookie != null)
            {
                ViewBag.language = cookie.Value;
            }
            return View("~/Views/ActualWO/ExportToMachine/ExportToMachineScan.cshtml");
        }
        #endregion
        //print QRcode from wmtid in table w_material_info
        public ActionResult PrintfQRCode(int code)
        {

            var data = (from a in db.w_material_info
                        join b in db.d_material_info on a.mt_no equals b.mt_no
                        where (a.wmtid == code)
                        select new
                        {
                            mt_cd = a.mt_cd,
                            mt_no = a.mt_no,
                            gr_qty = a.gr_qty,
                            expore_dt = a.expore_dt,
                            dt_of_receipt = a.dt_of_receipt,
                            expiry_dt = a.expiry_dt,
                            lot_no = a.lot_no,
                            send_qty = 1,
                            bundle_qty = b.bundle_qty,
                            bundle_unit = b.bundle_unit,
                            mt_type = b.mt_type,
                            width = b.width,
                            width_unit = b.width_unit,
                            spec = b.spec,
                            spec_unit = b.spec_unit,
                            mt_nm = b.mt_nm
                        }).ToList().FirstOrDefault();
            if (data == null)
            {
                return new ContentResult
                {
                    ContentType = "text/html",
                    Content = "lỗi không tìm thấy nguyên vật liệu",
                };
            }
            else
            {
                ViewBag.MaterialCode = data.mt_cd;
                ViewBag.MaterialNo = data.mt_no;
                ViewBag.Length = data.gr_qty;
                ViewBag.DateExport = "";
                if (!string.IsNullOrEmpty(data.expore_dt))
                {
                    ViewBag.DateExport = data.expore_dt.Substring(0, 4) + "-" + data.expore_dt.Substring(4, 2) + "-" + data.expore_dt.Substring(6, 2);
                }
                ViewBag.DateExpired = "";
                if (!string.IsNullOrEmpty(data.expiry_dt))
                {
                    ViewBag.DateExpired = data.expiry_dt.Substring(0, 4) + "-" + data.expiry_dt.Substring(4, 2) + "-" + data.expiry_dt.Substring(6, 2);
                }
                ViewBag.DateReceived = "";
                if (!string.IsNullOrEmpty(data.dt_of_receipt))
                {
                    ViewBag.DateReceived = data.dt_of_receipt.Substring(0, 4) + "-" + data.dt_of_receipt.Substring(4, 2) + "-" + data.dt_of_receipt.Substring(6, 2);
                }
                
                
                ViewBag.LotNo = data.lot_no;
                ViewBag.SendQuality = data.send_qty;
                ViewBag.BundleQuality = data.bundle_qty;
                ViewBag.BundleUnit = data.bundle_unit;
                ViewBag.MaterialType = data.mt_type;
                ViewBag.Width = data.width;
                ViewBag.WidthUnit = data.width_unit;
                ViewBag.OriginalLength = data.spec;
                ViewBag.OriginalLengthUnit = data.spec_unit;
                ViewBag.MaterialName = data.mt_nm;
                return View("~/Views/ActualWO/PrintfQR/PrintfQR.cshtml");
            }
        }
    }
}
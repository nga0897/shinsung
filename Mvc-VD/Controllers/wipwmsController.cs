using Microsoft.AspNet.SignalR.Client;
using Mvc_VD.Models;
using Mvc_VD.Models.Language;
using Mvc_VD.Models.WIP;
using Mvc_VD.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;


namespace Mvc_VD.Controllers
{
    public class wipwmsController : BaseController
    {

        private readonly IWIPService _IWIPService;
        private readonly IWOService _IWOService;
        private readonly Entities db;
        private HubConnection connection = new HubConnection(Extension.GetAppSetting("Realtime"));
        IHubProxy chat;
        //2 cai
        public wipwmsController(
            IWIPService IWIPService,
            IWOService IWOService,
             IDbFactory DbFactory)
        {

            _IWIPService = IWIPService;
            _IWOService = IWOService;
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
        // GET: /MaterialInformation/
        //private Entities db = new Entities();
        string exs = "Lỗi hệ thống!!!";
        string exits = "Dữ liệu không tồn tại!!!";
        string ss = "Thành công!!!";
        string used = "Đã được sử dụng!!!";
        string scanagain = "Vui lòng Scan Lại!!!";
        string chagain = "Vui lòng chọn  Lại!!!";
        string sts = "Đã chuyển trạng thái khác!!!";
        string check = "Vui lòng kiểm tra lại giữ liệu!!!";
        string sosanh = "Số lượng không bằng số lượng đã chia trước đó!";
        string vlnu = "Vật liệu không sử dụng!";
        string cnh = " Không có phản hồi nên không thể hủy!";
        string cfs = "Chỉ Feddback khi ở trạng thái đang chờ hàng!";
       
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

        #region API
        public ActionResult mmetrial_list_api(string ml_no)
        {


            var data1 = (from a in db.w_material_info
                         join b in db.d_material_info
                         on a.mt_no equals b.mt_no into d
                         from e in d.DefaultIfEmpty()
                         where a.mt_cd == ml_no
                         select new
                         {
                             ml_no = a.mt_cd,
                             mt_no = a.mt_no,
                             mt_nm = e.mt_nm,
                             gr_qty = a.gr_qty,
                             mt_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == a.mt_sts_cd).Select(x => x.dt_nm),
                             lct_nm = db.lct_info.Where(x => x.lct_cd == a.lct_cd).Select(x => x.lct_nm),
                             lct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002" && x.dt_cd == a.lct_sts_cd).Select(x => x.dt_nm),

                         }).ToList();
            if (data1.Count > 0)
            {

                return Json(new { result = data1 }, JsonRequestBehavior.AllowGet);
            }
            else
            {

                return Json(new { result = "The Code dont Exits" }, JsonRequestBehavior.AllowGet);
            }


        }
        #endregion

        #region Material_Information(WIP)
        public ActionResult Material_Information_Wip()
        {
            return View();
        }

        public ActionResult GetType(comm_mt comm_dt)
        {
            var type = db.comm_dt.ToList().Where(item => item.mt_cd == "COM004").ToList();
            return Json(type, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetLocationsts(comm_dt comm_dt)
        {
            var lists = db.comm_dt.Where(item => item.mt_cd == "WHS002").ToList();
            return Json(lists, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Getw_mate_info_wip()
        {
            //var sql = new StringBuilder();
            //sql.Append("select a.wmtid,a.mt_no,b.mt_nm,(select dt_nm from comm_dt where mt_cd='COM004' and dt_cd=b.mt_type) as mt_type,(select dt_nm from comm_dt where mt_cd='WHS005' and dt_cd=a.mt_sts_cd) as mt_sts_cd,(select dt_nm from comm_dt where mt_cd='WHS002' and dt_cd=a.lct_sts_cd) as lct_sts_cd,(select lct_nm from lct_info where lct_cd=a.lct_cd) as lct_nm, (select sum(gr_qty) from w_material_info as c where c.mt_no=a.mt_no) as total from w_material_info as a join d_material_info as b on a.mt_no=b.mt_no where a.lct_cd like '002%' group by a.mt_no");

            string tempSql = " SELECT a.wmtid, a.mt_no, b.mt_nm, SUM(a.gr_qty) AS total, "
                + " ROW_NUMBER() OVER (ORDER BY a.wmtid DESC) AS RowNum, "
                + " (SELECT dt_nm FROM comm_dt WHERE mt_cd='COM004' AND dt_cd=b.mt_type) AS mt_type, "
                + " (SELECT dt_nm FROM comm_dt WHERE mt_cd='WHS005' AND dt_cd=a.mt_sts_cd) AS mt_sts_cd, "
                + " (SELECT dt_nm FROM comm_dt WHERE mt_cd='WHS002' AND dt_cd=a.lct_sts_cd) AS lct_sts_cd, "
                + " (SELECT lct_nm FROM lct_info WHERE lct_cd=a.lct_cd) AS lct_nm "
                + " FROM w_material_info AS a "
                + " JOIN d_material_info AS b ON a.mt_no = b.mt_no "
                + " WHERE a.lct_cd LIKE '002%' "
                + " AND ('' = '' OR  a.mt_no IN (SELECT c.mt_no FROM d_bom_detail c WHERE (CONCAT(SUBSTR(c.bom_no, 1, 1), CONVERT(SUBSTR(c.bom_no, 2, 11),INT), SUBSTR(c.bom_no, 12, 1)) LIKE '%%'))) "
                + " AND ('' = '' OR a.mt_no IN (SELECT c.mt_no FROM d_bom_detail c WHERE (CONCAT(SUBSTR(c.bom_no, 1, 1), CONVERT(SUBSTR(c.bom_no, 2, 11),INT), SUBSTR(c.bom_no, 12, 1))) IN (SELECT (CONCAT(SUBSTR(bom_no, 1, 1), CONVERT(SUBSTR(bom_no, 2, 11),INT), SUBSTR(bom_no, 12, 1))) FROM w_product_info WHERE style_no LIKE '%%')))"
                + " GROUP BY a.mt_no "
                ;

            var data = new DataTable();
            using (var cmd = db.Database.Connection.CreateCommand())
            {
                db.Database.Connection.Open();
                cmd.CommandText = tempSql.ToString();
                using (var reader = cmd.ExecuteReader())
                {
                    data.Load(reader);
                }
            }
            db.Database.Connection.Close();
            var result = GetJsonPersons(data);
            return result;

        }

        public ActionResult Warehouse(lct_info lct_info)
        {
            var lists = db.lct_info.Where(item => item.mv_yn == "Y" && item.index_cd != null && item.index_cd != "" && item.index_cd != "0").
              OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();

            return Json(lists, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Warehouse_machine(lct_info lct_info)
        {
            var lists = db.lct_info.Where(item => item.lct_cd.StartsWith("020")).
              OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();

            return Json(lists, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Warehouse_wip(lct_info lct_info)
        {
            var lists = db.lct_info.Where(item => item.lct_cd.StartsWith("002")).OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();

            return Json(lists, JsonRequestBehavior.AllowGet);
        }

        public ActionResult searchwmaterial_info_wip(Pageing pageing)
        {
            var bom_no = Request["bom_no"];
            var mt_no = Request["mt_no"];

            if (bom_no != "" && bom_no != null)
                if (bom_no.Substring(0, 1) == "B") bom_no = bom_no.Substring(1, bom_no.Length - 1);

            StringBuilder varname1 = new StringBuilder();
            varname1.Append("SELECT a.rel_bom, a.mt_no, a.mt_type, \n");
            varname1.Append("		IFNULL((SELECT i.mt_nm FROM d_material_info AS i WHERE i.mt_no = a.mt_no LIMIT 1),0) AS mt_nm, \n");
            varname1.Append("		 \n");
            varname1.Append("		IFNULL((SELECT SUM(CAST(i.gr_qty AS int)) \n");
            varname1.Append("			FROM  w_material_info AS i WHERE substring(i.to_lct_cd,1,3) ='002' AND i.mt_no = a.mt_no AND i.rel_bom = a.rel_bom \n");
            varname1.Append("							 AND i.lct_sts_cd = '201'	AND i.rel_bom IS NOT NULL \n");
            varname1.Append("									GROUP BY i.rel_bom, i.mt_no ORDER BY i.rel_bom DESC ),0) AS wait_qty, \n");
            varname1.Append("		 \n");
            varname1.Append("		IFNULL((SELECT SUM(CAST(i.gr_qty AS int)) \n");
            varname1.Append("			FROM  w_material_info AS i WHERE substring(i.lct_cd,1,3) ='002' AND i.mt_no = a.mt_no AND i.rel_bom = a.rel_bom \n");
            varname1.Append("							AND i.mt_sts_cd ='001' AND i.lct_sts_cd = '101'	AND i.rel_bom IS NOT NULL \n");
            varname1.Append("									GROUP BY i.rel_bom, i.mt_no ORDER BY i.rel_bom DESC ),0) AS stock_qty, \n");
            varname1.Append("		 \n");
            varname1.Append("		IFNULL((SELECT SUM(CAST(i.gr_qty AS int)) \n");
            varname1.Append("			FROM  w_material_info AS i WHERE substring(i.lct_cd,1,3) ='002' AND i.mt_no = a.mt_no AND i.rel_bom = a.rel_bom \n");
            varname1.Append("							AND i.mt_sts_cd ='002' AND i.lct_sts_cd = '220'	AND i.rel_bom IS NOT NULL \n");
            varname1.Append("									GROUP BY i.rel_bom, i.mt_no ORDER BY i.rel_bom DESC ),0) AS using_qty, \n");
            varname1.Append("		IFNULL((SELECT SUM(CAST(i.gr_qty AS int)) \n");
            varname1.Append("			FROM  w_material_info AS i WHERE substring(i.lct_cd,1,3) ='002' AND i.mt_no = a.mt_no AND i.rel_bom = a.rel_bom \n");
            varname1.Append("							AND i.mt_sts_cd ='005' AND i.lct_sts_cd = '220'	AND i.rel_bom IS NOT NULL \n");
            varname1.Append("									GROUP BY i.rel_bom, i.mt_no ORDER BY i.rel_bom DESC ),0) AS used_qty \n");
            varname1.Append(" \n");
            varname1.Append("	FROM  w_material_info AS a WHERE substring(a.lct_cd,1,3) = '002' AND   a.rel_bom IS NOT NULL \n");
            varname1.Append(" AND ('" + bom_no + "'='' OR  a.rel_bom LIKE '%" + bom_no + "%' ) ");
            varname1.Append(" AND ('" + mt_no + "'='' OR  a.mt_no LIKE '%" + mt_no + "%' ) ");
            varname1.Append("		GROUP BY a.rel_bom, a.mt_no ORDER BY a.rel_bom desc");

            var data = new Excute_query().get_data_from_data_base(varname1);
            var result = GetJsonPersons(data);
            return result;
            //var prd_lcd = Request["prd_lcd"];

            //var mt_nm = Request["mt_nm"];
            //var mt_type = Request["mt_type"];
            //var mt_sts_cd = Request["mt_sts_cd"];
            //var lct_cd = Request["lct_cd"];
            //var lct_sts_cd = Request["lct_sts_cd"];
            //var start = Request["start"];
            //var end = Request["end"];
            //var dateConvert = new DateTime();
            //if (DateTime.TryParse(end, out dateConvert))
            //{
            //    end = dateConvert.ToString("yyyy/MM/dd");
            //}

            //if (DateTime.TryParse(start, out dateConvert))
            //{
            //    start = dateConvert.ToString("yyyy/MM/dd");
            //}

            //var sql = new StringBuilder();
            //sql.Append("select a.wmtid,a.mt_no,b.mt_nm,(select dt_nm from comm_dt where mt_cd='COM004' and dt_cd=b.mt_type) as mt_type,(select dt_nm from comm_dt where mt_cd='WHS005' and dt_cd=a.mt_sts_cd) as mt_sts_cd,(select dt_nm from comm_dt where mt_cd='WHS002' and dt_cd=a.lct_sts_cd) as lct_sts_cd,(select lct_nm from lct_info where lct_cd=a.lct_cd) as lct_nm, (select sum(gr_qty) from w_material_info as c where c.mt_no=a.mt_no) as total from w_material_info as a join d_material_info as b on a.mt_no=b.mt_no where a.lct_cd like '002%' ")
            //.Append("and ('" + mt_no + "'='' OR  a.mt_no like '%" + mt_no + "%' )")
            //.Append("and ('" + mt_nm + "'='' OR  b.mt_nm like '%" + mt_nm + "%' )")
            //.Append("and ('" + mt_type + "'='' OR  b.mt_type like '%" + mt_type + "%' )")
            //.Append("and ('" + mt_sts_cd + "'='' OR  a.mt_sts_cd like '%" + mt_sts_cd + "%' )")
            //.Append("and ('" + lct_cd + "'='' OR  a.lct_cd like '%" + lct_cd + "%' )")
            //.Append("and ('" + lct_sts_cd + "'='' OR  a.lct_sts_cd like '%" + lct_sts_cd + "%' )")
            //.Append("AND ('" + start + "'='' OR DATE_FORMAT(a.input_dt,'%Y/%m/%d') >= DATE_FORMAT('" + start + "','%Y/%m/%d'))")
            //.Append("AND ('" + end + "'='' OR DATE_FORMAT(a.input_dt,'%Y/%m/%d') <= DATE_FORMAT('" + end + "','%Y/%m/%d')) group by a.mt_no");


            //string sql = " SELECT a.wmtid, a.mt_no, b.mt_nm, SUM(cast(a.gr_qty as int)) AS total, "
            //    + " ROW_NUMBER() OVER (ORDER BY a.wmtid DESC) AS RowNum, "
            //    + " (SELECT dt_nm FROM comm_dt WHERE mt_cd='COM004' AND dt_cd=b.mt_type) AS mt_type, "
            //    + " (SELECT dt_nm FROM comm_dt WHERE mt_cd='WHS005' AND dt_cd=a.mt_sts_cd) AS mt_sts_cd, "
            //    + " (SELECT dt_nm FROM comm_dt WHERE mt_cd='WHS002' AND dt_cd=a.lct_sts_cd) AS lct_sts_cd, "
            //    + " (SELECT lct_nm FROM lct_info WHERE lct_cd=a.lct_cd) AS lct_nm "
            //    + " FROM w_material_info AS a "
            //    + " JOIN d_material_info AS b ON a.mt_no = b.mt_no "
            //    + " WHERE a.lct_cd LIKE '002%' "
            //    + " AND ('" + mt_no + "'='' OR  a.mt_no LIKE '%" + mt_no + "%' ) "
            //    + " AND ('" + mt_nm + "'='' OR  b.mt_nm LIKE '%" + mt_nm + "%' ) "
            //    + " AND ('" + mt_type + "'='' OR  b.mt_type LIKE '%" + mt_type + "%' ) "
            //    + " AND ('" + mt_sts_cd + "'='' OR  a.mt_sts_cd LIKE '%" + mt_sts_cd + "%' ) "
            //    + " AND ('" + lct_cd + "'='' OR  a.lct_cd LIKE '%" + lct_cd + "%' )"
            //    + " AND ('" + lct_sts_cd + "'='' OR  a.lct_sts_cd LIKE '%" + lct_sts_cd + "%' ) "
            //    + " AND ('" + start + "'='' OR DATE_FORMAT(a.input_dt,'%Y/%m/%d') >= DATE_FORMAT('" + start + "','%Y/%m/%d')) "
            //    + " AND ('" + end + "'='' OR DATE_FORMAT(a.input_dt,'%Y/%m/%d') <= DATE_FORMAT('" + end + "','%Y/%m/%d')) "
            //    + " AND ('" + bom_no + "' = '' OR   a.sd_no IN (SELECT sd_no FROM w_sd_info AS i WHERE i.mr_no IN (SELECT mr_no FROM w_mr_info AS j WHERE  CONCAT(substr(j.rel_bom, 1, 1), CONVERT(substr(j.rel_bom, 2, 11),INT),substr(j.rel_bom, 12, 1)) LIKE  '%" + bom_no + "%'  )))"
            //    //+ " AND ('" + bom_no + "' = '' OR  a.mt_no IN (SELECT c.mt_no FROM d_bom_detail c WHERE (CONCAT(SUBSTR(c.bom_no, 1, 1), CONVERT(SUBSTR(c.bom_no, 2, 11),INT), SUBSTR(c.bom_no, 12, 1)) LIKE '%" + bom_no + "%'))) "
            //    + " AND ('" + prd_lcd + "' = '' OR a.mt_no IN (SELECT c.mt_no FROM d_bom_detail c WHERE (CONCAT(SUBSTR(c.bom_no, 1, 1), CONVERT(SUBSTR(c.bom_no, 2, 11),INT), SUBSTR(c.bom_no, 12, 1))) IN (SELECT (CONCAT(SUBSTR(bom_no, 1, 1), CONVERT(SUBSTR(bom_no, 2, 11),INT), SUBSTR(bom_no, 12, 1))) FROM w_product_info WHERE style_no LIKE '%" + prd_lcd + "%')))"
            //    + " GROUP BY a.mt_no "
            //    ;


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

        }
        #endregion

        #region Material_List_Wip
        public ActionResult Material_List_Wip()
        {
            return View();
        }

        public ActionResult Getw_mate_list_wip()
        {
            //var sql = new StringBuilder();
            //sql.Append("select  a.reg_id,a.reg_dt,a.chg_id,a.chg_dt,a.mt_cd,a.bb_no,a.remark,a.to_lct_cd,a.from_lct_cd,a.gr_qty,a.wmtid,a.mt_no,b.mt_nm,(select dt_nm from comm_dt where mt_cd='COM004' and dt_cd=b.mt_type) as mt_type,(select dt_nm from comm_dt where mt_cd='WHS005' and dt_cd=a.mt_sts_cd) as mt_sts_cd,(select dt_nm from comm_dt where mt_cd='WHS002' and dt_cd=a.lct_sts_cd) as lct_sts_cd,(select lct_nm from lct_info where lct_cd=a.lct_cd) as lct_nm from w_material_info as a join d_material_info as b on a.mt_no=b.mt_no where a.lct_cd like '002%' ");

            var sql = new StringBuilder();
            sql.Append(" SELECT")
                .Append(" a.mt_sts_cd,")
                .Append(" a.lct_cd,")
                .Append(" a.lct_sts_cd,")
                .Append(" a.mt_cd,")
                .Append(" a.mt_no,")
                .Append(" b.mt_nm,")
                .Append(" b.mt_type,")
                .Append(" a.gr_qty,")
                .Append(" (SELECT dt_nm FROM comm_dt WHERE mt_cd = 'WHS005' AND dt_cd = a.mt_sts_cd) AS status,")
                .Append(" (SELECT lct_nm FROM lct_info WHERE ((mv_yn = 'Y' OR sf_yn = 'Y') AND (index_cd IS NOT NULL) AND (index_cd != '') AND (index_cd != '0')) AND lct_cd = a.lct_cd) AS location,")
                .Append(" (SELECT dt_nm FROM comm_dt WHERE mt_cd = 'WHS002' AND dt_cd = a.lct_sts_cd) AS location_status,")
                .Append(" a.from_lct_cd,")
                .Append(" a.to_lct_cd,")
                .Append(" a.bb_no,")
                .Append(" a.remark,")
                .Append(" a.reg_id,")
                .Append(" a.reg_dt,")
                .Append(" a.chg_id,")
                .Append(" a.chg_dt,")
                .Append(" a.wmtid")
                .Append(" FROM w_material_info AS a")
                .Append(" JOIN d_material_info AS b")
                .Append(" ON a.mt_no = b.mt_no")
                .Append(" JOIN comm_dt c")
                .Append(" ON a.mt_sts_cd = c.dt_cd")
                .Append(" JOIN lct_info d")
                .Append(" ON a.lct_cd = d.lct_cd");

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

        public ActionResult searchwmaterial_list_wip(Pageing paging)
        {

            /*
            //var dateConvert = new DateTime();
            //if (DateTime.TryParse(end, out dateConvert))
            //{
            //    end = dateConvert.ToString("yyyy/MM/dd");
            //}

            //if (DateTime.TryParse(start, out dateConvert))
            //{
            //    start = dateConvert.ToString("yyyy/MM/dd");
            //}

            //---------------------------------

            //var query = new StringBuilder();
            //query.Append(" SELECT a.*   from w_material_info as a join d_material_info as b on a.mt_no=b.mt_no");
            //query.Append(" Where  b.del_yn='N' and ('" + mt_no + "'='' OR  a.mt_no like '%" + mt_no + "%' )");
            //query.Append("and ('" + mt_nm + "'='' OR  b.mt_nm like '%" + mt_nm + "%' )");
            //query.Append("and ('" + mt_type + "'='' OR  b.mt_type like '%" + mt_type + "%' )");
            //query.Append("and ('" + mt_sts_cd + "'='' OR  a.mt_sts_cd like '%" + mt_sts_cd + "%' )");
            //query.Append("and ('" + lct_cd + "'='' OR  a.lct_cd like '%" + lct_cd + "%' )");
            //query.Append("and ('" + mt_cd + "'='' OR  a.mt_cd like '%" + mt_cd + "%' )");
            //query.Append("and ('" + lct_sts_cd + "'='' OR  a.lct_sts_cd like '%" + lct_sts_cd + "%' )");
            //query.Append("AND ('" + start + "'='' OR DATE_FORMAT(a.input_dt,'%Y/%m/%d') >= DATE_FORMAT('" + start + "','%Y/%m/%d'))");
            //query.Append("AND ('" + end + "'='' OR DATE_FORMAT(a.input_dt,'%Y/%m/%d') <= DATE_FORMAT('" + end + "','%Y/%m/%d'))");

            //var value = db.w_material_info.SqlQuery(query.ToString()).ToList<w_material_info>().Count();
             * */

            //---------------------------------
            var mt_cd = Request["mt_cd"];
            var mt_no = Request["mt_no"];
            var mt_nm = Request["mt_nm"];
            var mt_type = Request["mt_type"];
            var mt_sts_cd = Request["mt_sts_cd"];
            var lct_cd = Request["lct_cd"];
            var lct_sts_cd = Request["lct_sts_cd"];
            var bom_no = Request["bom_no"];
            var style_no = Request["style_no"];
            var start = Request["start"];
            var end = Request["end"];
            Dictionary<string, string> list = PagingAndOrderBy(paging, " ORDER BY myData.wmtid DESC ");
            string tempSql = " SELECT a.mt_cd, a.mt_no, b.mt_nm, b.mt_type, a.gr_qty, a.bb_no, a.remark, a.reg_id, a.reg_dt, a.chg_id, a.chg_dt, a.wmtid, ROW_NUMBER() OVER (ORDER BY a.wmtid DESC) AS RowNum, "
                            + " (SELECT dt_nm FROM comm_dt WHERE mt_cd = 'WHS005' AND dt_cd=a.mt_sts_cd) AS status, "
                            + " (SELECT  lct_nm FROM lct_info WHERE lct_cd = a.lct_cd) AS location, "
                            + " (SELECT l_info.lct_nm FROM lct_info l_info WHERE l_info.lct_cd = a.from_lct_cd) AS from_lct_cd, "
                            + " (SELECT l_info.lct_nm FROM lct_info l_info WHERE l_info.lct_cd = a.to_lct_cd) AS to_lct_cd, "
                            + " (SELECT dt_nm FROM comm_dt WHERE mt_cd = 'WHS002' AND dt_cd = a.lct_sts_cd) AS location_status "
                            + " FROM w_material_info AS a JOIN d_material_info AS b ON a.mt_no = b.mt_no "
                            + " WHERE a.lct_cd LIKE '002%' "
                            + " AND a.mt_no LIKE '%" + mt_no + "%' "
                            + " AND b.mt_nm LIKE '%" + mt_nm + "%' "
                            + " AND b.mt_type LIKE '%" + mt_type + "%' "
                            + " AND a.mt_sts_cd LIKE '%" + mt_sts_cd + "%' "
                            + " AND a.lct_cd LIKE '%" + lct_cd + "%' "
                            + " AND a.mt_cd LIKE '%" + mt_cd + "%' "
                            + " AND a.lct_sts_cd LIKE '%" + lct_sts_cd + "%' "
                            //+ " AND ('" + start + "' = '' OR (DATE_FORMAT(a.input_dt, '%Y/%m/%d') >= DATE_FORMAT('" + start + "', '%Y/%m/%d'))) "
                            //+ " AND ('" + end + "' = '' OR (DATE_FORMAT(a.input_dt, '%Y/%m/%d') <= DATE_FORMAT('" + end + "', '%Y/%m/%d'))) "
                            + " AND ('" + start + "' = '' OR (DATE_FORMAT(a.reg_dt, '%Y/%m/%d') >= DATE_FORMAT('" + start + "', '%Y/%m/%d'))) "
                            + " AND ('" + end + "' = '' OR (DATE_FORMAT(a.reg_dt, '%Y/%m/%d') <= DATE_FORMAT('" + end + "', '%Y/%m/%d'))) "
                            + " AND ('" + bom_no + "' = '' OR  a.mt_no IN (SELECT c.mt_no FROM d_bom_detail c WHERE (CONCAT(SUBSTR(c.bom_no, 1, 1), CONVERT(SUBSTR(c.bom_no, 2, 11),INT), SUBSTR(c.bom_no, 12, 1)) LIKE '%" + bom_no + "%'))) "
                            + " AND ('" + style_no + "' = '' OR a.mt_no IN (SELECT c.mt_no FROM d_bom_detail c WHERE (CONCAT(SUBSTR(c.bom_no, 1, 1), CONVERT(SUBSTR(c.bom_no, 2, 11),INT), SUBSTR(c.bom_no, 12, 1))) IN (SELECT (CONCAT(SUBSTR(bom_no, 1, 1), CONVERT(SUBSTR(bom_no, 2, 11),INT), SUBSTR(bom_no, 12, 1))) FROM w_product_info WHERE style_no LIKE '%" + style_no + "%'))) "
                            ;
            string countSql = " SELECT COUNT(myData.wmtid) FROM ( " + tempSql + " ) myData ";

            string viewSql = " SELECT * FROM ( " + tempSql + " ) myData WHERE myData.RowNum BETWEEN " + list["start"] + " AND " + list["end"] + list["orderBy"];

            return SearchAndPaging(countSql, viewSql, int.Parse(list["index"]), int.Parse(list["size"]));
        }

        #endregion

        #region Receving_Scan_Wip
       
        public ActionResult UpdateReceving_Scan_Wip(string mt_barcode, string lct_cd)
        {
            try
            {
                var count_mt = db.w_material_info.Count(x => x.mt_cd == mt_barcode && x.lct_sts_cd == "201" );
                if (count_mt == 0)
                {
               
                    return Json(new { result = false, mess = "Material " +exits}, JsonRequestBehavior.AllowGet);
                }
                w_material_info w_material_info = db.w_material_info.FirstOrDefault(x => x.mt_cd == mt_barcode);

                //  w_material_info.to_lct_cd = lct_cd;
                var lct_cd1 = db.lct_info.Where(x => x.lct_bar_cd == lct_cd).FirstOrDefault().lct_cd;
                if (string.IsNullOrEmpty(lct_cd1))
                {
                    return Json(new { result = false, mess = "Location "+exits }, JsonRequestBehavior.AllowGet);
                }
                w_material_info.lct_cd = lct_cd1;
                w_material_info.to_lct_cd = lct_cd1;
                w_material_info.lct_sts_cd = "101";
                w_material_info.mt_sts_cd = "001";
                w_material_info.use_yn = "Y";
                //w_material_info.chg_dt = DateTime.Now;

                w_material_info.input_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                db.Entry(w_material_info).State = EntityState.Modified;
                db.SaveChanges();
                var ds = (from y in db.w_material_info
                          join k in db.d_material_info on y.mt_no equals k.mt_no into e
                          from f in e.DefaultIfEmpty()
                          where y.wmtid == w_material_info.wmtid
                          select new
                          {
                              reg_id = y.reg_id,
                              reg_dt = y.reg_dt,
                              chg_id = y.chg_id,
                              chg_dt = y.chg_dt,
                              input_dt = y.input_dt.Substring(0, 4) + "-" + y.input_dt.Substring(4, 2) + "-" + y.input_dt.Substring(6, 2),
                              mt_cd = y.mt_cd,
                              bb_no = y.bb_no,
                              remark = y.remark,
                              to_lct_cd = db.lct_info.Where(x => x.lct_cd == y.to_lct_cd).Select(x => x.lct_nm),
                              from_lct_cd = db.lct_info.Where(x => x.lct_cd == y.from_lct_cd).Select(x => x.lct_nm),
                              gr_qty = y.gr_qty,
                              wmtid = y.wmtid,
                              mt_no = y.mt_no,
                              mt_nm = f.mt_nm,
                              lct_nm = db.lct_info.Where(x => x.lct_cd == y.lct_cd).Select(x => x.lct_nm),
                              lct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002" && x.dt_cd == y.lct_sts_cd).Select(x => x.dt_nm),
                              mt_type = db.comm_dt.Where(x => x.mt_cd == "COM004" && x.dt_cd == f.mt_type).Select(x => x.dt_nm),
                              mt_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == y.mt_sts_cd).Select(x => x.dt_nm),
                          }).FirstOrDefault();
                return Json(new { result = true, data = ds, mess = ss}, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {

                return Json(new { result = false, mess = exs }, JsonRequestBehavior.AllowGet);
            }



            //try
            //{
            //    var w = db.w_material_info.Where(x => x.mt_qrcode == mt_barcode).ToList();
            //    var m = 0;
            //    if (w.Count > 0)
            //    {
            //        m = w.FirstOrDefault().wmtid;

            //        w_material_info w_material_info = db.w_material_info.Find(m);
            //        if (w_material_info.lct_sts_cd == null) return Json(new { result = false, mess = "Material do not have BOM" }, JsonRequestBehavior.AllowGet);
            //        if (w_material_info.lct_sts_cd == "201" || w_material_info.lct_sts_cd == "" || w_material_info.lct_sts_cd == null)
            //        {
            //            var lct_cd1 = db.lct_info.Where(x => x.lct_bar_cd == lct_cd).FirstOrDefault().lct_cd;
            //            if (string.IsNullOrEmpty(lct_cd1))
            //            {
            //                return View();
            //            }
            //            w_material_info.lct_cd = lct_cd1;
            //            w_material_info.to_lct_cd = lct_cd1;
            //            w_material_info.lct_sts_cd = "101";
            //            w_material_info.mt_sts_cd = "001";
            //            w_material_info.use_yn = "Y";
            //            w_material_info.del_yn = "N";
            //            w_material_info.chg_dt = DateTime.Now;

            //            w_material_info.input_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
            //            db.Entry(w_material_info).State = EntityState.Modified;
            //            db.SaveChanges();
                        
            //                var ds = (from y in db.w_material_info
            //                          join k in db.d_material_info on y.mt_no equals k.mt_no into e
            //                          from f in e.DefaultIfEmpty()
            //                          where y.wmtid == w_material_info.wmtid
            //                          select new
            //                          {
            //                              reg_id = y.reg_id,
            //                              reg_dt = y.reg_dt,
            //                              chg_id = y.chg_id,
            //                              chg_dt = y.chg_dt,
            //                              input_dt = y.input_dt.Substring(0, 4) + "-" + y.input_dt.Substring(4, 2) + "-" + y.input_dt.Substring(6, 2),
            //                              mt_cd = y.mt_cd,
            //                              bb_no = y.bb_no,
            //                              rel_bom = y.rel_bom,
            //                              remark = y.remark,
            //                              to_lct_cd = db.lct_info.Where(x => x.lct_cd == y.to_lct_cd).Select(x => x.lct_nm),
            //                              from_lct_cd = db.lct_info.Where(x => x.lct_cd == y.from_lct_cd).Select(x => x.lct_nm),
            //                              gr_qty = y.gr_qty,
            //                              wmtid = y.wmtid,
            //                              mt_no = y.mt_no,
            //                              mt_nm = f.mt_nm,
            //                              lct_nm = db.lct_info.Where(x => x.lct_cd == y.lct_cd).Select(x => x.lct_nm),
            //                              lct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002" && x.dt_cd == y.lct_sts_cd).Select(x => x.dt_nm),
            //                              mt_type = db.comm_dt.Where(x => x.mt_cd == "COM004" && x.dt_cd == f.mt_type).Select(x => x.dt_nm),
            //                              mt_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == y.mt_sts_cd).Select(x => x.dt_nm),
            //                          }).FirstOrDefault();
            //                return Json(new{result = true, data = ds, mess = "Success"}, JsonRequestBehavior.AllowGet);
                        
            //        }
            //    }
            //    else
            //    return Json(new { result = false, mess = "Material not Exist" }, JsonRequestBehavior.AllowGet); ;
            //}
            //catch (Exception ex)
            //{
            //    return Json(new { result = false, mess = ex.Message }, JsonRequestBehavior.AllowGet); ;
            //}
          
        }

        #endregion

        #region Receiving_Manual
        public ActionResult Receiving_Manual()
        {
            return View();
        }
        #region Put_in
        public ActionResult getLocation(lct_info lct_info)
        {
            //var lists = db.lct_info.Where(item => item.wp_yn == "Y" && item.level_cd != "001" && item.level_cd != "000").
            var lists = db.lct_info.Where(item => item.level_cd != "001" && item.level_cd != "000" && item.lct_cd.Substring(0, 3) == "002").
            OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();

            return Json(lists, JsonRequestBehavior.AllowGet);
        }
        public ActionResult getLocationPR(lct_info lct_info)
        {
            //var lists = db.lct_info.Where(item => item.wp_yn == "Y" && item.level_cd != "001" && item.level_cd != "000").
            //var lists = db.lct_info.Where(item => item.level_cd != "001" && item.level_cd != "000" && item.lct_cd.Substring(0, 3) == "002").
            //OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();
            var lists = db.Database.SqlQuery<lct_Wip>("select a.lct_cd, a.lct_nm from lct_info as a where a.level_cd != '001' and a.level_cd != '000' and substring(a.lct_cd,1,3) ='002'").ToList();

            return Json(lists, JsonRequestBehavior.AllowGet);
        }
        public class lct_Wip{
            public string lct_cd {get; set;}
            public string lct_nm {get; set;}
        }

        public ActionResult GetStatus(comm_dt comm_dt)
        {
            var lists = db.comm_dt.Where(item => item.mt_cd == "WHS005").ToList();
            return Json(lists, JsonRequestBehavior.AllowGet);
        }
        public ActionResult getmanual_wip(string shelf_cd)
        {
            if (shelf_cd == null) { shelf_cd = ""; }
            var list = (from a in db.lct_info
                        join b in db.w_material_info
                       on a.lct_cd equals b.lct_cd
                        join m in db.d_material_info
                        on b.mt_no equals m.mt_no
                        into c
                        from d in c.DefaultIfEmpty()
                        where b.to_lct_cd.StartsWith("002") && b.lct_sts_cd == "201"
                        select new
                        {
                            reg_id = b.reg_id,
                            reg_dt = b.reg_dt,
                            chg_id = b.chg_id,
                            chg_dt = b.chg_dt,
                            mt_cd = b.mt_cd,
                            output_dt = b.output_dt,
                            input_dt = b.input_dt,
                            bb_no = b.bb_no,
                            remark = b.remark,
                            from_lct_cd = db.lct_info.Where(x => x.lct_cd == b.from_lct_cd).Select(x => x.lct_nm),
                            to_lct_cd = db.lct_info.Where(x => x.lct_cd == b.to_lct_cd).Select(x => x.lct_nm),
                            gr_qty = b.gr_qty,
                            wmtid = b.wmtid,
                            mt_no = b.mt_no,
                            mt_nm = d.mt_nm,
                            lct_nm = db.lct_info.Where(x => x.lct_cd == b.lct_cd).Select(x => x.lct_nm),
                            lct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002" && x.dt_cd == b.lct_sts_cd).Select(x => x.dt_nm),
                            mt_type = db.comm_dt.Where(x => x.mt_cd == "COM004" && x.dt_cd == d.mt_type).Select(x => x.dt_nm),
                            mt_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == b.mt_sts_cd).Select(x => x.dt_nm),
                        }).ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        public JsonResult searchmanual_wip()
        {

            var mt_no = Request["mt_no"];
            var mt_barcode = Request["mt_barcode"];
            // var mt_sts_cd = "";

            var lct_cd = Request["lct_cd"];
            var bom_no = Request["bom_no"];
            if (bom_no != null && bom_no != "")
                if (bom_no.Substring(0, 1) == "B") bom_no = bom_no.Substring(1, bom_no.Length - 1);
            StringBuilder varname1 = new StringBuilder();
            varname1.Append("SELECT a.wmtid,  a.rel_bom, a.rel_bom as rel_bom1, a.mt_no,a.mt_cd, b.mt_nm, a.mt_cd, c.lct_nm AS lct_nm, a.gr_qty, a.remark, \n");
            varname1.Append("  a.input_dt, a.chg_dt, a.reg_dt, a.reg_id, a.chg_id,   \n");
            varname1.Append("	(select lct_nm from lct_info AS i WHERE  i.lct_cd = a.to_lct_cd) AS to_lct_cd,\n");
            varname1.Append("  (select lct_nm from lct_info AS i WHERE  i.lct_cd = a.from_lct_cd) AS from_lct_cd, \n");
            varname1.Append("	(select dt_nm from comm_dt AS i WHERE i.mt_cd ='WHS005' AND i.dt_cd = a.mt_sts_cd) AS mt_sts_cd, \n");
            varname1.Append("	(select dt_nm from comm_dt AS i WHERE i.mt_cd ='WHS002' AND i.dt_cd = a.mt_sts_cd) AS lct_sts_cd \n");
            varname1.Append("		FROM w_material_info AS a \n");
            varname1.Append("		LEFT JOIN d_material_info AS b ON a.mt_no = b.mt_no \n");
            varname1.Append("		LEFT JOIN lct_info AS c ON c.lct_cd = a.lct_cd \n");
            varname1.Append("		WHERE a.rel_bom is not null and a.lct_sts_cd = '201' and (a.mt_sts_cd != '001' or a.mt_sts_cd != '004') AND SUBSTRING(a.to_lct_cd, 1,3) ='002' AND SUBSTRING(a.to_lct_cd, 1,6) !='002007' ");
            varname1.Append("   AND ('' = '" + mt_no + "' OR a.mt_no like '%" + mt_no + "%') ");
            varname1.Append("   AND ('' = '" + bom_no + "' OR a.rel_bom like '%" + bom_no + "%') ");
            varname1.Append("   AND ('' = '" + mt_barcode + "' OR a.mt_cd like '%" + mt_barcode + "%') ");
            varname1.Append("   AND ('' = '" + lct_cd + "' OR a.to_lct_cd like '%" + lct_cd + "%') ");
            varname1.Append(" order by a.rel_bom desc, a.mt_no desc, a.mt_cd desc");

            var data = new Excute_query().get_data_from_data_base(varname1);
            var result = GetJsonPersons(data);
            return result;
        }

        //public JsonResult searchmanual_wip()
        //{
        //    // Get Data from ajax function
        //    var mt_no = Request["mt_no"].ToString();
        //    var mt_barcode = Request["mt_barcode"].ToString();
        //    //var mt_sts_cd = Request["mt_sts_cd"].ToString();
        //    var mt_sts_cd = "";

        //    var lct_cd = Request["lct_cd"].ToString();

        //    var bom_no = Request["bom_no"].ToString();
        //    if (bom_no != null && bom_no != "") bom_no = bom_no.Substring(1, bom_no.Length - 1);
        //    //var sql = new StringBuilder();
        //    //sql.Append(" SELECT b.reg_id,b.reg_dt,b.chg_id, b.chg_dt, b.mt_cd,b.bb_no, b.remark, b.gr_qty, b.wmtid, b.mt_no, b.output_dt, b.input_dt, d.bom_no, d.mt_nm ")
        //    //    //.Append(" ( SELECT mt_nm FROM d_material_info WHERE mt_no = b.mt_no) AS mt_nm, ")
        //    //    .Append(" (SELECT dt_nm FROM comm_dt WHERE dt_cd = b.lct_sts_cd AND mt_cd = 'WHS002') AS lct_sts_cd, ")
        //    //    .Append(" (SELECT dt_nm FROM comm_dt WHERE dt_cd = c.mt_type AND mt_cd = 'COM004') AS mt_type, ")
        //    //    .Append(" (SELECT dt_nm FROM comm_dt WHERE dt_cd = b.mt_sts_cd AND mt_cd = 'WHS005') AS mt_sts_cd, ")
        //    //    .Append(" (SELECT lct_nm FROM lct_info WHERE lct_cd = b.lct_cd ) AS lct_nm, ")
        //    //    .Append(" (SELECT lct_nm FROM lct_info WHERE lct_cd = b.from_lct_cd ) AS from_lct_cd, ")
        //    //    .Append(" (SELECT lct_nm FROM lct_info WHERE lct_cd = b.to_lct_cd ) AS to_lct_cd ")
        //    //    .Append(" From w_material_info as b  ")
        //    //    .Append(" INNER JOIN d_bom_detail d ON b.mt_no = d.mt_no ")
        //    //    .Append(" where b.to_lct_cd like '002%' and  b.lct_sts_cd = '201'  ")
        //    //   .Append(" AND ('" + bom_no + "' = '' or (CONCAT(SUBSTR(d.bom_no, 1, 1), CONVERT(substr(d.bom_no, 2, 11),INT), substr(d.bom_no, 12, 1)) ) LIKE '%" + bom_no + "%')")
        //    //    //.Append("AND (select mt_no from d_bom_detail where ('" + bom_no + "'=' OR CONCAT(substr(bom_no, 1, 1),CONVERT(substr(bom_no, 2, 11),INT),substr(bom_no, 12, 1)) like '%" + bom_no + "%'')  and mt_no = b.mt_no) ")
        //    //    .Append("AND ('" + mt_no + "'='' OR  b.mt_no like '%" + mt_no + "%' )")
        //    //    .Append("AND ('" + mt_barcode + "'='' OR  b.mt_barcode like '%" + mt_barcode + "%')")
        //    //    .Append("AND ('" + mt_sts_cd + "'='' OR  b.mt_sts_cd like '%" + mt_sts_cd + "%')")
        //    //    .Append("AND ('" + lct_cd + "'='' OR  b.lct_cd like '%" + lct_cd + "%')")
        //    //    .Append(" GROUP BY b.mt_cd ");
        //    Dictionary<string, string> list = PagingAndOrderBy(pageing, " ORDER BY myData.mt_no DESC ");
        //    string tempSql = " SELECT b.relbom, b.reg_id,b.reg_dt,b.chg_id,b.chg_dt,b.mt_cd, b.bb_no,b.remark,b.gr_qty,b.wmtid,b.mt_no,d.mt_nm,b.output_dt,b.input_dt, "
        //        + " ROW_NUMBER() OVER (ORDER BY b.mt_no DESC) AS RowNum, "
        //        + " (SELECT dt_nm FROM comm_dt WHERE dt_cd = b.lct_sts_cd AND mt_cd = 'WHS002') AS lct_sts_cd, "
        //        + "(SELECT dt_nm FROM comm_dt WHERE dt_cd = b.mt_type AND mt_cd = 'COM004') AS mt_type,  "
        //        + " (SELECT dt_nm FROM comm_dt WHERE dt_cd = b.mt_sts_cd AND mt_cd = 'WHS005') AS mt_sts_cd, "
        //        + " (SELECT lct_nm FROM lct_info WHERE lct_cd = b.lct_cd ) AS lct_nm, "
        //        + " (SELECT lct_nm FROM lct_info WHERE lct_cd = b.from_lct_cd ) AS from_lct_cd, "
        //        + " (SELECT lct_nm FROM lct_info WHERE lct_cd = b.to_lct_cd ) AS to_lct_cd "
        //        + " FROM w_material_info AS b "
        //        + " LEFT JOIN d_material_info d ON b.mt_no = d.mt_no "
        //        + " LEFT JOIN d_bom_detail a ON b.mt_no = a.mt_no "
        //        + " WHERE b.to_lct_cd LIKE '002%' "
        //        + " AND b.lct_sts_cd = '201' and (b.mt_sts_cd = '001' or  b.mt_sts_cd ='004') "
        //        + " AND ('" + mt_no + "'='' OR b.mt_no LIKE '%" + mt_no + "%') "
        //        + " AND ('" + bom_no + "'='' OR b.rel_bom LIKE '%" + bom_no + "%')) "
        //        + " AND ('" + mt_barcode + "'='' OR b.mt_barcode LIKE '%" + mt_barcode + "%') "
        //        + " AND ('" + mt_sts_cd + "'='' OR b.mt_sts_cd LIKE '%" + mt_sts_cd + "%') "
        //        + " AND ('" + lct_cd + "'='' OR b.to_lct_cd LIKE '%" + lct_cd + "%') "
        //        + " GROUP BY b.mt_cd order by b.rel_bom  "
        //        ;
        //    string countSql = " SELECT COUNT(myData.wmtid) FROM ( " + tempSql + " ) myData ";
        //    string viewSql = " SELECT * FROM ( " + tempSql + " ) myData WHERE myData.RowNum BETWEEN " + list["start"] + " AND " + list["end"] + list["orderBy"];
        //    return SearchAndPaging(countSql, viewSql, int.Parse(list["index"]), int.Parse(list["size"]));
        //    //var data = new DataTable();
        //    //using (var cmd = db.Database.Connection.CreateCommand())
        //    //{
        //    //    db.Database.Connection.Open();
        //    //    cmd.CommandText = sql.ToString();
        //    //    using (var reader = cmd.ExecuteReader())
        //    //    {
        //    //        data.Load(reader);
        //    //    }
        //    //}
        //    //var result = GetJsonPersons(data);
        //    //return result;
        //}

        //public JsonResult searchmanual_wip() {
        //    // Get Data from ajax function
        //    var mt_no = Request["mt_no"];
        //    var mt_barcode = Request["mt_barcode"];
        //    var mt_sts_cd = Request["mt_sts_cd"];
        //    var lct_cd = Request["lct_cd"];
        //    var list = (from a in db.lct_info
        //                join b in db.w_material_info
        //               on a.lct_cd equals b.lct_cd
        //                join m in db.d_material_info
        //                on b.mt_no equals m.mt_no
        //                into c
        //                from d in c.DefaultIfEmpty()
        //                where b.to_lct_cd.StartsWith("002") && b.lct_sts_cd == "201" && (mt_no == "" || b.mt_no.Contains(mt_no)) && (mt_barcode == "" || b.mt_qrcode.Contains(mt_barcode)) && (mt_sts_cd == "" || b.mt_sts_cd.Contains(mt_sts_cd)) && (lct_cd == "" || b.to_lct_cd.Contains(lct_cd))
        //                select new {
        //                    reg_id = b.reg_id,
        //                    reg_dt = b.reg_dt,
        //                    chg_id = b.chg_id,
        //                    chg_dt = b.chg_dt,
        //                    mt_cd = b.mt_cd,
        //                    bb_no = b.bb_no,
        //                    remark = b.remark,
        //                    from_lct_cd = db.lct_info.Where(x => x.lct_cd == b.from_lct_cd).Select(x => x.lct_nm),
        //                    to_lct_cd = db.lct_info.Where(x => x.lct_cd == b.to_lct_cd).Select(x => x.lct_nm),
        //                    gr_qty = b.gr_qty,
        //                    wmtid = b.wmtid,
        //                    mt_no = b.mt_no,
        //                    mt_nm = d.mt_nm,
        //                    lct_nm = db.lct_info.Where(x => x.lct_cd == b.lct_cd).Select(x => x.lct_nm),
        //                    lct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002" && x.dt_cd == b.lct_sts_cd).Select(x => x.dt_nm),
        //                    mt_type = db.comm_dt.Where(x => x.mt_cd == "COM004" && x.dt_cd == d.mt_type).Select(x => x.dt_nm),
        //                    mt_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == b.mt_sts_cd).Select(x => x.dt_nm),
        //                }).ToList();


        //    return Json(list, JsonRequestBehavior.AllowGet);
        //}

        public List<Dictionary<string, object>> GetTableRows1(DataTable data)
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
        public JsonResult GetJsonPersons1(DataTable data)
        {
            var lstPersons = GetTableRows(data);
            return Json(lstPersons, JsonRequestBehavior.AllowGet);
        }
        #endregion
        #endregion

      

        #region Shipping_Manual(WIP)
        public ActionResult Shipping_Manual()
        {
            return View();
        }

        public ActionResult getshippingmanual_wip(string shelf_cd)
        {
            if (shelf_cd == null) { shelf_cd = ""; }
            var list = (from a in db.lct_info
                        join b in db.w_material_info
                        on a.lct_cd equals b.lct_cd
                        join m in db.d_material_info
                        on b.mt_no equals m.mt_no
                        into c
                        from d in c.DefaultIfEmpty()
                        where (shelf_cd == "" || a.shelf_cd.Contains(shelf_cd)) && (a.lct_cd.StartsWith("002")) && (b.lct_sts_cd == "101")
                        select new
                        {
                            reg_id = b.reg_id,
                            reg_dt = b.reg_dt,
                            chg_id = b.chg_id,
                            chg_dt = b.chg_dt,
                            mt_cd = b.mt_cd,
                            bb_no = b.bb_no,
                            remark = b.remark,
                            from_lct_cd = db.lct_info.Where(x => x.lct_cd == b.from_lct_cd).Select(x => x.lct_nm),
                            to_lct_cd = db.lct_info.Where(x => x.lct_cd == b.to_lct_cd).Select(x => x.lct_nm),
                            gr_qty = b.gr_qty,
                            wmtid = b.wmtid,
                            mt_no = b.mt_no,
                            mt_nm = d.mt_nm,
                            lct_nm = db.lct_info.Where(x => x.lct_cd == b.lct_cd).Select(x => x.lct_nm),
                            lct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002" && x.dt_cd == b.lct_sts_cd).Select(x => x.dt_nm),
                            mt_type = db.comm_dt.Where(x => x.mt_cd == "COM004" && x.dt_cd == d.mt_type).Select(x => x.dt_nm),
                            mt_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == b.mt_sts_cd).Select(x => x.dt_nm),
                        }).ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /*
        public JsonResult searchshippingmanual_wip( string shelf_cd ) {
            // Get Data from ajax function
            var mt_no = Request["mt_no"];
            var mt_barcode = Request["mt_barcode"];
            var mt_sts_cd = Request["mt_sts_cd"];
            var lct_cd = Request["lct_cd"];
            if (shelf_cd == null) { shelf_cd = ""; }
            var list = (from a in db.lct_info
                        join b in db.w_material_info
                       on a.lct_cd equals b.lct_cd

                        join m in db.d_material_info
                        on b.mt_no equals m.mt_no

                        into c
                        from d in c.DefaultIfEmpty()
                        where (shelf_cd == "" || a.shelf_cd.Contains(shelf_cd)) && (a.lct_cd.StartsWith("002")) && (b.lct_sts_cd == "101")
                        && (mt_no == "" || b.mt_no.Contains(mt_no)) && (mt_barcode == "" || b.mt_qrcode.Contains(mt_barcode)) && (mt_sts_cd == "" || b.mt_sts_cd.Contains(mt_sts_cd)) && (lct_cd == "" || b.to_lct_cd.Contains(lct_cd))
                        select new {
                            reg_id = b.reg_id,
                            reg_dt = b.reg_dt,
                            chg_id = b.chg_id,
                            chg_dt = b.chg_dt,
                            mt_cd = b.mt_cd,
                            bb_no = b.bb_no,
                            remark = b.remark,
                            gr_qty = b.gr_qty,
                            wmtid = b.wmtid,
                            mt_no = b.mt_no,
                            mt_nm = d.mt_nm,
                            lct_nm = db.lct_info.Where(x => x.lct_cd == b.lct_cd).Select(x => x.lct_nm),
                            from_lct_cd = db.lct_info.Where(x => x.lct_cd == b.from_lct_cd).Select(x => x.lct_nm),
                            to_lct_cd = db.lct_info.Where(x => x.lct_cd == b.to_lct_cd).Select(x => x.lct_nm),
                            lct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002" && x.dt_cd == b.lct_sts_cd).Select(x => x.dt_nm),
                            mt_type = db.comm_dt.Where(x => x.mt_cd == "COM004" && x.dt_cd == d.mt_type).Select(x => x.dt_nm),
                            mt_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == b.mt_sts_cd).Select(x => x.dt_nm),
                        }).ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }
         * */
        public JsonResult searchshippingmanual_wip(Pageing pageing)
        {
            // Get Data from ajax function
            var mt_no = Request["mt_no"];
            var mt_barcode = Request["mt_barcode"];
            var mt_sts_cd = Request["mt_sts_cd"];
            var lct_cd = Request["lct_cd"];
            var bom_no = Request["bom_no"];

            //if(bom_no != null && bom_no != "")
            //    if (bom_no.Substring(0, 1) == "B") bom_no = bom_no.Substring(1, bom_no.Length - 1);             

            int pageIndex = pageing.page;
            int pageSize = pageing.rows;
            int start_r = pageing.page;
            if (start_r > 1)
            {
                start_r = ((pageIndex - 1) * pageSize);
            }
            int end_r = (pageIndex * pageSize);


            string order_by = "";
            if (pageing.sidx != null)
            {
                order_by = " ORDER BY " + pageing.sidx + " " + pageing.sord;
            }

            if (pageing.sidx == null)
            {
                order_by = " ORDER BY MyDerivedTable.bom_no, MyDerivedTable.mt_cd DESC ";
            }

            string tempSql = " SELECT b.rel_bom as bom_no, b.input_dt, b.output_dt, b.worker_id, b.reg_id, b.reg_dt, b.chg_id, b.chg_dt, b.mt_cd, b.bb_no, b.remark, b.gr_qty, b.wmtid, b.mt_no, c.mt_nm, a.lct_nm, b.mt_qrcode, b.mt_sts_cd, b.to_lct_cd, "
                + " ROW_NUMBER() OVER (ORDER BY b.mt_cd DESC) AS RowNum, "
                + " (SELECT lct_nm FROM lct_info WHERE lct_cd = b.from_lct_cd) AS departure, "
                + " (SELECT lct_nm FROM lct_info WHERE lct_cd = b.to_lct_cd) AS destination, "
                + " (SELECT dt_nm FROM comm_dt WHERE dt_cd = b.lct_sts_cd AND mt_cd = 'WHS002') AS lct_sts_nm, "
                + " (SELECT dt_nm FROM comm_dt WHERE dt_cd = c.mt_type AND mt_cd = 'COM004') AS mt_type, "
                + " (SELECT dt_nm FROM comm_dt WHERE dt_cd = b.mt_sts_cd AND mt_cd = 'WHS005') AS mt_sts_nm "
                + " FROM lct_info a "
                + " LEFT JOIN w_material_info b ON a.lct_cd = b.lct_cd "
                + " LEFT JOIN d_material_info c ON b.mt_no = c.mt_no "
                + " WHERE substring(a.lct_cd,1,3) = '002' and substring(a.lct_cd,1,6) != '002007' AND b.lct_sts_cd = '101' AND b.del_yn = 'N' AND b.use_yn = 'Y' and b.mt_sts_cd ='001' and b.rel_bom is not null"
                + " AND ( b.mt_no = '" + mt_no + "') "
                + " AND (b.rel_bom = '" + bom_no + "') "
                //  + " AND ('" + lct_cd + "' = '' OR b.to_lct_cd LIKE '%" + lct_cd + "%') "
                ;

            string countSql = " SELECT COUNT(*) FROM ( " + tempSql + " ) a ";

            string viewSql = " SELECT * FROM ( " + tempSql + " ) AS MyDerivedTable WHERE MyDerivedTable.RowNum BETWEEN " + start_r + " AND " + end_r + order_by;

            var data = new DataTable();
            using (var cmd = db.Database.Connection.CreateCommand())
            {
                db.Database.Connection.Open();
                cmd.CommandText = viewSql;
                using (var reader = cmd.ExecuteReader())
                {
                    data.Load(reader);
                }
            }
            db.Database.Connection.Close();
            int totalRecords = db.Database.SqlQuery<int>(countSql).FirstOrDefault();

            var values = ConvertDataTableToJson(data);

            int totalPages = (int)Math.Ceiling((float)totalRecords / (float)pageSize);

            var result = new
            {
                total = totalPages,
                page = pageIndex,
                records = totalRecords,
                rows = values.Data,
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetpopupStyle_no()
        {
            var style_no = Request["style_no"];
            var md_cd = Request["md_cd"];
            StringBuilder varname1 = new StringBuilder();
            varname1.Append("SELECT a.sid, a.style_no, a.style_nm , a.md_cd  FROM d_style_info AS a ")
            .Append(" where (''='" + style_no + "' or a.style_no like '%" + style_no + "%') and (''='" + md_cd + "' or a.md_cd like '%" + md_cd + "%')   ORDER BY a.style_no");
            var data = new Excute_query().get_data_from_data_base(varname1);
            var result = GetJsonPersons(data);
            return Json(result.Data, JsonRequestBehavior.AllowGet);

        }

        public JsonResult searchshippingmanual_wip_bom(Pageing pageing)
        {
            // Get Data from ajax function
            var mt_no = Request["mt_no"];
            var mt_barcode = Request["mt_barcode"];
            var mt_sts_cd = Request["mt_sts_cd"];
            var style_no = Request["style_no"];
            var bom_no = Request["bom_no"];
            if (bom_no != "" && bom_no != null)
                if (bom_no.Substring(0, 1) == "B") bom_no = bom_no.Substring(1, bom_no.Length - 1);
            int pageIndex = pageing.page;
            int pageSize = pageing.rows;
            int start_r = pageing.page;
            if (start_r > 1)
            {
                start_r = ((pageIndex - 1) * pageSize);
            }
            int end_r = (pageIndex * pageSize);


            string order_by = "";
            if (pageing.sidx != null)
            {
                order_by = " ORDER BY " + pageing.sidx + " " + pageing.sord;
            }

            if (pageing.sidx == null)
            {
                order_by = " ORDER BY MyDerivedTable.bom_no desc ";
            }

            StringBuilder varname1 = new StringBuilder();
            varname1.Append("SELECT  ROW_NUMBER() OVER (ORDER BY a.rel_bom desc) AS RowNum, a.rel_bom AS bom_no, a.rel_bom AS bom_no1, a.mt_no, (SELECT i.mt_nm \n");
            varname1.Append("        FROM   d_material_info AS i \n");
            varname1.Append("        WHERE  i.mt_no = a.mt_no \n");
            varname1.Append("        LIMIT  1) AS mt_nm, b.style_no 	AS style_no	 \n");
            varname1.Append("	 FROM w_material_info AS a LEFT JOIN d_bom_info AS b ON a.rel_bom = b.bom_no \n");
            varname1.Append("		WHERE a.lct_sts_cd = '101' AND a.mt_sts_cd ='001' AND SUBSTRING(a.lct_cd,1,3) ='002' AND SUBSTRING(a.lct_cd,1,6) !='002007' AND a.rel_bom IS NOT NULL \n");
            varname1.Append("		  and (a.rel_bom like '%" + bom_no + "%' or ''='" + bom_no + "') and (a.mt_no like '%" + mt_no + "%' or ''='" + mt_no + "') and (b.style_no like '%" + style_no + "%' or ''='" + style_no + "') ");
            varname1.Append("			GROUP BY a.rel_bom, a.mt_no order by a.rel_bom desc");


            //   var result = GetJsonPersons(data);
            //  return Json(result.Data, JsonRequestBehavior.AllowGet);

            string countSql = " SELECT COUNT(*) FROM ( " + varname1 + " ) as a ";
            StringBuilder viewSql = new StringBuilder();
            viewSql.Append(" SELECT * FROM ( " + varname1 + " ) AS MyDerivedTable WHERE MyDerivedTable.RowNum BETWEEN " + start_r + " AND " + end_r + order_by);


            var data = new Excute_query().get_data_from_data_base(viewSql);
            int totalRecords = db.Database.SqlQuery<int>(countSql).FirstOrDefault();

            var values = ConvertDataTableToJson(data);

            int totalPages = (int)Math.Ceiling((float)totalRecords / (float)pageSize);

            var result = new
            {
                total = totalPages,
                page = pageIndex,
                records = totalRecords,
                rows = values.Data,
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /*
        public ActionResult Updateshippingmanual_wip(string id, string lct_cd, w_material_lct_hist c)
        {
            if (id != null)
            {
                var a2 = id.TrimStart('[').TrimEnd(']').Split(',');
                for (int i = 0; i < a2.Length; i++)
                {
                    w_material_info w_material_info = db.w_material_info.Find(int.Parse(a2[i]));
                    w_material_info.to_lct_cd = lct_cd;
                    w_material_info.lct_sts_cd = "201";
                    w_material_info.use_yn = "Y";
                    w_material_info.del_yn = "N";
                    w_material_info.chg_dt = DateTime.Now;
                    db.Entry(w_material_info).State = EntityState.Modified;
                    db.SaveChanges();

                    //insert lct_list
                    c.pdid = w_material_info.pdid;
                    c.mt_cd = w_material_info.mt_cd;
                    c.mt_no = w_material_info.mt_no;
                    c.lot_div_cd = w_material_info.lot_div_cd;
                    c.bbmp_sts_cd = w_material_info.bbmp_sts_cd;
                    c.mpo_no = w_material_info.mpo_no;
                    c.prounit_cd = w_material_info.prounit_cd;
                    c.gr_qty = w_material_info.gr_qty;
                    c.mdpo_no = w_material_info.mdpo_no;
                    c.date = w_material_info.date;
                    c.mt_barcode = w_material_info.mt_barcode;
                    c.mt_sts_cd = w_material_info.mt_sts_cd;
                    c.bb_no = w_material_info.bb_no;
                    c.lct_cd = w_material_info.lct_cd;
                    c.lct_sts_cd = w_material_info.lct_sts_cd;
                    c.from_lct_cd = w_material_info.from_lct_cd;
                    c.to_lct_cd = w_material_info.to_lct_cd;
                    c.output_dt = w_material_info.output_dt;
                    c.input_dt = w_material_info.input_dt;
                    c.worker_id = w_material_info.worker_id;
                    c.sd_no = w_material_info.sd_no;
                    c.chg_dt = DateTime.Now;
                    c.reg_dt = DateTime.Now;

                    if (ModelState.IsValid)
                    {
                        db.Entry(c).State = EntityState.Added;
                        db.SaveChanges(); // line that threw exception
                    }
                }
            }
            var sql = new StringBuilder();
            sql.Append("SELECT  (select lct_nm from lct_info where lct_cd=a.to_lct_cd) as to_lct_cd ,a.reg_id,a.reg_dt,a.chg_id,a.chg_dt,a.mt_cd,a.bb_no,a.remark,(select lct_nm from lct_info where lct_cd=a.from_lct_cd) as from_lct_cd ,a.gr_qty,a.wmtid,b.mt_no,b.mt_nm,(select dt_nm from comm_dt where mt_cd='COM004' and dt_cd=b.mt_type) as mt_type,(select dt_nm from comm_dt where mt_cd='WHS005' and dt_cd=a.mt_sts_cd) as mt_sts_cd,(select dt_nm from comm_dt where mt_cd='WHS002' and dt_cd=a.lct_sts_cd) as lct_sts_cd,(select lct_nm from lct_info where lct_cd=a.lct_cd) as lct_nm from w_material_info as a join d_material_info as b on a.mt_no=b.mt_no ")
                 .Append("where a.wmtid in (" + id + ")");
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
            var result = GetJsonPersons(data);
            return result;
        }
        */
        public ActionResult Updateshippingmanual_wip(string id, string worker_id)
        {
            try
            {
                if(id=="" || id == null)
                     { return Json(new { result = true, mess = chagain }, JsonRequestBehavior.AllowGet); }

                StringBuilder varname1 = new StringBuilder();
                varname1.Append("UPDATE w_material_info \n");
                varname1.Append("SET worker_id='root', lct_sts_cd='220',use_yn ='Y', del_yn ='N', mt_sts_cd ='002', from_lct_cd = lct_cd, output_dt ="+DateTime.Now.ToString("yyyyMMdd")+" \n");
                varname1.Append("WHERE w_material_info.wmtid IN ("+ id +")");

                var effect_row = new Excute_query().Execute_NonQuery(varname1);
                varname1.Clear();

                varname1.Append(" SELECT a.wmtid, a.worker_id,  a.rel_bom, a.rel_bom as rel_bom1, a.mt_no,a.mt_cd, b.mt_nm, a.mt_cd, c.lct_nm AS lct_nm, a.gr_qty, a.remark, \n");
                varname1.Append("  a.input_dt, a.output_dt,a.chg_dt, a.reg_dt, a.reg_id, a.chg_id,   \n");
                varname1.Append("	(select lct_nm from lct_info AS i WHERE  i.lct_cd = a.to_lct_cd) AS to_lct_cd,\n");
                varname1.Append("  (select lct_nm from lct_info AS i WHERE  i.lct_cd = a.from_lct_cd) AS from_lct_cd, \n");
                varname1.Append("	(select dt_nm from comm_dt AS i WHERE i.mt_cd ='WHS005' AND i.dt_cd = a.mt_sts_cd) AS mt_sts_cd, \n");
                varname1.Append("	(select dt_nm from comm_dt AS i WHERE i.mt_cd ='WHS002' AND i.dt_cd = a.mt_sts_cd) AS lct_sts_cd \n");
                varname1.Append("		FROM w_material_info AS a \n");
                varname1.Append("		LEFT JOIN d_material_info AS b ON a.mt_no = b.mt_no \n");
                varname1.Append("		LEFT JOIN lct_info AS c ON c.lct_cd = a.lct_cd \n");
                varname1.Append("		WHERE a.wmtid in (" + id + ")");

                var data_tb = new Excute_query().get_data_from_data_base(varname1);

                var data = GetJsonPersons(data_tb);
                return  Json(new { result = true,data = data.Data, mess = ss}, JsonRequestBehavior.AllowGet);

            }
            catch(Exception ex)
            {
                return Json(new { result = false, mess = exs}, JsonRequestBehavior.AllowGet);
            }
            //if (id != null)
            //{
            //    var a2 = id.TrimStart('[').TrimEnd(']').Split(',');
            //    for (int i = 0; i < a2.Length; i++)
            //    {
            //        w_material_info w_material_info = db.w_material_info.Find(int.Parse(a2[i]));

            //        var condition_1 = db.d_material_info.Where(x => x.mt_no == w_material_info.mt_no && x.consum_yn == "Y").ToList();
            //        var condition_2 = db.lct_info.Where(x => x.lct_cd.Contains("002") && x.ft_yn == "Y" && x.lct_cd == w_material_info.to_lct_cd).ToList();

            //        if (condition_1.Count > 0 && condition_2.Count > 0)
            //        {
            //            w_material_info.worker_id = worker_id;
            //            w_material_info.lct_sts_cd = "201";
            //            w_material_info.use_yn = "Y";
            //            w_material_info.del_yn = "N";
            //            w_material_info.chg_dt = DateTime.Now;
            //            w_material_info.mt_sts_cd = "005";
            //            // w_material_info.lct_cd = w_material_info.to_lct_cd; 
            //            // w_material_info.lct_cd = "020000000000000000"; 
            //            db.Entry(w_material_info).State = EntityState.Modified;
            //            db.SaveChanges();
            //        }
            //        else
            //        {
            //            w_material_info.worker_id = worker_id;
            //            w_material_info.lct_sts_cd = "220";
            //            w_material_info.use_yn = "Y";
            //            w_material_info.del_yn = "N";
            //            w_material_info.mt_sts_cd = "002"; //  add new 14.5
            //            w_material_info.from_lct_cd = w_material_info.lct_cd;         //  add new 14.5
            //                                                                          // w_material_info.lct_cd = w_material_info.to_lct_cd;        //  add new 14.5
            //                                                                          //  w_material_info.lct_cd = "020000000000000000";        //  add new 14.5
            //            w_material_info.chg_dt = DateTime.Now;
            //            db.Entry(w_material_info).State = EntityState.Modified;
            //            db.SaveChanges();
            //        }

            //        ////insert lct_list
            //        //c.pdid = w_material_info.pdid;
            //        //c.mt_cd = w_material_info.mt_cd;
            //        //c.mt_no = w_material_info.mt_no;
            //        //c.lot_div_cd = w_material_info.lot_div_cd;
            //        //c.bbmp_sts_cd = w_material_info.bbmp_sts_cd;
            //        //c.mpo_no = w_material_info.mpo_no;
            //        //c.prounit_cd = w_material_info.prounit_cd;
            //        //c.gr_qty = w_material_info.gr_qty;
            //        //c.mdpo_no = w_material_info.mdpo_no;
            //        //c.date = w_material_info.date;
            //        //c.mt_barcode = w_material_info.mt_barcode;
            //        //c.mt_sts_cd = w_material_info.mt_sts_cd;
            //        //c.bb_no = w_material_info.bb_no;
            //        //c.lct_cd = w_material_info.lct_cd;
            //        //c.lct_sts_cd = w_material_info.lct_sts_cd;
            //        //c.from_lct_cd = w_material_info.from_lct_cd;
            //        //c.to_lct_cd = w_material_info.to_lct_cd;
            //        //c.output_dt = w_material_info.output_dt;
            //        //c.input_dt = w_material_info.input_dt;
            //        //c.worker_id = w_material_info.worker_id;
            //        //c.sd_no = w_material_info.sd_no;
            //        //c.chg_dt = DateTime.Now;
            //        //c.reg_dt = DateTime.Now;

            //        //if (ModelState.IsValid)
            //        //{
            //        //    db.Entry(c).State = EntityState.Added;
            //        //    db.SaveChanges(); // line that threw exception
            //        //}
            //    }
            //    return Json(true, JsonRequestBehavior.AllowGet);
            //}

            ////var sql = new StringBuilder();
            ////sql.Append("SELECT  (select lct_nm from lct_info where lct_cd=a.to_lct_cd) as to_lct_cd ,a.reg_id,a.reg_dt,a.chg_id,a.chg_dt,a.mt_cd,a.bb_no,a.remark,a.worker_id as worker,(select lct_nm from lct_info where lct_cd=a.from_lct_cd) as from_lct_cd , a.gr_qty,a.wmtid,a.mt_no,b.mt_nm,(select dt_nm from comm_dt where mt_cd='COM004' and dt_cd=b.mt_type) as mt_type,(select dt_nm from comm_dt where mt_cd='WHS005' and dt_cd=a.mt_sts_cd) as mt_sts_nm,(select dt_nm from comm_dt where mt_cd='WHS002' and dt_cd=a.lct_sts_cd) as lct_sts_cd,(select lct_nm from lct_info where lct_cd=a.lct_cd) as lct_nm from w_material_info as a left join d_material_info as b on a.mt_no=b.mt_no ")
            ////     .Append("where a.wmtid in (" + id + ")");
            ////var data = new DataTable();
            ////using (var cmd = db.Database.Connection.CreateCommand())
            ////{
            ////    db.Database.Connection.Open();
            ////    cmd.CommandText = sql.ToString();
            ////    using (var reader = cmd.ExecuteReader())
            ////    {
            ////        data.Load(reader);
            ////    }
            ////}
            ////var result = GetJsonPersons(data);
            ////return result;
            //return Json(false, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Picking_Scan_F
        public ActionResult Picking_Scan_F()
        {
            return View();
        }
        #endregion

        #region Receiving_Direction_gr
        public ActionResult Receiving_Direction_gr()
        {
            return View();
        }

        public ActionResult lct_destination_WIP(lct_info lct_info)
        {
            var lists = db.lct_info.Where(item => item.lct_cd.StartsWith("002")).
              OrderBy(item => item.lct_cd).ToList();
            return Json(lists, JsonRequestBehavior.AllowGet);
        }


        public JsonResult searchsd_WIP()
        {
            var sd_no = Request["sd_no"]; //mpo_no
            var lct_cd = Request["lct_cd"];
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
            sql.Append(" SELECT (select dt_nm from comm_dt where comm_dt.mt_cd='WHS007' and comm_dt.dt_cd=B.sdm_sts_cd) as sd_sts_cd,B.* FROM w_sd_info AS A ")
                            .Append("JOIN w_sd_detail AS B ON A.sd_no=B.sd_no ")
                            .Append(" LEFT JOIN d_material_info AS C ON B.mt_no=C.mt_no")
                            .Append(" WHERE B.sdm_sts_cd='003'  ")
                            .Append("AND ('" + sd_no + "'='' OR B.sd_no like '%" + sd_no + "%' )")
                            .Append("AND ('" + lct_cd + "'='' OR A.lct_cd like '%" + lct_cd + "%' )")
                            .Append("AND ('" + start + "'='' OR CONVERT(A.work_dt,DATE)=CONVERT('" + start + "',DATE))")
                            .Append("AND ('" + end + "'='' OR CONVERT(A.work_dt,DATE)=CONVERT('" + end + "',DATE))");
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

        public ActionResult Getw_rd_info_WIP()
        {
            var list = (from p in db.w_rd_info
                        orderby p.rd_no descending
                        select new
                        {
                            rid = p.rid,
                            rd_no = p.rd_no,
                            rd_sts_cd = db.comm_dt.Where(x => x.dt_cd == p.rd_sts_cd && x.mt_cd == "WHS004").Select(x => x.dt_nm),
                           
                            reg_id = p.reg_id,
                            reg_dt = p.reg_dt,
                        }).ToList();
            return Json(new { rows = list }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Receiving_Directions_WIP
        public ActionResult Receiving_Directions_WIP()
        {
            return View();
        }
        public JsonResult getPopupSD(w_sd_info w_sd_info)
        {

            var vaule = db.w_sd_info.ToList();
            var Data = new { rows = vaule };
            return Json(Data, JsonRequestBehavior.AllowGet);
        }
        public ActionResult getshipinfo_Directions_WIP()
        {
            var list = (from d in db.w_sd_info
                        join p in db.lct_info
                        on d.lct_cd equals p.lct_cd
                        join c in db.comm_dt
                        on d.sd_sts_cd equals c.dt_cd
                        where c.mt_cd == "WHS007" && d.sd_sts_cd == "003"
                        orderby d.sd_no descending
                        select new
                        {
                            sid = d.sid,
                            sd_no = d.sd_no,
                            lct_cd = p.lct_nm,
                            sd_sts_cd = c.dt_nm,
                           

                        }).ToList();
            return Json(new { rows = list }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult lct_destination(lct_info lct_info)
        {
            var sql = new StringBuilder();
            sql.Append(" select 'MMS' d_cd, lct_cd, lct_nm from lct_info where ft_yn = 'Y' ")
                .Append("and SUBSTRING(lct_cd, 1, 3) = '002' ")
                .Append("union all ")
                .Append("select 'MWMS' d_cd, lct_cd, Lct_nm ")
                .Append("from lct_info where mv_yn = 'Y' and SUBSTRING(lct_cd, 1, 3) = '001'");
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
            //return Json(lists, JsonRequestBehavior.AllowGet);
        }

        public JsonResult searchRequest_Directions_WIP()
        {
            var sd_no = Request["sd_no"]; //mpo_no
            var lct_cd = Request["lct_cd"];
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
            sql.Append("  SELECT b.lct_nm as lct_cd,c.dt_nm as sd_sts_cd,a.*")
                            .Append(" FROM w_sd_info as a ")
                            .Append(" join lct_info as b ")
                            .Append(" on a.lct_cd = b.lct_cd  ")
                            .Append(" join comm_dt as c ")
                            .Append(" on a.sd_sts_cd=c.dt_cd   ")
                            .Append(" join w_sd_detail as d ")
                            .Append(" on a.sd_no=d.sd_no   ")
                             .Append("where c.mt_cd='WHS007' && a.sd_sts_cd='003' ")
                            .Append("AND ('" + sd_no + "'='' OR a.sd_no like '%" + sd_no + "%' )")
                            .Append("AND ('" + lct_cd + "'='' OR b.lct_nm like '%" + lct_cd + "%' )")
                            .Append("AND ('" + start + "'='' OR CONVERT(a.work_dt,DATE)=CONVERT('" + start + "',DATE))")
                            .Append("AND ('" + end + "'='' OR CONVERT(a.work_dt,DATE)=CONVERT('" + end + "',DATE))");
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

        #endregion

        #region Shipping_direc_WIP
        public ActionResult SP_direction_WIP()
        {
            return View();
        }
        public ActionResult getshipinfo_WIP()
        {
            var list = (from d in db.w_sd_info
                        join p in db.lct_info
                        on d.lct_cd equals p.lct_cd
                        join c in db.comm_dt
                        on d.sd_sts_cd equals c.dt_cd
                       
                        orderby d.sd_no descending
                        select new
                        {
                            sid = d.sid,
                            sd_no = d.sd_no,
                            lct_cd = p.lct_nm,
                            sd_sts_cd = c.dt_nm,
                          

                        }).ToList();
            return Json(new { rows = list }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult searchRequest_WIP()
        {
            var sd_no = Request["sd_no"]; //mpo_no
            var lct_cd = Request["lct_cd"];
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
            sql.Append(" SELECT (select lct_nm from lct_info where a.lct_cd=lct_info.lct_cd) as lct_cd,(select dt_nm from comm_dt where comm_dt.mt_cd='WHS007' and comm_dt.dt_cd=a.sd_sts_cd) as sd_sts_cd,a.*")
                            .Append(" FROM w_sd_info as a ")
                            .Append(" where  a.div_cd='WIP' ")
                            .Append("AND ('" + sd_no + "'='' OR a.sd_no like '%" + sd_no + "%' )")
                            .Append("AND ('" + lct_cd + "'='' OR a.lct_cd like '%" + lct_cd + "%' )")
                            .Append("AND ('" + start + "'='' OR CONVERT(a.work_dt,DATE)=CONVERT('" + start + "',DATE))")
                            .Append("AND ('" + end + "'='' OR CONVERT(a.work_dt,DATE)=CONVERT('" + end + "',DATE))");
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

        #endregion

        #region Shippping_direction_gr(WIP)
        public ActionResult Shippping_direction_WIP()
        {
            return View();
        }
        public ActionResult location_sr_shipp_wip(lct_info lct_info)
        {
            var lists = db.lct_info.Where(item => item.lct_cd.StartsWith("002")).
              OrderBy(item => item.lct_cd).ToList();
            return Json(lists, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Getmt_list_WIP()
        {
            //var sql = new StringBuilder();

            // sql.Append("SELECT sum(a.gr_qty) mt_qty,b.mt_no ,a.wmtid,b.mt_nm,(0) as Available,(0) as reg,b.unit_cd,")
            //.Append(" CONCAT(FORMAT(b.spec, '#,#'),' ',b.spec_unit) as spec,CONCAT(FORMAT(b.width, '#,#'),' ',b.width_unit) as width ")
            //.Append(" ,CONCAT(FORMAT(b.area, '#,#'), ' ', b.area_unit) AS area")
            //.Append(" FROM  w_material_info as a   join d_material_info as b on a.mt_no=b.mt_no ")
            //.Append(" left join d_bom_detail as c on b.mt_no=c.mt_no ")
            //.Append(" left join s_order_factory_info as d on c.bom_no=d.bom_no ")
            //.Append("  where a.lct_sts_cd='101' and a.mt_sts_cd='001' and substring(a.lct_cd,1,3)='002'  group by a.mt_no ");

            //string sql = " SELECT a.wmtid, a.mt_no, b.mt_nm, b.gr_qty, count(a.wmtid) mt_bundle_qty "
            //    + ",sum(a.gr_qty) mt_qty ,"
            //    + "(select sum(gr_qty) from w_material_info where mt_no = a.mt_no and lct_sts_cd='101' and mt_sts_cd in ('001','002') and lct_cd in (select lct_cd from lct_info where (wp_yn = 'Y' or mv_yn = 'Y') and substring(lct_cd, 1, 3)='002')) available_qty "
            //    + " ,(0) AS req_bundle_qty "
            //    + " ,(0) AS req_qty "
            //    + " ,b.unit_cd "
            //    + " ,CONCAT(FORMAT(b.spec, '#,#'), ' ', b.spec_unit) AS spec "
            //    + " ,CONCAT(FORMAT(b.width, '#,#'), ' ', b.width_unit) AS width "
            //    + " ,CONCAT(FORMAT(b.area, '#,#'), ' ', b.area_unit) AS area "
            //    + " ,CONCAT(FORMAT(SUM(b.gr_qty), '#,#'), ' ', b.unit_cd) AS group_unit "
            //    + " FROM w_material_info AS a JOIN d_material_info AS b ON a.mt_no=b.mt_no "
            //    + " WHERE a.mt_sts_cd in ('001','002') "
            //    + " AND a.lct_cd in (select lct_cd from lct_info where (wp_yn = 'Y' or mv_yn = 'Y') and substring(lct_cd, 1, 3)='002') "
            //    + " GROUP BY a.mt_no "
            //    ;
            StringBuilder varname1 = new StringBuilder();
            varname1.Append("SELECT a.bdid AS bdid,a.bom_no, a.mt_no,   ( 0 ) AS Available, \n");
            varname1.Append("       ( 0 )  AS reg, b.mt_nm, b.unit_cd,  b.gr_qty, \n");
            varname1.Append("		 Concat(FORMAT(b.spec, '#,#'), ' ', b.spec_unit) AS spec, \n");
            varname1.Append("		 Concat(FORMAT(b.width, '#,#'), ' ', b.width_unit) AS width , \n");
            varname1.Append("		  Concat(Format(b.area, '#,#'), ' ', b.area_unit)      AS area, \n");
            varname1.Append("       Concat(ifnull(Format(Sum(b.gr_qty), '#,#'),1), ' ', ifnull(b.unit_cd,'')) AS group_unit, \n");
            varname1.Append(" \n");
            varname1.Append("		 (SELECT count(gr_qty) \n");
            varname1.Append("			FROM   w_material_info AS i \n");
            varname1.Append("				WHERE  i.lct_sts_cd = '101' \n");
            varname1.Append("     			  AND i.mt_sts_cd = '001' \n");
            varname1.Append("   			  	  AND Substring(i.lct_cd, 1, 3) = '002' \n");
            varname1.Append("						 AND i.rel_bom IS NOT NULL AND i.rel_bom != '' \n");
            varname1.Append("   				    AND i.rel_bom = a.bom_no AND i.mt_no = a.mt_no \n");
            varname1.Append("  					     GROUP BY i.rel_bom, i.mt_no order by i.rel_bom)  AS mt_bundle_qty, \n");
            varname1.Append("		(SELECT SUM(gr_qty) \n");
            varname1.Append("			FROM   w_material_info AS i \n");
            varname1.Append("				WHERE  i.lct_sts_cd = '101' \n");
            varname1.Append("     			  AND i.mt_sts_cd = '001' \n");
            varname1.Append("   			  	  AND Substring(i.lct_cd, 1, 3) = '002' \n");
            varname1.Append("						 AND i.rel_bom IS NOT NULL AND i.rel_bom != '' \n");
            varname1.Append("   				    AND i.rel_bom = a.bom_no AND i.mt_no = a.mt_no \n");
            varname1.Append("  					     GROUP BY i.rel_bom, i.mt_no order by i.rel_bom)  AS mt_qty \n");
            //varname1.Append("  		(SELECT Sum(gr_qty) \n");
            //varname1.Append("        FROM   w_material_info \n");
            //varname1.Append("        WHERE  mt_no = a.mt_no \n");
            //varname1.Append("               AND lct_sts_cd = '101' \n");
            //varname1.Append("               AND mt_sts_cd IN ( '001', '002' ) \n");
            //varname1.Append("               AND lct_cd IN (SELECT lct_cd \n");
            //varname1.Append("                              FROM   lct_info \n");
            //varname1.Append("                              WHERE  ( wp_yn = 'Y' \n");
            //varname1.Append("                                        OR mv_yn = 'Y' ) \n");
            //varname1.Append("                                     AND Substring(lct_cd, 1, 3) = '002')) AS available_qty \n");
            //varname1.Append("                                                             \n");
            varname1.Append("		FROM  d_bom_detail AS a \n");
            varname1.Append("		LEFT JOIN d_material_info AS b ON a.mt_no = b.mt_no \n");
            varname1.Append("					 WHERE a.bom_no IN \n");
            varname1.Append("								(SELECT i.rel_bom \n");
            varname1.Append("								FROM   w_material_info AS i \n");
            varname1.Append("								WHERE  i.lct_sts_cd = '101' \n");
            varname1.Append("     								  AND i.mt_sts_cd = '001' \n");
            varname1.Append("    					 		 	 AND Substring(i.lct_cd, 1, 3) = '002' AND i.rel_bom IS NOT NULL AND i.rel_bom != '' \n");
            varname1.Append("    							   AND i.rel_bom = a.bom_no AND i.mt_no = a.mt_no \n");
            varname1.Append("     							  GROUP BY i.rel_bom, i.mt_no order by i.rel_bom) \n");
            varname1.Append("GROUP BY bom_no, mt_no ORDER BY bom_no");


            var data = new DataTable();
            using (var cmd = db.Database.Connection.CreateCommand())
            {
                db.Database.Connection.Open();
                cmd.CommandText = varname1.ToString();
                using (var reader = cmd.ExecuteReader())
                {
                    data.Load(reader);
                }
            }
            db.Database.Connection.Close();
            var result = GetJsonPersons1(data);

            return result;

        }

        public ActionResult searchmt_list_WIP()
        {
            var fo_no = Request["fo_no"];
            var bom_no = Request["bom_no"];
            var mt_no = Request["mt_no"];
            var location = Request["location"];


            //var sql = new StringBuilder();

            //sql.Append("SELECT sum(a.gr_qty) mt_qty,b.mt_no ,a.wmtid,b.mt_nm,(0) as Available,(0) as reg  ,CONCAT(FORMAT(b.spec, '#,#'),' ',b.spec_unit) as spec,CONCAT(FORMAT(b.width, '#,#'),' ',b.width_unit) as width  FROM  w_material_info as a   join d_material_info as b on a.mt_no=b.mt_no  left join d_bom_detail as c on b.mt_no=c.mt_no left join s_order_factory_info as d on c.bom_no=d.bom_no where a.lct_sts_cd='101' and a.mt_sts_cd='001' and substring(a.lct_cd,1,3)='002'  ")

            // varname1.Append(" and ('" + fo_no + "'='' OR  d.fo_no like '%" + fo_no + "%')")
            //varname1.Append("AND ('" + bom_no + "'='' OR  c.bom_no like '%" + bom_no + "%')")
            //varname1.Append("AND ('" + mt_no + "'='' OR  a.mt_no like '%" + mt_no + "%')")
            //.Append("AND ('" + location + "'='' OR  a.lct_cd like '%" + location + "%')")
            //.Append(" group by a.mt_no");
            StringBuilder varname1 = new StringBuilder();
            varname1.Append("SELECT a.bdid AS bdid,a.bom_no, a.mt_no,   ( 0 ) AS Available, \n");
            varname1.Append("       ( 0 )  AS reg, b.mt_nm, b.unit_cd,  b.gr_qty, \n");
            varname1.Append("		 Concat(FORMAT(b.spec, '#,#'), ' ', b.spec_unit) AS spec, \n");
            varname1.Append("		 Concat(FORMAT(b.width, '#,#'), ' ', b.width_unit) AS width , \n");
            varname1.Append("		  Concat(Format(b.area, '#,#'), ' ', b.area_unit)      AS area, \n");
            varname1.Append("       Concat(ifnull(Format(Sum(b.gr_qty), '#,#'),1), ' ', ifnull(b.unit_cd,'')) AS group_unit, \n");
            varname1.Append(" \n");
            varname1.Append("		 (SELECT count(gr_qty) \n");
            varname1.Append("			FROM   w_material_info AS i \n");
            varname1.Append("				WHERE  i.lct_sts_cd = '101' \n");
            varname1.Append("     			  AND i.mt_sts_cd = '001' \n");
            varname1.Append("   			  	  AND Substring(i.lct_cd, 1, 3) = '002' \n");
            varname1.Append("						 AND i.rel_bom IS NOT NULL AND i.rel_bom != '' \n");
            varname1.Append("   				    AND i.rel_bom = a.bom_no AND i.mt_no = a.mt_no \n");
            varname1.Append("  					     GROUP BY i.rel_bom, i.mt_no order by i.rel_bom)  AS mt_bundle_qty, \n");
            varname1.Append("		(SELECT SUM(gr_qty) \n");
            varname1.Append("			FROM   w_material_info AS i \n");
            varname1.Append("				WHERE  i.lct_sts_cd = '101' \n");
            varname1.Append("     			  AND i.mt_sts_cd = '001' \n");
            varname1.Append("   			  	  AND Substring(i.lct_cd, 1, 3) = '002' \n");
            varname1.Append("						 AND i.rel_bom IS NOT NULL AND i.rel_bom != '' \n");
            varname1.Append("   				    AND i.rel_bom = a.bom_no AND i.mt_no = a.mt_no \n");
            varname1.Append("  					     GROUP BY i.rel_bom, i.mt_no order by i.rel_bom)  AS mt_qty \n");
            varname1.Append("		FROM  d_bom_detail AS a \n");
            varname1.Append("		LEFT JOIN d_material_info AS b ON a.mt_no = b.mt_no \n");
            varname1.Append("					 WHERE a.bom_no IN \n");
            varname1.Append("								(SELECT i.rel_bom \n");
            varname1.Append("								FROM   w_material_info AS i \n");
            varname1.Append("								WHERE  i.lct_sts_cd = '101' \n");
            varname1.Append("     								  AND i.mt_sts_cd = '001' \n");
            varname1.Append("    					 		 	 AND Substring(i.lct_cd, 1, 3) = '002' AND i.rel_bom IS NOT NULL AND i.rel_bom != '' \n");
            varname1.Append("    							   AND i.rel_bom = a.bom_no AND i.mt_no = a.mt_no \n");
            varname1.Append("     							  GROUP BY i.rel_bom, i.mt_no order by i.rel_bom) \n");
            varname1.Append("AND ('" + bom_no + "'='' OR  a.bom_no like '%" + bom_no + "%')");
            varname1.Append("AND ('" + mt_no + "'='' OR  a.mt_no like '%" + mt_no + "%')");
            varname1.Append("GROUP BY bom_no, mt_no ORDER BY bom_no");
            var data = new DataTable();
            using (var cmd = db.Database.Connection.CreateCommand())
            {
                db.Database.Connection.Open();
                cmd.CommandText = varname1.ToString();
                using (var reader = cmd.ExecuteReader())
                {
                    data.Load(reader);
                }
            }
            db.Database.Connection.Close();
            var result = GetJsonPersons(data);
            return result;


        }
        public ActionResult getReDetails(string sd_no)
        {
            StringBuilder varname1 = new StringBuilder();
            varname1.Append("SELECT a.sdid AS sdid, a.sd_no AS sd_no, \n");
            varname1.Append("		 a.mt_no AS mt_no, c.mt_nm AS mt_nm, \n");
            varname1.Append("			a.req_qty AS d_mt_qty, concat(ifnull(c.width,0),ifnull(c.width_unit, '')) AS width,  concat(ifnull(c.spec,0),ifnull(c.spec_unit, '')) AS spec \n");
            varname1.Append("		   FROM w_sd_detail AS a \n");
            varname1.Append(" 			left JOIN w_sd_info AS b ON a.sd_no = b.sd_no \n");
            varname1.Append("			left	JOIN d_material_info AS c ON c.mt_no = a.mt_no \n");
            varname1.Append(" WHERE a.sd_no = '" + sd_no + "' ORDER BY a.mt_no");
            var data = new Excute_query().get_data_from_data_base(varname1);
            var result = GetJsonPersons(data);
            return Json(new { rows = result.Data }, JsonRequestBehavior.AllowGet);

            //var list = (from d in db.w_sd_detail
            //            join b in db.w_sd_info
            //            on d.sd_no equals b.sd_no
            //            join p in db.d_material_info
            //            on d.mt_no equals p.mt_no
            //            where d.sd_no == sd_no
            //            select new
            //            {
            //                sdid = d.sdid,
            //                sd_no = d.sd_no,
            //                mt_no = d.mt_no,
            //                mt_nm = p.mt_nm,
            //                d_mt_qty = d.req_qty,
            //                width = p.width + p.width_unit,
            //                spec = p.spec + p.spec_unit,
            //            }).ToList();

            //return Json(new { rows = list }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Detail_mt_WIP(string id, string req_qty, string req_bundle)
        {
            if ((id != null) && (req_qty != null) && (id != "") && (req_qty != "") && (!string.IsNullOrEmpty(req_bundle)))
            {
                var a = id.TrimStart('[').TrimEnd(']').Split(',');
                var b = req_qty.TrimStart('[').TrimEnd(']').Split(',');
                var c = req_bundle.TrimStart('[').TrimEnd(']').Split(',');

                //var sql = new StringBuilder();
                //sql.Append("SELECT sum(a.gr_qty) mt_qty,b.mt_no ,a.wmtid,b.mt_nm,(0) as available_qty,(0) as req_qty, (0) AS req_bundle_qty  ,CONCAT(FORMAT(b.spec, '#,#'),' ',b.spec_unit) as spec,CONCAT(FORMAT(b.width, '#,#'),' ',b.width_unit) as width FROM  w_material_info as a   join d_material_info as b on a.mt_no=b.mt_no  where a.lct_sts_cd='101' and a.mt_sts_cd='001' and substring(a.lct_cd,1,3)='002'  ")
                // .Append(" and a.wmtid in (" + id + ") group by a.mt_no  ");
                string sql = " SELECT a.wmtid, a.mt_no, b.mt_nm, b.gr_qty, count(a.wmtid) mt_bundle_qty "
               + ",sum(a.gr_qty) mt_qty ,"
               + "(select sum(gr_qty) from w_material_info where mt_no = a.mt_no and lct_sts_cd='101' and mt_sts_cd in ('001','002') and lct_cd in (select lct_cd from lct_info where (wp_yn = 'Y' or mv_yn = 'Y') and substring(lct_cd, 1, 3)='002')) available_qty "
               + " ,(0) AS req_bundle_qty "
               + " ,(0) AS req_qty "
               + " ,b.unit_cd "
               + " ,CONCAT(FORMAT(b.spec, '#,#'), ' ', b.spec_unit) AS spec "
               + " ,CONCAT(FORMAT(b.width, '#,#'), ' ', b.width_unit) AS width "
               + " ,CONCAT(FORMAT(b.area, '#,#'), ' ', b.area_unit) AS area "
               + " ,CONCAT(FORMAT(SUM(b.gr_qty), '#,#'), ' ', b.unit_cd) AS group_unit "
               + " FROM w_material_info AS a JOIN d_material_info AS b ON a.mt_no=b.mt_no "
               + " WHERE a.mt_sts_cd in ('001','002') "
               + " AND a.lct_cd in (select lct_cd from lct_info where (wp_yn = 'Y' or mv_yn = 'Y') and substring(lct_cd, 1, 3)='002') "
               + " AND a.wmtid IN ( " + id + " ) "
               + " GROUP BY a.mt_no "
               ;
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
                List<ShipppingDirectionWIPModel> list = new List<ShipppingDirectionWIPModel>();
                list = (from DataRow dr in data.Rows
                        select new ShipppingDirectionWIPModel()
                        {
                            wmtid = dr["wmtid"].ToString(),
                            mt_no = dr["mt_no"].ToString(),
                            mt_nm = dr["mt_nm"].ToString(),
                            gr_qty = dr["gr_qty"].ToString(),
                            mt_bundle_qty = dr["mt_bundle_qty"].ToString(),
                            mt_qty = dr["mt_qty"].ToString(),
                            available_qty = dr["available_qty"].ToString(),
                            req_bundle_qty = dr["req_bundle_qty"].ToString(),
                            req_qty = dr["req_qty"].ToString(),
                            unit_cd = dr["unit_cd"].ToString(),
                            spec = dr["spec"].ToString(),
                            width = dr["width"].ToString(),
                            area = dr["area"].ToString(),
                            group_unit = dr["group_unit"].ToString()
                        }).ToList();

                for (int i = 0; i < list.Count(); i++)
                {
                    list[i].req_bundle_qty = c[i].ToString();
                    list[i].req_qty = b[i].ToString();
                }

                var result = Json(list, JsonRequestBehavior.AllowGet);
                return result;

                //var lstRows = new List<Dictionary<string, object>>();

                //var result = Json(lstRows, JsonRequestBehavior.AllowGet);

                //Dictionary<string, object> dictRow = null;
                //foreach (DataRow row in data.Rows)
                //{
                //    dictRow = new Dictionary<string, object>();
                //    foreach (DataColumn column in data.Columns)
                //    {
                //        dictRow.Add(column.ColumnName, row[column]);
                //    }
                //    lstRows.Add(dictRow);
                //}

                //for (int i = 0; i < a.Length; i++)
                //{
                //    for (int j = 0; j < b.Length; j++)
                //    {
                //        for (int x = 0; x < c.Length; x++)
                //        {
                //            if (i == j && i == x)
                //            {
                //                for (int k = 0; k < lstRows.Count; k++)
                //                {
                //                    //var h = list[k].Values;
                //                    var m = lstRows[k];
                //                    foreach (var item in m)
                //                    {
                //                        if ((item.Key == "req_qty") && (k == j) && (k == x))
                //                        {
                //                            lstRows[k].Remove(item.Key);
                //                            lstRows[k].Add("req_qty", b[j]);
                //                            lstRows[k].Add("req_bundle_qty", c[x]);
                //                            break;
                //                        }
                //                    }

                //                }
                //            }
                //        }
                //    }
                //}
                //return result;
            }
            return Json(false, JsonRequestBehavior.AllowGet);

        }
        public ActionResult sd_info_WIP()
        {
            var ds = (from a in db.w_sd_info
                    
                      orderby a.sd_no descending
                      select new
                      {
                          sid = a.sid,
                          sd_no = a.sd_no,
                          lct_cd = db.lct_info.Where(x => x.lct_cd == a.lct_cd).Select(x => x.lct_nm),
                        
                          remark = a.remark,
                          reg_id = a.reg_id,
                          reg_dt = a.reg_dt,
                          chg_id = a.chg_id,
                          chg_dt = a.chg_dt,
                      }).ToList();
            return Json(new { rows = ds }, JsonRequestBehavior.AllowGet);

        }
        public class w_count_affect
        {
            public int w_mt { get; set; }
            public int sdmt { get; set; }
        }
        #endregion

        #region w_rd_info
        public ActionResult Getw_rd_info()
        {
            var list = (from p in db.w_rd_info
                        orderby p.rd_no descending
                        select new
                        {
                            rid = p.rid,
                            rd_no = p.rd_no,
                            rd_sts_cd = db.comm_dt.Where(x => x.dt_cd == p.rd_sts_cd && x.mt_cd == "WHS004").Select(x => x.dt_nm),
                         
                            reg_id = p.reg_id,
                            reg_dt = p.reg_dt,
                        }).ToList();
            return Json(new { rows = list }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult getadmin(string id)
        {

            var admin = db.mb_info.Where(x => x.userid == id).ToList();
            return Json(admin, JsonRequestBehavior.AllowGet);
        }


        public ActionResult Savetablerd_info(w_rd_info wri, string whouse, string worker, string manager, string work_date, int mt_qty)
        {
            var lct = db.lct_info.Where(x => x.index_cd == whouse).FirstOrDefault();
           
            wri.rd_sts_cd = "001";
            var menuCd = string.Empty;
            var subMenuIdConvert = 0;
            var sublog1 = new w_rd_info();
            var list = db.w_rd_info.ToList().LastOrDefault();
            if (list == null)
            {
                wri.rd_no = "RD000000001";
            }
            else
            {
                var bien = list.rd_no;
                var subMenuId1 = bien.Substring(bien.Length - 9, 9);
                int.TryParse(subMenuId1, out subMenuIdConvert);
                menuCd = "RD" + string.Format("{0}{1}", menuCd, Creatrd((subMenuIdConvert + 1)));
                wri.rd_no = menuCd;

            }

            DateTime reg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
            String dateString = reg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");

            DateTime chg_dt = DateTime.Now; // Or your date, as long as it is in DateTime format
            String dateChString = chg_dt.ToString("yyyy-MM-dd HH:mm:ss.FFF");
            wri.reg_dt = reg_dt;
            wri.chg_dt = chg_dt;

            if (ModelState.IsValid)
            {
                db.Entry(wri).State = EntityState.Added;
                db.SaveChanges();
                var result = new { result = true };
                return Json(result, JsonRequestBehavior.AllowGet);
                ;
            }
            return View(wri);
        }
        private string Creatrd(int id)
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

        #endregion

        #region Receving_manual_api
        public ActionResult getLct(lct_info lct_info)
        {
            //var lists = db.lct_info.Where(item => item.wp_yn=="Y")
            //    .Select(item => item.lct_cd).
            // OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();

            var list = (from p in db.lct_info
                        where (p.wp_yn == "Y")
                        select new
                        {
                            lct_cd = p.lct_cd,
                            lct_nm = p.lct_nm,
                            level_cd = p.level_cd
                        }).
                         OrderBy(item => item.lct_cd)
                        .ThenBy(item => item.level_cd)
                        .ToList();
            if (list != null)
            {
                return Json(new { result = list }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }

        }
        public ActionResult Get_Status(comm_dt comm_dt)
        {
            var list = (from p in db.comm_dt
                        where (p.mt_cd == "WHS005")
                        select new
                        {
                            dt_cd = p.dt_cd,
                            dt_nm = p.dt_nm,

                        })
                       .ToList();
            if (list != null)
            {
                return Json(new { result = list }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult get_Destination(lct_info lct_info)
        {
            var list = (from item in db.lct_info
                        where (item.mv_yn == "Y" && item.index_cd != null && item.index_cd != "" && item.index_cd != "0")
                        select new
                        {
                            lct_cd = item.lct_cd,
                            lct_nm = item.lct_nm,
                            level_cd = item.level_cd
                        }).
                        OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();
            if (list != null)
            {
                return Json(new { result = list }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }

            //var lists = db.lct_info.Where(item => item.mv_yn == "Y" && item.index_cd != null && item.index_cd != "" && item.index_cd != "0").
            //  OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();

            //return Json(lists, JsonRequestBehavior.AllowGet);
        }
        public ActionResult getRecManual_wip(string shelf_cd)
        {
            if (shelf_cd == null) { shelf_cd = ""; }
            var list = (from a in db.lct_info
                        join b in db.w_material_info
                       on a.lct_cd equals b.lct_cd
                        join m in db.d_material_info
                        on b.mt_no equals m.mt_no
                        into c
                        from d in c.DefaultIfEmpty()
                        where b.to_lct_cd.StartsWith("002") && b.lct_sts_cd == "201"
                        select new
                        {
                            wmtid = b.wmtid,
                            mt_cd = b.mt_cd,
                            mt_no = b.mt_no,
                            mt_nm = d.mt_nm,
                            mt_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == b.mt_sts_cd).Select(x => x.dt_nm),
                            lct_nm = db.lct_info.Where(x => x.lct_cd == b.lct_cd).Select(x => x.lct_nm),
                            lct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002" && x.dt_cd == b.lct_sts_cd).Select(x => x.dt_nm),
                            Departure = db.lct_info.Where(x => x.lct_cd == b.from_lct_cd).Select(x => x.lct_nm),
                            output_dt = b.output_dt.Substring(0, 4) + "-" + b.output_dt.Substring(4, 2) + "-" + b.output_dt.Substring(6, 2),
                            Destination = db.lct_info.Where(x => x.lct_cd == b.to_lct_cd).Select(x => x.lct_nm),
                            input_dt = b.input_dt.Substring(0, 4) + "-" + b.input_dt.Substring(4, 2) + "-" + b.input_dt.Substring(6, 2),
                            bb_no = b.bb_no,
                            Description = b.remark,
                            reg_id = b.reg_id,
                            chg_id = b.chg_id,
                            reg_dt = b.reg_dt,
                            chg_dt = b.chg_dt

                        }).ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        public JsonResult searchReceiving_Manual(string mt_no = "", string mt_barcode = "", string mt_sts_cd = "", string lct_cd = "")
        {


            var list = (from a in db.lct_info
                        join b in db.w_material_info
                       on a.lct_cd equals b.lct_cd
                        join m in db.d_material_info
                        on b.mt_no equals m.mt_no
                        into c
                        from d in c.DefaultIfEmpty()
                        where b.to_lct_cd.StartsWith("002") && b.lct_sts_cd == "201"
                        && (b.mt_no.Contains(mt_no))
                        && (b.mt_qrcode.Contains(mt_barcode))
                        && (b.mt_sts_cd.Contains(mt_sts_cd))
                        && (b.to_lct_cd.Contains(lct_cd))
                        select new
                        {
                            wmtid = b.wmtid,
                            mt_cd = b.mt_cd,
                            mt_no = b.mt_no,
                            mt_nm = d.mt_nm,
                            mt_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == b.mt_sts_cd).Select(x => x.dt_nm),
                            lct_nm = db.lct_info.Where(x => x.lct_cd == b.lct_cd).Select(x => x.lct_nm),
                            lct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002" && x.dt_cd == b.lct_sts_cd).Select(x => x.dt_nm),
                            Departure = db.lct_info.Where(x => x.lct_cd == b.from_lct_cd).Select(x => x.lct_nm),
                            output_dt = b.output_dt.Substring(0, 4) + "-" + b.output_dt.Substring(4, 2) + "-" + b.output_dt.Substring(6, 2),
                            Destination = db.lct_info.Where(x => x.lct_cd == b.to_lct_cd).Select(x => x.lct_nm),
                            input_dt = b.input_dt.Substring(0, 4) + "-" + b.input_dt.Substring(4, 2) + "-" + b.input_dt.Substring(6, 2),
                            bb_no = b.bb_no,
                            Description = b.remark,
                            reg_id = b.reg_id,
                            chg_id = b.chg_id,
                            reg_dt = b.reg_dt,
                            chg_dt = b.chg_dt

                        }).ToList();


            return Json(list, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region open_Btn_lot_info
        public ActionResult open_Btn_lot_info(int wmtid)
        {
            var data = db.w_material_info.Find(wmtid);
            if (data != null)
            {
                var md_cd = data.mt_cd;
                ViewBag.open_md_cd_popup_window__md_cd = md_cd;
                ViewBag.open_wmtid_popup_window__wmtid = wmtid;
            }

            return View();
        }

        public ActionResult Get_data_use_material_lot(int id, string mt_cd_1 = "")
        {
            var list = db.w_material_info.Find(id);

            var mt_lot = list.mt_cd;
            //int i;
            if (list != null)
            {
                var data1 = (from mp in db.w_material_mapping
                             join a in db.w_material_info
                             on mp.mt_lot equals a.mt_cd into h
                             from k in h.DefaultIfEmpty()
                             join b in db.d_material_info
                             on mp.mt_no equals b.mt_no into d
                             from e in d.DefaultIfEmpty()
                             where mp.mt_lot == mt_lot
                             orderby mp.mt_no
                             select new
                             {
                                 //record = i++,
                                 ml_no = mp.mt_cd,
                                 mt_no = mp.mt_no,
                                 mt_nm = e.mt_nm,
                                 mapping_dt = mp.mapping_dt.Substring(0, 4) + "-" + mp.mapping_dt.Substring(4, 2) + "-" + mp.mapping_dt.Substring(6, 2),
                                 bb_no = mp.bb_no,

                                 mt_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == k.mt_sts_cd).Select(x => x.dt_nm),
                                 lct_nm = db.lct_info.Where(x => x.lct_cd == k.lct_cd).Select(x => x.lct_nm),
                                 lct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002" && x.dt_cd == k.lct_sts_cd).Select(x => x.dt_nm),

                             })//.ToList()
                             .AsEnumerable()
                             .Select((r, i) => new
                             {
                                 recordIndex = ++i,
                                 ml_no = r.ml_no,
                                 mt_no = r.mt_no,
                                 mt_nm = r.mt_nm,
                                 mapping_dt = r.mapping_dt,
                                 bb_no = r.bb_no,
                                 mt_sts_cd = r.mt_sts_cd,
                                 lct_nm = r.lct_nm,
                                 lct_sts_cd = r.lct_sts_cd
                             }).ToList()
                             ;
                return Json(new { data = data1 }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { result = false }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region open_Mt_Cd_popup_window
        public ActionResult open_Mt_Cd_popup_window(int wmtid)
        {
            var data = db.w_material_info.Find(wmtid);
            if (data != null)
            {
                var md_cd = data.mt_cd;
                ViewBag.open_md_cd_popup_window__md_cd = md_cd;
                ViewBag.open_wmtid_popup_window__wmtid = wmtid;
            }

            return View();
        }
        public ActionResult Get_data_Standard(int id)
        {
            var value = (from a in db.w_material_info
                         join b in db.d_material_info
                         on a.mt_no equals b.mt_no into d
                         from e in d.DefaultIfEmpty()
                         where a.wmtid == id

                         select new
                         {
                             ml_no = a.mt_cd,
                             mt_no = a.mt_no,
                             mt_nm = e.mt_nm,
                             gr_qty = a.gr_qty,
                             mt_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == a.mt_sts_cd).Select(x => x.dt_nm),
                             lct_nm = db.lct_info.Where(x => x.lct_cd == a.lct_cd).Select(x => x.lct_nm),
                             lct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002" && x.dt_cd == a.lct_sts_cd).Select(x => x.dt_nm),

                         })
                         .AsEnumerable()
                         .Select((r, i) => new
                         {
                             recordIndex = ++i,
                             ml_no = r.ml_no,
                             mt_no = r.mt_no,
                             mt_nm = r.mt_nm,
                             gr_qty = r.gr_qty,
                             mt_sts_cd = r.mt_sts_cd,
                             lct_nm = r.lct_nm,
                             lct_sts_cd = r.lct_sts_cd
                         })
                         .ToList();
            return Json(new { data = value }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Get_data_use_material(int id, string mt_cd_1 = "")
        {
            var list = db.w_material_info.Find(id);
            var bien = new ArrayList();
            var data = new List<w_material_mapping>();

            var tempData = new List<w_material_mapping>();
            var newData = new List<w_material_mapping>();
            var mt_lot = list.mt_cd;
            data = db.w_material_mapping.Where(x => x.mt_lot == mt_lot).ToList();
            tempData = data;//tempData : 2
            var list1 = tempData.ToList();
            if (list != null)
            {
                do
                {
                    newData = new List<w_material_mapping>();
                    foreach (var item in list1)
                    {
                        newData = db.w_material_mapping.Where(x => x.mt_lot == item.mt_cd).ToList();//  = 1
                        data.AddRange(newData);
                    }
                    list1.Clear();
                    list1.AddRange(newData.ToList());

                } while (list1.Count > 0);

                foreach (var item in data)
                {
                    var data1 = (from mp in db.w_material_mapping
                                 join a in db.w_material_info
                                 on mp.mt_lot equals a.mt_cd into h
                                 from k in h.DefaultIfEmpty()
                                 join b in db.d_material_info
                                 on k.mt_no equals b.mt_no into d
                                 from e in d.DefaultIfEmpty()
                                 where mp.wmmid == item.wmmid && k.mt_type == "CMT"

                                 select new
                                 {
                                     ml_no = mp.mt_cd,
                                     mt_no = mp.mt_no,
                                     mt_nm = e.mt_nm,
                                     mapping_dt = mp.mapping_dt.Substring(0, 4) + "-" + mp.mapping_dt.Substring(4, 2) + "-" + mp.mapping_dt.Substring(6, 2),
                                     bb_no = mp.bb_no,
                                     mt_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == k.mt_sts_cd).Select(x => x.dt_nm),
                                     lct_nm = db.lct_info.Where(x => x.lct_cd == k.lct_cd).Select(x => x.lct_nm),
                                     lct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002" && x.dt_cd == k.lct_sts_cd).Select(x => x.dt_nm),

                                 }).ToList();
                    bien.AddRange(data1);
                }
                return Json(new { data = bien }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { result = false }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region open_Btn_use_info
        public ActionResult open_Btn_use_info(int wmtid)
        {
            var data = db.w_material_info.Find(wmtid);
            if (data != null)
            {
                var md_cd = data.mt_cd;
                ViewBag.open_md_cd_popup_window__md_cd = md_cd;
                ViewBag.open_wmtid_popup_window__wmtid = wmtid;
            }

            return View();
        }
        public ActionResult Get_data_composite_material(int id)
        {
            var list = db.w_material_info.Find(id);
            var mt_lot = list.mt_cd;
            if (list != null)
            {
                var value = (from mp in db.w_material_mapping
                             join a in db.w_material_info
                             on mp.mt_lot equals a.mt_cd into h
                             from k in h.DefaultIfEmpty()
                             join b in db.d_material_info
                             on k.mt_no equals b.mt_no into d
                             from e in d.DefaultIfEmpty()
                             where mp.mt_cd == mt_lot

                             select new
                             {
                                 ml_no = mp.mt_lot,
                                 mt_no = mp.mt_no,
                                 mt_nm = e.mt_nm,
                                 mapping_dt = mp.mapping_dt.Substring(0, 4) + "-" + mp.mapping_dt.Substring(4, 2) + "-" + mp.mapping_dt.Substring(6, 2),
                                 bb_no = mp.bb_no,

                                 mt_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == k.mt_sts_cd).Select(x => x.dt_nm),
                                 lct_nm = db.lct_info.Where(x => x.lct_cd == k.lct_cd).Select(x => x.lct_nm),
                                 lct_sts_cd = db.comm_dt.Where(x => x.mt_cd == "WHS002" && x.dt_cd == k.lct_sts_cd).Select(x => x.dt_nm),

                             })
                             .AsEnumerable()
                             .Select((r, i) => new
                             {
                                 recordIndex = ++i,
                                 ml_no = r.ml_no,
                                 mt_no = r.mt_no,
                                 mt_nm = r.mt_nm,
                                 mapping_dt = r.mapping_dt,
                                 bb_no = r.bb_no,

                                 mt_sts_cd = r.mt_sts_cd,
                                 lct_nm = r.lct_nm,
                                 lct_sts_cd = r.lct_sts_cd,
                             })
                             .ToList();

                return Json(new { data = value }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { result = false }, JsonRequestBehavior.AllowGet);
        }
        #endregion



        #region   Material_Return_WIP
        public ActionResult Material_Return_WIP()
        {
            return View();
        }
        public ActionResult Warehouse_Return_Wip(lct_info lct_info)
        {
            var lists = db.lct_info.Where(item => item.mv_yn == "Y" && (item.lct_cd.StartsWith("002"))).
              OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();

            return Json(lists, JsonRequestBehavior.AllowGet);
        }
        //public ActionResult Getmt_list_WIP_machine()
        //{

        //    StringBuilder varname1 = new StringBuilder();
        //    varname1.Append("SELECT a.wmtid,  a.rel_bom, a.mt_no,a.mt_cd, b.mt_nm, a.mt_cd, c.lct_nm AS lct_nm, a.gr_qty, a.remark, \n");
        //    varname1.Append("		(select lct_nm from lct_info AS i WHERE  i.lct_cd = a.to_lct_cd) AS to_lct_cd, \n");
        //    varname1.Append("		(select dt_nm from comm_dt AS i WHERE i.mt_cd ='WHS005' AND i.dt_cd = a.mt_sts_cd) AS mt_sts_cd \n");
        //    varname1.Append("	FROM   w_material_info AS a \n");
        //    varname1.Append("	LEFT JOIN d_material_info AS b ON a.mt_no = b.mt_no \n");
        //    varname1.Append("	LEFT JOIN lct_info AS c ON c.lct_cd = a.lct_cd \n");
        //    varname1.Append("	WHERE  a.lct_sts_cd = '201' \n");
        //    varname1.Append("    		  AND Substring(a.lct_cd, 1, 3) = '002' \n");
        //    varname1.Append("				 AND a.use_yn = 'Y' AND a.del_yn = 'N' \n");
        //    varname1.Append("				 AND a.mt_sts_cd = '002' \n");
        //    varname1.Append("				 AND a.rel_bom IS NOT NULL  order by a.rel_bom desc, a.mt_no desc, a.mt_cd desc");
        //    var data = new Excute_query().get_data_from_data_base(varname1);
        //    var result = GetJsonPersons1(data);     
        //    return result;

        //}
        public ActionResult search_mt_list_WIP_machine()
        {
            var mt_no = Request["mt_no"];
            var bom_no = Request["bom_no"];

            if (bom_no != null && bom_no != "")
                if (bom_no.Substring(0, 1) == "B") bom_no = bom_no.Substring(1, bom_no.Length - 1);
            var mt_cd = Request["mt_cd"];
            var lct_cd = Request["lct_cd"];

            StringBuilder varname1 = new StringBuilder();
            varname1.Append("SELECT a.wmtid,  a.rel_bom, a.rel_bom as rel_bom1, a.mt_no,a.mt_cd, b.mt_nm, a.mt_cd, c.lct_nm AS lct_nm, a.gr_qty, a.remark, \n");
            varname1.Append("		(select lct_nm from lct_info AS i WHERE  i.lct_cd = a.to_lct_cd) AS to_lct_cd, \n");
            varname1.Append("		(select dt_nm from comm_dt AS i WHERE i.mt_cd ='WHS005' AND i.dt_cd = a.mt_sts_cd) AS mt_sts_cd \n");
            varname1.Append("	FROM   w_material_info AS a \n");
            varname1.Append("	LEFT JOIN d_material_info AS b ON a.mt_no = b.mt_no \n");
            varname1.Append("	LEFT JOIN lct_info AS c ON c.lct_cd = a.lct_cd \n");
            varname1.Append("	WHERE  a.lct_sts_cd = '220' \n");
            varname1.Append("    		  AND Substring(a.lct_cd, 1, 3) = '002' AND Substring(a.lct_cd, 1, 6) != '002007' \n");
            varname1.Append("				 AND a.use_yn = 'Y' AND a.del_yn = 'N' \n");
            varname1.Append("				 AND a.mt_sts_cd = '002' \n");
            varname1.Append("				 AND a.rel_bom IS NOT NULL ");
            varname1.Append("   AND ('' = '" + mt_no + "' OR a.mt_no like '%" + mt_no + "%') ");
            varname1.Append("   AND ('' = '" + bom_no + "' OR a.rel_bom like '%" + bom_no + "%') ");
            varname1.Append("   AND ('' = '" + mt_cd + "' OR a.mt_cd like '%" + mt_cd + "%') ");
            varname1.Append("   AND ('' = '" + lct_cd + "' OR a.lct_cd like '%" + lct_cd + "%') ");
            varname1.Append(" order by a.rel_bom desc, a.mt_no desc, a.mt_cd desc");

            var data = new Excute_query().get_data_from_data_base(varname1);
            var result = GetJsonPersons1(data);
            return result;

        }

        public ActionResult insert_update_split(int wmtid, int gr_qty, string remark /*, string lct_cd*/)
        {
            try
            {
                if (db.w_material_info.Find(wmtid).mt_sts_cd != "002")
                    return Json(new { result = false, message = vlnu }, JsonRequestBehavior.AllowGet);

                w_material_info a = db.w_material_info.Find(wmtid);
                w_material_info x = db.w_material_info.Find(wmtid);
                if (gr_qty > gr_qty)
                { return Json(new { result = false, message = sosanh }, JsonRequestBehavior.AllowGet); }
                 else
                {
                    int tinh = gr_qty - gr_qty;
                    x.gr_qty = tinh;
                  
                    // x.lct_sts_cd = "220";
                    x.mt_sts_cd = "005";
                    //x.chg_dt = DateTime.Now;
                    x.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    db.Entry(x).State = EntityState.Modified;
                    db.SaveChanges();
                    // add mt_no if it does not exists  in  d_material_info
                    var d_material_info = db.d_material_info.Where(y => y.mt_no == x.mt_no).FirstOrDefault();
                    if (d_material_info == null)
                        return Json(new { result = false, message = "MT NO "+exits }, JsonRequestBehavior.AllowGet);
                    var mt_no_new = d_material_info.mt_cd + "-" + gr_qty.ToString();
                    var count_mt_no = db.d_material_info.Where(y => y.mt_no == mt_no_new).Count();
                    if (count_mt_no == 0)
                    {

                        d_material_info.mt_no_origin = d_material_info.mt_no;
                        d_material_info.mt_no = mt_no_new;
                        db.Entry(d_material_info).State = EntityState.Added;
                    }
                    var b = new w_material_info();
                    b.mt_type = x.mt_type;
                    // b.mt_no = x.mt_no;                  
                    b.date = x.date;
                    b.expiry_dt = x.expiry_dt;
                    b.mt_barcode = x.mt_barcode;
                    b.mt_qrcode = x.mt_qrcode;
                    b.lct_cd = x.lct_cd;
                    b.lct_sts_cd = "201";
                    b.bbmp_sts_cd = x.bbmp_sts_cd;
                    b.mt_sts_cd = "004";
                    b.sd_no = x.sd_no;
                    b.from_lct_cd = x.lct_cd;
                    b.to_lct_cd = db.lct_info.FirstOrDefault(y => y.lct_cd.Substring(0, 3) == "002" && y.level_cd == "000").lct_cd;
                    b.use_yn = x.use_yn;
                    b.mt_no = mt_no_new;
                    b.chg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    b.reg_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    //   b.mr_no = mr_no;
                    // b.rel_bom = rel_bom;


                    if (a.orgin_mt_cd != null && a.orgin_mt_cd != "" && a.orgin_mt_cd != "underfined")
                    {
                        b.orgin_mt_cd = a.orgin_mt_cd;
                    }
                    else
                    {
                        b.orgin_mt_cd = a.mt_cd;
                    }
                    b.chg_dt = DateTime.Now;
                    b.reg_dt = DateTime.Now;
                    b.gr_qty = gr_qty;
                    b.remark = remark;
                    var code = x.mt_cd;
                    var menuCd = string.Empty;


                    var subMenuIdConvert = 0;
                    var subMenuId = "";

                    var menuCd2 = string.Empty;
                    var subMenuIdConvert2 = 0;
                    var subMenuId2 = "";


                    var ds = db.w_material_info.Where(u => u.mt_cd.StartsWith(x.mt_cd)).OrderBy(u => u.reg_dt).ToList();
                    var kiemtratrung = db.w_material_info.Where(u => u.mt_cd.StartsWith(x.mt_cd)).OrderBy(u => u.reg_dt).GroupBy(u => u.mt_cd).ToList();

                    var te = ds.LastOrDefault().mt_cd;
                    var mt = te.Substring(te.Length - 4, 4);

                    var mt_cdtam = te.Substring(te.Length - 5, 5);

                    if (mt_cdtam.Contains("-S"))
                    {

                        var bien = ds.LastOrDefault().mt_cd;
                        subMenuId = bien.Substring(bien.Length - 3, 3);
                        var bien2 = bien.Remove(bien.Length - 3, 3);

                        var ds2 = db.w_material_info.Where(z => z.mt_cd.StartsWith(bien2)).OrderBy(u => u.reg_dt).ToList();


                        var te2 = ds2.LastOrDefault().mt_cd;
                        subMenuId2 = te2.Substring(te2.Length - 3, 3);

                        var bien3 = te2.Remove(te2.Length - 3, 3);
                        int.TryParse(subMenuId2, out subMenuIdConvert2);


                        menuCd2 = string.Format("{0}{1}", menuCd2, CreateId_slip((subMenuIdConvert2 + 1)));


                        int.TryParse(subMenuId, out subMenuIdConvert);
                        menuCd = string.Format("{0}{1}", menuCd, CreateId_slip((subMenuIdConvert + 1)));
                        b.mt_cd = bien3 + menuCd2;
                        b.mt_barcode = bien3 + menuCd2;
                        b.mt_qrcode = bien3 + menuCd2;
                    }
                    else
                    {
                        if (kiemtratrung.Count == 1)
                        {
                            b.mt_cd = code + "-S001";
                            b.mt_barcode = code + "-S001";
                            b.mt_qrcode = code + "-S001";
                        }
                        else
                        {
                            var bien = ds.LastOrDefault().mt_cd;
                            subMenuId = bien.Substring(bien.Length - 3, 3);
                            var bien2 = bien.Remove(bien.Length - 3, 3);

                            var ds2 = db.w_material_info.Where(z => z.mt_cd.StartsWith(bien2)).OrderBy(u => u.reg_dt).ToList();


                            var te2 = ds2.LastOrDefault().mt_cd;
                            subMenuId2 = te2.Substring(te2.Length - 3, 3);

                            var bien3 = te2.Remove(te2.Length - 3, 3);
                            int.TryParse(subMenuId2, out subMenuIdConvert2);


                            menuCd2 = string.Format("{0}{1}", menuCd2, CreateId_slip((subMenuIdConvert2 + 1)));


                            int.TryParse(subMenuId, out subMenuIdConvert);
                            menuCd = string.Format("{0}{1}", menuCd, CreateId_slip((subMenuIdConvert + 1)));
                            b.mt_cd = bien3 + menuCd2;
                            b.mt_barcode = bien3 + menuCd2;
                            b.mt_qrcode = bien3 + menuCd2;

                        }

                    }
                    //   d_bom_detail.mt_no =b.mt_no;
                    //   db.Entry(d_bom_detail).State = EntityState.Added;                                        
                    db.Entry(b).State = EntityState.Added;
                    db.SaveChanges();

                    var user_id = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                    //add history
                    StringBuilder varname1 = new StringBuilder();
                    varname1.Append("INSERT INTO wip_material_hist (wmtid,remark,  rel_bom,mt_cd, mt_no, mpo_no,prounit_cd,gr_qty, mdpo_no,mt_barcode, mt_sts_cd,bb_no, lct_cd, lct_sts_cd,from_lct_cd, \n");
                    varname1.Append("										 to_lct_cd,output_dt, input_dt, hist_sts_cd, worker_id, sd_no, reg_id, reg_dt, chg_id, chg_dt ) \n");
                    varname1.Append("  \n");
                    varname1.Append("                        SELECT wmtid, remark, rel_bom,mt_cd, mt_no, mpo_no,prounit_cd,gr_qty, mdpo_no,mt_barcode, mt_sts_cd,bb_no, lct_cd, lct_sts_cd,from_lct_cd, \n");
                    varname1.Append("										to_lct_cd,output_dt, input_dt, '001', worker_id, sd_no, '" + user_id + "', current_timestamp(), '" + user_id + "', current_timestamp()   \n");
                    varname1.Append("									FROM w_material_info WHERE w_material_info.wmtid ='" + b.wmtid + "';");

                    var effect_row = new Excute_query().Execute_NonQuery(varname1);
                    // sent data to client

                    x.from_lct_cd = db.lct_info.Where(y => y.lct_cd == x.from_lct_cd).FirstOrDefault().lct_nm;
                    x.to_lct_cd = db.lct_info.Where(y => y.lct_cd == x.to_lct_cd).FirstOrDefault().lct_nm;
                    x.mt_sts_cd = db.comm_dt.Where(y => y.dt_cd == x.mt_sts_cd && y.mt_cd == "WHS005").FirstOrDefault().dt_nm;
                    x.lct_sts_cd = db.comm_dt.Where(y => y.dt_cd == x.lct_sts_cd && y.mt_cd == "WHS002").FirstOrDefault().dt_nm;
                    b.from_lct_cd = db.lct_info.Where(y => y.lct_cd == b.from_lct_cd).FirstOrDefault().lct_nm;
                    b.to_lct_cd = db.lct_info.Where(y => y.lct_cd == b.to_lct_cd).FirstOrDefault().lct_nm;
                    b.mt_sts_cd = db.comm_dt.Where(y => y.dt_cd == b.mt_sts_cd && y.mt_cd == "WHS005").FirstOrDefault().dt_nm;
                    b.lct_sts_cd = db.comm_dt.Where(y => y.dt_cd == b.lct_sts_cd && y.mt_cd == "WHS002").FirstOrDefault().dt_nm;
                    return Json(new { result = true, result1 = x, result2 = b }, JsonRequestBehavior.AllowGet);
                }


            }
            catch (Exception e)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }


        }
        private string CreateId(int id)
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
        private string CreateId_slip(int id)
        {
            if (id.ToString().Length < 2)
            {
                return string.Format("00{0}", id);
            }

            if (id.ToString().Length < 3 || (id.ToString().Length == 2))
            {
                return string.Format("0{0}", id);
            }

            if (id.ToString().Length < 4 || (id.ToString().Length == 3))
            {
                return string.Format("{0}", id);
            }
            return string.Empty;
        }

        #endregion
        #region   Material_Return_M_WMS
        public ActionResult Material_Return_M_WMS()
        {
            return View();
        }
        //public ActionResult Getmt_list_WIP_stock()
        //{

        //    StringBuilder varname1 = new StringBuilder();
        //    varname1.Append("SELECT a.wmtid,  a.rel_bom, a.mt_no,a.mt_cd, b.mt_nm, a.mt_cd, c.lct_nm AS lct_nm , a.gr_qty, a.remark,  \n");               
        //    varname1.Append(" (select lct_nm from lct_info AS i WHERE  i.lct_cd = a.to_lct_cd) AS to_lct_cd, \n");   
        //    varname1.Append(" (select dt_nm from comm_dt AS i WHERE i.mt_cd ='WHS005' AND i.dt_cd = a.mt_sts_cd) AS mt_sts_cd	FROM   w_material_info AS a \n");
        //    varname1.Append("	LEFT JOIN d_material_info AS b ON a.mt_no = b.mt_no \n");
        //    varname1.Append("	LEFT JOIN lct_info AS c ON c.lct_cd = a.lct_cd \n");
        //    varname1.Append("	WHERE  a.lct_sts_cd = '101' \n");
        //    varname1.Append("		 AND a.mt_sts_cd = '001' \n");
        //    varname1.Append("    		  AND Substring(a.lct_cd, 1, 3) = '002' AND a.rel_bom IS NOT NULL order by a.rel_bom desc, a.mt_no desc, a.mt_cd desc"); 
        //    var data = new Excute_query().get_data_from_data_base(varname1);                 
        //    var result = GetJsonPersons1(data);

        //    return result;

        //}
        public ActionResult search_list_WIP_Return_W_WMS()
        {
            var mt_no = Request["mt_no"];
            var bom_no = Request["bom_no"];
            if (bom_no != null && bom_no != "") bom_no = bom_no.Substring(1, bom_no.Length - 1);
            var mt_cd = Request["mt_cd"];
            var lct_cd = Request["lct_cd"];
            //int pageIndex = pageing.page;
            //int pageSize = pageing.rows;
            //int start_r = pageing.page;
            //if (start_r > 1)
            //{
            //    start_r = ((pageIndex - 1) * pageSize);
            //}
            //int end_r = (pageIndex * pageSize);


            //string order_by = "";
            //if (pageing.sidx != null)
            //{
            //    order_by = " ORDER BY " + pageing.sidx + " " + pageing.sord;
            //}

            //if (pageing.sidx == null)
            //{
            //    order_by = " ORDER BY a.rel_bom DESC , a.mt_no DESC , a.mt_cd desc ";
            //}



            StringBuilder varname1 = new StringBuilder();
            //StringBuilder varname2 = new StringBuilder();
            //StringBuilder varname3 = new StringBuilder();
            varname1.Append("SELECT a.wmtid, a.rel_bom,a.rel_bom as rel_bom1, a.mt_no, b.mt_nm, a.mt_cd, c.lct_nm AS lct_nm , a.gr_qty, a.remark,  \n");
            varname1.Append(" (select lct_nm from lct_info AS i WHERE  i.lct_cd = a.to_lct_cd) AS to_lct_cd, \n");
            varname1.Append(" (select dt_nm from comm_dt AS i WHERE i.mt_cd ='WHS005' AND i.dt_cd = a.mt_sts_cd) AS mt_sts_cd	FROM   w_material_info AS a \n");
            varname1.Append("	LEFT JOIN d_material_info AS b ON a.mt_no = b.mt_no \n");
            varname1.Append("	LEFT JOIN lct_info AS c ON c.lct_cd = a.lct_cd \n");
            varname1.Append("	WHERE  a.lct_sts_cd = '101' \n");
            varname1.Append("		 AND a.mt_sts_cd = '001' \n");
            varname1.Append("    		  AND Substring(a.lct_cd, 1, 3) = '002' AND Substring(a.lct_cd, 1, 6) != '002007' AND a.rel_bom IS NOT NULL ");
            varname1.Append("   AND ('' = '" + mt_no + "' OR a.mt_no like '%" + mt_no + "%') ");
            varname1.Append("   AND ('' = '" + bom_no + "' OR a.rel_bom like '%" + bom_no + "%') ");
            varname1.Append("   AND ('' = '" + mt_cd + "' OR a.mt_cd like '%" + mt_cd + "%') ");
            varname1.Append("   AND ('' = '" + lct_cd + "' OR a.lct_cd like '%" + lct_cd + "%') ");
            varname1.Append(" order by a.rel_bom desc, a.mt_no desc, a.mt_cd desc");

            //varname2.Append(" SELECT * FROM ( " + varname1 + " ) AS result WHERE  result.RN BETWEEN " + start_r + " AND " + end_r);
            //varname3.Append(" SELECT count(*) FROM ( " + varname1 + "  ) AS result  ");

            var data = new Excute_query().get_data_from_data_base(varname1);

            var result = GetJsonPersons1(data);
            return result;
            //var totalRecords = new Excute_query().Execute_Scalar(varname3);
            ////int totalRecords = db.Database.SqlQuery<int>(countSql).FirstOrDefault();

            ////var values = ConvertDataTableToJson(data);

            ////int totalPages = (int)Math.Ceiling((float)totalRecords / (float)pageSize);

            //int totalPages = (int)Math.Ceiling((float)totalRecords / ((pageSize!=0)?(float)pageSize:1));               

            //var result = new
            //{
            //    total = totalPages,
            //    page = pageIndex,
            //    records = totalRecords,
            //    rows = GetJsonPersons1(data).Data,
            //};

            //return Json(result, JsonRequestBehavior.AllowGet);





        }

        public ActionResult return_material_m_wms(string id, string to_lct_cd)
        {
            try
            {
                if (id == null || id == "") return Json(false, JsonRequestBehavior.AllowGet);
                StringBuilder varname1 = new StringBuilder();
                varname1.Append(" UPDATE w_material_info  \n");
                varname1.Append(" SET  w_material_info.from_lct_cd = w_material_info.lct_cd, w_material_info.lct_cd= '" + to_lct_cd + "',     \n");
                varname1.Append("  w_material_info.to_lct_cd='" + to_lct_cd + "',w_material_info.lct_sts_cd='201',w_material_info.mt_sts_cd='004' WHERE w_material_info.wmtid IN (" + id + ") \n");
                var data = new Excute_query().Execute_NonQuery(varname1);

                varname1.Clear();
                varname1.Append("INSERT INTO wip_material_hist (wmtid,remark, rel_bom, mt_cd, mt_no, mpo_no,prounit_cd,gr_qty, mdpo_no,mt_barcode, mt_sts_cd,bb_no, lct_cd, lct_sts_cd,from_lct_cd, \n");
                varname1.Append("										 to_lct_cd,output_dt, input_dt, hist_sts_cd, worker_id, sd_no, reg_id, reg_dt, chg_id, chg_dt ) \n");
                varname1.Append("  \n");
                varname1.Append("                        SELECT wmtid,remark, rel_bom, mt_cd, mt_no, mpo_no,prounit_cd,gr_qty, mdpo_no,mt_barcode, mt_sts_cd,bb_no, lct_cd, lct_sts_cd,from_lct_cd, \n");
                varname1.Append("										to_lct_cd,output_dt, input_dt, '002', worker_id, sd_no, reg_id, current_timestamp(), chg_id, current_timestamp() \n");
                varname1.Append("									FROM w_material_info WHERE w_material_info.wmtid IN (" + id + ")");
                //  var result = GetJsonPersons1(data);
                var effect_row_add = new Excute_query().Execute_NonQuery(varname1);
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }

        }



        #endregion

        #region Material_Return_History
        public ActionResult Material_Return_History()
        {
            return View();
        }


        //public ActionResult Getmt_list_WIP_History()
        //{

        //    StringBuilder varname1 = new StringBuilder();
        //    varname1.Append("SELECT a.wmtid,  a.rel_bom, a.mt_no,a.mt_cd, b.mt_nm, a.mt_cd, c.lct_nm AS lct_nm, a.gr_qty, a.remark, \n");
        //    varname1.Append("	(select lct_nm from lct_info AS i WHERE  i.lct_cd = a.to_lct_cd) AS to_lct_cd,\n");
        //    varname1.Append("  (select lct_nm from lct_info AS i WHERE  i.lct_cd = a.from_lct_cd) AS from_lct_cd, \n");
        //    varname1.Append("	(select dt_nm from comm_dt AS i WHERE i.mt_cd ='WHS005' AND i.dt_cd = a.mt_sts_cd) AS mt_sts_cd, \n");
        //    varname1.Append("	(select dt_nm from comm_dt AS i WHERE i.mt_cd ='WHS002' AND i.dt_cd = a.mt_sts_cd) AS lct_sts_cd \n");
        //    varname1.Append("		FROM w_material_info AS a \n");
        //    varname1.Append("		LEFT JOIN d_material_info AS b ON a.mt_no = b.mt_no \n");
        //    varname1.Append("		LEFT JOIN lct_info AS c ON c.lct_cd = a.lct_cd \n");
        //    varname1.Append("		WHERE a.mt_sts_cd = '004' AND (SUBSTRING(a.from_lct_cd, 1,3) ='002' OR SUBSTRING(a.from_lct_cd, 1,3) ='020') order by a.rel_bom desc, a.mt_no desc, a.mt_cd desc");
        //    var data = new Excute_query().get_data_from_data_base(varname1);
        //    var result = GetJsonPersons1(data);

        //    return result;

        //}

        public ActionResult search_list_WIP_History()
        {
            var mt_no = Request["mt_no"];
            var bom_no = Request["bom_no"];
            if (bom_no != null && bom_no != "") bom_no = bom_no.Substring(1, bom_no.Length - 1);
            var mt_cd = Request["mt_cd"];
            var lct_cd = Request["lct_cd"];
            //StringBuilder varname1 = new StringBuilder();
            //varname1.Append("SELECT a.wmtid,  a.rel_bom, a.rel_bom as rel_bom1, a.mt_no,a.mt_cd, b.mt_nm, a.mt_cd, c.lct_nm AS lct_nm, a.gr_qty, a.remark, \n");
            //varname1.Append("	(select lct_nm from lct_info AS i WHERE  i.lct_cd = a.to_lct_cd) AS to_lct_cd,\n");
            //varname1.Append("  (select lct_nm from lct_info AS i WHERE  i.lct_cd = a.from_lct_cd) AS from_lct_cd, \n");
            //varname1.Append("	(select dt_nm from comm_dt AS i WHERE i.mt_cd ='WHS005' AND i.dt_cd = a.mt_sts_cd) AS mt_sts_cd, \n");
            //varname1.Append("	(select dt_nm from comm_dt AS i WHERE i.mt_cd ='WHS002' AND i.dt_cd = a.mt_sts_cd) AS lct_sts_cd \n");
            //varname1.Append("		FROM w_material_info AS a \n");
            //varname1.Append("		LEFT JOIN d_material_info AS b ON a.mt_no = b.mt_no \n");
            //varname1.Append("		LEFT JOIN lct_info AS c ON c.lct_cd = a.lct_cd \n");
            //varname1.Append("		WHERE a.mt_sts_cd = '004' AND SUBSTRING(a.from_lct_cd, 1,3) ='002' ");
            //varname1.Append("   AND ('' = '"+mt_no+"' OR a.mt_no like '%"+mt_no+"%') ");
            //varname1.Append("   AND ('' = '" + bom_no + "' OR a.rel_bom like '%" + bom_no + "%') ");
            //varname1.Append("   AND ('' = '" + mt_cd + "' OR a.mt_cd like '%" + mt_cd + "%') ");
            //varname1.Append("   AND ('' = '" + lct_cd + "' OR a.to_lct_cd like '%" + lct_cd + "%') ");
            //varname1.Append(" order by a.rel_bom desc, a.mt_no desc, a.mt_cd desc");
            StringBuilder varname1 = new StringBuilder();
            varname1.Append("SELECT a.wipmid, a.hist_sts_cd, a.rel_bom, a.mt_cd, a.mt_no, a.mt_cd, a.rel_bom AS rel_bom1, a.mt_sts_cd,a.gr_qty,a.lct_sts_cd,a.output_dt, a.input_dt,a.bb_no,   (SELECT lct_nm FROM lct_info AS i WHERE i.lct_cd  = a.lct_cd LIMIT 1)  AS  lct_nm, \n");
            varname1.Append("		(SELECT lct_nm FROM lct_info AS i WHERE i.lct_cd  = a.from_lct_cd LIMIT 1)  AS  from_lct_nm, \n");
            varname1.Append("		(SELECT lct_nm FROM lct_info AS i WHERE i.lct_cd  = a.to_lct_cd LIMIT 1)  AS  to_lct_nm, \n");
            varname1.Append("			(SELECT mt_nm FROM d_material_info AS i WHERE i.mt_no  = a.mt_no LIMIT 1)  AS  mt_nm \n");
            varname1.Append("	 FROM wip_material_hist AS a WHERE (a.hist_sts_cd ='001' OR a.hist_sts_cd ='002')");
            varname1.Append("   AND ('' = '" + mt_no + "' OR a.mt_no like '%" + mt_no + "%') ");
            varname1.Append("   AND ('' = '" + bom_no + "' OR a.rel_bom like '%" + bom_no + "%') ");
            varname1.Append("   AND ('' = '" + mt_cd + "' OR a.mt_cd like '%" + mt_cd + "%') ");
            varname1.Append("   AND ('' = '" + lct_cd + "' OR a.to_lct_cd like '%" + lct_cd + "%') ");
            varname1.Append(" order by a.rel_bom desc, a.mt_no desc, a.mt_cd desc");
            var data = new Excute_query().get_data_from_data_base(varname1);
            var result = GetJsonPersons1(data);

            return result;

        }
        public ActionResult Warehouse_Wip_Hitory(lct_info lct_info)
        {
            var lists = db.lct_info.Where(item => (item.mv_yn == "Y" && item.index_cd != null && item.index_cd != "" && item.index_cd != "0") && (item.lct_cd.StartsWith("002") || item.lct_cd.StartsWith("001"))).
              OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();

            return Json(lists, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Warehouse_Return_M_WMS(lct_info lct_info)
        {
            var lists = db.lct_info.Where(item => item.lct_cd.StartsWith("002")).
              OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();
            return Json(lists, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Warehouse_Return_Wip_Search(lct_info lct_info)
        {
            var lists = db.lct_info.Where(item => item.lct_cd.StartsWith("020")).
              OrderBy(item => item.lct_cd).ThenBy(item => item.level_cd).ToList();
            return Json(lists, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region N_Receving_Scan_Wip
        public ActionResult Receving_Scan_Wip(string code)
        {
            ViewData["Message"] = code;
            return SetLanguage("");
    
        }
        public ActionResult GetPickingScan(string sd_no, string sd_nm, string product_cd,string remark)
        {
            var sql = new StringBuilder();
            sql.Append(" SELECT a.*, ")
                .Append(" (select dt_nm  from comm_dt where mt_cd='WHS005' and dt_cd = a.sd_sts_cd) as sts_nm, ")
                .Append("(select lct_nm  from lct_info where lct_cd = a.lct_cd) as lct_nm ")
               .Append(" FROM w_sd_info as a ")
                .Append(" WHERE a.use_yn ='Y' and (a.alert > 0 OR  a.sd_sts_cd <> '000' ) ")
                 .Append("AND ('" + sd_no + "'='' OR  a.sd_no like '%" + sd_no + "%' )")
                 .Append("AND ('" + sd_nm + "'='' OR  a.sd_nm like '%" + sd_nm + "%' )")
                .Append("AND ('" + product_cd + "'='' OR  a.product_cd like '%" + product_cd + "%' )")
                .Append("AND ('" + remark + "'='' OR  a.remark like '%" + remark + "%' )")
                .Append(" order by sid desc ");
            return new InitMethods().ConvertDataTableToJsonAndReturn(sql);
        }
        public ActionResult FinishMaterialWIP( string sid ,string sd_no)
        {
            int resultRow = 0;
            //update lại SD thành alert la 0 , trang thai 001 => cho no vo kho
            if (!string.IsNullOrEmpty(sid)  && !string.IsNullOrEmpty(sd_no))
            {
                resultRow = _IWIPService.UpdateFinishMaterialWIP(sid, sd_no);
                if(resultRow != 0)
                {
                    return Json(new { result = resultRow, message = "finish thành công" }, JsonRequestBehavior.AllowGet);
                }
            }
            return Json(new { result = resultRow, message = "finish thất bại" }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetPickingScanMLQR(string sd_no)
        {

            var sql = new StringBuilder();
            sql.Append(" SELECT t.*, ROW_NUMBER() OVER(ORDER BY (SELECT wmtid)) as id  ")
                .Append(" FROM ( ")
                .Append(" SELECT wmtid , mt_cd,lot_no,expiry_dt,dt_of_receipt,expore_dt,gr_qty, sd_no, mt_sts_cd , ")
                .Append(" (select dt_nm from comm_dt where mt_cd = 'WHS005' and dt_cd = mt_sts_cd) as sts_nm")
                .Append(" FROM w_material_info_tam ")
                .Append(" where sd_no = '" + sd_no + "' and mt_sts_cd='000' and lct_cd LIKE '002%' ")

               .Append(" UNION SELECT ROW_NUMBER() OVER (ORDER BY wmtid ) AS id , mt_cd,lot_no,expiry_dt,dt_of_receipt,expore_dt,gr_qty, sd_no , mt_sts_cd , ")
               .Append(" (select dt_nm from comm_dt where mt_cd = 'WHS005' and dt_cd = mt_sts_cd) as sts_nm")
               .Append(" FROM w_material_info ")
               .Append(" where sd_no = '" + sd_no + "' and mt_sts_cd='000' and lct_cd LIKE '002%' ")
               .Append(" ) t ");
                return new InitMethods().ConvertDataTableToJsonAndReturn(sql);
        }
        public ActionResult InsertMTQRSDList(string data, string sd_no)
        {
            try
            {
               //INSERT w_material_info
                DateTime dt = DateTime.Now; //Today at 00:00:00
                string day_now = dt.ToString("yyyyMMdd");

                var user = Session["authName"] == null ? null : Session["authName"].ToString();
                var id = Convert.ToInt32(data);
                var kt_mt_cd = db.w_material_info_tam.Find(id);
                if (kt_mt_cd == null)
                {
                    return Json(new { result = false, message = exits }, JsonRequestBehavior.AllowGet);
                }

                var mt_cd = kt_mt_cd.mt_cd;
              
                var sql = new StringBuilder()
                    .Append(" INSERT INTO w_material_info(sd_no, mt_no, mt_type,mt_cd,gr_qty ,real_qty," +
                    "recevice_dt,date,expiry_dt ,dt_of_receipt,expore_dt,recevice_dt_tims, lot_no,mt_barcode,mt_qrcode,mt_sts_cd," +
                    "lct_cd,lct_sts_cd,from_lct_cd," +
                    "reg_id, chg_id) ")

                    .Append(" SELECT '" + sd_no + "', mt_no,mt_type, mt_cd,gr_qty,real_qty," +
                    "'" + day_now + "' , '" + day_now + "' ,expiry_dt,dt_of_receipt,expore_dt,recevice_dt_tims,lot_no,mt_barcode,mt_qrcode, '001' ," +
                    "lct_cd,'101',from_lct_cd," +

                    " '" + user + "', '" + user + "'   ")

                    .Append(" FROM w_material_info_tam  as a ")
                   .Append(" where a.wmtid in (" + data + ") ");
                using (var cmd = db.Database.Connection.CreateCommand())
                {
                    db.Database.Connection.Open();
                    cmd.CommandText = sql.ToString();

                    var reader = cmd.ExecuteNonQuery();
                    int result1 = Int32.Parse(reader.ToString());
                    db.Database.Connection.Close();
                }
                db.Database.Connection.Close();
                //DELEtE w_material_info_tam  COLUMN SD
                var sql2 = new StringBuilder();
                sql2.Append(" DELETE FROM  w_material_info_tam  ")


                .Append(" WHERE w_material_info_tam.wmtid in (" + data + ") ");

                using (var cmd = db.Database.Connection.CreateCommand())
                {
                    db.Database.Connection.Open();

                    cmd.CommandText = sql2.ToString();
                    var reader = cmd.ExecuteNonQuery();
                    int result1 = Int32.Parse(reader.ToString());
                    db.Database.Connection.Close();
                }
                db.Database.Connection.Close();
                //UPDATE SD ALERT = 0, AND STS = 001
                //ktra xem còn sd nào trong w_material_tam không, nêu ko có thì update
                var kt_sd = db.w_material_info_tam.Count(x => x.sd_no == sd_no);
                if (kt_sd == 0)
                {
                    var KTTT = db.w_sd_info.Where(x => x.sd_no == sd_no).FirstOrDefault();

                    KTTT.alert = 0;
                    KTTT.sd_sts_cd = "001";
                    KTTT.chg_id = Session["authName"] == null ? null : Session["authName"].ToString();
                    //KTTT.chg_dt = DateTime.Now;

                    db.Entry(KTTT).State = EntityState.Modified;
                    db.SaveChanges();
                }
                //get data table 1
                var sqlsd = new StringBuilder();
                sqlsd.Append(" SELECT a.*, ")
                    .Append(" (select dt_nm  from comm_dt where mt_cd='WHS005' and dt_cd = a.sd_sts_cd) as sts_nm, ")
                    .Append("(select lct_nm  from lct_info where lct_cd = a.lct_cd) as lct_nm ")

                   .Append(" FROM w_sd_info as a ")
                    .Append(" WHERE a.sd_no ='" + sd_no + "' ");

                var ddd = new InitMethods().ConvertDataTableToJsonAndReturn(sqlsd);

                //get data table 2

              
                var datalist = (from a in db.w_material_info
                            where a.mt_cd.Equals(mt_cd)
                            select new
                            {

                                id = a.wmtid,
                                mt_cd = a.mt_cd,
                                lot_no = a.lot_no,
                                gr_qty = a.gr_qty,
                                expiry_dt = a.expiry_dt,
                                dt_of_receipt = a.dt_of_receipt,
                                expore_dt = a.expore_dt,
                                sd_sts_cd = a.mt_sts_cd,
                                sts_nm = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == a.mt_sts_cd).Select(x => x.dt_nm),

                            }
                        ).FirstOrDefault();

                return Json(new { result = true, message = ss , data1 = ddd.Data ,data2 = datalist, kt_sd }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message =exs}, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult UpStatusSd(string sd_no)
        {
            if (string.IsNullOrEmpty(sd_no))
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
            var kt_sd = db.w_material_info_tam.Count(x => x.sd_no == sd_no);
            if (kt_sd == 0)
            {
                var KTTT = db.w_sd_info.Where(x => x.sd_no == sd_no).FirstOrDefault();

                KTTT.alert = 0;
                KTTT.sd_sts_cd = "001";
                KTTT.chg_id = Session["authName"] == null ? null : Session["authName"].ToString();
                //KTTT.chg_dt = DateTime.Now;

                db.Entry(KTTT).State = EntityState.Modified;
                db.SaveChanges();
                //get data table 1
                var sqlsd = new StringBuilder();
                sqlsd.Append(" SELECT a.*, ")
                    .Append(" (select dt_nm  from comm_dt where mt_cd='WHS005' and dt_cd = a.sd_sts_cd) as sts_nm, ")
                    .Append("(select lct_nm  from lct_info where lct_cd = a.lct_cd) as lct_nm ")

                   .Append(" FROM w_sd_info as a ")
                    .Append(" WHERE a.sd_no ='" + sd_no + "' ");

                var ddd = new InitMethods().ConvertDataTableToJsonAndReturn(sqlsd);
                return Json(new { result = true, data1 = ddd.Data }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { result = false }, JsonRequestBehavior.AllowGet);
        }
        #region PartialView_Receving_Scan_Wip_Missing_M_Popup
        public ActionResult PartialView_Receving_Scan_Wip_Missing_M_Popup(string sid = "", string sd_no = "", string alert = "")
        {
            ViewBag.sid = sid;
            ViewBag.alert = alert;
            ViewBag.sd_no = sd_no;
            return PartialView();
        }
        public  ActionResult Get_List_MissMaterial(string sd_no)
        {
            try
            {
                //Check input
                if (string.IsNullOrEmpty(sd_no))
                {
                    return Json(new { result = false, message =chagain+ " SD No!!!" }, JsonRequestBehavior.AllowGet);
                }

                var datalist = (from a in db.w_material_info_tam
                                where a.sd_no.Equals(sd_no) && a.mt_sts_cd.Equals("000")
                                select new
                                {

                                    wmtid = a.wmtid,
                                    mt_cd = a.mt_cd,
                                    mt_no = a.mt_no,
                                    mt_type = a.mt_type,
                                    lot_no = a.lot_no,
                                    gr_qty = a.gr_qty,
                                    expiry_dt = a.expiry_dt,
                                    dt_of_receipt = a.dt_of_receipt,
                                    expore_dt = a.expore_dt,
                                    sd_sts_cd = a.mt_sts_cd,
                                    sts_nm = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == a.mt_sts_cd).Select(x => x.dt_nm),

                                }
                          ).ToList();

                if (datalist.Count > 0)
                {
                    return Json(datalist, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message =exits}, JsonRequestBehavior.AllowGet);

            }
            catch (Exception)
            {
                return Json(new { result = false, message =exs }, JsonRequestBehavior.AllowGet);
            }


            }
    
        public async Task<ActionResult> Feed_back_sd_no(string sid)
        {
            try
            {
                if (string.IsNullOrEmpty(sid))
                {
                    return Json(new { result = false, message = chagain+" SD No!!!" }, JsonRequestBehavior.AllowGet);
                }
                int kt_id;
                if (!int.TryParse(sid, out kt_id))
                {
                    return Json(new { result = false, message = chagain }, JsonRequestBehavior.AllowGet);
                }
                var id = Convert.ToInt32(sid);
                var Updatesd = db.w_sd_info.Find(id);

                if (!Updatesd.sd_sts_cd.Equals("000"))
                {
                    return Json(new { result = false, message = cfs }, JsonRequestBehavior.AllowGet);
                }
                if (Updatesd != null && Updatesd.alert != 2)
                {
                    Updatesd.alert = 2;
                    Updatesd.sd_sts_cd = "000";
                    //Updatesd.chg_dt = DateTime.Now;
                    Updatesd.chg_id = Session["authName"] == null ? null : Session["authName"].ToString();

                    db.Entry(Updatesd).State = EntityState.Modified;
                    db.SaveChanges();
                    await chat.Invoke<string>("Hello", "013").ContinueWith(task => {
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
                    return Json(new { result = true, data = Updatesd, message = ss }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = false, message =exits }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Cancel_sd_no(string sid)
        {
            try
            {
                if (string.IsNullOrEmpty(sid))
                {
                    return Json(new { result = false, message =chagain+ " SD No!!!" }, JsonRequestBehavior.AllowGet);
                }
                int kt_id;
                if (!int.TryParse(sid, out kt_id))
                {
                    return Json(new { result = false, message = chagain }, JsonRequestBehavior.AllowGet);
                }

                var id = Convert.ToInt32(sid);
                var Updatesd = db.w_sd_info.Find(id);

                if (!Updatesd.sd_sts_cd.Equals("000"))
                {
                    return Json(new { result = false, message =cfs }, JsonRequestBehavior.AllowGet);
                }
              
                if (Updatesd != null && Updatesd.alert == 2)
                {
                    Updatesd.alert = 1;
                    Updatesd.sd_sts_cd = "000";
                    //Updatesd.chg_dt = DateTime.Now;
                    Updatesd.chg_id = Session["authName"] == null ? null : Session["authName"].ToString();

                    db.Entry(Updatesd).State = EntityState.Modified;
                    db.SaveChanges();

                    return Json(new { result = true, data = Updatesd, message = ss }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { result = false, message = cnh }, JsonRequestBehavior.AllowGet);
                }
               
            }
            catch (Exception)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
         

        }

        public ActionResult PartialView_WIP_Recei_SD_Popup(string sd_no)
        {
            ViewBag.sd_no = sd_no;

            return PartialView();
        }
        #endregion
        #endregion

        #region N_API_Receiving_Scan_Wip
        //count remain w_material_info_tam

        public  ActionResult Count_Remain_Qty(string sd_no)
        {
            try
            {
                //kiêm tra input
                if (string.IsNullOrEmpty(sd_no))
                {
                    return Json(new { result = false, message = chagain +" SD No!!!" }, JsonRequestBehavior.AllowGet);
                }
                // count w_material_info_tam mt_sts_cd = 000
                var Remain_Qty = db.w_material_info_tam.Count(x => x.mt_sts_cd.Contains("000") && x.sd_no.Contains(sd_no));

                return Json(Remain_Qty, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }

        public async Task<ActionResult> ScanML_no_ReceiWIP(string ml_no, string sd_no)
        {
            try
            {
                //Check input
                if (string.IsNullOrEmpty(sd_no))
                {
                    return Json(new { result = false, message =chagain +" SD No!!!" }, JsonRequestBehavior.AllowGet);
                }
                if (string.IsNullOrEmpty(ml_no))
                {
                    return Json(new { result = false, message = scanagain +" LOT" }, JsonRequestBehavior.AllowGet);
                }
                ml_no = ml_no.Trim();
                //var kttt_null = db.w_material_info_tam.Any(x => x.mt_cd == ml_no);
                var CheckMaterialTam = _IWIPService.GetWMaterialInfoTamwithmtcd(ml_no);
                if (CheckMaterialTam == null)
                {
                    //var kttt_kho_mms = db.w_material_info.Any(x => x.mt_cd == ml_no);
                    var kttt_kho_mms = _IWOService.GetWMaterialInfowithmtcd(ml_no);
                    if (kttt_kho_mms != null)
                    {
                        return Json(new { result = false, message = "Đã được đưa vào kho" }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { result = false, message =exits}, JsonRequestBehavior.AllowGet);
                }
                //var KT_SD = db.w_material_info_tam.Any(x => x.mt_cd == ml_no && x.sd_no.Equals(sd_no) && (x.mt_sts_cd.Equals("000")) );
                var KT_SD = _IWIPService.GetWMaterialInfoTamwithSTS(ml_no);
                //if (KT_SD == false)
                if (KT_SD == null)
                {
                    return Json(new { result = false, message =used }, JsonRequestBehavior.AllowGet);
                }
                //INSERT w_material_info
                DateTime dt = DateTime.Now; //Today at 00:00:00
                string day_now = dt.ToString("yyyyMMdd");

                var user = Session["authName"] == null ? null : Session["authName"].ToString();


                var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                //var sql = new StringBuilder()
                //    .Append(" INSERT INTO w_material_info(rece_wip_dt,picking_dt,sd_no, mt_no, mt_type,mt_cd,gr_qty ,real_qty," +
                //    "recevice_dt,date,expiry_dt ,dt_of_receipt,expore_dt,recevice_dt_tims, lot_no,mt_barcode,mt_qrcode,mt_sts_cd," +
                //    "lct_cd,lct_sts_cd,from_lct_cd," +
                //    "reg_id, chg_id) ")

                //    .Append(" SELECT '"+ time + "',picking_dt,'" + sd_no + "', mt_no,mt_type, mt_cd,gr_qty,real_qty," +
                //    "'" + day_now + "' , '" + day_now + "' ,expiry_dt,dt_of_receipt,expore_dt,recevice_dt_tims,lot_no,mt_barcode,mt_qrcode, '001' ," +
                //    "lct_cd,'101',from_lct_cd," +

                //    " '" + user + "', '" + user + "'   ")

                //    .Append(" FROM w_material_info_tam  as a ")
                //   .Append(" where a.mt_cd in ('" + ml_no + "') ");
                //using (var cmd = db.Database.Connection.CreateCommand())
                //{
                //    db.Database.Connection.Open();
                //    cmd.CommandText = sql.ToString();

                //    var reader = cmd.ExecuteNonQuery();
                //    int result1 = Int32.Parse(reader.ToString());
                //    db.Database.Connection.Close();
                //}
                //db.Database.Connection.Close();

                var w_material_info = new w_material_info();
                w_material_info.rece_wip_dt = time;
                w_material_info.picking_dt = CheckMaterialTam.picking_dt;
                w_material_info.sd_no = sd_no;
                w_material_info.mt_no = CheckMaterialTam.mt_no;
                w_material_info.mt_cd = CheckMaterialTam.mt_cd;
                w_material_info.mt_type = CheckMaterialTam.mt_type;
                w_material_info.gr_qty = CheckMaterialTam.gr_qty;
                w_material_info.real_qty = CheckMaterialTam.real_qty;
                w_material_info.recevice_dt = day_now;
                w_material_info.date = day_now;
                w_material_info.expiry_dt = CheckMaterialTam.expiry_dt;
                w_material_info.expore_dt = CheckMaterialTam.expore_dt;

                w_material_info.dt_of_receipt = CheckMaterialTam.dt_of_receipt;
                w_material_info.recevice_dt_tims = CheckMaterialTam.recevice_dt_tims;
                w_material_info.lot_no = CheckMaterialTam.lot_no;
                w_material_info.mt_barcode = CheckMaterialTam.mt_barcode;
                w_material_info.mt_qrcode = CheckMaterialTam.mt_qrcode;
                w_material_info.mt_sts_cd = "001";
                w_material_info.lct_cd = CheckMaterialTam.lct_cd;
                w_material_info.lct_sts_cd = "101";
                w_material_info.from_lct_cd = CheckMaterialTam.from_lct_cd;
                w_material_info.reg_id = user;
                w_material_info.chg_id = user;
                w_material_info.reg_dt = DateTime.Now; 
                w_material_info.chg_dt = DateTime.Now;

                int idWMaterialInfo = _IWOService.InsertToWmaterialInfo(w_material_info);


                //DELEtE w_material_info_tam  COLUMN SD
                //var sql2 = new StringBuilder();
                //sql2.Append(" DELETE FROM  w_material_info_tam  ")


                //.Append(" WHERE w_material_info_tam.mt_cd in ('" + ml_no + "') ");

                //using (var cmd = db.Database.Connection.CreateCommand())
                //{
                //    db.Database.Connection.Open();

                //    cmd.CommandText = sql2.ToString();
                //    var reader = cmd.ExecuteNonQuery();
                //    int result1 = Int32.Parse(reader.ToString());
                //    db.Database.Connection.Close();
                //}
                //db.Database.Connection.Close();


                _IWIPService.DeleteWMaterialTam(ml_no);

                //UPDATE SD ALERT = 0, AND STS = 001
                //ktra xem còn sd nào trong w_material_tam không, nêu ko có thì update
                //var kt_sd = db.w_material_info_tam.Count(x => x.sd_no == sd_no && x.mt_sts_cd.Equals("000"));
                //if (kt_sd == 0)
                int kt_sd = _IWIPService.GetWMaterialInfoTamwithCount(sd_no, "000");
                if (kt_sd <= 0)
                {
                    //var KTTT = db.w_sd_info.Where(x => x.sd_no == sd_no).FirstOrDefault();

                    //KTTT.alert = 0;
                    //KTTT.sd_sts_cd = "001";
                    //KTTT.chg_id = Session["authName"] == null ? null : Session["authName"].ToString();
                    ////KTTT.chg_dt = DateTime.Now;

                    //db.Entry(KTTT).State = EntityState.Modified;
                    //db.SaveChanges();
                    
                    _IWIPService.UpdatedSdInfo(0, "001", user, sd_no,DateTime.Now);
                  
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
                //get data table 1
                //var sqlsd = new StringBuilder();
                //sqlsd.Append(" SELECT a.*, ")
                //    .Append(" (select dt_nm  from comm_dt where mt_cd='WHS005' and dt_cd = a.sd_sts_cd) as sts_nm, ")
                //    .Append("(select lct_nm  from lct_info where lct_cd = a.lct_cd) as lct_nm ")

                //   .Append(" FROM w_sd_info as a ")
                //    .Append(" WHERE a.sd_no ='" + sd_no + "' ");

                //var ddd = new InitMethods().ConvertDataTableToJsonAndReturn(sqlsd);
                IEnumerable<SdInfoModel> ddd = _IWIPService.GetListSDInfo(sd_no);

                //get data table 2


                //var datalist = (from a in db.w_material_info
                //                where a.mt_cd.Equals(ml_no)
                //                select new
                //                {

                //                    id = a.wmtid,
                //                    mt_cd = a.mt_cd,
                //                    lot_no = a.lot_no,
                //                    gr_qty = a.gr_qty,
                //                    expiry_dt = a.expiry_dt,
                //                    dt_of_receipt = a.dt_of_receipt,
                //                    expore_dt = a.expore_dt,
                //                    sd_sts_cd = a.mt_sts_cd,
                //                    sts_nm = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == a.mt_sts_cd).Select(x => x.dt_nm),

                //                }
                //        ).FirstOrDefault();

                WMaterialInfo datalist = _IWIPService.GetListWMaterialInfo(ml_no);

                return Json(new { result = true, message = ss, data1 = ddd, data2 = datalist, remain_qty= kt_sd }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = exs}, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult GetPickingScanMLQRTotal(string sd_no)
        {

            var data = _IWIPService.GetListMaterialInfoBySdNo(sd_no);
            return (Json(data, JsonRequestBehavior.AllowGet));

        }
        public async Task<ActionResult> ScanML_no_ReceiWIP2(string ml_no, string sd_no, string LocationCode)
        {
            try
            {
                //Check input
                if (string.IsNullOrEmpty(sd_no))
                {
                    return Json(new { result = false, message = chagain + " SD No!!!" }, JsonRequestBehavior.AllowGet);
                }
                if (string.IsNullOrEmpty(ml_no))
                {
                    return Json(new { result = false, message = scanagain + " LOT" }, JsonRequestBehavior.AllowGet);
                }
                if (string.IsNullOrEmpty(LocationCode))
                {
                    return Json(new { result = false, message = "Vui lòng chọn kệ" }, JsonRequestBehavior.AllowGet);
                }
                ml_no = ml_no.Trim();

                //KIỂM TRA XEM SD CÓ TỒN TẠI KHÔNG.
                if (_IWIPService.CheckSdinfo(sd_no))
                {
                    return Json(new { result = false, message = "SD này không được kho NVL gửi qua" }, JsonRequestBehavior.AllowGet);
                }

                //var KT_SD = _IWIPService.GetWMaterialInfoTamwithSTS(ml_no);

                //if (KT_SD == null)
                //{
                //    return Json(new { result = false, message = used }, JsonRequestBehavior.AllowGet);
                //}
                //var CheckMaterialTam = _IWIPService.GetWMaterialInfoTamwithmtcd(ml_no);
                var CheckMaterialTam = _IWIPService.GetWMaterialInfoTamwithSTS(ml_no);
                if (CheckMaterialTam == null)
                {
                    
                    var kttt_kho_mms = _IWOService.GetWMaterialInfowithmtcd(ml_no);
                    if (kttt_kho_mms != null)
                    {
                        return Json(new { result = false, message = "Đã được đưa vào kho SẢN XUẤT <br/>Thuộc sd: " + kttt_kho_mms.sd_no + "<br/> Thời gian:" + kttt_kho_mms.reg_dt}, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { result = false, message = "Mã vừa quét là: "+ ml_no + " <br/> Vui lòng kiểm tra và thử lại" }, JsonRequestBehavior.AllowGet);
                }
                //kiểm tra xem nguyên vật liệu này nhận đủ chưa

                var ktNhanduchua = _IWIPService.GetListMaterialInfoBySdNoMT(sd_no, CheckMaterialTam.mt_no).ToList();
                if (ktNhanduchua.Count > 0)
                {
                    if (ktNhanduchua[0].SoluongConLai == 0)
                    {
                        return Json(new { result = false, message = "Mã " +  ktNhanduchua[0].mt_no + " đã nhận đủ" }, JsonRequestBehavior.AllowGet);
                    }
                }
               
               


                //kiểm tra tên nguyên vật liệu đó có trong ds được order ko
                if (_IWIPService.CheckMaterialNoShipp(CheckMaterialTam.mt_no, sd_no))
                {
                    return Json(new { result = false, message = "Mã này không thuộc những nguyên vật liệu xuất kho" }, JsonRequestBehavior.AllowGet);
                }


                //INSERT w_material_info
                DateTime dt = DateTime.Now; //Today at 00:00:00
                string day_now = dt.ToString("yyyyMMdd");

                var user = Session["authName"] == null ? null : Session["authName"].ToString();


                var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                var w_material_info = new w_material_info();
                w_material_info.rece_wip_dt = time;
                w_material_info.picking_dt = CheckMaterialTam.picking_dt;
                w_material_info.sd_no = sd_no;
                w_material_info.mt_no = CheckMaterialTam.mt_no;
                w_material_info.mt_cd = CheckMaterialTam.mt_cd;
                w_material_info.mt_type = CheckMaterialTam.mt_type;
                w_material_info.gr_qty = CheckMaterialTam.gr_qty;
                w_material_info.real_qty = CheckMaterialTam.real_qty;
                w_material_info.recevice_dt = day_now;
                w_material_info.date = day_now;
                w_material_info.expiry_dt = CheckMaterialTam.expiry_dt;
                w_material_info.expore_dt = CheckMaterialTam.expore_dt;

                w_material_info.dt_of_receipt = CheckMaterialTam.dt_of_receipt;
                w_material_info.recevice_dt_tims = CheckMaterialTam.recevice_dt_tims;
                w_material_info.lot_no = CheckMaterialTam.lot_no;
                w_material_info.mt_barcode = CheckMaterialTam.mt_barcode;
                w_material_info.mt_qrcode = CheckMaterialTam.mt_qrcode;
                w_material_info.mt_sts_cd = "001";
                w_material_info.lct_cd = LocationCode;
                //w_material_info.lct_cd = "002000000000000000";
                w_material_info.lct_sts_cd = "101";
                w_material_info.from_lct_cd = LocationCode;
                w_material_info.reg_id = user;
                w_material_info.chg_id = user;
                w_material_info.reg_dt = DateTime.Now;
                w_material_info.chg_dt = DateTime.Now;

                int idWMaterialInfo = _IWOService.InsertToWmaterialInfo(w_material_info);

                _IWIPService.DeleteWMaterialTam(ml_no);

                //UPDATE SD ALERT = 0, AND STS = 001 // trở về trạng thái tồn kho
             
               var kt_sd = _IWIPService.GetListMaterialInfoBySdNo(sd_no).ToList();
                if (!kt_sd.Any(x=>x.SoluongConLai > 0))
                {
                    _IWIPService.UpdatedSdInfo(0, "001", user, sd_no,DateTime.Now);

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
             
                //IEnumerable<SdInfoModel> ddd = _IWIPService.GetListSDInfo(sd_no);

             
                var data = _IWIPService.GetListMaterialInfoBySdNoMT(sd_no, w_material_info.mt_no);
                return (Json(new { result = true, data = data }, JsonRequestBehavior.AllowGet));
          
            }
            catch (Exception e)
            {
                return Json(new { result = false, message = e }, JsonRequestBehavior.AllowGet);
            }
        }

        //Api Get status mt_cd
        public ActionResult CheckStatusMaterial(string ml_no)
        {
            try
            {
                if (string.IsNullOrEmpty(ml_no))
                {
                    return Json(new { result = false, message = scanagain+" ML No!!!" }, JsonRequestBehavior.AllowGet);
                }
                var mt_cd = Request["ml_no"].ToString().Trim();
              
                var data = (from a in db.w_material_info
                                where a.mt_cd.Equals(mt_cd)
                                select new
                                {
                                    id = a.wmtid,
                                    mt_cd = a.mt_cd,
                                    mt_no = a.mt_no,
                                    buyer_qr = a.buyer_qr,
                                    lot_no = a.lot_no,
                                    gr_qty = a.gr_qty,
                                    recevice_dt_tims = a.recevice_dt_tims,
                                    expiry_dt = a.expiry_dt,
                                    dt_of_receipt = a.dt_of_receipt,
                                    expore_dt = a.expore_dt,
                                    status = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == a.mt_sts_cd).Select(x => x.dt_nm),

                                    Location = db.lct_info.Where(x => x.lct_cd == a.lct_cd).Select(x => x.lct_nm),
                                    Departure = db.lct_info.Where(x => x.lct_cd == a.to_lct_cd).Select(x => x.lct_nm),
                                    Destination = db.lct_info.Where(x => x.lct_cd == a.from_lct_cd).Select(x => x.lct_nm),

                                    Location_Status = db.comm_dt.Where(x => x.mt_cd == "WHS002" && x.dt_cd == a.lct_sts_cd).Select(x => x.dt_nm),

                                }
                       ).FirstOrDefault();
                if (data == null)
                {
                    return Json(new { result = false, message = exits }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = true, data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region N_Shipping_Scan_WIP
        public ActionResult Shipping_Scan_WIP()
        {
            return SetLanguage("~/Views/wipwms/ShippingScanWIP/Shipping_Scan_WIP.cshtml");

        }

        public JsonResult GetEXInfo(Pageing paging, string ex_no, string ex_nm)
        {
            try
            {
                var sql = new StringBuilder();
                sql.Append(" SELECT a.* ")

                   .Append(" FROM w_ex_info as a ")
                   .Append(" WHERE a.use_yn ='Y' ")
                   .Append("AND ('" + ex_no + "'='' OR  a.ex_no like '%" + ex_no + "%' )")
                   .Append("AND ('" + ex_nm + "'='' OR  a.ex_nm like '%" + ex_nm + "%' )")
                   .Append(" order by exid desc ");

                DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);
                int total = dt.Rows.Count;
                var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("exid"));
                return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
            }
            catch (Exception)
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
           
           
        }
        public ActionResult InsertEXInfo(w_ex_info w_ex_info)
        {
            try
            {
                #region Tang tự động

                String ex_no = "EX1";

                var ex_no_last = db.w_ex_info.ToList().LastOrDefault();
                if (ex_no_last != null)
                {
                    var ex_noCode = ex_no_last.ex_no;
                    ex_no = string.Concat("EX", (int.Parse(ex_noCode.Substring(2)) + 1).ToString());
                }

                #endregion Tang tự động

                w_ex_info.ex_no = ex_no;
                w_ex_info.lct_cd = "002000000000000000";
                w_ex_info.alert = 0;

                DateTime dt = DateTime.Now;
                string day_now = dt.ToString("yyyy-MM-dd");
                w_ex_info.work_dt = day_now;

                w_ex_info.ex_sts_cd = "000";
                w_ex_info.reg_id = Session["authName"] == null ? null : Session["authName"].ToString();
                w_ex_info.chg_id = Session["authName"] == null ? null : Session["authName"].ToString();

                w_ex_info.use_yn = "Y";

                w_ex_info.reg_dt = DateTime.Now;
                w_ex_info.chg_dt = DateTime.Now;

                db.w_ex_info.Add(w_ex_info);
                db.SaveChanges();

                var sql = new StringBuilder();
                sql.Append(" SELECT a.* ")

                  .Append(" FROM w_ex_info as a ")
                  .Append(" WHERE a.ex_no ='" + ex_no + "' ");



                var ddd = new InitMethods().ConvertDataTableToJsonAndReturn(sql);
                return Json(new { result = true, data = ddd.Data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { message = exs , result = false }, JsonRequestBehavior.AllowGet);
            }
           

        }
        public ActionResult ModifyEXInfo(w_ex_info w_ex_info)
        {
            try
            {
                var KTTT = db.w_ex_info.Find(w_ex_info.exid);
                if (KTTT == null)
                {
                    return Json(new { result = false, message = exits }, JsonRequestBehavior.AllowGet);
                }
                KTTT.ex_nm = w_ex_info.ex_nm;
                KTTT.remark = w_ex_info.remark;

                KTTT.chg_id = Session["authName"] == null ? null : Session["authName"].ToString();
                //KTTT.chg_dt = DateTime.Now;

                db.Entry(KTTT).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { result = true, data = KTTT }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message =exs}, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult GetShipping_ScanMLQR_WIP(string mt_cd)
        {
            try
            {
                if (string.IsNullOrEmpty(mt_cd))
                {
                    return Json(new { result = false, message = scanagain }, JsonRequestBehavior.AllowGet);
                }

                var kttt_null = db.w_material_info.Any(x => x.mt_cd == mt_cd);
                if (kttt_null == false)// data ko còn không kho sản xuất, kiểm tra kho nguyên vật liệu
                {
                    var kttt_nvl = db.w_material_info_tam.Any(x => x.mt_cd == mt_cd);
                    if (kttt_nvl)
                    {
                        return Json(new { result = false, message = "Mã này đã được đưa về kho nguyên vật liệu" }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { result = false, message = exits }, JsonRequestBehavior.AllowGet);
                }

                var KT_ML = db.w_material_info.Where(x => x.mt_cd.Contains(mt_cd)
                           && string.IsNullOrEmpty(x.ex_no)
                           && x.lct_cd.StartsWith("002")
                           && (x.mt_sts_cd == "001")).ToList();

                if (KT_ML.Count == 0)
                {
                    return Json(new { result = false, message =used }, JsonRequestBehavior.AllowGet);
                }

                var data = (from a in KT_ML

                            select new
                            {
                                wmtid = a.wmtid,
                                mt_cd = a.mt_cd,
                                mt_no = a.mt_no,
                                lot_no = a.lot_no,
                                gr_qty = a.gr_qty,
                                expiry_dt = a.expiry_dt,
                                dt_of_receipt = a.dt_of_receipt,
                                expore_dt = a.expore_dt,
                                sd_sts_cd = a.mt_sts_cd,
                                sts_nm = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == a.mt_sts_cd).Select(x => x.dt_nm),

                                mt_type_nm = db.comm_dt.Where(x => x.mt_cd == "COM004" && x.dt_cd == a.mt_type).Select(x => x.dt_nm),
                                mt_type = a.mt_type,

                            }
                      ).ToList();

                return Json(new { result = true, message =exits, data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult UpdateShipping_ScanMLQR_WIP(string data, string ex_no)
        {
            try
            {
                //ktra input
                if (string.IsNullOrEmpty(ex_no))
                {
                    return Json(new { result = false, message = chagain+" EX No" }, JsonRequestBehavior.AllowGet);
                }

                if (string.IsNullOrEmpty(data))
                {
                    return Json(new { result = false, message = check }, JsonRequestBehavior.AllowGet);
                }

                //UPDATE w_material_info  COLUMN ex

                DateTime dt = DateTime.Now;
                string day_now = dt.ToString("yyyyMMdd");
                string full_date = dt.ToString("yyyy-MM-dd HH:mm:ss");

                var user = Session["authName"] == null ? null : Session["authName"].ToString();

                //update w_ex_info thành sts= 001
                var sqlex = new StringBuilder();
                sqlex.Append(" UPDATE  w_ex_info  ")
                .Append("  SET ex_sts_cd = '001' ")
                .Append(" WHERE w_ex_info.ex_no in ('" + ex_no + "') ");

                int effect_ex = new Excute_query().Execute_NonQuery(sqlex);

                //insert table w_material_info_tam
                StringBuilder sql_insert = new StringBuilder();
                sql_insert.Append("INSERT INTO w_material_info_tam  ")
                          .Append("(mt_type, mt_cd,mt_no, gr_qty,real_qty,sp_cd,mt_sts_cd, date, lot_no,mt_barcode,mt_qrcode,bbmp_sts_cd,expiry_dt,dt_of_receipt,expore_dt) \n")
                          .Append("SELECT mt_type,mt_cd, mt_no, gr_qty, real_qty,sp_cd,'000',date, lot_no, mt_barcode,mt_qrcode,bbmp_sts_cd,expiry_dt,dt_of_receipt,expore_dt \n")
                          .Append("FROM w_material_info \n")
                          .Append(" WHERE w_material_info.wmtid in (" + data + ") ");

                int haminsert = new Excute_query().Execute_NonQuery(sql_insert);
                 //, w_material_info.shipping_wip_dt = '" + full_date + "'
                //update table w_material_info
                var sql2 = new StringBuilder();
                sql2.Append(" UPDATE  w_material_info  ")
               
                .Append("  SET mt_cd = CONCAT(mt_cd, '-TRA', GetNumberTra_Wip(mt_cd)),  w_material_info.ex_no = '" + ex_no + "' , w_material_info.mt_sts_cd = '013', ")
                .Append("   w_material_info.lct_cd = '002000000000000000' , w_material_info.shipping_wip_dt ='" + full_date + "',  ")
                .Append("   w_material_info.chg_id = '" + user + "'  ,w_material_info.chg_dt ='" + full_date + "'  ")

                .Append(" WHERE w_material_info.wmtid in (" + data + ") ");

                int effect_rows = new Excute_query().Execute_NonQuery(sql2);

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
                                mt_no = a.mt_no,
                                lot_no = a.lot_no,
                                gr_qty = a.gr_qty,
                                expiry_dt = a.expiry_dt,
                                dt_of_receipt = a.dt_of_receipt,
                                expore_dt = a.expore_dt,
                                sd_sts_cd = a.mt_sts_cd,
                                sts_nm = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == a.mt_sts_cd).Select(x => x.dt_nm),

                                mt_type_nm = db.comm_dt.Where(x => x.mt_cd == "COM004" && x.dt_cd == a.mt_type).Select(x => x.dt_nm),
                                mt_type = a.mt_type,

                            }
                ).ToList();

                return Json(new { result = true, message =ss , datalist }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = exs }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult DeleteEXInfo(w_ex_info w_ex_info)
        {
            try
            {
                var KTTT = db.w_ex_info.Find(w_ex_info.exid);
                if (KTTT == null)
                {
                    return Json(new { result = false, message = exits}, JsonRequestBehavior.AllowGet);
                }
                if (!(KTTT.ex_sts_cd.Equals("000")))
                {
                    return Json(new { result = false, message = "EX này đã có NVL trả về, Bạn không được xóa nó" }, JsonRequestBehavior.AllowGet);
                }
             
                //UPDATE sd

                var sql1 = new StringBuilder();
                sql1.Append(" UPDATE  w_material_info  ")
                    .Append(" SET w_material_info.ex_no = '' , w_material_info.mt_sts_cd ='001' , w_material_info.lct_cd ='002000000000000000' ")
                    .Append(" WHERE w_material_info.ex_no ='" + KTTT.ex_no + "' ");

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


                return Json(new { result = true, data = KTTT, message = ss }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false, message = exs}, JsonRequestBehavior.AllowGet);
            }
        }

        #region  PartialView_List_ML_NO_Info_Popup_WIP 
        public ActionResult PartialView_List_ML_NO_Info_Popup_WIP(string ex_no)
        {
            ViewBag.ex_no = ex_no;
            return PartialView("~/Views/wipwms/ShippingScanWip/PartialView_List_ML_NO_Info_Popup_WIP.cshtml");
        }
        public JsonResult Get_List_Material_Shipping_WIP(Pageing paging, string mt_cd = "", string mt_no = "")
        {
            try
            {
                StringBuilder sql = new StringBuilder($"CALL spWIP_Shipping_GetList_Ml_no('{mt_cd}','{mt_no}');");
                DataTable dt = new InitMethods().ReturnDataTableNonConstraints(sql);
                int total = dt.Rows.Count;
                var result = dt.AsEnumerable().OrderByDescending(x => x.Field<int>("wmtid"));
                return new InitMethods().ReturnJsonResultWithPaging(paging, total, result);
            }
            catch (Exception)
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
           

            //var KT_ML = db.w_material_info.Where(x => x.mt_cd.Contains(mt_cd)
            //             && x.mt_cd.Contains(mt_no)
            //             && string.IsNullOrEmpty(x.ex_no)
            //             && x.lct_cd.StartsWith("002")
            //             && (x.mt_sts_cd == "001")).ToList();

            //if (KT_ML.Count == 0)
            //{
            //    return Json(new { result = false, message = "Data has been used!!!" }, JsonRequestBehavior.AllowGet);
            //}

            //var data = (from a in KT_ML

            //            select new
            //            {
            //                wmtid = a.wmtid,
            //                mt_cd = a.mt_cd,
            //                mt_no = a.mt_no,
            //                lot_no = a.lot_no,
            //                gr_qty = a.gr_qty,
            //                expiry_dt = a.expiry_dt,
            //                dt_of_receipt = a.dt_of_receipt,
            //                expore_dt = a.expore_dt,
            //                sd_sts_cd = a.mt_sts_cd,
            //                sts_nm = db.comm_dt.Where(x => x.mt_cd == "WHS005" && x.dt_cd == a.mt_sts_cd).Select(x => x.dt_nm),

            //                mt_type_nm = db.comm_dt.Where(x => x.mt_cd == "COM004" && x.dt_cd == a.mt_type).Select(x => x.dt_nm),
            //                mt_type = a.mt_type,

            //            }
            //      ).ToList();


            //int total = data.Count;
            //int totalPages = (int)Math.Ceiling((float)total / paging.rows);
            //if (paging.sidx != null)
            //{
            //    if (string.Equals(paging.sord, "asc", StringComparison.OrdinalIgnoreCase))
            //    {
            //        data = data.OrderBy(x => x.mt_cd).ToList();
            //    }
            //    else
            //    {
            //        data = data.OrderByDescending(x => x.mt_cd).ToList();
            //    }
            //}
            //data = data.Skip((paging.page - 1) * paging.rows).Take(paging.rows).ToList();
            //var jsonReturn = new
            //{
            //    total = totalPages,
            //    page = paging.page,
            //    records = total,
            //    rows = data
            //};
            //return Json(jsonReturn, JsonRequestBehavior.AllowGet);





        }

        public ActionResult PrintEX_LIST(string ex_no)
        {
            ViewData["Message"] = ex_no;
            return PartialView("~/Views/wipwms/ShippingScanWip/PrintEX_LIST.cshtml");
        }

        #endregion

        #region PartialView_Create_List_Memory_Popup
        public ActionResult PartialView_Create_List_Memory_Popup(string ex_no)
        {
            ViewBag.ex_no = ex_no;
            return PartialView("~/Views/wipwms/ShippingScanWip/PartialView_Create_List_Memory_Popup.cshtml");
        }
        public ActionResult GetInfo_memo(string ex_no)
        {
            try
            {
                //get data 
                var datalist = (from a in db.w_material_info_memo
                                where  a.sd_no.Equals(ex_no)
                                select new
                                {
                                    id = a.id,
                                    mt_cd = a.mt_cd,
                                    memo = a.memo,
                                    lot_no = a.lot_no,

                                }
                        ).ToList();

                return Json(datalist, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }

        }
        public ActionResult InsertCreate_memo(w_material_info_memo w_material_info_memo)
        {
            try
            {
                DateTime dt = DateTime.Now; //Today at 00:00:00
                string day_now = dt.ToString("yyyyMMdd");

                w_material_info_memo.receiving_dt = day_now;
                w_material_info_memo.reg_id = Session["authName"] == null ? null : Session["authName"].ToString();
                w_material_info_memo.chg_id = Session["authName"] == null ? null : Session["authName"].ToString();

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
                return Json(new { result = false, message = exs}, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult DelCreate_memo(w_material_info_memo w_material_info_memo)
        {
            try
            {
                var KTTT = db.w_material_info_memo.Find(w_material_info_memo.id);
                if (KTTT == null)
                {
                    return Json(new { result = false, message =exits }, JsonRequestBehavior.AllowGet);
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

        #region PartialView_EX_Info_Popup

        public ActionResult PartialView_EX_Info_Popup(string ex_no)
        {
            ViewBag.ex_no = ex_no;
            return PartialView("~/Views/wipwms/ShippingScanWip/PartialView_EX_Info_Popup.cshtml");

        }

       
        public ActionResult GetExWip_pp(string ex_no)
        {
            var listdata = (from a in db.w_ex_info

                            where a.ex_no.Equals(ex_no)

                            select new
                            {
                                ex_no = a.ex_no,
                                ex_nm = a.ex_nm,
                                ex_sts_cd = a.ex_sts_cd,
                                remark = a.remark,
                            }).ToList();

            return Json(new { data = listdata }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetShippingWIPListPP(string ex_no)
        {
            try
            {
                var data1 = (from a in db.w_material_info
                             where a.ex_no.Equals(ex_no)
                             select new
                             {
                                 wmtid = a.wmtid,
                                 mt_cd = a.mt_cd,
                                 bb_no = a.bb_no,
                                 gr_qty = a.gr_qty,
                               
                                 recevice_dt_tims = a.shipping_wip_dt,

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

        public ActionResult GetPickingScanPP_Count_MT_no(string ex_no)
        {
            try
            {
                StringBuilder varname1 = new StringBuilder();
                varname1.Append(" SELECT COUNT(mt_cd) AS cap, max(abc.mt_no) AS  mt_no, max(abc.mt_sts_cd) mt_sts_cd,  \n");

                varname1.Append("   (SELECT COUNT(trave.mt_cd) FROM w_material_info AS trave WHERE trave.mt_no =  max(abc.mt_no) and  trave.ex_no ='" + ex_no + "' AND trave.lct_cd LIKE '002%' AND trave.mt_sts_cd ='013' ) as trave ");
                varname1.Append("FROM ( \n");
                varname1.Append("SELECT a.mt_no, a.mt_cd , a.mt_sts_cd \n");
                varname1.Append("FROM w_material_info_tam AS a \n");
                varname1.Append("WHERE a.ex_no ='" + ex_no + "' AND a.lct_cd LIKE '002%' UNION ALL \n");
                varname1.Append("SELECT b.mt_no, b.mt_cd, b.mt_sts_cd \n");
                varname1.Append("FROM w_material_info AS b \n");
                varname1.Append("WHERE b.ex_no ='" + ex_no + "' AND b.lct_cd LIKE '002%') AS abc \n");
                varname1.Append("GROUP BY abc.mt_no");

                var list2 = new InitMethods().ConvertDataTableToJsonAndReturn(varname1);


                return Json(new { data = list2.Data }, JsonRequestBehavior.AllowGet);


            }
            catch (Exception e)
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }

        }
        public ActionResult GetShippingScanPP_Memo(string ex_no)
        {
            var listdata = (from a in db.w_material_info_memo

                            where a.sd_no.Equals(ex_no) 

                            select new
                            {
                                id = a.id,
                                mt_cd = a.mt_cd,

                                lot_no = a.lot_no,
                                memo = a.memo

                            }).ToList();

            return Json(new { data = listdata }, JsonRequestBehavior.AllowGet);
        }
        #endregion


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

        public JsonResult ConvertDataTableToJson(DataTable data)
        {
            return Json(GetTableRows(data), JsonRequestBehavior.AllowGet);
        }

        private JsonResult SearchAndPaging(string countSql, string viewSql, int pageIndex, int pageSize)
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
                    data.Load(reader);
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

        private Dictionary<string, string> PagingAndOrderBy(Pageing pageing, string orderByStr)
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
    }

    public class MaterialWip
    {
        public string wmtid { get; set; }
        public string mt_cd { get; set; }
        public string mt_no { get; set; }
        public string mt_nm { get; set; }
        public string mt_type { get; set; }
        public string gr_qty { get; set; }
        public string from_lct_cd { get; set; }
        public string to_lct_cd { get; set; }
        public string bb_no { get; set; }
        public string remark { get; set; }
        public string reg_id { get; set; }
        public string reg_dt { get; set; }
        public string chg_id { get; set; }
        public string chg_dt { get; set; }
        public string status { get; set; }
        public string location { get; set; }
        public string location_status { get; set; }
    }

    public class ShipppingDirectionWIPModel
    {
        public string wmtid { get; set; }
        public string mt_no { get; set; }
        public string mt_nm { get; set; }
        public string gr_qty { get; set; }
        public string mt_bundle_qty { get; set; }
        public string mt_qty { get; set; }
        public string available_qty { get; set; }
        public string req_bundle_qty { get; set; }
        public string req_qty { get; set; }
        public string unit_cd { get; set; }
        public string spec { get; set; }
        public string width { get; set; }
        public string area { get; set; }
        public string group_unit { get; set; }
    }
}

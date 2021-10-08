using Mvc_VD.Models;
using Mvc_VD.Models.Language;
using Mvc_VD.Models.WIP;
using Mvc_VD.Models.WOModel;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mvc_VD.Services
{
    public interface IWIPService
    {
        IEnumerable<GeneralWIP> GetListGeneralMaterialWIP(string mt_no, string product_cd, string mt_nm, string recevice_dt_start,string recevice_dt_end, string sts, string lct_cd,string mt_cd);
        int TotalRecordsSearchGeneralMaterialWIP(string mt_no, string product_cd, string mt_nm, string recevice_dt_start, string recevice_dt_end, string sts, string lct_cd, string mt_cd);
        w_material_info_tam GetWMaterialInfoTamwithmtcd(string mt_cd);
        w_material_info_tam GetWMaterialInfoTamwithSTS(string mt_cd);
        int GetWMaterialInfoTamwithCount(string sd_no, string mt_sts_cd);
        int DeleteWMaterialTam(string mt_cd);
        void UpdatedSdInfo(int? alert, string sd_sts_cd, string chg_id, string sd_no, DateTime chg_dt);

        IEnumerable<SdInfoModel> GetListSDInfo(string sd_no);
        WMaterialInfo GetListWMaterialInfo(string mt_cd);
        IEnumerable<WMaterialInfo> GetListMaterialInfoBySdNo(string sd_no);
        IEnumerable<WMaterialInfo> GetListMaterialInfoBySdNoMT(string sd_no, string mt_no);
        int UpdateFinishMaterialWIP(string sid, string sd_no);
        bool CheckMaterialNoShipp(string mt_no, string sd_no);
        w_sd_info GetListSd(int id);
        bool CheckMaterialSd(string sd_no);
        int DeleteSDInfo(int sid);
        int DeleteShippingSDInfo(string sd_no);
        bool CheckSdinfo(string sd_no);
        IEnumerable<shippingsdmaterial> GetListShippngMaterial(string sd_no, string mt_no);
        shippingsdmaterial GetShippingExist(int id);
        IEnumerable<w_material_info> GetListWMaterial(string sd_no, string mt_no);
        int Deleteshippingsdmaterial(int id);
        int Updateshippingsdmaterial(int id ,int Qty);
        int UpdateshippingMeter(int id ,int Qty);
        IEnumerable<ExportToMachineModel> GetListSearchExportToMachine(string ExportCode, string ExportName, string ProductCode, string ProductName, string Description);
        IEnumerable<ExportToMachineModel> GetListExportToMachine(string ExportCode, string ExportName, string ProductCode, string ProductName, string Description, bool IsFinish);
      
        int TotalListExportToMachine(string ExportCode, string ExportName, string ProductCode, string ProductName, string Description, bool IsFinish);
        int InsertToExportToMachine(ExportToMachineModel item);
        void ModifyToExportToMachine(ExportToMachineModel item);
        int TotalRecordsSearchExportToMachine(string exportCode, string exportName, string productCode, string productName, string description);
        void FinishExportToMachine(ExportToMachineModel item);
        int DeleteToExportToMachine(int id);
        WMaterialInfoNew CheckWMaterialInfo(string materialCode);
        void UpdateMaterialToMachine(WMaterialInfoNew item, string data);
        IEnumerable<WMaterialInfoNew> GetListExportToMachine(string ExportCode);
        IEnumerable<string> GetWmaterialWip(string sd_no);
        WMaterialInfoNew CheckIsExistMaterial(string mt_cd);
        bool CheckMaterialEP(string ExportCode);
        void UpdateReturnMaterialToWIP(WMaterialInfoNew item, string data);
        lct_info CheckIsExistLocation(string lct_cd);
        void UpdateChangeRackMaterialToMachine(WMaterialInfoNew item, string data);
        IEnumerable<ExportToMachineModel> GetListSearchExportToMachinePP(string ExportCode);
        void UpdateMaterialToWIP(WMaterialInfoNew item);
        IEnumerable<ExportToMachineModel> GetListExportToMachine();
        IEnumerable<lct_info> GetLocationWIP(string lct_cd);
        IEnumerable<d_model_info> GetListModel(string ModelCode, string ModelName);
        List<Language> GetLanguage(string language, string router);
        void InsertReturnMaterialToWIP(string ListId);
        int TotalRecordsShowMaterialShipping(string datemonth, string product, string material);
        IEnumerable<MaterialShipping> GetListSearchShowMaterialShipping(string datemonth, string product, string material, string date);
        IEnumerable<MaterialShipping> getShowMaterialShippingDetail(string style_no, string mt_no, string recei_dt, string datemonth);
        IEnumerable<MaterialShippingMemo> GetListSearchShowMemo(string datemonth, string product, string material, string date);
        IEnumerable<InventoryWIPComposite> GetListSearchShowInventoryWIPCPS(string at_no, string model, string product, string product_name,string reg_dt_start,string reg_dt_end,string mt_cd,string bb_no);
    }

    public class WIPService : IWIPService
    {
        private Entities _db;
        public WIPService(IDbFactory dbFactory)
        {
            _db = dbFactory.Init();
        }

        public IEnumerable<GeneralWIP> GetListGeneralMaterialWIP(string mt_no, string product_cd, string mt_nm, string recevice_dt_start,string recevice_dt_end, string sts, string lct_cd, string mt_cd)
        {
            //string viewSql = @" SET sql_mode = '';SET @@sql_mode = '';
            //                SELECT * 
            //        FROM (  
            //                SELECT max(a.mt_no)mt_no,max(b.mt_nm)mt_nm,      
            //                concat( ROW_NUMBER() OVER (ORDER BY a.wmtid ), 'a') AS wmtid, concat(( Case 
            //                WHEN ( 
            //                max(`b`.`bundle_unit`) = 'Roll')  THEN concat(round((sum(`a`.`gr_qty`) / max(`b`.`spec`)),2), ' Roll') 
            //                 ELSE concat(round(SUM(`a`.`gr_qty`),2) ,' EA')
            //                END)) AS `qty`, 
            //              b.bundle_unit,
            //                SUM( CASE  WHEN a.mt_sts_cd='002' THEN a.gr_qty ELSE 0  END)AS 'DSD',
            //                SUM( CASE WHEN (a.mt_sts_cd='001' or a.mt_sts_cd='004') THEN a.gr_qty ELSE 0  END)  AS 'CSD' 
            //                FROM w_material_info AS a 
            //                LEFT JOIN d_material_info AS b ON a.mt_no=b.mt_no 
            //                LEFT JOIN  w_sd_info info ON info.sd_no = a.sd_no 

            //                WHERE  FIND_IN_SET(a.mt_sts_cd, @1) != 0
            //                AND (@2='' OR  a.mt_no like @6 )
            //                AND (@11='' OR  a.mt_cd like @12 )
            //                AND (@3='' OR b.mt_nm like @7 )  
            //                AND (@4='' OR info.product_cd like @8 ) 
            //                AND (@9='' OR a.lct_cd like @10 ) 
            //                 AND (a.ExportCode IS NULL OR a.ExportCode ='')
            //                AND (@5='' OR DATE_FORMAT(a.rece_wip_dt,'%Y/%m/%d') >= DATE_FORMAT(@5,'%Y/%m/%d'))  
            //                AND (@13='' OR DATE_FORMAT(a.recevice_dt,'%Y/%m/%d') <= DATE_FORMAT(@13,'%Y/%m/%d')) 
            //                AND a.lct_cd  LIKE '002%'
            //                AND a.mt_type!='CMT' 
            //                GROUP BY a.mt_no
            //    ) MyDerivedTable";
          

            string viewSql = @" SET sql_mode = '';SET @@sql_mode = '';
                            SELECT  max(table1.mt_no)mt_no,max(table1.mt_nm)mt_nm,      
                            concat( ROW_NUMBER() OVER (ORDER BY table1.wmtid ), 'a') AS wmtid, concat(( Case 
                            WHEN ( 
                            max(`table1`.`bundle_unit`) = 'Roll')  THEN concat(round((sum(`table1`.`gr_qty`) / max(`table1`.`spec`)),2), ' Roll') 
                             ELSE concat(round(SUM(`table1`.`gr_qty`),2) ,' EA')
                            END)) AS `qty`, 
 	                       table1.bundle_unit,
                            SUM( CASE  WHEN table1.mt_sts_cd='002' THEN table1.gr_qty ELSE 0  END)AS 'DSD',
                            SUM( CASE WHEN (table1.mt_sts_cd='001' or table1.mt_sts_cd='004') THEN table1.gr_qty ELSE 0  END)  AS 'CSD' , 

                    table1.recevice_dt
                    FROM (  
                            SELECT a.mt_no,b.mt_nm,    a.wmtid,`b`.`bundle_unit`,`a`.`gr_qty`,`b`.`spec`, a.mt_sts_cd,

                              (
                               CASE 
                               WHEN ('08:00:00' <= DATE_FORMAT( CAST( a.rece_wip_dt AS datetime ),'%H:%i:%s') AND  DATE_FORMAT( CAST( a.rece_wip_dt AS datetime ),'%H:%i:%s')  <  '23:59:59') THEN
                               DATE_FORMAT( CAST( a.rece_wip_dt AS DATETIME ),'%Y-%m-%d')

                               when (DATE_FORMAT( CAST( a.rece_wip_dt AS datetime ),'%H:%i:%s')  < '08:00:00') THEN  DATE_FORMAT( CAST( a.rece_wip_dt AS DATETIME ) - interval 1 DAY ,'%Y-%m-%d')
                                 ELSE ''
                               END )  as recevice_dt
                            FROM w_material_info AS a 
                            LEFT JOIN d_material_info AS b ON a.mt_no=b.mt_no 
                            LEFT JOIN  w_sd_info info ON info.sd_no = a.sd_no 

                            WHERE  FIND_IN_SET(a.mt_sts_cd, @1) != 0
                            AND (@2='' OR  a.mt_no like @6 )
                            AND (@11='' OR  a.mt_cd like @12 )
                            AND (@3='' OR b.mt_nm like @7 )  
                            AND (@4='' OR info.product_cd like @8 ) 
                            AND (@9='' OR a.lct_cd like @10 ) 
                             AND (a.ExportCode IS NULL OR a.ExportCode ='')
                           
                            AND a.lct_cd  LIKE '002%'
                            AND a.mt_type!='CMT' 
                           
                ) table1
                where  (@5='' OR DATE_FORMAT(table1.recevice_dt,'%Y/%m/%d') >= DATE_FORMAT(@5,'%Y/%m/%d'))  
                    AND (@13='' OR DATE_FORMAT(table1.recevice_dt,'%Y/%m/%d') <= DATE_FORMAT(@13,'%Y/%m/%d'))
  GROUP BY table1.mt_no
";



            return _db.Database.SqlQuery<GeneralWIP>(viewSql,
                new MySqlParameter("1", sts),
                new MySqlParameter("2", mt_no),
                new MySqlParameter("3", mt_nm),
                new MySqlParameter("4", product_cd),
                new MySqlParameter("9", lct_cd),
                new MySqlParameter("11", mt_cd),
                new MySqlParameter("5", recevice_dt_start),
                new MySqlParameter("13", recevice_dt_end),
                new MySqlParameter("6", "%" + mt_no + "%"),
                new MySqlParameter("7", "%" + mt_nm + "%"),
                new MySqlParameter("8", "%" + product_cd + "%"),
                new MySqlParameter("10", "%" + lct_cd + "%"),
                new MySqlParameter("12", "%" + mt_cd + "%")
                );
        }

        public int TotalRecordsSearchGeneralMaterialWIP(string mt_no, string product_cd, string mt_nm, string recevice_dt_start,string recevice_dt_end, string sts, string lct_cd, string mt_cd)
        {
            string countSql = @" 
                       SET sql_mode = '';SET @@sql_mode = '';
                            SELECT  COUNT(*) 
                            FROM ( SELECT max(a.mt_no)mt_no,max(b.mt_nm)mt_nm,      
                            concat( ROW_NUMBER() OVER (ORDER BY a.wmtid ), 'a') AS wmtid, concat(( Case 
                            WHEN ( 
                            max(`b`.`bundle_unit`) = 'Roll') THEN round((sum(`a`.`gr_qty`) / max(`b`.`spec`)),2) 
                            ELSE round(MAX(`a`.`gr_qty`),2) 
                            END),' ROLL') AS `qty`, 
 
                            SUM( CASE  WHEN a.mt_sts_cd='002' THEN a.gr_qty ELSE 0  END)AS 'DSD',
                            SUM( CASE WHEN (a.mt_sts_cd='001' or a.mt_sts_cd='004') THEN a.gr_qty ELSE 0  END)  AS 'CSD' 
                            FROM w_material_info AS a 
                            LEFT JOIN d_material_info AS b ON a.mt_no=b.mt_no 
                            LEFT JOIN  w_sd_info info ON info.sd_no = a.sd_no 

                             WHERE FIND_IN_SET(a.mt_sts_cd, @1) != 0
                             AND (@2='' OR  a.mt_no like @6 )
                             AND (@11='' OR  a.mt_cd like @12 )
                            AND (@3='' OR b.mt_nm like @7 )  
                            AND (@4='' OR info.product_cd like @8 ) 
                             AND (@9='' OR a.lct_cd like @10 ) 
                           AND (a.ExportCode IS NULL OR a.ExportCode ='')
                            AND (@5='' OR DATE_FORMAT(a.recevice_dt,'%Y/%m/%d') >= DATE_FORMAT(@5,'%Y/%m/%d'))  
                            AND (@5='' OR DATE_FORMAT(a.recevice_dt,'%Y/%m/%d') <= DATE_FORMAT(@5,'%Y/%m/%d')) 
                            AND a.lct_cd  LIKE '002%'
                            AND a.mt_type!='CMT' 
                     
                            GROUP BY a.mt_no

                        ) MyDerivedTable";

            return _db.Database.SqlQuery<int>(countSql,
                    new MySqlParameter("1", sts),
                    new MySqlParameter("2", mt_no),
                    new MySqlParameter("3", mt_nm),
                    new MySqlParameter("4", product_cd),
                    new MySqlParameter("9", lct_cd),
                    new MySqlParameter("5", recevice_dt_start),
                    new MySqlParameter("13", recevice_dt_end),
                    new MySqlParameter("11", mt_cd),
                    new MySqlParameter("6", "%" + mt_no + "%"),
                    new MySqlParameter("7", "%" + mt_nm + "%"),
                    new MySqlParameter("8", "%" + product_cd + "%"),
                    new MySqlParameter("10", "%" + lct_cd + "%"),
                    new MySqlParameter("12", "%" + mt_cd + "%")
                    ).FirstOrDefault();


        }
        public w_material_info_tam GetWMaterialInfoTamwithmtcd(string mt_cd)
        {
            string QuerySQL = "SELECT * FROM w_material_info_tam WHERE mt_cd = @1 ";
            return _db.Database.SqlQuery<w_material_info_tam>(QuerySQL, new MySqlParameter("1", mt_cd)).FirstOrDefault();
        }

        public w_material_info_tam GetWMaterialInfoTamwithSTS(string mt_cd)
        {
            string QuerySQL = @"SELECT * FROM w_material_info_tam
                                       WHERE mt_cd = @1  and mt_sts_cd = @2 and (mt_barcode != null or mt_barcode != '') and gr_qty > 0
                                            limit 1 ";
            return _db.Database.SqlQuery<w_material_info_tam>(QuerySQL,
                                        new MySqlParameter("1", mt_cd),
                                  
                                        new MySqlParameter("2", "000")
                ).FirstOrDefault();
        }

        public int DeleteWMaterialTam(string mt_cd)
        {
            string sqlquery = @"DELETE FROM w_material_info_tam WHERE mt_cd=@1";
            return _db.Database.ExecuteSqlCommand(sqlquery, new MySqlParameter("1", mt_cd));
        }

        public int GetWMaterialInfoTamwithCount(string sd_no, string mt_sts_cd)
        {
            string sqlquery = @"SELECT COUNT(*) FROM w_material_info_tam
                            WHERE sd_no=@1 AND mt_sts_cd = @2";
            return _db.Database.SqlQuery<int>(sqlquery,
                new MySqlParameter("1", sd_no),
                new MySqlParameter("2", mt_sts_cd)).FirstOrDefault();
        }

        public void UpdatedSdInfo(int? alert, string sd_sts_cd, string chg_id, string sd_no, DateTime chg_dt)
        {
            string sqlquery = @"UPDATE w_sd_info SET alert=@1,sd_sts_cd =@2 , chg_id = @3, chg_dt = @5
                                     WHERE sd_no=@4";
            _db.Database.ExecuteSqlCommand(sqlquery,
                new MySqlParameter("1", alert),
                new MySqlParameter("2", sd_sts_cd),
                new MySqlParameter("3", chg_id),
                new MySqlParameter("4", sd_no),
                new MySqlParameter("5", chg_dt)
                );
        }

        public IEnumerable<SdInfoModel> GetListSDInfo(string sd_no)
        {
            string getvalue = @"SELECT a.*,
                (select dt_nm  from comm_dt where mt_cd='WHS005' and dt_cd = a.sd_sts_cd) as sts_nm,
                (select lct_nm  from lct_info where lct_cd = a.lct_cd) as lct_nm
                FROM w_sd_info a
               
                WHERE a.sd_no= @1";
            return _db.Database.SqlQuery<SdInfoModel>(getvalue, new MySqlParameter("1", sd_no));
        }

        public WMaterialInfo GetListWMaterialInfo(string mt_cd)
        {
            string getvalue = @"SELECT wmtid as id,mt_cd,lot_no,gr_qty,expiry_dt,dt_of_receipt,expore_dt,mt_sts_cd,

                (select dt_nm  from comm_dt where mt_cd='WHS005' and dt_cd = mt_sts_cd) as sts_nm
                FROM w_material_info a
               
                WHERE mt_cd= @1 limit 1";
            return _db.Database.SqlQuery<WMaterialInfo>(getvalue, new MySqlParameter("1", mt_cd)).FirstOrDefault();
        }
        public  IEnumerable<WMaterialInfo> GetListMaterialInfoBySdNo(string sd_no)
        {
            string sql = @"SET sql_mode = '';SET @@sql_mode = '';  SELECT  ROW_NUMBER() OVER () AS wmtid, any_value(abc.mt_no) as mt_no,
                    any_value(abc.SoluongCap) AS SoluongCap, COUNT(info.mt_cd) AS SoLuongNhanDuoc,any_value(abc.meter) AS meter,
                                CASE
                                  WHEN any_value(abc.SoluongCap) > 0 THEN (any_value(abc.SoluongCap) -  COUNT(info.mt_cd)) 
                                  ELSE 0
                                END as SoluongConLai

                                FROM (
                                SELECT  max(a.mt_no) AS mt_no, max(a.id) AS id, sum(a.quantity) AS SoluongCap, max(a.sd_no) AS sd_no, sum(a.meter) AS meter
                                FROM shippingsdmaterial AS a
                                WHERE a.sd_no = @1
                                 GROUP BY a.mt_no
                                ) AS abc
                                left JOIN w_material_info  info ON info.sd_no = abc.sd_no AND abc.mt_no = info.mt_no AND ( info.sts_update IS NULL OR info.sts_update = '')
                                GROUP BY abc.mt_no";

            return _db.Database.SqlQuery<WMaterialInfo>(sql,
                new MySqlParameter("@1", sd_no));
        }
        public IEnumerable<WMaterialInfo> GetListMaterialInfoBySdNoMT(string sd_no, string mt_no)
        {
            string sql = @"SELECT ROW_NUMBER() OVER () AS wmtid, max(abc.mt_no) as mt_no,max(abc.SoluongCap) as SoluongCap, COUNT(info.mt_cd) AS SoLuongNhanDuoc,
                                CASE
                                  WHEN max(abc.SoluongCap) > 0 THEN (max(abc.SoluongCap) -  COUNT(info.mt_cd)) 
                                  ELSE 0
                                END as SoluongConLai

                                FROM (
                                SELECT  max(a.mt_no) AS mt_no,max(a.id) AS id, sum(a.quantity) AS SoluongCap, max(a.sd_no) AS sd_no
                                FROM shippingsdmaterial AS a
                                WHERE a.sd_no = @1
                                group by a.mt_no
                                ) AS abc
                                left JOIN w_material_info  info ON info.sd_no = abc.sd_no AND abc.mt_no = info.mt_no AND ( info.sts_update IS NULL OR info.sts_update = '')
                                Where abc.mt_no = @2 
                                GROUP BY abc.mt_no";

            return _db.Database.SqlQuery<WMaterialInfo>(sql,
                new MySqlParameter("1", sd_no),
                new MySqlParameter("2",  mt_no));
        }
        public int UpdateFinishMaterialWIP(string sid ,string sd_no)
        {
            string sql = "UPDATE w_sd_info set sd_sts_cd = '001' , alert=0 Where sd_no = @1 AND sid = @2 ";
            return _db.Database.ExecuteSqlCommand(sql, new MySqlParameter("1", sd_no), new MySqlParameter("2", sid));
        }

        public bool CheckMaterialNoShipp(string mt_no, string sd_no)
        {
            string QuerySQL = "SELECT a.id FROM shippingsdmaterial AS a WHERE a.mt_no=@1 and a.sd_no = @2";
            return String.IsNullOrWhiteSpace(_db.Database.SqlQuery<string>(QuerySQL, 
                new MySqlParameter("1", mt_no),
                new MySqlParameter("2", sd_no)
                ).FirstOrDefault());
        }
        public w_sd_info GetListSd(int id)
        {
            string getvalue = @"SELECT * FROM w_sd_info WHERE sid = @1";
            return _db.Database.SqlQuery<w_sd_info>(getvalue,
                new MySqlParameter("1", id)).FirstOrDefault();
        }

        public bool CheckMaterialSd(string sd_no)
        {
            string QuerySQL = "SELECT sd_no FROM w_material_info WHERE sd_no = @1 limit 1";
            return String.IsNullOrWhiteSpace(_db.Database.SqlQuery<string>(QuerySQL, 
                new MySqlParameter("1", sd_no)).FirstOrDefault());
        }

        public int DeleteSDInfo(int sid)
        {
            string sqlquery = @"DELETE FROM w_sd_info WHERE sid=@1";
            return _db.Database.ExecuteSqlCommand(sqlquery, new MySqlParameter("1", sid));
        }

        public int DeleteShippingSDInfo(string sd_no)
        {
            string sqlquery = @"DELETE FROM shippingsdmaterial WHERE sd_no=@1";
            return _db.Database.ExecuteSqlCommand(sqlquery, new MySqlParameter("1", sd_no));
        }

        public bool CheckSdinfo(string sd_no)
        {
            string QuerySQL = "SELECT sd_no FROM w_sd_info WHERE sd_no = @1 ";
            return String.IsNullOrWhiteSpace(_db.Database.SqlQuery<string>(QuerySQL,
                new MySqlParameter("1", sd_no)).FirstOrDefault());
        }

        public IEnumerable<shippingsdmaterial> GetListShippngMaterial(string sd_no,string mt_no)
        {
            string sql = @"
                            SELECT *
                            FROM shippingsdmaterial
                            WHERE sd_no = @1 AND mt_no = @2
                            ";

            return _db.Database.SqlQuery<shippingsdmaterial>(sql,
                                     new MySqlParameter("1", sd_no), 
                                     new MySqlParameter("2", mt_no)
                );
        }
        public shippingsdmaterial GetShippingExist(int id)
        {
            string getvalue = @"SELECT *
                            FROM shippingsdmaterial 
                            WHERE id = @1";
            return _db.Database.SqlQuery<shippingsdmaterial>(getvalue, new MySqlParameter("1", id)).FirstOrDefault();
        }

        public IEnumerable<w_material_info> GetListWMaterial(string sd_no, string mt_no)
        {
            string sql = @" SELECT *
                            FROM w_material_info
                            WHERE sd_no = @1 AND mt_no = @2 AND (sts_update IS NULL OR sts_update = '')
                            ";

            return _db.Database.SqlQuery<w_material_info>(sql,
                                     new MySqlParameter("1", sd_no),
                                     new MySqlParameter("2", mt_no)
                );
        }

        public int Deleteshippingsdmaterial(int id)
        {
            string sqlquery = @"DELETE FROM shippingsdmaterial WHERE id=@1";
            return _db.Database.ExecuteSqlCommand(sqlquery, new MySqlParameter("1", id));
           
        }

        public int Updateshippingsdmaterial(int id, int Qty)
        {
            string sql = "UPDATE shippingsdmaterial set quantity = @2 Where id = @1 ";
            return _db.Database.ExecuteSqlCommand(sql,
                new MySqlParameter("1", id),
                new MySqlParameter("2", Qty));
        }

        public int UpdateshippingMeter(int id, int Qty)
        {
            string sql = "UPDATE shippingsdmaterial set meter = @2 Where id = @1 ";
            return _db.Database.ExecuteSqlCommand(sql,
                new MySqlParameter("1", id),
                new MySqlParameter("2", Qty));
        }

        public IEnumerable<ExportToMachineModel> GetListSearchExportToMachine(string ExportCode, string ExportName, string ProductCode, string ProductName, string Description)
        {
            string viewSql = @" SELECT a.* 
              
	                    FROM   exporttomachine AS a
	                    Where  a.IsFinish='N' AND ((@1='' OR  a.ExportCode like @5 ) 
                                AND (@2='' OR  a.ProductCode like @6 )
                                AND (@3='' OR  a.ProductName like @7 )
                                AND (@4='' OR  a.Description like @8 ))
	           
                    order by a.id desc ";
            return _db.Database.SqlQuery<ExportToMachineModel>(viewSql,
                new MySqlParameter("1", ExportCode),
                new MySqlParameter("2", ProductCode),
                new MySqlParameter("3", ProductName),
                new MySqlParameter("4", Description),
                new MySqlParameter("5", "%" + ExportCode + "%"),
                new MySqlParameter("6", "%" + ProductCode + "%"),
                new MySqlParameter("7", "%" + ProductName + "%"),
                new MySqlParameter("8", "%" + Description + "%"));
              
        }
        public IEnumerable<ExportToMachineModel> GetListExportToMachine(string ExportCode, string ExportName, string ProductCode, string ProductName, string Description,bool IsFinish)
        {
            string viewSql = string.Format(@" SELECT a.* 
	                    FROM   exporttomachine AS a
	                    Where  a.IsFinish='{0}' AND ((@1='' OR  a.ExportCode like @5 ) 
                                AND (@2='' OR  a.ProductCode like @6 )
                                AND (@3='' OR  a.ProductName like @7 )
                                AND (@4='' OR  a.Description like @8 ))
                    order by a.id desc ",IsFinish==true? "Y":"N");
            return _db.Database.SqlQuery<ExportToMachineModel>(viewSql,
                new MySqlParameter("1", ExportCode),
                new MySqlParameter("2", ProductCode),
                new MySqlParameter("3", ProductName),
                new MySqlParameter("4", Description),
                new MySqlParameter("5", "%" + ExportCode + "%"),
                new MySqlParameter("6", "%" + ProductCode + "%"),
                new MySqlParameter("7", "%" + ProductName + "%"),
                new MySqlParameter("8", "%" + Description + "%"));

        }

        public int TotalRecordsSearchExportToMachine(string ExportCode, string ExportName, string ProductCode, string ProductName, string Description)
        {
            string countSql = @"SELECT COUNT(*) 
	                    FROM   exporttomachine AS a
	                   Where  a.IsFinish='N' AND ((@1='' OR  a.ExportCode like @5 ) 
                                AND (@2='' OR  a.ProductCode like @6 )
                                AND (@3='' OR  a.ProductName like @7 )
                                AND (@4='' OR  a.Description like @8 ))
                ";
            return _db.Database.SqlQuery<int>(countSql,
                new MySqlParameter("1", ExportCode),
                new MySqlParameter("2", ProductCode),
                new MySqlParameter("3", ProductName),
                new MySqlParameter("4", Description),
                new MySqlParameter("5", "%" + ExportCode + "%"),
                new MySqlParameter("6", "%" + ProductCode + "%"),
                new MySqlParameter("7", "%" + ProductName + "%"),
                new MySqlParameter("8", "%" + Description + "%")
                 ).FirstOrDefault();
        }

        public int TotalListExportToMachine(string ExportCode, string ExportName, string ProductCode, string ProductName, string Description,bool IsFinish)
        {
            string countSql = string.Format(@"SELECT COUNT(*) 
	                    FROM   exporttomachine AS a
	                   Where  a.IsFinish='{0}' AND ((@1='' OR  a.ExportCode like @5 ) 
                                AND (@2='' OR  a.ProductCode like @6 )
                                AND (@3='' OR  a.ProductName like @7 )
                                AND (@4='' OR  a.Description like @8 ))
                ",IsFinish==true? "Y":"N");
            return _db.Database.SqlQuery<int>(countSql,
                new MySqlParameter("1", ExportCode),
                new MySqlParameter("2", ProductCode),
                new MySqlParameter("3", ProductName),
                new MySqlParameter("4", Description),
                new MySqlParameter("5", "%" + ExportCode + "%"),
                new MySqlParameter("6", "%" + ProductCode + "%"),
                new MySqlParameter("7", "%" + ProductName + "%"),
                new MySqlParameter("8", "%" + Description + "%")
                 ).FirstOrDefault();
        }

        public int InsertToExportToMachine(ExportToMachineModel item)
        {
            string QuerySQL = @"INSERT INTO exporttomachine (ExportCode,ProductCode,ProductName,MachineCode,IsFinish,Description,CreateId,CreateDate,ChangeId,ChangeDate)
            VALUES (@1,@2, @3, @4, @5, @6, @7, @8, @9, @10);
            SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(QuerySQL,
                new MySqlParameter("1", item.ExportCode),
                new MySqlParameter("2", item.ProductCode),
                new MySqlParameter("3", item.ProductName),
                new MySqlParameter("4", item.MachineCode),
                new MySqlParameter("5", item.IsFinish),
                new MySqlParameter("6", item.Description),
                new MySqlParameter("7", item.CreateId), 
                new MySqlParameter("8", item.CreateDate),
                new MySqlParameter("9", item.ChangeId),
                new MySqlParameter("10", item.ChangeDate)).FirstOrDefault();
        }

        public void ModifyToExportToMachine(ExportToMachineModel item)
        {
            string QuerySQL = @"UPDATE exporttomachine SET 
     ProductCode=@1,ProductName= @2,MachineCode=@3,Description=@5,CreateId=@6,ChangeId=@7,ChangeDate=@8
            WHERE id=@9";
            _db.Database.ExecuteSqlCommand(QuerySQL,
                new MySqlParameter("1", item.ProductCode),
                new MySqlParameter("2", item.ProductName),
                new MySqlParameter("3", item.MachineCode),
                new MySqlParameter("5", item.Description), 
                new MySqlParameter("6", item.CreateId), 
                new MySqlParameter("7", item.ChangeId),
                new MySqlParameter("8", item.ChangeDate),
                new MySqlParameter("9", item.id));
        }
        public void FinishExportToMachine(ExportToMachineModel item)
        {
            string QuerySQL = string.Format(@"UPDATE exporttomachine SET IsFinish='{0}',ChangeId=@1,ChangeDate=@2 WHERE id=@3",(item.IsFinish=="true"? "Y":"N"));
          _db.Database.ExecuteSqlCommand(QuerySQL, 
                new MySqlParameter("1", item.ChangeId),
                new MySqlParameter("2", item.ChangeDate), 
                new MySqlParameter("3", item.id));
        }

        public int DeleteToExportToMachine(int id)
        {
            string sqlquery = @"DELETE FROM exporttomachine WHERE id=@1";
            return _db.Database.ExecuteSqlCommand(sqlquery, new MySqlParameter("1", id));
        }

        public WMaterialInfoNew CheckWMaterialInfo(string materialCode )
        {
            string QuerySQL = @"SELECT (SELECT lct_nm FROM lct_info WHERE lct_cd =a.lct_cd ) AS locationName, a.* 
                                FROM w_material_info as a WHERE a.mt_cd = @1";
            return _db.Database.SqlQuery<WMaterialInfoNew>(QuerySQL, new MySqlParameter("1", materialCode)).FirstOrDefault();
        }

        public void UpdateMaterialToMachine(WMaterialInfoNew item, string data)
        {
            string sqlupdate = @"Update w_material_info SET ExportCode=@2 ,LoctionMachine = @3,
                    ShippingToMachineDatetime = @4 
                            WHERE  FIND_IN_SET(wmtid,@1) ";
            _db.Database.ExecuteSqlCommand(sqlupdate, new MySqlParameter("1", data),
                                                      new MySqlParameter("2", item.ExportCode),
                                                      new MySqlParameter("3", item.LoctionMachine),
                                                      new MySqlParameter("4", item.ShippingToMachineDatetime)
                 );
        }

        public IEnumerable<WMaterialInfoNew> GetListExportToMachine(string ExportCode)
        {
            string QuerySQL = @"SELECT a.wmtid,a.mt_no,a.mt_sts_cd, a.at_no, a.ShippingToMachineDatetime,SUBSTRING(a.machine_id, 1, 13) AS machine_id, a.mt_cd, a.lot_no, a.gr_qty,    a.ExportCode,              (SELECT dt_nm FROM comm_dt WHERE mt_cd = 'WHS005' AND dt_cd = a.mt_sts_cd) statusName,
                        a.expore_dt, a.dt_of_receipt, a.expiry_dt, a.LoctionMachine 
                        FROM w_material_info as a WHERE ExportCode = @1";
            return _db.Database.SqlQuery<WMaterialInfoNew>(QuerySQL, new MySqlParameter("1", ExportCode));

        }

        public IEnumerable<string> GetWmaterialWip(string sd_no)
        {
            string QuerySQL = @"SELECT rece_wip_dt,picking_dt,wmtid,  mt_cd,mt_no,lot_no,expiry_dt,dt_of_receipt,expore_dt,gr_qty, sd_no, mt_sts_cd, (
                            SELECT dt_nm
                            FROM comm_dt
                            WHERE mt_cd = 'WHS005' AND dt_cd = mt_sts_cd) AS sts_nm, lct_cd,
                             (
                            SELECT lct_nm
                            FROM lct_info
                            WHERE lct_cd = w_material_info.lct_cd ) AS lct_nm,
                            (
                            SELECT at_no
                            FROM w_actual
                            WHERE w_actual.id_actual = w_material_info.id_actual
                            LIMIT 1) AS po
                            FROM w_material_info
                            WHERE sd_no = @1 AND lct_cd LIKE '002%' ";

            return _db.Database.SqlQuery<string>(QuerySQL, new MySqlParameter("1", sd_no));
        }
      
        WMaterialInfoNew IWIPService.CheckIsExistMaterial(string mt_cd)
        {
            string getvalue = @"SELECT *
                FROM w_material_info a
               
                WHERE mt_cd= @1  and a.lct_cd like '002%' and (a.ExportCode is null or a.ExportCode ='') limit 1";
            return _db.Database.SqlQuery<WMaterialInfoNew>(getvalue, new MySqlParameter("1", mt_cd)).FirstOrDefault();
        }

        public bool CheckMaterialEP(string ExportCode)
        {
            string QuerySQL = "SELECT ExportCode FROM w_material_info WHERE ExportCode = @1 limit 1";
            return String.IsNullOrWhiteSpace(_db.Database.SqlQuery<string>(QuerySQL,
                new MySqlParameter("1", ExportCode)).FirstOrDefault());
        }

        public void UpdateReturnMaterialToWIP(WMaterialInfoNew item, string data)
        {
            string sqlupdate = @"Update w_material_info SET ExportCode=@2 ,LoctionMachine = @3,chg_dt= @5, chg_id= @6,lct_cd = @7, ShippingToMachineDatetime=@8 
                            WHERE  FIND_IN_SET(wmtid,@1) ";
            _db.Database.ExecuteSqlCommand(sqlupdate, new MySqlParameter("1", data),
                                                      new MySqlParameter("2", item.ExportCode),
                                                      new MySqlParameter("3", item.LoctionMachine),
                                                      new MySqlParameter("4", item.sts_update),
                                                      new MySqlParameter("5", item.chg_dt),
                                                      new MySqlParameter("6", item.chg_id),
                                                      new MySqlParameter("7", item.lct_cd),
                                                      new MySqlParameter("8", item.ShippingToMachineDatetime)
                 );
        }

        public lct_info CheckIsExistLocation(string lct_cd)
        {
            string QuerySQL = @"SELECT  a.* 
                                FROM lct_info as a WHERE a.lct_cd = @1";
            return _db.Database.SqlQuery<lct_info>(QuerySQL, new MySqlParameter("1", lct_cd)).FirstOrDefault();
        }

        public void UpdateChangeRackMaterialToMachine(WMaterialInfoNew item, string data)
        {
            string sqlupdate = @"Update w_material_info SET chg_dt= @5, chg_id= @6, lct_cd = @7, rece_wip_dt =@8
                            WHERE  FIND_IN_SET(wmtid,@1) ";
            _db.Database.ExecuteSqlCommand(sqlupdate, new MySqlParameter("1", data),
                                                      new MySqlParameter("4", item.sts_update),
                                                      new MySqlParameter("5", item.chg_dt),
                                                      new MySqlParameter("6", item.chg_id),
                                                      new MySqlParameter("7", item.lct_cd),
                                                      new MySqlParameter("8", item.rece_wip_dt)
                 );
        }

        public IEnumerable<ExportToMachineModel> GetListSearchExportToMachinePP(string ExportCode)
        {
            string viewSql = @" SELECT a.* 
              
	                    FROM   exporttomachine AS a
	                    Where  a.ExportCode = @1
	                        ";
            return _db.Database.SqlQuery<ExportToMachineModel>(viewSql,
                new MySqlParameter("1", ExportCode));
        }

        void IWIPService.UpdateMaterialToWIP(WMaterialInfoNew item)
        {
            string sqlupdate = @"Update w_material_info SET ExportCode=@2 ,LoctionMachine = @3,chg_dt= @5, chg_id= @6, ShippingToMachineDatetime=@8 
                            WHERE wmtid = @1 ";
            _db.Database.ExecuteSqlCommand(sqlupdate, new MySqlParameter("1", item.wmtid),
                                                      new MySqlParameter("2", item.ExportCode),
                                                      new MySqlParameter("3", item.LoctionMachine),
                                                      new MySqlParameter("5", item.chg_dt),
                                                      new MySqlParameter("6", item.chg_id),
                                                      new MySqlParameter("8", item.ShippingToMachineDatetime));
        }

        public IEnumerable<ExportToMachineModel> GetListExportToMachine()
        {
            string viewSql = @" SELECT a.* 
              
	                    FROM   exporttomachine AS a
	           
                    order by a.id desc ";
            return _db.Database.SqlQuery<ExportToMachineModel>(viewSql);
        }

        public IEnumerable<lct_info> GetLocationWIP(string lct_cd)
        {
            string viewSql = @"SELECT a.* 
	                    FROM   lct_info  AS a
	                    Where  a.lct_cd LIKE '002%' AND (a.level_cd ='001' OR a.level_cd ='002' ) 
                                AND (@1='' OR  a.lct_cd like @2 )
                    order by a.lct_cd asc";
            return _db.Database.SqlQuery<lct_info>(viewSql,
                new MySqlParameter("1", lct_cd),
                new MySqlParameter("2", "%" + lct_cd + "%"));
        }

        public IEnumerable<d_model_info> GetListModel(string ModelCode, string ModelName)
        {
            string viewSql = @"SELECT a.md_cd  'Model_Code',a.md_nm 'Model_Name',a.use_yn 'Use_Y/N',a.reg_id 'Create_User',a.reg_dt 'Create_Date',a.chg_id 'Change_User',a.chg_dt 'Change_Date' 
	                    FROM   d_model_info  AS a
	                    Where  (@1='' OR  a.md_cd like @2 )
                                AND (@3='' OR  a.md_nm like @4 )
                    order by a.chg_dt asc";
            return _db.Database.SqlQuery<d_model_info>(viewSql,
                new MySqlParameter("1", ModelCode),
                new MySqlParameter("2", "%" + ModelCode + "%"),
                new MySqlParameter("3", ModelName),
                new MySqlParameter("4", "%" + ModelName + "%")
                );
        }
        public List<Language> GetLanguage(string language, string router)
        {
            string sqlQuerry = string.Format(@"SELECT keyname,{0} FROM language WHERE router='{1}' or router='public'", language, router);
            return _db.Database.SqlQuery<Language>(sqlQuerry).ToList<Language>();
        }

        public void InsertReturnMaterialToWIP(string ListId)
        {
            string QuerySQL = @"INSERT INTO w_material_info(mt_no,mt_cd, mt_type, gr_qty, real_qty, ExportCode,LoctionMachine,
                 ShippingToMachineDatetime ,lct_cd, shipping_wip_dt , mt_sts_cd, picking_dt)
           SELECT a.mt_no, concat(a.mt_cd,'-SP-', date_format(now(),'%y%m%d%H%i%s')), a.mt_type,  a.gr_qty, a.real_qty, a.ExportCode,a.LoctionMachine, a.ShippingToMachineDatetime, a.lct_cd,
                a.shipping_wip_dt,'014', date_format(NOW(),'%Y-%m-%d %H:%i:%s')
            FROM w_material_info  as a
             WHERE  FIND_IN_SET(a.wmtid, @1) != 0 ;
            ";
            _db.Database.ExecuteSqlCommand(QuerySQL, new MySqlParameter("1", ListId));
       
        }

        public int TotalRecordsShowMaterialShipping(string datemonth, string product, string material)
        {
            string countSql = @"SELECT count(*)
                    FROM w_material_info AS a
                    JOIN d_material_info AS b ON a.mt_no = b.mt_no
                    WHERE  DATE_FORMAT(a.rece_wip_dt, '%Y-%m') = @1
                    AND a.mt_type ='PMT' AND a.mt_sts_cd != '013'  
                    and (@2='' OR  a.product like @5 )
                    and (@3='' OR  a.mt_no like @6 )
                    GROUP BY CAST(a.rece_wip_dt AS DATE), a.mt_no
                     
                ";
            return _db.Database.SqlQuery<int>(countSql,
                new MySqlParameter("1", datemonth),
                new MySqlParameter("2", product),
                new MySqlParameter("3", material),
                new MySqlParameter("5", "%" + product + "%"),
                new MySqlParameter("6", "%" + material + "%")
                 ).FirstOrDefault();
        }

        public IEnumerable<MaterialShipping> GetListSearchShowMaterialShipping(string datemonth, string product, string material,string  date)
        {
            //string viewSql = @"SELECT max(a.mt_no) MaterialNo,a.lot_no, b.spec,b.width,b.unit_cd, max(a.product)product, COUNT(a.mt_cd) countSocuon,
            //SUM(a.gr_qty)TongSoMet, CAST(a.rece_wip_dt AS DATE) AS recevingDate
            //    FROM w_material_info AS a
            //    JOIN d_material_info AS b ON a.mt_no = b.mt_no
            //    WHERE  DATE_FORMAT(a.rece_wip_dt, '%Y-%m') = @1
            //    AND a.mt_type ='PMT' AND a.mt_sts_cd != '013'  
            //        and (@2='' OR  a.product like @5 )
            //        and (@3='' OR  a.mt_no like @6 )
            //    GROUP BY CAST(a.rece_wip_dt AS DATE), a.mt_no

            //    ORDER BY a.rece_wip_dt DESC  ";
            //return _db.Database.SqlQuery<MaterialShipping>(viewSql,
            //     new MySqlParameter("1", datemonth),
            //    new MySqlParameter("2", product),
            //    new MySqlParameter("3", material),
            //    new MySqlParameter("5", "%" + product + "%"),
            //    new MySqlParameter("6", "%" + material + "%")


            //    );

            string viewSql = @"
                                SELECT (TABLE1.mt_no) MaterialNo, TABLE1.product_cd product,TABLE1.lot_no, TABLE1.spec,TABLE1.width,TABLE1.unit_cd, count(TABLE1.gr_qty) countSocuon, SUM(TABLE1.real_qty) TongSoMet,( TABLE1.reg_date ) recevingDate FROM 
                                (SELECT b.lot_no, a.sd_no, a.product_cd, b.mt_no, b.gr_qty, c.spec,c.width,c.unit_cd, 
                                b.rece_wip_dt, b.real_qty
                                ,
                                 (
                                                CASE 
                                                WHEN ('08:00:00' <= DATE_FORMAT( CAST( b.rece_wip_dt AS datetime ),'%H:%i:%s') AND  DATE_FORMAT( CAST( b.rece_wip_dt AS datetime ),'%H:%i:%s')  <  '23:59:00') THEN
                                                DATE_FORMAT( CAST( b.rece_wip_dt AS DATETIME ),'%Y-%m-%d')

                                                when (DATE_FORMAT( CAST( b.rece_wip_dt AS datetime ),'%H:%i:%s')  < '08:00:00') THEN  DATE_FORMAT( CAST( b.rece_wip_dt AS DATETIME ) - interval 1 DAY ,'%Y-%m-%d')
                                                  ELSE ''
                                                END )  as reg_date

                                FROM w_sd_info AS a
                                JOIN w_material_info AS b ON a.sd_no = b.sd_no
                             JOIN d_material_info AS c ON b.mt_no = c.mt_no
                                WHERE   DATE_FORMAT(b.rece_wip_dt, '%Y-%m') = @1 AND b.mt_type ='PMT'    AND b.sts_update IS NULL 
                                  and (@2='' OR  a.product_cd like @5 )
                                 and (@3='' OR  b.mt_no like @6 ))
                                AS TABLE1  
                                    where  (@4='' OR  TABLE1.reg_date like @7 )
                                  GROUP BY TABLE1.reg_date, TABLE1.mt_no, TABLE1.product_cd
                                  ORDER BY  TABLE1.reg_date DESC, TABLE1.product_cd

                                     ";
            return _db.Database.SqlQuery<MaterialShipping>(viewSql,
                 new MySqlParameter("1", datemonth),
                new MySqlParameter("2", product),
                new MySqlParameter("3", material),
                new MySqlParameter("4", date),
                new MySqlParameter("5", "%" + product + "%"),
                new MySqlParameter("6", "%" + material + "%"),
                new MySqlParameter("7", "%" + date + "%")


                );
        }

        public IEnumerable<MaterialShipping> getShowMaterialShippingDetail(string style_no, string mt_no, string recei_dt, string datemonth)
        {
            string viewSql = @"
                                SELECT (TABLE1.mt_no) MaterialNo,table1.rece_wip_dt, TABLE1.product_cd product,TABLE1.lot_no, TABLE1.spec,table1.sd_no, TABLE1.width,TABLE1.unit_cd,table1.mt_cd, (TABLE1.gr_qty) countSocuon, (TABLE1.gr_qty) TongSoMet,( TABLE1.reg_date ) recevingDate FROM 
                                (SELECT b.lot_no, a.sd_no, a.product_cd, b.mt_no, b.gr_qty, c.spec,c.width,c.unit_cd, b.mt_cd, 
                                b.rece_wip_dt
                                ,
                                 (
                                                CASE 
                                                WHEN ('08:00:00' <= DATE_FORMAT( CAST( b.rece_wip_dt AS datetime ),'%H:%i:%s') AND  DATE_FORMAT( CAST( b.rece_wip_dt AS datetime ),'%H:%i:%s')  <  '23:59:00') THEN
                                                DATE_FORMAT( CAST( b.rece_wip_dt AS DATETIME ),'%Y-%m-%d')

                                                when (DATE_FORMAT( CAST( b.rece_wip_dt AS datetime ),'%H:%i:%s')  < '08:00:00') THEN  DATE_FORMAT( CAST( b.rece_wip_dt AS DATETIME ) - interval 1 DAY ,'%Y-%m-%d')
                                                  ELSE ''
                                                END )  as reg_date

                                FROM w_sd_info AS a
                                JOIN w_material_info AS b ON a.sd_no = b.sd_no
                             JOIN d_material_info AS c ON b.mt_no = c.mt_no
                                WHERE   DATE_FORMAT(b.rece_wip_dt, '%Y-%m') = @4 AND b.mt_type ='PMT'  
                                  and (a.product_cd = @1 )
                                 and (b.mt_no = @2))
                                AS TABLE1  
                                    where  ( TABLE1.reg_date = @3 )
                                
                                  ORDER BY  TABLE1.reg_date DESC , table1.mt_cd asc

                                     ";
            return _db.Database.SqlQuery<MaterialShipping>(viewSql,
                 new MySqlParameter("1", style_no),
                new MySqlParameter("2", mt_no),
                new MySqlParameter("3", recei_dt),
                new MySqlParameter("4", datemonth)


                );
        }

        public IEnumerable<MaterialShippingMemo> GetListSearchShowMemo(string datemonth, string product, string material, string date)
        {
            string viewSql = @"
                        SELECT max(TABLE1.mt_no) mt_no, max(TABLE1.reg_date) reg_date, max(TABLE1.style_no) product,MAX(TABLE1.width) width, MAX(TABLE1.spec) spec, SUM(TABLE1.TX) total_roll, SUM(TABLE1.total_m) total_m, SUM(TABLE1.total_m2) total_m2, SUM(TABLE1.total_ea) total_ea, table1.lot_no
                        FROM (        
                        SELECT (
                           CASE 
                           WHEN ('08:00:00' <= DATE_FORMAT( CAST( a.receiving_dt AS datetime ),'%H:%i:%s') AND  DATE_FORMAT( CAST( a.receiving_dt AS datetime ),'%H:%i:%s')  <  '23:59:00') THEN
                           DATE_FORMAT( CAST( a.receiving_dt AS DATETIME ),'%Y-%m-%d')

                           when (DATE_FORMAT( CAST(a.receiving_dt AS datetime ),'%H:%i:%s')  < '08:00:00') THEN  DATE_FORMAT( CAST(a.receiving_dt AS DATETIME ) - interval 1 DAY ,'%Y-%m-%d')
                             ELSE ''
                           END )  as reg_date,a.receiving_dt,
                        a.mt_cd AS mt_no ,a.style_no,
                        a.width AS width ,
                        a.spec AS spec ,
                        a.TX  ,a.total_m,a.total_m2,a.total_ea, a.lot_no
                        FROM w_material_info_memo a 
                         WHERE   DATE_FORMAT(a.receiving_dt, '%Y-%m') = @1  
                            and (@2='' OR  a.style_no like @5 )
                                 and (@3='' OR  a.mt_cd like @6 )
                        ) AS TABLE1
                            WHERE   (@4='' OR  TABLE1.reg_date like @7 )
                           GROUP BY TABLE1.reg_date, TABLE1.mt_no, TABLE1.style_no
                           ORDER BY  TABLE1.reg_date DESC, TABLE1.style_no

                                     ";
            return _db.Database.SqlQuery<MaterialShippingMemo>(viewSql,
                   new MySqlParameter("1", datemonth),
                new MySqlParameter("2", product),
                new MySqlParameter("3", material),
                new MySqlParameter("4", date),
                new MySqlParameter("5", "%" + product + "%"),
                new MySqlParameter("6", "%" + material + "%"),
                new MySqlParameter("7", "%" + date + "%")


                );
        }

        public IEnumerable<InventoryWIPComposite> GetListSearchShowInventoryWIPCPS(string at_no, string model, string product, string product_name, string reg_dt_start, string reg_dt_end, string mt_cd, string bb_no)
        {
            string viewSql = @" SET sql_mode = '';
                            SELECT table1.wmtid,concat(COUNT(1)) quantity,TABLE1.md_cd model, table1.product product_cd,table1.style_nm product_name, table1.reg_date,TABLE1.at_no,TABLE1.mt_cd,TABLE1.bb_no,

                                     case when 
				                            table1.`level` !=(SELECT max(`level`)level_id 
				                            FROM w_actual 
								                            WHERE at_no=table1.at_no AND `type`='SX') AND table1.`type`!='TIMS' 
						
 		                            then 
						                            SUM(table1.gr_qty) 
			                            ELSE 0 
			                            END AS 'BTP', 
			 
			                             case when 
				                            table1.`level` =(SELECT max(`level`)level_id FROM w_actual 
								                            WHERE at_no=table1.at_no AND `type`='SX') AND table1.`type`!='TIMS' 
							                            then 
						                            SUM(table1.real_qty) 
			                            ELSE 0 
			                            END AS 'TP' 
			
			                            FROM (SELECT a.wmtid, a.product, c.style_nm,c.md_cd, b.`level`,a.at_no,a.gr_qty,a.real_qty, b.`type`,a.mt_cd,a.bb_no,
			                             (
                                        CASE 
                                        WHEN ('08:00:00' <= DATE_FORMAT( CAST( a.reg_dt AS datetime ),'%H:%i:%s') AND  DATE_FORMAT( CAST( a.reg_dt AS datetime ),'%H:%i:%s')  <  '23:59:00') THEN
                                        DATE_FORMAT( CAST( a.reg_dt AS DATETIME ),'%Y-%m-%d')

                                        when (DATE_FORMAT( CAST( a.reg_dt AS datetime ),'%H:%i:%s')  < '08:00:00') THEN  DATE_FORMAT( CAST( a.reg_dt AS DATETIME ) - interval 1 DAY ,'%Y-%m-%d')
                                          ELSE ''
                                        END )  as reg_date
                            FROM  w_material_info a 
                            JOIN w_actual AS b ON a.id_actual=b.id_actual 
                            JOIN d_style_info AS c ON a.product = c.style_no 
                            WHERE (a.lct_cd = '002002000000000000' OR a.lct_cd = '002000000000000000') 
                            AND a.mt_type='CMT'
                             -- AND ( a.mt_sts_cd NOT IN ('005','003') ) 
                             AND ( a.mt_sts_cd = '002' ) 
                            AND (@1='' OR  a.at_no like @7 )
                            AND (@2='' OR  c.md_cd like @8 )
                            AND (@3='' OR  a.product like @9 )
                            AND (@4='' OR c.style_nm like @10 )  
                            AND (@5='' OR a.mt_cd like @11 ) 
                            AND (@6='' OR a.bb_no like @12 ) 
                           
                ) table1
                where  (@13='' OR DATE_FORMAT(table1.reg_date,'%Y/%m/%d') >= DATE_FORMAT(@13,'%Y/%m/%d'))  
                    AND (@14='' OR DATE_FORMAT(table1.reg_date,'%Y/%m/%d') <= DATE_FORMAT(@14,'%Y/%m/%d'))
             GROUP BY table1.product order by table1.product , table1.reg_date desc
";



            return _db.Database.SqlQuery<InventoryWIPComposite>(viewSql,
                new MySqlParameter("1", at_no),
                new MySqlParameter("2", model),
                new MySqlParameter("3", product),
                new MySqlParameter("4", product_name),
                new MySqlParameter("5", mt_cd),
                new MySqlParameter("6", bb_no),
                new MySqlParameter("7", "%" + at_no + "%"),
                new MySqlParameter("8", "%" + model + "%"),
                new MySqlParameter("9", "%" + product + "%"),
                new MySqlParameter("10", "%" + product_name + "%"),
                new MySqlParameter("11", "%" + mt_cd + "%"),
                new MySqlParameter("12", "%" + bb_no + "%"),
                 new MySqlParameter("13", reg_dt_start),
                  new MySqlParameter("14", reg_dt_end)


                );
        }
    }

}
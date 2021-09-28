using Mvc_VD.Classes;
using Mvc_VD.Models;
using Mvc_VD.Models.FG;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Mvc_VD.Services
{
    public interface IFGWmsService
    {
        int UpdateReceFGWMaterialInfo(w_material_info item,string listId);
        w_material_info FindOneMaterialInfoById(string buyerCode);
        generalfg FindOneBuyerInfoById(string buyerCode);
        void Insertgeneralfg(generalfg item);
        d_style_info GetStyleNo(string style_no);
        generalfg FindGeneralfg(string buyerCode);
        stamp_detail FindStampForId(string id);
        w_box_mapping IsFindStampBox(string buyerCode);
        IEnumerable<generalfg> GetListStampBuyerQr(string buyerCode, string productCode);
        void InsertToMesgeneralfg(string listId, string modelCode, string userID);
        d_style_info FindOneProductById(string product);
        IEnumerable<FGRecevingScanExcel> GetListKhacProduct(string buyerCode);
        IEnumerable<MappedProductModel> IsCheckBuyerExist(string buyerCode);
        stamp_detail FindStamp(string buyerCode);
        int InsertToSAPGeneralfg(generalfg generalfg);
        w_material_info CheckBuyerFG(string buyercode);
        int UpdateReceFGWMaterial(w_material_info item);
        int UpdateQtyGeneral(generalfg item);
        mb_info GetMbInfoGrade(string uname);
        bool CheckPOMove(string at_no);
        IEnumerable<FGReceiData> CheckBuyerStatus(string buyerCode);
        WBoxMapping CheckStampBox(string box_no, string status);
        w_box_mapping CheckStampBoxExist(string box_no);
        int UpdateWBoxMapping(w_box_mapping item, string box_no);
        int UpdateWMaterialInfo(w_material_info item, string box_no);
        int UpdateGenneral(generalfg item, string box_no);
        int Updatestamp_detail(stamp_detail item, string box_no);
        IEnumerable<w_dl_info> GetListDlNo(string dl_no);
        IEnumerable<FGShippingScanIDeliveryModel> GetListShippingScanIDelivery(string dl_no);
        void UpdateQtyGenneral(generalfg item, string id);
        w_dl_info CheckDLExist(int id);
        int DeleteDeliveryForId(int? id);
        bool CheckAnyGenneral(string dl_no);
        int TotalRecordsSearchShippingSortingFG(string ShippingCode, string productCode, string productName, string description);
        IEnumerable<ShippingFGSortingModel> GetListSearchShippingSortingFG(string ShippingCode, string ProductCode, string ProductName, string Description);
        IEnumerable<ShippingFGSortingModel> GetLastShippingFGSorting();
        int InsertToShippingFGSorting(ShippingFGSortingModel item);
        void ModifyShippingFGSorting(ShippingFGSortingModel item);
        generalfg CheckIsExistBuyerCode(string BuyerCode);
        void UpdateShippingSortingFG(generalfg item, string data);
        void InsertShippingSortingFGDetail(string ShippingCode, string ListId, string UserID);
        IEnumerable<ShippingFGSortingModel> GetListSearchShippingSortingFGPP(string ShippingCode);
        IEnumerable<ShippingFGSortingDetailModel> GetListShippingFGSorting(string ShippingCode);
        bool CheckSTinfo(string ShippingCode);
        ShippingTIMSSortingDetailModel isCheckExistSF(string ShippingCode, string buyer_qr);
        generalfg isCheckExistGenneral(string buyer_qr);
        string CheckStatus(string status);
        void UpdateLocationtimSorting(ShippingTIMSSortingDetailModel item);
        w_material_info isCheckExistWmaterialBuyer(string buyer_qr);
        void UpdateWMaterialInfoStatus(w_material_info item);
        void UpdateBuyerGeneral(generalfg item);
        void UpdateLotNoGenneral(generalfg item, string id);
        int CheckProcess(string at_no);
        string ChecktypeProduct(string ProductCode);
    }
    public class FGWmsService : IFGWmsService
    {
        private Entities _db;
        public FGWmsService(IDbFactory dbFactory)
        {
            _db = dbFactory.Init();
        }

        public w_material_info FindOneMaterialInfoById(string buyerCode)
        {
            string sql = @"select * from  w_material_info where buyer_qr = @1 limit 1";
            return _db.Database.SqlQuery<w_material_info>(sql, new MySqlParameter("1", buyerCode)).SingleOrDefault();
        }

        public generalfg FindOneBuyerInfoById(string buyerCode)
        {
            string sql = @"select * from  generalfg where buyer_qr = @1 limit 1";
            return _db.Database.SqlQuery<generalfg>(sql, new MySqlParameter("1", buyerCode)).SingleOrDefault();
        }

        public int UpdateReceFGWMaterialInfo(w_material_info item ,string listId)
        {
            string sql = @"UPDATE w_material_info set input_dt = @1  ,lct_cd=@2,from_lct_cd=@3,to_lct_cd=@4,mt_sts_cd=@5,chg_id=@6,chg_dt =@7


                      WHERE  FIND_IN_SET(wmtid,@8) ";
            return _db.Database.ExecuteSqlCommand(sql,
                new MySqlParameter("1", item.input_dt),
                new MySqlParameter("2", item.lct_cd),
                new MySqlParameter("3", item.from_lct_cd),
                new MySqlParameter("4", item.to_lct_cd),
                new MySqlParameter("5", item.mt_sts_cd),
                new MySqlParameter("6", item.chg_id),
                new MySqlParameter("7", item.chg_dt),
                new MySqlParameter("8", listId)              
                );
        }

        public void Insertgeneralfg(generalfg item)
        {
            try
            {
                string sql = @"INSERT INTO generalfg(buyer_qr,product_code,md_cd,dl_no,qty,lot_no,sts_cd,reg_id,reg_dt,chg_id,chg_dt,type)
            Values(@1,@2,@3,@4,@5,@6,@7,@8,@9,@10,@11,'SAP')  ; 
                    SELECT LAST_INSERT_ID()";
               ;
                _db.Database.ExecuteSqlCommand(sql,
                    new MySqlParameter("1", item.buyer_qr)
                    , new MySqlParameter("2", item.product_code),
                    new MySqlParameter("3", item.md_cd),
                    new MySqlParameter("4", item.dl_no)
                    , new MySqlParameter("5", item.qty),
                    new MySqlParameter("6", item.lot_no),
                    new MySqlParameter("7", item.sts_cd)
                    , new MySqlParameter("8", item.reg_id),
                    new MySqlParameter("9", item.reg_dt),
                    new MySqlParameter("10", item.chg_id)
                    , new MySqlParameter("11", item.chg_dt));
            }
            catch (Exception e)
            {

                throw;
            }
           
        }

        public d_style_info GetStyleNo(string style_no)
        {
            string sql = @"Select * From d_style_info Where REPLACE(style_no,'-','') =@1";
            return _db.Database.SqlQuery<d_style_info>(sql, new MySqlParameter("1", style_no)).FirstOrDefault();
        }

        public generalfg FindGeneralfg(string buyerCode)
        {
            string sql = @"Select * From generalfg WHERE buyer_qr = @1 ";
            return _db.Database.SqlQuery<generalfg>(sql,
                new MySqlParameter("1", buyerCode)
                ).FirstOrDefault();
        }

        public w_box_mapping IsFindStampBox(string buyerCode)
        {
            string sql = @"Select * From w_box_mapping WHERE buyer_cd = @1 ";
            return _db.Database.SqlQuery<w_box_mapping>(sql,
                new MySqlParameter("1", buyerCode)
                ).FirstOrDefault();
        }
      public IEnumerable<generalfg> GetListStampBuyerQr(string buyerCode, string productCode)
        {
            string sql = @"SELECT * FROM generalfg AS a WHERE a.sts_cd ='001' 
                     and buyer_qr like @1 and  product_code like @2 ";

            return _db.Database.SqlQuery<generalfg>(sql, 
                new MySqlParameter("1","%" + buyerCode + "%"),
                new MySqlParameter("2", "%" + productCode + "%")
                );
        }

        public stamp_detail FindStampForId(string id)
        {
            string sql = @"Select * From stamp_detail WHERE id = @1 ";
            return _db.Database.SqlQuery<stamp_detail>(sql,
                new MySqlParameter("1", id)
                ).FirstOrDefault();
        }

        public void InsertToMesgeneralfg(string listId, string modelCode, string userID)
        {
            //string sql = @"INSERT INTO generalfg  (buyer_qr,product_code,qty,lot_no,sts_cd,reg_id,reg_dt,chg_id,chg_dt,type,md_cd,at_no)
            //           SELECT  buyer_qr,product,gr_qty, DATE_FORMAT(end_production_dt, '%Y-%m-%d'),mt_sts_cd,@3,now(),@3,now(),'MES', @2,at_no
            //             FROM w_material_info AS b
            //          WHERE FIND_IN_SET(wmtid,@1) AND NOT EXISTS (SELECT * FROM generalfg 
            //        WHERE buyer_qr = b.buyer_qr) ";
            string sql = @"INSERT INTO generalfg  (buyer_qr,product_code,qty,lot_no,sts_cd,reg_id,reg_dt,chg_id,chg_dt,type,md_cd,at_no)
                       SELECT  b.buyer_qr,b.product,b.gr_qty,stam.lot_date,b.mt_sts_cd,@3,now(),@3,now(),'MES', @2,b.at_no
                         FROM w_material_info AS b
                        JOIN stamp_detail AS stam ON b.buyer_qr = stam.buyer_qr
                      WHERE FIND_IN_SET(wmtid,@1) AND NOT EXISTS (SELECT * FROM generalfg 
                    WHERE buyer_qr = b.buyer_qr) ";


            _db.Database.ExecuteSqlCommand(sql,
                new MySqlParameter("1", listId),
                new MySqlParameter("2", modelCode),
                new MySqlParameter("3", userID)
                );
        }

        public d_style_info FindOneProductById(string product)
        {
            
            string sql = @"select * from  d_style_info where REPLACE(style_no, '-','')  = @1 limit 1";
            return _db.Database.SqlQuery<d_style_info>(sql, new MySqlParameter("1", product.Replace("-",""))).SingleOrDefault();
        }

        public IEnumerable<FGRecevingScanExcel> GetListKhacProduct(string buyerCode)
        {
            string sql = @"SELECT TABLE1.*,ROW_NUMBER() OVER(ORDER BY id ASC) AS wmtid
                            FROM (
                            SELECT a.wmtid AS id, a.product AS ProductNo, a.gr_qty AS Quantity, a.buyer_qr AS BuyerCode
                            FROM w_material_info AS a
                            WHERE a.buyer_qr in ("+ buyerCode + @")
                            UNION
                            SELECT b.id,b.product_code AS ProductNo,b.standard_qty AS Quantity, b.buyer_qr AS BuyerCode
                            FROM stamp_detail AS b
                            WHERE b.buyer_qr IN  (" + buyerCode + @")
                            )AS TABLE1";
            var rs = _db.Database.SqlQuery<FGRecevingScanExcel>(sql).ToList();
            return  rs;
        }

        public IEnumerable<MappedProductModel> IsCheckBuyerExist(string buyerCode)
        {
            string sql = @"SELECT id Id , product_code ProductNo, buyer_qr BuyerCode,qty Quantity,
                    DATE_FORMAT(lot_no, '%Y-%m-%d') lot_date From generalfg  WHERE buyer_qr = @1 and sts_cd ='001' ";
            return _db.Database.SqlQuery<MappedProductModel>(sql,
                new MySqlParameter("1", buyerCode));
        }
        public stamp_detail FindStamp(string buyerCode)
        {
            string sql = @"Select * From stamp_detail WHERE buyer_qr = @1 ";
            return _db.Database.SqlQuery<stamp_detail>(sql,
                new MySqlParameter("1", buyerCode)
                ).FirstOrDefault();
        }

        public int InsertToSAPGeneralfg(generalfg generalfg)
        {
            string sql = @"INSERT INTO generalfg (md_cd,lot_no,qty,product_code,sts_cd,type,buyer_qr,chg_dt,reg_dt,chg_id,reg_id)
                VALUES (@1,@2, @3, @4, @5, @6, @7, @8, @9, @10, @11);
                    
                       ";

            return _db.Database.ExecuteSqlCommand(sql,
               new MySqlParameter("1", generalfg.md_cd),
                new MySqlParameter("2", generalfg.lot_no),
                new MySqlParameter("3", generalfg.qty),
                new MySqlParameter("4", generalfg.product_code),
                new MySqlParameter("5", generalfg.sts_cd),
                new MySqlParameter("6", generalfg.type),
                new MySqlParameter("7", generalfg.buyer_qr),
                new MySqlParameter("8", generalfg.chg_dt),
                new MySqlParameter("9", generalfg.reg_dt),
                new MySqlParameter("10", generalfg.chg_id),
                new MySqlParameter("11", generalfg.reg_id)
              );
        }

        public w_material_info CheckBuyerFG(string buyercode)
        {
            string QuerySQL = "SELECT * FROM w_material_info WHERE buyer_qr = @1 AND mt_sts_cd='010' AND lct_cd = '006000000000000000' AND buyer_qr IS NOT NULL";
            return _db.Database.SqlQuery<w_material_info>(QuerySQL, new MySqlParameter("1", buyercode)).FirstOrDefault();
        }

        public int UpdateReceFGWMaterial(w_material_info item)
        {
            string sql = @"UPDATE w_material_info set input_dt = @1  ,lct_cd=@2,from_lct_cd=@3,to_lct_cd=@4,mt_sts_cd=@5,chg_id=@6,chg_dt =@7
                      WHERE buyer_qr = @8  ";
            return _db.Database.ExecuteSqlCommand(sql,
                new MySqlParameter("1", item.input_dt),
                new MySqlParameter("2", item.lct_cd),
                new MySqlParameter("3", item.from_lct_cd),
                new MySqlParameter("4", item.to_lct_cd),
                new MySqlParameter("5", item.mt_sts_cd),
                new MySqlParameter("6", item.chg_id),
                new MySqlParameter("7", item.chg_dt),
                new MySqlParameter("8", item.buyer_qr)
                );
        }

        public int UpdateQtyGeneral(generalfg item)
        {
            string sql = @"UPDATE generalfg set qty = @1 ,chg_id=@2,chg_dt =@3
                      WHERE id = @4  ";
            return _db.Database.ExecuteSqlCommand(sql,
                new MySqlParameter("1", item.qty),
                new MySqlParameter("2", item.chg_id),
                new MySqlParameter("3", item.chg_dt),
                new MySqlParameter("4", item.id)
                );    }
        public mb_info GetMbInfoGrade(string uname)
        {
            string sqlquery = @"SELECT * FROM mb_info WHERE userid=@1 ";
            return _db.Database.SqlQuery<mb_info>(sqlquery,
                new MySqlParameter("1", uname)).FirstOrDefault();
        }

        public bool CheckPOMove(string at_no)
        {
            string sql = @"SELECT IsMove FROM w_actual_primary WHERE at_no = @1 ";
            return _db.Database.SqlQuery<bool>(sql, new MySqlParameter("1", at_no)).SingleOrDefault();
        }
        IEnumerable<FGReceiData> IFGWmsService.CheckBuyerStatus(string buyerCode)
        {
            string QuerySQL = @"SELECT  a.wmtid, a.product ProductNo,b.style_nm ProductName,b.md_cd Model,a.bb_no,a.gr_qty Quantity,a.buyer_qr BuyerCode
                FROM w_material_info AS a
                left JOIN d_style_info AS b 
                ON a.product = b.style_no
                WHERE a.buyer_qr = @1
                AND a.lct_cd = '006000000000000000'
                AND a.mt_sts_cd = '010'   LIMIT 1  ";
            return _db.Database.SqlQuery<FGReceiData>(QuerySQL, new MySqlParameter("1", buyerCode));
        }

        public WBoxMapping CheckStampBox(string box_no, string status)
        {
            string QuerySQL = @" SELECT  MIN(a.bmno) id, min(a.product) product, sum(a.gr_qty) gr_qty, MIN(a.bx_no) bx_no
                                FROM w_box_mapping AS a 
                                WHERE a.bx_no = @1 AND a.sts= @2 
                                GROUP BY a.bx_no ";
            return _db.Database.SqlQuery<WBoxMapping>(QuerySQL, 
                new MySqlParameter("1", box_no),
                new MySqlParameter("2", status)
                ).FirstOrDefault();
        }

        public w_box_mapping CheckStampBoxExist(string box_no)
        {
            string QuerySQL = "SELECT * FROM w_box_mapping WHERE bx_no = @1  ";
            return _db.Database.SqlQuery<w_box_mapping>(QuerySQL,
                new MySqlParameter("1", box_no)
                ).FirstOrDefault();
        }

        public int UpdateWBoxMapping(w_box_mapping item, string box_no)
        {
            string sql = @"UPDATE w_box_mapping SET sts = @1, chg_id=@2,chg_dt=@3
                      WHERE bx_no IN  (" + box_no + @")  ";
            return _db.Database.ExecuteSqlCommand(sql,
                new MySqlParameter("1", item.sts),
                new MySqlParameter("2", item.chg_id),
                new MySqlParameter("3", item.chg_dt)   );
        }

        public int UpdateWMaterialInfo(w_material_info item, string box_no)
        {
            string sql = @"UPDATE w_material_info SET dl_no = @1, mt_sts_cd=@2,lct_cd=@3,from_lct_cd=@4,to_lct_cd=@5,output_dt=@6,chg_id=@7
                      WHERE  buyer_qr IN
                             (SELECT  a.buyer_cd
                               FROM w_box_mapping AS a
                            WHERE a.bx_no  IN  (" + box_no + @")) ";
            return _db.Database.ExecuteSqlCommand(sql,
                new MySqlParameter("1", item.dl_no),
                new MySqlParameter("2", item.mt_sts_cd),
                new MySqlParameter("3", item.lct_cd),
                new MySqlParameter("4", item.from_lct_cd),
                new MySqlParameter("5", item.to_lct_cd),
                new MySqlParameter("6", item.output_dt),
                new MySqlParameter("7", item.chg_id)
                );
        }
        public int UpdateGenneral(generalfg item, string box_no)
        {
            string sql = @"UPDATE generalfg SET dl_no = @1, sts_cd=@2,chg_id=@3,chg_dt=@4
                      WHERE  buyer_qr IN
                             (SELECT  a.buyer_cd
                               FROM w_box_mapping AS a
                            WHERE a.bx_no  IN  (" + box_no + @")) ";
            return _db.Database.ExecuteSqlCommand(sql,
                new MySqlParameter("1", item.dl_no),
                new MySqlParameter("2", item.sts_cd),
                new MySqlParameter("3", item.chg_id),
                new MySqlParameter("4", item.chg_dt)
                );
        }

        public int Updatestamp_detail(stamp_detail item, string box_no)
        {
            string sql = @"UPDATE stamp_detail SET is_sent = @1,chg_id=@2,chg_dt=@3
                      WHERE  buyer_qr IN
                             (SELECT  a.buyer_cd
                               FROM w_box_mapping AS a
                            WHERE a.bx_no  IN  (" + box_no + @")) ";
            return _db.Database.ExecuteSqlCommand(sql,
                new MySqlParameter("1", item.is_sent),
                new MySqlParameter("2", item.chg_id),
                new MySqlParameter("3", item.chg_dt)
                );
        }

        public IEnumerable<w_dl_info> GetListDlNo(string dl_no)
        {
            string sql = @"SELECT a.*, MAX(a.dlid) AS dlid, MAX(a.dl_no) AS dl_no, MAX(a.dl_no) AS dl_no1, MAX(a.dl_nm) AS dl_nm,  MAX(a.remark) AS remark,
                         IFNULL(SUM(g.qty),0) AS quantity
                    FROM  w_dl_info  AS a   LEFT JOIN generalfg AS g ON a.dl_no =g.dl_no
                    WHERE  a.dl_no = @1";

            return _db.Database.SqlQuery<w_dl_info>(sql,
                new MySqlParameter("1", dl_no));
        }

        public IEnumerable<FGShippingScanIDeliveryModel> GetListShippingScanIDelivery(string dl_no)
        {
            string QuerySQL = @"SELECT  c.id AS wmtid, c.buyer_qr,c.qty gr_qty, d.bx_no AS box_no, c.lot_no, date_format(c.chg_dt,'%Y-%m-%d %H:%i:%s') AS shippingDt
               FROM generalfg c
              JOIN w_box_mapping AS  d ON c.buyer_qr = d.buyer_cd
               WHERE c.dl_no = @1  ORDER BY c.chg_dt DESC 
                LIMIT 1000";
            return _db.Database.SqlQuery<FGShippingScanIDeliveryModel>(QuerySQL, new MySqlParameter("1", dl_no));
        }

        public void UpdateQtyGenneral(generalfg item, string id)
        {
            string sqlupdate = @"Update generalfg SET qty= @2 , chg_id = @3, chg_dt = @4
                                            WHERE FIND_IN_SET(id, @1) != 0 ";
           var asf =  _db.Database.ExecuteSqlCommand(sqlupdate,
                new MySqlParameter("1", id),
                new MySqlParameter("2", item.qty),
                new MySqlParameter("3", item.chg_id),
                new MySqlParameter("4", item.chg_dt));
        }

        public w_dl_info CheckDLExist(int id)
        {
            string getvalue = @"SELECT a.*
                            FROM w_dl_info AS a
                            WHERE a.dlid = @1 limit 1";
            return _db.Database.SqlQuery<w_dl_info>(getvalue, new MySqlParameter("1", id)).FirstOrDefault();
        }

        public int DeleteDeliveryForId(int? id)
        {
            string sqlquery = @"DELETE FROM w_dl_info WHERE dlid=@1  ";
            return _db.Database.ExecuteSqlCommand(sqlquery,
                new MySqlParameter("1", id)
                );
        }

        public bool CheckAnyGenneral(string dl_no)
        {
            string QuerySQL = "SELECT true FROM generalfg WHERE dl_no = @1";

            var result = _db.Database.SqlQuery<bool>(QuerySQL,
                new MySqlParameter("1", dl_no)).FirstOrDefault();

            return result;
        }

        public int TotalRecordsSearchShippingSortingFG(string ShippingCode, string productCode, string productName, string description)
        {
            string countSql = @"SELECT COUNT(*) 
	                    FROM   shippingfgsorting AS a
	                   Where  a.IsFinish='N' AND ((@1='' OR  a.ShippingCode like @5 ) 
                                AND (@2='' OR  a.ProductCode like @6 )
                                AND (@3='' OR  a.ProductName like @7 )
                                AND (@4='' OR  a.Description like @8 ))
                ";
            return _db.Database.SqlQuery<int>(countSql,
                new MySqlParameter("1", ShippingCode),
                new MySqlParameter("2", productCode),
                new MySqlParameter("3", productName),
                new MySqlParameter("4", description),
                new MySqlParameter("5", "%" + ShippingCode + "%"),
                new MySqlParameter("6", "%" + productCode + "%"),
                new MySqlParameter("7", "%" + productName + "%"),
                new MySqlParameter("8", "%" + description + "%")
                 ).FirstOrDefault();
        }

        public IEnumerable<ShippingFGSortingModel> GetListSearchShippingSortingFG(string ShippingCode, string ProductCode, string ProductName, string Description)
        {
            string viewSql = @" SELECT a.* 
              
	                    FROM   shippingfgsorting AS a
	                    Where  a.IsFinish='N' AND ((@1='' OR  a.ShippingCode like @5 ) 
                                AND (@2='' OR  a.ProductCode like @6 )
                                AND (@3='' OR  a.ProductName like @7 )
                                AND (@4='' OR  a.Description like @8 ))
	           
                    order by a.id desc ";
            return _db.Database.SqlQuery<ShippingFGSortingModel>(viewSql,
                new MySqlParameter("1", ShippingCode),
                new MySqlParameter("2", ProductCode),
                new MySqlParameter("3", ProductName),
                new MySqlParameter("4", Description),
                new MySqlParameter("5", "%" + ShippingCode + "%"),
                new MySqlParameter("6", "%" + ProductCode + "%"),
                new MySqlParameter("7", "%" + ProductName + "%"),
                new MySqlParameter("8", "%" + Description + "%"));
        }

        public IEnumerable<ShippingFGSortingModel> GetLastShippingFGSorting()
        {
            string viewSql = @" SELECT a.ShippingCode
              
	                    FROM   shippingfgsorting AS a
	           
                    order by a.id desc  limit 1 ";
            return _db.Database.SqlQuery<ShippingFGSortingModel>(viewSql);
        }

        public int InsertToShippingFGSorting(ShippingFGSortingModel item)
        {
            string QuerySQL = @"INSERT INTO shippingfgsorting (ShippingCode,ProductCode,ProductName,IsFinish,Description,CreateId,CreateDate,ChangeId,ChangeDate)
            VALUES (@1,@2, @3, @5, @6, @7, @8, @9, @10);
            SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(QuerySQL,
                new MySqlParameter("1", item.ShippingCode),
                new MySqlParameter("2", item.ProductCode),
                new MySqlParameter("3", item.ProductName),
                new MySqlParameter("5", item.IsFinish),
                new MySqlParameter("6", item.Description),
                new MySqlParameter("7", item.CreateId),
                new MySqlParameter("8", item.CreateDate),
                new MySqlParameter("9", item.ChangeId),
                new MySqlParameter("10", item.ChangeDate)).FirstOrDefault();
        }

        public void ModifyShippingFGSorting(ShippingFGSortingModel item)
        {
            string QuerySQL = @"UPDATE shippingfgsorting SET 
     ProductCode=@1,ProductName= @2,Description=@5,CreateId=@6,ChangeId=@7,ChangeDate=@8
            WHERE id=@9";
            _db.Database.ExecuteSqlCommand(QuerySQL,
                new MySqlParameter("1", item.ProductCode),
                new MySqlParameter("2", item.ProductName),
                new MySqlParameter("5", item.Description),
                new MySqlParameter("6", item.CreateId),
                new MySqlParameter("7", item.ChangeId),
                new MySqlParameter("8", item.ChangeDate),
                new MySqlParameter("9", item.id));
        }

        public generalfg CheckIsExistBuyerCode(string BuyerCode)
        {
            string QuerySQL = @"SELECT a.*
                                FROM generalfg as a WHERE a.buyer_qr = @1 limit 1";
            return _db.Database.SqlQuery<generalfg>(QuerySQL, new MySqlParameter("1", BuyerCode)).FirstOrDefault();
        }

        public void UpdateShippingSortingFG(generalfg item, string data)
        {
            string sqlupdate = @"Update generalfg SET sts_cd=@2 
                            WHERE  FIND_IN_SET(id,@1) ";
            _db.Database.ExecuteSqlCommand(sqlupdate, new MySqlParameter("1", data),
                                                      new MySqlParameter("2", item.sts_cd)
                 );
        }

        public void InsertShippingSortingFGDetail(string ShippingCode, string ListId, string UserID)
        {
            string QuerySQL = @"INSERT INTO shippingfgsortingdetail(ShippingCode,buyer_qr,CreateId,Model,productCode,lot_no,Quantity,location)
           SELECT @2, a.buyer_qr, @3, a.md_cd,a.product_code,a.lot_no, a.qty,a.dl_no
            FROM generalfg  as a
             WHERE  FIND_IN_SET(a.id, @1) != 0 ;
            ";
            _db.Database.ExecuteSqlCommand(QuerySQL, 
                new MySqlParameter("1", ListId),
                new MySqlParameter("2", ShippingCode),
                new MySqlParameter("3", UserID)
                );
        }

        public IEnumerable<ShippingFGSortingModel> GetListSearchShippingSortingFGPP(string ShippingCode)
        {
            string viewSql = @" SELECT a.* 
              
	                    FROM   shippingfgsorting AS a
	                    Where  a.ShippingCode = @1
	                        ";
            return _db.Database.SqlQuery<ShippingFGSortingModel>(viewSql,
                new MySqlParameter("1", ShippingCode));
        }

        public IEnumerable<ShippingFGSortingDetailModel> GetListShippingFGSorting(string ShippingCode)
        {
            string QuerySQL = @"SELECT a.*,( CASE 
                                        WHEN a.location IS NULL   THEN 'FG'
                                        WHEN '006000000000000000' THEN  'TIMS' END) AS locationname
                        
                        FROM shippingfgsortingdetail as a WHERE ShippingCode = @1";
            return _db.Database.SqlQuery<ShippingFGSortingDetailModel>(QuerySQL, new MySqlParameter("1", ShippingCode));
        }

        public bool CheckSTinfo(string ShippingCode)
        {
            string QuerySQL = "SELECT ShippingCode FROM shippingtimssorting WHERE ShippingCode = @1 limit 1";
            return String.IsNullOrWhiteSpace(_db.Database.SqlQuery<string>(QuerySQL,
                new MySqlParameter("1", ShippingCode)).FirstOrDefault());
        }

        public ShippingTIMSSortingDetailModel isCheckExistSF(string ShippingCode, string buyer_qr)
        {
            string getvalue = @"SELECT *
                            FROM shippingtimssortingdetail 
                            WHERE ShippingCode = @1 and buyer_qr = @2";
            return _db.Database.SqlQuery<ShippingTIMSSortingDetailModel>(getvalue,
                new MySqlParameter("1", ShippingCode),
                new MySqlParameter("2", buyer_qr)
                ).FirstOrDefault();
        }

        public generalfg isCheckExistGenneral(string buyer_qr)
        {
            string getvalue = @"SELECT *
                            FROM generalfg  
                            WHERE buyer_qr = @1 ";
            return _db.Database.SqlQuery<generalfg>(getvalue,
                new MySqlParameter("1", buyer_qr)
                ).FirstOrDefault();
        }

        public string CheckStatus(string status)
        {
            string sqlquery = @"SELECT dt_nm FROM comm_dt WHERE  dt_cd = @1 and mt_cd = 'WHS005' ";
            return _db.Database.SqlQuery<string>(sqlquery, new MySqlParameter("@1", status)).FirstOrDefault();
        }
        public void UpdateLocationtimSorting(ShippingTIMSSortingDetailModel item)
        {
            string sqlupdate = @"Update shippingtimssortingdetail SET location = @1, ChangeId=@2
                            WHERE  buyer_qr = @3 ";
            _db.Database.ExecuteSqlCommand(sqlupdate, new MySqlParameter("1", item.location),
                                                      new MySqlParameter("2", item.ChangeId),
                                                      new MySqlParameter("3", item.buyer_qr)
                 );
        }

        public w_material_info isCheckExistWmaterialBuyer(string buyer_qr)
        {
            string getvalue = @"SELECT *
                            FROM w_material_info 
                            WHERE  buyer_qr = @1";
            return _db.Database.SqlQuery<w_material_info>(getvalue,
                new MySqlParameter("1", buyer_qr)
                ).FirstOrDefault();
        }

        public void UpdateWMaterialInfoStatus(w_material_info item)
        {
            string sqlupdate = @"Update w_material_info SET mt_sts_cd = @1, lct_cd=@2
                            WHERE  buyer_qr = @3 ";
            _db.Database.ExecuteSqlCommand(sqlupdate, new MySqlParameter("1", item.mt_sts_cd),
                                                      new MySqlParameter("2", item.lct_cd),
                                                      new MySqlParameter("3", item.buyer_qr)
                 );
        }

        public void UpdateBuyerGeneral(generalfg item)
        {
            string sqlupdate = @"Update generalfg SET sts_cd = @1
                            WHERE  buyer_qr = @3 ";
            _db.Database.ExecuteSqlCommand(sqlupdate, new MySqlParameter("1", item.sts_cd),
                                                      new MySqlParameter("3", item.buyer_qr)
                 );
        }

        public void UpdateLotNoGenneral(generalfg item, string id)
        {
            string sqlupdate = @"Update generalfg SET lot_no= @2, chg_id = @3, chg_dt = @4
                                            WHERE FIND_IN_SET(id, @1) != 0 
                                            AND ( ( SELECT productType FROM d_style_info
														   WHERE d_style_info.style_no = generalfg.product_code )= 1)";
            var asf = _db.Database.ExecuteSqlCommand(sqlupdate,
                 new MySqlParameter("1", id),
                 new MySqlParameter("2", item.lot_no),
                 new MySqlParameter("3", item.chg_id),
                 new MySqlParameter("4", item.chg_dt));
        }

        public int CheckProcess(string at_no)
        {
            string sqlupdate = @" SELECT COUNT(a.id_actual)
                    FROM w_actual AS a
                    WHERE a.at_no =@1 AND a.`type` ='TIMS'; ";
          return _db.Database.SqlQuery<int>(sqlupdate,
                 new MySqlParameter("1", at_no)).FirstOrDefault();
        }

        public string ChecktypeProduct(string ProductCode)
        {
            string sql = @"Select productType 
            from d_style_info a
          
            where   a.style_no = @1";
            return _db.Database.SqlQuery<string>(sql, new MySqlParameter("@1", ProductCode)).FirstOrDefault();
        }
    }
}


using Mvc_VD.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mvc_VD.Services
{
    public interface IDMSService
    {
        IEnumerable<MaterialBomModel> GetListmaterialbom(int id);
        BomInfoModel GetListBom(int id);
        void UpdateMaterialBomDeTinhHieuSuat(int MaterialBOMID,int BOMID);
        int InsertToBomInfo(d_bom_info item, bool IsActive);
        d_bom_info CheckBomExist(string style_no, string mt_no);
        int DeleteMaterialBom(string style_no, int? MaterialID);
        int DeleteMaterialBomForId(int? MaterialBOMID);
        int DelMaterial(int bid);
        int DeleteBomInfo(string style_no);
        int DeleteAaLlMaterialBom(string BOMID);
        void UpdateBomInfo(d_bom_info item, int? isActive);
        void UpdateBomDeTinhHieuSuat(string product);
        int InsertToWmaterialTam(w_material_info_tam item, string _barcode, int send_qty, string date, string time_now);
        IEnumerable<WMaterialTam> GetListTam(string mt_no, string date, string time_now, int send_qty);
        void UpdateQtyWMaterailTam(w_material_info_tam item, string id);
        void UpdateProductDeApply(int bid, string IsApply);
        int InsertToMaterialBom(CreateBomMaterialModel item);
        IEnumerable<CreateBomMaterialModel> GetListmaterialbomcap3(string style_no, string mt_no);
        int DelMaterialBom(int Id);
        int InsertToDRoutingInfo(DRoutingInfo item);
        void UpdateDRoutingInfo(d_rounting_info item, string description, string isFinish);
        void UpdateNVLDeTinhHieuSuat(string product);
        int InsertToProductMaterial(ProductMaterailModel item);
        ProductMaterailModel GetProductMaterial(int id);
        void UpdateToProductMaterial(ProductMaterailModel item);
        int DeleteProductMaterialForId(int? id);
        ProductMaterailModel CheckMaterialExist(string style_no, int level, string mt_no, string process_code);
        int InsertToMaterialChild(ProductMaterialDetailModel item);
        IEnumerable<ProductMaterialDetailModel> GetListmaterialChild(string ProductCode, string name, string MaterialPrarent, string process_code);
        int DeleteMaterialChild(int id);
        int DeleteProductMaterialDetail(string ProductCode,int? level, string MaterialPrarent);
        int InsertToStyleInfo(d_style_info item);
        void UpdateToStyleInfo(d_style_info item);
        void UpdateIsFinish(string product, string process_code);
        int InsertToProductProcess(ProductProcess item);
        ProductProcess GetProductProcess(int id);
        void UpdateToProductProcess(ProductProcess item);
        void UpdateProcessToApply(string product);
        IEnumerable<DRoutingInfo> CheckLevelProcess(string style_no, string process_code);
    }
    public class DMSService : IDMSService
    {
        private Entities _db;
        public DMSService(IDbFactory dbFactory)
        {
            _db = dbFactory.Init();
        }
        public IEnumerable<MaterialBomModel> GetListmaterialbom(int id)
        {
            string getvalue = @"SELECT MaterialBOMID,IsActive,
                (SELECT mt_no FROM d_material_info WHERE mtid = MaterialID) AS materialNo,
                (SELECT mt_nm FROM d_material_info WHERE mtid = MaterialID) AS materialName
                FROM materialbom a
              
                where a.BOMID= @1 order by a.BOMID desc";
            return _db.Database.SqlQuery<MaterialBomModel>(getvalue, new MySqlParameter("@1", id));
        }
        public void UpdateMaterialBomDeTinhHieuSuat(int BOMID , int MaterialBOMID)
        {
            string sqlupdate = @"Update materialbom SET IsActive=0 WHERE BOMID=@1;
                                 Update materialbom SET IsActive=1 WHERE MaterialBOMID=@2; ";
            _db.Database.ExecuteSqlCommand(sqlupdate, new MySqlParameter("@1", BOMID),
                                                      new MySqlParameter("@2", MaterialBOMID )
                 );


        }
        public int InsertToBomInfo(d_bom_info item, bool IsActive)
        {
            string QuerySQL = @"INSERT INTO d_bom_info
                (bom_no,style_no,mt_no,need_time,cav,need_m,buocdap,del_yn,reg_id,reg_dt,chg_id,chg_dt, IsActive)
            VALUES (@1,@2, @3, @4, @5, @6, @7, @8, @9, @10,@9, @10,@11);
            SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(QuerySQL, new MySqlParameter("@1", item.bom_no),
                new MySqlParameter("@2", item.style_no), new MySqlParameter("@3", item.mt_no), new MySqlParameter("@4", item.need_time),
                new MySqlParameter("@5", item.cav), new MySqlParameter("@6", item.need_m), new MySqlParameter("@7", item.buocdap),
                new MySqlParameter("@8", item.del_yn), new MySqlParameter("@9", item.reg_id),
                new MySqlParameter("@10", item.reg_dt), new MySqlParameter("@11", IsActive)).FirstOrDefault();
        }


        public BomInfoModel GetListBom(int id)
        {
            string getvalue = @"SELECT a.*,b.md_cd,b.style_nm,
                                (SELECT mt_nm from d_material_info where a.mt_no = mt_no limit 1) as mt_nm
                            FROM d_bom_info AS a
                            JOIN d_style_info AS b ON a.style_no=b.style_no
                            
                            WHERE a.bid = @1";
            return _db.Database.SqlQuery<BomInfoModel>(getvalue, new MySqlParameter("1", id)).FirstOrDefault();
        }

        public d_bom_info CheckBomExist(string style_no, string mt_no)
        {
            string QuerySQL = "SELECT * FROM d_bom_info WHERE style_no = @1 and mt_no = @2";
            return _db.Database.SqlQuery<d_bom_info>(QuerySQL, 
                new MySqlParameter("1", style_no),
                new MySqlParameter("2", mt_no)
                ).FirstOrDefault();
           
        }

        public int DeleteMaterialBom(string style_no, int? MaterialID)
        {
            string sqlquery = @"DELETE FROM materialbom WHERE ProductCode=@1 AND MaterialID = @2 ";
            return _db.Database.ExecuteSqlCommand(sqlquery, 
                new MySqlParameter("1", style_no),
                   new MySqlParameter("2", MaterialID)
                );
        }

        public int DeleteMaterialBomForId(int? MaterialBOMID)
        {
            string sqlquery = @"DELETE FROM materialbom WHERE MaterialBOMID=@1  ";
            return _db.Database.ExecuteSqlCommand(sqlquery,
                new MySqlParameter("1", MaterialBOMID)
                );
        }
        public int DelMaterial(int bid)
        {
            string sqlquery = @"DELETE FROM d_bom_info WHERE bid=@1 ";
            return _db.Database.ExecuteSqlCommand(sqlquery,
                new MySqlParameter("1", bid)
                );
        }
        public int DeleteBomInfo(string style_no)
        {
            string sqlquery = @"DELETE FROM d_bom_info WHERE  FIND_IN_SET(style_no, @1) != 0  ";
            return _db.Database.ExecuteSqlCommand(sqlquery,
                new MySqlParameter("1", style_no)
                );
        }

       

        public int DeleteAaLlMaterialBom(string BOMID)
        {
            string sqlquery = @"DELETE FROM materialbom WHERE  FIND_IN_SET(BOMID, @1) != 0  ";
            return _db.Database.ExecuteSqlCommand(sqlquery,
                new MySqlParameter("1", BOMID)
                );
        }

        public void UpdateBomInfo(d_bom_info item, int? isActive)
        {
            string sqlupdate = @"Update d_bom_info SET style_no = @0,mt_no=@1,need_time=@2,cav=@3,need_m=@4,buocdap=@5,
            isActive=@6,reg_id=@7,chg_dt=@8
            WHERE bid=@9";
            _db.Database.ExecuteSqlCommand(sqlupdate, new MySqlParameter("@0", item.style_no),
                new MySqlParameter("@1", item.mt_no),
                new MySqlParameter("@2", item.need_time), 
                new MySqlParameter("@3", item.cav), 
                new MySqlParameter("@4", item.need_m),
                new MySqlParameter("@5", item.buocdap), 
                new MySqlParameter("@6", isActive),
                new MySqlParameter("@7", item.reg_id),
                new MySqlParameter("@8", item.chg_dt),
                new MySqlParameter("@9", item.bid));
        }

        public void UpdateBomDeTinhHieuSuat(string product)
        {
            string sqlupdate = @"Update d_bom_info SET IsActive=0 WHERE style_no=@1; ";
            _db.Database.ExecuteSqlCommand(sqlupdate, new MySqlParameter("@1", product)
                 );
        }
       
        public int InsertToWmaterialTam(w_material_info_tam item, string _barcode, int send_qty, string date, string time_now)
        {
            string sqlquery = @"CALL insert_lot_tam(@1, @2, @3, @4,@5,@6,@7,@8,@9,@10,@11,@12)";
            return _db.Database.ExecuteSqlCommand(sqlquery, 
                new MySqlParameter("1", item.mt_no),
                new MySqlParameter("2", item.gr_qty),
                new MySqlParameter("3", item.expore_dt),
                new MySqlParameter("4", item.dt_of_receipt),
                new MySqlParameter("5", item.expiry_dt),
                new MySqlParameter("6", item.lot_no),
                new MySqlParameter("7", item.mt_type),
                new MySqlParameter("8", item.reg_id),
                new MySqlParameter("9", _barcode),
                new MySqlParameter("10", send_qty),
                new MySqlParameter("11", date),
                new MySqlParameter("12", time_now)

                );

        }

        public IEnumerable<WMaterialTam> GetListTam(string mt_no,string date, string time_now, int send_qty)
        {
            string sqlquery = @"SELECT wmtid, mt_cd,lot_no,gr_qty,expiry_dt,dt_of_receipt,expore_dt
                             FROM w_material_info_tam 
                             WHERE mt_cd LIKE CONCAT(@1,'-', @2, @3,'%')
                            limit @4 ;";
            return _db.Database.SqlQuery<WMaterialTam>(sqlquery, 
                new MySqlParameter("1", mt_no),
                new MySqlParameter("2", date),
                new MySqlParameter("3", time_now),
                new MySqlParameter("4", send_qty)
                );
        }

        public void UpdateQtyWMaterailTam(w_material_info_tam item, string id)
        {
            string sqlupdate = @"Update w_material_info_tam SET gr_qty= @2 , real_qty = @2, chg_id = @3, chg_dt = @4
                                            WHERE FIND_IN_SET(wmtid, @1) != 0 ";
            _db.Database.ExecuteSqlCommand(sqlupdate,
                new MySqlParameter("1", id),
                new MySqlParameter("2", item.gr_qty),
                new MySqlParameter("3", item.chg_id),
                new MySqlParameter("4", item.chg_dt));
            
        }

        public void UpdateProductDeApply(int bid, string IsApply)
        {
            string sqlupdate = @"Update d_bom_info SET IsApply=@2 WHERE bid=@1";
            _db.Database.ExecuteSqlCommand(sqlupdate, new MySqlParameter("1", bid),
                                                      new MySqlParameter("2", IsApply)
                 );
        }

        public int InsertToMaterialBom(CreateBomMaterialModel item)
        {
            string QuerySQL = @"REPLACE  INTO materialbom
                (ProductCode,MaterialPrarent,MaterialNo,CreateId,CreateDate,ChangeId,ChangeDate)
            VALUES (@1,@2, @3, @4, @5,@6,@7);
            SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(QuerySQL, 
                new MySqlParameter("1", item.ProductCode),
                new MySqlParameter("2", item.MaterialPrarent),
                new MySqlParameter("3", item.MaterialNo), 
                new MySqlParameter("4", item.CreateId),
                new MySqlParameter("5", item.CreateDate),
                new MySqlParameter("6", item.ChangeId),
                new MySqlParameter("7", item.ChangeDate)
                ).FirstOrDefault();
        }

        public IEnumerable<CreateBomMaterialModel> GetListmaterialbomcap3(string style_no, string mt_no)
        {
            string getvalue = @"SELECT *,
                (SELECT mt_nm FROM d_material_info WHERE mt_no = a.MaterialNo) AS materialName
                FROM materialbom a
              
                where a.ProductCode= @1 and a.MaterialPrarent =@2  order by a.id desc";
            return _db.Database.SqlQuery<CreateBomMaterialModel>(getvalue,
                new MySqlParameter("1", style_no),
                new MySqlParameter("2", mt_no)
                );
        }

        public int DelMaterialBom(int Id)
        {
            string sqlquery = @"DELETE FROM materialbom WHERE Id=@1 ";
            return _db.Database.ExecuteSqlCommand(sqlquery,
                new MySqlParameter("1", Id)
                );
        }

        public int InsertToDRoutingInfo(DRoutingInfo item)
        {
            string QuerySQL = @"INSERT INTO d_rounting_info
                (style_no,name,description,level,don_vi_pr,type,item_vcd,reg_dt,reg_id,chg_id,chg_dt,isFinish, process_code)
            VALUES (@1,@2, @3, @4, @5,@6,@7,@8,@9,@10,@11,@12, @13);
            SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(QuerySQL,
                new MySqlParameter("1", item.style_no),
                new MySqlParameter("2", item.name),
                new MySqlParameter("3", item.description),
                new MySqlParameter("4", item.level),
                new MySqlParameter("5", item.don_vi_pr),
                new MySqlParameter("6", item.type),
                new MySqlParameter("7", item.item_vcd),
                new MySqlParameter("8", item.reg_dt),
                new MySqlParameter("9", item.reg_id),
                new MySqlParameter("10", item.chg_id),
                new MySqlParameter("11", item.chg_dt),
                new MySqlParameter("12", item.isFinish),
                new MySqlParameter("13", item.process_code)
                ).FirstOrDefault();
        }
        public void UpdateDRoutingInfo(d_rounting_info item, string description, string isFinish)
        {
            string sqlupdate = @"Update d_rounting_info SET name= @2 , don_vi_pr = @3, chg_id = @4, type = @5,
                                          item_vcd = @6, description = @7 , isFinish = @8
                                            WHERE idr = @1 ";
            _db.Database.ExecuteSqlCommand(sqlupdate,
                new MySqlParameter("1", item.idr),
                new MySqlParameter("2", item.name),
                new MySqlParameter("3", item.don_vi_pr),
                new MySqlParameter("4", item.chg_id),
                new MySqlParameter("5", item.type),
                new MySqlParameter("6", item.item_vcd),
                new MySqlParameter("7", description),
                new MySqlParameter("8", isFinish)
                );
        }

        public void UpdateNVLDeTinhHieuSuat(string product)
        {
            string sqlupdate = @"Update product_material SET isActive='N' WHERE style_no=@1; ";
            _db.Database.ExecuteSqlCommand(sqlupdate, new MySqlParameter("@1", product)
                 );
        }

        public int InsertToProductMaterial(ProductMaterailModel item)
        {
            string QuerySQL = @"INSERT INTO product_material
                (style_no,level,mt_no,need_time,cav,need_m,buocdap,isActive,reg_id,reg_dt,chg_id,chg_dt,name, process_code )
            VALUES (@1,@2, @3, @4, @5, @6, @7, @8, @9, @10,@11,@12,@13, @14);
            SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(QuerySQL,
                new MySqlParameter("@1", item.style_no),
                new MySqlParameter("@2", item.level),
                new MySqlParameter("@3", item.mt_no),
                new MySqlParameter("@4", item.need_time),
                new MySqlParameter("@5", item.cav),
                new MySqlParameter("@6", item.need_m),
                new MySqlParameter("@7", item.buocdap),
                new MySqlParameter("@8", item.isActive),
                new MySqlParameter("@9", item.reg_id),
                new MySqlParameter("@10", item.reg_dt),
                new MySqlParameter("@11", item.chg_id),
                new MySqlParameter("@12", item.chg_dt),
                new MySqlParameter("@13", item.name),
                new MySqlParameter("@14", item.process_code)
                ).FirstOrDefault();
        }

        public ProductMaterailModel GetProductMaterial(int id)
        {
            string getvalue = @"SELECT a.*
                            FROM product_material AS a
                            WHERE a.id = @1";
            return _db.Database.SqlQuery<ProductMaterailModel>(getvalue, new MySqlParameter("1", id)).FirstOrDefault();
        }

        public void UpdateToProductMaterial(ProductMaterailModel item)
        {
            string sqlupdate = @"Update product_material SET mt_no=@1,need_time=@2,cav=@3,need_m=@4,buocdap=@5,
            isActive=@6,reg_id=@7,chg_dt=@8
            WHERE id=@9";
            _db.Database.ExecuteSqlCommand(sqlupdate, 
                new MySqlParameter("@1", item.mt_no),
                new MySqlParameter("@2", item.need_time),
                new MySqlParameter("@3", item.cav),
                new MySqlParameter("@4", item.need_m),
                new MySqlParameter("@5", item.buocdap),
                new MySqlParameter("@6", item.isActive),
                new MySqlParameter("@7", item.reg_id),
                new MySqlParameter("@8", item.chg_dt),
                new MySqlParameter("@9", item.id));
        }

        public int DeleteProductMaterialForId(int? id)
        {
            string sqlquery = @"DELETE FROM product_material WHERE id=@1  ";
            return _db.Database.ExecuteSqlCommand(sqlquery,
                new MySqlParameter("1", id)
                );
        }

        public ProductMaterailModel CheckMaterialExist(string style_no, int level, string mt_no, string process_code)
        {
            string QuerySQL = "SELECT * FROM product_material WHERE style_no = @1 and level = @2 and mt_no = @3 and process_code = @4";
            return _db.Database.SqlQuery<ProductMaterailModel>(QuerySQL,
                new MySqlParameter("1", style_no),
                new MySqlParameter("2", level),
                new MySqlParameter("3", mt_no),
                new MySqlParameter("4", process_code)
                ).FirstOrDefault();
        }

        public int InsertToMaterialChild(ProductMaterialDetailModel item)
        {
            string QuerySQL = @"REPLACE  INTO product_material_detail
                (ProductCode,level,MaterialPrarent,MaterialNo,CreateId,CreateDate,ChangeId,ChangeDate,name,process_code)
            VALUES (@1,@2, @3, @4, @5,@6,@7,@8,@9,@10);
            SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(QuerySQL,
                new MySqlParameter("1", item.ProductCode),
                new MySqlParameter("2", item.level),
                new MySqlParameter("3", item.MaterialPrarent),
                new MySqlParameter("4", item.MaterialNo),
                new MySqlParameter("5", item.CreateId),
                new MySqlParameter("6", item.CreateDate),
                new MySqlParameter("7", item.ChangeId),
                new MySqlParameter("8", item.ChangeDate),
                new MySqlParameter("9", item.name),
                new MySqlParameter("10", item.process_code)
                ).FirstOrDefault();
        }

        public IEnumerable<ProductMaterialDetailModel> GetListmaterialChild(string ProductCode, string name, string MaterialPrarent, string process_code)
        {
            string getvalue = @"SELECT *, (SELECT mt_nm FROM d_material_info WHERE mt_no = a.MaterialNo) AS MaterialName
                FROM product_material_detail a
              
                WHERE ProductCode = @3 and a.name= @1 and a.MaterialPrarent =@2 and a.process_code = @4 order by a.id desc";
                 return _db.Database.SqlQuery<ProductMaterialDetailModel>(getvalue,
                new MySqlParameter("1", name),
                new MySqlParameter("2", MaterialPrarent),
                new MySqlParameter("3", ProductCode),
                new MySqlParameter("4", process_code)
                );
        }

        public int DeleteMaterialChild(int id)
        {
            string sqlquery = @"DELETE FROM product_material_detail WHERE id=@1 ";
            return _db.Database.ExecuteSqlCommand(sqlquery,
                new MySqlParameter("1", id)
                );
        }

        public int DeleteProductMaterialDetail(string ProductCode,int? level, string MaterialPrarent)
        {
            string sqlquery = @"DELETE FROM product_material_detail WHERE ProductCode = @3 and level=@1 and MaterialPrarent = @2 ";
            return _db.Database.ExecuteSqlCommand(sqlquery,
                new MySqlParameter("1", level),
                new MySqlParameter("2", MaterialPrarent),
                new MySqlParameter("3", ProductCode)
                );
        }

        public int InsertToStyleInfo(d_style_info item)
        {
            string QuerySQL = @"REPLACE INTO d_style_info
                (style_no,style_nm,md_cd,ssver,prj_nm,part_nm,standard,cust_rev,order_num,pack_amt,cav,bom_type,tds_no,item_vcd,qc_range_cd,drawingname,use_yn,del_yn,reg_id,chg_id, reg_dt,chg_dt,stamp_code,expiry,expiry_month,loss,Description,productType)
            VALUES (@1,@2, @3, @4, @5,@6,@7,@8,@9,@10,@11,@12,@13,@14,@15,@16,@17,@18,@19,@20,now(),now(),@21,@22,@23,@24,@25,@26);
            SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(QuerySQL,
                new MySqlParameter("1", item.style_no),
                new MySqlParameter("2", item.style_nm),
                new MySqlParameter("3", item.md_cd),
                new MySqlParameter("4", item.ssver),
                  new MySqlParameter("5", item.prj_nm),
                new MySqlParameter("6", item.part_nm),
                new MySqlParameter("7", item.standard),
                new MySqlParameter("8", item.cust_rev),
                new MySqlParameter("9", item.order_num),
                new MySqlParameter("10", item.pack_amt),
                new MySqlParameter("11", item.cav),
                new MySqlParameter("12", item.bom_type),
                new MySqlParameter("13", item.tds_no),
                new MySqlParameter("14", item.item_vcd),
                new MySqlParameter("15", item.qc_range_cd),
                new MySqlParameter("16", item.drawingname),
                new MySqlParameter("17", item.use_yn),
                new MySqlParameter("18", item.del_yn),
                new MySqlParameter("19", item.reg_id),
                new MySqlParameter("20", item.chg_id),
                new MySqlParameter("21", item.stamp_code),
                new MySqlParameter("22", item.expiry),
                new MySqlParameter("23", item.expiry_month),
                new MySqlParameter("24", item.loss),
                new MySqlParameter("25", item.Description),
                new MySqlParameter("26", item.productType)
                ).FirstOrDefault();
        }

        public void UpdateToStyleInfo(d_style_info item)
        {

            string sqlupdate = @"Update d_style_info SET style_nm=@2,md_cd=@3,ssver=@4,prj_nm = @5,part_nm=@6,standard=@7,
                     cust_rev=@8,order_num=@9,pack_amt=@10,cav=@11,bom_type=@12,tds_no=@13,item_vcd=@14,
                        qc_range_cd= @15,drawingname=@16,chg_id=@17,chg_dt = now(),stamp_code=@20,expiry_month=@18,expiry=@19,loss=@21,Description=@22, productType = @23
            WHERE sid=@1";
            _db.Database.ExecuteSqlCommand(sqlupdate,
               new MySqlParameter("1", item.sid),
                new MySqlParameter("2", item.style_nm),
                new MySqlParameter("3", item.md_cd),
                new MySqlParameter("4", item.ssver),
                  new MySqlParameter("5", item.prj_nm),
                new MySqlParameter("6", item.part_nm),
                new MySqlParameter("7", item.standard),
                new MySqlParameter("8", item.cust_rev),
                new MySqlParameter("9", item.order_num),
                new MySqlParameter("10", item.pack_amt),
                new MySqlParameter("11", item.cav),
                new MySqlParameter("12", item.bom_type),
                new MySqlParameter("13", item.tds_no),
                new MySqlParameter("14", item.item_vcd),
                new MySqlParameter("15", item.qc_range_cd),
                new MySqlParameter("16", item.drawingname),
                new MySqlParameter("17", item.chg_id),
                new MySqlParameter("18", item.expiry_month),
                new MySqlParameter("19", item.expiry),
                new MySqlParameter("20", item.stamp_code),
                new MySqlParameter("21", item.loss),
                new MySqlParameter("22", item.Description),
                new MySqlParameter("23", item.productType)
                );
        }

        public w_dl_info CheckDLExist(int id)
        {
            throw new NotImplementedException();
        }

        public void UpdateIsFinish(string product, string process_code)
        {
            string sqlupdate = @"Update d_rounting_info SET isFinish='N' WHERE style_no=@1 and process_code = @2; ";
            _db.Database.ExecuteSqlCommand(sqlupdate, new MySqlParameter("1", product), new MySqlParameter("2", process_code)
                 );
        }

        public int InsertToProductProcess(ProductProcess item)
        {
            string QuerySQL = @"INSERT INTO product_routing
                (style_no,process_code,process_name,description,reg_id,IsApply )
            VALUES (@1,@2, @3, @4, @5,@6);
            SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(QuerySQL,
                new MySqlParameter("@1", item.style_no),
                new MySqlParameter("@2", item.process_code),
                new MySqlParameter("@3", item.process_name),
                new MySqlParameter("@4", item.description),
                new MySqlParameter("@5", item.reg_id),
                new MySqlParameter("6", item.IsApply)
                ).FirstOrDefault();
        }

        public ProductProcess GetProductProcess(int id)
        {
            string getvalue = @"SELECT a.*
                            FROM product_routing AS a
                            WHERE a.id = @1";
            return _db.Database.SqlQuery<ProductProcess>(getvalue, new MySqlParameter("1", id)).FirstOrDefault();
        }

        public void UpdateToProductProcess(ProductProcess item)
        {
            string sqlupdate = @"Update product_routing SET process_name=@1,description=@2,chg_id=@3, IsApply = @5
            WHERE id=@4";
            _db.Database.ExecuteSqlCommand(sqlupdate,
                new MySqlParameter("@1", item.process_name),
                new MySqlParameter("@2", item.description),
                new MySqlParameter("@3", item.chg_id),
                new MySqlParameter("@4", item.id),
                new MySqlParameter("5", item.IsApply)
                );
        }

        public void UpdateProcessToApply(string product)
        {
            string sqlupdate = @"Update product_routing SET IsApply='N' WHERE style_no=@1; ";
            _db.Database.ExecuteSqlCommand(sqlupdate, new MySqlParameter("@1", product)
                 );
        }

        public IEnumerable<DRoutingInfo> CheckLevelProcess(string style_no, string process_code)
        {
            string getvalue = @"SELECT *
                FROM d_rounting_info a
              
                where a.style_no= @1 and a.process_code =@2 ";
            return _db.Database.SqlQuery<DRoutingInfo>(getvalue,
                new MySqlParameter("1", style_no),
                new MySqlParameter("2", process_code)
                );
        }
    }

}


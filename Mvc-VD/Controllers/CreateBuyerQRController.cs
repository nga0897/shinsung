using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Mvc_VD.Classes;
using Mvc_VD.Models;
using Mvc_VD.Models.Language;
using Mvc_VD.Services;
using MySql.Data.MySqlClient;

namespace Mvc_VD.Controllers
{
    public class CreateBuyerQRController : BaseController
    {

        private readonly IEntityService _EntityService;
        private readonly ICreateBuyerQRService _CreateBuyerQRService;
      
        private readonly Entities db;
        #region Override JsonResult

        public CreateBuyerQRController(IEntityService EntityService,
                ICreateBuyerQRService CreateBuyerQRService,
                  IDbFactory DbFactory
                )
        {
            _EntityService = EntityService;
            _CreateBuyerQRService = CreateBuyerQRService;
            db = DbFactory.Init();
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


        #endregion
        public ActionResult BuyerQR()
        {
            return SetLanguage("~/Views/fgwms/CreateBuyerQR/BuyerQR.cshtml");
          
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
        public JsonResult GetProductForBuyer()
        {
            // Get Data from ajax function
            var code = Request["style_no"];
            var code_nm = Request["style_nm"];
            var modecode = Request["md_cd"];
         
            var sql = new StringBuilder();

            sql.Append(" SELECT a.* ")
            .Append("FROM  d_style_info as a ")
            .Append("Where ('" + code + "'='' OR  a.style_no like '%" + code + "%' )")
            .Append(" AND ( a.stamp_code IS NOT NULL  and a.stamp_code <> '' ) ")
            .Append("AND ('" + code_nm + "'='' OR  a.style_nm like '%" + code_nm + "%' )")
            .Append("AND ('" + modecode + "'='' OR  a.md_cd like '%" + modecode + "%' )")
            
            .Append("ORDER BY a.chg_dt desc ");

            return new InitMethods().ConvertDataTableToJsonAndstring(sql.ToString());

        }
        public JsonResult Create(string printedDate)
        {
            string productCode = Request["productCode"] == null ? "" : Request["productCode"].Trim();
            string vendorCode = Request["vendorCode"] == null ? "DZIH" : Request["vendorCode"].Trim();
            string vendorLine = Request["vendorLine"] == null ? "A" : Request["vendorLine"].Trim();
            string labelPrinter = Request["labelPrinter"] == null ? "1" : Request["labelPrinter"].Trim();
            string type = Request["type"] == null ? "N" : Request["type"].Trim();
            string pcn = Request["pcn"] == null ? "0" : Request["pcn"].Trim();
            string machineLine = Request["machineLine"] == null ? "01" : Request["machineLine"].Trim();
            string shift = Request["shift"] == null ? "0" : Request["shift"].Trim();
            string quantity = Request["quantity"] == null ? "0" : Request["quantity"].Trim();
            string quantityPerTray = Request["quantityPerTray"] == null ? "0" : Request["quantityPerTray"].Trim();
            string stampCode = Request["stampCode"] == null ? "0" : Request["stampCode"].Trim();
            string ssver = Request["ssver"] == null ? "" : Request["ssver"].Trim().ToUpper();

            //kiểm tra ngày có phải dạng date không, tránh trường hợp 31-09-2021 không tồn tại nhưng vẫn tạo
          
            DateTime date;
            bool chValidity = DateTime.TryParseExact(
                    printedDate,
                     "yyyy-MM-dd",
                     CultureInfo.InvariantCulture,
                     DateTimeStyles.None,
                     out date);
            if (chValidity == false)
            {
                return Json(new { result = false, message = "Chọn ngày không có thực, vui lòng chọn lại" }, JsonRequestBehavior.AllowGet);
            }
            using (Entities db = new Entities())
            {
                StringBuilder tempBuyerQR = new StringBuilder();
                tempBuyerQR.Append(productCode.Replace("-", ""))
                    .Append(vendorCode)
                    .Append(vendorLine)
                    .Append(labelPrinter)
                    .Append(type)
                    .Append(pcn)
                    .Append(DateFormatByShinsungRule(printedDate));

                string tempQR = tempBuyerQR.ToString();
                try
                {
                    int increament = 0;

                    List<stamp_detail> list = new List<stamp_detail>();

                    for (int i = 0; i < uint.Parse(quantity); i++)
                    {
                        //increament++;

                        StringBuilder countSql2 = new StringBuilder();

                        countSql2.Append($"SELECT SUBSTRING(MAX(a.buyer_qr), LENGTH('{tempQR}')+1, 3) AS bientang, SUBSTRING(MAX(a.buyer_qr), LENGTH('{tempQR}')+4, 2) AS machine_line  FROM stamp_detail a  WHERE a.buyer_qr LIKE '{tempQR}%' AND a.shift = '{shift}' AND a.machine_line = (select max(machine_line)  from stamp_detail st WHERE st.buyer_qr  LIKE '{tempQR}%' AND st.shift = '{shift}' ) ORDER BY a.buyer_qr LIMIT 1; ");
                        List<ListIntModel> listInt1 = new InitMethods().ConvertDataTableToList<ListIntModel>(countSql2);

                        if (listInt1[0].bientang != null && listInt1[0].bientang != "")
                        {
                            if (listInt1[0].bientang == "Z99")
                            {
                                int MachineLineAuto = int.Parse(listInt1[0].machine_line) + 1;
                                machineLine = MachineLineAuto.ToString();
                                if (machineLine.Length < 10)
                                {
                                    machineLine = '0' + machineLine;
                                }
                                increament = 1;
                            }
                            else
                            {
                                var x = listInt1[0].bientang.Substring(0, 1);
                                var y = listInt1[0].bientang.Substring(1, 1);
                                var z = listInt1[0].bientang.Substring(2, 1);
                                string xx;
                                if (x == "Z")
                                {
                                    xx = "35";
                                }
                                else
                                {
                                    xx = ChangeCharacterToNumber(x).ToString();
                                }
                                string yy = ChangeCharacterToNumber(y).ToString();
                                string zz = ChangeCharacterToNumber(z).ToString();
                                string sotang = xx + yy + zz;
                                increament = int.Parse(sotang) +1 ;
                                machineLine = listInt1[0].machine_line;
                            }
                        }
                        else
                        {
                            increament = 1;
                        }
                        string buyerQR = tempQR;
                       
                        stamp_detail detail = new stamp_detail();
                        detail.product_code = productCode;
                        detail.vendor_code = vendorCode;
                        detail.vendor_line = vendorLine;
                        detail.label_printer = labelPrinter;
                        detail.is_sample = type;
                        detail.pcn = pcn;
                        detail.lot_date = printedDate;
                        detail.serial_number = increament.ToString();
                        detail.machine_line = machineLine;
                        detail.shift = shift;
                        detail.standard_qty = int.Parse(quantityPerTray);
                        detail.stamp_code = stampCode;
                        detail.ssver = ssver;
                        if (stampCode != "002")
                        {
                            detail.buyer_qr = String.Concat(tempQR, BuyerQRSerialFormat(increament), machineLine, shift, ProductQuantityFormatForBuyerQR(int.Parse(quantityPerTray))).ToUpper();
                        }
                        else
                        {
                            detail.buyer_qr = String.Concat(tempQR, BuyerQRSerialFormat(increament), machineLine, shift, ProductQuantityFormatForSDV3(int.Parse(quantityPerTray))).ToUpper();
                        }

                      
                       var idStamp =  _CreateBuyerQRService.Insertstampdetail(detail);
                        detail.id = idStamp;
                        list.Add(detail);

                        //db.Entry(detail).State = EntityState.Added;
                        //db.SaveChanges();
                    }
                    //db.SaveChanges();

                    List<BuyerQRModel> returnList = new List<BuyerQRModel>();
                    returnList = GetBuyerQRShow(list);
                    if (returnList != null)
                    {
                        return Json(new { result = true, data = returnList, message = "" }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        return Json(new { result = false, message = "Không thể tạo mã tem." }, JsonRequestBehavior.AllowGet);
                    }

                }
                catch (Exception e)
                {
                    //throw;
                    return Json(new { result = false, message = e.Message}, JsonRequestBehavior.AllowGet);
                }
            }
        }

    
        public int ChangeCharacterToNumber(string number)
        {
            int temp;
            if (int.TryParse(number, out temp))
            {

                return int.Parse(number);
            }

            char c = 'Z';
            char a = char.Parse(number);
            int i = 35;
            do
            {

                c--;
                i--;
            }
            while (c != a);
            return i;


        }
        public List<BuyerQRModel> GetBuyerQRShow(List<stamp_detail> list)
        {
            try
            {
                List<BuyerQRModel> showList = new List<BuyerQRModel>();
                string product_code = "";
                string product_name = "";
                string model = "";
                string stamp_code = "";
                string stamp_name = "";

                using (Entities db = new Entities())
                {
                    if (list != null)
                    {
                        product_code = list.FirstOrDefault().product_code;
                        product_name = db.d_style_info.Where(x => x.style_no == product_code).FirstOrDefault().style_nm;
                        model = db.d_style_info.Where(x => x.style_no == product_code).FirstOrDefault().md_cd;
                        stamp_code = list.FirstOrDefault().stamp_code;
                        stamp_name = db.stamp_master.Where(x => x.stamp_code == stamp_code).FirstOrDefault().stamp_name;
                        //int id = 0;

                        foreach (var item in list)
                        {
                            //id++;
                            BuyerQRModel buyerQRModel = new BuyerQRModel();
                            buyerQRModel.id = item.id;
                            buyerQRModel.buyer_qr = item.buyer_qr;
                            buyerQRModel.stamp_code = item.stamp_code;
                            buyerQRModel.product_code = product_code;
                            buyerQRModel.product_name = product_name;
                            buyerQRModel.lotNo = string.Concat(item.lot_date.Replace("-", ""));
                            buyerQRModel.model = model;
                            buyerQRModel.quantity = item.standard_qty;
                            buyerQRModel.stamp_name = stamp_name;
                            showList.Add(buyerQRModel);
                        }
                        return showList;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            catch (Exception)
            {
                return null;
            }

        }
        public JsonResult GetAllStamp()
        {
            return Json(new InitMethods().GetAllStamp(), JsonRequestBehavior.AllowGet);
        }

        public string DateFormatByShinsungRule(string input)
        {
            input = input.Replace("-", "");
            string year = ChangeNumberToCharacter(int.Parse(input.Substring(2, 2))).ToString();
            string month = ChangeNumberToCharacter(int.Parse(input.Substring(4, 2))).ToString();
            string date = ChangeNumberToCharacter(int.Parse(input.Substring(6, 2))).ToString();
            StringBuilder result = new StringBuilder();
            result.Append(year);
            result.Append(month);
            result.Append(date);
            return result.ToString();
        }

        public char ChangeNumberToCharacter(int number)
        {
            string temp = number.ToString();
            char result = 'A';
            int subtraction = 0;
            if (number < 10)
            {
                return char.Parse(temp);
            }
            else
            {
                subtraction = number - 10;
                for (int i = 0; i < subtraction; i++)
                {
                    result++;
                }
            }
            return result;
        }

        public string BuyerQRSerialFormat(int number)
        {
            return ProductQuantityFormatForBuyerQR(number);
        }

        public string ProductQuantityFormatForBuyerQR(int quantity)
        {
            string str = quantity.ToString();
            int length = str.Length;

            if (length % 2 == 0)
            {
                if (length == 4)
                {
                    string fisrtTwoCharacters = str.Substring(0, 2);
                    string lastTwoCharacter = str.Substring(2, 2);

                    char a = ChangeNumberToCharacter(int.Parse(fisrtTwoCharacters));
                    StringBuilder result = new StringBuilder();
                    result.Append(a.ToString())
                        .Append(lastTwoCharacter);
                    str = result.ToString();
                }
                else
                {
                    str = string.Concat("0", str);
                }
            }
            else
            {
                if (length == 5)
                {
                    string fisrtTwoCharacters = str.Substring(0, 2);
                    string secondTwoCharacter = str.Substring(2, 2);
                    string lastCharacter = str.Substring(4, 1);

                    char a = ChangeNumberToCharacter(int.Parse(fisrtTwoCharacters));
                    char b = ChangeNumberToCharacter(int.Parse(secondTwoCharacter));
                    StringBuilder result = new StringBuilder();
                    result.Append(a.ToString())
                        .Append(b.ToString())
                        .Append(lastCharacter);
                    str = result.ToString();
                }
                if (length == 1)
                {
                    str = string.Concat("00", str);
                }
            }
            return str;
        }

        public string ProductQuantityFormatForSDV3(int quantity)
        {
            StringBuilder str = new StringBuilder();
            int x = 0;
            int y = 0;
            int z = 0;

            x = (int)Math.Floor((float)quantity / (32 * 32));
            y = (int)Math.Floor((float)(quantity - x * 32 * 32) / 32);
            z = (int)Math.Floor((float)(quantity - (x * 32 * 32) - (y * 32)));

            return str.Append(ChangeNumberToCharacter(x).ToString()).Append(ChangeNumberToCharacter(y).ToString()).Append(ChangeNumberToCharacter(z).ToString()).ToString();
        }

        public string ProductQuantityFormatForBoxQR(int quantity)
        {
            string str = quantity.ToString();
            int length = str.Length;
            if (length == 4)
            {
                str = "0" + str;
            }
            if (length == 3)
            {
                str = "00" + str;
            }
            if (length == 2)
            {
                str = "000" + str;
            }
            if (length == 1)
            {
                str = "0000" + str;
            }

            return str;
        }
        #region PrintQR
        public ActionResult PrintQR()
        {
            string[] keys = Request.Form.AllKeys;
            ViewData["Message"] = Request.Form[keys[0]];
        
            return View("~/Views/fgwms/CreateBuyerQR/PrintQR.cshtml");

        }
        public ActionResult QRbarcodeInfo(string id)
        {

            using (Entities db = new Entities())
            {
                try
                {
                    var multiIDs = id.TrimStart('[').TrimEnd(']').Split(',');
                    var row_data = new List<BuyerQRModel>();


                    for (int i = 0; i < multiIDs.Length; i++)
                    {
                        var id2 = int.Parse(multiIDs[i]);


                        //     var data = (from a in db.stamp_detail
                        //                 join b in db.stamp_master on a.stamp_code equals b.stamp_code
                        //                 join c in db.d_style_info on a.product_code equals c.style_no

                        //                 where a.id.Equals(id2)
                        //                 select new
                        //                 {
                        //                     id = a.id,
                        //                     buyer_qr = a.buyer_qr,
                        //                     stamp_code = a.stamp_code,
                        //                     product_code = a.product_code,
                        //                     md_cd = c.md_cd,
                        //                     part_nm = c.part_nm,
                        //                     standard_qty = a.standard_qty,
                        //                     lot_date = a.lot_date,
                        //                     expiry_month = c.expiry_month,
                        //                     expiry = c.expiry,
                        //                     ssver = c.ssver,
                        //                     vendor_code = a.vendor_code
                        //                 }
                        //).FirstOrDefault();

                        var data = _CreateBuyerQRService.GetStamp(id2);
                        var itemBuyer = new BuyerQRModel();
                        //add
                        itemBuyer.id = data.id;
                        itemBuyer.model = data.model;
                        itemBuyer.buyer_qr = data.buyer_qr;
                        itemBuyer.stamp_code = data.stamp_code;
                        itemBuyer.vendor_line = data.vendor_line == null ? "" : "(" + data.vendor_line +")";
                        itemBuyer.part_name = data.part_name;
                        itemBuyer.product_code = data.product_code;
                        itemBuyer.quantity = data.quantity;
                        itemBuyer.nhietdobaoquan = data.nhietdobaoquan;
                        itemBuyer.lotNo = string.Concat(data.lotNo.Replace("-", ""));
                        if (!string.IsNullOrEmpty(data.lotNo))
                        {
                            var ymd = string.Concat(data.lotNo.Replace("-", ""));
                            string y = ymd.Substring(0, 4);
                            string m = ymd.Substring(4, 2);
                            string d = ymd.Substring(6, 2);

                            itemBuyer.nsx = d + "/" + m + "/" + y;
                        }
                        else
                        {
                            itemBuyer.nsx = "";
                        }

                        //itemBuyer.ssver = (data.ssver == "" || data.ssver == null) ? "" :  "(" + data.ssver + ")";
                        itemBuyer.ssver = (data.ssver == "" || data.ssver == null) ? "" :   data.ssver ;
                        itemBuyer.supplier = data.vendor_code == null ? "" :  data.vendor_code;
                        string hsd = "";
                        //if (data.stamp_code == "001")
                         if (data.expiry_month == "0")
                        {
                            hsd = data.hsd;
                        }
                        else
                        {

                            string s = data.lotNo;

                            DateTime dt = DateTime.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                            if (!string.IsNullOrEmpty(data.expiry_month))
                            {
                                hsd = dt.AddMonths(int.Parse(data.expiry_month)).ToString("dd/MM/yyyy");
                            }
                        }

                        itemBuyer.hsd = hsd;
                        //add view_qc_Model
                        row_data.Add(itemBuyer);

                    }
                    return Json(new { result = true, row_data }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception e)
                {
                    return Json(new { result = false, message = e.Message }, JsonRequestBehavior.AllowGet);
                }

            }


        }
        #endregion
      
    }
}
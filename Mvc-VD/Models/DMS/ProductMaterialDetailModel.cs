using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Mvc_VD.Models
{
    public class ProductMaterialDetailModel
    {
        [Required]
 
        public int id { get; set; }
        public string ProductCode { get; set; }
        public string process_code { get; set; }
        public int level { get; set; }
        public string name { get; set; }
        public string MaterialPrarent { get; set; }
        public string MaterialNo { get; set; }
        public string MaterialName { get; set; }
        public string CreateId { get; set; }
        public string ChangeId { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? ChangeDate { get; set; }
        public string[] ListMaterial { get; set; }
    }
}
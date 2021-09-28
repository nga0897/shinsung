using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace Mvc_VD.Data
{
    public class ApplicationDBText : DbContext
    {
        public ApplicationDBText():base("name=SqlConnection")
        {
                
        }

    }
}
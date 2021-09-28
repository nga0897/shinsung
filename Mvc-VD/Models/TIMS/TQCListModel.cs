using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mvc_VD.Models.TIMS
{
    public class TQCListModel
    {
        public DateTime Ngay { get; set; }
        public CaNgayModel CaNgay  { get; set; }
        public CaDemModel CaDem  { get; set; }
    }
}
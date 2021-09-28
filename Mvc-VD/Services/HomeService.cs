using Mvc_VD.Models;
using Mvc_VD.Models.HomeModel;
using Mvc_VD.Models.Language;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mvc_VD.Services
{
    public interface IHomeService
    {
        VersionApp GetVersionApp(string name_app);
        List<Language> GetLanguage(string language, string router);
    }
    public class HomeService: IHomeService
    {
        private Entities _db;
        public HomeService(IDbFactory dbFactory)
        {
            _db = dbFactory.Init();
        }
        public VersionApp GetVersionApp(string name_app)
        {
            string sqlquery = @"SELECT id_app,type,name_file,version,Date_format(chg_dt, '%Y-%m-%d %H:%i:%s') AS chg_dt FROM version_app
            WHERE type=@1
            ORDER BY version DESC
            LIMIT 1";
            return _db.Database.SqlQuery<VersionApp>(sqlquery, new MySqlParameter("@1", name_app)).FirstOrDefault();
        }
        public List<Language> GetLanguage(string language, string router)
        {
            if (string.IsNullOrEmpty(language))
            {
                language = "en";
            }
            string sqlQuerry = string.Format(@"SELECT keyname,{0} FROM language WHERE router='{1}' or router='public'", language, router);
            return _db.Database.SqlQuery<Language>(sqlQuerry).ToList<Language>();
        }
    }
}
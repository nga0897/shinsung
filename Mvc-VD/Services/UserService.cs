using Mvc_VD.Classes;
using Mvc_VD.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mvc_VD.Services
{
    public interface IUserService
    {
        string CheckLoginUser(string username, string password);
        string GetAuthData(string userid);

        BuyerLogin GetRoleFromAuthData(string AuthData);

        IEnumerable<menu> GetListMenuByAuthData(string AuthData, string language);
    }
    public class UserService : IUserService
    {
        private Entities _db;
        public UserService(IDbFactory dbFactory)
        {
            _db = dbFactory.Init();
        }
        public string CheckLoginUser(string username, string password)
        {
            try
            {
                string sqlquery = @"SELECT userid FROM mb_info WHERE userid=@1 and upw=@2";
                string result = _db.Database.SqlQuery<string>(sqlquery, new MySqlParameter("1", username), new MySqlParameter("2", password)).FirstOrDefault();
                return result;
            }
            catch(Exception e)
            {
                return e.Message;
            }
            

        }
        public string GetAuthData(string userid)
        {
            string sqlquery = @"SELECT at_cd FROM mb_author_info WHERE userid=@1";
            string result = _db.Database.SqlQuery<string>(sqlquery, new MySqlParameter("1", userid)).FirstOrDefault();
            return result;
        }

        //public IEnumerable<menu> GetListMenuByAuthData(string AuthData)
        //{
        //    string sqlquery = @"CALL Menu_Author(@1)";
        //    return _db.Database.SqlQuery<menu>(sqlquery, new MySqlParameter("1", AuthData));
        //}
        public IEnumerable<menu> GetListMenuByAuthData(string AuthData, string language)
        {
            //string sqlquery = @"CALL Menu_Author(@1)";
            //return _db.Database.SqlQuery<menu>(sqlquery, new MySqlParameter("1", AuthData));
            string sqlQuery = string.Format(@"SET sql_mode = '';SET @@sql_mode = '';
            select b.mn_cd,b.name{1} as mn_nm,b.up_mn_cd,b.url_link 
            from author_menu_info as a 
            join menu_info as b on concat(substring(a.mn_cd,1,3),'000000000') = b.mn_cd 
            where a.at_cd='{0}' AND b.use_yn='Y'
            group by  substring(a.mn_cd,1,3) 
            UNION 
            select b.mn_cd,b.name{1} as mn_nm,b.up_mn_cd,b.url_link 
            from author_menu_info as a 
            join menu_info as b on concat(substring(a.mn_cd,1,6),'000000') = b.mn_cd 
            where a.at_cd='{0}' AND b.use_yn='Y'
            group by b.mn_cd 
            UNION 
            SELECT  b.mn_cd,b.name{1} as mn_nm,b.up_mn_cd,b.url_link 
            from author_menu_info AS a 
            join menu_info as b on a.mn_cd = b.mn_cd 
            where a.at_cd='{0}' AND b.use_yn='Y'  ORDER BY mn_cd", AuthData, language);
            return _db.Database.SqlQuery<menu>(sqlQuery);
        }

        public BuyerLogin GetRoleFromAuthData(string AuthData)
        {
            string sqlquery = @"SELECT role,at_nm FROM author_info WHERE at_cd=@1";
            return _db.Database.SqlQuery<BuyerLogin>(sqlquery, new MySqlParameter("1", AuthData)).FirstOrDefault();
        }
    }
}
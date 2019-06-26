using CRMC.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMC.Server
{
    public static class DatabaseHelper
    {
        private static Db db = new Db(Config.Instance.DbConnectionString);

        public static User Login(string userName,string passwordMd5)
        {
            User user = db.User_Login.FirstOrDefault(p => p.Name == userName);
            if(user==null)
            {
                throw new Exception("用户名不存在");
            }

            if (user.Password != passwordMd5)
            {
                throw new Exception("密码错误");
            }
            return user;
        }
    }
}

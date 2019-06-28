using CRMC.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMC.Client
{
    public class Config : FzLib.DataStorage.Serialization.JsonSerializationBase
    {
        private static Config instance;
        public static Config Instance
        {
            get
            {
                if(instance==null)
                {
                    instance = TryOpenOrCreate<Config>("ClientConfig.json");
                }
                return instance;
            }
        }
        //public string ServerIP { get; set; } = "192.168.2.234";//127.0.0.1";
        //public string ServerIP { get; set; } = "47.101.216.232";
        public string ServerIP { get; set; } = "127.0.0.1";
        //public int ServerPort { get; set; } = 8008;
        public int ServerPort { get; set; } = 8009;

        public string UserName { get; set; } = "1";
        public string UserPassword { get; set; } = "000000";

        public static User CurrentUser;
    }
}

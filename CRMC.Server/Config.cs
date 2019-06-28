using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMC.Server
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
                    instance = TryOpenOrCreate<Config>("ServerConfig.json");
                }
                return instance;
            }
        }
        //public string DbConnectionString { get; set; } = @"data source=FZ-LAPTOP\SQLEXPRESS;initial catalog=CRMC;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework";//@"Data Source=FZ-LAPTOP\SQLEXPRESS;Initial Catalog=SCMS;Integrated Security=True";
        public string DbConnectionString { get; set; } = @"Data Source=FM-PC\SQLEXPRESS;Initial Catalog=CRMC;Integrated Security=True";
        public string DeviceIP { get; set; } = "127.0.0.1";
        //public string DeviceIP { get; set; } = "192.168.2.234";
        public int Port { get; set; } = 8009;
    }
}

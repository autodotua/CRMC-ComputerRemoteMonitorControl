using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMC.Common.Model
{
    [Serializable]
    public class ClientInfo
    {
        [JsonIgnore]
        [NonSerialized]
        private Telnet telnet;

        public ClientInfo()
        {
            ID = Guid.NewGuid();
        }

        public Guid ID { get; set; }
        public string Name { get; set; }
        public Telnet Telnet { get => telnet; set => telnet = value; }
        public User User { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }
        public DateTime OnlineTime { get; set; }
    }
}

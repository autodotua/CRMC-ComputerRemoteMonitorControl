using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMC.Common.Model
{
    [Serializable]
   public class LoginFeedback
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public ClientInfo Client { get; set; }
        public User User { get; set; }
    }
}

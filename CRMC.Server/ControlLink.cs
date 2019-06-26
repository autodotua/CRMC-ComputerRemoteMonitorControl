using CRMC.Common;
using CRMC.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMC.Server
{
   public class ControlLink
    {
        public ControlLink(ControlType type,ClientInfo control, ClientInfo controlled)
        {
            Control = control;
            Controlled = controlled;
            ControlType = type;
        }
        public Guid Id { get; } = Guid.NewGuid();
        public ClientInfo Control { get; private set; }
        public ClientInfo Controlled { get; private set; }
        public ControlType ControlType { get; set; }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMC.Common.Model
{
    [Serializable]
    public class WMIClassInfo
    {
        public string Namespace { get; set; }
        public string Class { get; set; }
    }

    [Serializable]
    public class WMIPropertyInfo
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    [Serializable]
    public class WMIObjectCollection:List<WMIPropertyCollection>
    {
        public string Namespace { get; set; }
        public string Class { get; set; }

    }

    [Serializable]
    public class WMIPropertyCollection:List<WMIPropertyInfo>
    {
        public int Index { get; set; }
        public string Name { get; set; }
    }
}

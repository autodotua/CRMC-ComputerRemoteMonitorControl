using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMC.Common.Model
{
    [Serializable]
    public class CommandBody
    {
        public CommandBody(ApiCommand command, Guid aId=default,Guid bId=default, object data=null)
        {
            Command = command;
            AId = aId;
            BId = bId;
            Data = data;
        }
     public  ApiCommand Command { get;private set; }

        public Guid AId { get; private set; }
        public Guid BId { get; private set; }
        public object Data { get; private set; }

        public byte[] DataToByteArray()
        {
            if(Data is string)
            {
                return Encoding.UTF8.GetBytes(Data as string);
            }
            throw new Exception("不支持的类型");
        }
    }
}

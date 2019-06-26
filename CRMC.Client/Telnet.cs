using CRMC.Client.Controlled;
using CRMC.Common;
using CRMC.Common.Model;
using FzLib.Basic.Collection;
using FzLib.Control.Dialog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static CRMC.Common.ApiCommand;
using Client = CRMC.Common.Model.ClientInfo;

namespace CRMC.Client
{
    public class Telnet : Common.Telnet
    {
        private Telnet() : base(Config.Instance.ServerIP, Config.Instance.ServerPort, true)
        {
            DataReceived += async (s, e) =>
             {
                 var command = (ApiCommand)e.Content.Command;
                 Debug.WriteLine("客户端接收到：" + command);

                 
                 switch (command)
                 {
                    //以下为通用
                    case S_ClientsUpdate:
                         ClientInfo[] clients = e.Content.Data as ClientInfo[];
                         ClientsUpdate?.Invoke(this, new DataReceivedEventArgs(e.Content));
                         break;
                     case S_LoginFeedback:
                         LoginFeedback feedback = e.Content.Data as LoginFeedback;
                         LoginFeedback?.Invoke(this, new DataReceivedEventArgs(e.Content));
                         break;
                     case S_NoSuchClient:
                         NoSuchClient?.Invoke(this, new DataReceivedEventArgs(e.Content));
                         break;

                    //以下为控制端
                    case Screen_NewScreen:
                         ReceivedNewImage?.Invoke(this, new DataReceivedEventArgs(e.Content));
                         break;

                     case WMI_Namespace:
                         WMINamespacesReceived?.Invoke(this, new DataReceivedEventArgs(e.Content));
                         break;
                     case WMI_Classes:
                         WMIClassesReceived?.Invoke(this, new DataReceivedEventArgs(e.Content));
                         break;
                     case WMI_Props:
                         WMIPropsReceived?.Invoke(this, new DataReceivedEventArgs(e.Content));
                         break;

                    //以下为被控端
                    case Screen_AskForStartScreen:
                         await ScreenHelper.StartSendScreen();
                         break;
                     case Screen_AskForStopScreen:
                          ScreenHelper.StopSendScreen();
                         break;
                     case Screen_AskForNextScreen:
                         SendNextScreen?.Invoke(this, new DataReceivedEventArgs(e.Content));
                         break;
                     case WMI_AskForNamespaces:
                             WMIHelper.SendNamespaces(e.Content.AId);
                         break;
                     case WMI_AskForClasses:
                             WMIHelper.SendWMIClasses(e.Content.Data as string,e.Content.AId);
                         break;
                     case WMI_AskForProps:
                         WMIHelper.SendProperties(e.Content.Data as WMIClassInfo,e.Content.AId);
                         break;

                 }
             };
        }

        public event EventHandler<DataReceivedEventArgs> ReceivedNewImage;
        public event EventHandler<DataReceivedEventArgs> SendNextScreen;
        public event EventHandler<DataReceivedEventArgs> LoginFeedback;
        public event EventHandler<DataReceivedEventArgs> ClientsUpdate;
        public event EventHandler<DataReceivedEventArgs> NoSuchClient;
        public event EventHandler<DataReceivedEventArgs> WMINamespacesReceived;
        public event EventHandler<DataReceivedEventArgs> WMIClassesReceived;
        public event EventHandler<DataReceivedEventArgs> WMIPropsReceived;

        public static Telnet Instance { get; } = new Telnet();
        public bool CanSendNextScreen { get; private set; } = true;
    }

    public class TelnetBase
    {
        /// <summary>
        /// socket对象
        /// </summary>
        public static Socket ListenerSocket { get; private set; }

        public static byte[] SocketEnd = { 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, };        /// <summary>
        //private static Thread thread;
        public static IPAddress IP { get; private set; }
      
        public Socket ClientSocket { get; protected set; }
        protected TelnetBase(string ip, int port, bool receive)
        {
            IP = IPAddress.Parse(ip);

            ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ClientSocket.Connect(new IPEndPoint(IP, port)); //配置服务器IP与端口 
            if (receive)
            {
                Task.Run(() => Receive());
            }
        }
        protected TelnetBase()
        {
        }

        /// <summary>  
        /// 接收消息线程方法
        /// </summary>  
        /// <param name="clientSocket"></param>  
        protected void Receive()
        {
            int bufferLength = 1024 * 1024 * 16;
            byte[] buffer = new byte[bufferLength];
            while (true)
            {
                int length = 0;
                int totalLength = 0;
                byte[] received = null;
                using (MemoryStream stream = new MemoryStream())
                {
                    try
                    {
                        Stopwatch sw = Stopwatch.StartNew();
                        while (true)
                        {
                            try
                            {
                                length = ClientSocket.Receive(buffer);
                            }
                            catch (ObjectDisposedException)
                            {
                                return;
                            }
                            stream.Write(buffer, 0, length);
                            totalLength += length;
                            bool end = true;
                            if (length < SocketEnd.Length)
                            {
                                continue;
                            }
                            for (int i = length - SocketEnd.Length, j = 0; i < length; i++, j++)
                            {
                                if (buffer[i] != SocketEnd[j])
                                {
                                    end = false;
                                    break;
                                }
                            }
                            if (end)
                            {
                                break;
                            }
                        }
                    }
                    catch (SocketException ex)
                    {
                        if (ex.SocketErrorCode != SocketError.TimedOut)
                        {
                            SocketClosedByAccident?.Invoke(this, new SocketClosedByAccidentEventArgs(ex));
                            return;
                        }
                    }
                    stream.Flush();
                    received = stream.ToArray();
                }
            _a:
                byte command = received[0];
                byte[] data = new byte[totalLength - 1 - SocketEnd.Length];
                if (data.Length > 0)
                {
                    Array.Copy(received.ToArray(), 1, data, 0, totalLength - 1 - SocketEnd.Length);
                }
                DataReceived?.Invoke(this, new DataReceivedEventArgs(command, data));
            }
        }

        public void Send(ApiCommand command)
        {
            Send((byte)command, new byte[0]);
        }
        public void Send(byte command)
        {
            Send(command, new byte[0]);
        }
        public void Send(byte command, object obj)
        {
            Send(command, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj)));
        }
        public void Send(ApiCommand command, object obj)
        {
            Send((byte)command, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj)));
        }
        public void Send(ApiCommand command, byte[] data)
        {
            Send((byte)command, data);
        }
        public void Send(byte command, byte[] data)
        {
            Debug.Assert(data != null);
            byte[] buffer = new byte[data.Length + 1 + SocketEnd.Length];
            if (data.Length > 0)
            {
                Array.Copy(data, 0, buffer, 1, data.Length);
            }
            Array.Copy(SocketEnd, 0, buffer, 1 + data.Length, SocketEnd.Length);
            buffer[0] = command;

            ClientSocket.Send(buffer);
            //Debug.Assert(data != null);
            //ClientSocket.Send(new byte[]{    command});
            ////byte[] buffer = new byte[data.Length + 1 ];
            ////if (data.Length > 0)
            ////{
            ////    Array.Copy(data, 0, buffer, 1, data.Length);
            ////}
            ////buffer[0] = command;

            //ClientSocket.Send(data);
            //ClientSocket.Send(SocketEnd);

        }

        /// <summary>
        /// 服务是否正在关闭
        /// </summary>
        private static bool closing = false;
        private object stream;

        /// <summary>
        /// 关闭Socket
        /// </summary>
        public static void Close()
        {

            closing = true;
            ListenerSocket.Close();
            ListenerSocket.Dispose();
        }
        public event EventHandler<SocketClosedByAccidentEventArgs> SocketClosedByAccident;
        public class SocketClosedByAccidentEventArgs : EventArgs
        {
            public SocketClosedByAccidentEventArgs(SocketException ex)
            {
                Exception = ex;
            }
            public SocketException Exception { get; private set; }
        }
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public class DataReceivedEventArgs : EventArgs
        {
            public DataReceivedEventArgs(byte command, byte[] datas)
            {
                Command = command;
                Data = datas;
            }

            public byte Command { get; private set; }
            public byte[] Data { get; private set; }

            public T GetObjectAsJson<T>()
            {
                return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(Data));
            }
        }
        public class DataReceivedEventArgs<T> : EventArgs
        {
            public DataReceivedEventArgs(byte command, T datas)
            {
                Command = command;
                Data = datas;
            }

            public byte Command { get; private set; }
            public T Data { get; private set; }


        }



        public static event EventHandler<ClientLinkedEventArgs> ClientLinked;
        public class ClientLinkedEventArgs : EventArgs
        {
            public ClientLinkedEventArgs(Telnet instance)
            {
                Instance = instance;
            }

            public Telnet Instance { get; private set; }
        }

        public enum Role
        {
            Client,
            Server
        }
    }

}

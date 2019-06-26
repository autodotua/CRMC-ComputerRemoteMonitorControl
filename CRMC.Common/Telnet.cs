using CRMC.Common.Model;
using FzLib.Basic.Collection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CRMC.Common
{
    public class Telnet
    {
        /// <summary>
        /// socket对象
        /// </summary>
        public static Socket ListenerSocket { get; private set; }

        public static byte[] SocketEnd = { 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, };        /// <summary>
        //private static Thread thread;
        public static IPAddress IP { get; private set; }
        /// <summary>
        /// 打开监听线程
        /// </summary>
        public static Task StartListening(string ip, int port)
        {
            IP = IPAddress.Parse(ip);
            ListenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ListenerSocket.Bind(new IPEndPoint(IP, port));
            ListenerSocket.Listen(100);//设定最多100个排队连接请求   
            return Task.Run(() =>
            {
                {
                    while (true)
                    {
                        Socket clientSocket = null;
                        try
                        {
                            clientSocket = ListenerSocket.Accept();
                            Telnet t = new Telnet();
                            t.ClientSocket = clientSocket;
                            Thread receiveThread = new Thread(t.Receive);
                            receiveThread.Start();
                            ClientLinked?.Invoke(t, new ClientLinkedEventArgs(t));
                        }
                        catch
                        {

                        }
                        if (closing)
                        {
                            return;
                        }
                    }
                }
            });
        }
        public static Task StartListening<T>(string ip, int port, Func<T> getNew) where T : Telnet
        {
            IP = IPAddress.Parse(ip);
            ListenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ListenerSocket.Bind(new IPEndPoint(IP, port));
            ListenerSocket.Listen(100);//设定最多100个排队连接请求   
            return Task.Run(() =>
            {
                {
                    while (true)
                    {
                        Socket clientSocket = null;
                        try
                        {
                            clientSocket = ListenerSocket.Accept();
                            T t = getNew();
                            t.ClientSocket = clientSocket;
                            //Task.Run(() =>
                            //{
                            //    while (true)
                            //    {
                            //        Debug.WriteLine(clientSocket.Connected);
                            //        t.Send(100);
                            //        Task.Delay(1000).Wait();
                            //    }
                            //});
                            Thread receiveThread = new Thread(t.Receive);
                            receiveThread.Start();
                            ClientLinked?.Invoke(t, new ClientLinkedEventArgs(t));
                        }
                        catch
                        {

                        }
                        if (closing)
                        {
                            return;
                        }
                    }
                }
            });
        }

        public Socket ClientSocket { get; protected set; }
        protected Telnet(string ip, int port, bool receive)
        {
            IP = IPAddress.Parse(ip);

            ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ClientSocket.Connect(new IPEndPoint(IP, port)); //配置服务器IP与端口 
            if (receive)
            {
                Task.Run(() => Receive());
            }
        }
        protected Telnet()
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
                CommandContent content = null;
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
                    //received = stream.ToArray();
                    //stream.Seek(0, SeekOrigin.Begin);
                    //received = new byte[totalLength - SocketEnd.Length];
                    //stream.Read(received, 0, totalLength - SocketEnd.Length);
                    stream.SetLength(totalLength - SocketEnd.Length);
                    stream.Seek(0, SeekOrigin.Begin);
                    content = Deserialize(stream);
                }
                //byte command = received[0];
                //byte[] data = new byte[totalLength - 1 - SocketEnd.Length];
                //if (data.Length > 0)
                //{
                //    Array.Copy(received.ToArray(), 1, data, 0, totalLength - 1 - SocketEnd.Length);
                //}
                DataReceived?.Invoke(this, new DataReceivedEventArgs(content));
            }
        }

        //public void Send(ApiCommand command)
        //{
        //    Send((byte)command, new byte[0]);
        //}
        //public void Send(byte command)
        //{
        //    Send(command, new byte[0]);
        //}
        //public void Send(byte command, object obj)
        //{
        //    Send(command, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj)));
        //}
        //public void Send(ApiCommand command, object obj)
        //{
        //    Send((byte)command, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj)));
        //}
        //public void Send(ApiCommand command, byte[] data)
        //{
        //    Send((byte)command, data);
        //}


        public void Send(CommandContent content)
        {
            Send(Serialize(content));

        }
        private void Send(byte[] data)
        {
            Debug.Assert(data != null);
            Debug.Assert(data.Length > 0);

            byte[] buffer = new byte[data.Length /*+ 1 */+ SocketEnd.Length];
            //if (data.Length > 0)
            //{
            //    Array.Copy(data, 0, buffer, 1, data.Length);

            //}

            Array.Copy(data, buffer, data.Length);

            Array.Copy(SocketEnd, 0, buffer,  data.Length, SocketEnd.Length);
            //buffer[0] = command;

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

        private byte[] Serialize(CommandContent obj)
        {

            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();

                try
                {
                    formatter.Serialize(ms, obj);
                }
                catch (SerializationException e)
                {
                    throw;
                }

                return ms.ToArray();
            }
        }


        static CommandContent Deserialize(MemoryStream ms)
        {
            CommandContent content = null;

            try
            {
                BinaryFormatter formatter = new BinaryFormatter();

                content = (CommandContent)formatter.Deserialize(ms);
            }
            catch (Exception e)
            {
                throw;
            }
            return content;
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
            public DataReceivedEventArgs(CommandContent datas)
            {
                //Command = command;
                Content = datas;
            }

            //public byte Command { get; private set; }
            public CommandContent Content { get; private set; }



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

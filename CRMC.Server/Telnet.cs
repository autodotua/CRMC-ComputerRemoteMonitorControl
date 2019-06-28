using CRMC.Common;
using CRMC.Common.Model;
using FzLib.Basic.Collection;
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
using static CRMC.Common.ControlType;

namespace CRMC.Server
{
    public class Telnet : Common.Telnet
    {
        private Telnet() : base()
        {

        }
        public ClientInfo Client { get; private set; }
        public static List<ClientInfo> Clients { get; } = new List<ClientInfo>();
        public static List<ControlLink> Links { get; } = new List<ControlLink>();
        public static void Start()
        {
            ClientLinked += (p1, p2) =>
            {
                var clientNet = p2.Instance as Telnet;

                clientNet.DataReceived += clientNet.ClientDataReceived;
                clientNet.SocketClosedByAccident += ClientSocketClosedByAccident;

                //p2.Instance.DataReceived += TelnetDataReceived;
                //Debug.WriteLine("增加了一个");
            };
            StartListening(Config.Instance.DeviceIP, Config.Instance.Port, () => new Telnet());

        }

        private void ClientDataReceived(object sender, DataReceivedEventArgs e)
        {
            var command = e.Content.Command;
            var body = e.Content;
            var data = e.Content.Data;
            Debug.WriteLine("服务端接收到：" + command);
            switch (command)
            {
                case C_Login:

                    ClientInfo client = data as ClientInfo;

                    try
                    {
                        User user = DatabaseHelper.Login(client.User.Name, client.User.Password);
                        client.IP = (ClientSocket.RemoteEndPoint as IPEndPoint).Address.ToString();
                        client.Port = (ClientSocket.RemoteEndPoint as IPEndPoint).Port;
                        client.Telnet = this;
                        client.OnlineTime = DateTime.Now;
                        Client = client;
                        Send(new CommandBody(S_LoginFeedback, default, default, new LoginFeedback() { Success = true, Message = "登录成功", User = user, Client = client }));
                        Clients.Add(client);
                        SendClientListToAllClients();
                    }
                    catch (Exception ex)
                    {
                        Send(new CommandBody(S_LoginFeedback, default, default, new LoginFeedback() { Success = false, Message = "登录失败：" + ex.Message }));
                    }
                    //client.Telnet = new Telnet() { ClientSocket = p2.Instance.ClientSocket };
                    //Clients.Add(client);
                    break;
                case C_Exit:
                    ClientExit();
                    break;

                case Screen_NewScreen:
                    foreach (var link1 in Links.Where(p => p.Controlled == Client))
                    {
                        link1.Control.Telnet.Send(new CommandBody(Screen_NewScreen, link1.Control.Id, Client.Id, e.Content.Data));
                    }
                    Send(new CommandBody(Screen_AskForNextScreen, default, default, default));
                    break;
                case C_AskForClientList:
                    Send(new CommandBody(S_ClientsUpdate, default, default, Clients));
                    break;
                case Screen_AskForStartScreen:
                    ForwardAToB(e, (p1, p2) => { Links.Add(new ControlLink(Screen, Client, Clients.First(p => p.Id == p2.BId))); });
                    break;
                case Screen_AskForStopScreen:
                    {
                        ControlLink link = Links.Where(p => p.ControlType == ControlType.Screen)
                            .First(p => p.Control == Client && p.Controlled.Id.Equals(e.Content.BId));
                        Links.Remove(link);
                        if (!Links.Any(p => p.Controlled == link.Controlled))
                        {
                            link.Controlled.Telnet.Send(e.Content);
                        }
                    }
                    break;



                case WMI_AskForNamespaces:
                case WMI_AskForClasses:
                case WMI_AskForProps:
                    ForwardAToB(e);
                    break;

                case WMI_Namespace:
                case WMI_Classes:
                case WMI_Props:
                    ForwardBToA(e);
                    break;

                case File_AskForRootDirectory:
                case File_AskForDirectoryContent:
                case File_AskForDownloading:
                case File_AskForCancelDownload:
                    ForwardAToB(e);
                    break;

                case File_RootDirectory:
                case File_DirectoryContent:
                case File_ReadDownloadFileError:
                    ForwardBToA(e);
                    break;
                case File_Download:
                    var download = body.Data as DownloadPartInfo;
                    //downloadParts.Add(body);
                    Common.Telnet a = Clients.First(p => p.Id == body.AId).Telnet;

                    a.Send(body);
                    Send(new CommandBody(File_CanSendDownloadPartAgain, body.AId, body.BId,download.ID ));

                    //if (download.Position == 0)
                    //{
                    //    //Task task = new Task(() =>
                    //    //{
                    //    //    StartDownloadingToA(body);
                    //    //});
                    //    //download.Task = task;
                    //    StartDownloadingToA(body);
                    //    //task.Start();
                    //}
                    break;

            }
        }
        int count = 0;

        public List<CommandBody> downloadParts = new List<CommandBody>();

        public async Task StartDownloadingToA(CommandBody body)
        {
            Common.Telnet a = Clients.First(p => p.Id == body.AId).Telnet;

            DownloadPartInfo download = body.Data as DownloadPartInfo;
            long currentLength = 0;
            int count = 0;
            while (true)
            {
                if (currentLength == download.Length)
                {
                    return;
                }
                var part = downloadParts.FirstOrDefault(p => p.AId == body.AId && p.BId == body.BId
                  && (p.Data as DownloadPartInfo).Path == download.Path);
                if (part == null)
                {

                    await Task.Delay(50);
                    continue;
                }
                Debug.WriteLine("发送了"+ ++count);
                a.Send(part);
                currentLength += (part.Data as DownloadPartInfo).Content.Length;
                downloadParts.Remove(part);
            }
        }

        public void ForwardBToA(DataReceivedEventArgs e)
        {
            var a = Clients.First(p => p.Id == e.Content.AId).Telnet;
            a.Send(e.Content);
        }

        public void ForwardAToB(DataReceivedEventArgs e, Action<ClientInfo, CommandBody> doWhat = null)
        {
            Guid guid = e.Content.BId;
            ClientInfo targetClient = Clients.FirstOrDefault(p => p.Id.Equals(guid));
            if (targetClient == null)
            {
                SendNoSuchClient(guid);
            }
            else
            {
                targetClient.Telnet.Send(e.Content);
                doWhat?.Invoke(targetClient, e.Content);
            }
        }

        private void ClientExit()
        {
            Clients.Remove(Client);
            Client.Telnet.ClientSocket.Close();
            foreach (var link in Links.ToArray())
            {
                if (link.Control == Client)
                {
                    link.Controlled.Telnet.Send(new CommandBody(Screen_AskForStopScreen, default, link.Controlled.Id, null));
                }
                Links.Remove(link);
            }
            SendClientListToAllClients();
        }

        private void SendNoSuchClient(Guid guid)
        {
            Send(new CommandBody(S_NoSuchClient, Client.Id, guid, null));
        }

        private void SendClientListToAllClients()
        {
            foreach (var client in Clients)
            {
                client.Telnet.Send(new CommandBody(S_ClientsUpdate, default, default, Clients));
            }
        }

        private static void ClientSocketClosedByAccident(object sender, SocketClosedByAccidentEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}

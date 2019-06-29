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
                clientNet.SocketClosedByAccident +=clientNet. ClientSocketClosedByAccident;
                clientNet.DataSended += clientNet.ClientSocketDataSended;
                //p2.Instance.DataReceived += TelnetDataReceived;
                //Debug.WriteLine("增加了一个");
            };
            StartListening(Config.Instance.DeviceIP, Config.Instance.Port, () => new Telnet());

        }

        private  void ClientSocketDataSended(object sender, DataReceivedEventArgs e)
        {
            MainWindow.AddLog("发送：" + e.Content.Command,Client);
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
                        link1.Control.Telnet.Send(new CommandBody(Screen_NewScreen, link1.Control.ID, Client.ID, e.Content.Data));
                    }
                    Send(new CommandBody(Screen_AskForNextScreen, default, default, default));
                    break;
                case C_AskForClientList:
                    Send(new CommandBody(S_ClientsUpdate, default, default, Clients));
                    break;
                case Screen_AskForStartScreen:
                    ForwardAToB(e, c => { Links.Add(new ControlLink(Screen, Client, Clients.First(p => p.ID == e.Content.BId))); });
                    break;
                case Screen_AskForStopScreen:
                    {
                        ControlLink link = Links.Where(p => p.ControlType == Screen)
                            .First(p => p.Control == Client && p.Controlled.ID.Equals(e.Content.BId));
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
                case File_AskForStartUpload:
                case File_AskForCancelUpload:
                case File_Upload:
                case File_CanSendNextDownloadPart:
                case File_Operation:
                    ForwardAToB(e);
                    break;

                case File_Download:
                case File_RootDirectory:
                case File_DirectoryContent:
                case File_ReadDownloadingFileError:
                case File_PrepareUploadingFeedback:
                case File_CanSendNextUploadPart:
                case File_OperationFeedback:
                    ForwardBToA(e);
                    break;
                    //var download = body.Data as FileTransmissionPartInfo;
                    ////downloadParts.Add(body);
                    //Common.Telnet a = Clients.First(p => p.ID == body.AId).Telnet;

                    //a.Send(body);
                    //break;
                    //Send(new CommandBody(File_CanSendNextDownloadPart, body.AId, body.BId, download.ID));

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
                //case File_Upload:
                //     ForwardAToB(e,c=>
                //    Send(new CommandBody(File_CanSendNextUploadPart, body.AId, body.BId, (e.Content.Data as FileTransmissionPartInfo).ID)));

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

            MainWindow.AddLog("接收：" + command, Client);

        }

        public List<CommandBody> downloadParts = new List<CommandBody>();

        public async Task StartDownloadingToA(CommandBody body)
        {
            Common.Telnet a = Clients.First(p => p.ID == body.AId).Telnet;

            FileTransmissionPartInfo download = body.Data as FileTransmissionPartInfo;
            long currentLength = 0;
            int count = 0;
            while (true)
            {
                if (currentLength == download.Length)
                {
                    return;
                }
                var part = downloadParts.FirstOrDefault(p => p.AId == body.AId && p.BId == body.BId
                  && (p.Data as FileTransmissionPartInfo).Path == download.Path);
                if (part == null)
                {

                    await Task.Delay(50);
                    continue;
                }
                Debug.WriteLine("发送了" + ++count);
                a.Send(part);
                currentLength += (part.Data as FileTransmissionPartInfo).Content.Length;
                downloadParts.Remove(part);
            }
        }

        public void ForwardBToA(DataReceivedEventArgs e)
        {
            var a = Clients.First(p => p.ID == e.Content.AId).Telnet;
            a.Send(e.Content);
        }

        public void ForwardAToB(DataReceivedEventArgs e, Action<ClientInfo> doWhat = null)
        {
            Guid guid = e.Content.BId;
            ClientInfo targetClient = Clients.FirstOrDefault(p => p.ID.Equals(guid));
            if (targetClient == null)
            {
                SendNoSuchClient(guid);
            }
            else
            {
                targetClient.Telnet.Send(e.Content);
                doWhat?.Invoke(targetClient);
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
                    link.Controlled.Telnet.Send(new CommandBody(Screen_AskForStopScreen, default, link.Controlled.ID, null));
                }
                Links.Remove(link);
            }
            SendClientListToAllClients();
        }

        private void SendNoSuchClient(Guid guid)
        {
            Send(new CommandBody(S_NoSuchClient, Client.ID, guid, null));
        }

        private void SendClientListToAllClients()
        {
            foreach (var client in Clients)
            {
                client.Telnet.Send(new CommandBody(S_ClientsUpdate, default, default, Clients));
            }
        }

        private void ClientSocketClosedByAccident(object sender, SocketClosedByAccidentEventArgs e)
        {
            MainWindow.AddLog("客户端意外退出", Client);
        }
    }
}

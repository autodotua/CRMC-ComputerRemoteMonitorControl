using CRMC.Common.Model;
using FzLib.Control.Dialog;
using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace CRMC.Server
{
    public class LogInfo
    {
        public LogInfo(string content, string ip, string clientName, Guid? clientID)
        {
            Time = DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");
            ClientIP = ip;
            Content = content;
            ClientID = clientID;
            ClientName = clientName;
        }
        public Guid? ClientID { get; }
        public string ClientName { get; }
        public string ClientIP { get; }
        public string Time { get; }
        public string Content { get; }
    }

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        public Config Config => Config.Instance;
        public ObservableCollection<LogInfo> Logs { get; } = new ObservableCollection<LogInfo>();
        public static MainWindow Instance { get; private set; }
        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Config.Save();

            grdControl.IsEnabled = false;
            btnStart.Content = "正在启动服务";
            try
            {

                //DatabaseHelper.Instance.Model = new SCMSModel(Config.DbConnectionString);
                //DatabaseHelper.Instance.Model.Database.CommandTimeout = 2;

                //bool exist = false;
                //await Task.Run(() =>
                //{
                //    //exist = DatabaseHelper.Instance.Model.Database.Exists();
                //});
                //if (!exist)
                //{
                //    TaskDialog.ShowError("数据库连接失败");
                //    grdControl.IsEnabled = true;
                //    btnStart.Content = "启动";
                //    return;

                //}
                ////model.Database.Exists();

                Telnet.Start();
                btnStart.Content = "服务已启动";
            }
            catch (Exception ex)
            {
                TaskDialog.ShowException(ex, "启动失败");
                try
                {
                    //DatabaseHelper.Instance.Model.Dispose();
                }
                catch
                {

                }
                grdControl.IsEnabled = true;
                btnStart.Content = "启动";
            }
        }




        public static void AddLog(string content, ClientInfo client)
        {
            Instance.Dispatcher.Invoke(() =>
            {
                try
                {
                    string ip = null;
                    try
                    {
                        ip = (client?.Telnet.ClientSocket.RemoteEndPoint as IPEndPoint)?.Address?.ToString();
                    }
                    catch
                    {

                    }
                    LogInfo log = new LogInfo(content, ip, client?.Name, client?.ID);
                    Instance.Logs.Add(log);
                    Instance.lvwLog.ScrollIntoView(log);
                }
                catch
                {

                }
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //NetHelper.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Button_Click(null, null);
            WindowState = WindowState.Minimized;
#endif
        }
    }
}

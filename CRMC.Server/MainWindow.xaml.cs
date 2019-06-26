using FzLib.Control.Dialog;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace CRMC.Server
{
    public class LogInfo
    {
        public LogInfo(string content,string ip)
        {
            Time = DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");
            IP = ip;
            Content = content;
        }
        public string IP { get; }
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
        public MainWindow()
        {
            InitializeComponent();
            TaskDialog.DefaultOwner = this;
        }
        private  void Button_Click(object sender, RoutedEventArgs e)
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




        public void AddLog(string content,string ip)
        {
            LogInfo log = new LogInfo(content,ip);
            Logs.Add(log);
            lvwLog.ScrollIntoView(log);
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

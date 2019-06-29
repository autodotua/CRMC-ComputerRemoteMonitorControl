using CRMC.Common;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Image = System.Drawing.Image;
using Point = System.Drawing.Point;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using FzLib.Control.Extension;
using FzLib.Control.Dialog;
using CRMC.Common.Model;

namespace CRMC.Client.UI
{
    /// <summary>
    /// WinStudentManager.xaml 的交互逻辑
    /// </summary>
    public partial class WinScreen : CtrlWinBase
    {

        public WinScreen(Common.Model.ClientInfo client) : base(client)
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Telnet.Instance.ReceivedNewImage += ReceivedNewImage;
            Telnet.Instance.Send(new CommandBody(ApiCommand.Screen_AskForStartScreen, Global.CurrentClient.ID, ControlledClient.ID, ControlledClient.ID.ToByteArray()));

        }

        private void NoSuchClient(object sender, Common.Telnet.DataReceivedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                TaskDialog.Show("找不到对应的客户端！");
            });
            Close();
        }

        private void ReceivedNewImage(object sender, Common.Telnet.DataReceivedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                //Title = e.Data.Length.ToString();
                img.Source = LoadImage(e.Content.Data as byte[]);
            });
        }

        private static BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
            {
                return null;
            }
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Telnet.Instance.Send(new CommandBody(ApiCommand.Screen_AskForStopScreen, Global.CurrentClient.ID, ControlledClient.ID, ControlledClient.ID.ToByteArray()));
        }
    }
}

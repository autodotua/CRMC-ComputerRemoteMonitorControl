using CRMC.Common;
using CRMC.Common.Model;
using FzLib.Control.Dialog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CRMC.Client.UI
{
    /// <summary>
    /// PageLogin.xaml 的交互逻辑
    /// </summary>
    public partial class PageLogin : Page
    {
        FzLib.Cryptography.Hash md5 = new FzLib.Cryptography.Hash();
        public PageLogin()
        {
            InitializeComponent();
            pswd.Password = Config.Instance.UserPassword;

        }

        private void LoginFeedback(object sender, Common.Telnet.DataReceivedEventArgs e)
        {
            LoginFeedback result = e.Content.Data as LoginFeedback;
            Dispatcher.Invoke(() =>
            {

                if (result.Success)
                {
                    Global.CurrentClient = result.Client;

#if DEBUG
                    NavigationService.Navigate(new Uri($"UI/{nameof(PageComputerList)}.xaml", UriKind.Relative));
                    return;
#endif

                    //登录界面渐隐
                    DoubleAnimation ani = new DoubleAnimation(0, TimeSpan.FromSeconds(0.3), FillBehavior.Stop);
                    ani.Completed += (p1, p2) =>
                    {
                        grd.Opacity = 0;
                        grd.Children.Clear();
                        //向grd添加“登陆成功”的标签
                        TextBlock tbk = new TextBlock() { FontSize = 28, Text = "登录成功", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                        grd.Children.Add(tbk);
                        Grid.SetColumnSpan(tbk, 1000);
                        Grid.SetRowSpan(tbk, 1000);
                        //登陆成功渐显
                        DoubleAnimation ani2 = new DoubleAnimation(1, TimeSpan.FromSeconds(0.3), FillBehavior.Stop);
                        ani2.Completed += async (p3, p4) =>
                        {
                            grd.Opacity = 1;
                            await Task.Delay(1000);
                            //登陆成功渐隐
                            DoubleAnimation ani3 = new DoubleAnimation(0, TimeSpan.FromSeconds(0.3), FillBehavior.HoldEnd);
                            ani3.Completed += (p5, p6) =>
                            {
                                User user = result.User;

                                switch (user.Role)
                                {
                                    case "用户":
                                        NavigationService.Navigate(new Uri($"UI/{nameof(PageComputerList)}.xaml", UriKind.Relative));
                                        break;
                                    case "管理员":

                                        break;
                                    default:
                                        TaskDialog.Show("未知角色");
                                        break;
                                }
                            };
                                grd.BeginAnimation(OpacityProperty, ani3);
                        };
                         grd.BeginAnimation(OpacityProperty, ani2);

                    }; 
                     grd.BeginAnimation(OpacityProperty, ani);

                }
                else
                {
                    //登陆失败，显示错误信息
                    TaskDialog.ShowError(result.Message);
                    btnLogin.IsEnabled = true;

                }
            });
        }

        public Config Config => Config.Instance;
        /// <summary>
        /// 单击登录按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoginButtonClick(object sender, RoutedEventArgs e)
        {
            btnLogin.IsEnabled = false;
            //使用MD5加密密码
            Config.Instance.UserPassword = pswd.Password.Length == 32 ? pswd.Password : md5.GetString("MD5", pswd.Password);

            Telnet.Instance.LoginFeedback += LoginFeedback;
            Telnet.Instance.Send(new CommandContent( ApiCommand.C_Login,data: new ClientInfo()
            { Name = System.Environment.MachineName, User = new User() { Name = Config.UserName, Password = Config.UserPassword } }));

            Config.Save();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            LoginButtonClick(null, null);
#endif
            //PageComputerList page1 = new PageComputerList();

            //NavigationService.Navigate(page1);


        }
    }
}

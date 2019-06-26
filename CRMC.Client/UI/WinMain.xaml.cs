using CRMC.Common;
using FzLib.Control.Dialog;
using System.Windows;

namespace CRMC.Client
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class WinMain : Window
    {
        public WinMain()
        {
            InitializeComponent();
            TaskDialog.DefaultOwner = this;
            frame.Navigated += (p1, p2) =>
              {
                
              };
        }

        bool restart = false;
        private void LogoutButtonClick(object sender, RoutedEventArgs e)
        {
            restart = true;
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
          
            if (restart)
            {
                new WinMain().Show();
            }
        }

        private void ChangePasswordButtonClick(object sender, RoutedEventArgs e)
        {
        
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}

using CRMC.Common;
using System.Windows;
using System.Windows.Controls;
using FzLib.Basic.Collection;
using System.Diagnostics;
using System.Collections.Generic;
using CRMC.Common.Model;

namespace CRMC.Client.UI
{
    /// <summary>
    /// WinStudentManager.xaml 的交互逻辑
    /// </summary>
    public partial class PageComputerList : Page
    {
        public PageComputerList()
        {
            Telnet.Instance.ClientsUpdate += (s, e) =>
              {
                  Dispatcher.Invoke(() =>
                  {
                      Clients.Clear();
                      Clients.AddRange(e.Content.Data as IEnumerable<ClientInfo>);
#if DEBUG
                      if (Clients.Count > 0)
                      {
                          lvw.SelectedItem = Clients[0];
                      }
#endif
                  });

              };
        }

        public Common.Model.ClientInfo SelectedClient => lvw.SelectedItem as Common.Model.ClientInfo;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            Telnet.Instance.Send(new CommandBody(ApiCommand.C_AskForClientList,default,default,default));
        }

        public ExtendedObservableCollection<Common.Model.ClientInfo> Clients { get; } = new ExtendedObservableCollection<CRMC.Common.Model.ClientInfo>();

        private void ScreenControlButtonClick(object sender, RoutedEventArgs e)
        {
            Debug.Assert(SelectedClient != null);
            new WinScreen(SelectedClient).Show();
        }

        private void ButtonWMIClick(object sender, RoutedEventArgs e)
        {
            Debug.Assert(SelectedClient != null);
            new WinWMI(SelectedClient).Show();

        }

        private void ButtonFileSystemClick(object sender, RoutedEventArgs e)
        {
            Debug.Assert(SelectedClient != null);
            new WinFileSystem(SelectedClient).Show();
        }
    }
}

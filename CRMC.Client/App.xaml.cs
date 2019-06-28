using FzLib.Program.Runtime;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CRMC.Client
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            UnhandledException.RegistAll();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Telnet.Instance.Send(new Common.Model.CommandBody(Common.ApiCommand.C_Exit));
        }
    }
    
}

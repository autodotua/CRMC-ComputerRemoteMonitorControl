using CRMC.Common;
using System.Windows;
using System.Windows.Controls;
using FzLib.Basic.Collection;
using System.Diagnostics;
using FzLib.Control.Extension;
using System;
using System.Linq;
using System.Text;
using CRMC.Common.Model;
using System.Collections.Generic;
using static CRMC.Common.ApiCommand;

namespace CRMC.Client.UI
{
    /// <summary>
    /// WinStudentManager.xaml 的交互逻辑
    /// </summary>
    public partial class WinWMI : CtrlWinBase
    {
        private string selectedNamespace;
        private string selectedClass;
        private string selectedObject;

        public WinWMI(ClientInfo client) : base(client)
        {
            InitializeComponent();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Telnet.Instance.WMINamespacesReceived += WMINamespacesReceived;
            Telnet.Instance.WMIClassesReceived += WMIClassesReceived;
            Telnet.Instance.WMIPropsReceived += WMIPropsReceived;


            Telnet.Instance.Send(new CommandBody(WMI_AskForNamespaces, Global.CurrentClient.Id, ControlledClient.Id));
        }

        private void WMIPropsReceived(object sender, Common.Telnet.DataReceivedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                WMIProperties.Clear();
                WMIObjects.Clear();
                data = e.Content.Data as WMIObjectCollection;
                if (data.Namespace != SelectedNamespace || data.Class != SelectedClass)
                {
                    return;
                }
                foreach (WMIPropertyCollection props in data)
                {
                    WMIObjects.Add(props.Name);
                }
                if (WMIObjects.Count > 0)
                {
                    SelectedWMIObject = WMIObjects[0];
                }
                RefreshGridViewColumns(lvw);
            });

        }

        private void WMIClassesReceived(object sender, Common.Telnet.DataReceivedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                WMIClassess.Clear();
                foreach (var wmi in e.Content.Data as IEnumerable<WMIClassInfo>)
                {
                    if (wmi.Namespace == SelectedNamespace && !WMIClassess.Contains(wmi.Class))
                    {
                        WMIClassess.Add(wmi.Class);
                    }
                }
                StopLoading();
            });
        }

        private void WMINamespacesReceived(object sender, Common.Telnet.DataReceivedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                foreach (var wmi in e.Content.Data as IEnumerable<string>)
                {
                    if (!WMINamespaces.Contains(wmi))
                    {
                        WMINamespaces.Add(wmi);
                    }
                }
            });
        }

        public WMIObjectCollection data = null;
        public string SelectedNamespace
        {
            get => selectedNamespace;
            set
            {
                selectedNamespace = value;
                Telnet.Instance.Send(new CommandBody(WMI_AskForClasses, Global.CurrentClient.Id, ControlledClient.Id, value));
                StartLoading();
            }
        }
        public string SelectedClass
        {
            get => selectedClass;
            set
            {
                selectedClass = value;
                if (value != null)
                {
                    Telnet.Instance.Send(new CommandBody(WMI_AskForProps, Global.CurrentClient.Id, ControlledClient.Id, new WMIClassInfo() { Namespace = SelectedNamespace, Class = value }));
                    StartLoading();
                }
            }
        }
        public string SelectedWMIObject
        {
            get => selectedObject;
            set
            {
                selectedObject = value;
                WMIProperties.Clear();
                if (value != null)
                {
                    WMIProperties.AddRange(data.First(p => p.Name == value));
                }
                Notify(nameof(SelectedWMIObject));
            }
        }
        public ExtendedObservableCollection<string> WMINamespaces { get; } = new ExtendedObservableCollection<string>();
        public ExtendedObservableCollection<string> WMIClassess { get; } = new ExtendedObservableCollection<string>();
        public ExtendedObservableCollection<string> WMIObjects { get; } = new ExtendedObservableCollection<string>();
        public ExtendedObservableCollection<WMIPropertyInfo> WMIProperties { get; } = new ExtendedObservableCollection<WMIPropertyInfo>();


    }
}

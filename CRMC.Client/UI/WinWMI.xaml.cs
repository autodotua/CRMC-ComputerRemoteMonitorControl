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
            Telnet.Instance.WMINamespacesReceived += (p1, p2) =>
            {
                Dispatcher.Invoke(() =>
                {
                    foreach (var wmi in p2.Content.Data as IEnumerable<string>)
                    {
                        if (!WMINamespaces.Contains(wmi))
                        {
                            WMINamespaces.Add(wmi);
                        }
                    }
                });
            };
            Telnet.Instance.WMIClassesReceived += (p1, p2) =>
              {
                  Dispatcher.Invoke(() =>
                  {
                      WMIClassess.Clear();
                      foreach (var wmi in p2.Content.Data as IEnumerable<WMIClassInfo>)
                      {
                          if (wmi.Namespace == SelectedNamespace && !WMIClassess.Contains(wmi.Class))
                          {
                              WMIClassess.Add(wmi.Class);
                          }
                      }
                      StopLoading();
                  });
              };
            Telnet.Instance.WMIPropsReceived += (p1, p2) =>
              {
                  Dispatcher.Invoke(() =>
                  {
                      WMIProperties.Clear();
                      WMIObjects.Clear();
                      data = p2.Content.Data as WMIObjectCollection;
                      if (data.Namespace != SelectedNamespace || data.Class != SelectedClass)
                      {
                          return;
                      }
                      foreach (WMIPropertyCollection props in data)
                      {
                          WMIObjects.Add(props.Name);
                      }
                      if(WMIObjects.Count>0)
                      {
                          SelectedWMIObject = WMIObjects[0];
                      }

                      if (lvw.View is GridView gv)
                      {
                          foreach (var c in gv.Columns)
                          {
                              // Code below was found in GridViewColumnHeader.OnGripperDoubleClicked() event handler (using Reflector)
                              // i.e. it is the same code that is executed when the gripper is double clicked
                              if (double.IsNaN(c.Width))
                              {
                                  c.Width = c.ActualWidth;
                              }
                              c.Width = double.NaN;
                          }

                          StopLoading();
                      }
                  });

                
              };


            Telnet.Instance.Send(new CommandContent(WMI_AskForNamespaces, Global.CurrentClient.Id, ControlledClient.Id));
        }
        public WMIObjectCollection data = null;
        public string SelectedNamespace
        {
            get => selectedNamespace;
            set
            {
                selectedNamespace = value;
                Telnet.Instance.Send(new CommandContent(WMI_AskForClasses, Global.CurrentClient.Id, ControlledClient.Id, value));
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
                    Telnet.Instance.Send(new CommandContent(WMI_AskForProps, Global.CurrentClient.Id, ControlledClient.Id, new WMIClassInfo() { Namespace = SelectedNamespace, Class = value }));
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

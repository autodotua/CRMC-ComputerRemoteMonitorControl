using FzLib.Control.Dialog;
using FzLib.Control.Extension;
using FzLib.Control.Progress;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CRMC.Client.UI
{
    public abstract class CtrlWinBase : ExtendedWindow
    {
        public Common.Model.ClientInfo ControlledClient { get; protected set; }

        public CtrlWinBase(Common.Model.ClientInfo client)
        {
            ControlledClient = client;
            Telnet.Instance.NoSuchClient += (p1, p2) =>
              {
                  if (p2.Content.BId == client.Id)
                  {
                      Dispatcher.Invoke(() =>
                      {
                          TaskDialog.Show("找不到对应的被控端或被控端失去连接");
                          Close();
                      });
                  }
              };

        }

        protected void StartLoading()
        {
            Grid grd = Content as Grid;
            LoadingOverlay loading = grd.Children.OfType<LoadingOverlay>().SingleOrDefault();
            if (loading == null)
            {
                loading = new LoadingOverlay();
                loading.RingSize = 0.1;
                Grid.SetColumnSpan(loading, int.MaxValue);
                Grid.SetRowSpan(loading, int.MaxValue);
                loading.Margin = new System.Windows.Thickness(
                    -grd.Margin.Left,
                    -grd.Margin.Top,
                    -grd.Margin.Right,
                    -grd.Margin.Bottom);
                grd.Children.Add(loading);
            }
            loading.Show();
        }

        protected void StopLoading()
        {
            Grid grd = Content as Grid;
            LoadingOverlay loading = grd.Children.OfType<LoadingOverlay>().SingleOrDefault();
            loading?.Hide();
        }
    }
}

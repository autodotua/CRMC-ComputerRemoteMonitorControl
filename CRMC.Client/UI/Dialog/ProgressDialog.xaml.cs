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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static FzLib.Windows.WindowStyle;

namespace CRMC.Client.UI.Dialog
{
    /// <summary>
    /// ProgressDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ProgressDialog : FzLib.Control.Dialog.DialogWindowBase
    {
        public event RoutedEventHandler Cancle;
        public ProgressDialog(Window owner) : base(owner)
        {
            InitializeComponent();
            //var hwnd = new WindowInteropHelper(this).Handle;
            //FzLib.Windows.WindowStyle winStyle = new FzLib.Windows.WindowStyle(this);
            //winStyle.Set(winStyle.)
        }
        public void SetToError()
        {
            Dispatcher.Invoke(() =>
            {
                prgb.Background = Brushes.Red;
            });
        }
        public string Message
        {
            get => title;
            set => SetValueAndNotify(ref title, value, nameof(Message));
        }
        public string ButtonLabel
        {
            get => buttonLabel;
            set => SetValueAndNotify(ref buttonLabel, value, nameof(ButtonLabel));
        }
        public double Value
        {
            get => value;
            set => SetValueAndNotify(ref this.value, value, nameof(Value));
        }
        public double Maximum
        {
            get => maximum;
            set => SetValueAndNotify(ref maximum, value, nameof(Maximum));
        }
        public double Minimum
        {
            get => minimum;
            set => SetValueAndNotify(ref minimum, value, nameof(Minimum));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (buttonLabel == "取消")
            {
                Cancle?.Invoke(sender, e);
            }
            closing = true;
            Close();
        }
        bool closing = false;
        private double minimum;
        private double maximum;
        private double value;
        private string title;
        private string buttonLabel="取消";

        private void DialogWindowBase_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !closing;
        }
    }
}

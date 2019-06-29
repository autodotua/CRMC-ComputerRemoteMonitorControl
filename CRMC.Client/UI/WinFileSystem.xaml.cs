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
using System.Windows.Data;
using System.Globalization;
using FzLib.Control.Dialog;
using System.IO;
using CRMC.Client.UI.Dialog;
using FzLib.Basic;
using CRMC.Client.Control;
using System.Threading.Tasks;

namespace CRMC.Client.UI
{
    /// <summary>
    /// WinStudentManager.xaml 的交互逻辑
    /// </summary>
    public partial class WinFileSystem : CtrlWinBase
    {
        private FileFolderInfo selectedFile = null;
        public FileFolderInfo SelectedFile
        {
            get => selectedFile;
            set
            {
                SetValueAndNotify(ref selectedFile, value, nameof(SelectedFile));
            }
        }
        private string selectedDrive = null;
        public string SelectedDrive
        {
            get => selectedDrive;
            set
            {
                SetValueAndNotify(ref selectedDrive, value, nameof(SelectedDrive));
                OpenFolder(value);
            }
        }

        public List<FileFolderCollection> openedFolders = new List<FileFolderCollection>();
        public ExtendedObservableCollection<FileFolderInfo> Files { get; } = new ExtendedObservableCollection<FileFolderInfo>();
        public ExtendedObservableCollection<string> Drives { get; } = new ExtendedObservableCollection<string>();



        public WinFileSystem(ClientInfo client) : base(client)
        {
            InitializeComponent();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            Telnet.Instance.FileSystemRootReceived += FileSystemRootReceived;
            Telnet.Instance.FileSystemDirectoryContentReceived += FileSystemDirectoryContentReceived;
            Telnet.Instance.FileSystemFileFolderOperationFeedback += FileFolderOperationFeedback;
            Telnet.Instance.Send(new CommandBody(File_AskForRootDirectory, Global.CurrentClient.ID, ControlledClient.ID));

        }

        private void FileFolderOperationFeedback(object sender, Common.Telnet.DataReceivedEventArgs e)
        {
            var feedback = e.Content.Data as FileFolderFeedback;

            Dispatcher.Invoke(() =>
            {
                if (feedback.HasError)
                {
                    SnakeBar.ShowError(this, feedback.Message);
                }
                else
                {
                    SnakeBar.Show(this, feedback.Message);
                }
                RefreshButtonClick(null, null);
            });
        }

        private void FileSystemRootReceived(object sender, Common.Telnet.DataReceivedEventArgs e)
        {
            string[] drives = e.Content.Data as string[];
            if (drives.Length == 0)
            {
                TaskDialog.ShowError("目标计算机本地磁盘为空");
                Close();
                return;
            }
            Dispatcher.Invoke(() =>
            {
                Drives.Clear();
                Drives.AddRange(drives);
                SelectedDrive = Drives[0];

#if DEBUG
                txtPath.Text = @"C:\Users\admin\Desktop\test";
                JumpToFolderButtonClick(null, null);
#endif
            });

        }


        private void FileSystemDirectoryContentReceived(object sender, Common.Telnet.DataReceivedEventArgs e)
        {
            FileFolderCollection files = e.Content.Data as FileFolderCollection;
            if (files.IsError)
            {
                Dispatcher.Invoke(() =>
                {
                    TaskDialog.ShowError("打开失败", files.Error);
                    StopLoading();
                });

                return;
            }
            Dispatcher.Invoke(() =>
            {
                Files.Clear();
                Files.AddRange(files);
                CurrentPath = files.Path;
                RefreshGridViewColumns(lvw);
                StopLoading();
            });
            openedFolders.Add(files);
        }



        private void LvwItemPreviewMouseLeftButtonDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Debug.Assert(SelectedFile != null);
            if (SelectedFile.IsDirectory)
            {
                OpenFolder(SelectedFile.Path);
            }
            else
            {
                DownloadMenuClick(null, null);
            }
        }
        private string currentPath = null;
        public string CurrentPath
        {
            get => currentPath;
            set
            {
                currentPath = value;
                txtPath.Text = value;
            }
        }

        private void JumpToUpButtonClick(object sender, RoutedEventArgs e)
        {
            string parent = Path.GetDirectoryName(CurrentPath);
            if (parent != null)
            {
                OpenFolder(parent);
            }
        }

        private void JumpToFolderButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFolder(txtPath.Text);
        }

        public void RefreshFolder()
        {
            var existedFiles = openedFolders.FirstOrDefault(p => p.Path == CurrentPath);
            if (existedFiles != null)
            {
                openedFolders.Remove(existedFiles);
            }
            OpenFolder(CurrentPath, false);
        }
        private void OpenFolder(string path, bool allowCache = true)
        {
            if (allowCache)
            {
                var existedFiles = openedFolders.FirstOrDefault(p => p.Path == path);
                if (existedFiles != null)
                {
                    Files.Clear();
                    Files.AddRange(existedFiles);
                    CurrentPath = existedFiles.Path;
                    return;
                }
            }
            //StartLoading();
            Telnet.Instance.Send(new CommandBody(File_AskForDirectoryContent, Global.CurrentClient.ID, ControlledClient.ID, path));
        }

        private void LvwPreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            bool overSelecedItem = true;
            if (SelectedFile == null)
            {
                overSelecedItem = false;
            }
            else
            {
                var item = lvw.ItemContainerGenerator.ContainerFromItem(SelectedFile);
                if (item == null)
                {
                    overSelecedItem = false;
                }
                else if (!(item as ListViewItem).IsMouseOver)
                {
                    overSelecedItem = false;
                }
            }

            ContextMenu menu;
            if (overSelecedItem)
            {
                menu = FindResource("menuOverItem") as ContextMenu;
            }
            else
            {
                menu = FindResource("menuEmpty") as ContextMenu;
                (menu.Items[0] as MenuItem).Visibility = (ongoingOperation == null) ? Visibility.Collapsed : Visibility.Visible;
            }

            menu.IsOpen = true;
        }

        private void DownloadMenuClick(object sender, RoutedEventArgs e)
        {
            FileDownloadHelper download = new FileDownloadHelper();
            download.StartNew(this, ControlledClient, SelectedFile);
        }

        private void PasteMenuClick(object sender, RoutedEventArgs e)
        {
            ongoingOperation.Target = Path.Combine(CurrentPath, Path.GetFileName(ongoingOperation.Source));

            bool ok = true;
            if (Files.Any(p => p.Path == ongoingOperation.Target))
            {
                if (TaskDialog.ShowWithYesNoButtons("可能存在相同名称的文件（夹），是否覆盖？", "文件" + ongoingOperation.Target + "可能已存在") == false)
                {
                    ok = false;
                }
            }
            if (ok)
            {
                Telnet.Instance.Send(new CommandBody(File_Operation, Global.CurrentClient.ID, ControlledClient.ID, ongoingOperation));
                SnakeBar.Show("已通知被控端进行" + ongoingOperation.OperationDescription + "操作");
                if(ongoingOperation.Operation!=FileFolderOperation.Copy)
                {
                    ongoingOperation = null;
                }
            }

        }

        private void UploadMenuClick(object sender, RoutedEventArgs e)
        {
            FileUploadHelper uploadHelper = new FileUploadHelper();
            uploadHelper.StartNew(this, ControlledClient, CurrentPath);
        }

        private void RefreshButtonClick(object sender, RoutedEventArgs e)
        {
            RefreshFolder();
        }

        private void CopyButtonClick(object sender, RoutedEventArgs e)
        {
            ongoingOperation = new FileFolderOperationInfo() { Source = SelectedFile.Path, Operation = FileFolderOperation.Copy };
        }

        public FileFolderOperationInfo ongoingOperation = null;

        private void CutButtonClick(object sender, RoutedEventArgs e)
        {
            ongoingOperation = new FileFolderOperationInfo() { Source = SelectedFile.Path, Operation = FileFolderOperation.Move };
        }

        private void DeleteButtonClick(object sender, RoutedEventArgs e)
        {
            var op = new FileFolderOperationInfo() { Source = SelectedFile.Path, Operation = FileFolderOperation.Delete };
            Telnet.Instance.Send(new CommandBody(File_Operation, Global.CurrentClient.ID, ControlledClient.ID,   op));
            SnakeBar.Show("已通知被控端进行" + op.OperationDescription + "操作");

        }
    }
    public class FileFolderIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isDir = (bool)value;
            {
                if (isDir)
                {
                    return FzLib.Control.Common.CloneXaml(Application.Current.FindResource("imgFolder"));
                }
                return FzLib.Control.Common.CloneXaml(Application.Current.FindResource("imgFile"));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}

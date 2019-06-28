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
            Telnet.Instance.FileSystemDownloadErrorReceived += FileSystemDownloadErrorReceived;
            Telnet.Instance.FileSystemRootReceived += FileSystemRootReceived;
            Telnet.Instance.FileSystemDirectoryContentReceived += FileSystemDirectoryContentReceived;
            Telnet.Instance.FileSystemDownloadPartReceived += FileSystemDownloadPartReceived;
            Telnet.Instance.Send(new CommandBody(File_AskForRootDirectory, Global.CurrentClient.Id, ControlledClient.Id));
        }

        private void FileSystemDownloadErrorReceived(object sender, Common.Telnet.DataReceivedEventArgs e)
        {
            Debug.Assert(currentDownload != null);
            currentDownload.Dialog.Message = "下载失败：" + (e.Content.Data as DownloadError).Error;
            currentDownload.Dialog.SetToError();

            currentDownload.Dispose(true);
           
            currentDownload = null;

        }

        private void FileSystemDownloadPartReceived(object sender, Common.Telnet.DataReceivedEventArgs e)
        {
            Debug.Assert(currentDownload != null);
            DownloadPartInfo download = e.Content.Data as DownloadPartInfo;

            if (currentDownload.ID!= download.ID)
            {
                return;
            }
            if(currentDownload.Canceled)
            {
                return;
            }

            currentDownload.Stream.Position = download.Position;
            currentDownload.Stream.Write(download.Content, 0, download.Content.Length);

            currentDownload.Dialog.Value = 1.0 * currentDownload.Stream.Position / download.Length;
            currentDownload.Dialog.Message = $"正在下载{  currentDownload.File.Name}：{Number.ByteToFitString(currentDownload.Stream.Position)}/{Number.ByteToFitString(download.Length)}";

            if (download.Position + download.Content.Length == download.Length)
            {
                currentDownload.Dialog.Message = "下载成功";

                currentDownload.Dispose();
                currentDownload = null;
            }

        }



        public DownloadingFileInfo currentDownload;

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
            OpenFolder(SelectedFile.Path);
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

        private void OpenFolder(string path)
        {
            var existedFiles = openedFolders.FirstOrDefault(p => p.Path == path);
            if (existedFiles != null)
            {
                Files.Clear();
                Files.AddRange(existedFiles);
                CurrentPath = existedFiles.Path;
                return;
            }
            StartLoading();
            Telnet.Instance.Send(new CommandBody(File_AskForDirectoryContent, Global.CurrentClient.Id, ControlledClient.Id, path));
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
            }

            menu.IsOpen = true;
        }

        private void DownloadMenuClick(object sender, RoutedEventArgs e)
        {
            string path = FileSystemDialog.GetSaveFile(null, true, false, SelectedFile.Name);
            if (path != null)
            {
                DownloadInfo download = new DownloadInfo() { File = SelectedFile };
                try
                {
                    currentDownload = new DownloadingFileInfo(this,ControlledClient, path, download);
                }
                catch (Exception ex)
                {
                    TaskDialog.ShowException(ex, "建立本地文件失败！");
                    return;
                }
                Telnet.Instance.Send(new CommandBody(
     File_AskForDownloading, Global.CurrentClient.Id, ControlledClient.Id, download));

                currentDownload.Dialog.ShowDialog();


            }
        }

        private void PasteMenuClick(object sender, RoutedEventArgs e)
        {

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

    public class DownloadingFileInfo : IDisposable
    {
        public DownloadingFileInfo(Window win,ClientInfo controlledClient, string localPath, DownloadInfo download)
        {
            LocalPath = localPath;
            File = download.File;
            ID = download.ID;
            Dialog = new ProgressDialog(win) { Message = "正在下载" + File.Name };
            Dialog.Value = 0;
            Dialog.Minimum = 0;
            Dialog.Maximum = 1;
            Dialog.Cancle += (p1, p2) =>
              {
                  Canceled = true;
                  Dispose(true);

                  Telnet.Instance.Send(new CommandBody(
File_AskForCancelDownload, Global.CurrentClient.Id, controlledClient.Id, ID));

              };

            Stream = System.IO.File.OpenWrite(localPath);
            Stream.SetLength(File.Length);

        }

        public string LocalPath { get; private set; }
        public Guid ID { get; private set; }
        public FileStream Stream { get; private set; }
        public FileFolderInfo File { get; private set; }
        public ProgressDialog Dialog { get; private set; }
        public bool Canceled { get;private set; } = false;

        public void Dispose()
        {
            Dispose(false);
        }
        public void Dispose(bool failed)
        {
            Dialog.ButtonLabel = "关闭";
            Stream?.Flush();
            Stream?.Dispose();

            if(failed)
            {
                if (System.IO. File.Exists(LocalPath))
                {
                    try
                    {
                        System.IO.File.Delete(LocalPath);
                    }
                    catch
                    {

                    }
                }
            }
        }
    }
}

using CRMC.Client.UI;
using CRMC.Client.UI.Dialog;
using CRMC.Common;
using CRMC.Common.Model;
using FzLib.Basic;
using FzLib.Control.Dialog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static CRMC.Common.ApiCommand;

namespace CRMC.Client.Control
{
    public  class FileDownloadHelper
    {
        public FileDownloadHelper()
        {
        }

        public Common.Model.ClientInfo ControlledClient { get; protected set; }

        public  void StartNew(WinFileSystem win,ClientInfo client, FileFolderInfo selectedFile)
        {
            ControlledClient = client;
            string path = FileSystemDialog.GetSaveFile(null, true, false, selectedFile.Name);
            if (path != null)
            {
                FileTransmissionInfo trans = new FileTransmissionInfo() { File = selectedFile };
                try
                {
                    download = new DownloadingInfo(win, client, path, trans);
                }
                catch (Exception ex)
                {
                    TaskDialog.ShowException(ex, "建立本地文件失败！");
                    return;
                }
                Telnet.Instance.FileSystemDownloadErrorReceived += FileSystemDownloadErrorReceived;
                Telnet.Instance.FileSystemDownloadPartReceived += FileSystemDownloadPartReceived;

                Telnet.Instance.Send(new CommandBody(
     File_AskForDownloading, Global.CurrentClient.ID, client.ID, trans));

                download.Dialog.ShowDialog();


            }
        }
        public  DownloadingInfo download;

        private  void FileSystemDownloadErrorReceived(object sender, Common.Telnet.DataReceivedEventArgs e)
        {
            Debug.Assert(download != null);
            download.Dialog.Message = "下载失败：" + (e.Content.Data as FileFolderFeedback).Message;
            download.Dialog.SetToError();

            download.Dispose(true);


        }

        private  void FileSystemDownloadPartReceived(object sender, Common.Telnet.DataReceivedEventArgs e)
        {
            FileTransmissionPartInfo part = e.Content.Data as FileTransmissionPartInfo;

            try
            {
                Debug.Assert(download != null);

                if (download.ID != part.ID)
                {
                    return;
                }
                if (download.Canceled)
                {
                    return;
                }

                download.Stream.Position = part.Position;
                download.Stream.Write(part.Content, 0, part.Content.Length);

                download.Dialog.Value = 1.0 * download.Stream.Position / part.Length;
                download.Dialog.Message = $"正在下载{  download.File.Name}：{Number.ByteToFitString(download.Stream.Position)}/{Number.ByteToFitString(part.Length)}";

                if (part.Position + part.Content.Length == part.Length)
                {
                    download.Dialog.Message = "下载成功";

                    download.Dispose();
                    download = null;
                    DownloadEnd();
                }
                else
                {
                    Telnet.Instance.Send(new CommandBody(File_CanSendNextDownloadPart, Global.CurrentClient.ID, ControlledClient.ID, download.ID));
                }
            }
            catch (ObjectDisposedException ex)
            {

            }
            catch (Exception ex)
            {
                Telnet.Instance.Send(new CommandBody(File_AskForCancelUpload, Global.CurrentClient.ID, ControlledClient.ID, part.ID));

                FileSystemDownloadErrorReceived(null, null);
            }
        }


        private void DownloadEnd()
        { 
            Telnet.Instance.FileSystemDownloadErrorReceived -= FileSystemDownloadErrorReceived;
            Telnet.Instance.FileSystemDownloadPartReceived -= FileSystemDownloadPartReceived;
        }
    }

    public  class FileUploadHelper
    {
        private WinFileSystem win;
        public void StartNew(WinFileSystem win,ClientInfo client,string remoteFolderPath)
        {
            this.win = win;
            ControlledClient = client;
            string path = FileSystemDialog.GetOpenFile();
            if (path != null)
            {
                var trans = new FileTransmissionInfo
                {
                    File = new FileFolderInfo()
                    { Path = Path.Combine(remoteFolderPath, Path.GetFileName(path)), Length = new FileInfo(path).Length }
                };
                currentUpload = new UploadInfo(win, ControlledClient, path, trans);

                Telnet.Instance.Send(new CommandBody(File_AskForStartUpload, Global.CurrentClient.ID, ControlledClient.ID, trans));
            }

            Telnet.Instance.FileSystemPrepareForUploadingReceived += FileSystemPrepareForUploadingReceived; ;

        }

        private void FileSystemPrepareForUploadingReceived(object sender, Common.Telnet.DataReceivedEventArgs e)
        {
            var feedback = e.Content.Data as FileFolderFeedback;
            if (feedback.HasError)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    TaskDialog.ShowError(feedback.Message);
                });
                return;
            }
            Debug.Assert((e.Content.Data as FileFolderFeedback).ID == currentUpload.ID);
            StartUploadingToB(currentUpload, ControlledClient);
        }



        public UploadInfo currentUpload;

        public Common.Model.ClientInfo ControlledClient { get; protected set; }


        public static ConcurrentBag<Guid> CanSendNextPart { get; } = new ConcurrentBag<Guid>();
        public static ConcurrentDictionary<Guid, string> Error { get; } = new ConcurrentDictionary<Guid, string>();

        public  void StartUploadingToB(UploadInfo upload, ClientInfo controlledClient)
        {
            var dialog = upload.Dialog;
            var fs = upload.Stream;
            win.Dispatcher.BeginInvoke((Action)(() =>
            {
                dialog.ShowDialog();
            }));
            Task.Run(() =>
            {
                //Debug.Assert(file.IsDirectory == false);
                try
                {
                    int bufferLength = 1024 * 1024;
                    byte[] buffer = new byte[bufferLength];

                    int length = 0;
                    while (true)
                    {
                        if (upload.Canceled)
                        {
                            return;
                        }
                        long position = fs.Position;
                        win.Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                dialog.Value = position;
                                dialog.Message = $"正在上传{  upload.File.Name}：{Number.ByteToFitString(upload.Stream.Position)}/{Number.ByteToFitString(fs.Length)}";
                            }
                            catch (System.ObjectDisposedException)
                            {

                            }
                        });


                        int count = bufferLength;
                        if (fs.Length - fs.Position < bufferLength)
                        {
                            count = (int)(fs.Length - fs.Position);
                        }
                        length = fs.Read(buffer, 0, count);
                        if (length == 0)
                        {
                            win.Dispatcher.Invoke(() =>
                            {
                                dialog.Message = "上传完成";
                                upload.Dispose();
                            });
                            break;
                        }
                        if (length < bufferLength)
                        {
                            byte[] newBuffer = new byte[length];
                            Array.Copy(buffer, newBuffer, length);
                            buffer = newBuffer;
                        }

                        Telnet.Instance.Send(new CommandBody(ApiCommand.File_Upload, Global.CurrentClient.ID, controlledClient.ID,
                            new FileTransmissionPartInfo() { Content = buffer, Path = upload.File.Path, Position = position, Length = fs.Length, ID = upload.ID }));

                        while (true)
                        {
                            if (Error.ContainsKey(upload.ID))
                            {
                                string message = Error[upload.ID];
                                upload.Dialog.SetToError();
                                upload.Dialog.Message = "上传失败：" + message;
                                //Telnet.Instance.Send(new CommandBody(File_AskForCancelUpload, Global.CurrentClient.ID, controlledClient.ID, upload.ID));

                            }
                            var canNext = CanSendNextPart.FirstOrDefault(p => p == upload.ID);
                            if (canNext != default)
                            {
                                CanSendNextPart.TryTake(out canNext);
                                break;
                            }
                            Thread.Sleep(30);
                        }
                    }

                }
                catch (System.ObjectDisposedException)
                {

                }
                catch (Exception ex)
                {
                    upload.Dialog.SetToError();
                    upload.Dialog.Message = "上传失败：" + ex.Message;
                    Telnet.Instance.Send(new CommandBody(File_AskForCancelUpload, Global.CurrentClient.ID, controlledClient.ID, upload.ID));
                }
            });
        }

    }

    public class DownloadingInfo : FileTransmissionInfo, IDisposable
    {
        public DownloadingInfo(WinFileSystem win, ClientInfo controlledClient, string localPath, FileTransmissionInfo download)
        {
            LocalPath = localPath;
            File = download.File;
            ID = download.ID;
            Dialog = new ProgressDialog(win) { Message = "正在下载" + File.Name };
            Dialog.Value = 0;
            Dialog.Minimum = 0;
            Dialog.Maximum = 1;
            Dialog.Title = "下载";
            Dialog.Cancle += (p1, p2) =>
            {
                Canceled = true;
                Dispose(true);

                Telnet.Instance.Send(new CommandBody(File_AskForCancelDownload, Global.CurrentClient.ID, controlledClient.ID, ID));

            };
            Dialog.Closed += (p1, p2) =>
              {
                  if (Dialog.Canceled == false)
                  {
                      win.RefreshFolder();
                  }
              };

            Stream = System.IO.File.OpenWrite(localPath);
            Stream.SetLength(File.Length);

        }

        public string LocalPath { get; private set; }
        public FileStream Stream { get; private set; }
        public ProgressDialog Dialog { get; private set; }
        public bool Canceled { get; private set; } = false;

        public void Dispose()
        {
            Dispose(false);
        }
        public void Dispose(bool failed)
        {
            Dialog.ButtonLabel = "关闭";
            Stream?.Flush();
            Stream?.Dispose();

            if (failed)
            {
                if (System.IO.File.Exists(LocalPath))
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
    public class UploadInfo : FileTransmissionInfo, IDisposable
    {
        public UploadInfo(WinFileSystem win, ClientInfo controlledClient, string localPath, FileTransmissionInfo trans)
        {
            LocalPath = localPath;
            File = trans.File;
            ID = trans.ID;
            Dialog = new ProgressDialog(win) { Message = "正在上传" + File.Name };
            Dialog.Value = 0;
            Dialog.Minimum = 0;
            Dialog.Maximum = trans.File.Length;
            Dialog.Title = "上传";
            Dialog.Cancle += (p1, p2) =>
            {
                Canceled = true;
                Dispose();

                Telnet.Instance.Send(new CommandBody(File_AskForCancelUpload, Global.CurrentClient.ID, controlledClient.ID, ID));

            };
            Dialog.Closed += (p1, p2) =>
            {
                if (Dialog.Canceled == false)
                {
                    win.RefreshFolder();
                }
            };

            Stream = System.IO.File.OpenRead(localPath);

        }

        public string LocalPath { get; private set; }
        public FileStream Stream { get; private set; }
        public ProgressDialog Dialog { get; private set; }
        public bool Canceled { get; private set; } = false;

        public void Dispose()
        {
            Dialog.ButtonLabel = "关闭";
            Stream?.Flush();
            Stream?.Dispose();
        }
    }

}

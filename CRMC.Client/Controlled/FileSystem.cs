using CRMC.Common;
using CRMC.Common.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CRMC.Client.Controlled
{
    public static class FileSystemHelper
    {
        public static void SendDiskDrives(CommandBody cmd)
        {
            Telnet.Instance.Send(new CommandBody(ApiCommand.File_RootDirectory, cmd.AId, cmd.BId, Directory.GetLogicalDrives()));
        }

        public static void SendDirectoryContent(CommandBody cmd)
        {
            string path = cmd.Data as string;
            FileFolderCollection files = new FileFolderCollection() { Path = path };
            if (!Directory.Exists(path))
            {
                files.Error = "不存在目录" + path;
            }
            else
            {
                try
                {
                    foreach (var p in Directory.EnumerateDirectories(path))
                    {
                        files.Add(new FileFolderInfo(new DirectoryInfo(p)));
                    }
                    foreach (var p in Directory.EnumerateFiles(path))
                    {
                        files.Add(new FileFolderInfo(new FileInfo(p)));
                    }
                }
                catch (Exception ex)
                {
                    files.Error = ex.Message;
                }
            }
            Telnet.Instance.Send(new CommandBody(ApiCommand.File_DirectoryContent, cmd.AId, cmd.BId, files));
        }
        public static ConcurrentBag<Guid> CanSendNextPart { get; } = new ConcurrentBag<Guid>();
        public static ConcurrentBag<Guid> Cancle { get; } = new ConcurrentBag<Guid>();
        public static void StartDownloadingToA(CommandBody cmd)
        {
            Task.Run(() =>
            {
                FileTransmissionInfo download = cmd.Data as FileTransmissionInfo;
                var file = download.File;
                Debug.Assert(file.IsDirectory == false);
                try
                {


                    int bufferLength = 1024 * 1024;
                    byte[] buffer = new byte[bufferLength];
                    using (FileStream fs = File.OpenRead(file.Path))
                    {
                        int length = 0;
                        while (true)
                        {
                            var cancel = Cancle.FirstOrDefault(p => p == download.ID);
                            if (cancel != default)
                            {
                                Cancle.TryTake(out cancel);
                                return;
                            }
                            long position = fs.Position;
                            int count = bufferLength;
                            if (fs.Length - fs.Position < bufferLength)
                            {
                                count = (int)(fs.Length - fs.Position);
                            }
                            length = fs.Read(buffer, 0, count);
                            if (length == 0)
                            {
                                break;
                            }
                            if (length < bufferLength)
                            {
                                byte[] newBuffer = new byte[length];
                                Array.Copy(buffer, newBuffer, length);
                                buffer = newBuffer;
                            }

                            Telnet.Instance.Send(new CommandBody(ApiCommand.File_Download, cmd.AId, cmd.BId,
                                new FileTransmissionPartInfo() { Content = buffer, Path = file.Path, Position = position, Length = fs.Length, ID = download.ID }));
                            while (true)
                            {
                                var canNext = CanSendNextPart.FirstOrDefault(p => p == download.ID);
                                if (canNext != default)
                                {
                                    CanSendNextPart.TryTake(out canNext);
                                    break;
                                }
                                Thread.Sleep(30);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Telnet.Instance.Send(new CommandBody(ApiCommand.File_ReadDownloadingFileError, cmd.AId, cmd.BId,
                 new FileFolderFeedback() { ID = download.ID, Path = file.Path, Message = ex.Message, HasError = true }));
                }
            });
        }
        public static Dictionary<Guid, FileStream> uploadStreams = new Dictionary<Guid, FileStream>();

        public static void StartAcceptingUploading(CommandBody cmd)
        {
            var trans = cmd.Data as FileTransmissionInfo;

            string path = trans.File.Path;
            if (File.Exists(path))
            {
                Telnet.Instance.Send(new CommandBody(ApiCommand.File_PrepareUploadingFeedback, cmd.AId, cmd.BId, new FileFolderFeedback() { ID = trans.ID, HasError = true, Message = "存在相同文件名的文件" }));
                return;
            }
            FileStream fs = null;
            try
            {
                fs = File.OpenWrite(path);
                fs.SetLength(trans.File.Length);
                uploadStreams.Add(trans.ID, fs);
            }
            catch (Exception ex)
            {
                try
                {
                    fs?.Dispose();
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch
                {

                }
                Telnet.Instance.Send(new CommandBody(ApiCommand.File_PrepareUploadingFeedback, cmd.AId, cmd.BId, new FileFolderFeedback() { ID = trans.ID, HasError = true, Message = "创建文件失败：" + ex.Message }));
                return;
            }

            Telnet.Instance.Send(new CommandBody(ApiCommand.File_PrepareUploadingFeedback, cmd.AId, cmd.BId, new FileFolderFeedback() { ID = trans.ID, HasError = false, Message = "可以开始上传" }));
        }

        public static void AcceptUploadPartFromA(CommandBody cmd)
        {
            FileTransmissionPartInfo upload = cmd.Data as FileTransmissionPartInfo;

            try
            {
                var fs = uploadStreams[upload.ID];

                fs.Position = upload.Position;
                fs.Write(upload.Content, 0, upload.Content.Length);


                Debug.WriteLine(fs.Position + "/" + fs.Length);
                if (upload.Position + upload.Content.Length == upload.Length)
                {
                    Debug.WriteLine("退出");
                    fs.Flush();
                    fs.Dispose();
                    uploadStreams.Remove(upload.ID);
                }

                Telnet.Instance.Send(new CommandBody(ApiCommand.File_CanSendNextUploadPart, cmd.AId, cmd.BId, upload.ID));
            }
            catch (Exception ex)
            {
                Telnet.Instance.Send(new CommandBody(ApiCommand.File_AskForCancelUpload, cmd.AId, cmd.BId, new FileFolderFeedback() { ID = upload.ID, HasError = true, Message = ex.Message }));
            }
        }

        public static void CancelUploadFromA(CommandBody cmd)
        {
            var fs = uploadStreams[(Guid)cmd.Data];
            fs.Dispose();
            try
            {
                if (File.Exists(fs.Name))
                {
                    File.Delete(fs.Name);
                }
            }
            catch
            {

            }
        }


        public static void FileFolderOperate(CommandBody cmd)
        {
            FileFolderOperationInfo operation = cmd.Data as FileFolderOperationInfo;
            FileFolderFeedback feedback = new FileFolderFeedback()
            {
                Path = operation.Source,
            };
            Task.Run(() =>
                {
                    try
                    {
                        if (File.Exists(operation.Source))
                        {
                            switch (operation.Operation)
                            {
                                case FileFolderOperation.Copy:
                                    File.Copy(operation.Source, operation.Target, true);
                                    feedback.Message = $"成功复制文件{operation.Source}到{operation.Target}";
                                    break;
                                case FileFolderOperation.Move:
                                    if (File.Exists(operation.Source))
                                    {
                                        feedback.HasError = false;
                                        feedback.Message = "目标文件已存在";
                                        break;
                                    }
                                    File.Move(operation.Source, operation.Target);
                                    feedback.Message = $"成功移动文件{operation.Source}到{operation.Target}";
                                    break;
                                case FileFolderOperation.Delete:
                                    File.Delete(operation.Source);
                                    feedback.Message = $"成功删除文件{operation.Source}";
                                    break;
                            }
                        }
                        else if(Directory.Exists(operation.Source))
                        {
                            switch (operation.Operation)
                            {
                                case FileFolderOperation.Copy:
                                    FzLib.IO.FileSystem.CopyDirectory(operation.Source, operation.Target);
                                    feedback.Message = $"成功复制文件夹{operation.Source}到{operation.Target}";
                                    break;
                                case FileFolderOperation.Move:
                                    if (File.Exists(operation.Source))
                                    {
                                        feedback.HasError = false;
                                        feedback.Message = "目标文件已存在";
                                        break;
                                    }
                                    FzLib.IO.FileSystem.CopyDirectory(operation.Source, operation.Target);
                                    feedback.Message = $"成功移动文件夹{operation.Source}到{operation.Target}";
                                    Directory.Delete(operation.Source, true);
                                    break;
                                case FileFolderOperation.Delete:
                                    Directory.Delete(operation.Source,true);
                                    feedback.Message = $"成功复制文件夹{operation.Source}";
                                    break;
                            }
                        }
                        else
                        {
                            feedback.HasError = true;
                            feedback.Message = "源文件不存在";
                        }
                    }
                    catch (Exception ex)
                    {
                        feedback.HasError = true;
                        feedback.Message = ex.Message;
                    }
                    Telnet.Instance.Send(new CommandBody(ApiCommand.File_OperationFeedback, cmd.AId, cmd.BId, feedback));

                });
        }
    }
}

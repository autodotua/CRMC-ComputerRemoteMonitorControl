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
                DownloadInfo download = cmd.Data as DownloadInfo;
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
                                new DownloadPartInfo() { Content = buffer, Path = file.Path, Position = position, Length = fs.Length ,ID=download.ID}));
                            while (true)
                            {
                                var canNext = CanSendNextPart.FirstOrDefault(p => p== download.ID);
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
                    Telnet.Instance.Send(new CommandBody(ApiCommand.File_ReadDownloadFileError, cmd.AId, cmd.BId,
                 new DownloadError() {ID=download.ID, Path = file.Path, Error = ex.Message }));
                }
            });
        }
    }
}

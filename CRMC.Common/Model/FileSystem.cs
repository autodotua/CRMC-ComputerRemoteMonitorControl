using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMC.Common.Model
{
    [Serializable]
    public class FileFolderInfo
    {
        public FileFolderInfo()
        {

        }
        public FileFolderInfo(FileInfo file)
        {
            Debug.Assert(file != null);

            Name = file.Name;
            Path = file.FullName;
            Length = file.Length;
            LastWriteTime = file.LastWriteTime;
            LastAccessTime = file.LastAccessTime;
            CreationTime = file.CreationTime;
        }
        public FileFolderInfo(DirectoryInfo dir)
        {
            Debug.Assert(dir != null);

            Name = dir.Name;
            Path = dir.FullName;
            LastWriteTime = dir.LastWriteTime;
            LastAccessTime = dir.LastAccessTime;
            CreationTime = dir.CreationTime;
            IsDirectory = true;
        }

        public string Name { get; set; }

        public bool IsDirectory { get; set; }

        public string Path { get; set; }

        public long Length { get; set; } = long.MinValue;
        public string LengthString => Length == long.MinValue ? "" : FzLib.Basic.Number.ByteToFitString(Length);

        public DateTime? LastWriteTime { get; set; }
        public DateTime? LastAccessTime { get; set; }
        public DateTime? CreationTime { get; set; }
    }

    [Serializable]
    public class FileFolderCollection : List<FileFolderInfo>
    {
        public FileFolderCollection()
        {
        }

        public FileFolderCollection(IEnumerable<FileFolderInfo> collection) : base(collection)
        {
        }

        public string Path { get; set; }

        public string Error { get; set; } = null;
        public bool IsError => Error != null;
    }
    [Serializable]
    public class FileTransmissionInfo
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public FileFolderInfo File { get; set; }
    }
    [Serializable]
    public class FileTransmissionPartInfo
    {
        [NonSerialized]
        private Task task;

        public Guid ID { get; set; }
        public string Path { get; set; }
        public byte[] Content { get; set; }
        public long Position { get; set; }
        public long Length { get; set; }
        public Task Task { get => task; set => task = value; }
    }

    [Serializable]
    public class FileFolderFeedback
    {
        public Guid ID { get; set; }
        public string Path { get; set; }
        public string Message { get; set; }
        public bool HasError { get; set; }
    }
    [Serializable]
    public class FileFolderOperationInfo
    {
        public string Source { get; set; }
        public string Target { get; set; }
        public FileFolderOperation Operation { get; set; } = FileFolderOperation.Unknown;
        public string OperationDescription
        {
            get
            {
                switch (Operation)
                {
                    case FileFolderOperation.Copy:
                        return "复制";
                    case FileFolderOperation.Move:
                        return "移动";
                    case FileFolderOperation.Delete:
                        return "复制";
                    //case FileFolderOperation.Rename:
                    //    return "重命名";
                    default:
                        return "位置";
                }

            }
        }
    }

    public enum FileFolderOperation
    {
        Unknown,
        Delete,
        Copy,
        //Rename,
        Move
    }
}

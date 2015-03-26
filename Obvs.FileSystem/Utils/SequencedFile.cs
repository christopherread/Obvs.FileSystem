using System;
using System.IO;

namespace Obvs.FileSystem.Utils
{
    internal class SequencedFile
    {
        public const string TextExtension = ".txt";
        public const string BinaryExtension = ".bin";

        private readonly string _directory;
        private readonly int _sequenceId;
        private readonly string _typeName;
        private readonly string _extension;
        private readonly string _path;

        public static SequencedFile Create(FileSystemEventArgs args)
        {
            return new SequencedFile(args.FullPath);
        }

        public SequencedFile(string directory, int sequenceId, string typeName, string extension)
        {
            _directory = directory;
            _sequenceId = sequenceId;
            _typeName = typeName;
            _extension = extension.StartsWith(".") ? extension : string.Format(".{0}", extension);
            _path = Path.Combine(_directory, string.Format("{0}-{1}{2}", _sequenceId.ToString("D10"), _typeName, _extension));
        }

        public SequencedFile(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName == null)
            {
                throw new Exception("Invalid path: " + path);
            }
            string[] split = fileName.Split('-');
            _sequenceId = Convert.ToInt32(split[0].TrimStart('0'));
            _typeName = split[1];
            _directory = Path.GetDirectoryName(path);
            _extension = Path.GetExtension(path);
            _path = path;
        }

        public int SequenceId
        {
            get { return _sequenceId; }
        }

        public string TypeName
        {
            get { return _typeName; }
        }

        public string Extension
        {
            get { return _extension; }
        }

        public object Read()
        {
            return IsTextFile ? (object)ReadText() : ReadBytes();
        }

        public void Write(object data)
        {
            if (IsTextFile)
            {
                WriteText(data);
            }
            else
            {
                WriteBytes(data);
            }
        }

        private string ReadText()
        {
            return File.ReadAllText(_path);
        }

        private byte[] ReadBytes()
        {
            return File.ReadAllBytes(_path);
        }

        private void WriteBytes(object data)
        {
            File.WriteAllBytes(_path, (byte[]) data);
        }

        private void WriteText(object data)
        {
            File.WriteAllText(_path, (string) data);
        }

        private bool IsTextFile
        {
            get { return Extension == TextExtension; }
        }
    }
}
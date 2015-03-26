using System;
using System.IO;
using System.Linq;

namespace Obvs.FileSystem.Utils
{
    public interface ISequencedDirectory
    {
        int ReadCurrentSequenceId();
        string DirectoryPath { get; }
    }

    public class SequencedDirectory : ISequencedDirectory
    {
        private readonly string _path;

        public SequencedDirectory(string path)
        {
            _path = path;
            Directory.CreateDirectory(_path);
        }

        public int ReadCurrentSequenceId()
        {
            var files = Directory.GetFiles(_path);
            if (files.Any())
            {
                return files.Select(SequenceNumber).Max();
            }
            return 0;
        }

        public string DirectoryPath { get { return _path; } }

        private int SequenceNumber(string fileName)
        {
            var name = Path.GetFileName(fileName);
            if (name != null)
            {
                var value = name.Split('-')[0].TrimStart('0');
                return Convert.ToInt32(value);
            }
            return 0;
        }
    }
}
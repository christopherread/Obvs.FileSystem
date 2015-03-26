using System;
using System.IO;
using System.Reactive.Linq;

namespace Obvs.FileSystem.Utils
{
    internal interface IFileSystemWatcher : IDisposable
    {
        IObservable<FileSystemEventArgs> Created { get; }
    }

    internal class ObservableFileSystemWatcher : IFileSystemWatcher
    {
        private readonly FileSystemWatcher _fileSystemWatcher;

        public ObservableFileSystemWatcher(string path, string filter = "*.*")
        {
            Directory.CreateDirectory(path);

            _fileSystemWatcher = new FileSystemWatcher(path, filter)
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };
        }

        public IObservable<FileSystemEventArgs> Created
        {
            get
            {
                return Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
                    handler => (sender, e) => handler(e),
                    handler => _fileSystemWatcher.Created += handler,
                    handler => _fileSystemWatcher.Created -= handler);
            }
        }

        public void Dispose()
        {
            _fileSystemWatcher.Dispose();
        }
    }
}
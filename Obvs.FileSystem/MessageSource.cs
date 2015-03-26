using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Obvs.FileSystem.Utils;
using Obvs.Types;

namespace Obvs.FileSystem
{
    public class MessageSource<TMessage> : IMessageSource<TMessage> where TMessage : IMessage
    {
        private readonly IScheduler _scheduler;
        private readonly IDictionary<string, IMessageDeserializer<TMessage>> _deserializers;
        private readonly IDisposable _subscription;
        private readonly IContiguousBuffer _buffer;
        private readonly IFileSystemWatcher _fileSystemWatcher;

        public MessageSource(ISequencedDirectory directory, IEnumerable<IMessageDeserializer<TMessage>> deserializers, IScheduler scheduler)
        {
            _scheduler = scheduler;
            _deserializers = deserializers.ToDictionary(d => d.GetTypeName());
           
            if (!_deserializers.Any())
            {
                throw new Exception("You need to supply at least one deserializer");
            }

            _fileSystemWatcher = new ObservableFileSystemWatcher(directory.DirectoryPath);

            _buffer = new ContiguousBuffer(scheduler, directory);
           
            _subscription = _fileSystemWatcher.Created
                .ObserveOn(_scheduler)
                .Select(SequencedFile.Create)
                .Subscribe(_buffer.Add);
        }

        public void Dispose()
        {
            _subscription.Dispose();
            _buffer.Dispose();
            _fileSystemWatcher.Dispose();
        }

        public IObservable<TMessage> Messages
        {
            get { return _buffer.Files.ObserveOn(_scheduler).Select(Deserialize); }
        }

        private TMessage Deserialize(SequencedFile file)
        {
            return _deserializers[file.TypeName].Deserialize(file.Read());
        }
    }
}
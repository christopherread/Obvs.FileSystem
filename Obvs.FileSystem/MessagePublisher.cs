using System.IO;
using System.Reactive.Concurrency;
using System.Threading;
using Obvs.FileSystem.Utils;
using Obvs.Types;

namespace Obvs.FileSystem
{
    public class MessagePublisher<TMessage> : IMessagePublisher<TMessage> where TMessage : IMessage
    {
        private readonly ISequencedDirectory _directory;
        private readonly IMessageSerializer _serializer;
        private readonly IScheduler _scheduler;
        
        // ReSharper disable StaticFieldInGenericType
        private static int _sequenceId;
        // ReSharper restore StaticFieldInGenericType

        public MessagePublisher(ISequencedDirectory directory, IMessageSerializer serializer, IScheduler scheduler)
        {
            _directory = directory;
            _serializer = serializer;
            _scheduler = scheduler;

            Init();
        }

        private void Init()
        {
            if (_sequenceId == 0)
            {
                int sequenceId = _directory.ReadCurrentSequenceId();
                Interlocked.CompareExchange(ref _sequenceId, sequenceId, 0);
            }
        }

        private void SaveToFile(TMessage message)
        {
            object data = _serializer.Serialize(message);
            string extension = data is string ? SequencedFile.TextExtension : SequencedFile.BinaryExtension;

            SequencedFile file = new SequencedFile(_directory.DirectoryPath, Interlocked.Increment(ref _sequenceId), message.GetType().Name, extension);
            file.Write(data);
        }

        public void Publish(TMessage message)
        {
            _scheduler.Schedule(() => SaveToFile(message));
        }

        public void Dispose()
        {
        }
    }
}

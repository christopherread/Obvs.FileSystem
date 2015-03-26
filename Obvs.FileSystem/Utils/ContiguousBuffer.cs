using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Obvs.FileSystem.Utils
{
    internal interface IContiguousBuffer : IDisposable
    {
        IObservable<SequencedFile> Files { get; }
        void Add(SequencedFile file);
    }

    internal class ContiguousBuffer : IContiguousBuffer
    {
        private readonly IScheduler _scheduler;
        private readonly Dictionary<int, SequencedFile> _buffer = new Dictionary<int, SequencedFile>();
        private int _lastId;
        private int _bufferCheckSum;
        private readonly Subject<SequencedFile> _files = new Subject<SequencedFile>();

        public ContiguousBuffer(IScheduler scheduler, ISequencedDirectory directory)
        {
            _scheduler = scheduler;
            _lastId = directory.ReadCurrentSequenceId();
            _files.Subscribe(file => _lastId = file.SequenceId);
        }

        public IObservable<SequencedFile> Files
        {
            get { return _files; }
        }

        public void Add(SequencedFile file)
        {
            int id = file.SequenceId;

            if (id <= _lastId)
            {
                return; // duplicate
            }

            int expectedId = _lastId + 1;
            bool receivedNextInSequence = expectedId == id;
            bool needToBuffer = !receivedNextInSequence || _bufferCheckSum != 0;
            if (!needToBuffer)
            {
                _files.OnNext(file);
                _lastId = id;
                return;
            }

            _buffer[id] = file;
            _bufferCheckSum += id;

            bool bufferIsContiguous = _bufferCheckSum == ExpectedCheckSum(expectedId, _buffer.Count);

            if (receivedNextInSequence && bufferIsContiguous)
            {
                SendBuffer();
            }
            else
            {
                Observable.Timer(TimeSpan.FromSeconds(3), _scheduler).Subscribe(i => GiveUpAndSendBuffer(_lastId));
            }
        }

        private void SendBuffer()
        {
            var buffer = _buffer.OrderBy(pair => pair.Key).ToArray();
            foreach (var keyValuePair in buffer)
            {
                _files.OnNext(keyValuePair.Value);
            }
            _lastId = buffer.Last().Key;
            _buffer.Clear();
            _bufferCheckSum = 0;
        }

        private void GiveUpAndSendBuffer(int lastId)
        {
            if (_lastId == lastId)
            {
                SendBuffer();
            }
        }

        private static int ExpectedCheckSum(int id, int length)
        {
            return Enumerable.Range(id, length).Sum();
        }

        public void Dispose()
        {
            _files.Dispose();
        }
    }
}
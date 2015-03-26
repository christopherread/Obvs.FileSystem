using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using NUnit.Framework;
using Obvs.FileSystem.Utils;
using Obvs.Serialization.Json;
using Obvs.Types;

namespace Obvs.FileSystem.Tests
{
    [TestFixture]
    public class TestSendReceiveFileSystem
    {
        [Test, Explicit, Timeout(5000)]
        public void Test()
        {
            const string rootDirectory = "C:\\Temp";

            IMessageSource<IMessage> source = new MessageSource<IMessage>(new SequencedDirectory(rootDirectory),
                new List<IMessageDeserializer<IMessage>> {new JsonMessageDeserializer<TestMessage>()},
                new EventLoopScheduler());

            source.Messages.Subscribe(Console.WriteLine, ex => Console.WriteLine(ex));

            IMessagePublisher<IMessage> publisher = new MessagePublisher<IMessage>(new SequencedDirectory(rootDirectory),
                new JsonMessageSerializer(), new EventLoopScheduler());

            for (int i = 0; i < 20; ++i)
            {
                publisher.Publish(new TestMessage {Data = string.Format("Blah{0}", i)});
            }

            source.Messages.Take(20).Wait();
        }
    }

    public interface ITestMessage : IMessage { }

    public class TestMessage : ITestMessage
    {
        public DateTime Timestamp { get; set; }
        public string Data { get; set; }

        public TestMessage()
        {
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            return string.Format("TestMessage(Timestamp={0},Data={1})", Timestamp.ToString("G"), Data);
        }
    }
}

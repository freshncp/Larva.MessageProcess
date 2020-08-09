using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Larva.MessageProcess.Mailboxes;
using Larva.MessageProcess.Messaging;
using Larva.MessageProcess.Processing;
using Xunit;

namespace Larva.MessageProcess.Tests.Mailboxes
{
    public class MailboxTest
    {
        [Fact]
        public void InitializeWithEmptyBusinessKeyIsNotAllowed()
        {
            IProcessingMessageMailbox mailbox = new DefaultProcessingMessageMailbox();
            var processingMessageHandler = new MockupProcessingMessageHandler("");
            Assert.Throws<ArgumentNullException>(() =>
            {
                mailbox.Initialize("", "", processingMessageHandler, false, 1, 2);
            });
        }

        [Fact]
        public void EnqueueNullIsNotAllowed()
        {
            IProcessingMessageMailbox mailbox = new DefaultProcessingMessageMailbox();
            var processingMessageHandler = new MockupProcessingMessageHandler("");
            mailbox.Initialize("B001", "", processingMessageHandler, false, 1, 2);
            Assert.Throws<ArgumentNullException>(() =>
            {
                mailbox.Enqueue(null);
            });
        }

        [Fact]
        public void EnqueueDifferentBussinessKeyIsNotAllowed()
        {
            IProcessingMessageMailbox mailbox = new DefaultProcessingMessageMailbox();
            var processingMessageHandler = new MockupProcessingMessageHandler("");
            mailbox.Initialize("B001", "", processingMessageHandler, false, 1, 2);
            Assert.Throws<InvalidOperationException>(() =>
            {
                mailbox.Enqueue(new ProcessingMessage(new MessageA { Id = "1", Timestamp = DateTime.Now, BusinessKey = "B002" }, new MockupMessageExecutingContext()));
            });
        }

        [Fact]
        public void MustSequentialProcessing()
        {
            IProcessingMessageMailbox mailbox = new DefaultProcessingMessageMailbox();
            var processingMessageHandler = new MockupProcessingMessageHandler("");
            mailbox.Initialize("B001", "", processingMessageHandler, false, 1, 5);
            var testCount = 20;
            for (var i = 0; i < testCount; i++)
            {
                mailbox.Enqueue(new ProcessingMessage(new MessageA { Id = $"M{i + 1}", Timestamp = DateTime.Now, BusinessKey = "B001" }, new MockupMessageExecutingContext()));
            }
            var processedMessages = processingMessageHandler.GetProcessedMessages();
            while (processedMessages.Length < testCount)
            {
                Thread.Sleep(100);
                processedMessages = processingMessageHandler.GetProcessedMessages();
            }
            for (var i = 0; i < testCount; i++)
            {
                Assert.Equal(i + 1, processedMessages[i].Sequence);
            }
        }

        public class MessageA : IMessage
        {
            public string Id { get; set; }
            public DateTime Timestamp { get; set; }
            public string BusinessKey { get; set; }
            public IDictionary<string, string> ExtraDatas { get; set; }
        }

        public class MessageB : IMessage
        {
            public string Id { get; set; }
            public DateTime Timestamp { get; set; }
            public string BusinessKey { get; set; }
            public IDictionary<string, string> ExtraDatas { get; set; }
        }

        public class MockupMessageExecutingContext : IMessageExecutingContext
        {
            private string _result;
            public string GetResult()
            {
                return _result;
            }

            public async Task NotifyMessageExecutedAsync(MessageExecutingResult messageResult)
            {
                await Task.Delay(10);
            }

            public void SetResult(string result)
            {
                _result = result;
            }
        }

        public class MockupProcessingMessageHandler : IProcessingMessageHandler
        {
            private string _subscriber;
            private ConcurrentQueue<ProcessingMessage> _queue = new ConcurrentQueue<ProcessingMessage>();

            public MockupProcessingMessageHandler(string subscriber)
            {
                _subscriber = subscriber;
            }

            public async Task<bool> HandleAsync(ProcessingMessage processingMessage)
            {
                _queue.Enqueue(processingMessage);
                await Task.Delay(10);
                await processingMessage.CompleteAsync(new MessageExecutingResult(MessageExecutingStatus.Success, processingMessage.Message, _subscriber));
                return true;
            }

            public ProcessingMessage[] GetProcessedMessages()
            {
                return _queue.ToArray();
            }
        }
    }
}
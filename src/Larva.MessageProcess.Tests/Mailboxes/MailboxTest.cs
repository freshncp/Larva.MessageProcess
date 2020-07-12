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
            var processingMessageHandler = new MockupProcessingMessageHandler();
            Assert.Throws<ArgumentNullException>(() =>
            {
                mailbox.Initialize("", processingMessageHandler, 2);
            });
        }

        [Fact]
        public void EnqueueNullIsNotAllowed()
        {
            IProcessingMessageMailbox mailbox = new DefaultProcessingMessageMailbox();
            var processingMessageHandler = new MockupProcessingMessageHandler();
            mailbox.Initialize("B001", processingMessageHandler, 2);
            Assert.Throws<ArgumentNullException>(() =>
            {
                mailbox.Enqueue(null);
            });
        }

        [Fact]
        public void EnqueueDifferentBussinessKeyIsNotAllowed()
        {
            IProcessingMessageMailbox mailbox = new DefaultProcessingMessageMailbox();
            var processingMessageHandler = new MockupProcessingMessageHandler();
            mailbox.Initialize("B001", processingMessageHandler, 2);
            Assert.Throws<InvalidOperationException>(() =>
            {
                mailbox.Enqueue(new ProcessingMessage(new MessageA { Id = "1", Timestamp = DateTime.Now, BusinessKey = "B002" }, string.Empty, new MockupMessageExecutingContext()));
            });
        }

        [Fact]
        public void MustSequentialProcessing()
        {
            IProcessingMessageMailbox mailbox = new DefaultProcessingMessageMailbox();
            var processingMessageHandler = new MockupProcessingMessageHandler();
            mailbox.Initialize("B001", processingMessageHandler, 10);
            var testCount = 30;
            for (var i = 0; i < testCount; i++)
            {
                mailbox.Enqueue(new ProcessingMessage(new MessageA { Id = $"M{i + 1}", Timestamp = DateTime.Now, BusinessKey = "B001" }, string.Empty, new MockupMessageExecutingContext()));
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

            public Task NotifyMessageExecutedAsync(MessageExecutingResult messageResult)
            {
                return Task.CompletedTask;
            }

            public void SetResult(string result)
            {
                _result = result;
            }
        }

        public class MockupProcessingMessageHandler : IProcessingMessageHandler
        {
            private ConcurrentQueue<ProcessingMessage> _queue = new ConcurrentQueue<ProcessingMessage>();
            public async Task HandleAsync(ProcessingMessage processingMessage)
            {
                _queue.Enqueue(processingMessage);
                await processingMessage.CompleteAsync(new MessageExecutingResult(MessageExecutingStatus.Success, processingMessage.Message, processingMessage.MessageSubscriber));
            }

            public ProcessingMessage[] GetProcessedMessages()
            {
                return _queue.ToArray();
            }
        }
    }
}
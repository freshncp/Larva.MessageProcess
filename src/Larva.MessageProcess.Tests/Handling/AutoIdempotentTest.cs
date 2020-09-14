using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Larva.MessageProcess.Handling;
using Larva.MessageProcess.Handling.AutoIdempotent;
using Larva.MessageProcess.Messaging;
using Xunit;

namespace Larva.MessageProcess.Tests.Handling
{
    public class AutoIdempotentTest
    {
        [Fact]
        public void TestSave()
        {
            IAutoIdempotentStore store = new InMemoryAutoIdempotentStore();
            var message = new MessageA { Id = Guid.NewGuid().ToString(), BusinessKey = "Key1" };
            
            Assert.False(store.Exists(message, typeof(MessageHandler1)));
            Assert.False(store.Exists(message, typeof(MessageHandler2)));
            store.Save(message, typeof(MessageHandler1));
            Assert.True(store.Exists(message, typeof(MessageHandler1)));
            Assert.False(store.Exists(message, typeof(MessageHandler2)));
        }

        public class MessageA : IMessage
        {
            public string Id { get; set; }
            public DateTime Timestamp { get; set; }
            public string BusinessKey { get; set; }
            public IDictionary<string, string> ExtraDatas { get; set; }
        }

        public class MessageHandler1 : IMessageHandler<MessageA>
        {
            public Task HandleAsync(MessageA message, IMessageContext ctx)
            {
                ctx.SetResult($"MessageA_{message.Id}");
                return Task.CompletedTask;
            }
        }

        public class MessageHandler2 : IMessageHandler<MessageA>
        {
            public Task HandleAsync(MessageA message, IMessageContext ctx)
            {
                ctx.SetResult($"MessageA_{message.Id}");
                return Task.CompletedTask;
            }
        }
    }
}
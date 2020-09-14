using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Larva.DynamicProxy.Interception;
using Larva.MessageProcess.Handling;
using Larva.MessageProcess.Handling.AutoIdempotent;
using Larva.MessageProcess.Messaging;
using Xunit;

namespace Larva.MessageProcess.Tests.Handling
{
    public class MessageHandlerProxyTest
    {
        [Fact]
        public void NewWithNullOfMessageHandlerMustThrowException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var proxy = new MessageHandlerProxy<MessageA>(null, null);
            });
        }

        [Fact]
        public void HandleWithNoInterceptor()
        {
            var proxy = new MessageHandlerProxy<MessageA>(new MessageHandler1(), null);
            var message = new MessageA { Id = Guid.NewGuid().ToString(), BusinessKey = "Key1" };
            var ctx = new MockupMessageContext();
            proxy.HandleAsync(message, ctx).Wait();
            Assert.Equal($"MessageA_{message.Id}", ctx.GetResult());
        }

        [Fact]
        public void HandleWithOneInterceptor()
        {
            var proxy = new MessageHandlerProxy<MessageA>(new MessageHandler1(),
                new IInterceptor[] {
                    new InterceptorA()
                });
            var message = new MessageA { Id = Guid.NewGuid().ToString(), BusinessKey = "Key1" };
            var ctx = new MockupMessageContext();
            proxy.HandleAsync(message, ctx).Wait();
            Assert.Equal($"MessageA_{message.Id}", ctx.GetResult());
        }

        [Fact]
        public void HandleWithTwoInterceptor()
        {
            var proxy = new MessageHandlerProxy<MessageA>(new MessageHandler1(),
                new IInterceptor[] {
                    new InterceptorA(),
                    new InterceptorB()
                });
            var message = new MessageA { Id = Guid.NewGuid().ToString(), BusinessKey = "Key1" };
            var ctx = new MockupMessageContext();
            proxy.HandleAsync(message, ctx).Wait();
            Assert.Equal($"MessageA_{message.Id}", ctx.GetResult());
        }

        [Fact]
        public void HandleWithAutoIdempotentInterceptor()
        {
            var proxy = new MessageHandlerProxy<MessageA>(new MessageHandler1(),
                new IInterceptor[] {
                    new AutoIdempotentInterceptor(new InMemoryAutoIdempotentStore())
                });
            var message = new MessageA { Id = Guid.NewGuid().ToString(), BusinessKey = "Key1" };
            var ctx = new MockupMessageContext();
            proxy.HandleAsync(message, ctx).Wait();
            Assert.Equal($"MessageA_{message.Id}", ctx.GetResult());
            Assert.Throws<DuplicateMessageHandlingException>(() =>
            {
                proxy.HandleAsync(message, ctx).Wait();
            });
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

        public class InterceptorA : IInterceptor
        {
            public void Intercept(IInvocation invocation)
            {
                invocation.Proceed();
            }
        }

        public class InterceptorB : IInterceptor
        {
            public void Intercept(IInvocation invocation)
            {
                invocation.Proceed();
            }
        }

        public class MockupMessageContext : IMessageContext
        {
            private string _result;

            public string GetResult()
            {
                return _result;
            }

            public void SetResult(string result)
            {
                _result = result;
            }
        }
    }
}

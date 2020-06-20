using Larva.DynamicProxy.Interception;
using Larva.MessageProcess.Messaging;
using System;
using System.Threading.Tasks;

namespace Larva.MessageProcess.Handlers
{
    internal class MessageHandlerProxy<TMessage> : IMessageHandlerProxy where TMessage : class, IMessage
    {
        private readonly IMessageHandler<TMessage> _messageHandler;
        private readonly IInterceptor[] _interceptors;

        public MessageHandlerProxy(IMessageHandler<TMessage> messageHandler, IInterceptor[] interceptors)
        {
            _messageHandler = messageHandler;
            _interceptors = interceptors;
        }

        public Task HandleAsync(IMessage message, IMessageContext ctx)
        {
            if (_interceptors != null)
            {
                var arguments = new object[] { message, ctx };
                var argumentTypes = new Type[] { typeof(TMessage), typeof(IMessageContext) };
                var invocation = new MessageHandlerInvocation(_interceptors, nameof(_messageHandler.HandleAsync), typeof(TMessage),
                    _messageHandler, (m, c) => _messageHandler.HandleAsync((TMessage)m, c),
                    this, arguments);
                invocation.Proceed();
                return (Task)invocation.ReturnValue;
            }
            else
            {
                return _messageHandler.HandleAsync(message as TMessage, ctx);
            }
        }

        public object GetWrappedObject()
        {
            return _messageHandler;
        }
    }
}

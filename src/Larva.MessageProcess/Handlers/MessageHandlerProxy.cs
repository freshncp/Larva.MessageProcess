using Larva.MessageProcess.Interception;
using Larva.MessageProcess.Messaging;
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
            var handler = _messageHandler as IMessageHandler<TMessage>;
            if (_interceptors != null)
            {
                var invocation = new MessageHandlerInvocation(_interceptors, new object[] { message, ctx }, handler, nameof(handler.HandleAsync), (m, c) => handler.HandleAsync((TMessage)m, c), this, nameof(MessageHandlerProxy<TMessage>.HandleAsync), HandleAsync);
                invocation.Proceed();
                return (Task)invocation.ReturnValue;
            }
            else
            {
                return handler.HandleAsync(message as TMessage, ctx);
            }
        }

        public object GetWrappedObject()
        {
            return _messageHandler;
        }
    }
}

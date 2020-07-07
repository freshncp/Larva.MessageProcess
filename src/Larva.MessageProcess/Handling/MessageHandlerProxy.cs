using Larva.DynamicProxy.Interception;
using Larva.MessageProcess.Messaging;
using System;
using System.Threading.Tasks;

namespace Larva.MessageProcess.Handling
{
    /// <summary>
    /// 消息处理器代理
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public sealed class MessageHandlerProxy<TMessage> : IMessageHandlerProxy where TMessage : class, IMessage
    {
        private readonly IMessageHandler<TMessage> _messageHandler;
        private readonly IInterceptor[] _interceptors;

        /// <summary>
        /// 消息处理器代理
        /// </summary>
        /// <param name="messageHandler"></param>
        /// <param name="interceptors"></param>
        public MessageHandlerProxy(IMessageHandler<TMessage> messageHandler, params IInterceptor[] interceptors)
        {
            if (messageHandler == null)
            {
                throw new ArgumentNullException(nameof(messageHandler));
            }
            _messageHandler = messageHandler;
            _interceptors = interceptors;
        }

        /// <summary>
        /// 处理
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public Task HandleAsync(IMessage message, IMessageContext ctx)
        {
            if (_interceptors != null)
            {
                var arguments = new object[] { message, ctx };
                var argumentTypes = new Type[] { typeof(TMessage), typeof(IMessageContext) };
                var invocation = new MessageHandlerInvocation(_interceptors, typeof(TMessage),
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

        /// <summary>
        /// 获取被代理的对象
        /// </summary>
        /// <returns></returns>
        public object GetProxiedObject()
        {
            return _messageHandler;
        }
    }
}

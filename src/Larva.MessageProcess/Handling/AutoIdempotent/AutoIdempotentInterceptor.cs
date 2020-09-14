using Larva.DynamicProxy.Interception;
using Larva.MessageProcess.Messaging;
using System.Collections.Generic;

namespace Larva.MessageProcess.Handling.AutoIdempotent
{
    /// <summary>
    /// 自动幂等 拦截器
    /// </summary>
    public class AutoIdempotentInterceptor : StandardInterceptor
    {
        private IAutoIdempotentStore _autoIdempotentStore;
        private IDictionary<string, bool> _excludeMessageTypeDict;

        /// <summary>
        /// 自动幂等 拦截器
        /// </summary>
        /// <param name="autoIdempotentStore">自动幂等存储</param>
        /// <param name="excludeMessageTypeDict">排除的消息类型字典</param>
        public AutoIdempotentInterceptor(IAutoIdempotentStore autoIdempotentStore, IDictionary<string, bool> excludeMessageTypeDict = null)
        {
            _autoIdempotentStore = autoIdempotentStore;
            _excludeMessageTypeDict = excludeMessageTypeDict;
        }

        /// <summary>
        /// 处理前
        /// </summary>
        /// <param name="invocation"></param>
        protected override void PreProceed(IInvocation invocation)
        {
            if (invocation.ArgumentTypes.Length == 2
                && typeof(IMessage).IsAssignableFrom(invocation.ArgumentTypes[0])
                && typeof(IMessageHandler<>).MakeGenericType(invocation.ArgumentTypes[0]).IsInstanceOfType(invocation.InvocationTarget))
            {
                var message = (IMessage)invocation.Arguments[0];
                if (_excludeMessageTypeDict != null
                    && _excludeMessageTypeDict.ContainsKey(message.GetMessageTypeName()))
                {
                    return;
                }

                var messageHandleType = invocation.InvocationTarget.GetType();
                if (_autoIdempotentStore.Exists(message, messageHandleType))
                {
                    throw new DuplicateMessageHandlingException(message, messageHandleType);
                }
            }
        }

        /// <summary>
        /// 处理后
        /// </summary>
        /// <param name="invocation"></param>
        protected override void PostProceed(IInvocation invocation)
        {
            if (invocation.ArgumentTypes.Length == 2
                && typeof(IMessage).IsAssignableFrom(invocation.ArgumentTypes[0])
                && typeof(IMessageHandler<>).MakeGenericType(invocation.ArgumentTypes[0]).IsInstanceOfType(invocation.InvocationTarget))
            {
                var message = (IMessage)invocation.Arguments[0];
                if (_excludeMessageTypeDict != null
                    && _excludeMessageTypeDict.ContainsKey(message.GetMessageTypeName()))
                {
                    return;
                }

                var messageHandleType = invocation.InvocationTarget.GetType();
                _autoIdempotentStore.Save(message, messageHandleType);
            }
        }
    }
}
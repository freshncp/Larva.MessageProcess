using Larva.DynamicProxy.Interception;
using Larva.MessageProcess.Messaging;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Larva.MessageProcess.Handling
{
    /// <summary>
    /// IMessageHandler.HandleAsync 调用
    /// </summary>
    public sealed class MessageHandlerInvocation : InvocationBase
    {
        private Func<IMessage, IMessageContext, Task> _methodInvocationTargetFunc;

        /// <summary>
        /// IMessageHandler.HandleAsync 调用
        /// </summary>
        /// <param name="interceptors">拦截器</param>
        /// <param name="messageType">消息类型</param>
        /// <param name="messageHandler">消息处理器</param>
        /// <param name="handleFunc">处理函数</param>
        /// <param name="messageHandlerProxy">消息处理器代理</param>
        /// <param name="arguments">消息处理参数</param>
        public MessageHandlerInvocation(IInterceptor[] interceptors, Type messageType, object messageHandler, Func<IMessage, IMessageContext, Task> handleFunc, object messageHandlerProxy, object[] arguments)
            : base(interceptors, MemberTypes.Method, "HandleAsync", MemberOperateTypes.None, new Type[] { messageType, typeof(IMessageContext) }, Type.EmptyTypes, typeof(Task), messageHandler, messageHandlerProxy, arguments)
        {
            _methodInvocationTargetFunc = handleFunc;
        }

        /// <summary>
        /// 调用目标对象
        /// </summary>
        /// <returns></returns>
        protected override object InvokeInvocationTarget()
        {
            return _methodInvocationTargetFunc.Invoke((IMessage)Arguments[0], (IMessageContext)Arguments[1]);
        }
    }
}

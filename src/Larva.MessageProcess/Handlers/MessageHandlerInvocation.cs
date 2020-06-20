using Larva.DynamicProxy.Interception;
using Larva.MessageProcess.Messaging;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Larva.MessageProcess.Handlers
{
    internal sealed class MessageHandlerInvocation : InvocationBase
    {
        public MessageHandlerInvocation(IInterceptor[] interceptors, string methodName, Type messageType, object invocationTarget, Func<IMessage, IMessageContext, Task> methodInvocationTargetFunc, object proxy, object[] arguments)
            : base(interceptors, MemberTypes.Method, methodName, MemberOperateTypes.None, new Type[] { messageType, typeof(IMessageContext) }, Type.EmptyTypes, typeof(Task), invocationTarget, proxy, arguments)
        {
            MethodInvocationTargetFunc = methodInvocationTargetFunc;
        }

        public Func<IMessage, IMessageContext, Task> MethodInvocationTargetFunc { get; private set; }

        protected override object InvokeInvocationTarget()
        {
            return MethodInvocationTargetFunc.Invoke((IMessage)Arguments[0], (IMessageContext)Arguments[1]);
        }
    }
}

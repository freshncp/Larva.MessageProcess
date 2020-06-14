using Larva.MessageProcess.Interception;
using Larva.MessageProcess.Messaging;
using System;
using System.Threading.Tasks;

namespace Larva.MessageProcess.Handlers
{
    internal sealed class MessageHandlerInvocation : InvocationBase
    {
        public MessageHandlerInvocation(IInterceptor[] interceptors, string methodName, Type messageType, object invocationTarget, Func<IMessage, IMessageContext, Task> methodInvocationTargetFunc, object proxy, object[] arguments)
            : base(interceptors, methodName, new Type[] { messageType, typeof(IMessageContext) }, typeof(Task), invocationTarget, proxy, arguments)
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

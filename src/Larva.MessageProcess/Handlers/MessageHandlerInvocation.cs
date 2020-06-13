using Larva.MessageProcess.Interception;
using Larva.MessageProcess.Messaging;
using System;
using System.Threading.Tasks;

namespace Larva.MessageProcess.Handlers
{
    internal sealed class MessageHandlerInvocation : InvocationBase<Task>
    {
        public MessageHandlerInvocation(IInterceptor[] interceptors, object[] arguments, object invocationTarget, string methodNameInvocationTarget, Func<IMessage, IMessageContext, Task> methodInvocationTargetFunc, object proxy, string methodName, Func<IMessage, IMessageContext, Task> methodFunc)
            : base(interceptors, arguments, invocationTarget, methodNameInvocationTarget, proxy, methodName)
        {
            MethodInvocationTargetFunc = methodInvocationTargetFunc;
            MethodFunc = methodFunc;
        }

        public Func<IMessage, IMessageContext, Task> MethodInvocationTargetFunc { get; private set; }

        public Func<IMessage, IMessageContext, Task> MethodFunc { get; private set; }

        protected override Task InvokeInvocationTarget()
        {
            return MethodInvocationTargetFunc.Invoke((IMessage)Arguments[0], (IMessageContext)Arguments[1]);
        }
    }
}

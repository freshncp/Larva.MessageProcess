using Larva.MessageProcess.Interception;
using System;

namespace Larva.MessageProcess.RabbitMQ.Tests
{
    public class PerformanceCounterInterceptor : StandardInterceptor
    {
        protected override void PreProceed(IInvocation invocation)
        {
            Console.WriteLine($"Performance PreProceed {invocation.InvocationTarget.GetType().FullName}.{invocation.MethodNameInvocationTarget}");
        }

        protected override void PostProceed(IInvocation invocation)
        {
            Console.WriteLine($"Performance PostProceed {invocation.InvocationTarget.GetType().FullName}.{invocation.MethodNameInvocationTarget}");
        }

        protected override void ExceptionThrown(IInvocation invocation, Exception ex)
        {
            Console.WriteLine($"Performance ExceptionThrown {invocation.InvocationTarget.GetType().FullName}.{invocation.MethodNameInvocationTarget} {ex.Message}\r\n{ex.StackTrace}");
        }
    }
}
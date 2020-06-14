using Larva.MessageProcess.Interception;
using System;
using System.Linq;

namespace Larva.MessageProcess.RabbitMQ.Tests
{
    public class PerformanceCounterInterceptor : StandardInterceptor
    {
        protected override void PreProceed(IInvocation invocation)
        {
            Console.WriteLine($"Performance PreProceed {invocation.InvocationTarget.GetType().FullName}.{invocation.MethodName}({string.Join(", ", invocation.ArgumentTypes.Select(s => s.Name))})");
        }

        protected override void PostProceed(IInvocation invocation)
        {
            Console.WriteLine($"Performance PostProceed {invocation.InvocationTarget.GetType().FullName}.{invocation.MethodName}({string.Join(", ", invocation.ArgumentTypes.Select(s => s.Name))})");
        }
    }
}
using Larva.MessageProcess.Handling;
using Larva.MessageProcess.Handling.Attributes;
using Larva.MessageProcess.RabbitMQ.Eventing;
using Larva.MessageProcess.RabbitMQ.Tests.DomainEvents;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Larva.MessageProcess.RabbitMQ.Tests.DomainEventHandlers
{
    [HandlePriority(1)]
    public class DomainEventHandler1
        : IEventHandler<DomainEvent1>
    {
        private volatile int _globalSequence;
        public async Task HandleAsync(DomainEvent1 message, IMessageContext ctx)
        {
            await Task.Delay(20);
            if (DateTime.Now.Millisecond % 10 == 0)
            {
                throw new ApplicationException("Random exception occurred.");
            }
            var globalSequence = Interlocked.Increment(ref _globalSequence);
            ctx.SetResult($"DomainEventHandler1 sequence={globalSequence}");
            Console.WriteLine($"{message.Id} DomainEventHandler1 handle DomainEvent1");
        }
    }
}
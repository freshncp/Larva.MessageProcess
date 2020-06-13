using Larva.MessageProcess.Handlers;
using Larva.MessageProcess.Handlers.Attributes;
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
            await Task.Delay(100);
            var globalSequence = Interlocked.Increment(ref _globalSequence);
            ctx.SetResult($"DomainEventHandler1 sequence={globalSequence}");
            Console.WriteLine($"{message.Id} DomainEventHandler1 handle DomainEvent1");
        }
    }
}
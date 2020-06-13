using Larva.MessageProcess.Handlers;
using Larva.MessageProcess.Handlers.Attributes;
using Larva.MessageProcess.RabbitMQ.Eventing;
using Larva.MessageProcess.RabbitMQ.Tests.DomainEvents;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Larva.MessageProcess.RabbitMQ.Tests.DomainEventHandlers
{
    [MessageSubscriber("Subscriber2")]
    public class DomainEventHandler2
        : IEventHandler<DomainEvent1>
    {
        private volatile int _globalSequence;
        public async Task HandleAsync(DomainEvent1 message, IMessageContext ctx)
        {
            await Task.Delay(100);
            var globalSequence = Interlocked.Increment(ref _globalSequence);
            ctx.SetResult($"DomainEventHandler2 sequence={globalSequence}");
            Console.WriteLine($"{message.Id} DomainEventHandler2 handle DomainEvent1");
        }
    }
}
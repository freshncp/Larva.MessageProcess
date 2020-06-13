using System.Threading.Tasks;

namespace Larva.MessageProcess.RabbitMQ.Eventing
{
    public static class IEventBusExtensions
    {
        public static async Task PublishAsync(this IEventBus eventBus, IDomainEvent domainEvent)
        {
            var eventStream = new EventStream(domainEvent.Id, domainEvent.Timestamp, domainEvent.BusinessKey, domainEvent.ExtraDatas, new IDomainEvent[] { domainEvent });
            await eventBus.PublishAsync(eventStream);
        }
    }
}
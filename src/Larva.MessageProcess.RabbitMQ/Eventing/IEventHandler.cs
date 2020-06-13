using Larva.MessageProcess.Handlers;

namespace Larva.MessageProcess.RabbitMQ.Eventing
{
    public interface IEventHandler<TDomainEvent> : IMessageHandler<TDomainEvent>
        where TDomainEvent : class, IDomainEvent
    { }
}
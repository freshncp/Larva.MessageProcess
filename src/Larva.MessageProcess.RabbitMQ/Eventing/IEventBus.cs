using System.Threading.Tasks;

namespace Larva.MessageProcess.RabbitMQ.Eventing
{
    public interface IEventBus
    {
        Task PublishAsync(EventStream eventStream);
    }
}
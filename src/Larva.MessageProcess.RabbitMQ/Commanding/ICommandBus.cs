using System.Threading.Tasks;

namespace Larva.MessageProcess.RabbitMQ.Commanding
{
    public interface ICommandBus
    {
        Task SendAsync(ICommand command);
    }
}
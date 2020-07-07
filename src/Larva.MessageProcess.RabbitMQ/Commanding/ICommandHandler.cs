using Larva.MessageProcess.Handling;

namespace Larva.MessageProcess.RabbitMQ.Commanding
{
    public interface ICommandHandler<TCommand> : IMessageHandler<TCommand>
       where TCommand : class, ICommand
    { }
}

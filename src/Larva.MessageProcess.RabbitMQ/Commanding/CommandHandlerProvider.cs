using Larva.MessageProcess.Handling;
using System;

namespace Larva.MessageProcess.RabbitMQ.Commanding
{
    public class CommandHandlerProvider : MessageHandlerProviderBase
    {
        protected override bool AllowMultipleMessageHandlers => false;

        protected override Type GetMessageHandlerInterfaceGenericType()
        {
            return typeof(ICommandHandler<>);
        }
    }
}

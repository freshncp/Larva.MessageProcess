using Larva.MessageProcess.Handling;
using System;

namespace Larva.MessageProcess.RabbitMQ.Eventing
{
    public class EventHandlerProvider : MessageHandlerProviderBase
    {
        protected override bool AllowMultipleMessageHandlers => true;

        protected override Type GetMessageHandlerInterfaceGenericType()
        {
            return typeof(IEventHandler<>);
        }
    }
}
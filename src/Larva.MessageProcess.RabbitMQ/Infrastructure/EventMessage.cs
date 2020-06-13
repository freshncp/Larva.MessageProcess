using System;

namespace Larva.MessageProcess.RabbitMQ.Infrastructure
{
   [Serializable]
    public class EventMessage
    {
        public string EventTypeName { get; set; }

        public string EventData { get; set; }
    }
}
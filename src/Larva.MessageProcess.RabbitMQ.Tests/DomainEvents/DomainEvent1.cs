using Larva.MessageProcess.Messaging.Attributes;
using Larva.MessageProcess.RabbitMQ.Eventing;
using System;
using System.Collections.Generic;

namespace Larva.MessageProcess.RabbitMQ.Tests.DomainEvents
{

    [MessageType("DomainEvent1")]
    public class DomainEvent1 : IDomainEvent
    {
        public DomainEvent1(string businessKey)
        {
            Id = Guid.NewGuid().ToString();
            Timestamp = DateTime.Now;
            BusinessKey = businessKey;
            ExtraDatas = new Dictionary<string, string>();
        }

        public string Id { get; set; }

        public DateTime Timestamp { get; set; }

        public string BusinessKey { get; set; }

        public IDictionary<string, string> ExtraDatas { get; set; }

        public int Sequence { get; set; }
    }
}
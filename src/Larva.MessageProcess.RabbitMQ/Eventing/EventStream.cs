using Larva.MessageProcess.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Larva.MessageProcess.RabbitMQ.Eventing
{
    [Serializable]
    public class EventStream
    {
        public EventStream(string id, DateTime timestamp, string businessKey, IDictionary<string, string> extraDatas, IEnumerable<IDomainEvent> events)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }
            if (timestamp == DateTime.MinValue)
            {
                throw new ArgumentNullException(nameof(timestamp));
            }
            if (string.IsNullOrEmpty(businessKey))
            {
                throw new ArgumentNullException(nameof(businessKey));
            }
            if (events == null || !events.Any())
            {
                throw new ArgumentNullException(nameof(events));
            }
            if (events.Any(a => a.BusinessKey != businessKey))
            {
                throw new ArgumentOutOfRangeException(nameof(businessKey));
            }
            Id = id;
            Timestamp = timestamp;
            BusinessKey = businessKey;
            ExtraDatas = extraDatas;
            Events = events;
        }

        public string Id { get; private set; }

        public DateTime Timestamp { get; private set; }

        public string BusinessKey { get; private set; }

        public IDictionary<string, string> ExtraDatas { get; private set; }

        public IEnumerable<IDomainEvent> Events { get; private set; }

        public static implicit operator MessageGroup(EventStream eventStream)
        {
            if (eventStream == null)
                return null;
            return new MessageGroup(eventStream.Id, eventStream.Timestamp, eventStream.BusinessKey, eventStream.ExtraDatas, eventStream.Events, true);
        }
    }
}
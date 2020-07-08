using Larva.MessageProcess.Messaging.Attributes;
using Larva.MessageProcess.RabbitMQ.Commanding;
using System;
using System.Collections.Generic;

namespace Larva.MessageProcess.RabbitMQ.Tests.Commands
{
    [MessageType("Command1")]
    public class Command1 : ICommand
    {
        public Command1(string businessKey)
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
    }
}
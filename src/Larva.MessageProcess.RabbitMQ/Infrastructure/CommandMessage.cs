using System;
using System.Collections.Generic;

namespace Larva.MessageProcess.RabbitMQ.Infrastructure
{
    [Serializable]
    public class CommandMessage
    {
        public string CommandTypeName { get; set; }

        public string CommandData { get; set; }

        public Dictionary<string, string> ExtraDatas { get; set;}
    }
}
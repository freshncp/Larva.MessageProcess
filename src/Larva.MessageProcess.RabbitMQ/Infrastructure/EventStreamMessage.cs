using Larva.MessageProcess.Messaging;
using System;
using System.Collections.Generic;

namespace Larva.MessageProcess.RabbitMQ.Infrastructure
{
    /// <summary>
    /// 事件流消息
    /// </summary>
    [Serializable]
    public class EventStreamMessage : IMessage
    {
        /// <summary>
        /// 事件消息列表
        /// </summary>
        public IDictionary<string, EventMessage> Events { get; set; }

        /// <summary>
        /// 事件流ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 消息时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 业务键（相同业务键串行处理；不同业务键并行处理）
        /// </summary>
        public string BusinessKey { get; set; }

        /// <summary>
        /// 额外数据
        /// </summary>
        public IDictionary<string, string> ExtraDatas { get; set; }
    }
}
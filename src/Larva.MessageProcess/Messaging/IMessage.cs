using System;
using System.Collections.Generic;

namespace Larva.MessageProcess.Messaging
{
    /// <summary>
    /// 消息
    /// </summary>
    public interface IMessage
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// 消息时间戳
        /// </summary>
        DateTime Timestamp { get; set; }

        /// <summary>
        /// 业务键（相同业务键串行处理；不同业务键并行处理）
        /// </summary>
        string BusinessKey { get; set; }

        /// <summary>
        /// 额外数据
        /// </summary>
        IDictionary<string, string> ExtraDatas { get; set; }
    }
}

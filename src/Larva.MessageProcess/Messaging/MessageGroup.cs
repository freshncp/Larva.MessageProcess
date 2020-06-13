using System;
using System.Collections.Generic;

namespace Larva.MessageProcess.Messaging
{
    /// <summary>
    /// 消息组
    /// </summary>
    [Serializable]
    public sealed class MessageGroup : IMessage
    {
        /// <summary>
        /// 消息组
        /// </summary>
        /// <param name="id">消息组ID</param>
        /// <param name="timestamp">消息时间戳</param>
        /// <param name="businessKey">业务键（相同业务键串行处理；不同业务键并行处理）</param>
        /// <param name="extraDatas">额外数据</param>
        /// <param name="messages">消息列表</param>
        /// <param name="noHandlerAllowed">是否允许无Handler</param>
        public MessageGroup(string id, DateTime timestamp, string businessKey, IDictionary<string, string> extraDatas, IEnumerable<IMessage> messages, bool noHandlerAllowed)
        {
            ((IMessage)this).Id = id;
            ((IMessage)this).Timestamp = timestamp;
            ((IMessage)this).BusinessKey = businessKey;
            ((IMessage)this).ExtraDatas = extraDatas;
            Messages = messages;
            NoHandlerAllowed = noHandlerAllowed;
        }

        /// <summary>
        /// 消息列表
        /// </summary>
        public IEnumerable<IMessage> Messages { get; private set; }

        /// <summary>
        /// 是否允许无Handler
        /// </summary>
        public bool NoHandlerAllowed { get; private set; }

        /// <summary>
        /// 消息组ID
        /// </summary>
        string IMessage.Id { get; set; }

        /// <summary>
        /// 消息时间戳
        /// </summary>
        DateTime IMessage.Timestamp { get; set; }

        /// <summary>
        /// 业务键（相同业务键串行处理；不同业务键并行处理）
        /// </summary>
        string IMessage.BusinessKey { get; set; }

        /// <summary>
        /// 额外数据
        /// </summary>
        IDictionary<string, string> IMessage.ExtraDatas { get; set; }
    }
}
using System;

namespace Larva.MessageProcess.Handlers.Attributes
{
    /// <summary>
    /// 消息订阅者
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MessageSubscriberAttribute : Attribute
    {
        /// <summary>
        /// 消息订阅者
        /// </summary>
        /// <param name="name">订阅者名（对应MQ消费组名）</param>
        public MessageSubscriberAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 订阅者名（对应MQ消费组名）
        /// </summary>
        public string Name { get; set; }
    }
}

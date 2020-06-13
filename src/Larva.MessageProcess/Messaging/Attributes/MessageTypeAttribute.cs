using System;

namespace Larva.MessageProcess.Messaging.Attributes
{
    /// <summary>
    /// 消息类型
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MessageTypeAttribute : Attribute
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        /// <param name="name">类型名</param>
        public MessageTypeAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 订阅者名（对应MQ消费组名）
        /// </summary>
        public string Name { get; set; }
    }
}

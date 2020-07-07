using System;

namespace Larva.MessageProcess.Handling.Attributes
{
    /// <summary>
    /// 处理优先级，用于同一个消息类型、同一个订阅者，多个处理器的优先级设置
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class HandlePriorityAttribute : Attribute
    {
        /// <summary>
        /// 处理优先级，用于同一个消息类型、同一个订阅者，多个处理器的优先级设置
        /// </summary>
        /// <param name="priority">优先级（数字越小，优先级越高）</param>
        public HandlePriorityAttribute(byte priority)
        {
            Priority = priority;
        }

        /// <summary>
        /// 优先级（数字越小，优先级越高）
        /// </summary>
        public byte Priority { get; set; }
    }
}

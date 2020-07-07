using Larva.MessageProcess.Messaging;
using System;

namespace Larva.MessageProcess.Handling.AutoIdempotent
{
    /// <summary>
    /// 重复消息处理异常
    /// </summary>
    [Serializable]
    public class DuplicateMessageHandlingException : Exception
    {
        /// <summary>
        /// 重复消息处理异常
        /// </summary>
        public DuplicateMessageHandlingException() : base() { }

        /// <summary>
        /// 重复消息处理异常
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="messageHandlerType">消息处理器类型</param>
        public DuplicateMessageHandlingException(IMessage message, Type messageHandlerType) : base($"Message [{message.GetMessageTypeName()}] ({message.Id}) with business key \"{message.BusinessKey}\" has already handled by {messageHandlerType.FullName}.") { }

        /// <summary>
        /// 重复消息处理异常
        /// </summary>
        /// <param name="message"></param>
        public DuplicateMessageHandlingException(string message) : base(message) { }

        /// <summary>
        /// 重复消息处理异常
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public DuplicateMessageHandlingException(string message, Exception innerException) : base(message, innerException) { }
    }
}

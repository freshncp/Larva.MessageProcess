using Larva.MessageProcess.Messaging;
using System;

namespace Larva.MessageProcess.Handling.AutoIdempotent
{
    /// <summary>
    /// 自动幂等存储
    /// </summary>
    public interface IAutoIdempotentStore
    {
        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="messageHandlerType">处理器类型</param>
        /// <param name="multipleMessageHandlers">是否多个消息处理器，如果为false，则handlerType可以不作为存储Key的一部分</param>
        void Save(IMessage message, Type messageHandlerType, bool multipleMessageHandlers);

        /// <summary>
        /// 是否存在
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="messageHandlerType">处理器类型</param>
        /// <param name="multipleMessageHandlers">是否多个消息处理器，如果为false，则handlerType可以不作为存储Key的一部分</param>
        /// <returns></returns>
        bool Exists(IMessage message, Type messageHandlerType, bool multipleMessageHandlers);
    }
}

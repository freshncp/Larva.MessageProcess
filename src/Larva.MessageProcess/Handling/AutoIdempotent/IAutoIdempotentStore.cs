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
        void Save(IMessage message, Type messageHandlerType);

        /// <summary>
        /// 是否存在
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="messageHandlerType">处理器类型</param>
        /// <returns></returns>
        bool Exists(IMessage message, Type messageHandlerType);

        /// <summary>
        /// 等待保存完成
        /// </summary>
        void WaitForSave();

        /// <summary>
        /// 等待保存完成
        /// </summary>
        /// <param name="timeout">超时时间</param>
        void WaitForSave(TimeSpan timeout);
    }
}

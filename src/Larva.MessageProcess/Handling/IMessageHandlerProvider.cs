using System;
using System.Collections.Generic;

namespace Larva.MessageProcess.Handling
{
    /// <summary>
    /// 消息处理器提供者 接口
    /// </summary>
    public interface IMessageHandlerProvider
    {
        /// <summary>
        /// 获取消息类型列表
        /// </summary>
        /// <returns></returns>
        IDictionary<string, Type> GetMessageTypes();

        /// <summary>
        /// 获取处理器列表
        /// </summary>
        /// <param name="messageType">消息类型</param>
        /// <param name="subscriber">订阅者</param>
        /// <returns></returns>
        IEnumerable<IMessageHandlerProxy> GetHandlers(Type messageType, string subscriber);
    }
}

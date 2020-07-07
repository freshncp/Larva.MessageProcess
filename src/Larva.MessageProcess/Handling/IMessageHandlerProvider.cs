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
        /// <param name="messageType"></param>
        /// <param name="subscriber"></param>
        /// <returns></returns>
        IEnumerable<IMessageHandlerProxy> GetHandlers(Type messageType, string subscriber);
    }
}

using Larva.MessageProcess.Messaging;
using System;
using System.Collections.Concurrent;

namespace Larva.MessageProcess.Handling.AutoIdempotent
{
    /// <summary>
    /// 基于内存的自动幂等存储（仅用于测试）
    /// </summary>
    public class InMemoryAutoIdempotentStore : IAutoIdempotentStore
    {
        private ConcurrentDictionary<string, string> _handledMessageDict = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="messageHandlerType">处理器类型</param>
        /// <returns></returns>
        public void Save(IMessage message, Type messageHandlerType)
        {
            var key = BuildKey(message, messageHandlerType);
            _handledMessageDict.TryAdd(key, message.GetMessageTypeName());
        }

        /// <summary>
        /// 是否存在
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="messageHandlerType">处理器类型</param>
        /// <returns></returns>
        public bool Exists(IMessage message, Type messageHandlerType)
        {
            var key = BuildKey(message, messageHandlerType);
            return _handledMessageDict.ContainsKey(key);
        }

        /// <summary>
        /// 等待保存完成
        /// </summary>
        public void WaitForSave()
        {
            // Retry save agin if fail, like io error.
        }

        /// <summary>
        /// 等待保存完成
        /// </summary>
        /// <param name="timeout">超时时间</param>
        public void WaitForSave(TimeSpan timeout)
        {
            // Retry save agin if fail, like io error.
        }

        private string BuildKey(IMessage message, Type messageHandlerType)
        {
            return $"{message.BusinessKey}:{message.Id}:{messageHandlerType.FullName}";
        }
    }
}

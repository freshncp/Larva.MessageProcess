﻿using Larva.MessageProcess.Messaging;
using System;
using System.Collections.Concurrent;

namespace Larva.MessageProcess.Handling.AutoIdempotent
{
    /// <summary>
    /// 基于内存的自动幂等存储（仅用于测试，线上推荐采用Redis）
    /// </summary>
    public class MemoryAutoIdempotentStore : IAutoIdempotentStore
    {
        private ConcurrentDictionary<string, string> _handledMessageDict = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="messageHandlerType">处理器类型</param>
        /// <param name="multipleMessageHandlers">是否多个消息处理器，如果为false，则handlerType可以不作为存储Key的一部分</param>
        /// <returns></returns>
        public void Save(IMessage message, Type messageHandlerType, bool multipleMessageHandlers)
        {
            var key = BuildKey(message, messageHandlerType, multipleMessageHandlers);
            _handledMessageDict.TryAdd(key, message.GetMessageTypeName());
        }

        /// <summary>
        /// 是否存在
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="messageHandlerType">处理器类型</param>
        /// <param name="multipleMessageHandlers">是否多个消息处理器，如果为false，则handlerType可以不作为存储Key的一部分</param>
        /// <returns></returns>
        public bool Exists(IMessage message, Type messageHandlerType, bool multipleMessageHandlers)
        {
            var key = BuildKey(message, messageHandlerType, multipleMessageHandlers);
            return _handledMessageDict.ContainsKey(key);
        }

        private string BuildKey(IMessage message, Type messageHandlerType, bool multipleMessageHandlers)
        {
            return multipleMessageHandlers ? $"{message.BusinessKey}:{message.Id}:{messageHandlerType.FullName}" : $"{message.BusinessKey}:{message.Id}";
        }
    }
}
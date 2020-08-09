using Larva.MessageProcess.Messaging.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Larva.MessageProcess.Messaging
{
    /// <summary>
    /// 消息接口扩展
    /// </summary>
    public static class IMessageExtensions
    {
        private static ConcurrentDictionary<Type, string> _messageTypeCache = new ConcurrentDictionary<Type, string>();

        /// <summary>
        /// 获取消息类型名
        /// </summary>
        /// <param name="message">消息</param>
        /// <returns></returns>
        public static string GetMessageTypeName(this IMessage message)
        {
            var messageGroup = message as MessageGroup;
            if (messageGroup == null)
            {
                return message.GetType().GetMessageTypeName();
            }
            else if (messageGroup.Messages == null || !messageGroup.Messages.Any())
            {
                return "[]";
            }
            else
            {
                var messageTypes = messageGroup.Messages.Select(s => s.GetType()).Distinct();
                return $"[{string.Join(", ", messageTypes.Select(s => s.GetMessageTypeName()))}]";
            }
        }

        /// <summary>
        /// 获取消息类型名
        /// </summary>
        /// <param name="messageType">消息类型</param>
        /// <returns></returns>
        public static string GetMessageTypeName(this Type messageType)
        {
            if (!_messageTypeCache.ContainsKey(messageType))
            {
                var messageTypeAttr = messageType.GetCustomAttribute<MessageTypeAttribute>();
                var messageTypeName = messageTypeAttr == null ? messageType.FullName : messageTypeAttr.Name;
                _messageTypeCache.TryAdd(messageType, messageTypeName);
            }
            return _messageTypeCache[messageType];
        }

        /// <summary>
        /// 合并额外数据
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="extraDatas">额外数据</param>
        public static void MergeExtraDatas(this IMessage message, IDictionary<string, string> extraDatas)
        {
            if (extraDatas == null || extraDatas.Count == 0)
            {
                return;
            }
            if (message.ExtraDatas == null)
            {
                message.ExtraDatas = new Dictionary<string, string>();
            }
            foreach (var key in extraDatas.Keys)
            {
                if (message.ExtraDatas.ContainsKey(key))
                {
                    message.ExtraDatas[key] = extraDatas[key];
                }
                else
                {
                    message.ExtraDatas.Add(key, extraDatas[key]);
                }
            }
        }

        /// <summary>
        /// 设置额外数据
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="extraDataKey">额外数据键</param>
        /// <param name="extraDataVal">额外数据值</param>
        public static void SetExtraData(this IMessage message, string extraDataKey, string extraDataVal)
        {
            if (message.ExtraDatas == null)
            {
                message.ExtraDatas = new Dictionary<string, string>();
            }
            if (message.ExtraDatas.ContainsKey(extraDataKey))
            {
                message.ExtraDatas[extraDataKey] = extraDataVal;
            }
            else
            {
                message.ExtraDatas.Add(extraDataKey, extraDataVal);
            }
        }

        /// <summary>
        /// 获取额外数据
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="extraDataKey">额外数据键</param>
        /// <returns></returns>
        public static string GetExtraData(this IMessage message, string extraDataKey)
        {
            if (message.ExtraDatas == null || !message.ExtraDatas.ContainsKey(extraDataKey))
            {
                return null;
            }
            return message.ExtraDatas[extraDataKey];
        }
    }
}
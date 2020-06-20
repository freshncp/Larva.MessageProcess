using Larva.DynamicProxy.Interception;
using Larva.MessageProcess.Handlers.Attributes;
using Larva.MessageProcess.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Larva.MessageProcess.Handlers
{
    /// <summary>
    /// 消息处理器提供者 抽象类
    /// </summary>
    public abstract class MessageHandlerProviderBase : IMessageHandlerProvider
    {
        private readonly IDictionary<Type, IList<Tuple<string, byte, IMessageHandlerProxy>>> _handlerDict;
        private readonly IDictionary<string, Type> _messageTypes;
        private IDictionary<Type, IDictionary<string, IEnumerable<IMessageHandlerProxy>>> _sortedHandlerDict;
        private static readonly IList<IMessageHandlerProxy> _emptyHandlers = new List<IMessageHandlerProxy>().AsReadOnly();
        
        /// <summary>
        /// 消息处理器提供者 抽象类
        /// </summary>
        protected MessageHandlerProviderBase()
        {
            _handlerDict = new Dictionary<Type, IList<Tuple<string, byte, IMessageHandlerProxy>>>();
            _messageTypes = new Dictionary<string, Type>();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="interceptors">拦截器</param>
        /// <param name="assemblies">程序集</param>
        public void Initialize(IInterceptor[] interceptors, params Assembly[] assemblies)
        {
            _handlerDict.Clear();
            _messageTypes.Clear();
            foreach (var handlerType in assemblies.SelectMany(assembly => assembly.GetTypes().Where(type => type != null && type.IsClass && !type.IsAbstract)))
            {
                RegisterHandler(handlerType, interceptors);
            }
            _sortedHandlerDict = _handlerDict
                .ToDictionary(kv => kv.Key, kv => (IDictionary<string, IEnumerable<IMessageHandlerProxy>>)kv.Value
                    .GroupBy(g => g.Item1)
                    .ToDictionary(kv2 => kv2.Key, kv2 => kv2.OrderBy(o => o.Item2).Select(s => s.Item3)));
            foreach (var messageType in _handlerDict.Keys)
            {
                var messageTypeName = messageType.GetMessageTypeName();
                if (_messageTypes.ContainsKey(messageTypeName))
                {
                    throw new InvalidOperationException($"MessageType \"{messageTypeName}\" has already exists");
                }
                _messageTypes.Add(messageTypeName, messageType);
            }
        }

        /// <summary>
        /// 获取消息类型列表
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, Type> GetMessageTypes()
        {
            return _messageTypes;
        }

        /// <summary>
        /// 获取处理器列表
        /// </summary>
        /// <param name="messageType">消息类型</param>
        /// <param name="subscriber">订阅者</param>
        /// <returns></returns>
        public IEnumerable<IMessageHandlerProxy> GetHandlers(Type messageType, string subscriber)
        {
            if (_sortedHandlerDict.ContainsKey(messageType))
            {
                var allSubscriberHandlers = _sortedHandlerDict[messageType];
                if (allSubscriberHandlers.ContainsKey(subscriber))
                {
                    return allSubscriberHandlers[subscriber];
                }
            }
            return _emptyHandlers;
        }

        /// <summary>
        /// 是否允许多个消息处理器
        /// </summary>
        protected abstract bool AllowMultipleMessageHandlers { get; }

        /// <summary>
        /// 获取消息处理接口范性类型
        /// </summary>
        /// <returns></returns>
        protected abstract Type GetMessageHandlerInterfaceGenericType();

        private void RegisterHandler(Type handlerType, IInterceptor[] interceptors)
        {
            var handlerInterfaceTypes = handlerType.GetInterfaces().Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == GetMessageHandlerInterfaceGenericType());
            if (!handlerInterfaceTypes.Any()) return;

            var realHandler = ObjectContainer.Resolve(handlerType);
            var priorityAttr = handlerType.GetCustomAttribute<HandlePriorityAttribute>();
            var priority = priorityAttr == null ? (byte)0 : priorityAttr.Priority;
            var subscriberAttr = handlerType.GetCustomAttribute<MessageSubscriberAttribute>();
            var subscriber = subscriberAttr == null || subscriberAttr.Name == null ? string.Empty : subscriberAttr.Name;

            foreach (var handlerInterfaceType in handlerInterfaceTypes)
            {
                var key = handlerInterfaceType.GetGenericArguments().Single();
                var handlerProxyType = typeof(MessageHandlerProxy<>).MakeGenericType(handlerInterfaceType.GetGenericArguments().Single());
                IList<Tuple<string, byte, IMessageHandlerProxy>> handlers;
                if (!_handlerDict.TryGetValue(key, out handlers))
                {
                    handlers = new List<Tuple<string, byte, IMessageHandlerProxy>>();
                    _handlerDict.Add(key, handlers);
                }
                if (handlers.Any(x => x.Item3.GetWrappedObject().GetType() == handlerType))
                {
                    continue;
                }
                if (!AllowMultipleMessageHandlers)
                {
                    if (handlers.Count > 0)
                    {
                        throw new InvalidOperationException($"Exists multiple message handlers to handle the same message \"{key.FullName}\", handlerType:{handlerType.FullName}");
                    }
                }
                handlers.Add(new Tuple<string, byte, IMessageHandlerProxy>(subscriber, priority, (IMessageHandlerProxy)Activator.CreateInstance(handlerProxyType, new[] { realHandler, interceptors })));
            }
        }
    }
}

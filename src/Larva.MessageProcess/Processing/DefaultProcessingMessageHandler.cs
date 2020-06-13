using Larva.MessageProcess.Handlers;
using Larva.MessageProcess.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Larva.MessageProcess.Processing
{
    /// <summary>
    /// 处理中消息的处理器
    /// </summary>
    public class DefaultProcessingMessageHandler : IProcessingMessageHandler
    {
        private volatile int _initialized;
        private IMessageHandlerProvider _messageHandlerProvider;
        private ConcurrentDictionary<string, string> _problemBusinessKeys = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="messageHandlerProvider">消息处理器提供者</param>
        public void Initialize(IMessageHandlerProvider messageHandlerProvider)
        {
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) == 0)
            {
                _messageHandlerProvider = messageHandlerProvider;
            }
        }

        /// <summary>
        /// 处理
        /// </summary>
        /// <param name="processingMessage">处理中消息</param>
        /// <returns></returns>
        public async Task HandleAsync(ProcessingMessage processingMessage)
        {
            var message = processingMessage.Message;
            var messageTypeName = message.GetMessageTypeName();

            if (string.IsNullOrEmpty(message.BusinessKey))
            {
                var errorMessage = string.Format("The businessKey of message cannot be null or empty. messageType:{0}, messageId:{1}", messageTypeName, message.Id);
                await CompleteMessageAsync(processingMessage, MessageExecutingStatus.Failed, typeof(string).FullName, errorMessage).ConfigureAwait(false);
                return;
            }
            if (!processingMessage.ContinueWhenHandleFail && _problemBusinessKeys.ContainsKey(processingMessage.Message.BusinessKey))
            {
                var lastMessageId = _problemBusinessKeys[processingMessage.Message.BusinessKey];
                var errorMessage = string.Format("The last message with same business key, handle fail. messageType:{0}, messageId:{1}, businessKey:{2}, last messageId:{3}", messageTypeName, message.Id, message.BusinessKey, lastMessageId);
                await CompleteMessageAsync(processingMessage, MessageExecutingStatus.Failed, typeof(string).FullName, errorMessage).ConfigureAwait(false);
                return;
            }

            var findResult = GetMessageHandler(processingMessage, out IDictionary<IMessage, IEnumerable<IMessageHandlerProxy>> messageHandlerProxyDict);
            if (findResult == HandlerFindResult.Found)
            {
                await HandleInternal(processingMessage, messageHandlerProxyDict).ConfigureAwait(false);
            }
            else if (findResult == HandlerFindResult.NotFound)
            {
                var warnMessage = string.Format("No message handler found of message. messageType:{0}, messageId:{1}", messageTypeName, message.Id);
                await CompleteMessageAsync(processingMessage, MessageExecutingStatus.HandlerNotFound, typeof(string).FullName, warnMessage).ConfigureAwait(false);
            }
            else if (findResult == HandlerFindResult.NoMessage)
            {
                await CompleteMessageAsync(processingMessage, MessageExecutingStatus.Success, string.Empty, string.Empty).ConfigureAwait(false);
            }
        }

        private async Task HandleInternal(ProcessingMessage processingMessage, IDictionary<IMessage, IEnumerable<IMessageHandlerProxy>> messageHandlerProxyDict)
        {
            try
            {
                foreach (var message in messageHandlerProxyDict.Keys)
                {
                    var handlerProxyList = messageHandlerProxyDict[message];
                    foreach (var messageHandler in handlerProxyList)
                    {
                        await messageHandler.HandleAsync(message, processingMessage.ExecutingContext).ConfigureAwait(false);
                    }
                }
                var result = processingMessage.ExecutingContext.GetResult();
                await CompleteMessageAsync(processingMessage, MessageExecutingStatus.Success, typeof(string).FullName, result);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(processingMessage, ex, ex.Message).ConfigureAwait(false);
            }
        }

        private async Task HandleExceptionAsync(ProcessingMessage processingMessage, Exception exception, string errorMessage)
        {
            if (!processingMessage.ContinueWhenHandleFail)
            {
                _problemBusinessKeys.TryAdd(processingMessage.Message.BusinessKey, processingMessage.Message.Id);
            }
            var realException = GetRealException(exception);
            await CompleteMessageAsync(processingMessage, MessageExecutingStatus.Failed, realException.GetType().FullName, realException != null ? realException.Message : errorMessage, realException != null ? realException.StackTrace : string.Empty).ConfigureAwait(false);
        }

        private Exception GetRealException(Exception exception)
        {
            if (exception is AggregateException && ((AggregateException)exception).InnerExceptions.Count > 0)
            {
                return ((AggregateException)exception).InnerExceptions.First();
            }
            return exception;
        }

        private HandlerFindResult GetMessageHandler(ProcessingMessage processingMessage, out IDictionary<IMessage, IEnumerable<IMessageHandlerProxy>> messageHandlerProxyDict)
        {
            messageHandlerProxyDict = new Dictionary<IMessage, IEnumerable<IMessageHandlerProxy>>();
            var messageGroup = processingMessage.Message as MessageGroup;
            if (messageGroup != null)
            {
                if (messageGroup.Messages == null || !messageGroup.Messages.Any())
                {
                    return HandlerFindResult.NoMessage;
                }
                foreach (var message in messageGroup.Messages)
                {
                    var messageType = message.GetType();
                    var subscriber = processingMessage.MessageSubscriber;
                    var handlerProxyList = _messageHandlerProvider.GetHandlers(messageType, subscriber);
                    if (handlerProxyList != null && handlerProxyList.Any())
                    {
                        messageHandlerProxyDict.Add(message, handlerProxyList);
                    }
                    else if (!messageGroup.NoHandlerAllowed)
                    {
                        messageHandlerProxyDict.Clear();
                        return HandlerFindResult.NotFound;
                    }
                }
                return HandlerFindResult.Found;
            }
            else
            {
                var messageType = processingMessage.Message.GetType();
                var subscriber = processingMessage.MessageSubscriber;
                var handlerProxyList = _messageHandlerProvider.GetHandlers(messageType, subscriber);
                if (handlerProxyList == null || handlerProxyList.Count() == 0)
                {
                    return HandlerFindResult.NotFound;
                }
                else
                {
                    messageHandlerProxyDict.Add(processingMessage.Message, handlerProxyList);
                    return HandlerFindResult.Found;
                }
            }
        }

        private async Task CompleteMessageAsync(ProcessingMessage processingMessage, MessageExecutingStatus commandStatus, string resultType, string result, string stackTrace = null)
        {
            var commandResult = new MessageExecutingResult(commandStatus, processingMessage.Message, processingMessage.MessageSubscriber, result, resultType, stackTrace);
            await processingMessage.CompleteAsync(commandResult).ConfigureAwait(false);
        }

        private enum HandlerFindResult
        {
            NotFound,
            Found,
            NoMessage
        }
    }
}

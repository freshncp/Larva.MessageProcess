using Larva.MessageProcess.Handling;
using Larva.MessageProcess.Handling.AutoIdempotent;
using Larva.MessageProcess.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Larva.MessageProcess.Processing
{
    /// <summary>
    /// 处理中消息的处理器
    /// </summary>
    public class DefaultProcessingMessageHandler : IProcessingMessageHandler
    {
        private readonly ILogger _logger;

        /// <summary>
        /// 处理中消息的处理器
        /// </summary>
        public DefaultProcessingMessageHandler()
        {
            _logger = LoggerManager.GetLogger(GetType());
        }

        /// <summary>
        /// 处理
        /// </summary>
        /// <param name="subscriber">订阅者</param>
        /// <param name="processingMessage">处理中消息</param>
        /// <param name="messageHandlerProvider">消息处理器提供者</param>
        /// <returns></returns>
        public async Task<bool> HandleAsync(string subscriber, ProcessingMessage processingMessage, IMessageHandlerProvider messageHandlerProvider)
        {
            var message = processingMessage.Message;
            var messageTypeName = message.GetMessageTypeName();

            var findResult = GetMessageHandler(subscriber, processingMessage, messageHandlerProvider, out IDictionary<IMessage, IEnumerable<IMessageHandlerProxy>> messageHandlerProxyDict);
            if (findResult == HandlerFindResult.Found)
            {
                return await HandleInternal(subscriber, processingMessage, messageHandlerProxyDict).ConfigureAwait(false);
            }
            else if (findResult == HandlerFindResult.NotFound)
            {
                var warnMessage = $"No message handler found of message, businessKey: {message.BusinessKey}, subscriber: {subscriber}, messageId: {message.Id}, messageType: {messageTypeName}";
                await CompleteMessageAsync(subscriber, processingMessage, MessageExecutingStatus.HandlerNotFound, typeof(string).FullName, warnMessage).ConfigureAwait(false);
                return true;
            }
            else
            {
                await CompleteMessageAsync(subscriber, processingMessage, MessageExecutingStatus.Success, string.Empty, string.Empty).ConfigureAwait(false);
                return true;
            }
        }

        private async Task<bool> HandleInternal(string subscriber, ProcessingMessage processingMessage, IDictionary<IMessage, IEnumerable<IMessageHandlerProxy>> messageHandlerProxyDict)
        {
            try
            {
                foreach (var message in messageHandlerProxyDict.Keys)
                {
                    var handlerProxyList = messageHandlerProxyDict[message];
                    foreach (var messageHandler in handlerProxyList)
                    {
                        try
                        {
                            await messageHandler.HandleAsync(message, processingMessage.ExecutingContext).ConfigureAwait(false);
                        }
                        catch (DuplicateMessageHandlingException ex)
                        {
                            _logger.Warn(ex.Message);
                        }
                    }
                }

                var result = processingMessage.ExecutingContext.GetResult();
                await CompleteMessageAsync(subscriber, processingMessage, MessageExecutingStatus.Success, typeof(string).FullName, result).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(subscriber, processingMessage, ex, ex.Message).ConfigureAwait(false);
                return false;
            }
        }

        private async Task HandleExceptionAsync(string subscriber, ProcessingMessage processingMessage, Exception exception, string errorMessage)
        {
            var realException = GetRealException(exception);
            await CompleteMessageAsync(subscriber, processingMessage, MessageExecutingStatus.Failed, realException.GetType().FullName, realException != null ? realException.Message : errorMessage, realException != null ? realException.StackTrace : string.Empty).ConfigureAwait(false);
        }

        private Exception GetRealException(Exception exception)
        {
            if (exception is AggregateException && ((AggregateException)exception).InnerExceptions.Count > 0)
            {
                return ((AggregateException)exception).InnerExceptions[0];
            }
            return exception;
        }

        private HandlerFindResult GetMessageHandler(string subscriber, ProcessingMessage processingMessage, IMessageHandlerProvider messageHandlerProvider, out IDictionary<IMessage, IEnumerable<IMessageHandlerProxy>> messageHandlerProxyDict)
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
                    var handlerProxyList = messageHandlerProvider.GetHandlers(messageType, subscriber);
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
                var handlerProxyList = messageHandlerProvider.GetHandlers(messageType, subscriber);
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

        private async Task CompleteMessageAsync(string subscriber, ProcessingMessage processingMessage, MessageExecutingStatus commandStatus, string resultType, string result, string stackTrace = null)
        {
            var commandResult = new MessageExecutingResult(commandStatus, processingMessage.Message, subscriber, result, resultType, stackTrace);
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

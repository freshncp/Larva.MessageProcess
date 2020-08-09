using Larva.MessageProcess.Handling;
using Larva.MessageProcess.Messaging;
using Larva.MessageProcess.Processing;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Larva.MessageProcess.Processing.Mailboxes
{
    /// <summary>
    /// 处理中消息邮箱
    /// </summary>
    public class DefaultProcessingMessageMailbox : IProcessingMessageMailbox
    {
        private readonly object _lockObj = new object();
        private readonly ConcurrentDictionary<long, ProcessingMessage> _processingMessageDict;
        private readonly ConcurrentQueue<ProcessingMessageWithCreatedTime> _problemProcessingMessages;
        private readonly ConcurrentDictionary<long, ProcessingMessageWithCreatedTime> _problemProcessingMessages2;
        private readonly ILogger _logger;
        private readonly IMessageHandlerProvider _messageHandlerProvider;
        private readonly IProcessingMessageHandler _processingMessageHandler;
        private readonly bool _continueWhenHandleFail;
        private readonly int _retryIntervalSeconds;
        private readonly int _batchSize;
        private volatile int _isRemoved;
        private volatile int _isRunning;
        private long _nextSequence;
        private long _consumingSequence;
        private volatile int _isHandlingProblemMessage;

        /// <summary>
        /// 处理中消息邮箱
        /// </summary>
        /// <param name="businessKey">业务键</param>
        /// <param name="subscriber">订阅者</param>
        /// <param name="messageHandlerProvider">消息处理器提供者</param>
        /// <param name="processingMessageHandler">处理中消息处理器</param>
        /// <param name="continueWhenHandleFail">相同BusinessKey的消息处理失败后，是否继续推进</param>
        /// <param name="retryIntervalSeconds">重试间隔秒数</param>
        /// <param name="batchSize">批量处理大小</param>
        public DefaultProcessingMessageMailbox(
            string businessKey,
            string subscriber,
            IMessageHandlerProvider messageHandlerProvider,
            IProcessingMessageHandler processingMessageHandler,
            bool continueWhenHandleFail,
            int retryIntervalSeconds,
            int batchSize)
        {
            if (string.IsNullOrEmpty(businessKey))
            {
                throw new ArgumentNullException(nameof(businessKey));
            }
            _processingMessageDict = new ConcurrentDictionary<long, ProcessingMessage>();
            _problemProcessingMessages = new ConcurrentQueue<ProcessingMessageWithCreatedTime>();
            _problemProcessingMessages2 = new ConcurrentDictionary<long, ProcessingMessageWithCreatedTime>();
            _logger = LoggerManager.GetLogger(GetType());
            _nextSequence = 1;
            LastActiveTime = DateTime.Now;
            Locker = new SpinLock();

            BusinessKey = businessKey;
            Subscriber = subscriber;
            _messageHandlerProvider = messageHandlerProvider;
            _processingMessageHandler = processingMessageHandler;
            _continueWhenHandleFail = continueWhenHandleFail;
            _retryIntervalSeconds = retryIntervalSeconds <= 0 ? 1 : retryIntervalSeconds;
            _batchSize = batchSize <= 0 ? 1000 : batchSize;
            ProcessProblemMessage();
        }

        /// <summary>
        /// 业务键
        /// </summary>
        public string BusinessKey { get; }

        /// <summary>
        /// 订阅者
        /// </summary>
        public string Subscriber { get; }

        /// <summary>
        /// 最后一次激活时间，作为清理的依据之一
        /// </summary>
        public DateTime LastActiveTime { get; private set; }

        /// <summary>
        /// 锁
        /// </summary>
        public SpinLock Locker { get; }

        /// <summary>
        /// 是否已移除
        /// </summary>
        public bool IsRemoved => _isRemoved == 1;

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning => _isRunning == 1;

        /// <summary>
        /// 未处理数
        /// </summary>
        public long UnhandledCount => _nextSequence - _consumingSequence;

        /// <summary>
        /// 未处理问题数
        /// </summary>
        public long UnhandledProblemCount => _continueWhenHandleFail ? _problemProcessingMessages2.Count : _problemProcessingMessages.Count;

        /// <summary>
        /// 是否空闲，作为清理的依据之一
        /// </summary>
        public bool IsFree => !IsRunning && UnhandledCount == 0 && UnhandledProblemCount == 0;

        /// <summary>
        /// 入队
        /// </summary>
        /// <param name="processingMessage"></param>
        public void Enqueue(ProcessingMessage processingMessage)
        {
            if (processingMessage == null)
            {
                throw new ArgumentNullException(nameof(processingMessage));
            }
            if (processingMessage.Message == null)
            {
                throw new ArgumentNullException(nameof(processingMessage), $"Property \"{nameof(processingMessage.Message)}\" cann't be null.");
            }
            if (processingMessage.Message.BusinessKey != BusinessKey)
            {
                throw new InvalidOperationException($"Message's business key \"{processingMessage.Message.BusinessKey}\" is not equal with mailbox's, businessKey: {BusinessKey}, subscriber: {Subscriber}, messageId: {processingMessage.Message.Id}, messageType: {processingMessage.Message.GetMessageTypeName()}, timestamp: {processingMessage.Message.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")}.");
            }
            lock (_lockObj)
            {
                processingMessage.Sequence = _nextSequence;
                processingMessage.SetTryDequeueCallback(this.TryDequeue);
                if (_processingMessageDict.TryAdd(processingMessage.Sequence, processingMessage))
                {
                    _nextSequence++;
                    TryRun();
                }
                else
                {
                    _logger.Error($"{GetType().Name} enqueue message failed, businessKey: {BusinessKey}, subscriber: {Subscriber}, messageId: {processingMessage.Message.Id}, messageType: {processingMessage.Message.GetMessageTypeName()}, messageSequence: {processingMessage.Sequence}");
                }
            }
        }

        /// <summary>
        /// 尝试出队
        /// </summary>
        /// <param name="processingMessage"></param>
        /// <returns></returns>
        public bool TryDequeue(ProcessingMessage processingMessage)
        {
            return _processingMessageDict.TryRemove(processingMessage.Sequence, out ProcessingMessage _);
        }

        /// <summary>
        /// 重置消费序号
        /// </summary>
        /// <param name="consumingSequence"></param>
        public void ResetConsumingSequence(long consumingSequence)
        {
            var originConsumingSequence = Interlocked.Exchange(ref _consumingSequence, consumingSequence);
            _logger.Debug($"{GetType().FullName} reset consumingSequence, businessKey: {BusinessKey}, subscriber: {Subscriber}, consumingSequence: {consumingSequence}, originConsumingSequence: {originConsumingSequence}");
        }

        /// <summary>
        /// 是否未激活
        /// </summary>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        public bool IsInactive(int timeoutSeconds)
        {
            return IsFree && (DateTime.Now - LastActiveTime).TotalSeconds >= timeoutSeconds;
        }

        /// <summary>
        /// 标记为已移除
        /// </summary>
        public void MarkAsRemoved()
        {
            Interlocked.Exchange(ref _isRemoved, 1);
        }

        private void TryRun()
        {
            LastActiveTime = DateTime.Now;
            if (Interlocked.CompareExchange(ref _isRunning, 1, 0) == 0)
            {
                // 等待问题消息处理完成
                while (_isHandlingProblemMessage == 1)
                {
                    Thread.Sleep(1);
                }
                Task.Factory.StartNew(async () =>
                {
                    await ProcessMessagesAsync();

                    // 再次运行
                    if (Interlocked.CompareExchange(ref _isRunning, 0, 1) == 1
                        && UnhandledCount > 0)
                    {
                        TryRun();
                    }
                });
            }
        }

        private async Task ProcessMessagesAsync()
        {
            try
            {
                var scannedCount = 0;
                while (UnhandledCount > 0 && scannedCount < _batchSize)
                {
                    var processingMessage = GetProcessingMessage(_consumingSequence);
                    if (processingMessage != null)
                    {
                        if (!_continueWhenHandleFail
                            && _problemProcessingMessages.TryPeek(out ProcessingMessageWithCreatedTime firstProblemProcessingMessagePair))
                        {
                            var message = processingMessage.Message;
                            var messageTypeName = message.GetMessageTypeName();
                            _problemProcessingMessages.Enqueue(new ProcessingMessageWithCreatedTime(processingMessage));
                            var lastMessageId = firstProblemProcessingMessagePair.Message.Message.Id;
                            var errorMessage = $"The last message with same business key, handle fail. businessKey: {BusinessKey}, subscriber: {Subscriber}, messageId: {message.Id}, messageType: {messageTypeName}, last messageId: {lastMessageId}";
                            await CompleteMessageAsync(processingMessage, MessageExecutingStatus.Failed, typeof(string).FullName, errorMessage).ConfigureAwait(false);
                            continue;
                        }

                        var processSuccess = await _processingMessageHandler.HandleAsync(Subscriber, processingMessage, _messageHandlerProvider).ConfigureAwait(false);
                        if (!processSuccess)
                        {
                            if (_continueWhenHandleFail)
                            {
                                _problemProcessingMessages2.TryAdd(processingMessage.Sequence, new ProcessingMessageWithCreatedTime(processingMessage));
                            }
                            else
                            {
                                _problemProcessingMessages.Enqueue(new ProcessingMessageWithCreatedTime(processingMessage));
                            }
                        }
                    }
                    Interlocked.Increment(ref _consumingSequence);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{GetType().Name} run has unknown exception, businessKey: {BusinessKey}, subscriber: {Subscriber}", ex);
            }
        }

        private void ProcessProblemMessage()
        {
            Task.Delay(Math.Min(10, _retryIntervalSeconds) * 1000).ContinueWith(async (lastTask, state) =>
            {
                if (Interlocked.CompareExchange(ref _isHandlingProblemMessage, 1, 0) == 0)
                {
                    try
                    {
                        if (_isRunning == 1)
                        {
                            // 暂停3秒，等待 ProcessMessage 完成，此处为提高处理问题消息的抢占概率
                            await Task.Delay(1000);
                        }
                        if (_isRunning == 1)
                        {
                            return;
                        }
                        if (_continueWhenHandleFail)
                        {
                            foreach (var sequence in _problemProcessingMessages2.Keys.OrderBy(o => o).ToArray())
                            {
                                var processingMessagePair = _problemProcessingMessages2[sequence];
                                if (!processingMessagePair.CanRetry(_retryIntervalSeconds))
                                {
                                    continue;
                                }
                                var processingMessage = processingMessagePair.Message;
                                var message = processingMessage.Message;
                                var messageTypeName = message.GetMessageTypeName();
                                var processSuccess = await _processingMessageHandler.HandleAsync(Subscriber, processingMessage, _messageHandlerProvider).ConfigureAwait(false);
                                if (processSuccess)
                                {
                                    _problemProcessingMessages2.TryRemove(sequence, out ProcessingMessageWithCreatedTime _);
                                    _logger.Info($"Last failed message retry success, businessKey: {BusinessKey}, subscriber: {Subscriber}, messageId: {message.Id}, messageType: {messageTypeName}");
                                }
                                else
                                {
                                    processingMessagePair.ResetCreatedTime();
                                }
                            }
                        }
                        else
                        {
                            while (_problemProcessingMessages.TryPeek(out ProcessingMessageWithCreatedTime processingMessagePair))
                            {
                                if (!processingMessagePair.CanRetry(_retryIntervalSeconds))
                                {
                                    break;
                                }
                                var processingMessage = processingMessagePair.Message;
                                var message = processingMessage.Message;
                                var messageTypeName = message.GetMessageTypeName();
                                var processSuccess = await _processingMessageHandler.HandleAsync(Subscriber, processingMessage, _messageHandlerProvider).ConfigureAwait(false);
                                if (processSuccess)
                                {
                                    _problemProcessingMessages.TryDequeue(out ProcessingMessageWithCreatedTime _);
                                    _logger.Info($"Last failed message retry success, businessKey: {BusinessKey}, subscriber: {Subscriber}, messageId: {message.Id}, messageType: {messageTypeName}");
                                }
                                else
                                {
                                    processingMessagePair.ResetCreatedTime();
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"{GetType().Name} run has unknown exception, businessKey: {BusinessKey}, subscriber: {Subscriber}", ex);
                    }
                    finally
                    {
                        ProcessProblemMessage();
                        Interlocked.CompareExchange(ref _isHandlingProblemMessage, 0, 1);
                    }
                }
            }, this);
        }

        private ProcessingMessage GetProcessingMessage(long sequence)
        {
            if (_processingMessageDict.TryGetValue(sequence, out ProcessingMessage processingMessage))
            {
                return processingMessage;
            }
            return null;
        }

        private async Task CompleteMessageAsync(ProcessingMessage processingMessage, MessageExecutingStatus commandStatus, string resultType, string result, string stackTrace = null)
        {
            var commandResult = new MessageExecutingResult(commandStatus, processingMessage.Message, Subscriber, result, resultType, stackTrace);
            await processingMessage.CompleteAsync(commandResult).ConfigureAwait(false);
        }

        private class ProcessingMessageWithCreatedTime
        {
            public ProcessingMessageWithCreatedTime(ProcessingMessage message)
            {
                Message = message;
                CreatedTime = DateTime.Now;
            }
            public ProcessingMessage Message { get; private set; }

            public DateTime CreatedTime { get; private set; }

            public void ResetCreatedTime()
            {
                CreatedTime = DateTime.Now;
            }

            public bool CanRetry(int retryIntervalSeconds)
            {
                return CreatedTime.AddSeconds(retryIntervalSeconds) <= DateTime.Now;
            }
        }
    }
}

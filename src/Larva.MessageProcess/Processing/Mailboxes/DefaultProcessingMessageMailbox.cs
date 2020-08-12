using Larva.MessageProcess.Handling;
using Larva.MessageProcess.Messaging;
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
        private readonly ConcurrentDictionary<long, ProcessingMessageWithCreatedTime> _problemProcessingMessages;
        private readonly ILogger _logger;
        private readonly IMessageHandlerProvider _messageHandlerProvider;
        private readonly IProcessingMessageHandler _processingMessageHandler;
        private readonly bool _continueWhenHandleFail;
        private readonly int _retryIntervalSeconds;
        private readonly bool _retryEnabled;
        private readonly int _batchSize;
        private readonly CancellationTokenSource _ctsWhenDisposing;
        private volatile int _isRemoved;
        private volatile int _isRunning;
        private volatile int _disposing;
        private long _nextSequence;
        private long _consumingSequence;

        /// <summary>
        /// 处理中消息邮箱
        /// </summary>
        /// <param name="businessKey">业务键</param>
        /// <param name="subscriber">订阅者</param>
        /// <param name="messageHandlerProvider">消息处理器提供者</param>
        /// <param name="processingMessageHandler">处理中消息处理器</param>
        /// <param name="continueWhenHandleFail">相同BusinessKey的消息处理失败后，是否继续推进</param>
        /// <param name="retryIntervalSeconds">重试间隔秒数（-1 表示不重试）</param>
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
            _problemProcessingMessages = new ConcurrentDictionary<long, ProcessingMessageWithCreatedTime>();
            _logger = LoggerManager.GetLogger(GetType());
            _nextSequence = 1;
            _consumingSequence = 1;
            LastActiveTime = DateTime.Now;
            Locker = new SpinLock();

            BusinessKey = businessKey;
            Subscriber = subscriber;
            _messageHandlerProvider = messageHandlerProvider;
            _processingMessageHandler = processingMessageHandler;
            _continueWhenHandleFail = continueWhenHandleFail;
            if (retryIntervalSeconds != -1)
            {
                _retryIntervalSeconds = retryIntervalSeconds <= 0 ? 1 : retryIntervalSeconds;
                _retryEnabled = true;
            }
            else
            {
                _retryIntervalSeconds = Int32.MaxValue;
                _retryEnabled = false;
            }
            _batchSize = batchSize <= 0 ? 1000 : batchSize;
            _ctsWhenDisposing = new CancellationTokenSource();
            if (_continueWhenHandleFail && _retryEnabled)
            {
                ProcessProblemMessage();
            }
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
        public long UnhandledProblemCount => _problemProcessingMessages.Count;

        /// <summary>
        /// 是否空闲，作为清理的依据之一
        /// </summary>
        public bool IsFree => !IsRunning && (_disposing == 1 || (UnhandledProblemCount == 0 && UnhandledCount == 0));

        /// <summary>
        /// 入队
        /// </summary>
        /// <param name="processingMessage"></param>
        public void Enqueue(ProcessingMessage processingMessage)
        {
            if (_disposing == 1) return;
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

        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {
            if (Interlocked.CompareExchange(ref _disposing, 1, 0) == 0)
            {
                _ctsWhenDisposing.Cancel();
            }
        }

        private void TryRun()
        {
            LastActiveTime = DateTime.Now;
            if (Interlocked.CompareExchange(ref _isRunning, 1, 0) == 0)
            {
                Task.Factory.StartNew(async () =>
                {
                    await ProcessMessagesAsync();

                    // 再次运行
                    lock (_lockObj)
                    {
                        Interlocked.Exchange(ref _isRunning, 0);
                        if (UnhandledCount > 0 && _disposing == 0)
                        {
                            TryRun();
                        }
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
                    if (_disposing == 1) break;
                    var processingMessage = GetProcessingMessage(_consumingSequence);
                    if (processingMessage != null)
                    {
                        var processSuccess = await _processingMessageHandler.HandleAsync(Subscriber, processingMessage, _messageHandlerProvider).ConfigureAwait(false);
                        if (processSuccess)
                        {
                            _processingMessageDict.TryRemove(processingMessage.Sequence, out ProcessingMessage _);
                            Interlocked.Increment(ref _consumingSequence);
                        }
                        else if (_continueWhenHandleFail)
                        {
                            _processingMessageDict.TryRemove(processingMessage.Sequence, out ProcessingMessage _);
                            Interlocked.Increment(ref _consumingSequence);
                            if (_retryEnabled)
                            {
                                _problemProcessingMessages.TryAdd(processingMessage.Sequence, new ProcessingMessageWithCreatedTime(processingMessage));
                            }
                        }
                        else
                        {
                            await Task.Delay(_retryIntervalSeconds, _ctsWhenDisposing.Token);
                        }
                    }
                    else
                    {
                        Interlocked.Increment(ref _consumingSequence);
                    }
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                _logger.Error($"{GetType().Name} run has unknown exception: {ex.Message}, businessKey: {BusinessKey}, subscriber: {Subscriber}", ex);
            }
        }

        private void ProcessProblemMessage()
        {
            if (_disposing == 1) return;
            try
            {
                Task.Delay(Math.Min(60, _retryIntervalSeconds) * 1000, _ctsWhenDisposing.Token).ContinueWith(async (lastTask, state) =>
                {
                    if (_disposing == 1) return;

                    if (UnhandledProblemCount > 0)
                    {
                        if (Interlocked.CompareExchange(ref _isRunning, 1, 0) == 0)
                        {
                            try
                            {
                                foreach (var sequence in _problemProcessingMessages.Keys.OrderBy(o => o).ToArray())
                                {
                                    var processingMessagePair = _problemProcessingMessages[sequence];
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
                                        _problemProcessingMessages.TryRemove(sequence, out ProcessingMessageWithCreatedTime _);
                                        _logger.Info($"Last failed message retry success, businessKey: {BusinessKey}, subscriber: {Subscriber}, messageId: {message.Id}, messageType: {messageTypeName}");
                                    }
                                    else
                                    {
                                        processingMessagePair.ResetCreatedTime();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Error($"{GetType().Name} run has unknown exception, businessKey: {BusinessKey}, subscriber: {Subscriber}", ex);
                            }

                            // 再次运行
                            lock (_lockObj)
                            {
                                Interlocked.Exchange(ref _isRunning, 0);
                                if (UnhandledCount > 0 && _disposing == 0)
                                {
                                    TryRun();
                                }
                            }
                        }
                    }
                    ProcessProblemMessage();
                }, this);
            }
            catch (TaskCanceledException) { }
        }

        private ProcessingMessage GetProcessingMessage(long sequence)
        {
            if (_processingMessageDict.TryGetValue(sequence, out ProcessingMessage processingMessage))
            {
                return processingMessage;
            }
            return null;
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

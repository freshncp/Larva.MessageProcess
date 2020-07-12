using Larva.MessageProcess.Messaging;
using Larva.MessageProcess.Processing;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Larva.MessageProcess.Mailboxes
{
    /// <summary>
    /// 处理中消息邮箱
    /// </summary>
    public class DefaultProcessingMessageMailbox : IProcessingMessageMailbox
    {
        private readonly object _lockObj = new object();
        private readonly ConcurrentDictionary<long, ProcessingMessage> _processingMessageDict;
        private IProcessingMessageHandler _processingMessageHandler;
        private int _batchSize;
        private readonly ILogger _logger;
        private volatile int _initialized;
        private volatile int _isRemoved;
        private volatile int _isRunning;
        private long _nextSequence;
        private long _consumingSequence;

        /// <summary>
        /// 处理中消息邮箱
        /// </summary>
        public DefaultProcessingMessageMailbox()
        {
            _processingMessageDict = new ConcurrentDictionary<long, ProcessingMessage>();
            _nextSequence = 1;
            _logger = LoggerManager.GetLogger(GetType());
            LastActiveTime = DateTime.Now;
            Locker = new SpinLock();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="businessKey"></param>
        /// <param name="processingMessageHandler"></param>
        /// <param name="batchSize"></param>
        public void Initialize(string businessKey, IProcessingMessageHandler processingMessageHandler, int batchSize)
        {
            if (string.IsNullOrEmpty(businessKey))
            {
                throw new ArgumentNullException(nameof(businessKey));
            }
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) == 0)
            {
                BusinessKey = businessKey;
                _processingMessageHandler = processingMessageHandler;
                _batchSize = batchSize <= 0 ? 1000 : batchSize;
            }
        }

        /// <summary>
        /// 业务键
        /// </summary>
        public string BusinessKey { get; private set; }

        /// <summary>
        /// 最后一次激活时间，作为清理的依据之一
        /// </summary>
        public DateTime LastActiveTime { get; private set; }

        /// <summary>
        /// 锁
        /// </summary>
        public SpinLock Locker { get; private set; }

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
                throw new InvalidOperationException($"Message's business key \"{processingMessage.Message.BusinessKey}\" is not equal with mailbox's, businessKey: {BusinessKey}, messageId: {processingMessage.Message.Id}, messageType: {processingMessage.Message.GetMessageTypeName()}.");
            }
            lock (_lockObj)
            {
                processingMessage.Sequence = _nextSequence;
                processingMessage.SetTryDequeueCallback(this.TryDequeue);
                if (_processingMessageDict.TryAdd(processingMessage.Sequence, processingMessage))
                {
                    _nextSequence++;
                    _logger.Debug($"{GetType().Name} enqueued new message, businessKey: {BusinessKey}, messageId: {processingMessage.Message.Id}, messageType: {processingMessage.Message.GetMessageTypeName()}, messageSequence: {processingMessage.Sequence}");
                    TryRun();
                }
                else
                {
                    _logger.Error($"{GetType().Name} enqueue message failed, businessKey: {BusinessKey}, messageId: {processingMessage.Message.Id}, messageType: {processingMessage.Message.GetMessageTypeName()}, messageSequence: {processingMessage.Sequence}");
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
            _logger.Debug($"{GetType().FullName} reset consumingSequence, businessKey: {BusinessKey}, consumingSequence: {consumingSequence}, originConsumingSequence: {originConsumingSequence}");
        }

        /// <summary>
        /// 尝试运行
        /// </summary>
        /// <returns></returns>
        public bool TryRun()
        {
            LastActiveTime = DateTime.Now;
            var trySuccess = Interlocked.CompareExchange(ref _isRunning, 1, 0) == 0;
            if (trySuccess)
            {
                Task.Factory.StartNew(ProcessMessagesAsync);
            }
            return trySuccess;
        }

        /// <summary>
        /// 完成运行
        /// </summary>
        public void CompleteRun()
        {
            if (Interlocked.CompareExchange(ref _isRunning, 0, 1) == 1
                && UnhandledCount > 0)
            {
                TryRun();
            }
        }

        /// <summary>
        /// 是否未激活
        /// </summary>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        public bool IsInactive(int timeoutSeconds)
        {
            return (DateTime.Now - LastActiveTime).TotalSeconds >= timeoutSeconds;
        }

        /// <summary>
        /// 标记为已移除
        /// </summary>
        public void MarkAsRemoved()
        {
            Interlocked.Exchange(ref _isRemoved, 1);
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
                        await _processingMessageHandler.HandleAsync(processingMessage).ConfigureAwait(false);
                    }
                    Interlocked.Increment(ref _consumingSequence);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{GetType().Name} run has unknown exception, businessKey: {BusinessKey}", ex);
            }
            finally
            {
                CompleteRun();
            }
        }

        private ProcessingMessage GetProcessingMessage(long sequence)
        {
            if (_processingMessageDict.TryGetValue(sequence, out ProcessingMessage processingMessage))
            {
                return processingMessage;
            }
            return null;
        }
    }
}

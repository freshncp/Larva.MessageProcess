using Larva.MessageProcess.Processing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Larva.MessageProcess.MailBoxes
{
    /// <summary>
    /// 处理中消息邮箱
    /// </summary>
    public class DefaultProcessingMessageMailbox : IProcessingMessageMailbox
    {
        private readonly object _lockObj = new object();
        private readonly ConcurrentDictionary<long, ProcessingMessage> _messageDict;
        private IProcessingMessageHandler _messageHandler;
        private int _batchSize;
        private readonly ILog _logger;
        private volatile int _initialized;
        private volatile int _isRemoved;
        private volatile int _isRunning;
        private long _nextSequence;
        private long _consumingSequence;

        /// <summary>
        /// 批量处理 配置名
        /// </summary>
        public const string CONFIG_BATCH_SIZE = "batchSize";

        /// <summary>
        /// 处理中消息邮箱
        /// </summary>
        public DefaultProcessingMessageMailbox()
        {
            _messageDict = new ConcurrentDictionary<long, ProcessingMessage>();
            _nextSequence = 1;
            _logger = LogManager.GetLogger(GetType());
            LastActiveTime = DateTime.Now;
            Locker = new SpinLock();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="businessKey"></param>
        /// <param name="messageHandler"></param>
        /// <param name="configDict"></param>
        public void Initialize(string businessKey, IProcessingMessageHandler messageHandler, IDictionary<string, string> configDict = null)
        {
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) == 0)
            {
                BusinessKey = businessKey;
                _messageHandler = messageHandler;
                _batchSize = 1000;
                if (configDict != null)
                {
                    if (configDict.ContainsKey(CONFIG_BATCH_SIZE)
                        && !int.TryParse(configDict[CONFIG_BATCH_SIZE], out _batchSize))
                    {
                        _batchSize = 1000;
                    }
                }
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
        /// <param name="message"></param>
        public void Enqueue(ProcessingMessage message)
        {
            lock (_lockObj)
            {
                message.Sequence = _nextSequence;
                message.SetTryDequeueCallback(this.TryDequeue);
                if (_messageDict.TryAdd(message.Sequence, message))
                {
                    _nextSequence++;
                    _logger.Debug($"{GetType().Name} enqueued new message, businessKey: {BusinessKey}, messageId: {message.Message.Id}, messageSequence: {message.Sequence}");
                    TryRun();
                }
                else
                {
                    _logger.Error($"{GetType().Name} enqueue message failed, businessKey: {BusinessKey}, messageId: {message.Message.Id}, messageSequence: {message.Sequence}");
                }
            }
        }

        /// <summary>
        /// 尝试出队
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool TryDequeue(ProcessingMessage message)
        {
            return _messageDict.TryRemove(message.Sequence, out ProcessingMessage _);
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
                Task.Factory.StartNew(ProcessMessagesAsync, TaskCreationOptions.LongRunning);
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
                    var message = GetMessage(_consumingSequence);
                    if (message != null)
                    {
                        await _messageHandler.HandleAsync(message).ConfigureAwait(false);
                    }
                    Interlocked.Increment(ref _consumingSequence);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(string.Format("{0} run has unknown exception, businessKey: {1}", GetType().Name, BusinessKey), ex);
            }
            finally
            {
                CompleteRun();
            }
        }

        private ProcessingMessage GetMessage(long sequence)
        {
            if (_messageDict.TryGetValue(sequence, out ProcessingMessage message))
            {
                return message;
            }
            return null;
        }
    }
}

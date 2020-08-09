using Larva.MessageProcess.Handling;
using Larva.MessageProcess.Processing.Mailboxes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Larva.MessageProcess.Processing
{
    /// <summary>
    /// 默认消息处理器
    /// </summary>
    public class DefaultMessageProcessor : IMessageProcessor
    {
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, IProcessingMessageMailbox> _mailboxDict;
        private readonly Timer _cleanInactiveMailboxTimer;
        private readonly string _subscriber;
        private readonly IMessageHandlerProvider _messageHandlerProvider;
        private readonly IProcessingMessageMailboxProvider _mailboxProvider;
        private readonly bool _continueWhenHandleFail;
        private readonly int _retryIntervalSeconds;
        private readonly int _mailboxTimeoutSeconds;
        private readonly int _cleanInactiveMailboxIntervalSeconds;
        private readonly int _batchSize;
        private volatile int _isRunning;

        /// <summary>
        /// 默认消息处理器
        /// </summary>
        /// <param name="subscriber">订阅者</param>
        /// <param name="messageHandlerProvider">消息处理器提供者</param>
        /// <param name="mailboxProvider">Mailbox提供者</param>
        /// <param name="continueWhenHandleFail">相同BusinessKey的消息处理失败后，是否继续推进</param>
        /// <param name="retryIntervalSeconds">重试间隔秒数</param>
        /// <param name="mailboxTimeoutSeconds">邮箱超时秒数</param>
        /// <param name="cleanInactiveMailboxIntervalSeconds">清理未激活邮箱间隔秒数</param>
        /// <param name="batchSize">批量处理大小</param>
        public DefaultMessageProcessor(
            string subscriber,
            IMessageHandlerProvider messageHandlerProvider,
            IProcessingMessageMailboxProvider mailboxProvider,
            bool continueWhenHandleFail,
            int retryIntervalSeconds,
            int mailboxTimeoutSeconds = 86400,
            int cleanInactiveMailboxIntervalSeconds = 10,
            int batchSize = 1000)
        {
            _mailboxDict = new ConcurrentDictionary<string, IProcessingMessageMailbox>();
            _logger = LoggerManager.GetLogger(GetType());
            _cleanInactiveMailboxTimer = new Timer(CleanInactiveMailbox);

            _subscriber = subscriber;
            _messageHandlerProvider = messageHandlerProvider;
            _mailboxProvider = mailboxProvider;
            _continueWhenHandleFail = continueWhenHandleFail;
            _retryIntervalSeconds = retryIntervalSeconds;
            _mailboxTimeoutSeconds = mailboxTimeoutSeconds;
            _cleanInactiveMailboxIntervalSeconds = cleanInactiveMailboxIntervalSeconds;
            _batchSize = batchSize;
        }

        /// <summary>
        /// 处理
        /// </summary>
        /// <param name="processinMessage">处理中消息</param>
        public void Process(ProcessingMessage processinMessage)
        {
            if (_isRunning == 0) throw new InvalidOperationException($"{GetType().Name} is not running!");
            var businessKey = processinMessage.Message.BusinessKey;
            if (string.IsNullOrEmpty(businessKey))
            {
                throw new ArgumentException("businessKey of message cannot be null or empty, messageId:" + processinMessage.Message.Id);
            }

            var mailbox = _mailboxDict.GetOrAdd(businessKey, x =>
            {
                var newMailBox = _mailboxProvider.CreateMailbox(x, _subscriber, _messageHandlerProvider, _continueWhenHandleFail, _retryIntervalSeconds, _batchSize);
                return newMailBox;
            });

            try
            {
                var lockToken = false;
                mailbox.Locker.Enter(ref lockToken);
                if (mailbox.IsRemoved)
                {
                    mailbox = _mailboxProvider.CreateMailbox(businessKey, _subscriber, _messageHandlerProvider, _continueWhenHandleFail, _retryIntervalSeconds, _batchSize);
                    _mailboxDict.TryAdd(businessKey, mailbox);
                }
                mailbox.Enqueue(processinMessage);
            }
            finally
            {
                if (mailbox.Locker.IsHeldByCurrentThread)
                {
                    mailbox.Locker.Exit(true);
                }
            }
        }

        /// <summary>
        /// 启动
        /// </summary>
        public void Start()
        {
            if (Interlocked.CompareExchange(ref _isRunning, 1, 0) == 0)
            {
                _cleanInactiveMailboxTimer.Change(_cleanInactiveMailboxIntervalSeconds * 1000, _cleanInactiveMailboxIntervalSeconds * 1000);
            }
        }

        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {
            if (Interlocked.CompareExchange(ref _isRunning, 0, 1) == 1)
            {
                _cleanInactiveMailboxTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            while (true)
            {
                var allMailBoxFree = true;
                foreach (var mailBox in _mailboxDict.Values)
                {
                    if (!mailBox.IsFree)
                    {
                        allMailBoxFree = false;
                        break;
                    }
                }
                if (allMailBoxFree)
                {
                    break;
                }
                Thread.Sleep(1);
            }
        }

        private void CleanInactiveMailbox(object state)
        {
            var inactiveList = new List<KeyValuePair<string, IProcessingMessageMailbox>>();

            foreach (var pair in _mailboxDict)
            {
                if (pair.Value.IsInactive(_mailboxTimeoutSeconds))
                {
                    inactiveList.Add(pair);
                }
            }

            foreach (var pair in inactiveList)
            {
                var mailbox = pair.Value;
                try
                {
                    var lockToken = false;
                    mailbox.Locker.TryEnter(ref lockToken);
                    if (lockToken)
                    {
                        if (mailbox.IsInactive(_mailboxTimeoutSeconds))
                        {
                            if (_mailboxDict.TryRemove(pair.Key, out IProcessingMessageMailbox removed))
                            {
                                removed.MarkAsRemoved();
                                _logger.Info($"Removed inactive message mailbox, businessKey: {pair.Key}");
                            }
                        }
                    }
                }
                finally
                {
                    if (mailbox.Locker.IsHeldByCurrentThread)
                    {
                        mailbox.Locker.Exit();
                    }
                }
            }
        }
    }
}

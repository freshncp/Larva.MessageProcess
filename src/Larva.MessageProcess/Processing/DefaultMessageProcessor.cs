using Larva.MessageProcess.Mailboxes;
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
        private volatile int _initialized;
        private volatile int _isRunning;
        private string _subscriber;
        private IProcessingMessageHandler _handler;
        private bool _continueWhenHandleFail;
        private int _retryIntervalSeconds;
        private int _mailboxTimeoutSeconds;
        private int _cleanInactiveMailboxIntervalSeconds;
        private int _batchSize;

        /// <summary>
        /// 默认消息处理器
        /// </summary>
        public DefaultMessageProcessor()
        {
            _mailboxDict = new ConcurrentDictionary<string, IProcessingMessageMailbox>();
            _logger = LoggerManager.GetLogger(GetType());
            _cleanInactiveMailboxTimer = new Timer(CleanInactiveMailbox);
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="subscriber">订阅者</param>
        /// <param name="handler"></param>
        /// <param name="continueWhenHandleFail">相同BusinessKey的消息处理失败后，是否继续推进</param>
        /// <param name="retryIntervalSeconds">重试间隔秒数</param>
        /// <param name="mailboxTimeoutSeconds">邮箱超时秒数</param>
        /// <param name="cleanInactiveMailboxIntervalSeconds">清理未激活邮箱间隔秒数</param>
        /// <param name="batchSize">批量处理大小</param>
        public void Initialize(
            string subscriber,
            IProcessingMessageHandler handler,
            bool continueWhenHandleFail,
            int retryIntervalSeconds,
            int mailboxTimeoutSeconds = 86400,
            int cleanInactiveMailboxIntervalSeconds = 10,
            int batchSize = 1000)
        {
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) == 0)
            {
                _subscriber = subscriber;
                _handler = handler;
                _continueWhenHandleFail = continueWhenHandleFail;
                _retryIntervalSeconds = retryIntervalSeconds;
                _mailboxTimeoutSeconds = mailboxTimeoutSeconds;
                _cleanInactiveMailboxIntervalSeconds = cleanInactiveMailboxIntervalSeconds;
                _batchSize = batchSize;
            }
        }

        /// <summary>
        /// 处理
        /// </summary>
        /// <param name="processinMessage"></param>
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
                var newMailBox = (IProcessingMessageMailbox)ObjectContainer.Resolve(typeof(IProcessingMessageMailbox), typeof(DefaultProcessingMessageMailbox));
                newMailBox.Initialize(x, _subscriber, _handler, _continueWhenHandleFail, _retryIntervalSeconds, _batchSize);
                return newMailBox;
            });

            try
            {
                var lockToken = false;
                mailbox.Locker.Enter(ref lockToken);
                if (mailbox.IsRemoved)
                {
                    mailbox = (IProcessingMessageMailbox)ObjectContainer.Resolve(typeof(IProcessingMessageMailbox), typeof(DefaultProcessingMessageMailbox));
                    mailbox.Initialize(businessKey, _subscriber, _handler, _continueWhenHandleFail, _retryIntervalSeconds, _batchSize);
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

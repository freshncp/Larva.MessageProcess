using Larva.MessageProcess.MailBoxes;
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
        private volatile int _initialized;
        private volatile int _isRunning;
        private IProcessingMessageHandler _handler;
        private int _timeoutSeconds;
        private int _cleanInactiveMailboxIntervalSeconds;
        private Timer _cleanInactiveMailboxTimer;
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
        /// <param name="handler"></param>
        /// <param name="timeoutSeconds"></param>
        /// <param name="cleanInactiveMailboxIntervalSeconds"></param>
        /// <param name="batchSize"></param>
        public void Initialize(IProcessingMessageHandler handler, int timeoutSeconds = 86400, int cleanInactiveMailboxIntervalSeconds = 10, int batchSize = 1000)
        {
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) == 0)
            {
                _handler = handler;
                _timeoutSeconds = timeoutSeconds;
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
                var newMailBox = (IProcessingMessageMailbox)ObjectContainer.Resolve(typeof(IProcessingMessageMailbox),typeof(DefaultProcessingMessageMailbox));
                newMailBox.Initialize(x, _handler, _batchSize);
                return newMailBox;
            });

            try
            {
                var lockToken = false;
                mailbox.Locker.Enter(ref lockToken);
                if (mailbox.IsRemoved)
                {
                    mailbox = (IProcessingMessageMailbox)ObjectContainer.Resolve(typeof(IProcessingMessageMailbox),typeof(DefaultProcessingMessageMailbox));
                    mailbox.Initialize(businessKey, _handler, _batchSize);
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
                    if (!IsMailBoxFree(mailBox))
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
                if (IsMailBoxAllowRemove(pair.Value))
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
                        if (IsMailBoxAllowRemove(mailbox))
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

        private bool IsMailBoxAllowRemove(IProcessingMessageMailbox mailbox)
        {
            return mailbox.IsInactive(_timeoutSeconds) && IsMailBoxFree(mailbox);
        }

        private bool IsMailBoxFree(IProcessingMessageMailbox mailbox)
        {
            return !mailbox.IsRunning && mailbox.UnhandledCount == 0;
        }
    }
}

using Larva.MessageProcess.Handling;
using Larva.MessageProcess.Processing.Mailboxes;
using Larva.MessageProcess.Processing.Strategies;
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
        private readonly IEliminationStrategy _mailboxEliminationStrategy;
        private readonly ConcurrentDictionary<string, bool> _eliminationKeyDict;
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
        /// <param name="retryIntervalSeconds">重试间隔秒数（-1 表示不重试）</param>
        /// <param name="mailboxEliminationStrategy">mailbox淘汰策略，默认为LFU</param>
        /// <param name="mailboxTimeoutSeconds">mailbox超时秒数</param>
        /// <param name="cleanInactiveMailboxIntervalSeconds">清理不活跃邮箱间隔秒数</param>
        /// <param name="batchSize">批量处理大小</param>
        public DefaultMessageProcessor(
            string subscriber,
            IMessageHandlerProvider messageHandlerProvider,
            IProcessingMessageMailboxProvider mailboxProvider,
            bool continueWhenHandleFail,
            int retryIntervalSeconds,
            IEliminationStrategy mailboxEliminationStrategy = null,
            int mailboxTimeoutSeconds = 600,
            int cleanInactiveMailboxIntervalSeconds = 10,
            int batchSize = 1000)
        {
            if (messageHandlerProvider == null)
            {
                throw new ArgumentNullException(nameof(messageHandlerProvider));
            }
            if (mailboxProvider == null)
            {
                throw new ArgumentNullException(nameof(mailboxProvider));
            }
            _mailboxDict = new ConcurrentDictionary<string, IProcessingMessageMailbox>();
            _logger = LoggerManager.GetLogger(GetType());
            _cleanInactiveMailboxTimer = new Timer(CleanInactiveMailbox);

            _subscriber = subscriber;
            _messageHandlerProvider = messageHandlerProvider;
            _mailboxProvider = mailboxProvider;
            _mailboxEliminationStrategy = mailboxEliminationStrategy ?? new LfuStrategy();
            _eliminationKeyDict = new ConcurrentDictionary<string, bool>();
            _continueWhenHandleFail = continueWhenHandleFail;
            _retryIntervalSeconds = retryIntervalSeconds;
            _mailboxTimeoutSeconds = mailboxTimeoutSeconds;
            _cleanInactiveMailboxIntervalSeconds = cleanInactiveMailboxIntervalSeconds;
            _batchSize = batchSize;

            _mailboxEliminationStrategy.OnKnockedOut += (sender, e) =>
            {
                foreach (var key in e.Keys)
                {
                    _eliminationKeyDict.TryAdd(key, false);
                }
            };
        }

        /// <summary>
        /// 处理
        /// </summary>
        /// <param name="processinMessage">处理中消息</param>
        public void Process(ProcessingMessage processinMessage)
        {
            if (_isRunning == 0) return;
            var businessKey = processinMessage.Message.BusinessKey;
            if (string.IsNullOrEmpty(businessKey))
            {
                throw new ArgumentException("businessKey of message cannot be null or empty, messageId:" + processinMessage.Message.Id);
            }

            _mailboxEliminationStrategy.AddKey(businessKey);
            var mailbox = _mailboxDict.GetOrAdd(
                businessKey,
                x => _mailboxProvider.CreateMailbox(x, _subscriber, _messageHandlerProvider, _continueWhenHandleFail, _retryIntervalSeconds, _batchSize)
            );

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
                _mailboxEliminationStrategy.Start();
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
                _mailboxEliminationStrategy.Stop();
            }
            foreach (var mailbox in _mailboxDict.Values)
            {
                mailbox.Stop();
            }
            while (true)
            {
                var allMailboxFree = true;
                foreach (var mailbox in _mailboxDict.Values)
                {
                    if (!mailbox.IsFree)
                    {
                        allMailboxFree = false;
                        break;
                    }
                }
                if (allMailboxFree)
                {
                    break;
                }
                Thread.Sleep(1);
            }
            _eliminationKeyDict.Clear();
        }

        private void CleanInactiveMailbox(object state)
        {
            var inactiveList = new List<KeyValuePair<string, IProcessingMessageMailbox>>();

            foreach (var pair in _mailboxDict)
            {
                if (_eliminationKeyDict.ContainsKey(pair.Key) || pair.Value.IsInactive(_mailboxTimeoutSeconds))
                {
                    inactiveList.Add(pair);
                }
            }
            _eliminationKeyDict.Clear();

            foreach (var pair in inactiveList)
            {
                var mailbox = pair.Value;
                try
                {
                    var lockToken = false;
                    mailbox.Locker.TryEnter(ref lockToken);
                    if (lockToken)
                    {
                        if (mailbox.IsFree)
                        {
                            mailbox.Stop();
                            mailbox.MarkAsRemoved();
                            if (_mailboxDict.TryRemove(pair.Key, out IProcessingMessageMailbox removed))
                            {
                                removed.Stop();
                                removed.MarkAsRemoved();
                                _logger.Info($"Removed inactive message mailbox, businessKey: {pair.Key}");
                            }
                        }
                        else
                        {
                            _eliminationKeyDict.TryAdd(mailbox.BusinessKey, false);
                        }
                    }
                }
                catch
                {
                    _eliminationKeyDict.TryAdd(mailbox.BusinessKey, false);
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

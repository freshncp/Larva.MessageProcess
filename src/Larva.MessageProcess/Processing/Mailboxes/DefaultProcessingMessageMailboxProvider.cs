using Larva.MessageProcess.Handling;

namespace Larva.MessageProcess.Processing.Mailboxes
{
    /// <summary>
    /// 处理中消息邮箱提供者
    /// </summary>
    public class DefaultProcessingMessageMailboxProvider : IProcessingMessageMailboxProvider
    {
        private readonly IProcessingMessageHandler _processingMessageHandler;

        /// <summary>
        /// 处理中消息邮箱提供者
        /// </summary>
        /// <param name="processingMessageHandler">处理中消息处理器</param>
        public DefaultProcessingMessageMailboxProvider(IProcessingMessageHandler processingMessageHandler)
        {
            _processingMessageHandler = processingMessageHandler;
        }

        /// <summary>
        /// 创建邮箱
        /// </summary>
        /// <param name="businessKey">业务键</param>
        /// <param name="subscriber">订阅者</param>
        /// <param name="messageHandlerProvider">消息处理器提供者</param>
        /// <param name="continueWhenHandleFail">相同BusinessKey的消息处理失败后，是否继续推进</param>
        /// <param name="retryIntervalSeconds">重试间隔秒数（-1 表示不重试）</param>
        /// <param name="batchSize">批量处理大小</param>
        /// <returns></returns>
        public IProcessingMessageMailbox CreateMailbox(string businessKey, string subscriber, IMessageHandlerProvider messageHandlerProvider, bool continueWhenHandleFail, int retryIntervalSeconds, int batchSize)
        {
            return new DefaultProcessingMessageMailbox(businessKey, subscriber, messageHandlerProvider, _processingMessageHandler, continueWhenHandleFail, retryIntervalSeconds, batchSize);
        }
    }
}
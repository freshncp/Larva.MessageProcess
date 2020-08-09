using Larva.MessageProcess.Processing;
using System;
using System.Threading;

namespace Larva.MessageProcess.Mailboxes
{
    /// <summary>
    /// 处理中消息邮箱 接口
    /// </summary>
    public interface IProcessingMessageMailbox
    {
        /// <summary>
        /// 业务键
        /// </summary>
        string BusinessKey { get; }

        /// <summary>
        /// 订阅者
        /// </summary>
        string Subscriber { get; }

        /// <summary>
        /// 最后一次激活时间，作为清理的依据之一
        /// </summary>
        DateTime LastActiveTime { get; }

        /// <summary>
        /// 锁
        /// </summary>
        SpinLock Locker { get; }

        /// <summary>
        /// 是否已移除
        /// </summary>
        bool IsRemoved { get; }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// 未处理数
        /// </summary>
        long UnhandledCount { get; }

        /// <summary>
        /// 未处理问题数
        /// </summary>
        long UnhandledProblemCount { get; }

        /// <summary>
        /// 是否空闲，作为清理的依据之一
        /// </summary>
        bool IsFree { get; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="businessKey">业务键</param>
        /// <param name="subscriber">订阅者</param>
        /// <param name="processingMessageHandler">消息处理器</param>
        /// <param name="continueWhenHandleFail">相同BusinessKey的消息处理失败后，是否继续推进</param>
        /// <param name="retryIntervalSeconds">重试间隔秒数</param>
        /// <param name="batchSize">批量处理大小</param>
        void Initialize(string businessKey, string subscriber, IProcessingMessageHandler processingMessageHandler, bool continueWhenHandleFail, int retryIntervalSeconds, int batchSize);
        
        /// <summary>
        /// 入队
        /// </summary>
        /// <param name="message"></param>
        void Enqueue(ProcessingMessage message);

        /// <summary>
        /// 尝试出队
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        bool TryDequeue(ProcessingMessage message);

        /// <summary>
        /// 重置消费序号
        /// </summary>
        /// <param name="consumingSequence"></param>
        void ResetConsumingSequence(long consumingSequence);

        /// <summary>
        /// 是否未激活
        /// </summary>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        bool IsInactive(int timeoutSeconds);

        /// <summary>
        /// 标记为已移除
        /// </summary>
        void MarkAsRemoved();
    }
}

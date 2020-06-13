using Larva.MessageProcess.Messaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Larva.MessageProcess.Processing
{
    /// <summary>
    /// 处理中消息
    /// </summary>
    public sealed class ProcessingMessage
    {
        private Func<ProcessingMessage, bool> _tryDequeueFunc;

        /// <summary>
        /// 处理中消息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageSubscriber"></param>
        /// <param name="executingContext"></param>
        /// <param name="items"></param>
        /// <param name="continueWhenHandleFail">相同BusinessKey的消息处理失败后，是否继续推进</param>
        public ProcessingMessage(IMessage message, string messageSubscriber, IMessageExecutingContext executingContext, IDictionary<string, string> items = null, bool continueWhenHandleFail = false)
        {
            Message = message;
            MessageSubscriber = messageSubscriber;
            ExecutingContext = executingContext;
            Items = items ?? new Dictionary<string, string>();
            ContinueWhenHandleFail = continueWhenHandleFail;
        }

        /// <summary>
        /// 消息
        /// </summary>
        public IMessage Message { get; private set; }

        /// <summary>
        /// 消息订阅者
        /// </summary>
        public string MessageSubscriber { get; private set; }

        /// <summary>
        /// 执行上下文
        /// </summary>
        public IMessageExecutingContext ExecutingContext { get; private set; }

        /// <summary>
        /// 相同BusinessKey的消息处理失败后，是否继续推进
        /// </summary>
        public bool ContinueWhenHandleFail { get; private set; }

        /// <summary>
        /// 扩展项
        /// </summary>
        public IDictionary<string, string> Items { get; private set; }

        /// <summary>
        /// 顺序
        /// </summary>
        public long Sequence { get; set; }

        /// <summary>
        /// 设置 TryDequeue 回调
        /// </summary>
        /// <param name="tryDequeueFunc"></param>
        public void SetTryDequeueCallback(Func<ProcessingMessage, bool> tryDequeueFunc)
        {
            _tryDequeueFunc = tryDequeueFunc;
        }

        /// <summary>
        /// 完成
        /// </summary>
        /// <param name="messageResult"></param>
        /// <returns></returns>
        public async Task CompleteAsync(MessageExecutingResult messageResult)
        {
            if (_tryDequeueFunc(this))
            {
                await ExecutingContext.NotifyMessageExecutedAsync(messageResult);
            }
        }
    }
}

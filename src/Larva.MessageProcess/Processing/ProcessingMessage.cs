using Larva.MessageProcess.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Larva.MessageProcess.Processing
{
    /// <summary>
    /// 处理中消息
    /// </summary>
    public sealed class ProcessingMessage
    {
        /// <summary>
        /// 处理中消息
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="executingContext">消息执行上下文</param>
        /// <param name="extraDatas">额外数据</param>
        public ProcessingMessage(IMessage message, IMessageExecutingContext executingContext, IDictionary<string, string> extraDatas = null)
        {
            Message = message;
            ExecutingContext = executingContext;
            ExtraDatas = extraDatas ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// 消息
        /// </summary>
        public IMessage Message { get; private set; }

        /// <summary>
        /// 执行上下文
        /// </summary>
        public IMessageExecutingContext ExecutingContext { get; private set; }

        /// <summary>
        /// 额外数据
        /// </summary>
        public IDictionary<string, string> ExtraDatas { get; private set; }

        /// <summary>
        /// 顺序
        /// </summary>
        public long Sequence { get; set; }

        /// <summary>
        /// 完成
        /// </summary>
        /// <param name="messageResult">消息结果</param>
        /// <returns></returns>
        public async Task CompleteAsync(MessageExecutingResult messageResult)
        {
            await ExecutingContext.NotifyMessageExecutedAsync(messageResult);
        }
    }
}

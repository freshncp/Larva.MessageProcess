using Larva.MessageProcess.Messaging;
using System.Threading.Tasks;

namespace Larva.MessageProcess.Handling
{
    /// <summary>
    /// 消息处理器 接口
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public interface IMessageHandler<TMessage>
        where TMessage : class, IMessage
    {
        /// <summary>
        /// 处理
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="ctx">消息上下文</param>
        /// <returns></returns>
        Task HandleAsync(TMessage message, IMessageContext ctx);
    }
}

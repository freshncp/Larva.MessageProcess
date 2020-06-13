using Larva.MessageProcess.Messaging;
using System.Threading.Tasks;

namespace Larva.MessageProcess.Handlers
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
        /// <param name="message"></param>
        /// <param name="ctx"></param>
        /// <returns></returns>
        Task HandleAsync(TMessage message, IMessageContext ctx);
    }
}

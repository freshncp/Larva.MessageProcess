using Larva.MessageProcess.Handling;
using System.Threading.Tasks;

namespace Larva.MessageProcess.Processing
{
    /// <summary>
    /// 消息执行上下文
    /// </summary>
    public interface IMessageExecutingContext : IMessageContext
    {
        /// <summary>
        /// 通知消息已执行
        /// </summary>
        /// <param name="messageResult"></param>
        /// <returns></returns>
        Task NotifyMessageExecutedAsync(MessageExecutingResult messageResult);
    }
}

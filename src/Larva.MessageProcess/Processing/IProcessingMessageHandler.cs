using System.Threading.Tasks;
using Larva.MessageProcess.Handling;

namespace Larva.MessageProcess.Processing
{
    /// <summary>
    /// 处理中消息处理器
    /// </summary>
    public interface IProcessingMessageHandler
    {
        /// <summary>
        /// 处理
        /// </summary>
        /// <param name="subscriber">订阅者</param>
        /// <param name="processingMessage">处理中消息</param>
        /// <param name="messageHandlerProvider">消息处理器提供者</param>
        /// <returns></returns>
        Task<bool> HandleAsync(string subscriber, ProcessingMessage processingMessage, IMessageHandlerProvider messageHandlerProvider);
    }
}

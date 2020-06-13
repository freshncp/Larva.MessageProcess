using System.Threading.Tasks;

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
        /// <param name="processingMessage"></param>
        /// <returns></returns>
        Task HandleAsync(ProcessingMessage processingMessage);
    }
}

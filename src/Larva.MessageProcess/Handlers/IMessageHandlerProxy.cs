using Larva.MessageProcess.Messaging;
using System.Threading.Tasks;

namespace Larva.MessageProcess.Handlers
{
    /// <summary>
    /// 消息处理器代理 接口
    /// </summary>
    public interface IMessageHandlerProxy
    {
        /// <summary>
        /// 处理
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ctx"></param>
        /// <returns></returns>
        Task HandleAsync(IMessage message, IMessageContext ctx);

        /// <summary>
        /// 获取封装的对象
        /// </summary>
        /// <returns></returns>
        object GetWrappedObject();
    }
}

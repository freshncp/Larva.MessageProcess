using Larva.MessageProcess.Messaging;
using System.Threading.Tasks;

namespace Larva.MessageProcess.Handling
{
    /// <summary>
    /// 消息处理器代理 接口
    /// </summary>
    public interface IMessageHandlerProxy
    {
        /// <summary>
        /// 处理
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="ctx">消息上下文</param>
        /// <returns></returns>
        Task HandleAsync(IMessage message, IMessageContext ctx);

        /// <summary>
        /// 获取被代理的对象
        /// </summary>
        /// <returns></returns>
        object GetProxiedObject();
    }
}

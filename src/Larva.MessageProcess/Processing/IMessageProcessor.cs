namespace Larva.MessageProcess.Processing
{
    /// <summary>
    /// 消息处理器 接口
    /// </summary>
    public interface IMessageProcessor
    {
        /// <summary>
        /// 处理
        /// </summary>
        /// <param name="processingMessage"></param>
        void Process(ProcessingMessage processingMessage);
        
        /// <summary>
        /// 启动
        /// </summary>
        void Start();

        /// <summary>
        /// 停止
        /// </summary>
        void Stop();
    }
}

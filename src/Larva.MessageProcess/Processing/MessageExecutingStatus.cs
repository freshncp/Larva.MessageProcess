namespace Larva.MessageProcess.Processing
{
    /// <summary>
    /// 消息执行状态
    /// </summary>
    public enum MessageExecutingStatus
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,

        /// <summary>
        /// 成功
        /// </summary>
        Success = 1,

        /// <summary>
        /// 失败
        /// </summary>
        Failed = 2,

        /// <summary>
        /// 未找到Handler
        /// </summary>
        HandlerNotFound = 3,
    }
}

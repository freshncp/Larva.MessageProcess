namespace Larva.MessageProcess.Handlers
{
    /// <summary>
    /// 消息上下文
    /// </summary>
    public interface IMessageContext
    {
        /// <summary>
        /// 获取结果
        /// </summary>
        /// <returns></returns>
        string GetResult();

        /// <summary>
        /// 设置结果
        /// </summary>
        /// <param name="result"></param>
        void SetResult(string result);
    }
}

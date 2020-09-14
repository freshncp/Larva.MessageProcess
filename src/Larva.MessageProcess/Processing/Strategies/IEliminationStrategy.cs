using System;

namespace Larva.MessageProcess.Processing.Strategies
{
    /// <summary>
    /// 淘汰策略
    /// </summary>
    public interface IEliminationStrategy
    {
        /// <summary>
        /// 已淘汰事件
        /// </summary>
        event EventHandler<KnockedOutEventArgs> OnKnockedOut;

        /// <summary>
        /// 添加Key
        /// </summary>
        /// <param name="key"></param>
        void AddKey(string key);

        /// <summary>
        /// 启动
        /// </summary>
        void Start();

        /// <summary>
        /// 停止
        /// </summary>
        void Stop();
    }

    /// <summary>
    /// 已淘汰事件参数
    /// </summary>
    public class KnockedOutEventArgs : EventArgs
    {
        /// <summary>
        /// 已淘汰事件参数
        /// </summary>
        /// <param name="keys">Keys列表</param>
        public KnockedOutEventArgs(string[] keys)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }
            Keys = keys;
        }

        /// <summary>
        /// Keys列表
        /// </summary>
        public string[] Keys { get; }
    }
}

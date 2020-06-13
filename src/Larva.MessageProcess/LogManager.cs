using System;

namespace Larva.MessageProcess
{
    /// <summary>
    /// 日志管理器
    /// </summary>
    public sealed class LogManager
    {
        private static ILogProvider _logProvider;
        private static ILogProvider _defaultLogProvider = new ConsoleLogProvider();

        /// <summary>
        /// 设置日志提供者
        /// </summary>
        /// <param name="logProvider"></param>
        public static void SetLogProvider(ILogProvider logProvider)
        {
            _logProvider = logProvider;
        }

        /// <summary>
        /// 获取日志
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static ILog GetLogger(Type source)
        {
            if (_logProvider == null)
            {
                return _defaultLogProvider.GetLogger(source);
            }
            return _logProvider.GetLogger(source);
        }
    }

    /// <summary>
    /// 日志提供者 接口
    /// </summary>
    public interface ILogProvider
    {
        /// <summary>
        /// 获取日志
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        ILog GetLogger(Type source);
    }

    /// <summary>
    /// 日志
    /// </summary>
    public interface ILog
    {
        /// <summary>
        /// 跟踪
        /// </summary>
        /// <param name="content"></param>
        void Trace(string content);

        /// <summary>
        /// 调试
        /// </summary>
        /// <param name="content"></param>
        void Debug(string content);

        /// <summary>
        /// 信息
        /// </summary>
        /// <param name="content"></param>
        void Info(string content);

        /// <summary>
        /// 警告
        /// </summary>
        /// <param name="content"></param>
        void Warn(string content);

        /// <summary>
        /// 错误
        /// </summary>
        /// <param name="content"></param>
        void Error(string content);

        /// <summary>
        /// 错误
        /// </summary>
        /// <param name="content"></param>
        /// <param name="exception"></param>
        void Error(string content, Exception exception);

        /// <summary>
        /// 致命错误
        /// </summary>
        /// <param name="content"></param>
        void Fatal(string content);

        /// <summary>
        /// 致命错误
        /// </summary>
        /// <param name="content"></param>
        /// <param name="exception"></param>
        void Fatal(string content, Exception exception);
    }
}
using Larva.MessageProcess.Messaging;
using System;

namespace Larva.MessageProcess.Processing
{
    /// <summary>
    /// 消息执行结果
    /// </summary>
    [Serializable]
    public class MessageExecutingResult
    {
        /// <summary>
        /// 消息执行结果
        /// </summary>
        public MessageExecutingResult() { }

        /// <summary>
        /// 消息执行结果
        /// </summary>
        /// <param name="status">状态</param>
        /// <param name="rawMessage">原始消息</param>
        /// <param name="subscriber">订阅者</param>
        /// <param name="result">结果数据</param>
        /// <param name="resultType">结果数据类型</param>
        /// <param name="stackTrace">堆栈跟踪信息</param>
        public MessageExecutingResult(MessageExecutingStatus status, IMessage rawMessage, string subscriber,
            string result = null, string resultType = null, string stackTrace = null)
        {
            Status = status;
            RawMessage = rawMessage;
            Subscriber = subscriber;
            Result = result;
            ResultType = resultType;
            StackTrace = stackTrace;
        }

        /// <summary>
        /// 状态
        /// </summary>
        public MessageExecutingStatus Status { get; private set; }

        /// <summary>
        /// 原始消息
        /// </summary>
        public IMessage RawMessage { get; private set; }

        /// <summary>
        /// 订阅者
        /// </summary>
        public string Subscriber { get; private set; }

        /// <summary>
        /// 结果数据
        /// </summary>
        public string Result { get; private set; }

        /// <summary>
        /// 结果数据类型
        /// </summary>
        public string ResultType { get; private set; }

        /// <summary>
        /// 堆栈跟踪信息
        /// </summary>
        public string StackTrace { get; private set; }

        /// <summary>
        /// 消息执行结果字符串形式
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("[MessageId={0},MessageType={1},Subscriber={2},BusinessKey={3},Status={4},Result=\"{5}\",ResultType={6}]",
                RawMessage.Id,
                RawMessage.GetMessageTypeName(),
                Subscriber,
                RawMessage.BusinessKey,
                Status,
                Result,
                ResultType);
        }
    }
}

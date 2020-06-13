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
        /// <param name="status"></param>
        /// <param name="rawMessage"></param>
        /// <param name="messageSubscriber"></param>
        /// <param name="result"></param>
        /// <param name="resultType"></param>
        /// <param name="stackTrace"></param>
        public MessageExecutingResult(MessageExecutingStatus status, IMessage rawMessage, string messageSubscriber,
            string result = null, string resultType = null, string stackTrace = null)
        {
            Status = status;
            RawMessage = rawMessage;
            MessageSubscriber = messageSubscriber;
            Result = result;
            ResultType = resultType;
            StackTrace = stackTrace;
        }

        /// <summary>
        /// 状态
        /// </summary>
        public MessageExecutingStatus Status { get; private set; }

        /// <summary>
        /// 结果数据
        /// </summary>
        public string Result { get; private set; }

        /// <summary>
        /// 结果数据类型
        /// </summary>
        public string ResultType { get; private set; }

        /// <summary>
        /// 原始消息
        /// </summary>
        public IMessage RawMessage { get; private set; }

        /// <summary>
        /// 消息订阅者
        /// </summary>
        public string MessageSubscriber { get; private set; }

        /// <summary>
        /// 堆栈跟踪信息
        /// </summary>
        public string StackTrace { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("[MessageId={0},MessageType={1},Subscriber={2},BusinessKey={3},Status={4},Result=\"{5}\",ResultType={6}]",
                RawMessage.Id,
                RawMessage.GetMessageTypeName(),
                MessageSubscriber,
                RawMessage.BusinessKey,
                Status,
                Result,
                ResultType);
        }
    }
}

﻿using System;

namespace Larva.MessageProcess.Interception
{
    /// <summary>
    /// 调用
    /// </summary>
    public interface IInvocation
    {
        /// <summary>
        /// 方法名
        /// </summary>
        string MethodName { get; }

        /// <summary>
        /// 参数类型
        /// </summary>
        Type[] ArgumentTypes { get; }

        /// <summary>
        /// 返回值类型
        /// </summary>
        Type ReturnValueType { get; }

        /// <summary>
        /// 调用目标对象
        /// </summary>
        object InvocationTarget { get; }

        /// <summary>
        /// 代理对象
        /// </summary>
        object Proxy { get; }

        /// <summary>
        /// 参数
        /// </summary>
        object[] Arguments { get; }

        /// <summary>
        /// 返回值
        /// </summary>
        object ReturnValue { get; }

        /// <summary>
        /// 处理
        /// </summary>
        void Proceed();
    }
}

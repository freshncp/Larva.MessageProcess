using System;
using System.Collections.Generic;

namespace Larva.MessageProcess.Interception
{
    /// <summary>
    /// 调用 抽象类
    /// </summary>
    /// <typeparam name="TReturnValueType">返回值类型</typeparam>
    public abstract class InvocationBase<TReturnValueType> : IInvocation
    {
        private Queue<IInterceptor> _interceptors;

        /// <summary>
        /// 调用 抽象类
        /// </summary>
        /// <param name="interceptors">拦截器</param>
        /// <param name="arguments">参数</param>
        /// <param name="invocationTarget">调用目标对象</param>
        /// <param name="methodNameInvocationTarget">调用目标的方法名</param>
        /// <param name="proxy">代理对象</param>
        /// <param name="methodName">代理的方法名</param>
        protected InvocationBase(IInterceptor[] interceptors, object[] arguments, object invocationTarget, string methodNameInvocationTarget, object proxy, string methodName)
        {
            if (interceptors != null && interceptors.Length > 0)
            {
                _interceptors = new Queue<IInterceptor>(interceptors);
            }
            Arguments = arguments;
            InvocationTarget = invocationTarget;
            MethodNameInvocationTarget = methodNameInvocationTarget;
            Proxy = proxy;
            MethodName = methodName;
        }

        /// <summary>
        /// 参数
        /// </summary>
        public object[] Arguments { get; private set; }

        /// <summary>
        /// 调用目标对象
        /// </summary>
        public object InvocationTarget { get; private set; }

        /// <summary>
        /// 调用目标的方法名
        /// </summary>
        public string MethodNameInvocationTarget { get; private set; }

        /// <summary>
        /// 代理对象
        /// </summary>
        public object Proxy { get; private set; }

        /// <summary>
        /// 代理的方法名
        /// </summary>
        public string MethodName { get; private set; }

        /// <summary>
        /// 返回值类型
        /// </summary>
        public Type ReturnValueType => typeof(TReturnValueType);

        /// <summary>
        /// 返回值
        /// </summary>
        public object ReturnValue { get; private set; }

        /// <summary>
        /// 处理
        /// </summary>
        public void Proceed()
        {
            if (_interceptors != null && _interceptors.Count > 0)
            {
                var interceptor = _interceptors.Dequeue();
                interceptor.Intercept(this);
            }
            else
            {
                ReturnValue = InvokeInvocationTarget();
            }
        }

        /// <summary>
        /// 调用目标对象
        /// </summary>
        /// <returns></returns>
        protected abstract TReturnValueType InvokeInvocationTarget();
    }
}
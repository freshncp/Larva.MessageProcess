using System;
using System.Threading.Tasks;

namespace Larva.MessageProcess.Interception
{
    /// <summary>
    /// 标准拦截器
    /// </summary>
    public abstract class StandardInterceptor : IInterceptor, IDisposable
    {
        /// <summary>
        /// 拦截
        /// </summary>
        /// <param name="invocation">调用</param>
        public void Intercept(IInvocation invocation)
        {
            if (typeof(Task).IsAssignableFrom(invocation.ReturnValueType))
            {
                var isFailBeforePostProceed = true;
                try
                {
                    PreProceed(invocation);
                    invocation.Proceed();
                    isFailBeforePostProceed = false;
                    if (invocation.ReturnValue == null)
                    {
                        try
                        {
                            PostProceed(invocation);
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        ExceptionThrown(invocation, ex);
                    }
                    catch { }
                    if (ex is AggregateException)
                    {
                        throw ex;
                    }
                    else
                    {
                        throw new AggregateException(ex);
                    }
                }
                finally
                {
                    if (isFailBeforePostProceed
                        || invocation.ReturnValue == null)
                    {
                        Dispose();
                    }
                }
                if (!isFailBeforePostProceed
                    && invocation.ReturnValue != null)
                {
                    ((Task)invocation.ReturnValue).ContinueWith((lastTask, state) =>
                    {
                        if (lastTask.Exception == null)
                        {
                            try
                            {
                                PostProceed((IInvocation)state);
                            }
                            catch { }
                        }
                        else
                        {
                            try
                            {
                                ExceptionThrown((IInvocation)state, lastTask.Exception.InnerExceptions[0]);
                            }
                            catch { }
                        }
                    }, invocation).ContinueWith((lastTask) =>
                    {
                        Dispose();
                    });
                }
            }
            else
            {
                try
                {
                    PreProceed(invocation);
                    invocation.Proceed();
                    try
                    {
                        PostProceed(invocation);
                    }
                    catch { }
                }
                catch (Exception ex)
                {
                    try
                    {
                        ExceptionThrown(invocation, ex);
                    }
                    catch { }
                    throw new AggregateException(ex);
                }
                finally
                {
                    Dispose();
                }
            }
        }

        /// <summary>
        /// 调用前
        /// </summary>
        /// <param name="invocation">调用</param>
        protected abstract void PreProceed(IInvocation invocation);

        /// <summary>
        /// 调用后
        /// </summary>
        /// <param name="invocation">调用</param>
        protected abstract void PostProceed(IInvocation invocation);

        /// <summary>
        /// 调用时抛异常
        /// </summary>
        /// <param name="invocation">调用</param>
        /// <param name="exception">异常</param>
        protected virtual void ExceptionThrown(IInvocation invocation, Exception exception)
        {

        }

        /// <summary>
        /// 释放
        /// </summary>
        public virtual void Dispose()
        {

        }
    }
}

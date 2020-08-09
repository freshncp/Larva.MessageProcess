using System;

namespace Larva.MessageProcess
{
    /// <summary>
    /// 对象容器
    /// </summary>
    public sealed class ObjectContainer
    {
        private static Func<Type, object> _resolverFunc;

        /// <summary>
        /// 设置解析器
        /// </summary>
        /// <param name="resolverFunc"></param>
        public static void SetResolver(Func<Type, object> resolverFunc)
        {
            _resolverFunc = resolverFunc;
        }

        /// <summary>
        /// 解析
        /// </summary>
        /// <param name="implementationType">实现类型</param>
        /// <returns></returns>
        public static object Resolve(Type implementationType)
        {
            try
            {
                if (_resolverFunc != null)
                {
                    return _resolverFunc(implementationType);
                }
            }
            catch { }
            return Activator.CreateInstance(implementationType);
        }
    }
}

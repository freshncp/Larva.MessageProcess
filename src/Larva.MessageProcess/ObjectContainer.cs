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
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public static object Resolve(Type serviceType)
        {
            if (_resolverFunc == null)
            {
                return Activator.CreateInstance(serviceType);
            }
            return _resolverFunc(serviceType);
        }
    }
}

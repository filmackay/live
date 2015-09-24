using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Vertigo.Live
{
    public static partial class LiveValue
    {
        public static ILiveValue<object> ToUntyped<T>(this ILiveValue<T> source)
        {
            return source.Cast<T, object>();
        }

        private static MethodInfo _toUntypedMethodInfo =
            typeof(LiveValue)
                .GetMethods()
                .Single(m => m.Name == "ToUntyped" && m.IsGenericMethodDefinition);

        public static ILiveValue<object> ToUntyped(object source)
        {
            if (source == null)
                return null;

            // find ILiveValue<> interface
            var liveValueInterfaceType =
                source.GetType()
                    .GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ILiveValue<>));
            if (liveValueInterfaceType == null)
                return null;

            // invoke ToUntyped<>
            var method = _toUntypedMethodInfo.MakeGenericMethod(liveValueInterfaceType.GetGenericArguments()[0]);
            var ret = method.Invoke(null, new[] { source });
            return ret as ILiveValue<object>;
        }
    }
}

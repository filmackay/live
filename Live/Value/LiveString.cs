using System;
using System.Linq;

namespace Vertigo.Live
{
    public static class LiveString
    {
        public static ILiveValue<string> Join<TIDelta>(string separator, ILiveCollection<string, TIDelta> values)
            where TIDelta : class, ICollectionDelta<string>
        {
            return values.Aggregate(items => string.Join(separator, items));
        }

        public static ILiveValue<string> Format(ILiveValue<string> format, params ILiveValue<object>[] args)
        {
            return
                LiveFunc
                    .Create<string, object[], string>(
                        (f, a) => string.Format(f, a))
                        (format, args.ToLiveList().Unwrap().ToLiveArray()
                        );
        }

        public static ILiveValue<string[]> Split(this ILiveValue<string> str, ILiveValue<char> delimeter)
        {
            return new Func<char, string, string[]>((d, s) => s.Split(new[] { d }))
                .Create()
                (delimeter, str);
        }
    }
}

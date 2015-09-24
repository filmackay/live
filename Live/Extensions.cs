#define NOPARALLEL

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Vertigo.Live
{
    public partial class Extensions
    {
        public static T CustomAttribute<T>(this Type type)
        {
            return type.GetCustomAttributes(false).OfType<T>().FirstOrDefault();
        }

        public static T CustomAttribute<T>(this PropertyInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttributes(false).OfType<T>().FirstOrDefault();
        }

        public static T[] CustomAttributes<T>(this PropertyInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttributes(false).OfType<T>().ToArray();
        }

        public static ILiveValue<string> ToLiveString<T>(this ILiveValue<T> source)
        {
            return source.SelectStatic(s => s.ToString());
        }

        //        public static void Run(this Action<bool> action, bool runAsync)
        //        {
        //            // fastest way to run a routine synchronous/asynchronously
        //            if (runAsync)
        //                ThreadPool.QueueUserWorkItem(_ => action(true));
        //            else
        //                action(false);
        //        }

        public static void RunRange(this IList<Action> actions, int from, int to)
        {
            for (var i = from; i < to; i++)
                actions[i]();
        }

        public static void FastParallel(this IList<Action> items)
        {
            if (items.Count == 0)
                return;
            if (items.Count == 1)
                items[0]();
            else
                items.FastParallel(RunRange);
        }

        public static void FastParallel<T>(this IList<T> items, Action<IList<T>, int, int> actionRange)
        {
            const int maxPartitionSize = 100;

            if (items.Count == 0)
                return;
            if (items.Count <= maxPartitionSize)
                // small batch
                actionRange(items, 0, items.Count);
            else
            {
                var partitions = Math.Min(Environment.ProcessorCount, 1 + (items.Count / maxPartitionSize));
                var partitionSize = items.Count / partitions;

                // last paritition will get the left over items due to rounding
                Parallel.For(0,
                    partitions,
                    partition => actionRange(items, partition * partitionSize, partition == (partitions - 1) ? items.Count : (partition + 1) * partitionSize));
            }
        }

        //        public static readonly int Processors =
        //#if NOPARALLEL
        //            1;
        //#else
        //            Environment.ProcessorCount;
        //#endif

        public static bool IsSubclassOfGeneric(this Type toCheck, Type generic)
        {
            while (toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                    return true;
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        public static Type GetGenericBase(this Type type, Type generic)
        {
            while (type != typeof(object))
            {
                var cur = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                if (generic == cur)
                    return type;
                type = type.BaseType;
            }
            return null;
        }

        public static void SafeDispose(this IDisposable disposable)
        {
            if (disposable != null)
                disposable.Dispose();
        }

        public static IEnumerable<Tuple<T1, T2>> CrossJoin<T1, T2>(this IEnumerable<T1> outer, IEnumerable<T2> inner)
        {
            return outer.Join(inner, o => true, i => true, (o, i) => new Tuple<T1, T2>(o, i));
        }

        public static IEnumerable<KeyValuePair<TKey, Tuple<T1, T2>>> Join<TKey, T1, T2>(this IEnumerable<KeyValuePair<TKey, T1>> outer, IEnumerable<KeyValuePair<TKey, T2>> inner)
        {
            return outer.Join(inner, o => o.Key, i => i.Key, (o, i) => new KeyValuePair<TKey, Tuple<T1, T2>>(o.Key, new Tuple<T1, T2>(o.Value, i.Value)));
        }

        public static void AddRange<T>(this ICollection<T> items, IEnumerable<T> newItems)
        {
            foreach (var newItem in newItems)
                items.Add(newItem);
        }

        public static void RemoveRange<T>(this ICollection<T> source, IEnumerable<T> items)
        {
            items.ForEach(t => source.Remove(t));
        }

        public static void AddRange<T>(this ISet<T> items, IEnumerable<T> newItems)
        {
            foreach (var newItem in newItems)
                items.Add(newItem);
        }

        public static void RemoveRange<T>(this IList<T> source, int index, int count)
        {
            for (var i = 0; i < count; i++)
                source.RemoveAt(index);
        }

        public static void RemoveRange(this IList source, int index, int count)
        {
            for (var i = 0; i < count; i++)
                source.RemoveAt(index);
        }

        public static void InsertRange<T>(this IList<T> source, int index, IEnumerable<T> items)
        {
            items.ForEach((item, i) => source.Insert(index + i, item));
        }

        public static void InsertRange(this IList source, int index, IEnumerable items)
        {
            items.ForEach((item, i) => source.Insert(index + i, item));
        } 

        public static TValue ValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key, TValue defaultValue = default(TValue))
        {
            TValue value;
            if (!source.TryGetValue(key, out value))
                return defaultValue;
            return value;
        }

        public static int IndexOf<T>(this IEnumerable<T> sequence, T findItem)
        {
            if (sequence == null)
                return -1;

            // use list
            var list = sequence as IList<T>;
            if (list != null)
                return list.IndexOf(findItem);

            // use enumerable
            var index = 0;
            foreach (var item in sequence)
            {
                if (item.Equals(findItem))
                    return index;
                index++;
            }

            return -1;
        }

        public static IEnumerable<int> Range(int from, int to, int step)
        {
            for (var i = from; step > 0 ? i <= to : i >= to; i += step)
                yield return i;
        }

        public static void OnDispatcherInvoke(this DispatcherObject obj, Action action)
        {
            // send to dispatcher if needed
            if (obj.CheckAccess())
                action();
            else
                obj.Dispatcher.Invoke(DispatcherPriority.DataBind, action);
        }

        public static DispatcherOperation OnDispatcherBeginInvoke(this DispatcherObject obj, Action action)
        {
            // send to dispatcher if needed
            if (obj.CheckAccess())
            {
                action();
                return null;
            }
            return obj.Dispatcher.BeginInvoke(DispatcherPriority.DataBind, action);
        }

        public static void IntegrateInto<TSource, TTarget, TKey>(this IEnumerable<TSource> source, IEnumerable<TTarget> target, Func<TSource, TKey> sourceKeySelector, Func<TTarget, TKey> targetKeySelector, Func<TSource, TTarget> insertTarget, Action<TSource, TTarget> updateTarget, Action<TTarget> deleteTarget)
            where TTarget : class
        {
            var sourceByKey = source.ToDictionary(sourceKeySelector, s => s);
            var targetByKey = target.ToDictionary(targetKeySelector, s => s);

            var inserts = sourceByKey.Where(s => !targetByKey.ContainsKey(s.Key)).ToArray();
            var updates = targetByKey.Where(t => sourceByKey.ContainsKey(t.Key)).ToArray();
            var deletes = targetByKey.Where(t => !sourceByKey.ContainsKey(t.Key)).ToArray();

            // insert
            inserts.ForEach(t =>
                {
                    var ret = insertTarget(t.Value);
                    updateTarget(t.Value, ret);
                });

            // update
            updates.ForEach(t => updateTarget(sourceByKey[t.Key], t.Value));

            // delete
            deletes.ForEach(t => deleteTarget(t.Value));
        }

        public static bool UnorderedEqual<T>(this IEnumerable<T> a, IEnumerable<T> b)
        {
            if (a == null && b == null)
                return true;
            if (!(a != null && b != null))
                return false;

            if (a.Count() != b.Count())
                return false;

            var sortedA = a.ToList();
            sortedA.Sort(HashComparer<T>.Default);
            return b.All(item =>
                {
                    var index = sortedA.BinarySearch(item, HashComparer<T>.Default);
                    if (index < 0)
                        return false;

                    // go to first hash match
                    var hashCode = item.GetHashCode();
                    while (index > 0 && sortedA[index - 1].GetHashCode() == hashCode)
                        index--;

                    // go through all hash matches
                    while (index < sortedA.Count && sortedA[index].GetHashCode() == hashCode)
                    {
                        if (EqualityComparer<T>.Default.Equals(sortedA[index], item))
                        {
                            sortedA.RemoveAt(index);
                            return true;
                        }
                        index++;
                    }

                    return false;
                });
        }

        public static void ForEach(this IList<Action> sequence)
        {
            if (sequence == null)
                return;
            for (var i = 0; i < sequence.Count; i++)
                sequence[i]();
        }

        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            if (sequence == null)
                return;
            foreach (var i in sequence)
                action(i);
        }

        public static void ForEach(this IEnumerable<Action> sequence)
        {
            if (sequence == null)
                return;
            foreach (var action in sequence)
                action();
        }

        public static void ForEach(this IEnumerable sequence, Action<object, int> action)
        {
            if (sequence == null)
                return;
            var index = 0;
            foreach (var item in sequence)
            {
                action(item, index);
                index++;
            }
        }

        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T, int, int> action)
        {
            if (sequence == null)
                return;
            var index = 0;
            var count = sequence.Count();
            foreach (var item in sequence)
            {
                action(item, index, count - index);
                index++;
            }
        }

        public static bool IsZero(this TimeSpan time)
        {
            return time.TotalMilliseconds == 0;
        }

        public static bool IsZero(this DateTime date)
        {
            return date.ToBinary() == 0;
        }

        public static void TruncateCount<T>(this IList<T> list, int max)
        {
            while (list.Count > max)
                list.RemoveAt(max);
        }
    }

    public static class DecimalExtensions
    {
        // Avoiding implicit conversions just for clarity
        private static readonly BigInteger Ten = new BigInteger(10);
        private static readonly BigInteger UInt32Mask = new BigInteger(0xffffffffU);

        public static decimal Normalize(this decimal input)
        {
            unchecked
            {
                int[] bits = decimal.GetBits(input);
                BigInteger mantissa =
                    new BigInteger((uint)bits[0]) +
                    (new BigInteger((uint)bits[1]) << 32) +
                    (new BigInteger((uint)bits[2]) << 64);

                int sign = bits[3] & int.MinValue;
                int exponent = (bits[3] & 0xff0000) >> 16;

                // The loop condition here is ugly, because we want
                // to do both the DivRem part and the exponent check :(
                while (exponent > 0)
                {
                    BigInteger remainder;
                    BigInteger divided = BigInteger.DivRem(mantissa, Ten, out remainder);
                    if (remainder != BigInteger.Zero)
                    {
                        break;
                    }
                    exponent--;
                    mantissa = divided;
                }
                // Okay, now put it all back together again...
                bits[3] = (exponent << 16) | sign;
                // For each 32 bits, convert the bottom 32 bits into a uint (which won't
                // overflow) and then cast to int (which will respect the bits, which
                // is what we want)
                bits[0] = (int)(uint)(mantissa & UInt32Mask);
                mantissa >>= 32;
                bits[1] = (int)(uint)(mantissa & UInt32Mask);
                mantissa >>= 32;
                bits[2] = (int)(uint)(mantissa & UInt32Mask);

                return new decimal(bits);
            }
        }

        public static decimal? Normalize(this decimal? input)
        {
            if (!input.HasValue)
                return null;
            return input.Value.Normalize();
        }
    }
}

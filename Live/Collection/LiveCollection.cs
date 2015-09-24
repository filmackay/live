using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vertigo.Live
{
    public interface ILiveCollection<out T, out TIDelta> : ILiveObservable<ICollectionState<T, TIDelta>>
        where TIDelta : ICollectionDelta<T>
    {
    }

    public interface ILiveCollection<out T> : ILiveCollection<T, ICollectionDelta<T>>
    {
    }

    public class LiveCollection<T, TICollection, TIDelta, TDelta, TLiveCollectionInner, TLiveCollectionPush> : LiveCollectionPublisher<T, TICollection, TIDelta, TDelta>
        where TICollection : class, ICollection<T>
        where TIDelta : class, ICollectionDelta<T>
        where TDelta : CollectionDelta<T, TIDelta, TICollection>, TIDelta, new()
        where TLiveCollectionPush : LiveCollectionPublisher<T, TICollection, TIDelta, TDelta>
        where TLiveCollectionInner : LiveCollectionInner<T, TICollection, TIDelta, TDelta, TLiveCollectionPush>, TICollection, new()
    {
        protected LiveCollection(TICollection publishCache, TICollection innerCache, IEnumerable<T> inner)
            : base(publishCache, innerCache, inner)
        {
            _publishInner = new TLiveCollectionInner { Parent = this as TLiveCollectionPush };
        }

        public TLiveCollectionInner PublishInner
        {
            get { return _publishInner; }
        }

        protected TLiveCollectionInner _publishInner;
    }

    public class LiveCollection<T> : LiveCollection<T, ICollection<T>, ICollectionDelta<T>, CollectionDelta<T>, LiveCollectionInner<T>, LiveCollection<T>>, ILiveCollection<T>
    {
        public LiveCollection()
            : this(new Collection<T>(), null)
        {
        }

        public LiveCollection(ICollection<T> publishCache, IEnumerable<T> inner)
            : base(publishCache, new Collection<T>(), inner)
        {
        }

        public static ILiveCollection<T> Empty = new T[0].ToLiveCollection();
        public static ILiveCollection<T> Default = new T[] { default(T) }.ToLiveCollection();
    }

    public static partial class Extensions
    {
        public static LiveCollection<T> ToLiveCollection<T>(this ICollection<T> publishCache)
        {
            return new LiveCollection<T>(publishCache, publishCache);
        }

        public static ILiveCollection<T> ToILiveCollection<T>(this ICollection<T> publishCache)
        {
            return publishCache.ToLiveCollection();
        }

        public static ILiveValue<string> Tag<T, TIDelta>(this ILiveCollection<T, TIDelta> collection, ILiveValue<string> tag)
            where TIDelta : ICollectionDelta<T>
        {
            var collectionObserver = default(LiveObserver<ICollectionState<T, TIDelta>>);
            var tagObserver = default(LiveObserver<IValueState<string>>);

            return
                LiveValueObservable<string>.Create(
                    innerChanged =>
                        {
                            collection.Subscribe(collectionObserver = collection.CreateObserver(innerChanged));
                            tag.Subscribe(tagObserver = tag.CreateObserver(innerChanged));
                        },
                    () => { },
                    (innerChanged, oldState) =>
                    {
                        var tagState = tagObserver.GetState();
                        if (!tagState.Status.IsConnected())
                            return new ValueState<string>();

                        using (var collectionState = collectionObserver.GetState())
                        {
                            string result;
                            if (collectionState.Status.IsConnecting())
                                result =
                                    string.Format("{0}: {1}",
                                        collectionState.Status,
                                        collectionState.Inner == null ? "null" : string.Join(",", collectionState.Inner.Select(e => e.ToString()).ToArray()));
                            else if (collectionState.Status == StateStatus.Connected)
                            {
                                result =
                                    collectionState.Delta.HasChange()
                                        ? string.Format("delta: {0}", collectionState.Delta)
                                        : string.Format("no change");
                            }
                            else
                                result = collection.States().ToString();

                            return
                                oldState.Add(
                                    new ValueState<string>
                                    {
                                        Status = StateStatus.Connected,
                                        NewValue = string.Format("{0} {1} ({2:F3}ms)", tagState.NewValue, result, collectionState.Latency().TotalMilliseconds),
                                        LastUpdated = Math.Max(tagState.LastUpdated, collectionState.LastUpdated),
                                    });
                        }
                    },
                    () =>
                    {
                        collectionObserver.Dispose();
                        tagObserver.Dispose();
                    });
        }

        public static IObservable<string> Trace<T, TIDelta>(this ILiveCollection<T, TIDelta> source, string tag)
            where TIDelta : ICollectionDelta<T>
        {
            return source.Trace(tag.ToLiveConst());
        }

        public static IObservable<string> Trace<T, TIDelta>(this ILiveCollection<T, TIDelta> source, ILiveValue<string> tag)
            where TIDelta : ICollectionDelta<T>
        {
            return source
                .Tag(tag)
                .Values();
        }

        public static ILiveValue<string> TagAll<T, TIDelta>(this ILiveCollection<T, TIDelta> collection, ILiveValue<string> tag)
            where TIDelta : ICollectionDelta<T>
        {
            var collectionObserver = default(LiveObserver<ICollectionState<T, TIDelta>>);
            var tagObserver = default(LiveObserver<IValueState<string>>);

            return
                LiveValueObservable<string>.Create(
                    innerChanged =>
                    {
                        collection.Subscribe(collectionObserver = collection.CreateObserver(innerChanged));
                        tag.Subscribe(tagObserver = tag.CreateObserver(innerChanged));
                    },
                    () => { },
                    (innerChanged, oldState) =>
                    {
                        var tagState = tagObserver.GetState();
                        if (!tagState.Status.IsConnected())
                            return new ValueState<string>();

                        using (var collectionState = collectionObserver.GetState())
                        {
                            var ret = new StringBuilder();
                            switch (collectionState.Status)
                            {
                                case StateStatus.Connecting:
                                case StateStatus.Reconnecting:
                                    ret.AppendFormat("{0} {1}", tagState.NewValue, collectionState.Status);
                                    break;
                                case StateStatus.Connected:
                                case StateStatus.Completing:
                                    if (!collectionState.Delta.HasChange())
                                        ret.AppendFormat("{0} no change", tagState.NewValue);
                                    else
                                        ret.AppendFormat("{0} delta: {1}", tagState.NewValue, collectionState.Delta);
                                    break;
                                default:
                                    ret.AppendFormat("{0} {1}", tagState.NewValue, collectionState.Status);
                                    break;
                            }

                            // inner
                            ret.AppendFormat(" ({0:F3}ms)", collectionState.Latency().TotalMilliseconds);
                            ret.AppendLine();
                            ret.AppendFormat("\tat {0}", DateTime.Now.TimeOfDay);
                            if (collectionState.Inner != null)
                            {
                                ret.AppendLine();
                                ret.AppendLine(collectionState.Inner.Select((item, index) => string.Format("{0}\t#{1}: {2}", tagState.NewValue, index, item)).ToArray().DelimeteredList("\r\n"));
                            }

                            return
                                oldState.Add(
                                    new ValueState<string>
                                    {
                                        Status = StateStatus.Connected,
                                        NewValue = ret.ToString(),
                                        LastUpdated = Math.Max(tagState.LastUpdated, collectionState.LastUpdated),
                                    });
                        }
                    },
                    () =>
                    {
                        collectionObserver.Dispose();
                        tagObserver.Dispose();
                    });
        }


        public static IObservable<string> TraceAll<T, TIDelta>(this ILiveCollection<T, TIDelta> source, string tag)
            where TIDelta : ICollectionDelta<T>
        {
            return source.TraceAll(tag.ToLiveConst());
        }

        public static IObservable<string> TraceAll<T, TIDelta>(this ILiveCollection<T, TIDelta> source, ILiveValue<string> tag)
            where TIDelta : ICollectionDelta<T>
        {
            return
                source
                    .TagAll(tag)
                    .Values();
        }
    }
}

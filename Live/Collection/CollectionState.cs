using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Diagnostics;
using System.Threading;

namespace Vertigo.Live
{
    public interface ICollectionState<out T, out TIDelta> : IState, IDisposable
        where TIDelta : ICollectionDelta<T>
    {
        IDisposable InnerSourceLock(bool detach = true);
        TIDelta Delta { get; }
        IEnumerable<T> Inner { get; }
        bool Disposed { get; }
    }

    public class CollectionState<T, TIDelta, TICollection> : State<ICollectionState<T, TIDelta>, CollectionState<T, TIDelta, TICollection>>, ICollectionState<T, TIDelta>
        where TIDelta : ICollectionDelta<T>
        where TICollection : ICollection<T>
    {
        private RefCountDisposable _sourceLockRefCount;
        public TIDelta Delta { get; private set; }
        public IEnumerable<T> Inner { get; protected set; }

        public CollectionDelta<T, TIDelta, TICollection> DeltaClass
        {
            get
            {
                var ret = Delta.ToMutable<T, TIDelta, TICollection>();
                Delta = ret;
                return ret;
            }
        }

        public override bool HasChange
        {
            get { return Delta.HasChange(); }
        }

        public override StateStatus Add(StateStatus left, StateStatus right)
        {
            return left.Add(right);
        }

        public override bool AddInline(ICollectionState<T, TIDelta> @new)
        {
            using (this.Lock())
            {
                var changed = base.AddInline(@new);

                // inner and delta
                Inner = @new.Inner;
                if (Status.IsDeltaRelevant() && @new.Delta.HasChange())
                {
                    // aggregate deltas
                    if (!Delta.HasChange())
                        Delta = @new.Delta;
                    else
                        DeltaClass.Add(@new.Delta);
                    changed = true;
                }
                else
                    Delta = default(TIDelta);

                // release/add new lock
                SourceLock = @new.InnerSourceLock();

                return changed;
            }
        }

        public override CollectionState<T, TIDelta, TICollection> Copy(bool detachStateLock)
        {
            var copy = base.Copy(detachStateLock);
            copy.Delta = Delta;
            copy.Inner = Inner;
            copy.SourceLock = InnerSourceLock(detachStateLock);
            return copy;
        }

        private IDisposable SourceLock
        {
            set
            {
                // release existing source
                if (_sourceLockRefCount != null)
                    _sourceLockRefCount.Dispose();

                // attach
                if (value != null)
                {
                    // source closure specific to this _sourceLockRefCount
                    var sourceLock = value;
                    _sourceLockRefCount =
                        new RefCountDisposable(Disposable.Create(() =>
                        {
                            Debug.Assert(sourceLock != null);

                            // tear down source access
                            sourceLock.Dispose();
                            sourceLock = null;
                            _sourceLockRefCount = null;
                            Inner = null;

                            // leave delta and status so they can still be used after disposal
                        }));
                }
            }
        }

        public void SetState(StateStatus status, TIDelta delta, IEnumerable<T> inner, long lastUpdated, IDisposable sourceLock)
        {
            using (this.Lock())
            {
                SourceLock = sourceLock;
                Status = status;
                Inner = Status.IsInnerRelevant() ? inner : null;
                Delta = Status.IsDeltaRelevant() ? delta : default(TIDelta);
                LastUpdated = lastUpdated;
            }
        }

        public void PreAddStatus(StateStatus previousStatus)
        {
            using (this.Lock())
            {
                Status = previousStatus.Add(Status);
            }
        }

        public void AddState(StateStatus status, IEnumerable<T> inner, TIDelta newDelta, long lastUpdated, IDisposable sourceLock)
        {
            Debug.Assert(_sourceLockRefCount == null);

            using (this.Lock())
            {
                SourceLock = sourceLock;
                LastUpdated = Math.Max(LastUpdated, lastUpdated);

                Inner = inner;
                if (status.IsConnecting())
                {
                    // start
                    Status = Status.Add(status);
                    Delta = default(TIDelta);
                }
                else
                {
                    if (Status != status)
                    {
                        var newStatus = Status.Add(status);
                        if (newStatus != Status)
                            Status = newStatus;
                    }

                    // apply delta
                    if (newDelta != null)
                        if (Delta == null)
                            // replace delta
                            Delta = newDelta;
                        else
                        {
                            // merge deltas
                            var deltaClass = Delta as CollectionDelta<T, TIDelta, TICollection>;
                            deltaClass.Add(newDelta);
                        }
                }
            }
        }

        public override void NextInline()
        {
            base.NextInline();
            Delta = default(TIDelta);
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Status, Delta);
        }

        public bool Equals(ICollectionState<T, TIDelta> other)
        {
            using (this.Lock())
            {
                return Status == other.Status && Delta.DeltaEquals(other.Delta) && Inner.UnorderedEqual(other.Inner);
            }
        }

        public IDisposable InnerSourceLock(bool detach)
        {
            if (!detach)
                return _sourceLockRefCount == null ? null : _sourceLockRefCount.GetDisposable();

            // state has been extracted from this cache - advance
            Status = Status.Next();
            Delta = default(TIDelta);

            // detach
            var ret = _sourceLockRefCount;
            _sourceLockRefCount = null;
            GC.SuppressFinalize(this);
            return ret;
        }

        public bool Disposed
        {
            get { return _sourceLockRefCount == null; }
        }

        public void Dispose()
        {
            SourceLock = null;
            GC.SuppressFinalize(this);
        }

        ~CollectionState()
        {
            Debug.WriteLine("Warning: CollectionState was not Disposed!");
        }
    }

    public static partial class Extensions
    {
        public static bool HasEffect<T, TIDelta>(this ICollectionState<T, TIDelta> state)
            where TIDelta : ICollectionDelta<T>
        {
            return state.Status.IsPending() || state.Delta.HasChange();
        }

        public static CollectionState<TResult, TResultIDelta, TResultICollection> Extract<TSource, TSourceIDelta, TResult, TResultIDelta, TResultICollection>(this ICollectionState<TSource, TSourceIDelta> delta, bool detach, Func<IEnumerable<TSource>, TSourceIDelta, TResultIDelta> deltaConverter, Func<IEnumerable<TSource>, IEnumerable<TResult>> innerSelector)
            where TSourceIDelta : ICollectionDelta<TSource>
            where TResultIDelta : ICollectionDelta<TResult>
            where TResultICollection : ICollection<TResult>
        {
            using (delta.Lock())
            {
                // extract state
                var ret = new CollectionState<TResult, TResultIDelta, TResultICollection>();
                ret.SetState(delta.Status,
                             deltaConverter(delta.Inner, delta.Delta),
                             innerSelector(delta.Inner),
                             delta.LastUpdated,
                             delta.InnerSourceLock(detach));
                return ret;
            }
        }

        public static CollectionState<TResult, ICollectionDelta<TResult>, ICollection<TResult>> Extract<TSource, TSourceIDelta, TResult>(this ICollectionState<TSource, TSourceIDelta> delta, bool detach, Func<IEnumerable<TSource>, TSourceIDelta, ICollectionDelta<TResult>> deltaConverter, Func<IEnumerable<TSource>, IEnumerable<TResult>> innerSelector)
            where TSourceIDelta : ICollectionDelta<TSource>
        {
            return delta.Extract<TSource, TSourceIDelta, TResult, ICollectionDelta<TResult>, ICollection<TResult>>(detach, deltaConverter, innerSelector);
        }

        public static CollectionState<TResult, IListDelta<TResult>, IList<TResult>> Extract<TSource, TSourceIDelta, TResult>(this ICollectionState<TSource, TSourceIDelta> delta, bool detach, Func<IEnumerable<TSource>, TSourceIDelta, IListDelta<TResult>> deltaConverter, Func<IEnumerable<TSource>, IEnumerable<TResult>> innerSelector)
            where TSourceIDelta : ICollectionDelta<TSource>
        {
            return delta.Extract<TSource, TSourceIDelta, TResult, IListDelta<TResult>, IList<TResult>>(detach, deltaConverter, innerSelector);
        }

        public static CollectionState<TResult, ISetDelta<TResult>, ISet<TResult>> Extract<TSource, TSourceIDelta, TResult>(this ICollectionState<TSource, TSourceIDelta> delta, bool detach, Func<IEnumerable<TSource>, TSourceIDelta, ISetDelta<TResult>> deltaConverter, Func<IEnumerable<TSource>, IEnumerable<TResult>> innerSelector)
            where TSourceIDelta : ICollectionDelta<TSource>
        {
            return delta.Extract<TSource, TSourceIDelta, TResult, ISetDelta<TResult>, ISet<TResult>>(detach, deltaConverter, innerSelector);
        }

        public static CollectionState<KeyValuePair<TResultKey, TResultValue>, IDictionaryDelta<TResultKey, TResultValue>, IDictionary<TResultKey, TResultValue>> Extract<TSource, TSourceIDelta, TResultKey, TResultValue>(this ICollectionState<TSource, TSourceIDelta> delta, bool detach, Func<IEnumerable<TSource>, TSourceIDelta, IDictionaryDelta<TResultKey, TResultValue>> deltaConverter, Func<IEnumerable<TSource>, IEnumerable<KeyValuePair<TResultKey, TResultValue>>> innerSelector)
            where TSourceIDelta : ICollectionDelta<TSource>
        {
            return delta.Extract<TSource, TSourceIDelta, KeyValuePair<TResultKey, TResultValue>, IDictionaryDelta<TResultKey, TResultValue>, IDictionary<TResultKey, TResultValue>>(detach, deltaConverter, innerSelector);
        }
    }
}

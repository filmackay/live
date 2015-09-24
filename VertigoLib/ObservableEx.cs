using System;
using System.Net;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using System.Web;

namespace Vertigo
{
    public static class ObservableEx
    {
        public static IObservable<T> Create<T>(this Func<Task<T>> taskFactory)
        {
            return Observable.Defer(() => taskFactory().ToObservable());
        }

        public static IObservable<long> TimerOrNow(TimeSpan interval)
        {
            return interval <= TimeSpan.Zero
                ? Observable.Return(0L)
                : Observable.Timer(interval);
        }

        public static IObservable<T> OnFirst<T>(this IObservable<T> source, Action<T> onFirst)
        {
            return Observable.Create<T>(observer =>
                {
                    var first = true;
                    return source.Subscribe(item =>
                    {
                        if (first)
                        {
                            onFirst(item);
                            first = false;
                        }
                        else
                            observer.OnNext(item);
                    },
                    observer.OnError,
                    observer.OnCompleted);
                });
        }

        public static IObservable<TSource> RetryConsecutive<TSource>(this IObservable<TSource> source, int retryCount)
        {
            return Observable
                .Create<TSource>(observer =>
                    {
                        var d = new SerialDisposable();
                        var count = 0;
                        Scheduler.Immediate
                            .Schedule(self =>
                                {
                                    d.Disposable = source.Subscribe(i =>
                                        {
                                            count = 0;
                                            observer.OnNext(i);
                                        },
                                        e =>
                                        {
                                            if (++count <= retryCount)
                                            {
                                                // retry
                                                if (!d.IsDisposed)
                                                    self();
                                            }
                                            else
                                                observer.OnError(e);
                                        },
                                        observer.OnCompleted);
                                });

                        return d;
                    });
        }


        public static IObservable<TResult> Create<TResult>(Func<IObserver<TResult>, Task<Action>> asyncMethod)
        {
            return Create<TResult>(observer => asyncMethod(observer).ContinueWith(t => Disposable.Create(t.Result)));
        }

        public static IObservable<TResult> Create<TResult>(Func<IObserver<TResult>, Task<IDisposable>> asyncMethod)
        {
            if (asyncMethod == null)
                throw new ArgumentNullException("asyncMethod");
            return Observable.Create<TResult>(observer =>
            {
                var disposable = new SingleAssignmentDisposable();
                asyncMethod(observer).ContinueWith(t => disposable.Disposable = t.Result);
                return disposable;
            });
        }

        public static IObservable<TValue> Defer<TValue>(Func<Task<TValue>> asyncObservableFactory)
        {
            return Observable.Defer(() => asyncObservableFactory().ToObservable());
        }

        public static IObservable<T> PostSubscribe<T>(this IObservable<T> source, Action postSubscribe)
        {
            return Observable
                .Create<T>(observer =>
                {
                    var d = source.Subscribe(observer);
                    postSubscribe();
                    return d;
                });
        }

        public static IObservable<TSource> OnCompletion<TSource>(this IObservable<TSource> source, Action<Exception> onCompleted)
        {
            return Observable
                .Create<TSource>(observer =>
                    source.Subscribe(observer.OnNext,
                        error =>
                        {
                            onCompleted(error);
                            observer.OnError(error);
                        },
                        () =>
                        {
                            onCompleted(null);
                            observer.OnCompleted();
                        }));
        }

        public static IObservable<TSource> OnCompletion<TSource>(this IObservable<TSource> source, TaskCompletionSource<Unit> tcs)
        {
            return Observable
                .Create<TSource>(observer =>
                    source.Subscribe(observer.OnNext,
                        error =>
                        {
                            tcs.SetException(error);
                            observer.OnError(error);
                        },
                        () =>
                        {
                            tcs.SetResult(Unit.Default);
                            observer.OnCompleted();
                        }));
        }

        public static IObservable<TSource> UseCompletion<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IObservable<TResult>> selector)
        {
            return Observable
                .Create<TSource>(observer =>
                    source
                        .Do(observer.OnNext)
                        .Select(selector)
                        .Switch()
                        .Subscribe(_ => { }, observer.OnError, observer.OnCompleted));
        }

        public static IObservable<TSource> Using<TSource>(this IObservable<TSource> source, Func<TSource, IDisposable> create)
        {
            return Observable
                .Defer(() =>
                {
                    var d = new SerialDisposable();
                    return source
                        .Do(i => d.Disposable = create(i))
                        .Finally(d.Dispose);
                });
        }

        public static IObservable<TResult> Using<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IDisposable> create, Func<TSource, IObservable<TResult>> transform)
        {
            return source.SelectMany(ts =>
            {
                var d = create(ts);
                return transform(ts)
                    .Finally(d.Dispose);
            });
        }

        public static IObservable<T> Run<T>(this Func<T> start)
        {
            return Observable.Defer(() => Observable.Return(start()));
        }

        public static IObservable<Unit> Run(this Action start)
        {
            return Observable.Defer(() => Observable.Return(Unit.Default));
        }

        public static IObservable<HttpSession.Response> ThrowHttpErrors(this IObservable<HttpSession.Response> source)
        {
            return source.ThrowHttpErrors(code => (int)code > 500);
        }

        public static IObservable<HttpSession.Response> ThrowHttpErrors(this IObservable<HttpSession.Response> source, Func<HttpStatusCode, bool> filter)
        {
            return source
                .Do(response =>
                {
                    if (filter(response.Code))
                        throw new HttpException((int)response.Code, "HTTP error");
                });
        }

        public static IObservable<TSource> RetryWhen<TSource>(this IObservable<TSource> source, Func<Exception, bool> retryFilter)
        {
            return source.RetryWhen<TSource, Exception>(retryFilter);
        }

        public static IObservable<TSource> RetryWhen<TSource, TException>(this IObservable<TSource> source, Func<TException, bool> filter)
            where TException : Exception
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (filter == null)
                throw new ArgumentNullException("retryFilter");
            return Observable.Create<TSource>(observer =>
            {
                var subscription = new SerialDisposable();
                var schedule =
                    Scheduler.Immediate.Schedule(
                        retry =>
                        {
                            var d = new SingleAssignmentDisposable();
                            subscription.Disposable = d;
                            d.Disposable = source
                                .Subscribe(observer.OnNext,
                                    exception =>
                                    {
                                        var e = exception as TException;
                                        if (e != null && filter(e))
                                            // retry
                                            retry();
                                        else
                                            // pass on error
                                            observer.OnError(exception);
                                    },
                                    observer.OnCompleted);
                        });
                return new CompositeDisposable(new[]
                {
                    subscription,
                    schedule,
                });
            });
        }

        public static IObservable<TSource> RestartWhen<TSource>(this IObservable<TSource> source, Func<TSource, bool> filter)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (filter == null)
                throw new ArgumentNullException("filter");
            return Observable.Create<TSource>(observer =>
            {
                var subscription = new SerialDisposable();
                var schedule =
                    Scheduler.Immediate.Schedule(
                        retry =>
                        {
                            var d = new SingleAssignmentDisposable();
                            subscription.Disposable = d;
                            d.Disposable = source
                                .Subscribe(item =>
                                    {
                                        if (filter(item))
                                            // retry
                                            retry();
                                        else
                                            // pass on error
                                            observer.OnNext(item);
                                    },
                                    observer.OnError,
                                    observer.OnCompleted);
                        });
                return new CompositeDisposable(new[]
                {
                    subscription,
                    schedule,
                });
            });
        }

        public static IObservable<T> RetrySame<T>(this IObservable<T> source, int count)
        {
            return source.RetrySame<T, Exception>(count);
        }

        public static IObservable<T> RetrySame<T, TException>(this IObservable<T> source, int count)
            where TException : Exception
        {
            return Observable
                .Defer(() =>
                {
                    var retry = 0;
                    var lastException = default(TException);

                    return source
                        .RetryWhen<T, TException>(e =>
                        {
                            if (lastException != null &&
                                e.GetType() == lastException.GetType() &&
                                e.ToString() == lastException.ToString())
                            {
                                // stop retrying?
                                if (++retry == count)
                                    return false;
                            }
                            else
                            {
                                // first occurance of this error
                                lastException = e;
                                retry = 0;
                            }
                            return true;
                        });
                });
        }
    }
}

using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Vertigo
{
    public static class OptionalSubject
    {
        public static ISubject<T, T> Create<T>()
            where T : class
        {
            var subject = new BehaviorSubject<T>(null);
            return Subject.Create(subject,
                subject
                    .Where(v => v != null)
                    .FirstAsync());
        }

        public static ISubject<T?, T?> CreateValue<T>()
            where T : struct
        {
            var subject = new BehaviorSubject<T?>(null);
            return Subject.Create(subject,
                subject
                    .Where(v => v != null)
                    .FirstAsync());
        }
    }

    //public class OptionalValueSubject<T> : ISubject<T?, T>
    //    where T : struct
    //{
    //    private readonly List<IObserver<T>> _observers = new List<IObserver<T>>();
    //    private readonly object _gate = new object();
    //    private T? _value;

    //    public void OnNext(T? value)
    //    {
    //        lock (_gate)
    //        {
    //            if (value == null)
    //            {
    //                _observers.ForEach(observer => observer.OnCompleted());
    //                _observers.Clear();
    //            }
    //            else
    //            {
    //                _observers.ForEach(observer => observer.OnNext(value.Value));
    //            }
    //            _value = value;
    //        }
    //    }

    //    public void OnError(Exception error)
    //    {
    //        OnNext(null);
    //    }

    //    public void OnCompleted()
    //    {
    //        OnNext(null);
    //    }

    //    public IDisposable Subscribe(IObserver<T> observer)
    //    {
    //        lock (_gate)
    //        {
    //            _observers.Add(observer);
    //            if (_value.HasValue)
    //                observer.OnNext(_value.Value);

    //            return Disposable.Create(() =>
    //            {
    //                lock (_gate)
    //                {
    //                    _observers.Remove(observer);
    //                }
    //            });
    //        }
    //    }
    //}
}
using System;

namespace Vertigo.Live
{
    public class LockAndNotify : IDisposable
    {
        private readonly object _target;
        private readonly IDisposable _lock;

        public LockAndNotify(object target, bool block)
        {
            _target = target;
            _lock = target.Lock(block);
        }

        public Action OnDispose { get; set; }

        public void Dispose()
        {
            OnDispose();
        }
    }

    public static partial class Extensions
    {
    }
}
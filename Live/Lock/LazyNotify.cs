using System;
using System.Threading;

namespace Vertigo.Live
{
    public class LazyNotify
    {
        private int _notifyPending;
        public Action OnNotify;

        public void Notify(bool notifyNow)
        {
            if (notifyNow)
            {
                _notifyPending = 0;
                OnNotify();
            }
            else
                _notifyPending = 1;
        }

        public void DoIfNotify()
        {
            if (Interlocked.CompareExchange(ref _notifyPending, 0, 1) == 1)
                OnNotify();
        }
    }
}
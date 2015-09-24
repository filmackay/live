using System;
using System.Threading;

namespace Vertigo.Live
{
    public class LiveCounter : LiveValuePublisher<int>
    {
        private int _counter;

        public LiveCounter()
        {
            Connect(_counter = 0);
        }

        public void Reset()
        {
            using (Publish.Transaction())
                Connect(_counter = 0);
        }

        public void Increment()
        {
            Add(1);
        }

        public void Decrement()
        {
            Add(-1);
        }

        public void Add(int value)
        {
            SetValue(Interlocked.Add(ref _counter, value), 0);
        }
    }
}

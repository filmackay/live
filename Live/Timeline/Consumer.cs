using System;
using System.Diagnostics;

namespace Vertigo.Live
{
    public abstract class Consumer
    {
        public static DispatcherConsumer Dispatcher;

        protected readonly NotifyList<Action> _runOnRefresh = new NotifyList<Action>();

        public void Refresh()
        {
            Publish.OnConsume(() => _runOnRefresh.ProcessGet(getActions => getActions().ForEach()), true);
            //Debug.Print("End Refresh {0}", HiResTimer.ToMicroseconds(HiResTimer.Now - start));
        }

        public virtual void RunOnRefresh(Action action)
        {
            _runOnRefresh.Add(action);
        }
    }
}

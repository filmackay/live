using System;
using System.Diagnostics;
using System.Windows;

namespace Vertigo.Live
{
    public abstract class LiveDependencyObject : DependencyObject
    {
        public void RunOnDispatcher(Action action)
        {
            Debug.Assert(Consumer.Dispatcher != null);
            if (Consumer.Dispatcher == null)
            {
                // no timeline - send directly to dispatcher
                this.OnDispatcherBeginInvoke(action);
            }
            else
            {
                // queue for dispatcher batch run
                Consumer.Dispatcher.RunOnDispatcher(action);
            }
        }

        public void RunOnRefresh(Action action)
        {
            Debug.Assert(Consumer.Dispatcher != null);
            if (Consumer.Dispatcher == null)
            {
                // no timeline - send directly to dispatcher, dont block as we may be holding a lock that action will need
                this.OnDispatcherBeginInvoke(action);
            }
            else
                Consumer.Dispatcher.RunOnRefresh(action);
        }
    }
}
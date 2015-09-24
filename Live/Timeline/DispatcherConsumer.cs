using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;

using Vertigo;

namespace Vertigo.Live
{
    public class DispatcherConsumer : Consumer
    {
        public DispatcherConsumer(Dispatcher dispatcher, TimeSpan minTimeSpan)
        {
            _dispatcher = dispatcher;
            _minTimeSpan = HiResTimer.FromTimeSpan(minTimeSpan);

            _timer = new Timer(state =>
                {
                    _nextQuantumTime = HiResTimer.Now() + _minTimeSpan;

                    // prepare batch (generate lots of RunOnDispatcher notifications)
                    _runOnDispatcher = new List<Action>(); // TODO: race condition where this is null at X, need to prevent timers colliding?
                    Refresh();

                    // obtain dispatcher actions
                    List<Action> actions;
                    using (this.Lock())
                    {
                        actions = _runOnDispatcher;
                        _runOnDispatcher = null;
                    }

                    // execute batch on dispatcher
                    if (actions != null && actions.Count > 0) // X
                    {
                        if (_dispatcher.CheckAccess())
                            actions.ForEach();
                        else
                            _dispatcher.Invoke(new Action(() => actions.ForEach()), DispatcherPriority.DataBind);
                    }
                });

            _runOnRefresh.OnNotify =
                () =>
                {
                    // see if this is too short a time since last quantum?
                    var wait = Math.Max(0, _nextQuantumTime - HiResTimer.Now());
                    _timer.Change(HiResTimer.ToTimeSpan(wait), TimeSpan.FromMilliseconds(-1));
                };
        }

        public void RunOnDispatcher(Action action)
        {
            if (_dispatcher.CheckAccess())
            {
                action();
                return;
            }

            using (this.Lock())
            {
                if (_runOnDispatcher != null)
                    _runOnDispatcher.Add(action);
            }
        }

        private readonly Dispatcher _dispatcher;
        private readonly Timer _timer;
        private long _nextQuantumTime;
        private readonly long _minTimeSpan;
        private List<Action> _runOnDispatcher;
    }
}

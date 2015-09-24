using System;
using System.Collections.Generic;
using System.Threading;

namespace Vertigo.Live
{
    public class ThrottledTimeline : Timeline
    {
        public ThrottledTimeline()
        {
        }

        public ThrottledTimeline(TimeSpan minTimeSpan, IEnumerable<Timeline> publishTimelines) : base(publishTimelines)
        {
            _minTimeSpan = minTimeSpan;
            _timer = new Timer(OnTimer);
        }

        private void OnTimer(object state)
        {
            lock (this)
            {
                _quantumQueued = false;
                base.Quantum();
            }
        }

        protected override sealed void Quantum()
        {
            lock (this)
            {
                if (_quantumQueued)
                    return;

                // see if this is too short a time since last quantum?
                var now = DateTime.Now;
                if (now >= _nextQuantumTime)
                {
                    // quantum now
                    _nextQuantumTime = now + _minTimeSpan;
                    base.Quantum();
                }
                else
                {
                    // schedule quantum for later
                    _quantumQueued = true;
                    var wait = _nextQuantumTime - now;
                    _timer.Change(wait, TimeSpan.FromMilliseconds(-1));
                }
            }
        }

        private readonly Timer _timer;
        private DateTime _nextQuantumTime;
        private bool _quantumQueued;
        private readonly TimeSpan _minTimeSpan;
    }
}
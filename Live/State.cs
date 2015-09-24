using System;
using System.Diagnostics;


namespace Vertigo.Live
{
    // states in order of precedence
    public enum StateStatus
    {
        Connected,
        Reconnecting,       // was connected, but is being reset from the start
        Connecting,         // was disconnected, and is now being connected
        Disconnected,       // is temporarily disconnected, can be re-connected (default state)
        Disconnecting,      // is being temporarily disconnected, can be re-connected (note: not yet implemented)
        Completed,          // already completed, cannot be re-connected
        Completing,         // is being completed, cannot be re-connected (currently assumed it was previously connected)
        Invalid,
    }

    public interface IState
    {
        StateStatus Status { get; }
        long LastUpdated { get; }
        bool HasChange { get; }
    }
    
    public abstract class State<TIState, TState> : IState
        where TIState : IState
        where TState : State<TIState, TState>, TIState, new()
    {
        public StateStatus Status = StateStatus.Disconnected;
        StateStatus IState.Status
        { get { return Status; } }

        public long LastUpdated;
        long IState.LastUpdated
        { get { return LastUpdated; } }

        public abstract StateStatus Add(StateStatus left, StateStatus right);
        public abstract bool HasChange { get; }

        public bool AddInline(StateStatus newStatus)
        {
            // update status
            newStatus = Add(Status, newStatus);
            if (newStatus != Status.Next())
            {
                // status was changed
                Status = newStatus;
                return true;
            }

            return false;
        }

        public virtual bool AddInline(TIState @new)
        {
            LastUpdated = Math.Max(LastUpdated, @new.LastUpdated);
            return AddInline(@new.Status);
        }

        public virtual TState Copy(bool detachStateLock)
        {
            return new TState
            {
                Status = Status,
                LastUpdated = LastUpdated,
            };
        }
        
        public virtual void NextInline()
        {
            Status = Status.Next();
        }

        public TState Extract(bool detachStateLock)
        {
            var ret = Copy(detachStateLock);
            NextInline();
            return ret;
        }
    }

    public static partial class Extensions
    {
        public static bool HasEffect(this IState state)
        {
            return state.Status.IsPending() || state.HasChange;
        }

        public static bool IsPending(this StateStatus a)
        {
            return
                a == StateStatus.Connecting ||
                a == StateStatus.Reconnecting ||
                a == StateStatus.Disconnecting ||
                a == StateStatus.Completing;
        }

        public static bool IsCompleted(this StateStatus a)
        {
            return
                a == StateStatus.Completed ||
                a == StateStatus.Completing;
        }

        public static bool IsConnected(this StateStatus a)
        {
            return
                a == StateStatus.Connected ||
                a == StateStatus.Reconnecting ||
                a == StateStatus.Connecting;
        }

        public static bool IsConnecting(this StateStatus a)
        {
            return
                a == StateStatus.Connecting ||
                a == StateStatus.Reconnecting;
        }

        public static bool IsDisconnecting(this StateStatus a)
        {
            return
                a == StateStatus.Disconnecting ||
                a == StateStatus.Reconnecting ||
                a == StateStatus.Completing;
        }

        public static bool WasConnected(this StateStatus a)
        {
            return
                a == StateStatus.Connected ||
                a == StateStatus.Reconnecting ||
                a == StateStatus.Disconnecting ||
                a == StateStatus.Completing;
        }

        private static readonly bool[] StateStatusInnerRelevant = new[]
            {
            //  Connected               Reconnecting	        Connecting	            Disconnected                Disconnecting               Completed               Completing                         
            //  -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                true,                   true,                   true,                   false,                      true,                       false,                  true,
            };

        public static bool IsInnerRelevant(this StateStatus a)
        {
            return StateStatusInnerRelevant[(int)a];
        }

        private static readonly bool[] StateStatusDeltaRelevant = new[]
            {
            //  Connected               Reconnecting	        Connecting	            Disconnected                Disconnecting               Completed               Completing                         
            //  -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                true,                   false,                  false,                  false,                      true,                       false,                  true,
            };

        public static bool IsDeltaRelevant(this StateStatus a)
        {
            return StateStatusDeltaRelevant[(int)a];
        }

        private static readonly StateStatus[] StartTypeNext = new[]
            {
            //  Connected               Reconnecting	        Connecting	            Disconnected                Disconnecting               Completed               Completing                         
            //  -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                StateStatus.Connected,  StateStatus.Connected,  StateStatus.Connected,  StateStatus.Disconnected,   StateStatus.Disconnected,   StateStatus.Completed,  StateStatus.Completed,
            };

        public static StateStatus Next(this StateStatus a)
        {
            var ret = StartTypeNext[(int)a];
            Debug.Assert(ret != StateStatus.Invalid);
            return ret;
        }
        public static StateStatus And(this StateStatus a, StateStatus b)
        {
            // get state with highest precendence
            var ret = (StateStatus)Math.Max((int) a, (int) b);
            Debug.Assert(ret != StateStatus.Invalid);
            return ret;
        }

        private static readonly StateStatus[,] StartTypeAdd = new[,]
            {
            //    (SECOND)
            //    Connected                 Reconnecting                Connecting	                Disconnected                  Disconnecting               Completed                  Completing                     
            //    ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                { StateStatus.Connected,    StateStatus.Reconnecting,   StateStatus.Reconnecting,   StateStatus.Disconnecting,    StateStatus.Disconnecting,  StateStatus.Completing,    StateStatus.Completing,     }, // Connected (FIRST)
                { StateStatus.Connected,    StateStatus.Reconnecting,   StateStatus.Reconnecting,   StateStatus.Disconnecting,    StateStatus.Disconnecting,  StateStatus.Completing,    StateStatus.Completing,     }, // Reconnecting
                { StateStatus.Connected,    StateStatus.Reconnecting,   StateStatus.Connecting,     StateStatus.Disconnected,     StateStatus.Disconnected,   StateStatus.Completing,    StateStatus.Completing,     }, // Connecting
                { StateStatus.Connecting,   StateStatus.Connecting,     StateStatus.Connecting,     StateStatus.Disconnected,     StateStatus.Disconnected,   StateStatus.Invalid,       StateStatus.Completing,     }, // Disconnected
                { StateStatus.Reconnecting, StateStatus.Reconnecting,   StateStatus.Reconnecting,   StateStatus.Disconnected,     StateStatus.Disconnected,   StateStatus.Completing,    StateStatus.Completing,     }, // Disconnecting
                { StateStatus.Completed,    StateStatus.Completed,      StateStatus.Completed,      StateStatus.Completed,        StateStatus.Completed,      StateStatus.Completed,     StateStatus.Completed,      }, // Completed
                { StateStatus.Completing,   StateStatus.Completing,     StateStatus.Completing,     StateStatus.Completing,       StateStatus.Completing,     StateStatus.Completing,    StateStatus.Completing,     }, // Completing
            };

        public static StateStatus Add(this StateStatus first, StateStatus second)
        {
            var ret = StartTypeAdd[(int)first, (int)second];
            Debug.Assert(ret != StateStatus.Invalid);
            return ret;
        }

        public static StateStatus AddSimple(this StateStatus first, StateStatus second)
        {
            // special handling for simple state types (eg. ILiveValue)

            // translate restart into update - since the state of these are simple we can ignore intermediate disconnections/reconnections and preserve the illusion of continuation where possible
            if (first.IsConnected() && second.IsConnecting())
                return StateStatus.Connected;
            return Add(first, second);
        }

        public static TimeSpan Latency(this IState state)
        {
            return state.LastUpdated == 0
                ? TimeSpan.Zero
                : HiResTimer.ToTimeSpan(HiResTimer.Now() - state.LastUpdated);
        }

        public static StateStatus GetStatus(this IState state)
        {
            return state == null ? StateStatus.Disconnected : state.Status;
        }
    }
}

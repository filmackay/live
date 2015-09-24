using System;
using System.Threading;

namespace Vertigo.Live
{
    public static class Callback
    {
        public static T Split<T>(Action<T, bool> call, Func<Action<T>, T> main)
        {
            var inside = true;
            var ret = main(parm => call(parm, inside));
            inside = false;
            return ret;
        }

        public static void Split(Action callInside, Action callOutside, Action<Action> main)
        {
            var inside = true;
            main(() => (inside ? callInside : callOutside)());
            inside = false;
        }

        public static void Split<T>(Action<T> callInside, Action<T> callOutside, Action<Action<T>> main)
        {
            var inside = true;
            main(t => (inside ? callInside : callOutside)(t));
            inside = false;
        }

        public static T Split<T>(Action<T> callInside, Action<T> callOutside, Func<Action<T>, T> main)
        {
            return Split<T, T>(callInside, callOutside, main);
        }

        public static TResult Split<T, TResult>(Action<T> callInside, Action<T> callOutside, Func<Action<T>, TResult> main)
        {
            var inside = true;
            var ret = main(parm => (inside ? callInside : callOutside)(parm));
            inside = false;
            return ret;
        }

        public static T Split<T>(Action callInside, Action<T> callOutside, Func<Action, T> main)
        {
            var inside = true;
            var ret = default(T);
            ret = main(() =>
            {
                if (inside)
                    callInside();
                else
                    callOutside(ret);
            });
            inside = false;
            return ret;
        }

        public static T SuppressInside<T>(Action<T> callOut, Func<Action<T>, T> main)
        {
            var inside = true;
            var ret = main(parm =>
            {
                if (!inside)
                    callOut(parm);
            });
            inside = false;
            return ret;
        }

        public static T SuppressInside<T>(Action callOut, Func<Action, T> main)
        {
            var inside = true;
            var ret = main(() =>
            {
                if (!inside)
                    callOut();
            });
            inside = false;
            return ret;
        }

        public static void SuppressInside(Action callOut, Action<Action> main)
        {
            var inside = true;
            main(() =>
            {
                if (!inside)
                    callOut();
            });
            inside = false;
        }

        public static T PostponeInside<T>(Action callOut, Func<Action, T> main)
        {
            var calledInside = false;
            var inside = true;
            var ret = main(() =>
            {
                if (inside)
                    calledInside = true;
                else
                    callOut();
            });
            inside = false;
            Thread.MemoryBarrier();
            if (calledInside)
                callOut();
            return ret;
        }

        public static void PostponeInside(Action callOut, Action<Action> main)
        {
            var calledInside = false;
            var inside = true;
            main(() =>
            {
                if (inside)
                    calledInside = true;
                else
                    callOut();
            });
            inside = false;
            Thread.MemoryBarrier();
            if (calledInside)
                callOut();
        }
    }
}
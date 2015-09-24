using System;


namespace Vertigo.Live
{
    public class Live<T> : LiveValuePublisher<T>
    {
        public Live()
        {
        }

        public Live(T inner)
        {
            Init(inner, HiResTimer.Now());
        }

        public Live(T inner, long lastUpdated)
        {
            Init(inner, lastUpdated);
        }

        static Live()
        {
            Never = new Live<T>();
        }
        public static readonly ILiveValue<T> Never;

        public static Live<T> NewDefault()
        {
            return new Live<T>(default(T));
        }

        public virtual T PublishValue
        {
            get { return GetValue(); }
            set { SetValue(value); }
        }

        public new void Init(T value, long lastUpdated)
        {
            base.Init(value, lastUpdated);
        }

        public void InitUntyped(object value, long lastUpdated)
        {
            base.Init(ConvertValue(value), lastUpdated);
        }

        public static T ConvertValue(object value)
        {
            // handle nullables
            var t = typeof (T);
            if (t.IsGenericType)
            {
                t = t.GetGenericBase(typeof(Nullable<>)).GetGenericArguments()[0];
                if (value is DBNull || value == null)
                    return (T)(object)null;
                if (t.IsEnum)
                    return (T)Enum.Parse(t, value.ToString());
            }
            else
            {
                // handle type conversions
                if (t.IsEnum)
                    return (T)Enum.Parse(t, value.ToString());
                if (value is DBNull || value == null)
                    return (T)(t.IsValueType ? Activator.CreateInstance(t) : null);
            }
            return (T)value;
        }

        public new void Connect(T value)
        {
            base.Connect(value);
        }

        public new void SetValue(T value, long timestamp)
        {
            base.SetValue(value, timestamp);
        }

        public new void Disconnect()
        {
            base.Disconnect();
        }

        public new void Complete()
        {
            base.Complete();
        }
    }

    public static partial class Extensions
    {
        public static Live<T> ToLive<T>(this T initialValue)
        {
            return new Live<T>(initialValue);
        }

        public static Live<T> ToLive<T>(this T initialValue, long lastUpdated)
        {
            return new Live<T>(initialValue, lastUpdated);
        }

        public static ILiveValue<T> ToReadOnly<T>(this ILiveValue<T> live)
        {
            return live;
        }
    }
}


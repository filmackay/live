using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Vertigo
{
    public enum LogType
    {
        Debug,
        Info,
        Error,
    }
    
    public interface ILog
    {
        void Entry(LogType logType, string text);
        void Entry(LogType logType, string format, params object[] args);
        void Debug(string text);
        void Info(string text);
        void Error(string text);
        void Debug(string format, params object[] args);
        void Info(string format, params object[] args);
        void Error(string format, params object[] args);
        void Flush();
    }

    public static class LogManager
    {
        private static readonly LockFreeQueue<LogEntry> _queue = new LockFreeQueue<LogEntry>();
        private static DateTime _fileDate;
        private static StreamWriter _file;
        public static string Path;
        private static int _id;
        public static LogType MinLogType = LogType.Info;

        static LogManager()
        {
            Path = ConfigurationManager.AppSettings["LogPath"];
            var thread = new Thread(WriteThread) {Priority = ThreadPriority.Lowest, IsBackground = true};
            thread.Start();
        }

        private static void WriteThread()
        {
            while (!Environment.HasShutdownStarted)
            {
                // get queued log entries
                var entries = _queue.GetAll();
                if (entries.Count == 0)
                {
                    Thread.Sleep(100);
                    continue;
                }

                // sort entries as they may have been queued out of order
                entries.Sort((a, b) =>
                    {
                        var comp = a.DateTime.CompareTo(b.DateTime);
                        if (comp == 0)
                            comp = a.ID.CompareTo(b.ID);
                        return comp;
                    });

                // aggregate entries
                var str = new StringBuilder();
                for (var i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    str.AppendLine(entry.ToString());
                }

                // trace output
                var str2 = str.ToString();
#if !DEBUG
                Trace.Write(str2);
#endif

                // any log file to write?
                if (Path != null)
                {
                    // prepare file
                    if (_fileDate != entries[0].DateTime.Date)
                    {
                        if (_file != null)
                            _file.Close();
                        _fileDate = entries[0].DateTime.Date;
                        _file = new StreamWriter(File.Open(System.IO.Path.Combine(Path, _fileDate.ToString("yyyy-MM-dd") + ".LOG"), FileMode.Append, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
                    }

                    _file.Write(str2);
                }
            }
        }

        public static void Flush()
        {
            while (!_queue.IsEmpty)
                Thread.Sleep(1);
        }

        public static void Entry(LogType logType, string source, string text)
        {
            if (logType < MinLogType)
                return;

            var now = DateTime.Now;
            var id = Interlocked.Increment(ref _id);
            var entry = new LogEntry
                {
                    DateTime = now,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Text = text,
                    Source = source,
                    ID = id,
                };
            _queue.Enqueue(entry);
            Debug.WriteLine(entry.ToString());
        }

        public struct LogEntry
        {
            public int ID;
            public DateTime DateTime;
            public int Thread;
            public string Text;
            public string Source;
            public LogType LogType;

            public override string ToString()
            {
                return string.Format("{0:HH:mm:ss.fff} #{1} {2} {3}", DateTime, Thread, Source, Text);
            }
        }

        public static ILog GetLogger(Type type)
        {
            return new Log(type.Name);
        }

        public static ILog GetLogger(string source)
        {
            return new Log(source);
        }

        public static ILog GetLogger<T>(T obj)
        {
            return GetLogger(typeof(T));
        }

        class Log : ILog
        {
            private readonly string _source;

            public Log(string source)
            {
                _source = source;
            }

            public void Entry(LogType logType, string text)
            {
                LogManager.Entry(logType, _source, text);
            }

            public void Entry(LogType logType, string format, params object[] args)
            {
                Entry(logType, string.Format(format, args));
            }

            public void Debug(string text)
            {
                Entry(LogType.Debug, text);
            }

            public void Info(string text)
            {
                Entry(LogType.Info, text);
            }

            public void Error(string text)
            {
                Entry(LogType.Error, text);
            }

            public void Debug(string format, params object[] args)
            {
                Entry(LogType.Debug, format, args);
            }

            public void Info(string format, params object[] args)
            {
                Entry(LogType.Info, format, args);
            }

            public void Error(string format, params object[] args)
            {
                Entry(LogType.Error, format, args);
            }

            public void Flush()
            {
                LogManager.Flush();
            }
        }
    }
}

using System;
using System.IO;

namespace Vertigo.Live
{
    public class TraceFile : IDisposable
    {
        private readonly StreamWriter _file;

        public TraceFile(string path)
        {
            _file = File.CreateText(path);
        }

        public IDisposable Add(IObservable<string> source)
        {
            return source
                .SynchronizeEx(this)
                .Subscribe(line => _file.WriteLine(line));
        }

        public void Dispose()
        {
            _file.Close();
            _file.Dispose();
        }
    }
}
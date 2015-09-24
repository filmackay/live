using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Vertigo
{
    public class StreamAsync : IDisposable
    {
        private readonly Stream _stream;
        public readonly byte[] ReadBuffer = new byte[10240];
        public int ReadOffset;
        public int ReadLength;
        private int _unreadFrom;
        private readonly TaskCompletionSource<Unit> _onClose = new TaskCompletionSource<Unit>();
        public readonly Task OnClose;

        public StreamAsync(Stream stream)
        {
            _stream = stream;
            OnClose = _onClose.Task;
        }

        public Task Write(byte[] buffer, int offset, int length)
        {
            return _stream.WriteAsync(buffer, offset, length);
        }

        public Task Write(byte[] buffer)
        {
            if (OnClose.IsCompleted)
            {
            }
            return Write(buffer, 0, buffer.Length);
        }

        public async Task Read()
        {
            // check for unread data
            if (_unreadFrom > 0)
            {
                ReadOffset = _unreadFrom;
                _unreadFrom = 0;
            }
            else
            {
                ReadOffset = 0;
                ReadLength = await _stream.ReadAsync(ReadBuffer, 0, ReadBuffer.Length);
                if (ReadLength == 0)
                    Dispose();
            }
        }

        public void UnreadFrom(int bytesRead)
        {
            _unreadFrom = bytesRead;
        }

        public async Task<byte[]> ReadUntil(byte[] delimeter)
        {
            var ret = new MemoryStream();
            var delimeterCount = 0;

            while (true)
            {
                await Read();
                if (ReadLength == 0)
                    // socket graceful close
                    throw new SocketException(-1);

                // look for delimeter
                for (var i = ReadOffset; i < ReadLength; i++)
                {
                    var b = ReadBuffer[i];
                    if (b == delimeter[delimeterCount])
                    {
                        // delimeter match
                        if (++delimeterCount == delimeter.Length)
                        {
                            // unread remainder
                            if (i + 1 < ReadLength)
                                UnreadFrom(i + 1);
                            return ret.ToArray();
                        }
                    }
                    else
                    {
                        if (delimeterCount > 0)
                        {
                            // partial delimeter
                            ret.Write(delimeter, 0, delimeterCount);
                            delimeterCount = 0;
                        }

                        // non-delimeter
                        ret.WriteByte(b);
                    }
                }
            }
        }

        public async Task<byte[]> ReadCount(int count)
        {
            var ret = new byte[count];
            var writeRet = 0;

            while (writeRet < count)
            {
                await Read();
                if (ReadLength == 0)
                    // socket graceful close
                    throw new SocketException(-1);

                // read data
                var readLen = Math.Min(ReadLength - ReadOffset, count - writeRet);
                Buffer.BlockCopy(ReadBuffer, ReadOffset, ret, writeRet, readLen);
                writeRet += readLen;

                // unread
                if ((ReadOffset + readLen) < ReadLength)
                    UnreadFrom(ReadOffset + readLen);
            }

            return ret;
        }

        public IObservable<byte[]> ReadStreamingCount(int count)
        {
            return Observable.Create<byte[]>(async (observer, cancellationToken) =>
            {
                var totalRead = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (totalRead == count)
                    {
                        // stream finished
                        observer.OnCompleted();
                        break;
                    }

                    try
                    {
                        await Read();
                    }
                    catch (SocketException socketException)
                    {
                        observer.OnError(socketException);
                        break;
                    }
                    if (ReadLength == 0)
                    {
                        // socket graceful close
                        observer.OnError(new SocketException(-1));
                        break;
                    }

                    // read data
                    var readLen = Math.Min(ReadLength - ReadOffset, count - totalRead);
                    observer.OnNext(ReadBuffer.Extract(ReadOffset, readLen));
                    totalRead += readLen;

                    // unread
                    if ((ReadOffset + readLen) < ReadLength)
                        UnreadFrom(ReadOffset + readLen);
                }
            });
        }

        //public IObservable<byte[]> ReadUntil(byte[] delimeter)
        //{
        //    return Observable.CreateAsync<byte[]>(async (observer, cancellationToken) =>
        //        {
        //            var delimeterCount = 0;

        //            while (!cancellationToken.IsCancellationRequested)
        //            {
                        //try
                        //{
                        //    await Read();
                        //}
                        //catch (SocketException socketException)
                        //{
                        //    observer.OnError(socketException);
                        //    break;
                        //}
                        //if (ReadBytes == 0)
                        //{
                        //    // socket graceful close
                        //    observer.OnError(new SocketException(-1));
                        //    break;
                        //}

        //                // look for delimeter
        //                for (var i = 0; i < read.Length; i++)
        //                {
        //                    var b = read.Buffer[i];
        //                    if (b == delimeter[delimeterCount])
        //                    {
        //                        // delimeter match
        //                        if (++delimeterCount == delimeter.Length)
        //                        {
        //                            // final piece
        //                            if (i > delimeter.Length)
        //                                observer.OnNext(read.Buffer.Extract(0, i - delimeterCount));

        //                            // unread remainder
        //                            Unread(read, i + 1);

        //                            // complete
        //                            observer.OnCompleted();
        //                            return;
        //                        }
        //                    }
        //                    else if (delimeterCount > 0)
        //                    {
        //                        // incomplete delimeter
        //                        delimeterCount = 0;
        //                    }
        //                }

        //                // pass-through data
        //                observer.OnNext(read.Buffer.Extract(0, read.Length - delimeterCount));
        //            }
        //        });
        //}

        public IObservable<byte[]> ReadStreamingToEnd()
        {
            return Observable.Create<byte[]>(async (observer, cancellationToken) =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await Read();
                    }
                    catch (SocketException socketException)
                    {
                        observer.OnError(socketException);
                        break;
                    }
                    if (ReadLength == 0)
                    {
                        // graceful socket close
                        observer.OnCompleted();
                        break;
                    }
                    observer.OnNext(ReadBuffer.Extract(ReadOffset, ReadLength));
                }
            });
        }

        public void Dispose()
        {
            if (!_onClose.Task.IsCompleted)
            {
                //LogManager.Entry(LogType.Error, "StreamAsync", "Close");
                _stream.Close();
                _onClose.SetResult(Unit.Default);
                GC.SuppressFinalize(this);
            }
        }
    }

    public static partial class Extensions
    {
        public static byte[] Extract(this byte[] source, int offset, int length)
        {
            if (source.Length == length)
                return source;
            var ret = new byte[length];
            Buffer.BlockCopy(source, offset, ret, 0, length);
            return ret;
        }
    }
}
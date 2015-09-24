using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Net.Security;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Vertigo
{
    public static partial class Extensions
    {
        //public static IObservable<HttpSession.PageResponse> GetPage(this IObservable<HttpSession.Response> source)
        //{
        //    return source
        //        .SelectMany(response => response.GetPage().TraceTo(TraceTargets.Debug, "Q"))
        //        .TraceTo(TraceTargets.Debug, "W");
        //}

        public static async Task<HttpSession.PageResponse> GetPage(this Task<HttpSession.Response> response)
        {
            var r = await response;
            return await r.GetPage();
        }
    }

    public static class Http
    {
        public static string FormatHeader(KeyValuePair<string, string> kv)
        {
            return string.Format("{0}: {1}\r\n", kv.Key, kv.Value);
        }

        public static string FormatHeaders(IEnumerable<KeyValuePair<string, string>> headers)
        {
            return string.Join("", headers.Select(FormatHeader));
        }
    }

    public class HttpSession : Logger
    {
        public IPAddress LocalIPAddress;
        public string UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.1 (KHTML, like Gecko) Chrome/14.0.835.202 Safari/535.1";
        public CookieContainer Cookies = new CookieContainer();
        public bool Proxy;
        public string ProxyHost = "127.0.0.1";
        public int ProxyPort = 8888;

        public async Task<Response> SingleRequest(Uri uri, string postData = null, string customHeaders = null)
        {
            // requests a page from a new HTTP socket, closing the socket afterwards
            using (var connection = await Connect(uri))
            {
                return await connection.Request(uri, postData, customHeaders);
            }
        }

        public async Task<Connection> Connect(Uri uri)
        {
            // open socket
            var tcpClient =
                LocalIPAddress == null
                    ? new TcpClient()
                    : new TcpClient(new IPEndPoint(LocalIPAddress, 0));
            tcpClient.NoDelay = true;
            await tcpClient.ConnectAsync(Proxy ? ProxyHost : uri.Host, Proxy ? ProxyPort : uri.Port);
            var stream = (Stream)tcpClient.GetStream();

            // handle SSL
            if (!Proxy && uri.Scheme == "https")
            {
                var ssl = new SslStream(stream, false);
                await ssl.AuthenticateAsClientAsync(uri.Host);
                stream = ssl;
            }

            return new Connection(this, stream);
        }

        private static string GetHeaderValue(string headers, string name, string defaultValue = null)
        {
            var headerTag = name + ": ";
            var index = headers.IndexOf(headerTag);
            if (index < 0)
                return defaultValue;

            var startIndex = index + headerTag.Length;
            var endIndex = headers.IndexOf('\r', startIndex);
            if (endIndex < 0)
                endIndex = headers.Length;
            return headers.Substring(startIndex, endIndex - startIndex);
        }

        private static IEnumerable<KeyValuePair<string, string>> GetHeaders(string headers)
        {
            var lines = headers.Split(new[] { "\r\n" }, StringSplitOptions.None);
            return lines.Select(line =>
            {
                var index = line.IndexOf(": ");
                return KeyValuePair.Create(line.Substring(0, index), line.Substring(index + 2));
            });
        }

        private static IEnumerable<KeyValuePair<string, string>> GetHeaders(string headers, string name)
        {
            var lines = headers.Split(new[] { "\r\n" }, StringSplitOptions.None);
            return lines
                .Where(line => line.StartsWith(name + ": "))
                .Select(line =>
                {
                    var index = line.IndexOf(": ");
                    return KeyValuePair.Create(line.Substring(0, index), line.Substring(index + 2));
                });
        }

        public struct Response
        {
            public Response(Uri uri, HttpStatusCode code, string headers, IObservable<byte[]> content)
            {
                var onCompletion = new TaskCompletionSource<Unit>();
                OnCompletion = onCompletion.Task;
                Uri = uri;
                Headers = headers;
                Content = content.OnCompletion(onCompletion);
                Code = code;
            }

            public readonly string Headers;
            public readonly IObservable<byte[]> Content;
            public readonly HttpStatusCode Code;
            public readonly Uri Uri;
            public readonly Task OnCompletion;

            public string GetHeaderValue(string name)
            {
                return HttpSession.GetHeaderValue(Headers, name);
            }

            public async Task<PageResponse> GetPage()
            {
                return new PageResponse(Uri, Code, Headers, await Content.GetPage());
            }
        }

        public struct PageResponse
        {
            public PageResponse(Uri uri, HttpStatusCode code, string headers, string page)
            {
                Uri = uri;
                Headers = headers;
                Content = page;
                Code = code;
            }

            public readonly string Headers;
            public readonly string Content;
            public readonly HttpStatusCode Code;
            public readonly Uri Uri;

            public string GetHeaderValue(string name)
            {
                return HttpSession.GetHeaderValue(Headers, name);
            }
        }

        public class Connection : IDisposable
        {
            public Connection(HttpSession session, Stream stream)
            {
                _session = session;
                _stream = new StreamAsync(stream);
            }

            public Task Write(byte[] buffer)
            {
                return _stream.Write(buffer);
            }

            public async Task<Response> Request(Uri uri, string postData = null, string customHeaders = null)
            {
                // NOTE: this routine does not retry errors, or close the stream after use

                if (uri.Scheme != "http" && uri.Scheme != "https")
                    throw new InvalidOperationException("Invalid URL");

                // work out cookies to send
                var cookies = _session.Cookies.GetCookieHeader(uri);

                // send request
                var httpRequest =
                    string.Format(
                        postData == null
                        ? "GET {0} HTTP/1.1\r\n" +
                            "User-Agent: {1}\r\n" +
                            "Pragma: no-cache\r\n" +
                            "Host: {2}\r\n" +
                            "{4}" +
                            "{5}" +
                            "\r\n"
                        : "POST {0} HTTP/1.1\r\n" +
                            "User-Agent: {1}\r\n" +
                            "Content-Type: application/x-www-form-urlencoded\r\n" +
                            "Host: {2}\r\n" +
                            "{4}" +
                            "Content-Length: {3}\r\n" +
                            "Pragma: no-cache\r\n" +
                            "{5}" +
                            "\r\n" +
                            "{6}",
                        _session.Proxy ? uri.ToString() : uri.PathAndQuery,
                        _session.UserAgent,
                        uri.Host,
                        postData == null ? 0 : postData.Length,
                        cookies == "" ? "" : "Cookie: " + cookies + "\r\n",
                        customHeaders,
                        postData);
                var requestData = Encoding.ASCII.GetBytes(httpRequest);

                // send request
                await Write(requestData);

                // get response
                var headers = Encoding.ASCII.GetString(await _stream.ReadUntil(new byte[] { 0x0d, 0x0a, 0x0d, 0x0a }));
                int startIndex, endIndex;
                if (headers.Substring(0, 5) != "HTTP/" ||
                    (startIndex = headers.IndexOf(' ', 5)) < 0 ||
                    (endIndex = headers.IndexOf(' ', startIndex + 1)) < 0)
                    throw new InvalidOperationException("Protocol is not HTTP");
                var code = int.Parse(headers.Substring(startIndex + 1, endIndex - (startIndex + 1)));

                // set cookies
                GetHeaders(headers, "Set-Cookie")
                    .ForEach(kv => _session.Cookies.SetCookies(uri, kv.Value));

                // look for content length
                var contentLength = default(int?);
                if (code == (int)HttpStatusCode.NotModified)
                    contentLength = 0;
                var contentLengthString = GetHeaderValue(headers, "Content-Length");
                if (contentLengthString != null)
                    contentLength = int.Parse(contentLengthString);
                var closeSocket = GetHeaderValue(headers, "Connection", "").Contains("close");

                // work out content
                IObservable<byte[]> content;
                if (contentLength == 0)
                {
                    // no content
                    content = Observable.Empty<byte[]>();
                }
                else if (contentLength > 0)
                {
                    // fixed content length
                    content = _stream.ReadStreamingCount(contentLength.Value);
                }
                else if (GetHeaderValue(headers, "Transfer-Encoding", "").Contains("chunked"))
                {
                    // use chunked encoding
                    content = Observable.Create<byte[]>(async (observer, cancellationToken) =>
                        {
                            while (!cancellationToken.IsCancellationRequested)
                            {
                                var delimeter = new byte[] { 0x0d, 0x0a };

                                // read chunk header
                                byte[] read = null;
                                try
                                {
                                    read = await _stream.ReadUntil(delimeter);
                                }
                                catch (SocketException error)
                                {
                                    observer.OnError(error);
                                }

                                // read chunk length & data
                                var chunkLength = int.Parse(Encoding.ASCII.GetString(read), NumberStyles.HexNumber);
                                var chunkData = await _stream.ReadCount(chunkLength);
                                var d = await _stream.ReadCount(2);
                                if (d[0] != delimeter[0] || d[1] != delimeter[1])
                                    throw new ProtocolViolationException("HTTP chunked transfer encoding - no CRLF after chunk");

                                // send
                                if (chunkLength == 0)
                                {
                                    // last chunk
                                    observer.OnCompleted();
                                    break;
                                }
                                observer.OnNext(chunkData);
                            }
                        });
                }
                else
                {
                    // unlimited content (contentLength == -1)
                    content = _stream.ReadStreamingToEnd();
                }

                var response = new Response(uri, (HttpStatusCode)code, headers, content);

                // force socket close?
                if (closeSocket)
                    response
                        .OnCompletion
                        .ContinueWith(t =>
                            {
                                if (t.Status == TaskStatus.RanToCompletion)
                                    _stream.Dispose();
                            }, TaskContinuationOptions.ExecuteSynchronously);

                return response;
            }

            public void Dispose()
            {
                _stream.Dispose();
                GC.SuppressFinalize(this);
            }

            public Task OnClose { get { return _stream.OnClose; } }

            private readonly HttpSession _session;
            private readonly StreamAsync _stream;
        }

        public class Pool : Logger
        {
            private class PersistantConnection : IDisposable
            {
                private readonly Pool _pool;
                private readonly ISubject<Connection, Connection> Connection;
                private int _lock;
                private string _date;
                private readonly IDisposable _reconnecting;
                private readonly Subject<Unit> _requestCompletion = new Subject<Unit>();

                public PersistantConnection(Pool pool)
                {
                    _pool = pool;

                    // current connection
                    {
                        var connection = new BehaviorSubject<Connection>(null);
                        Connection = Subject.Create(connection, connection
                            .Where(v => v != null)
                            .FirstAsync());
                    }

                    // start connecting
                    _reconnecting = Scheduler
                        .CurrentThread
                        .Schedule(async self =>
                            {
                                // create connection
                                var connection = await _pool._session.Connect(_pool._keepAliveUri);
                                connection.OnClose.ContinueWith(t => Connection.OnNext(null), TaskContinuationOptions.ExecuteSynchronously);
                                Connection.OnNext(connection);

                                // keep-alive
                                using (_requestCompletion
                                    .Throttle(_pool.KeepAliveTime)
                                    .Select(l => KeepAlive())
                                    .Subscribe())
                                {
                                    _requestCompletion.OnNext(Unit.Default);

                                    // handle close
                                    await connection.OnClose;
                                }

                                self();
                            });
                }

                public void Dispose()
                {
                    _reconnecting.Dispose();
                    GC.SuppressFinalize(this);
                }

                public async Task<Response?> TryRequest(Uri uri, string postData = null, string customHeaders = null)
                {
                    // try to get lock
                    var locked = Interlocked.CompareExchange(ref _lock, 1, 0) == 0;
                    if (!locked)
                        return null;

                    // return access to connection
                    var connection = await Connection;
                    var response = await connection.Request(uri, postData, customHeaders);
                    response
                        .OnCompletion
                        .ContinueWith(t =>
                            {
                                _lock = 0;
                                _requestCompletion.OnNext(Unit.Default);
                            }, TaskContinuationOptions.ExecuteSynchronously);
                    return response;
                }

                public async Task KeepAlive()
                {
                    // try keep alive
                    var response = await TryRequest(_pool._keepAliveUri, null, _date == null ? null : Http.FormatHeader(KeyValuePair.Create("If-Modified-Since", _date)));
                    if (response == null)
                        // could not get a lock - connection in use, no need to keep-alive
                        return;

                    // return request
                    var page = await response.Value.GetPage();
                    _date = page.GetHeaderValue("Date");
                }
            }

            private readonly HttpSession _session;
            private readonly Uri _keepAliveUri;
            private readonly int _count;
            public readonly TimeSpan KeepAliveTime;
            private readonly PersistantConnection[] _connections;

            public Pool(HttpSession session, Uri keepAliveUri, TimeSpan keepAliveTime, int maxConcurrent)
            {
                _count = maxConcurrent + 1;
                _session = session;
                _keepAliveUri = keepAliveUri;
                KeepAliveTime = keepAliveTime;

                // create connections
                _connections =
                    Enumerable
                        .Range(0, _count)
                        .Select(i => new PersistantConnection(this))
                        .ToArray();
            }

            public Task KeepAlive()
            {
                return Task.WhenAll(_connections.Select(c => c.KeepAlive()).ToArray());
            }

            public async Task<Response> Request(Uri uri, string postData = null, string customHeaders = null)
            {
                if (uri.Port != _keepAliveUri.Port || uri.Host != _keepAliveUri.Host || uri.Scheme != _keepAliveUri.Scheme)
                    throw new InvalidOperationException("Request must use the same scheme, port and host as pool");

                // get free connection from pool
                var i = 0;
                var connectionNotAvailable = false;
                while (true)
                {
                    var response = await _connections[i].TryRequest(uri, postData, customHeaders);
                    if (response != null)
                    {
                        //Log.Entry(LogType.Debug, "Got connection #{0}", i);
                        return response.Value;                                    
                    }

                    // try next, loop
                    if (++i >= _count)
                    {
                        if (!connectionNotAvailable)
                        {
                            Log.Entry(LogType.Error, "No Connection available");
                            connectionNotAvailable = true;
                        }
                        await Task.Delay(1);
                        i = 0;
                    }
                }
            }
        }
    }

    internal class ImmutableList<T>
    {
        // Fields
        private readonly T[] data;

        // Methods
        public ImmutableList()
        {
            this.data = new T[0];
        }

        private ImmutableList(T[] data)
        {
            this.data = data;
        }

        public ImmutableList<T> Add(T value)
        {
            T[] destinationArray = new T[this.data.Length + 1];
            Array.Copy(this.data, destinationArray, this.data.Length);
            destinationArray[this.data.Length] = value;
            return new ImmutableList<T>(destinationArray);
        }

        public bool Contains(T value)
        {
            return (this.IndexOf(value) >= 0);
        }

        public int IndexOf(T value)
        {
            for (int i = 0; i < this.data.Length; i++)
            {
                if (this.data[i].Equals(value))
                {
                    return i;
                }
            }
            return -1;
        }

        public ImmutableList<T> Remove(T value)
        {
            int index = this.IndexOf(value);
            if (index < 0)
                return this;
            T[] destinationArray = new T[this.data.Length - 1];
            Array.Copy(this.data, 0, destinationArray, 0, index);
            Array.Copy(this.data, index + 1, destinationArray, index, (this.data.Length - index) - 1);
            return new ImmutableList<T>(destinationArray);
        }

        // Properties
        public int Count
        {
            get { return this.data.Length; }
        }

        public T[] Data
        {
            get { return this.data; }
        }

        public T this[int index]
        {
            get { return this.data[index]; }
        }
    }
}

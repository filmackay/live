using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Text.RegularExpressions;

namespace Vertigo
{
    public static class AsyncIO
    {
        public static IObservable<byte[]> ToMessages(this IObservable<byte[]> blocks, byte[] eom, string traceFile = null)
        {
            return Observable.Create<byte[]>(observer =>
            {
                FileStream out2 = null;
                if (traceFile != null)
                    out2 = File.Create(traceFile, 0, FileOptions.WriteThrough);

                var buff = new List<byte>();
                return blocks
                    .Subscribe(block =>
                    {
                        var i = Math.Max(eom.Length - 1, buff.Count);

                        // add to buffer
                        buff.AddRange(block);

                        // look for EOM
                        while (i < buff.Count)
                        {
                            var j = eom.Length - 1;
                            for (var jc = 0; jc < eom.Length; j--, jc++)
                            {
                                if (eom[j] != buff[i - jc])
                                    break;
                            }
                            if (j == -1)
                            {
                                // complete message
                                var message = new byte[i - eom.Length + 1];
                                buff.CopyTo(0, message, 0, message.Length);
                                observer.OnNext(message);
                                buff.RemoveRange(0, i + 1);
                                i = eom.Length - 1;

                                if (out2 != null)
                                {
                                    out2.Write(message, 0, message.Length);
                                    out2.Write(eom, 0, eom.Length);
                                }
                            }
                            else
                                i++;
                        }
                    },
                    observer.OnError,
                    observer.OnCompleted);
            });
        }

        public static IObservable<string> ToMessages(this IObservable<string> blocks, string eom)
        {
            return Observable.Create<string>(observer =>
            {
                var buff = new StringBuilder();
                return blocks
                    .Subscribe(block =>
                    {
                        var i = Math.Max(eom.Length - 1, buff.Length);

                        // add to buffer
                        buff.Append(block);

                        // look for EOM
                        while (i < buff.Length)
                        {
                            var j = eom.Length - 1;
                            for (var jc = 0; jc < eom.Length; j--, jc++)
                            {
                                if (eom[j] != buff[i - jc])
                                    break;
                            }
                            if (j == -1)
                            {
                                // complete message
                                var message = buff.ToString(0, i - eom.Length + 1);
                                observer.OnNext(message);
                                buff.Remove(0, i + 1);
                                i = eom.Length - 1;
                            }
                            else
                                i++;
                        }
                    },
                    observer.OnError,
                    observer.OnCompleted);
            });
        }

        public static IObservable<string> ToStrings(this IObservable<byte[]> source)
        {
            // use StreamReader to handle Unicode byte-order-markers correctly
            return source.Select(t => new StreamReader(new MemoryStream(t)).ReadToEnd());
        }

        public static IObservable<Match> ToMatches(this IObservable<string> source, string regex)
        {
            return source.ToMatches(new Regex(regex));
        }

        public static IObservable<Match> ToMatches(this IObservable<string> source, Regex regex)
        {
            var buffer = new StringBuilder();
            return source
                .SelectMany<string,Match>(str =>
                {
                    // add to buffer
                    buffer.Append(str);

                    // look for matches
                    var matches = regex.Matches(buffer.ToString());
                    if (matches.Count > 0)
                    {
                        // remove from buffer
                        var lastMatch = matches[matches.Count-1];
                        buffer.Remove(0, lastMatch.Index + lastMatch.Length);
                    }

                    return matches.OfType<Match>();
                });
        }

        public static string[] ToLines(this string source)
        {
            return Regex.Split(source, @"\r?\n|\r");
        }

        public static IObservable<string> ToLines(this IObservable<string> source)
        {
            return Observable.Create<string>(observer =>
            {
                var sb = new StringBuilder();

                Action produceCurrentLine = () =>
                {
                    var text = sb.ToString();
                    sb.Clear();
                    observer.OnNext(text);
                };

                return source.Subscribe(data =>
                {
                    for (var i = 0; i < data.Length; i++)
                    {
                        var atEndofLine = false;
                        var c = data[i];
                        if (c == '\r')
                        {
                            atEndofLine = true;
                            var j = i + 1;
                            if (j < data.Length && data[j] == '\n')
                                i++;
                        }
                        else if (c == '\n')
                            atEndofLine = true;
                        if (atEndofLine)
                            produceCurrentLine();
                        else
                            sb.Append(c);
                    }
                },
                observer.OnError,
                () =>
                {
                    produceCurrentLine();
                    observer.OnCompleted();
                });
            });
        }

        public static string[] Fields(this string source, string eof)
        {
            // strip final EOF
            if (source.EndsWith(eof))
                source = source.Substring(0, source.Length - 1);

            // empty fields?
            if (source.Length == 0)
                return new string[0];

            // split out
            return source.Split(new[] { eof }, StringSplitOptions.None);
        }

        public static string EncodeFields(this IEnumerable<KeyValuePair<string, string>> fields, bool urlEncode = true)
        {
            return fields == null ? null : string.Join("&", fields.Select(f => string.Format("{0}={1}", f.Key, urlEncode ? HttpUtility.UrlEncode(f.Value) : f.Value)).ToArray());
        }

        public static Task<string> GetPage(this IObservable<byte[]> source)
        {
            var ret = new TaskCompletionSource<string>();
            var mem = new MemoryStream();

            source
                .Subscribe(read => mem.Write(read, 0, read.Length),
                    () =>
                        {
                            mem.Seek(0, SeekOrigin.Begin);
                            var str = new StreamReader(mem).ReadToEnd();
                            ret.SetResult(str);
                        });

            return ret.Task;
        }
    }
}

using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;

namespace Vertigo
{
    public static class AsyncSpeak
    {
        internal static void HandleCompletion<T>(TaskCompletionSource<T> tcs, bool requireMatch, AsyncCompletedEventArgs e, Func<T> getResult, Action unregisterHandler)
        {
            if (!requireMatch || (e.UserState == tcs))
            {
                try
                {
                    unregisterHandler();
                }
                finally
                {
                    if (e.Cancelled)
                    {
                        tcs.TrySetCanceled();
                    }
                    else if (e.Error != null)
                    {
                        tcs.TrySetException(e.Error);
                    }
                    else
                    {
                        tcs.TrySetResult(getResult());
                    }
                }
            }
        }

        public static IObservable<SpeakProgressEventArgs> SpeakAsyncObservable(this SpeechSynthesizer speechSynthesizer, Prompt prompt)
        {
            if (speechSynthesizer == null)
                throw new ArgumentNullException("speechSynthesizer");

            return Observable
                .Create<SpeakProgressEventArgs>(observer =>
                    {
                        var gate = new object();
                        var progressHandler = new EventHandler<SpeakProgressEventArgs>((s, e) =>
                            {
                                if (e.UserState == gate)
                                    observer.OnNext(e);
                            });
                        EventHandler<SpeakCompletedEventArgs> completedHandler = null;
                        completedHandler = (s, e) =>
                            {
                                if (e.UserState == gate)
                                    observer.OnCompleted();
                            };

                        // get notificatinos
                        speechSynthesizer.SpeakProgress += progressHandler;
                        speechSynthesizer.SpeakCompleted += completedHandler;

                        try
                        {
                            speechSynthesizer.SpeakAsync(prompt);
                        }
                        catch
                        {
                            speechSynthesizer.SpeakProgress -= progressHandler;
                            speechSynthesizer.SpeakCompleted -= completedHandler;
                            throw;
                        }

                        return () =>
                            {
                                speechSynthesizer.SpeakProgress -= progressHandler;
                                speechSynthesizer.SpeakCompleted -= completedHandler;
                            };
                    });
        }
    }
}

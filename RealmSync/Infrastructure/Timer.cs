using System;
using System.Threading;
using System.Threading.Tasks;

namespace RealmSync
{
    /// <summary>
    /// Missing from PCL, except if targeting .NET 4.5.1 + Win8.1 + WP8.1
    /// </summary>
    internal sealed class Timer : CancellationTokenSource
    {
        internal Timer(Action<object> callback, object state, int millisecondsDueTime, int millisecondsPeriod, bool waitForCallbackBeforeNextPeriod = false)
            : this((s) =>
            {
                callback(s);
                return Task.FromResult(true);
            }, state, millisecondsDueTime, millisecondsPeriod, waitForCallbackBeforeNextPeriod)
        {
        }

        internal Timer(Func<object, Task> callback, object state, int millisecondsDueTime, int millisecondsPeriod, bool waitForCallbackBeforeNextPeriod = false)
        {
            Task.Delay(millisecondsDueTime, Token).ContinueWith(async (t, s) =>
            {
                var tuple = (Tuple<Func<object, Task>, object>)s;

                while (!IsCancellationRequested)
                {
                    if (waitForCallbackBeforeNextPeriod)
                        await tuple.Item1(tuple.Item2);
                    else
                        await Task.Run(() => tuple.Item1(tuple.Item2));

                    await Task.Delay(millisecondsPeriod, Token).ConfigureAwait(false);
                }

            }, Tuple.Create(callback, state), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Cancel();

            base.Dispose(disposing);
        }
    }
}
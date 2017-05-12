////////////////////////////////////////////////////////////////////////////
//
// Copyright 2017 Rubius
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Realmius.Infrastructure
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
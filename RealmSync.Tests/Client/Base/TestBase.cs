using System;
using System.Threading;

namespace RealmSync.Tests.Client.Base
{
    public class TestBase
    {
        public static void Wait(Func<bool> terminateFunc)
        {
            var startDate = DateTimeOffset.Now;
            while (!terminateFunc() && DateTimeOffset.Now.Subtract(startDate) < TimeSpan.FromSeconds(5))
            {
                Thread.Sleep(10);
            }

            if (!terminateFunc())
                throw new TimeoutException("Wait timed out");
        }
    }
}
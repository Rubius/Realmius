using Realmius.Contracts.Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Realmius.Server.Infrastructure
{
    public class ConsoleLogger : ILogger
    {
        public void Exception(Exception ex, string text = null)
        {
            Trace.WriteLine($"Exception: {ex}, {text}");
        }

        public void Info(string text)
        {
            Trace.WriteLine(text);
        }

        public void Debug(string text)
        {
            Trace.WriteLine(text);
        }
    }
}

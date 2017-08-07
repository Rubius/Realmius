using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Realmius.Contracts.Logger;
using Realmius.Server.Infrastructure;

namespace RealmiusAdvancedExample_Web
{
    public class Logger : ILogger
    {
        public void Exception(Exception ex, string text = null)
        {
            System.Diagnostics.Debug.WriteLine($"Exception {ex}. {text}.");
        }

        public void Info(string text)
        {
            System.Diagnostics.Debug.WriteLine($"Info {text}.");
        }

        public void Debug(string text)
        {
            System.Diagnostics.Debug.WriteLine($"Debug {text}.");
        }
    }
}
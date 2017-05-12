using System;

namespace Realmius.Server
{
    internal class Logger
    {
        public static Logger Log { get; } = new Logger();

        private Logger()
        {
        }

        public void Exception(Exception ex, string text = null)
        {
            System.Diagnostics.Debug.WriteLine($"Exception: {ex}, {text}");
        }

        public void Info(string text)
        {
            System.Diagnostics.Debug.WriteLine(text);
        }

        public void Debug(string text)
        {
            System.Diagnostics.Debug.WriteLine(text);
        }
    }
}

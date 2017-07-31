using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Realmius.Contracts.Logger
{
    public interface ILogger
    {
        void Exception(Exception ex, string text = null);
        void Info(string text);
        void Debug(string text);
    }

    public class Logger : ILogger
    {
        public static Logger Log { get; } = new Logger();

        public Logger()
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

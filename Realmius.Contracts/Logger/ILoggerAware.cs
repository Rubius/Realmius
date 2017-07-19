using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Realmius.Contracts.Logger
{
    public interface ILoggerAware
    {
        ILogger Logger { get; set; }
    }
}

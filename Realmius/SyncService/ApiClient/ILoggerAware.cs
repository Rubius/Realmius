using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Realmius.SyncService.ApiClient
{
    public interface ILoggerAware
    {
        ILogger Logger { get; set; }
    }
}

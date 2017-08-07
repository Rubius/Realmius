using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealmiusAdvancedExample_Web.Interfaces
{
    public interface IUser
    {
        IList<string> Tags { get; }

        string Id { get; }

        string Name { get; }

        string Password { get; }
    }
}

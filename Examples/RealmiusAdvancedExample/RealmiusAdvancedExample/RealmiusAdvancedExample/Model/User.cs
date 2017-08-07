using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealmiusAdvancedExample.Model
{
    public class User
    {
        public string Name { get; }

        public string Password { get; }

        public User()
        {
            var defaultUser = UsersCredentialsDict.GetDefaultUserCreds();
            Name = defaultUser.Key;
            Password = defaultUser.Value;
        }

        public User(string name, string password)
        {
            Name = name;
            Password = password;
        }
    }
}

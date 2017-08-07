using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RealmiusAdvancedExample_Web.Models
{
    public static class UsersCredentialsDict
    {
        private static Dictionary<string, User> _usersCredentials = new Dictionary<string, User>()
        {
            {"admin", new User()
            {
                Id = "0", Name = "Admin", Password = "admin", Role = 3
            }},
            { "john",new User()
            {
                Id = "1", Name = "John", Password = "123", Role= 2
            }},
            { "homer", new User()
            {
                Id = "2", Name = "Homer", Password = "simpson", Role = 1
            }},
            { "anonymous", new User()
            {
                Id = "3", Name = "Anonymous", Password = "anonymous", Role = 0
            }},
        };

        public static User GetUser(string name, string password)
        {
            if (_usersCredentials.ContainsKey(name) && _usersCredentials[name].Password == password.Trim())
                return _usersCredentials[name];

            return null;
        }

        public static User GetDefaultUser()
        {
            return _usersCredentials.Count > 0
                ? _usersCredentials.Last().Value
                : new User
                {
                    Id = "3",
                    Name = "Anonymous",
                    Password = "anonymous",
                    Role = 0
                };
        }
    }
}
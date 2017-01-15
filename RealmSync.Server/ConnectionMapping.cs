using System.Collections.Generic;
using System.Linq;

namespace RealmSync.Server
{
    public class ConnectionMapping<T>
    {
        private readonly Dictionary<string, T> _connections = new Dictionary<string, T>();

        public int Count => _connections.Count;

        public void Add(string connectionId, T info)
        {
            _connections[connectionId] = info;
        }

        public void Remove(string connectionId)
        {
            if (_connections.ContainsKey(connectionId))
                _connections.Remove(connectionId);
        }
    }
}
using Newtonsoft.Json.Serialization;
using Realms;

namespace Realmius.Infrastructure
{
    public class DeserializationBinder: DefaultSerializationBinder
    {
        public Realm Realm { get; set; }
    }
}
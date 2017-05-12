using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Realms;

namespace RealmSync
{
    public class DeserializationBinder: DefaultSerializationBinder
    {
        public Realm Realm { get; set; }
    }
}
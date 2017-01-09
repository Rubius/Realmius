using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace RealmSync.SyncService
{
    internal class RealmObjectResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> props = base.CreateProperties(type, memberSerialization);
            return props.Where(p => p.PropertyName != "Realm"
                                    && p.PropertyName != "ObjectSchema"
                                    && p.PropertyName != "IsManaged"
                                    && p.PropertyName != "IsValid"
			                   		&& p.PropertyName != "LastSynchronizedVersion"
			                   		&& p.PropertyName != "SyncState"
            ).ToList();
        }
    }
}
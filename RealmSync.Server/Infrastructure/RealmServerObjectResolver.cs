using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RealmSync.SyncService;

namespace RealmSync.Server.Infrastructure
{
    internal class RealmServerObjectResolver : DefaultContractResolver
    {
        private static Dictionary<Type, IList<JsonProperty>> _propertyCache = new Dictionary<Type, IList<JsonProperty>>();
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            if (!_propertyCache.ContainsKey(type))
            {
                IList<JsonProperty> props = base.CreateProperties(type, memberSerialization);
                var realmObjectTypeInfo = typeof(IRealmSyncObjectServer).GetTypeInfo();
                var enumerableTypeInfo = typeof(IEnumerable).GetTypeInfo();
                _propertyCache[type] = props.Where(p => p.PropertyName != nameof(IRealmSyncObjectServer.MobilePrimaryKey)
                                           //&& (!p.AttributeProvider.GetAttributes(typeof(DoNotUploadAttribute), true).Any())
                                           && (!realmObjectTypeInfo.IsAssignableFrom(p.PropertyType.GetTypeInfo()) || p.AttributeProvider.GetAttributes(typeof(JsonConverterAttribute), true).Any())
                                           && (!enumerableTypeInfo.IsAssignableFrom(p.PropertyType.GetTypeInfo()) || p.PropertyType == typeof(string) || p.AttributeProvider.GetAttributes(typeof(JsonConverterAttribute), true).Any())

                ).ToList();
            }
            return _propertyCache[type];


        }
    }
}
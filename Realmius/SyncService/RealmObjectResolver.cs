using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Realmius.Infrastructure;
using Realms;

namespace Realmius.SyncService
{
    internal class RealmObjectResolver : DefaultContractResolver
    {
        private static Dictionary<Type, IList<JsonProperty>> _propertyCache = new Dictionary<Type, IList<JsonProperty>>();
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            if (!_propertyCache.ContainsKey(type))
            {
                IList<JsonProperty> props = base.CreateProperties(type, memberSerialization);
                var realmObjectTypeInfo = typeof(RealmObject).GetTypeInfo();
                var enumerableTypeInfo = typeof(IEnumerable).GetTypeInfo();
                _propertyCache[type] = props.Where(p => p.PropertyName != "Realm"
                                        && p.PropertyName != "ObjectSchema"
                                        && p.PropertyName != "IsManaged"
                                        && p.PropertyName != "IsValid"
                                           && p.PropertyName != "LastSynchronizedVersion"
                                           && p.PropertyName != "SyncState"
                                           && (!p.AttributeProvider.GetAttributes(typeof(DoNotUploadAttribute), true).Any())
                                           && (!realmObjectTypeInfo.IsAssignableFrom(p.PropertyType.GetTypeInfo()) || p.AttributeProvider.GetAttributes(typeof(JsonConverterAttribute), true).Any())
                                           && (!enumerableTypeInfo.IsAssignableFrom(p.PropertyType.GetTypeInfo()) || p.PropertyType == typeof(string) || p.AttributeProvider.GetAttributes(typeof(JsonConverterAttribute), true).Any())

                ).ToList();
            }
            return _propertyCache[type];


        }
    }
}
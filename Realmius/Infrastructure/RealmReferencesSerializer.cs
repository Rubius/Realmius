////////////////////////////////////////////////////////////////////////////
//
// Copyright 2017 Rubius
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Realmius.SyncService;
using Realms;
using Realms.Schema;

namespace Realmius.Infrastructure
{
    public class RealmReferencesSerializer : JsonConverter
    {
        private enum KeyType
        {
            String,
            Int
        }
        public Realm Realm { get; set; }
        public bool NotFoundReferencesDetected { get; set; }
        private static readonly Dictionary<string, KeyType> _keyTypes = new Dictionary<string, KeyType>();
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteValue((object)null);
                return;
            }

            var list = value as IEnumerable;
            if (list != null)
            {
                writer.WriteStartArray();
                foreach (var obj in list.OfType<IRealmiusObjectClient>())
                {
                    writer.WriteValue(obj.MobilePrimaryKey);
                }
                writer.WriteEndArray();
                return;
            }

            var realmObject = value as IRealmiusObjectClient;
            if (realmObject == null)
            {
                throw new SerializationException($"{nameof(RealmReferencesSerializer)} can only be applied to properties implementing {nameof(IRealmiusObjectClient)}");
            }

            writer.WriteValue(realmObject.MobilePrimaryKey);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            var container = serializer.Converters.OfType<RealmReferencesSerializer>().First();
            var realm = container.Realm;

            if (reader.TokenType == JsonToken.StartArray)
            {
                var genericTypeArgument = objectType.GenericTypeArguments[0];
                var addMethod = existingValue.GetType().GetRuntimeMethod("Add", new[] { genericTypeArgument });
                var clearMethod = existingValue.GetType().GetRuntimeMethod("Clear", new Type[] { });

                clearMethod.Invoke(existingValue, new object[] { });

                reader.Read();
                var result = new List<object>();
                while (reader.TokenType != JsonToken.EndArray)
                {
                    var referencedObject = GetReferencedObject(realm, reader, genericTypeArgument);
                    if (referencedObject == null)
                    {
                        container.NotFoundReferencesDetected = true;
                    }
                    else
                    {
                        addMethod.Invoke(existingValue, new[] { referencedObject });
                    }

                    reader.Read();
                }
                return result;
            }
            else
            {
                var referencedObject = GetReferencedObject(realm, reader, objectType);
                if (referencedObject == null)
                {
                    container.NotFoundReferencesDetected = true;
                }
                return referencedObject;
            }
        }

        private object GetReferencedObject(Realm realm, JsonReader reader, Type objectType)
        {
            KeyType keyType;
            if (!_keyTypes.TryGetValue(objectType.Name, out keyType))
            {
                keyType = GetKeyType(realm, objectType);
                _keyTypes[objectType.Name] = keyType;
            }

            if (keyType == KeyType.String)
            {
                var key = reader.Value.ToString();
                var referencedObject = realm.Find(objectType.Name, key);
                return referencedObject;
            }
            else if (keyType == KeyType.Int)
            {
                var key = (long)reader.Value;
                var referencedObject = realm.Find(objectType.Name, key);
                return referencedObject;
            }

            throw new NotImplementedException($"Key type {keyType} is not supported");
        }

        private KeyType GetKeyType(Realm realm, Type objectType)
        {
            var schema = realm.Schema.Find(objectType.Name);
            var keyProperty = schema.FirstOrDefault(x => x.IsPrimaryKey);
            if (((byte)keyProperty.Type & (byte)PropertyType.String) == (byte)PropertyType.String)
                return KeyType.String;
            if (((byte)keyProperty.Type & (byte)PropertyType.Int) == (byte)PropertyType.String)
                return KeyType.Int;

            throw new InvalidOperationException($"Not supported key type {keyProperty.Type} for type {objectType}");
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(RealmObject).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }
    }
}
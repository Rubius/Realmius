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
using Newtonsoft.Json;
using Realmius.Server.Models;

namespace Realmius.Server.Infrastructure
{

    public class RealmServerCollectionConverter : JsonConverter
    {
        private static readonly Type EnumerableType = typeof(IEnumerable);
        public ChangeTrackingDbContext Database { get; set; }

        public override bool CanConvert(Type objectType)
        {
            return objectType != typeof(string) && EnumerableType.GetTypeInfo().IsAssignableFrom(objectType);
        }

        public override object ReadJson(
            JsonReader reader, Type objectType,
            object existingValue, JsonSerializer serializer)
        {
            var container = serializer.Converters.OfType<RealmServerCollectionConverter>().First();
            var database = container.Database;

            var genericTypeArgument = objectType.GenericTypeArguments[0];
            var list = typeof(List<>);
            var listOfType = list.MakeGenericType(genericTypeArgument);

            var newList = Activator.CreateInstance(listOfType);

            if (existingValue == null)
            {
                existingValue = newList;
            }

            var addMethod = existingValue.GetType().GetRuntimeMethod("Add", new[] { genericTypeArgument });
            var clearMethod = existingValue.GetType().GetRuntimeMethod("Clear", new Type[] { });

            clearMethod.Invoke(existingValue, new object[] { });


            if (reader.TokenType == JsonToken.StartArray)
            {

                reader.Read();
                while (reader.TokenType != JsonToken.EndArray)
                {
                    var referencedObject = GetReferencedObject(database, reader, genericTypeArgument);
                    if (referencedObject == null)
                    {
                        throw new InvalidOperationException("Referenced object is not found");
                    }

                    addMethod.Invoke(existingValue, new[] { referencedObject });

                    reader.Read();
                }
            }

            return existingValue;
        }

        private object GetReferencedObject(ChangeTrackingDbContext database, JsonReader reader, Type objectType)
        {
            return database.GetObjectByKey(objectType.Name, reader.Value.ToString());
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var list = value as IEnumerable;
            if (list == null) return;

            writer.WriteStartArray();
            foreach (var obj in list.OfType<IRealmiusObjectServer>())
            {
                writer.WriteValue(obj.MobilePrimaryKey);
            }
            writer.WriteEndArray();
        }
    }
}
﻿////////////////////////////////////////////////////////////////////////////
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
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Realmius.Server.Infrastructure
{
    internal class RealmServerObjectResolver : DefaultContractResolver
    {
        private static readonly Dictionary<Type, IList<JsonProperty>> PropertyCache = new Dictionary<Type, IList<JsonProperty>>();

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            if (!PropertyCache.ContainsKey(type))
            {
                IList<JsonProperty> props = base.CreateProperties(type, memberSerialization);
                var realmObjectTypeInfo = typeof(IRealmiusObjectServer).GetTypeInfo();
                var enumerableTypeInfo = typeof(IEnumerable).GetTypeInfo();
                PropertyCache[type] = props.Where(p => p.PropertyName != nameof(IRealmiusObjectServer.MobilePrimaryKey)
                                           && !p.AttributeProvider.GetAttributes(typeof(NotMappedAttribute), true).Any()
                                           && (!realmObjectTypeInfo.IsAssignableFrom(p.PropertyType.GetTypeInfo()) || p.AttributeProvider.GetAttributes(typeof(JsonConverterAttribute), true).Any())
                                           && (!enumerableTypeInfo.IsAssignableFrom(p.PropertyType.GetTypeInfo()) || p.PropertyType == typeof(string) || p.AttributeProvider.GetAttributes(typeof(JsonConverterAttribute), true).Any())

                ).ToList();
            }
            return PropertyCache[type];
        }
    }
}
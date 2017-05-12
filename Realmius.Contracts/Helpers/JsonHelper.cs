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
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Realmius.Contracts.Helpers
{
    public class JsonHelper
    {

        public static string GetJsonDiff(object existing, object modified)
        {
            return GetJsonDiff(JObject.FromObject(existing), JObject.FromObject(modified)).ToString();
        }

        public static string GetJsonDiff(string existing, string modified)
        {
            JObject modifiedJson = JObject.Parse(modified);
            JObject existingJson = JObject.Parse(existing);

            return GetJsonDiff(existingJson, modifiedJson).ToString();
        }

        public static JObject GetJsonDiffAsJObject(string existing, string modified)
        {
            JObject modifiedJson = JObject.Parse(modified);
            JObject existingJson = JObject.Parse(existing);

            return GetJsonDiff(existingJson, modifiedJson);
        }

        public static JObject GetJsonDiff(JObject existingJson, JObject modifiedJson)
        {
            // read properties
            var modifiedProps = modifiedJson.Properties().ToDictionary(x => x.Name, x => x);
            var existingProps = existingJson.Properties().ToDictionary(x => x.Name, x => x);

            var result = new JObject();

            foreach (var modifiedProp in modifiedProps)
            {
                if (existingProps.ContainsKey(modifiedProp.Key))
                {
                    if (modifiedProp.Value.Value.Equals(existingProps[modifiedProp.Key].Value))
                        continue;
                }

                result.Add(modifiedProp.Key, modifiedProp.Value.Value);
            }
            return result;
        }


        public static string SerializeObject(object obj, int maxDepth)
        {
            using (var strWriter = new StringWriter())
            {
                using (var jsonWriter = new DepthAwareJsonTextWriter(strWriter))
                {
                    Func<bool> include = () => jsonWriter.CurrentDepth <= maxDepth;
                    var resolver = new DepthAwareContractResolver(include);
                    var serializer = new JsonSerializer { ContractResolver = resolver };
                    serializer.Serialize(jsonWriter, obj);
                }
                return strWriter.ToString();
            }
        }
    }
}
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

using FluentAssertions;
using Realmius.Contracts.Helpers;
using Xunit;

namespace Realmius.Tests
{
    public class JsonHelperTests
    {
        [Fact]
        public void GetDiff_SameObjects()
        {
            var obj = new { a = 1, b = "2" };
            var diff = JsonHelper.GetJsonDiff(obj, obj);
            diff.Should().BeEquivalentTo("{}");
        }

        [Fact]
        public void GetDiff_EqualObjects()
        {
            var diff = JsonHelper.GetJsonDiff(new { a = 1, b = "2" }, new { a = 1, b = "2" });
            diff.Should().BeEquivalentTo("{}");
        }


        [Fact]
        public void GetDiff_OneDiff()
        {
            var diff = JsonHelper.GetJsonDiff(new { a = 1, b = "2", c = "543" }, new { a = 21, b = "21", c = "543" });
            diff.Should().BeEquivalentTo("{\r\n  \"a\": 21,\r\n  \"b\": \"21\"\r\n}");
        }
    }
}

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
using NUnit.Framework;
using Realmius.SyncService.RealmModels;
using Xunit;

namespace Realmius.Tests.Client
{
    public class ObjectSyncStatusRealmTests
    {
        [Fact]
        public void KeyGenerationTest1()
        {
            var status = new ObjectSyncStatusRealm
            {
                MobilePrimaryKey = "123",
                Type = "qwe"
            };

            status.MobilePrimaryKey.Should().BeEquivalentTo("123");
            status.Type.Should().BeEquivalentTo("qwe");
        }

        [Fact]
        public void KeyGenerationTest2()
        {
            var status = new ObjectSyncStatusRealm
            {
                MobilePrimaryKey = "123",
                Type = "qwe"
            };

            var key = status.Key;
            var status2 = new ObjectSyncStatusRealm { Key = key };

            status2.MobilePrimaryKey.Should().Be("123");
            status2.Type.Should().Be("qwe");

            status2.Type = "zxc";

            status2.MobilePrimaryKey.Should().Be("123");
            status2.Type.Should().Be("zxc");
        }
    }
}
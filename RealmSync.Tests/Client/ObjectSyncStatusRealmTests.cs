using FluentAssertions;
using NUnit.Framework;
using Realmius.SyncService.RealmModels;

namespace Realmius.Tests.Client
{
    [TestFixture]
    public class ObjectSyncStatusRealmTests
    {
        [Test]
        public void KeyGenerationTest1()
        {
            var status = new ObjectSyncStatusRealm();
            status.MobilePrimaryKey = "123";
            status.Type = "qwe";

            status.MobilePrimaryKey.Should().BeEquivalentTo("123");
            status.Type.Should().BeEquivalentTo("qwe");
        }

        [Test]
        public void KeyGenerationTest2()
        {
            var status = new ObjectSyncStatusRealm();
            status.MobilePrimaryKey = "123";
            status.Type = "qwe";

            var key = status.Key;

            var status2 = new ObjectSyncStatusRealm() { Key = key };
            status2.MobilePrimaryKey.Should().Be("123");
            status2.Type.Should().Be("qwe");

            status2.Type = "zxc";

            status2.MobilePrimaryKey.Should().Be("123");
            status2.Type.Should().Be("zxc");
        }
    }
}
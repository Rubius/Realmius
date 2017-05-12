using RealmSync.SyncService;

using FluentAssertions;
using NUnit.Framework;

namespace RealmSync.Tests
{
    [TestFixture]
    public class JsonHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void GetDiff_SameObjects()
        {
            var obj = new { a = 1, b = "2" };
            var diff = JsonHelper.GetJsonDiff(obj, obj);
            diff.Should().BeEquivalentTo("{}");
        }

        [Test]
        public void GetDiff_EqualObjects()
        {
            var diff = JsonHelper.GetJsonDiff(new { a = 1, b = "2" }, new { a = 1, b = "2" });
            diff.Should().BeEquivalentTo("{}");
        }


        [Test]
        public void GetDiff_OneDiff()
        {
            var diff = JsonHelper.GetJsonDiff(new { a = 1, b = "2", c = "543" }, new { a = 21, b = "21", c = "543" });
            diff.Should().BeEquivalentTo("{\r\n  \"a\": 21,\r\n  \"b\": \"21\"\r\n}");
        }
    }
}

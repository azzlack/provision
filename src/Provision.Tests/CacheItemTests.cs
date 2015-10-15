namespace Provision.Tests
{
    using NUnit.Framework;
    using Provision.Extensions;
    using Provision.Models;
    using Provision.Tests.Models;
    using System;

    [TestFixture]
    public class CacheItemTests
    {
        [Test]
        public void MergeExpire_WhenGivenCacheItemWithPocoObject_ShouldReturnExpire()
        {
            var ci = new CacheItem<ExpireableReport>("test", new ExpireableReport(), DateTime.UtcNow.AddDays(1));
            ci.MergeExpire();

            Assert.That(ci.Value.Expires == ci.Expires);
        }
    }
}
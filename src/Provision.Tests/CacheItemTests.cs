namespace Provision.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using NUnit.Framework;

    using Provision.Extensions;
    using Provision.Models;
    using Provision.Tests.Models;

    [TestFixture]
    public class CacheItemTests
    {
        [Test]
        public void MergeExpire_WhenGivenCacheItemWithPocoObject_ShouldReturnExpire()
        {
            var ci = new CacheItem<ExpireableReport>("test", new ExpireableReport(), DateTime.Now.AddDays(1));
            ci.MergeExpire();

            Assert.That(ci.Value.Expires == ci.Expires);
        }
    }
}
using Provision.Models;

namespace Provision.Tests
{
    using System;

    using NUnit.Framework;
    using Provision.Providers.Redis;
    using Provision.Tests.Models;

    public class RedisCacheHandlerTests : BaseCacheHandlerTests
    {
        public override void SetUp()
        {
            this.CacheHandlers =
                new CacheHandlerCollection(
                    new RedisCacheHandler(new RedisCacheHandlerConfiguration("localhost", 6379, 3, null, "provision",
                        512, null, true)));

            base.SetUp();
        }

        [Test]
        public void Compress_WhenNotExplicitlySet_ShouldReturnFalse()
        {
            var ch = new RedisCacheHandler(new RedisCacheHandlerConfiguration());

            Assert.IsFalse(((RedisCacheHandlerConfiguration)ch.Configuration).Compress);
        }

        [Test]
        public void Compress_WhenSetToTrue_ShouldReturnTrue()
        {
            Assert.IsTrue(((RedisCacheHandlerConfiguration)this.CacheHandlers[0].Configuration).Compress);
        }

        [Test]
        public async void RemoveExpiredKeys_WhenAddingItem_ShouldRemoveExpiredKeys()
        {
            var ch = new RedisCacheHandler(new RedisCacheHandlerConfiguration(prefix: "RemoveExpiredKeys_WhenAddingItem_ShouldRemoveExpiredKeys"));
            var config = (RedisCacheHandlerConfiguration)ch.Configuration;

            var data = new Report() { Rating = 10 };
            var k = ch.CreateKey("first");

            await ch.AddOrUpdate(k, data, DateTimeOffset.UtcNow, "mytag");

            var removedKeys = await ch.RemoveExpiredKeys();

            var key = ch.GetDatabase().SortedSetScan(config.IndexKey, k);
            var tag = ch.GetDatabase().SortedSetScan($"{config.TagKey}{config.Separator}mytag", k);

            Assert.Greater(removedKeys, 0);
            CollectionAssert.IsEmpty(key);
            CollectionAssert.IsEmpty(tag);
        }
    }
}
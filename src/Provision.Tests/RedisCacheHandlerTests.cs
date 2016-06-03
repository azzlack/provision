using Provision.Models;

namespace Provision.Tests
{
    using NUnit.Framework;
    using Provision.Providers.Redis;

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
    }
}
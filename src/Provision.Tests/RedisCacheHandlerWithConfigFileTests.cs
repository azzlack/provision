using Provision.Providers.Redis;

namespace Provision.Tests
{
    using NUnit.Framework;
    using Provision.Config;

    public class RedisCacheHandlerWithConfigFileTests : BaseCacheHandlerTests
    {
        public override void SetUp()
        {
            this.CacheHandler = ProvisionConfiguration.Current.GetHandler();

            base.SetUp();
        }

        [Test]
        public void Config_WhenCompressHaveBeenSetToFalse_ShouldReturnFalse()
        {
            Assert.IsFalse(((RedisCacheHandlerConfiguration)this.CacheHandler.Configuration).Compress);
        }
    }
}
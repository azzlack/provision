using Provision.Providers.Redis;

namespace Provision.Tests
{
    using NUnit.Framework;
    using Provision.Config;

    public class RedisCacheHandlerWithConfigFileTests : BaseCacheHandlerTests
    {
        public override void SetUp()
        {
            this.CacheHandlers = ProvisionConfiguration.Current.GetHandlers();

            base.SetUp();
        }

        [Test]
        public void Config_WhenCompressHaveBeenSetToFalse_ShouldReturnFalse()
        {
            Assert.IsFalse(((RedisCacheHandlerConfiguration)this.CacheHandlers[1].Configuration).Compress);
        }
    }
}
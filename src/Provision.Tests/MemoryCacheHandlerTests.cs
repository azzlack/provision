using Provision.Models;

namespace Provision.Tests
{
    using NUnit.Framework;
    using Provision.Providers.MemoryCache;
    using Provision.Tests.Models;

    public class MemoryCacheHandlerTests : BaseCacheHandlerTests
    {
        public override void SetUp()
        {
            this.CacheHandlers = new CacheHandlerCollection(new MemoryCacheHandler());
            this.CacheHandlers.Purge().ConfigureAwait(false);

            base.SetUp();
        }

        [Test]
        public async void Construct_WhenGivenValidConfiguration_ShouldApplyConfiguration()
        {
            var ch = new MemoryCacheHandler(new MemoryCacheHandlerConfiguration("0 0 0/1 1/1 * ? *"));

            Assert.AreEqual(typeof(MemoryCacheHandler), ch.Configuration.Type);
            Assert.AreEqual("0 0 0/1 1/1 * ? *", ch.Configuration.ExpireTime);

            var key = ch.CreateKey("whale", "fail");

            var d = new ReportItem() { Key = "grumpy" };

            await ch.AddOrUpdate(key, d);

            var r = await ch.GetValue<ReportItem>(key);

            Assert.AreEqual(d.Key, r.Key);
        }
    }
}
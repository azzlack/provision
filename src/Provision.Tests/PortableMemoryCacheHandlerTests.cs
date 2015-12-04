namespace Provision.Tests
{
    using NUnit.Framework;
    using Provision.Providers.PortableMemoryCache;
    using Provision.Tests.Models;

    public class PortableMemoryCacheHandlerTests : BaseCacheHandlerTests
    {
        public override void SetUp()
        {
            this.CacheHandler = new PortableMemoryCacheHandler();

            base.SetUp();
        }

        [Test]
        public async void Construct_WhenGivenValidConfiguration_ShouldApplyConfiguration()
        {
            var ch = new PortableMemoryCacheHandler(new PortableMemoryCacheHandlerConfiguration("0 0 0/1 1/1 * ? *"));

            Assert.AreEqual("pclmem", ch.Configuration.Name);
            Assert.AreEqual("0 0 0/1 1/1 * ? *", ch.Configuration.ExpireTime);

            var key = ch.CreateKey("whale", "fail");

            var d = new ReportItem() { Key = "grumpy" };

            await ch.AddOrUpdate(key, d);

            var r = await ch.GetValue<ReportItem>(key);

            Assert.AreEqual(d.Key, r.Key);
        }
    }
}
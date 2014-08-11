namespace Provision.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using NUnit.Framework;

    using Provision.Interfaces;
    using Provision.Providers.PortableMemoryCache;
    using Provision.Tests.Models;

    [TestFixture]
    public class PortableMemoryCacheHandlerTests
    {
        private ICacheHandler cacheHandler;

        [SetUp]
        public void SetUp()
        {
            this.cacheHandler = new PortableMemoryCacheHandler();
        }

        [Test]
        public async void CreateKey_WhenGivenValidType_ShouldCreateValidKey()
        {
            var key = this.cacheHandler.CreateKey<Report>("reports", "something", "k4", "2014");

            Assert.AreEqual("reports_something_k4_2014", key);
        }

        [Test]
        public async void AddOrUpdate_WhenGivenValidDataWithExpireDate_ShouldInsertData()
        {
            var d = new Report()
            {
                Items = new List<ReportItem>()
                            {
                                new ReportItem() { Key = "1", Data = 100 }
                            }
            };

            var key = this.cacheHandler.CreateKey<Report>("report", "k4");

            await this.cacheHandler.AddOrUpdate(key, d, DateTime.Now.AddDays(1));

            var r = await this.cacheHandler.Contains<Report>("report_k4");

            Assert.IsTrue(r);
        }

        [Test]
        public async void AddOrUpdate_WhenGivenValidData_ShouldInsertData()
        {
            var d = new Report();

            var key = this.cacheHandler.CreateKey<Report>("reports", "blahblah", "k4", "2014");

            await this.cacheHandler.AddOrUpdate(key, d);

            var r = await this.cacheHandler.Contains<Report>("reports_blahblah_k4_2014");

            Assert.IsTrue(r);
        }

        [Test]
        public async void Get_WhenGivenExistingKey_ShouldReturnItem()
        {
            var d = new Report()
            {
                Items = new List<ReportItem>()
                            {
                                new ReportItem() { Key = "1", Data = 100 }
                            }
            };

            var key = this.cacheHandler.CreateKey<Report>("report", "k54");

            await this.cacheHandler.AddOrUpdate(key, d, DateTime.Now.AddDays(1));

            var r = await this.cacheHandler.GetValue<Report>("report_k54");

            Assert.IsNotNull(r);
            Assert.IsNotNull(r.Items);
            Assert.AreEqual(d.Items.First().Key, r.Items.First().Key);
        }

        [Test]
        public async void Get_WhenGivenExpiredKey_ShouldReturnNull()
        {
            var d = new Report()
            {
                Items = new List<ReportItem>()
                            {
                                new ReportItem() { Key = "1", Data = 100 }
                            }
            };

            var key = this.cacheHandler.CreateKey<Report>("report", "k84");

            await this.cacheHandler.AddOrUpdate(key, d, DateTime.Now.AddDays(-1));

            var r = await this.cacheHandler.Get<Report>("report_k84");

            Assert.IsFalse(r.HasValue);
            Assert.IsNull(r.Value);
        }

        [Test]
        public async void Get_WhenGivenNonExistingKey_ShouldReturnNull()
        {
            var r = await this.cacheHandler.GetValue<Report>("report_k445697894231,3");

            Assert.IsNull(r);
        }

        [Test]
        public async void Remove_WhenGivenValidData_ShouldRemoveData()
        {
            var d = new Report();

            var key = this.cacheHandler.CreateKey<Report>("reports", "training", "k5", "2014");

            await this.cacheHandler.AddOrUpdate(key, d);

            await this.cacheHandler.Remove<Report>(key);

            var exists = await this.cacheHandler.Contains<Report>(key);

            Assert.IsFalse(exists);
        }

        [Test]
        public async void Purge_WhenPurged_ShouldNotContainCacheItems()
        {
            var d = new Report();

            var key = this.cacheHandler.CreateKey<Report>("reports", "football", "k4", "2014");

            await this.cacheHandler.AddOrUpdate(key, d);

            var r1 = await this.cacheHandler.Contains<Report>("reports_football_k4_2014");

            Assert.IsTrue(r1);

            var p = await this.cacheHandler.Purge();

            var r2 = await this.cacheHandler.Contains<Report>("reports_football_k4_2014");

            Assert.IsFalse(r2);
        }
    }
}
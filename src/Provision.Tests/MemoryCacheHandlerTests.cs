namespace Provision.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    using Provision.Interfaces;
    using Provision.Providers.MemoryCache;
    using Provision.Tests.Extensions;
    using Provision.Tests.Models;

    [TestFixture]
    public class MemoryCacheHandlerTests
    {
        private ICacheHandler cacheHandler;

        [SetUp]
        public void SetUp()
        {
            this.cacheHandler = new MemoryCacheHandler();
        }

        [Test]
        public async void Construct_WhenGivenValidConfiguration_ShouldApplyConfiguration()
        {
            var ch = new MemoryCacheHandler(new MemoryCacheHandlerConfiguration("0 0 0/1 1/1 * ? *"));

            Assert.AreEqual("mem", ch.Configuration.Name);
            Assert.AreEqual("0 0 0/1 1/1 * ? *", ch.Configuration.ExpireTime);
            
            var key = ch.CreateKey("whale", "fail");

            var d = new ReportItem() { Key = "grumpy" };

            await ch.AddOrUpdate(key, d);

            var r = await ch.GetValue<ReportItem>(key);

            Assert.AreEqual(d.Key, r.Key);
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

            var r = await this.cacheHandler.Contains("report_k4");

            Assert.IsTrue(r);
        }

        [Test]
        public async void AddOrUpdate_WhenGivenValidData_ShouldInsertData()
        {
            var d = new Report();

            var key = this.cacheHandler.CreateKey<Report>("reports", "blahblah", "k4", "2014");

            await this.cacheHandler.AddOrUpdate(key, d);

            var r = await this.cacheHandler.Contains("reports_blahblah_k4_2014");

            Assert.IsTrue(r);
        }

        [Test]
        public async void AddOrUpdate_WhenUpdatingEntry_ShouldNotMutate()
        {
            var d = new ReportItem() { Key = "Test1" };

            var key = this.cacheHandler.CreateKey<Report>("reports", "blahblah", "k4", "2014");

            var obj1 = await this.cacheHandler.AddOrUpdate(key, d);

            var obj2 = await this.cacheHandler.GetValue<ReportItem>(key);

            var update = obj2.Clone();
            update.Key = "Test2";

            var obj3 = await this.cacheHandler.AddOrUpdate(key, update);

            Assert.AreEqual(obj1.Key, obj2.Key, "The item added to the cache is not the same as was sent in");
            Assert.AreEqual(update.Key, obj3.Key, "The item updated in the cache is not the same as was sent in");
            Assert.AreNotEqual(obj2.Key, update.Key, "The item updated in the cache has mutated the earlier object");
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
        public async void Remove_WhenGivenValidKey_ShouldRemoveData()
        {
            var d = new Report();

            var key = this.cacheHandler.CreateKey<Report>("reports", "training", "k5", "2014");

            await this.cacheHandler.AddOrUpdate(key, d);

            await this.cacheHandler.Remove(key);

            var exists = await this.cacheHandler.Contains(key);

            Assert.IsFalse(exists);
        }

        [Test]
        public async void Remove_WhenGivenValidPattern_ShouldRemoveData()
        {
            var d = new Report();

            var key1 = this.cacheHandler.CreateKey<Report>("reports", "love", "ks", "2013");
            await this.cacheHandler.AddOrUpdate(key1, d, DateTime.Now.AddMinutes(1));

            var key2 = this.cacheHandler.CreateKey<Report>("reports", "love", "ks", "2014");
            await this.cacheHandler.AddOrUpdate(key2, d, DateTime.Now.AddMinutes(1));

            var key3 = this.cacheHandler.CreateKey<Report>("letter", "love", "ks", "2014");
            await this.cacheHandler.AddOrUpdate(key3, d, DateTime.Now.AddMinutes(1));

            await this.cacheHandler.RemoveAll("reports_love_ks_*");

            var exists1 = await this.cacheHandler.Contains(key1);
            var exists2 = await this.cacheHandler.Contains(key2);
            var exists3 = await this.cacheHandler.Contains(key3);

            Assert.IsFalse(exists1);
            Assert.IsFalse(exists2);
            Assert.IsTrue(exists3);
        }

        [Test]
        public async void Remove_WhenGivenValidRegex_ShouldRemoveData()
        {
            var d = new Report();

            var key1 = this.cacheHandler.CreateKey<Report>("reports", "love", "ks", "2013");
            await this.cacheHandler.AddOrUpdate(key1, d, DateTime.Now.AddMinutes(1));

            var key2 = this.cacheHandler.CreateKey<Report>("reports", "love", "ks", "2014");
            await this.cacheHandler.AddOrUpdate(key2, d, DateTime.Now.AddMinutes(1));

            var key3 = this.cacheHandler.CreateKey<Report>("letter", "love", "ks", "2014");
            await this.cacheHandler.AddOrUpdate(key3, d, DateTime.Now.AddMinutes(1));

            await this.cacheHandler.RemoveAll(new Regex("reports_love_ks_*"));

            var exists1 = await this.cacheHandler.Contains(key1);
            var exists2 = await this.cacheHandler.Contains(key2);
            var exists3 = await this.cacheHandler.Contains(key3);

            Assert.IsFalse(exists1);
            Assert.IsFalse(exists2);
            Assert.IsTrue(exists3);
        }

        [Test]
        public async void Purge_WhenPurged_ShouldNotContainCacheItems()
        {
            var d = new Report();

            var key = this.cacheHandler.CreateKey<Report>("reports", "football", "k4", "2014");

            await this.cacheHandler.AddOrUpdate(key, d);

            var r1 = await this.cacheHandler.Contains("reports_football_k4_2014");

            Assert.IsTrue(r1);

            var p = await this.cacheHandler.Purge();

            var r2 = await this.cacheHandler.Contains("reports_football_k4_2014");

            Assert.IsFalse(r2);
        }
    }
}
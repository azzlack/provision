namespace Provision.Tests
{
    using NUnit.Framework;
    using Provision.Interfaces;
    using Provision.Providers.Redis;
    using Provision.Tests.Extensions;
    using Provision.Tests.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    [TestFixture]
    public class RedisCacheHandlerTests
    {
        private ICacheHandler cacheHandler;

        [SetUp]
        public void SetUp()
        {
            this.cacheHandler = new RedisCacheHandler(new RedisCacheHandlerConfiguration("localhost", 6379, 3, null, "provision", 512, null, true));
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
            Assert.IsTrue(((RedisCacheHandlerConfiguration)this.cacheHandler.Configuration).Compress);
        }

        [Test]
        public async void CreateKey_WhenGivenValidType_ShouldCreateValidKey()
        {
            var key = this.cacheHandler.CreateKey<Report>("reports", "1234567", "k4", "2014");

            Assert.AreEqual("provision:reports:1234567:k4:2014", key);
        }

        [Test]
        public async void AddOrUpdate_WhenGivenValidHashSetDataWithExpireDate_ShouldInsertData()
        {
            var d = new Report()
            {
                Items = new List<ReportItem>()
                            {
                                new ReportItem() { Key = "1", Data = 100 }
                            }
            };

            var key = string.Format("{0}#{1}", this.cacheHandler.CreateKey<Report>("xyz", "1"), "k4");

            await this.cacheHandler.AddOrUpdate(key, d, DateTime.UtcNow.AddDays(1));

            await Task.Delay(1000);

            var r = await this.cacheHandler.Get<Report>("provision:xyz:1#k4");

            Assert.IsNotNull(r);
            Assert.That(r.Expires > DateTime.MinValue);
        }
        [Test]
        public async void AddOrUpdateWithTags_WhenGivenValidExpireDate_ShouldInsertData()
        {
            var d = new Report()
            {
                Items = new List<ReportItem>()
                            {
                                new ReportItem() { Key = "1", Data = 100 }
                            }
            };

            var key = this.cacheHandler.CreateKey<Report>("report", "k4");

            await this.cacheHandler.AddOrUpdate(key, d, DateTime.UtcNow.AddDays(1), "report");

            await Task.Delay(1000);

            var r = await this.cacheHandler.Contains(key);

            Assert.IsTrue(r);
        }
        [Test]
        public async void RemoveTag_WhenGivenValidTag_ShouldRemoveData()
        {
            var d = new Report()
            {
                Items = new List<ReportItem>()
                            {
                                new ReportItem() { Key = "1", Data = 100 }
                            }
            };

            var key = this.cacheHandler.CreateKey<Report>("report", "k4");

            await this.cacheHandler.AddOrUpdate(key, d, DateTime.UtcNow.AddDays(1), "report");

            await Task.Delay(1000);

            await this.cacheHandler.RemoveTags("report");

            await Task.Delay(1000);

            var r = await this.cacheHandler.Contains(key);

            Assert.IsFalse(r);
        }
        [Test]
        public async void RemoveSpesificTag_WhenGivenValidTag_ShouldRemoveSpesificData()
        {
            var d = new Report()
            {
                Items = new List<ReportItem>()
                            {
                                new ReportItem() { Key = "1", Data = 100 }
                            }
            };

            var key = this.cacheHandler.CreateKey<Report>("report", "k4");

            await this.cacheHandler.AddOrUpdate(key, d, DateTime.UtcNow.AddDays(1), "report");

            var d2 = new Report()
            {
                Items = new List<ReportItem>()
                            {
                                new ReportItem() { Key = "1", Data = 100 }
                            }
            };

            var key2 = this.cacheHandler.CreateKey<Report>("report2", "k4");

            await this.cacheHandler.AddOrUpdate(key2, d2, DateTime.UtcNow.AddDays(1), "report2");

            await Task.Delay(1000);

            await this.cacheHandler.RemoveTags("report");

            var r = await this.cacheHandler.Contains(key);
            var r2 = await this.cacheHandler.Contains(key2);

            Assert.IsFalse(r);
            Assert.IsTrue(r2);

        }

        [Test]
        public async void AddOrUpdate_WhenGivenValidHashSetData_ShouldInsertData()
        {
            var d = new Report()
            {
                Items = new List<ReportItem>()
                            {
                                new ReportItem() { Key = "1", Data = 100 }
                            }
            };

            var key = string.Format("{0}#{1}", this.cacheHandler.CreateKey("delivery", "2"), "k4");

            await this.cacheHandler.AddOrUpdate(key, d);

            await Task.Delay(1000);

            var r = await this.cacheHandler.Get<Report>("provision:delivery:2#k4");

            Assert.IsNotNull(r);
            Assert.That(r.Expires > DateTime.MinValue);
        }

        [Test]
        public async void AddOrUpdate_WhenGivenValidNonHashSetData_ShouldInsertData()
        {
            var d = new Report();

            var key = this.cacheHandler.CreateKey<Report>("reports", "delivery", "k4", "2014");

            await this.cacheHandler.AddOrUpdate(key, d);

            await Task.Delay(1000);

            var r = await this.cacheHandler.Contains("provision:reports:delivery:k4:2014");

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
        public async void Remove_WhenGivenValidHashSetData_ShouldRemoveData()
        {
            var d = new Report();

            var key = string.Format("{0}#{1}", this.cacheHandler.CreateKey<Report>("traffic", "8"), "k4");

            await this.cacheHandler.AddOrUpdate(key, d);

            await Task.Delay(1000);

            await this.cacheHandler.Remove(key);

            await Task.Delay(1000);

            var exists = await this.cacheHandler.Contains(key);

            Assert.IsFalse(exists);
        }

        [Test]
        public async void Remove_WhenGivenValidNonHashSetData_ShouldRemoveData()
        {
            var d = new Report();

            var key = this.cacheHandler.CreateKey<Report>("reports", "garlic", "k5", "2014");

            await this.cacheHandler.AddOrUpdate(key, d);

            await Task.Delay(1000);

            await this.cacheHandler.Remove(key);

            await Task.Delay(1000);

            var exists = await this.cacheHandler.Contains(key);

            Assert.IsFalse(exists);
        }

        [Test]
        public async void Remove_WhenGivenValidRegex_ShouldThrowException()
        {
            Assert.Throws<NotSupportedException>(async () => await this.cacheHandler.RemoveAll(new Regex("provision:reports:love:ks:*")));
        }

        [Test]
        public async void Remove_WhenGivenValidPattern_ShouldRemoveData()
        {
            var d = new Report();

            var key1 = this.cacheHandler.CreateKey<Report>("reports", "love", "ks", "2013");
            await this.cacheHandler.AddOrUpdate(key1, d, DateTime.UtcNow.AddMinutes(1));

            var key2 = this.cacheHandler.CreateKey<Report>("reports", "love", "ks", "2014");
            await this.cacheHandler.AddOrUpdate(key2, d, DateTime.UtcNow.AddMinutes(1));

            var key3 = this.cacheHandler.CreateKey<Report>("letter", "love", "ks", "2014");
            await this.cacheHandler.AddOrUpdate(key3, d, DateTime.UtcNow.AddMinutes(1));

            var key4 = string.Format("{0}#{1}", this.cacheHandler.CreateKey<Report>("reports", "love", "ks", "2012"), "jan");

            await Task.Delay(1000);

            await this.cacheHandler.RemoveAll("provision:reports:love:ks:*");

            await Task.Delay(1000);

            var exists1 = await this.cacheHandler.Contains(key1);
            var exists2 = await this.cacheHandler.Contains(key2);
            var exists3 = await this.cacheHandler.Contains(key3);
            var exists4 = await this.cacheHandler.Contains(key4);

            Assert.IsFalse(exists1);
            Assert.IsFalse(exists2);
            Assert.IsTrue(exists3);
            Assert.IsFalse(exists4);
        }

        [Test]
        public async void GetAs_WhenGivenValidKey_ShouldReturnWrapper()
        {
            var d = new Report()
            {
                Items = new List<ReportItem>()
                            {
                                new ReportItem() { Key = "1", Data = 100 }
                            }
            };

            var key = this.cacheHandler.CreateKey<Report>("wrap", "up");

            await this.cacheHandler.AddOrUpdate(key, d);

            await Task.Delay(1000);

            var r = await this.cacheHandler.GetAs<ReportCacheItem, Report>(key);

            Assert.IsNotNull(r);
            Assert.IsInstanceOf<ReportCacheItem>(r);
            Assert.IsInstanceOf<ICacheItem<Report>>(r);
            Assert.AreEqual(d.Items.First().Key, r.Value.Items.First().Key);
        }

        [Test]
        public async void Get_WhenGivenValidHashSetKey_ShouldReturnData()
        {
            var d = new Report()
            {
                Items = new List<ReportItem>()
                            {
                                new ReportItem() { Key = "1", Data = 100 }
                            }
            };

            var key = this.cacheHandler.CreateKey<Report>("fish", "11");

            await this.cacheHandler.AddOrUpdate(key, d);

            await Task.Delay(1000);

            var r = await this.cacheHandler.Get<Report>(key);

            Assert.IsNotNull(r);
            Assert.AreEqual(d.Items.First().Key, r.Value.Items.First().Key);
        }

        [Test]
        public async void Get_WhenGivenValidNonHashSetKey_ShouldReturnData()
        {
            var d = new Report()
            {
                Items = new List<ReportItem>()
                            {
                                new ReportItem() { Key = "1", Data = 100 }
                            }
            };

            var key = this.cacheHandler.CreateKey("reports", "beans", "k4", "2014");

            await this.cacheHandler.AddOrUpdate(key, d);

            await Task.Delay(1000);

            var r = await this.cacheHandler.Get<Report>(key);

            Assert.IsNotNull(r);
            Assert.AreEqual("1", r.Value.Items.First().Key);
        }

        [Test]
        public async void Purge_WhenSuccessful_ShouldRemoveAllData()
        {
            var d = new Report();

            var key = this.cacheHandler.CreateKey<Report>("reports", "monthlyconsumption", "k4", "2014");

            await this.cacheHandler.AddOrUpdate(key, d);

            await Task.Delay(1000);

            var r1 = await this.cacheHandler.Contains("provision:reports:monthlyconsumption:k4:2014");

            Assert.IsTrue(r1);

            await this.cacheHandler.Purge();

            await Task.Delay(1000);

            var r2 = await this.cacheHandler.Contains("provision:reports:monthlyconsumption:k4:2014");

            Assert.IsFalse(r2);
        }
    }
}
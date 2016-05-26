namespace Provision.Tests
{
    using NUnit.Framework;
    using Provision.Interfaces;
    using Provision.Tests.Extensions;
    using Provision.Tests.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [TestFixture]
    public abstract class BaseCacheHandlerTests
    {
        public ICacheHandlerCollection CacheHandlers { get; set; }

        public string Prefix { get; private set; }

        public string Separator { get; private set; }

        [SetUp]
        public virtual void SetUp()
        {
            this.Prefix = "";
            this.Separator = "_";
        }

        [Test]
        public async void CreateKey_WhenGivenValidType_ShouldCreateValidKey()
        {
            var key = this.CacheHandlers.CreateKey<Report>("reports", "something", "k4", "2014");

            Assert.AreEqual($"{this.Prefix}reports{this.Separator}something{this.Separator}k4{this.Separator}2014", key);
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

            var key = this.CacheHandlers.CreateKey<Report>("report", "k4");

            await this.CacheHandlers.AddOrUpdate(key, d, DateTimeOffset.UtcNow.AddDays(1));

            var r = await this.CacheHandlers.Contains(key);

            Assert.IsTrue(r);
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

            var key = this.CacheHandlers.CreateKey<Report>("report", "k4");

            await this.CacheHandlers.AddOrUpdate(key, d, DateTimeOffset.UtcNow.AddDays(1), "report");

            var r = await this.CacheHandlers.Contains(key);

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

            var key = this.CacheHandlers.CreateKey<Report>("report", "k4");

            await this.CacheHandlers.AddOrUpdate(key, d, DateTimeOffset.UtcNow.AddDays(1), "report");

            await this.CacheHandlers.RemoveByTag("report");

            var r = await this.CacheHandlers.Contains(key);

            Assert.IsFalse(r);
        }

        [TestCase("report", "report2")]
        [TestCase("report#", "report2#")]
        public async void RemoveSpecificTag_WhenGivenValidTag_ShouldRemoveSpecificData(string k1, string k2)
        {
            var d = new Report()
            {
                Items = new List<ReportItem>()
                            {
                                new ReportItem() { Key = "1", Data = 100 }
                            }
            };

            var key1 = this.CacheHandlers.CreateKey<Report>(k1, "k4");

            await this.CacheHandlers.AddOrUpdate(key1, d, DateTimeOffset.UtcNow.AddDays(1), "tag1");

            var d2 = new Report()
            {
                Items = new List<ReportItem>()
                            {
                                new ReportItem() { Key = "1", Data = 100 }
                            }
            };

            var key2 = this.CacheHandlers.CreateKey<Report>(k2, "k4");

            await this.CacheHandlers.AddOrUpdate(key2, d2, DateTimeOffset.UtcNow.AddDays(1), "tag2");

            await this.CacheHandlers.RemoveByTag("tag1");

            var r = await this.CacheHandlers.Contains(key1);
            var r2 = await this.CacheHandlers.Contains(key2);

            Assert.IsFalse(r);
            Assert.IsTrue(r2);
        }

        [TestCase("reports", "blahblah", "k4", "2014")]
        [TestCase("reports", "blahblah2#", "k4", "2014")]
        public async void AddOrUpdate_WhenGivenValidData_ShouldInsertData(params string[] segments)
        {
            var d = new Report();

            var key = this.CacheHandlers.CreateKey<Report>(segments);

            await this.CacheHandlers.AddOrUpdate(key, d);

            var r = await this.CacheHandlers.Contains(key);

            Assert.IsTrue(r);
        }

        [TestCase("reports", "blahblah", "k4", "2014")]
        [TestCase("reports", "blahblah2#", "k4", "2014")]
        public async void AddOrUpdate_WhenUpdatingEntry_ShouldNotMutate(params string[] segments)
        {
            var d = new ReportItem() { Key = "Test1" };

            var key = this.CacheHandlers.CreateKey<Report>(segments);

            var obj1 = await this.CacheHandlers.AddOrUpdate(key, d);

            var obj2 = await this.CacheHandlers.GetValue<ReportItem>(key);

            var update = obj2.Clone();
            update.Key = "Test2";

            var obj3 = await this.CacheHandlers.AddOrUpdate(key, update);

            Assert.AreEqual(obj1.Key, obj2.Key, "The item added to the cache is not the same as was sent in");
            Assert.AreEqual(update.Key, obj3.Key, "The item updated in the cache is not the same as was sent in");
            Assert.AreNotEqual(obj2.Key, update.Key, "The item updated in the cache has mutated the earlier object");
        }

        [Test]
        public async void GetRaw_WhenGettingTwice_ShouldReturnSameData()
        {
            var d = new Report() { Rating = 4.00m };

            var key = this.CacheHandlers.CreateKey<Report>("report", "Get_WhenGettingTwice_ShouldReturnSameData");

            await this.CacheHandlers.AddOrUpdate(key, d, DateTimeOffset.UtcNow.AddMinutes(10));

            var r1 = await this.CacheHandlers.Get<Report>(key);
            var r2 = await this.CacheHandlers.Get<Report>(key);

            Console.WriteLine(r1.RawValue);
            Console.WriteLine(r2.RawValue);

            Assert.AreEqual(r1.RawValue.ToString(), r2.RawValue.ToString());
            Assert.AreEqual(d.Rating, r1.Value.Rating);
            Assert.AreEqual(d.Rating, r2.Value.Rating);
        }

        [TestCase("report", "k54")]
        [TestCase("reports#", "k54")]
        public async void Get_WhenGivenExistingKey_ShouldReturnItem(params string[] segments)
        {
            var d = new Report()
            {
                Items = new List<ReportItem>()
                            {
                                new ReportItem() { Key = "1", Data = 100 }
                            }
            };

            var key = this.CacheHandlers.CreateKey<Report>(segments);

            await this.CacheHandlers.AddOrUpdate(key, d, DateTimeOffset.UtcNow.AddDays(1));

            var r = await this.CacheHandlers.GetValue<Report>(key);

            Assert.IsNotNull(r);
            Assert.IsNotNull(r.Items);
            Assert.AreEqual(d.Items.First().Key, r.Items.First().Key);
        }

        [TestCase("report", "k84")]
        [TestCase("reporthash#", "k84")]
        public async void Get_WhenGivenExpiredKey_ShouldReturnNull(params string[] segments)
        {
            var d = new Report()
            {
                Items = new List<ReportItem>()
                            {
                                new ReportItem() { Key = "1", Data = 100 }
                            }
            };

            var key = this.CacheHandlers.CreateKey<Report>(segments);

            await this.CacheHandlers.AddOrUpdate(key, d, DateTimeOffset.UtcNow.AddDays(-1));

            var r = await this.CacheHandlers.Get<Report>(key);

            Assert.IsFalse(r.HasValue);
            Assert.IsNull(r.Value);
        }

        [TestCase("report", "k445697894231,3")]
        [TestCase("reports#", "k445697894231,3")]
        public async void Get_WhenGivenNonExistingKey_ShouldReturnNull(params string[] segments)
        {
            var k = this.CacheHandlers.CreateKey(segments);
            var r = await this.CacheHandlers.GetValue<Report>(k);

            Assert.IsNull(r);
        }

        [TestCase("reports", "training", "k5", "2014")]
        [TestCase("reports", "training", "_overall#", "k5", "2014")]
        public async void Remove_WhenGivenValidKey_ShouldRemoveData(params string[] segments)
        {
            var d = new Report();

            var key = this.CacheHandlers.CreateKey<Report>(segments);

            await this.CacheHandlers.AddOrUpdate(key, d);

            await this.CacheHandlers.RemoveByKey(key);

            var exists = await this.CacheHandlers.Contains(key);

            Assert.IsFalse(exists);
        }

        [Test]
        public async void Purge_WhenPurged_ShouldNotContainCacheItems()
        {
            var d = new Report();

            var key = this.CacheHandlers.CreateKey<Report>("reports", "football", "k4", "2014");

            await this.CacheHandlers.AddOrUpdate(key, d);

            var r1 = await this.CacheHandlers.Contains(key);

            Assert.IsTrue(r1);

            var p = await this.CacheHandlers.Purge();

            var r2 = await this.CacheHandlers.Contains(key);

            Assert.IsFalse(r2);
        }
    }
}
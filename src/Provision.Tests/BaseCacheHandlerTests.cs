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
        public ICacheHandler CacheHandler { get; set; }

        public string Prefix { get; private set; }

        public string Separator { get; private set; }

        [SetUp]
        public virtual void SetUp()
        {
            this.Prefix = !string.IsNullOrEmpty(this.CacheHandler.Configuration.Prefix) ? (this.CacheHandler.Configuration.Prefix + this.CacheHandler.Configuration.Separator) : "";
            this.Separator = this.CacheHandler.Configuration.Separator;
        }

        [Test]
        public async void CreateKey_WhenGivenValidType_ShouldCreateValidKey()
        {
            var key = this.CacheHandler.CreateKey<Report>("reports", "something", "k4", "2014");

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

            var key = this.CacheHandler.CreateKey<Report>("report", "k4");

            await this.CacheHandler.AddOrUpdate(key, d, DateTimeOffset.UtcNow.AddDays(1));

            var r = await this.CacheHandler.Contains(key);

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

            var key = this.CacheHandler.CreateKey<Report>("report", "k4");

            await this.CacheHandler.AddOrUpdate(key, d, DateTimeOffset.UtcNow.AddDays(1), "report");

            var r = await this.CacheHandler.Contains(key);

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

            var key = this.CacheHandler.CreateKey<Report>("report", "k4");

            await this.CacheHandler.AddOrUpdate(key, d, DateTimeOffset.UtcNow.AddDays(1), "report");

            await this.CacheHandler.RemoveByTag("report");

            var r = await this.CacheHandler.Contains(key);

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

            var key1 = this.CacheHandler.CreateKey<Report>("report", "k4");

            await this.CacheHandler.AddOrUpdate(key1, d, DateTimeOffset.UtcNow.AddDays(1), "report");

            var d2 = new Report()
            {
                Items = new List<ReportItem>()
                            {
                                new ReportItem() { Key = "1", Data = 100 }
                            }
            };

            var key2 = this.CacheHandler.CreateKey<Report>("report2", "k4");

            await this.CacheHandler.AddOrUpdate(key2, d2, DateTimeOffset.UtcNow.AddDays(1), "report2");

            await this.CacheHandler.RemoveByTag("report");

            var r = await this.CacheHandler.Contains(key1);
            var r2 = await this.CacheHandler.Contains(key2);

            Assert.IsFalse(r);
            Assert.IsTrue(r2);
        }

        [Test]
        public async void AddOrUpdate_WhenGivenValidData_ShouldInsertData()
        {
            var d = new Report();

            var key = this.CacheHandler.CreateKey<Report>("reports", "blahblah", "k4", "2014");

            await this.CacheHandler.AddOrUpdate(key, d);

            var r = await this.CacheHandler.Contains(key);

            Assert.IsTrue(r);
        }

        [Test]
        public async void AddOrUpdate_WhenUpdatingEntry_ShouldNotMutate()
        {
            var d = new ReportItem() { Key = "Test1" };

            var key = this.CacheHandler.CreateKey<Report>("reports", "blahblah", "k4", "2014");

            var obj1 = await this.CacheHandler.AddOrUpdate(key, d);

            var obj2 = await this.CacheHandler.GetValue<ReportItem>(key);

            var update = obj2.Clone();
            update.Key = "Test2";

            var obj3 = await this.CacheHandler.AddOrUpdate(key, update);

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

            var key = this.CacheHandler.CreateKey<Report>("report", "k54");

            await this.CacheHandler.AddOrUpdate(key, d, DateTimeOffset.UtcNow.AddDays(1));

            var r = await this.CacheHandler.GetValue<Report>(key);

            Assert.IsNotNull(r);
            Assert.IsNotNull(r.Items);
            Assert.AreEqual(d.Items.First().Key, r.Items.First().Key);
        }

        [TestCase("report", "k84")]
        [TestCase("reporthash", "#", "k84")]
        public async void Get_WhenGivenExpiredKey_ShouldReturnNull(params string[] segments)
        {
            var d = new Report()
            {
                Items = new List<ReportItem>()
                            {
                                new ReportItem() { Key = "1", Data = 100 }
                            }
            };

            var key = this.CacheHandler.CreateKey<Report>(segments);

            await this.CacheHandler.AddOrUpdate(key, d, DateTimeOffset.UtcNow.AddDays(-1));

            var r = await this.CacheHandler.Get<Report>(key);

            Assert.IsFalse(r.HasValue);
            Assert.IsNull(r.Value);
        }

        [Test]
        public async void Get_WhenGivenNonExistingKey_ShouldReturnNull()
        {
            var r = await this.CacheHandler.GetValue<Report>($"{this.Prefix}report{this.Separator}k445697894231,3");

            Assert.IsNull(r);
        }

        [Test]
        public async void Remove_WhenGivenValidKey_ShouldRemoveData()
        {
            var d = new Report();

            var key = this.CacheHandler.CreateKey<Report>("reports", "training", "k5", "2014");

            await this.CacheHandler.AddOrUpdate(key, d);

            await this.CacheHandler.RemoveByKey(key);

            var exists = await this.CacheHandler.Contains(key);

            Assert.IsFalse(exists);
        }

        [Test]
        public async void Remove_WhenGivenValidPattern_ShouldRemoveData()
        {
            var d = new Report();

            var key1 = this.CacheHandler.CreateKey<Report>("reports", "love", "ks", "2013");
            await this.CacheHandler.AddOrUpdate(key1, d, DateTimeOffset.UtcNow.AddMinutes(1));

            var key2 = this.CacheHandler.CreateKey<Report>("reports", "love", "ks", "2014");
            await this.CacheHandler.AddOrUpdate(key2, d, DateTimeOffset.UtcNow.AddMinutes(1));

            var key3 = this.CacheHandler.CreateKey<Report>("letter", "love", "ks", "2014");
            await this.CacheHandler.AddOrUpdate(key3, d, DateTimeOffset.UtcNow.AddMinutes(1));

            await this.CacheHandler.RemoveByPattern($"{this.Prefix}reports{this.Separator}love{this.Separator}ks{this.Separator}*");

            var exists1 = await this.CacheHandler.Contains(key1);
            var exists2 = await this.CacheHandler.Contains(key2);
            var exists3 = await this.CacheHandler.Contains(key3);

            Assert.IsFalse(exists1);
            Assert.IsFalse(exists2);
            Assert.IsTrue(exists3);
        }

        [Test]
        public async void Remove_WhenGivenValidRegex_ShouldRemoveData()
        {
            var d = new Report();

            var key1 = this.CacheHandler.CreateKey<Report>("reports", "love", "ks", "2013");
            await this.CacheHandler.AddOrUpdate(key1, d, DateTimeOffset.UtcNow.AddMinutes(1));

            var key2 = this.CacheHandler.CreateKey<Report>("reports", "love", "ks", "2014");
            await this.CacheHandler.AddOrUpdate(key2, d, DateTimeOffset.UtcNow.AddMinutes(1));

            var key3 = this.CacheHandler.CreateKey<Report>("letter", "love", "ks", "2014");
            await this.CacheHandler.AddOrUpdate(key3, d, DateTimeOffset.UtcNow.AddMinutes(1));

            await this.CacheHandler.RemoveByPattern($"{this.Prefix}reports{this.Separator}love{this.Separator}ks{this.Separator}*");

            var exists1 = await this.CacheHandler.Contains(key1);
            var exists2 = await this.CacheHandler.Contains(key2);
            var exists3 = await this.CacheHandler.Contains(key3);

            Assert.IsFalse(exists1);
            Assert.IsFalse(exists2);
            Assert.IsTrue(exists3);
        }

        [Test]
        public async void Purge_WhenPurged_ShouldNotContainCacheItems()
        {
            var d = new Report();

            var key = this.CacheHandler.CreateKey<Report>("reports", "football", "k4", "2014");

            await this.CacheHandler.AddOrUpdate(key, d);

            var r1 = await this.CacheHandler.Contains(key);

            Assert.IsTrue(r1);

            var p = await this.CacheHandler.Purge();

            var r2 = await this.CacheHandler.Contains(key);

            Assert.IsFalse(r2);
        }
    }
}
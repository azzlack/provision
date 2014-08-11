namespace Provision.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using NUnit.Framework;

    using Provision.Interfaces;
    using Provision.Providers.Redis;
    using Provision.Tests.Models;

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

            await this.cacheHandler.AddOrUpdate(key, d, DateTime.Now.AddDays(1));

            await Task.Delay(1000);

            var r = await this.cacheHandler.Contains("provision:xyz:1#k4");

            Assert.IsTrue(r);
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

            var r = await this.cacheHandler.Contains("provision:delivery:2#k4");

            Assert.IsTrue(r);
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
        public async void Get_WhenGivenValidHashSetKey_ShouldReturnData()
        {
            var d = new Report()
            {
                Items = new List<ReportItem>()
                            {
                                new ReportItem() { Key = "1", Data = 100 }
                            }
            };

            var key = string.Format("{0}#{1}", this.cacheHandler.CreateKey<Report>("fish", "11"), "k4");

            await this.cacheHandler.AddOrUpdate(key, d);

            await Task.Delay(1000);

            var r = await this.cacheHandler.Get<Report>(key);

            Assert.IsNotNull(r);
            Assert.AreEqual("1", r.Value.Items.First().Key);
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
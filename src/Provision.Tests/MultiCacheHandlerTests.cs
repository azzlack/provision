using System;
using Provision.Models;
using Provision.Providers.MemoryCache;

namespace Provision.Tests
{
    using NUnit.Framework;
    using Provision.Providers.Redis;

    public class MultiCacheHandlerTests : BaseCacheHandlerTests
    {
        public override void SetUp()
        {
            this.CacheHandlers = new CacheHandlerCollection(
                new MemoryCacheHandler(new MemoryCacheHandlerConfiguration(TimeSpan.FromSeconds(300))
                {
                    MaxMemory = 1000000
                }),
                new RedisCacheHandler(new RedisCacheHandlerConfiguration("localhost", 6379, 3, null, "provision", 512,
                    null, true))
                );

            base.SetUp();
        }
    }
}
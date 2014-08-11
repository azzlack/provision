namespace Provision.Providers.PortableMemoryCache
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;

    using Provision.Interfaces;
    using Provision.Models;
    using Provision.Providers.PortableMemoryCache.Mono;

    public class PortableMemoryCacheHandler : BaseCacheHandler
    {
        /// <summary>
        /// The cache
        /// </summary>
        private ConcurrentDictionary<string, object> cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortableMemoryCacheHandler"/> class.
        /// </summary>
        public PortableMemoryCacheHandler()
        {
            this.cache = new ConcurrentDictionary<string, object>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PortableMemoryCacheHandler"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public PortableMemoryCacheHandler(ICacheHandlerConfiguration configuration)
        {
            this.cache = new ConcurrentDictionary<string, object>();
        }

        /// <summary>
        /// Creates a cache item key from the specified segments.
        /// </summary>
        /// <param name="segments">The key segments.</param>
        /// <returns>A cache item key.</returns>
        public override string CreateKey<T>(params object[] segments)
        {
            return string.Format("{0}", string.Join("_", segments));
        }

        /// <summary>
        /// Checks if an item with the specified key exists in the cache.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if a cache item exists, <c>false</c> otherwise.</returns>
        public override async Task<bool> Contains<T>(string key)
        {
            return this.cache.ContainsKey(key);
        }

        /// <summary>
        /// Gets the cache item with specified key.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The cache item.</returns>
        public override async Task<ICacheItem<T>> Get<T>(string key)
        {
            try
            {
                var item = this.cache[key] as ICacheItem<T>;

                if (item != null)
                {
                    if (item.Expires == DateTime.MinValue)
                    {
                        return item;
                    }

                    if (item.Expires < DateTime.Now)
                    {
                        return CacheItem<T>.Empty(key);
                    }

                    return item;
                }

                return CacheItem<T>.Empty(key);
            }
            catch (KeyNotFoundException)
            {
                return CacheItem<T>.Empty(key);
            }
        }

        /// <summary>
        /// Gets the cache item value with the specified key.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The cache item.</returns>
        public override async Task<T> GetValue<T>(string key)
        {
            var item = await this.Get<T>(key);

            return item.HasValue ? item.Value : default(T);
        }

        /// <summary>
        /// Adds or updates a cache item with specified key and object.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <returns>A task.</returns>
        public override async Task<T> AddOrUpdate<T>(string key, T item)
        {
            return await this.AddOrUpdate(key, item, this.ExpireTime);
        }

        /// <summary>
        /// Sets a cache item with specified key and object.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <param name="expires">The expire time.</param>
        /// <returns>A task.</returns>
        public override async Task<T> AddOrUpdate<T>(string key, T item, DateTimeOffset expires)
        {
            if (item != null)
            {
                if (item is CacheItem<T>)
                {
                    var cacheItem = item as CacheItem<T>;

                    if (await this.Contains<T>(key))
                    {
                        await this.Remove<T>(key);
                    }

                    this.cache.TryAdd(key, cacheItem);
                }
                else
                {
                    this.cache.TryAdd(key, new CacheItem<T>(key, item, expires.DateTime));
                }

                if (typeof(IExpires).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo()))
                {
                    ((IExpires)item).Expires = expires.DateTime;
                }

                return item;
            }

            return default(T);
        }

        /// <summary>
        /// Removes the cache item with the specified key.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public override async Task<bool> Remove<T>(string key)
        {
            object existingValue;

            return this.cache.TryRemove(key, out existingValue);
        }

        /// <summary>
        /// Purges all cache items.
        /// </summary>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public override async Task<bool> Purge()
        {
            this.cache = new ConcurrentDictionary<string, object>();

            return true;
        }
    }
}
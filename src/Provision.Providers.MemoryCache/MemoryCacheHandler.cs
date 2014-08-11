namespace Provision.Providers.MemoryCache
{
    using System;
    using System.Runtime.Caching;
    using System.Threading.Tasks;

    using Provision.Interfaces;
    using Provision.Models;

    public class MemoryCacheHandler : BaseCacheHandler
    {
        /// <summary>
        /// The cache
        /// </summary>
        private MemoryCache cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryCacheHandler"/> class.
        /// </summary>
        public MemoryCacheHandler()
        {
            this.cache = MemoryCache.Default;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryCacheHandler"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public MemoryCacheHandler(ICacheHandlerConfiguration configuration)
            : base(configuration)
        {
            this.cache = MemoryCache.Default;
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
            return this.cache.Contains(key);
        }

        /// <summary>
        /// Gets the cache item with specified key.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The cache item.</returns>
        public override async Task<ICacheItem<T>> Get<T>(string key)
        {
            var item = this.cache.Get(key) as ICacheItem<T>;

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

                    this.cache.Set(key, new CacheItem<T>(cacheItem.Key, cacheItem.Value, cacheItem.Expires), expires);
                }
                else
                {
                    this.cache.Set(key, new CacheItem<T>(key, item, expires.DateTime), expires);
                }

                if (typeof(IExpires).IsAssignableFrom(typeof(T)))
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
            this.cache.Remove(key);

            return true;
        }

        /// <summary>
        /// Purges all cache items.
        /// </summary>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public override async Task<bool> Purge()
        {
            this.cache.Dispose();
            this.cache = MemoryCache.Default;

            return true;
        }
    }
}

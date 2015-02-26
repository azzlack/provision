namespace Provision.Providers.MemoryCache
{
    using System;
    using System.Runtime.Caching;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Provision.Extensions;
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
        public override string CreateKey(params object[] segments)
        {
            return string.Format("{0}", string.Join("_", segments));
        }

        /// <summary>
        /// Checks if an item with the specified key exists in the cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if a cache item exists, <c>false</c> otherwise.</returns>
        public override async Task<bool> Contains(string key)
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
                // Set expiry date if applicable
                item.MergeExpire();

                if (item.Expires.ToUniversalTime() == DateTime.MinValue)
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
                // Remove item before adding to prevent mutation of earlier calls to Get()
                if (this.cache.Contains(key))
                {
                    this.cache.Remove(key);
                }

                // Add item to cache
                if (item is CacheItem<T>)
                {
                    var cacheItem = item as CacheItem<T>;

                    this.cache.Set(key, new CacheItem<T>(cacheItem.Key, cacheItem.Value, cacheItem.Expires), expires);
                }
                else
                {
                    this.cache.Set(key, new CacheItem<T>(key, item, expires.DateTime), expires);
                }

                if (typeof(IExpires).IsAssignableFrom(typeof(T)) && expires.DateTime > DateTime.Now)
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
        /// <param name="key">The key.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public override async Task<bool> Remove(string key)
        {
            try
            {
                this.cache.Remove(key);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Removes all cache items matching the specified regular expression.
        /// </summary>
        /// <param name="regex">The regular expression.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public async override Task<bool> RemoveAll(Regex regex)
        {
            try
            {
                foreach (var item in cache)
                {
                    if (regex.Match(item.Key).Success)
                    {
                        await this.Remove(item.Key);
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
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

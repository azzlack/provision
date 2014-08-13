namespace Provision.Providers.PortableMemoryCache
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Provision.Extensions;
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
            : base(configuration)
        {
            this.cache = new ConcurrentDictionary<string, object>();
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
            catch (KeyNotFoundException)
            {
                return CacheItem<T>.Empty(key);
            }
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

                    if (await this.Contains(key))
                    {
                        await this.Remove(key);
                    }

                    this.cache.TryAdd(key, cacheItem);
                }
                else
                {
                    this.cache.TryAdd(key, new CacheItem<T>(key, item, expires.DateTime));
                }

                if (typeof(IExpires).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo()) && expires.DateTime > DateTime.Now)
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
            object existingValue;

            return this.cache.TryRemove(key, out existingValue);
        }

        /// <summary>
        /// Removes all cache items matching the specified regular expression.
        /// </summary>
        /// <param name="regex">The regular expression.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public override async Task<bool> RemoveAll(Regex regex)
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
            this.cache = new ConcurrentDictionary<string, object>();

            return true;
        }
    }
}
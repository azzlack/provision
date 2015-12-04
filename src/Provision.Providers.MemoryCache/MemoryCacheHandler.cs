namespace Provision.Providers.MemoryCache
{
    using Provision.Extensions;
    using Provision.Interfaces;
    using Provision.Models;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Caching;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class MemoryCacheHandler : BaseCacheHandler
    {
        /// <summary>The tags.</summary>
        private readonly ConcurrentDictionary<string, HashSet<string>> tags;

        /// <summary>The cache.</summary>
        private MemoryCache cache;

        /// <summary>Initializes a new instance of the <see cref="MemoryCacheHandler"/> class.</summary>
        public MemoryCacheHandler()
        {
            this.cache = new MemoryCache("Provision");
            this.tags = new ConcurrentDictionary<string, HashSet<string>>();
        }

        /// <summary>Initializes a new instance of the <see cref="MemoryCacheHandler"/> class.</summary>
        /// <param name="configuration">The configuration.</param>
        public MemoryCacheHandler(ICacheHandlerConfiguration configuration)
            : base(configuration)
        {
            this.cache = new MemoryCache("Provision");
            this.tags = new ConcurrentDictionary<string, HashSet<string>>();
        }

        /// <summary>Creates a cache item key from the specified segments.</summary>
        /// <exception cref="ArgumentException">Thrown when one or more arguments have unsupported or illegal values.</exception>
        /// <param name="segments">The key segments.</param>
        /// <returns>A cache item key.</returns>
        public override string CreateKey(params object[] segments)
        {
            // Throw exception if any segment is null
            if (segments.Any(x => x == null))
            {
                throw new ArgumentException("Cannot create key from null segments", nameof(segments));
            }

            return $"{string.Join(this.Configuration.Separator, segments)}";
        }

        /// <summary>Checks if an item with the specified key exists in the cache.</summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if a cache item exists, <c>false</c> otherwise.</returns>
        public override Task<bool> Contains(string key)
        {
            return Task.FromResult(this.cache.Contains(key));
        }

        /// <summary>Gets the cache item with specified key.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The cache item.</returns>
        public override Task<ICacheItem<T>> Get<T>(string key)
        {
            var item = this.cache.Get(key) as ICacheItem<T>;

            if (item != null)
            {
                // Set expiry date if applicable
                item.MergeExpire();

                if (item.Expires < DateTime.UtcNow)
                {
                    return Task.FromResult(CacheItem<T>.Empty(key));
                }

                return Task.FromResult(item);
            }

            return Task.FromResult(CacheItem<T>.Empty(key));
        }

        /// <summary>Sets a cache item with specified key and object.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <param name="expires">The expire time.</param>
        /// <param name="tags">The cache tags.</param>
        /// <returns>A task.</returns>
        public override Task<T> AddOrUpdate<T>(string key, T item, DateTimeOffset expires, params string[] tags)
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
                    this.cache.Set(key, new CacheItem<T>(key, item, expires.UtcDateTime), expires);
                }
                // Attach keys to tags
                foreach (var tag in tags)
                {
                    this.tags.AddOrUpdate(tag, new HashSet<string> { key },
                        (k, existingVal) =>
                    {
                        existingVal.Add(key);
                        return existingVal;
                    });
                }

                if (typeof(IExpires).IsAssignableFrom(typeof(T)) && expires.UtcDateTime > DateTime.UtcNow)
                {
                    ((IExpires)item).Expires = expires.UtcDateTime;
                }

                return Task.FromResult(item);
            }

            return Task.FromResult(default(T));
        }

        /// <summary>
        /// Removes the cache item with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public override Task<bool> RemoveByKey(string key)
        {
            try
            {
                this.cache.Remove(key);

                return Task.FromResult(true);
            }
            catch (Exception)
            {
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Removes all cache items matching the specified regular expression.
        /// </summary>
        /// <param name="regex">The regular expression.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public override async Task<bool> RemoveByPattern(Regex regex)
        {
            try
            {
                foreach (var item in this.cache)
                {
                    if (regex.Match(item.Key).Success)
                    {
                        await this.RemoveByKey(item.Key);
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
        /// Removes all cache items matching the specified tags.
        /// </summary>
        /// <param name="tags">The tags.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public override async Task<bool> RemoveByTag(params string[] tags)
        {
            try
            {
                foreach (var tag in tags)
                {
                    foreach (var key in this.tags[tag])
                    {
                        var itemRemoved = await this.RemoveByKey(key);
                        if (itemRemoved)
                        {
                            this.tags[tag].Remove(key);
                        }
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Purges all cache items.</summary>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public override Task<bool> Purge()
        {
            var oldCache = this.cache;
            this.cache = new MemoryCache("Provision");
            oldCache.Dispose();

            return Task.FromResult(true);
        }
    }
}

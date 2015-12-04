namespace Provision.Providers.PortableMemoryCache
{
    using Provision.Extensions;
    using Provision.Interfaces;
    using Provision.Models;
    using Provision.Providers.PortableMemoryCache.Mono;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class PortableMemoryCacheHandler : BaseCacheHandler
    {
        /// <summary>
        /// The cache
        /// </summary>
        private ConcurrentDictionary<string, object> cache;

        private readonly ConcurrentDictionary<string, HashSet<string>> cacheTags;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortableMemoryCacheHandler"/> class.
        /// </summary>
        public PortableMemoryCacheHandler()
        {
            this.cacheTags = new ConcurrentDictionary<string, HashSet<string>>();
            this.cache = new ConcurrentDictionary<string, object>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PortableMemoryCacheHandler"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public PortableMemoryCacheHandler(ICacheHandlerConfiguration configuration)
            : base(configuration)
        {
            this.cacheTags = new ConcurrentDictionary<string, HashSet<string>>();
            this.cache = new ConcurrentDictionary<string, object>();
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

                    if (item.Expires < DateTime.UtcNow)
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
        /// Sets a cache item with specified key and object.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <param name="expires">The expire time.</param>
        /// <param name="tags">The tags.</param>
        /// <returns>A task.</returns>
        public override async Task<T> AddOrUpdate<T>(string key, T item, DateTimeOffset expires, params string[] tags)
        {
            if (item != null)
            {
                if (item is CacheItem<T>)
                {
                    var cacheItem = item as CacheItem<T>;

                    if (await this.Contains(key))
                    {
                        await this.RemoveByKey(key);
                    }

                    this.cache.TryAdd(key, cacheItem);
                }
                else
                {
                    this.cache.TryAdd(key, new CacheItem<T>(key, item, expires.UtcDateTime));
                }
                // Attach keys to tags
                foreach (var tag in tags)
                {
                    this.cacheTags.AddOrUpdate(tag, new HashSet<string> { key },
                        (k, existingVal) =>
                        {
                            existingVal.Add(key);
                            return existingVal;
                        });
                }

                if (typeof(IExpires).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo()) && expires.UtcDateTime > DateTime.UtcNow)
                {
                    ((IExpires)item).Expires = expires.UtcDateTime;
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
        public override async Task<bool> RemoveByKey(string key)
        {
            object existingValue;

            return this.cache.TryRemove(key, out existingValue);
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
                foreach (var item in cache)
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
        public async override Task<bool> RemoveByTag(params string[] tags)
        {
            try
            {
                foreach (var tag in tags)
                {
                    foreach (var key in this.cacheTags[tag])
                    {
                        var itemRemoved = await this.RemoveByKey(key);
                        if (itemRemoved)
                        {
                            this.cacheTags[tag].Remove(key);
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
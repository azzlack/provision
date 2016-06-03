using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Provision.Interfaces;

namespace Provision.Models
{
    public class CacheHandlerCollection : ICacheHandlerCollection
    {
        /// <summary>The cache handlers.</summary>
        private readonly IEnumerable<ICacheHandler> cacheHandlers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheHandlerCollection" /> class.
        /// </summary>
        /// <param name="cacheHandlers">
        /// A variable-length parameters list containing cache handlers.
        /// </param>
        public CacheHandlerCollection(params ICacheHandler[] cacheHandlers)
        {
            this.cacheHandlers = cacheHandlers;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheHandlerCollection" /> class.
        /// </summary>
        /// <param name="cacheHandlers">
        /// A variable-length parameters list containing cache handlers.
        /// </param>
        public CacheHandlerCollection(IEnumerable<ICacheHandler> cacheHandlers)
        {
            this.cacheHandlers = cacheHandlers;
        }

        /// <summary>Gets the cache handler at the specified index.</summary>
        /// <param name="index">The index.</param>
        /// <returns>The cache handler if it exists.</returns>
        public ICacheHandler this[int index] => this.cacheHandlers.ElementAtOrDefault(index);

        /// <summary>Gets the cache handler with the specified name.</summary>
        /// <param name="name">The name.</param>
        /// <returns>The cache handler if it exists.</returns>
        public ICacheHandler this[string name]
        {
            get { return this.cacheHandlers.FirstOrDefault(x => x.Configuration.Name == name); }
        }

        /// <summary>Creates a cache item key from the specified segments.</summary>
        /// <typeparam name="T">The type to create a cache key for.</typeparam>
        /// <param name="segments">The key segments.</param>
        /// <returns>A cache item key.</returns>
        public string CreateKey<T>(params object[] segments)
        {
            return this.CreateKey(segments);
        }

        /// <summary>Creates a cache item key from the specified segments.</summary>
        /// <param name="segments">The key segments.</param>
        /// <returns>A cache item key.</returns>
        public string CreateKey(params object[] segments)
        {
            // Throw exception if any segment is null
            if (segments.Any(x => x == null))
            {
                throw new ArgumentException("Cannot create key from null segments", nameof(segments));
            }
            
            // Throw exception if any segment is underscore
            if (segments.Any(x => x.ToString() == "_"))
            {
                throw new ArgumentException("Cannot create key containing underscores ( _ )", nameof(segments));
            }

            return string.Join("_", segments);
        }

        /// <summary>Checks if an item with the specified key exists in the cache.</summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if a cache item exists, <c>false</c> otherwise.</returns>
        public async Task<bool> Contains(string key)
        {
            foreach (var handler in this.cacheHandlers)
            {
                var k = handler.CreateKey(key.Split('_'));
                if (await handler.Contains(k))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Gets the cache item with specified key.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The cache item.</returns>
        public async Task<ICacheItem<T>> Get<T>(string key)
        {
            foreach (var handler in this.cacheHandlers)
            {
                var k = handler.CreateKey<T>(key.Split('_'));
                var cacheItem = await handler.Get<T>(k);

                if (cacheItem.HasValue)
                {
                    return cacheItem;
                }
            }

            return CacheItem<T>.Empty(key);
        }

        /// <summary>Gets the cache item with specified tags.</summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="tags">The tags.</param>
        /// <returns>The cache items.</returns>
        public async Task<IEnumerable<ICacheItem<T>>> GetByTag<T>(params string[] tags)
        {
            var cacheItems = new List<ICacheItem<T>>();

            foreach (var handler in this.cacheHandlers)
            {
                var c = await handler.GetByTag<T>(tags);

                cacheItems.AddRange(c);
            }

            return cacheItems;
        }

        /// <summary>Gets the cache item with the specified key and casts it to the specified cache item wrapper.</summary>
        /// <typeparam name="TWrapper">The cache item wrapper type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The cache item.</returns>
        public async Task<TWrapper> GetAs<TWrapper, TValue>(string key) where TWrapper : ICacheItem<TValue>
        {
            foreach (var handler in this.cacheHandlers)
            {
                var k = handler.CreateKey<TValue>(key.Split('_'));
                var cacheItem = await handler.GetAs<TWrapper, TValue>(k);

                if (cacheItem.HasValue)
                {
                    return cacheItem;
                }
            }

            return default(TWrapper);
        }

        /// <summary>Gets the cache item value with the specified key.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The cache item.</returns>
        public async Task<T> GetValue<T>(string key)
        {
            foreach (var handler in this.cacheHandlers)
            {
                var k = handler.CreateKey<T>(key.Split('_'));
                var c = await handler.GetValue<T>(k);

                if (c != null)
                {
                    return c;
                }
            }

            return default(T);
        }

        /// <summary>Adds or updates a cache item with specified key and object.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <param name="tags">The tags.</param>
        /// <returns>A task.</returns>
        public async Task<T> AddOrUpdate<T>(string key, T item, params string[] tags)
        {
            foreach (var handler in this.cacheHandlers)
            {
                var k = handler.CreateKey<T>(key.Split('_'));
                var c = await handler.AddOrUpdate(k, item, tags);

                if (c != null)
                {
                    return c;
                }
            }

            return default(T);
        }

        /// <summary>Adds or updates a cache item with specified key and object. Can optionally tag an item for grouping similar keys.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <param name="expires">The expire time.</param>
        /// <param name="tags">The tags.</param>
        /// <returns>The added or updated value.</returns>
        public async Task<T> AddOrUpdate<T>(string key, T item, DateTimeOffset expires, params string[] tags)
        {
            foreach (var handler in this.cacheHandlers)
            {
                var k = handler.CreateKey<T>(key.Split('_'));
                var c = await handler.AddOrUpdate(k, item, expires, tags);

                if (c != null)
                {
                    return c;
                }
            }

            return default(T);
        }

        /// <summary>Removes the cache item with the specified key.</summary>
        /// <param name="key">The key.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public async Task<bool> RemoveByKey(string key)
        {
            var result = false;

            foreach (var handler in this.cacheHandlers)
            {
                var k = handler.CreateKey(key.Split('_'));
                result = await handler.RemoveByKey(k);
            }

            return result;
        }

        /// <summary>Removes all cache items matching the specified tags.</summary>
        /// <param name="tags">The tags.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public async Task<bool> RemoveByTag(params string[] tags)
        {
            var result = false;

            foreach (var handler in this.cacheHandlers)
            {
                result = await handler.RemoveByTag(tags);
            }

            return result;
        }

        /// <summary>Purges all cache items.</summary>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public async Task<bool> Purge()
        {
            var result = false;

            foreach (var handler in this.cacheHandlers)
            {
                result = await handler.Purge();
            }

            return result;
        }

        public IEnumerator<ICacheHandler> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
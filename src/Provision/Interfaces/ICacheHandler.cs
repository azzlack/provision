namespace Provision.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for cache handlers
    /// </summary>
    public interface ICacheHandler
    {
        /// <summary>Gets or sets the configuration.</summary>
        /// <value>The configuration.</value>
        ICacheHandlerConfiguration Configuration { get; set; }

        /// <summary>Creates a cache item key from the specified segments.</summary>
        /// <typeparam name="T">The type to create a cache key for.</typeparam>
        /// <param name="segments">The key segments.</param>
        /// <returns>A cache item key.</returns>
        string CreateKey<T>(params object[] segments);

        /// <summary>Creates a cache item key from the specified segments.</summary>
        /// <param name="segments">The key segments.</param>
        /// <returns>A cache item key.</returns>
        string CreateKey(params object[] segments);

        /// <summary>Checks if an item with the specified key exists in the cache.</summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if a cache item exists, <c>false</c> otherwise.</returns>
        Task<bool> Contains(string key);

        /// <summary>Gets the cache item with specified key.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The cache item.</returns>
        Task<ICacheItem<T>> Get<T>(string key);

        /// <summary>Gets the cache item with specified tags.</summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="tags">The tags.</param>
        /// <returns>The cache items.</returns>
        Task<IEnumerable<ICacheItem<T>>> GetByTag<T>(params string[] tags);

        /// <summary>Gets the cache item with the specified key and casts it to the specified cache item wrapper.</summary>
        /// <typeparam name="TWrapper">The cache item wrapper type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The cache item.</returns>
        Task<TWrapper> GetAs<TWrapper, TValue>(string key) where TWrapper : ICacheItem<TValue>;

        /// <summary>Gets the cache item value with the specified key.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The cache item.</returns>
        Task<T> GetValue<T>(string key);

        /// <summary>Adds or updates a cache item with specified key and object.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <param name="tags">The tags.</param>
        /// <returns>A task.</returns>
        Task<T> AddOrUpdate<T>(string key, T item, params string[] tags);

        /// <summary>Adds or updates a cache item with specified key and object. Can optionally tag an item for grouping similar keys.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <param name="expires">The expire time.</param>
        /// <param name="tags">The tags.</param>
        /// <returns>The added or updated value.</returns>
        Task<T> AddOrUpdate<T>(string key, T item, DateTimeOffset expires, params string[] tags);

        /// <summary>Removes the cache item with the specified key.</summary>
        /// <param name="key">The key.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        Task<bool> RemoveByKey(string key);

        /// <summary>Removes all cache items matching the specified pattern.</summary>
        /// <param name="pattern">The pattern, in wildcard format.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        Task<bool> RemoveByPattern(string pattern);

        /// <summary>Removes all cache items matching the specified pattern.</summary>
        /// <param name="pattern">The pattern, in regular expression format.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        Task<bool> RemoveByPattern(Regex pattern);

        /// <summary>Removes all cache items matching the specified tags.</summary>
        /// <param name="tags">The tags.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        Task<bool> RemoveByTag(params string[] tags);

        /// <summary>Purges all cache items.</summary>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        Task<bool> Purge();
    }
}
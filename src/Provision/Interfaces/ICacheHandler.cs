namespace Provision.Interfaces
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for cache handlers
    /// </summary>
    public interface ICacheHandler
    {
        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        ICacheHandlerConfiguration Configuration { get; set; }

        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        //ICacheHandlerProviderConfiguration Configuration { get; set; }

        /// <summary>
        /// Creates a cache item key from the specified segments.
        /// </summary>
        /// <typeparam name="T">The type to create a cache key for.</typeparam>
        /// <param name="segments">The key segments.</param>
        /// <returns>A cache item key.</returns>
        string CreateKey<T>(params object[] segments);

        /// <summary>
        /// Checks if an item with the specified key exists in the cache.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if a cache item exists, <c>false</c> otherwise.</returns>
        Task<bool> Contains<T>(string key);

        /// <summary>
        /// Gets the cache item with specified key.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The cache item.</returns>
        Task<ICacheItem<T>> Get<T>(string key);

        /// <summary>
        /// Gets the cache item value with the specified key.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The cache item.</returns>
        Task<T> GetValue<T>(string key);

        /// <summary>
        /// Adds or updates a cache item with specified key and object.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <returns>A task.</returns>
        Task<T> AddOrUpdate<T>(string key, T item);

        /// <summary>
        /// Adds or updates a cache item with specified key and object.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <param name="expires">The expire time.</param>
        /// <returns>The added or updated value.</returns>
        Task<T> AddOrUpdate<T>(string key, T item, DateTimeOffset expires);

        /// <summary>
        /// Removes the cache item with the specified key.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        Task<bool> Remove<T>(string key);

        /// <summary>
        /// Purges all cache items.
        /// </summary>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        Task<bool> Purge();
    }
}
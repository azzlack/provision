namespace Provision.Models
{
    using Provision.Interfaces;
    using System;

    /// <summary>
    /// A typed cache item.
    /// </summary>
    /// <typeparam name="T">The data type</typeparam>
    public class CacheItem<T> : ICacheItem<T>
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="CacheItem{T}" /> class from being created.
        /// </summary>
        /// <param name="key">The key.</param>
        private CacheItem(string key = "")
        {
            this.Key = key;
            this.Expires = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem{T}" /> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <param name="expires">The expire time.</param>
        public CacheItem(string key, T item, DateTimeOffset expires)
        {
            this.Key = key;
            this.Expires = expires;
            this.Value = item;
            this.RawValue = item;
        }

        /// <summary>Initializes a new instance of the <see cref="CacheItem{T}" /> class.</summary>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <param name="raw">The raw.</param>
        /// <param name="expires">The expire time.</param>
        public CacheItem(string key, T item, object raw, DateTimeOffset expires)
        {
            this.Key = key;
            this.Expires = expires;
            this.Value = item;
            this.RawValue = raw;
        }

        /// <summary>
        /// Gets or sets the expires.
        /// </summary>
        /// <value>The expires.</value>
        public DateTimeOffset Expires { get; set; }

        /// <summary>Gets or sets the cache handler name.</summary>
        /// <value>The cache handler name.</value>
        public string CacheHandler { get; set; }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>The key.</value>
        public string Key { get; private set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public T Value { get; private set; }

        /// <summary>Gets the raw value.</summary>
        /// <value>The raw value.</value>
        public object RawValue { get; private set; }

        /// <summary>Creates an empty cache item with the specified key.</summary>
        /// <param name="key">The key.</param>
        /// <returns>The cache item.</returns>
        public static ICacheItem<T> Empty(string key)
        {
            return new CacheItem<T>(key);
        }

        /// <summary>Gets a value indicating whether this instance has value.</summary>
        /// <value><c>true</c> if this instance has value; otherwise, <c>false</c>.</value>
        public bool HasValue => this.Value != null;

        /// <summary>Initializes the specified value.</summary>
        /// <param name="value">The value.</param>
        /// <param name="expires">The expires.</param>
        public void Initialize(T value, DateTimeOffset expires)
        {
            this.Value = value;
            this.RawValue = value;
            this.Expires = expires;
        }
    }
}
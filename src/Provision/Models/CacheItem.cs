namespace Provision.Models
{
    using System;

    using Provision.Interfaces;

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
            this.Expires = DateTime.Now;
        }
 
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem{T}" /> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <param name="expires">The expire time.</param>
        public CacheItem(string key, T item, DateTime expires)
        {
            this.Key = key;
            this.Expires = expires;
            this.Value = item;
        }

        /// <summary>
        /// Gets or sets the expires.
        /// </summary>
        /// <value>The expires.</value>
        public DateTime Expires { get; set; }

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

        /// <summary>
        /// Gets a value indicating whether this instance has value.
        /// </summary>
        /// <value><c>true</c> if this instance has value; otherwise, <c>false</c>.</value>
        public bool HasValue
        {
            get
            {
                return this.Value != null;
            }
        }

        /// <summary>
        /// Initializes the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="expires">The expires.</param>
        public void Initialize(T value, DateTime expires)
        {
            this.Value = value;
            this.Expires = expires;
        }

        /// <summary>
        /// Creates an empty cache item with the specified key.
        /// </summary>
        /// <returns>The cache item.</returns>
        public static CacheItem<T> Empty(string key)
        {
            return new CacheItem<T>(key);
        }
    }
}
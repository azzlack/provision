namespace Provision.Models
{
    using System;
    using System.Threading.Tasks;

    using Provision.Interfaces;
    using Provision.Quartz;

    public class BaseCacheHandler : ICacheHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCacheHandler"/> class.
        /// </summary>
        public BaseCacheHandler()
        {
            this.Configuration = new BaseCacheHandlerConfiguration();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCacheHandler"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public BaseCacheHandler(ICacheHandlerConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        /// <summary>
        /// Gets the expire time.
        /// </summary>
        /// <returns>A date/time offset. Defaults to 1 minute.</returns>
        public DateTimeOffset ExpireTime
        {
            get
            {
                var cron = new CronExpression(this.Configuration.ExpireTime);

                var expires = cron.GetNextValidTimeAfter(DateTime.Now) ?? new DateTimeOffset(DateTime.Now, new TimeSpan(0, 0, 1, 0));

                return expires;
            }
        }

        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        public ICacheHandlerConfiguration Configuration { get; set; }

        /// <summary>
        /// Creates a cache item key from the specified segments.
        /// </summary>
        /// <typeparam name="T">The type to create a cache key for.</typeparam>
        /// <param name="segments">The key segments.</param>
        /// <returns>A cache item key.</returns>
        public virtual string CreateKey<T>(params object[] segments)
        {
            return string.Join("", segments);
        }

        /// <summary>
        /// Checks if an item with the specified key exists in the cache.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if a cache item exists, <c>false</c> otherwise.</returns>
        public virtual async Task<bool> Contains<T>(string key)
        {
            return false;
        }

        /// <summary>
        /// Gets the cache item with specified key.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The cache item.</returns>
        public virtual async Task<ICacheItem<T>> Get<T>(string key)
        {
            return null;
        }

        /// <summary>
        /// Gets the cache item value with the specified key.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The cache item.</returns>
        public virtual async Task<T> GetValue<T>(string key)
        {
            return default(T);
        }

        /// <summary>
        /// Adds or updates a cache item with specified key and object.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <returns>A task.</returns>
        public virtual async Task<T> AddOrUpdate<T>(string key, T item)
        {
            return item;
        }

        /// <summary>
        /// Adds or updates a cache item with specified key and object.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <param name="expires">The expire time.</param>
        /// <returns>A task.</returns>
        public virtual async Task<T> AddOrUpdate<T>(string key, T item, DateTimeOffset expires)
        {
            return item;
        }

        /// <summary>
        /// Removes the cache item with the specified key.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public virtual async Task<bool> Remove<T>(string key)
        {
            return false;
        }

        /// <summary>
        /// Purges all cache items.
        /// </summary>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public virtual async Task<bool> Purge()
        {
            return false;
        }
    }
}
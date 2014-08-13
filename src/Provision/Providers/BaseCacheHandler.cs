namespace Provision.Providers
{
    using System;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Provision.Extensions;
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
            return this.CreateKey(segments);
        }

        /// <summary>
        /// Creates a cache item key from the specified segments.
        /// </summary>
        /// <param name="segments">The key segments.</param>
        /// <returns>A cache item key.</returns>
        public virtual string CreateKey(params object[] segments)
        {
            return string.Join("", segments);
        }

        /// <summary>
        /// Checks if an item with the specified key exists in the cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if a cache item exists, <c>false</c> otherwise.</returns>
        public virtual async Task<bool> Contains(string key)
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
        /// Gets the cache item with the specified key and casts it to the specified cache item wrapper.
        /// </summary>
        /// <typeparam name="TWrapper">The cache item wrapper type.</typeparam>
        /// <typeparam name="TValue">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The cache item.</returns>
        public virtual async Task<TWrapper> GetAs<TWrapper, TValue>(string key) where TWrapper : ICacheItem<TValue>
        {
            var item = await this.Get<TValue>(key);

            if (item != null && item.HasValue)
            {
                // Set expiry date if applicable
                item.MergeExpire();
                
                var p = (ICacheItem<TValue>)Activator.CreateInstance<TWrapper>();
                p.Initialize(item.Value, item.Expires);

                return (TWrapper)p;
            }

            return default(TWrapper);
        }

        /// <summary>
        /// Gets the cache item value with the specified key.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The cache item.</returns>
        public virtual async Task<T> GetValue<T>(string key)
        {
            var item = await this.Get<T>(key);

            if (item != null && item.HasValue)
            {
                // Set expiry date if applicable
                item.MergeExpire();

                return item.Value;
            }

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
        /// <param name="key">The key.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public virtual async Task<bool> Remove(string key)
        {
            return false;
        }

        /// <summary>
        /// Removes all cache items matching the specified pattern.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public virtual async Task<bool> RemoveAll(string pattern)
        {
            return await this.RemoveAll(new Regex(pattern));
        }

        /// <summary>
        /// Removes all cache items matching the specified regular expression.
        /// </summary>
        /// <param name="regex">The regular expression.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public virtual async Task<bool> RemoveAll(Regex regex)
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
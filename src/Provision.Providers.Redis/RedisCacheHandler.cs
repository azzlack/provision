namespace Provision.Providers.Redis
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Common.Logging;

    using MsgPack.Serialization;

    using Provision.Interfaces;
    using Provision.Models;

    using ServiceStack.Redis;
    using ServiceStack.Text;

    /// <summary>
    /// The redis cache handler.
    /// </summary>
    public class RedisCacheHandler : BaseCacheHandler
    {
        /// <summary>
        /// The log
        /// </summary>
        private readonly ILog log;

        /// <summary>
        /// The configuration
        /// </summary>
        private readonly RedisCacheHandlerConfiguration configuration;

        /// <summary>
        /// The client manager
        /// </summary>
        private readonly IRedisClientsManager clientManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCacheHandler" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public RedisCacheHandler(RedisCacheHandlerConfiguration configuration)
            : base(configuration)
        {
            this.configuration = configuration;

            this.log = !string.IsNullOrEmpty(this.configuration.LoggerName)
                           ? LogManager.GetLogger(this.configuration.LoggerName)
                           : LogManager.GetLogger(typeof(RedisCacheHandler));

            if (!string.IsNullOrEmpty(this.configuration.Password))
            {
                this.clientManager = new PooledRedisClientManager(configuration.Database, string.Format("{0}@{1}:{2}", this.configuration.Password, this.configuration.Host, this.configuration.Port));
            }
            else
            {
                this.clientManager = new PooledRedisClientManager(configuration.Database, string.Format("{0}:{1}", this.configuration.Host, this.configuration.Port));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCacheHandler" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="log">The log.</param>
        public RedisCacheHandler(RedisCacheHandlerConfiguration configuration, ILog log)
            : base(configuration)
        {
            this.configuration = configuration;
            this.log = log;

            if (!string.IsNullOrEmpty(this.configuration.Password))
            {
                this.clientManager = new PooledRedisClientManager(this.configuration.Database, string.Format("{0}@{1}:{2}", this.configuration.Password, configuration.Host, configuration.Port));
            }
            else
            {
                this.clientManager = new PooledRedisClientManager(this.configuration.Database, string.Format("{0}:{1}", configuration.Host, configuration.Port));
            }
        }

        /// <summary>
        /// Creates a cache item key from the specified segments.
        /// </summary>
        /// <typeparam name="T">The type to create a cache key for.</typeparam>
        /// <param name="segments">The key segments.</param>
        /// <returns>A cache item key.</returns>
        public override string CreateKey(params object[] segments)
        {
            var key = string.Format("{0}:{1}", this.configuration.Prefix, string.Join(":", segments.Select(x => x.ToString().Replace(':', '-'))));

            if (string.IsNullOrEmpty(this.configuration.Prefix))
            {
                key = string.Format("{0}", string.Join(":", segments.Select(x => x.ToString().Replace(':', '-'))));
            }

            return key;
        }

        /// <summary>
        /// Checks if an item with the specified key exists in the cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if a cache item exists, <c>false</c> otherwise.</returns>
        public override async Task<bool> Contains(string key)
        {
            var start = DateTime.Now;

            using (var client = this.clientManager.GetClient())
            {
                var exists = false;

                try
                {
                    if (key.Contains("#"))
                    {
                        var h = key.Split('#')[0];
                        var k = key.Split('#')[1];

                        exists = client.HashContainsEntry(h, k);
                    }
                    else
                    {
                        exists = client.ContainsKey(key);
                    }

                    if (!exists)
                    {
                        this.log.WarnFormat("Couldn't find cache item with key '{0}'", key);
                    }
                    else
                    {
                        this.log.DebugFormat("Found cache item with key '{0}'", key);
                    }
                }
                catch (Exception ex)
                {
                    this.log.Error(string.Format("Error when getting value '{0}' from database", key), ex);
                }

                this.log.InfoFormat("RedisCacheHandler.Contains({1}) Time: {0}s", DateTime.Now.Subtract(start).TotalSeconds, key);

                return exists;
            }
        }

        /// <summary>
        /// Gets the cache item with specified key.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The cache item.</returns>
        public override async Task<ICacheItem<T>> Get<T>(string key)
        {
            var start = DateTime.Now;

            try
            {
                using (var client = this.clientManager.GetClient())
                {
                    this.log.DebugFormat("Trying to get cache item with key '{0}' and type '{1}'", key, typeof(T));

                    try
                    {
                        byte[] data;
                        var expires = DateTime.MinValue;

                        var serializer = SerializationContext.Default.GetSerializer<T>();

                        if (key.Contains("#"))
                        {
                            var h = key.Split('#')[0];
                            var k = key.Split('#')[1];

                            // Get data
                            data = ((IRedisNativeClient)client).HGet(h, Encoding.UTF8.GetBytes(k));

                            // Get expire time
                            var ttl = client.GetTimeToLive(h);
                            if (ttl.TotalSeconds > 0)
                            {
                                expires = DateTime.Now.Add(ttl);
                            }
                        }
                        else
                        {
                            // Get data
                            data = ((IRedisNativeClient)client).Get(key);

                            // Get expire time
                            var ttl = client.GetTimeToLive(key);
                            if (ttl.TotalSeconds > 0)
                            {
                                expires = DateTime.Now.Add(ttl);
                            }
                        }

                        // Convert data to typed result
                        T result;

                        // If compression is specified, decompress result
                        if (this.configuration.Compress)
                        {
                            result = serializer.UnpackSingleObject(data);
                        }
                        else
                        {
                            result = JsonSerializer.DeserializeFromString<T>(Encoding.UTF8.GetString(data));
                        }

                        if (result == null)
                        {
                            this.log.WarnFormat("Couldn't retrieve cache item with key '{0}' and type '{1}'", key, typeof(T));
                        }
                        else
                        {
                            this.log.DebugFormat("Retrieved cache item with key '{0}' and type '{1}'. It expires at {2}", key, typeof(T), expires);
                            this.log.InfoFormat("RedisCacheHandler.GetCacheItem<{2}>({1}) Time: {0}s", DateTime.Now.Subtract(start).TotalSeconds, key, typeof(T));

                            // Set expiry date if applicable
                            if (typeof(IExpires).IsAssignableFrom(typeof(T)))
                            {
                                ((IExpires)result).Expires = expires;
                            }

                            return new CacheItem<T>(key, result, expires);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.log.Error(string.Format("Error when getting value '{0}' and type '{1}' from database", key, typeof(T)), ex);
                    }
                }
            }
            catch (Exception ex)
            {
                this.log.Error("Error when connecting to database", ex);
            }

            this.log.InfoFormat("RedisCacheHandler.GetCacheItem<{2}>({1}) Time: {0}s (Error)", DateTime.Now.Subtract(start).TotalSeconds, key, typeof(T));

            return null;
        }

        /// <summary>
        /// Gets the cache item value with the specified key.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The cache item.</returns>
        public override async Task<T> GetValue<T>(string key)
        {
            var item = await this.Get<T>(key);

            if (item.HasValue)
            {
                if (typeof(IExpires).IsAssignableFrom(typeof(T)))
                {
                    ((IExpires)item.Value).Expires = item.Expires;
                }

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
        public async override Task<T> AddOrUpdate<T>(string key, T item)
        {
            return await this.AddOrUpdate(key, item, this.ExpireTime.DateTime);
        }

        /// <summary>
        /// Adds or updates a cache item with specified key and object.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <param name="expires">The expire time.</param>
        /// <returns>A task.</returns>
        public override async Task<T> AddOrUpdate<T>(string key, T item, DateTimeOffset expires)
        {
            var start = DateTime.Now;

            if (item != null)
            {
                try
                {
                    using (var client = this.clientManager.GetClient())
                    {
                        try
                        {
                            var inserted = true;

                            var serializer = SerializationContext.Default.GetSerializer<T>();

                            byte[] data;
                            
                            // If compression is specified, compress data before inserting into database
                            if (this.configuration.Compress)
                            {
                                data = serializer.PackSingleObject(item);
                            }
                            else
                            {
                                data = Encoding.UTF8.GetBytes(JsonSerializer.SerializeToString(item));
                            }

                            if (key.Contains("#"))
                            {
                                var h = key.Split('#')[0];
                                var k = key.Split('#')[1];

                                // Add data to db
                                inserted = ((IRedisNativeClient)client).HSet(h, Encoding.UTF8.GetBytes(k), data) == 1;

                                // Make item expire if not already set
                                if (expires.DateTime > DateTime.Now)
                                {
                                    var ttl = client.GetTimeToLive(h);
                                    if (ttl.TotalSeconds <= 0)
                                    {
                                        client.ExpireEntryAt(h, expires.DateTime);
                                    }
                                }
                            }
                            else
                            {
                                // Add data to db
                                ((IRedisNativeClient)client).Set(key, data);

                                // Make item expire
                                if (expires.DateTime > DateTime.Now)
                                {
                                    client.ExpireEntryAt(key, expires.DateTime);
                                }
                            }

                            this.log.DebugFormat(
                                inserted
                                    ? "Successfully inserted cache item with key '{0}'"
                                    : "Successfully updated cache item with key '{0}'",
                                key);

                            // Set expires time if item is expireable
                            if (typeof(IExpires).IsAssignableFrom(typeof(T)) && expires.DateTime > DateTime.Now)
                            {
                                ((IExpires)item).Expires = expires.DateTime;
                            }

                            this.log.InfoFormat(
                                "RedisCacheHandler.AddOrUpdate<{2}>({1}, {2}, {3}) Time: {0}s",
                                DateTime.Now.Subtract(start).TotalSeconds,
                                key,
                                typeof(T),
                                expires);

                            return item;
                        }
                        catch (Exception ex)
                        {
                            this.log.Error(string.Format("Error when inserting value '{0}' for key '{1}' to database", item, key), ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.log.Error("Error when connecting to database", ex);
                }
            }

            this.log.InfoFormat(
                "RedisCacheHandler.AddOrUpdate<{2}>({1}, {2}, {3}) Time: {0}s (Error)",
                DateTime.Now.Subtract(start).TotalSeconds,
                key,
                typeof(T),
                expires);

            return item;
        }

        /// <summary>
        /// Removes the cache item with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        /// <exception cref="System.ArgumentException">Error when removing key from database</exception>
        public override async Task<bool> Remove(string key)
        {
            var start = DateTime.Now;

            var removed = false;

            try
            {
                using (var client = this.clientManager.GetClient())
                {
                    this.log.DebugFormat("Removing cache item with key '{0}'", key);

                    try
                    {
                        if (key.Contains("#"))
                        {
                            var h = key.Split('#')[0];
                            var k = key.Split('#')[1];

                            removed = client.RemoveEntryFromHash(h, k);
                        }
                        else
                        {
                            removed = client.Remove(key);
                        }

                        if (!removed)
                        {
                            throw new ArgumentException(string.Format("Error when removing key '{0}' to database", key));
                        }
                    }
                    catch (Exception ex)
                    {
                        this.log.Error(ex);
                    }

                    this.log.DebugFormat("Successfully removed cache item with key '{0}'", key);
                }
            }
            catch (Exception ex)
            {
                this.log.Error("Error when connecting to database", ex);
            }

            this.log.InfoFormat(
                "RedisCacheHandler.Remove({1}) Time: {0}s",
                DateTime.Now.Subtract(start).TotalSeconds,
                key);

            return removed;
        }

        /// <summary>
        /// Purges all cache items.
        /// </summary>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public override async Task<bool> Purge()
        {
            var start = DateTime.Now;

            var purged = false;

            try
            {
                using (var client = this.clientManager.GetClient())
                {
                    if (!string.IsNullOrEmpty(this.configuration.Prefix))
                    {
                        this.log.DebugFormat(
                            "Purging cache with prefix '{0}' in database {1}#{2}",
                            this.configuration.Prefix,
                            this.configuration.Host,
                            this.configuration.Database);

                        client.RemoveAll(client.ScanAllKeys(string.Format("{0}:*", this.configuration.Prefix)));   
                    }
                    else
                    {
                        this.log.DebugFormat(
                            "Purging cache in database {0}#{1}",
                            this.configuration.Host,
                            this.configuration.Database);
                        
                        client.FlushDb();
                    }
                    
                    purged = true;
                }
            }
            catch (Exception ex)
            {
                this.log.Error("Error when connecting to database", ex);
            }

            this.log.InfoFormat(
                "RedisCacheHandler.Purge() Time: {0}s",
                DateTime.Now.Subtract(start).TotalSeconds);

            return purged;
        }
    }
}

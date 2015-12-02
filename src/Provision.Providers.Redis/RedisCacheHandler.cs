namespace Provision.Providers.Redis
{
    using Common.Logging;
    using MsgPack.Serialization;
    using Newtonsoft.Json;
    using Provision.Extensions;
    using Provision.Interfaces;
    using Provision.Models;
    using StackExchange.Redis;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

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
        /// The hash provider
        /// </summary>
        private readonly SHA1CryptoServiceProvider hashProvider = new SHA1CryptoServiceProvider();

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
        }

        private async Task AddOrUpdateCacheTags(string cacheKey, params string[] tags)
        {
            var db = this.configuration.Connection.GetDatabase();

            foreach (var tag in tags)
            {

                await db.HashSetAsync(string.Format("{0}:{1}:{2}", this.configuration.Prefix, "_tags", tag), cacheKey, cacheKey);
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
            // Throw exception if any segment is null
            if (segments.Any(x => x == null))
            {
                throw new ArgumentException("Cannot create key from null segments", nameof(segments));
            }

            // Hash segments if they are too long, replace invalid characters with valid ones
            var seg = segments.Select(
                    x =>
                    x.ToString().Length <= 128
                        ? x.ToString().Replace(':', '-')
                        : Convert.ToBase64String(this.hashProvider.ComputeHash(Encoding.UTF8.GetBytes(x.ToString()))));

            var key = string.Format("{0}:{1}", this.configuration.Prefix, string.Join(":", seg));

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
            var start = DateTime.UtcNow;

            var exists = false;

            try
            {
                var db = this.configuration.Connection.GetDatabase();

                try
                {
                    if (key.Contains("#"))
                    {
                        var h = key.Split('#')[0];
                        var k = key.Split('#')[1];

                        exists = await db.HashExistsAsync(h, k);
                    }
                    else
                    {
                        exists = await db.KeyExistsAsync(key);
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
            }
            catch (Exception ex)
            {
                this.log.Error("Error when connecting to database", ex);
            }

            this.log.InfoFormat("RedisCacheHandler.Contains({1}) Time: {0}s", DateTime.UtcNow.Subtract(start).TotalSeconds, key);

            return exists;
        }

        /// <summary>
        /// Gets the cache item with specified key.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The cache item.</returns>
        public override async Task<ICacheItem<T>> Get<T>(string key)
        {
            var start = DateTime.UtcNow;

            try
            {
                var db = this.configuration.Connection.GetDatabase();

                this.log.DebugFormat("Trying to get cache item with key '{0}' and type '{1}'", key, typeof(T));

                try
                {
                    RedisValue data;
                    var expires = DateTime.MinValue;

                    if (key.Contains("#"))
                    {
                        var h = key.Split('#')[0];
                        var k = key.Split('#')[1];

                        // Get data
                        data = await db.HashGetAsync(h, k);

                        // Get expire time
                        var ttl = await db.KeyTimeToLiveAsync(h);
                        if (ttl.HasValue && ttl < TimeSpan.MaxValue && ttl.Value.TotalSeconds > 0)
                        {
                            expires = DateTime.UtcNow.Add(ttl.Value);
                        }
                    }
                    else
                    {
                        // Get data
                        data = await db.StringGetAsync(key);

                        // Get expire time
                        var ttl = await db.KeyTimeToLiveAsync(key);
                        if (ttl.HasValue && ttl < TimeSpan.MaxValue && ttl.Value.TotalSeconds > 0)
                        {
                            expires = DateTime.UtcNow.Add(ttl.Value);
                        }
                    }

                    // If data is empty, return empty result
                    if (!data.HasValue)
                    {
                        this.log.WarnFormat("Couldn't find cache item with key '{0}'", key);

                        return CacheItem<T>.Empty(key);
                    }

                    // Convert data to typed result
                    T result;

                    // If compression is specified, decompress result
                    if (this.configuration.Compress)
                    {
                        var serializer = SerializationContext.Default.GetSerializer<T>();
                        result = serializer.UnpackSingleObject(data);
                    }
                    else
                    {
                        result = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data));
                    }

                    if (result == null)
                    {
                        this.log.WarnFormat("Couldn't retrieve cache item with key '{0}' and type '{1}'", key, typeof(T));
                    }
                    else
                    {
                        this.log.DebugFormat("Retrieved cache item with key '{0}' and type '{1}'. It expires at {2}", key, typeof(T), expires);
                        this.log.InfoFormat("RedisCacheHandler.GetCacheItem<{2}>({1}) Time: {0}s", DateTime.UtcNow.Subtract(start).TotalSeconds, key, typeof(T));

                        var ci = new CacheItem<T>(key, result, expires);
                        ci.MergeExpire();

                        return ci;
                    }
                }
                catch (Exception ex)
                {
                    this.log.Error(string.Format("Error when getting value '{0}' and type '{1}' from database", key, typeof(T)), ex);
                }
            }
            catch (Exception ex)
            {
                this.log.Error("Error when connecting to database", ex);
            }

            this.log.InfoFormat("(ERROR) RedisCacheHandler.GetCacheItem<{2}>({1}) Time: {0}s", DateTime.UtcNow.Subtract(start).TotalSeconds, key, typeof(T));

            return CacheItem<T>.Empty(key);
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
            return await this.AddOrUpdate(key, item, this.ExpireTime.UtcDateTime);
        }

        /// <summary>
        /// Adds or updates a cache item with specified key and object.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <param name="expires">The expire time.</param>
        /// <returns>A task.</returns>
        public override async Task<T> AddOrUpdate<T>(string key, T item, DateTimeOffset expires, params string[] tags)
        {
            var start = DateTime.UtcNow;

            if (item != null)
            {
                try
                {
                    var db = this.configuration.Connection.GetDatabase();

                    try
                    {
                        var inserted = true;

                        byte[] data;

                        // If compression is specified, compress data before inserting into database
                        if (this.configuration.Compress)
                        {
                            var serializer = SerializationContext.Default.GetSerializer<T>();
                            data = serializer.PackSingleObject(item);
                        }
                        else
                        {
                            data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));
                        }

                        if (key.Contains("#"))
                        {
                            var h = key.Split('#')[0];
                            var k = key.Split('#')[1];

                            // Add data to db
                            inserted = await db.HashSetAsync(h, k, data);

                            // Make item expire if not already set
                            if (expires.UtcDateTime > DateTime.UtcNow)
                            {
                                var ttl = await db.KeyTimeToLiveAsync(h) ?? TimeSpan.FromSeconds(0);
                                if (ttl.TotalSeconds <= 0)
                                {
                                    await db.KeyExpireAsync(h, expires.UtcDateTime);
                                }
                            }
                        }
                        else
                        {
                            // Add data to db
                            inserted = await db.StringSetAsync(key, data);

                            // Make item expire
                            if (expires.UtcDateTime > DateTime.UtcNow)
                            {
                                await db.KeyExpireAsync(key, expires.UtcDateTime);
                            }
                        }

                        await this.AddOrUpdateCacheTags(key, tags);

                        this.log.DebugFormat(
                            inserted
                                ? "Successfully inserted cache item with key '{0}'"
                                : "Successfully updated cache item with key '{0}'",
                            key);

                        // Set expires time if item is expireable
                        if (typeof(IExpires).IsAssignableFrom(typeof(T)) && expires.UtcDateTime > DateTime.UtcNow)
                        {
                            ((IExpires)item).Expires = expires.UtcDateTime;
                        }

                        this.log.InfoFormat(
                            "RedisCacheHandler.AddOrUpdate<{2}>({1}, {2}, {3}) Time: {0}s",
                            DateTime.UtcNow.Subtract(start).TotalSeconds,
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
                catch (Exception ex)
                {
                    this.log.Error("Error when connecting to database", ex);
                }
            }

            this.log.InfoFormat(
                "RedisCacheHandler.AddOrUpdate<{2}>({1}, {2}, {3}) Time: {0}s (Error)",
                DateTime.UtcNow.Subtract(start).TotalSeconds,
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
            var start = DateTime.UtcNow;

            var removed = false;

            this.log.DebugFormat("Removing cache item with key '{0}'", key);

            try
            {
                var db = this.configuration.Connection.GetDatabase();

                try
                {
                    if (key.Contains("#"))
                    {
                        var h = key.Split('#')[0];
                        var k = key.Split('#')[1];

                        removed = await db.HashDeleteAsync(h, k);
                    }
                    else
                    {
                        removed = await db.KeyDeleteAsync(key);
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
            }
            catch (Exception ex)
            {
                this.log.Error("Error when connecting to database", ex);
            }

            this.log.DebugFormat("Successfully removed cache item with key '{0}'", key);

            this.log.InfoFormat(
                "RedisCacheHandler.Remove({1}) Time: {0}s",
                DateTime.UtcNow.Subtract(start).TotalSeconds,
                key);

            return removed;
        }

        /// <summary>
        /// Removes all cache items matching the specified pattern.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public override async Task<bool> RemoveAll(string pattern)
        {
            var start = DateTime.UtcNow;

            var removed = false;

            this.log.DebugFormat("Removing cache items matching pattern '{0}'", pattern);

            try
            {
                var server = this.configuration.Connection.GetServer(this.configuration.Host, this.configuration.Port);
                var db = this.configuration.Connection.GetDatabase();

                try
                {
                    var keys = server.Keys(this.configuration.Database, pattern);
                    foreach (var key in keys)
                    {
                        await db.KeyDeleteAsync(key);
                    }

                    removed = true;
                }
                catch (Exception ex)
                {
                    this.log.Error(ex);
                }
            }
            catch (Exception ex)
            {
                this.log.Error("Error when connecting to database", ex);
            }

            this.log.DebugFormat("Successfully removed cache items matching pattern '{0}'", pattern);

            this.log.InfoFormat(
                "RedisCacheHandler.RemoveAll({1}) Time: {0}s",
                DateTime.UtcNow.Subtract(start).TotalSeconds,
                pattern);

            return removed;
        }

        /// <summary>
        /// Removes all cache items matching the specified regular expression.
        /// </summary>
        /// <param name="regex">The regular expression.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public override async Task<bool> RemoveAll(Regex regex)
        {
            throw new NotSupportedException("Redis does not support regular expression matching");
        }

        public override async Task<bool> RemoveTags(params string[] tags)
        {
            var start = DateTime.UtcNow;

            try
            {
                var database = this.configuration.Connection.GetDatabase();

                try
                {
                    foreach (var tag in tags)
                    {
                        var tagsCacheKey = string.Format("{0}:{1}:{2}", this.configuration.Prefix, "_tags", tag);

                        var keys = await database.HashGetAllAsync(tagsCacheKey);

                        foreach (var key in keys)
                        {
                            await database.KeyDeleteAsync(key.Name.ToString());
                        }

                        await database.KeyDeleteAsync(tagsCacheKey);
                    }
                }
                catch (Exception ex)
                {
                    this.log.Error(ex);
                    return false;
                }

            }
            catch (Exception ex)
            {
                this.log.Error("Error when connecting to database", ex);
                return false;
            }

            this.log.DebugFormat("Successfully removed cache items matching tags '{0}'", string.Join(",", tags));

            this.log.InfoFormat(
                "RedisCacheHandler.RemoveTags({1}) Time: {0}s",
                DateTime.UtcNow.Subtract(start).TotalSeconds,
                string.Join(",", tags));

            return true;
        }

        /// <summary>
        /// Purges all cache items.
        /// </summary>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public override async Task<bool> Purge()
        {
            var start = DateTime.UtcNow;

            var purged = false;

            try
            {
                var server = this.configuration.Connection.GetServer(this.configuration.Host, this.configuration.Port);
                var db = this.configuration.Connection.GetDatabase();

                try
                {
                    if (!string.IsNullOrEmpty(this.configuration.Prefix))
                    {
                        this.log.DebugFormat(
                            "Purging cache with prefix '{0}' in database {1}#{2}",
                            this.configuration.Prefix,
                            this.configuration.Host,
                            this.configuration.Database);
                        var keys = server.Keys(this.configuration.Database, $"{this.configuration.Prefix}:*");

                        foreach (var key in keys)
                        {
                            await db.KeyDeleteAsync(key);
                        }
                    }
                    else
                    {
                        this.log.DebugFormat(
                            "Purging cache in database {0}#{1}",
                            this.configuration.Host,
                            this.configuration.Database);

                        await server.FlushDatabaseAsync(this.configuration.Database);
                    }

                    purged = true;
                }
                catch (Exception ex)
                {
                    this.log.Error(ex);
                }
            }
            catch (Exception ex)
            {
                this.log.Error("Error when connecting to database", ex);
            }

            this.log.InfoFormat(
                    "RedisCacheHandler.Purge() Time: {0}s",
                    DateTime.UtcNow.Subtract(start).TotalSeconds);

            return purged;
        }
    }
}

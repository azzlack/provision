namespace Provision.Providers.Redis
{
    using Common.Logging;
    using MsgPack.Serialization;
    using Newtonsoft.Json;
    using Provision.Extensions;
    using Provision.Interfaces;
    using Provision.Models;
    using Provision.Providers.Redis.Extensions;
    using StackExchange.Redis;
    using System;
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

        /// <summary>
        /// Creates a cache item key from the specified segments.
        /// </summary>
        /// <typeparam name="T">The type to create a cache key for.</typeparam>
        /// <param name="segments">The key segments.</param>
        /// <returns>A cache item key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="format" /> is null. </exception>
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
                        ? x.ToString().Replace(this.configuration.Separator, "-")
                        : Convert.ToBase64String(this.hashProvider.ComputeHash(Encoding.UTF8.GetBytes(x.ToString()))));

            var key = $"{this.configuration.Prefix}{this.configuration.Separator}{string.Join(this.configuration.Separator, seg)}";

            // Add prefix
            if (string.IsNullOrEmpty(this.configuration.Prefix))
            {
                key = $"{string.Join(this.configuration.Separator, segments.Select(x => x.ToString().Replace(this.configuration.Separator, "-")))}";
            }

            // Remove any separators before and after hash
            if (key.Contains("#"))
            {
                var f = key.Split('#')[0].TrimEnd(Convert.ToChar(this.configuration.Separator));
                var l = key.Split('#')[1].TrimStart(Convert.ToChar(this.configuration.Separator));

                key = string.Join("#", f, l);
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
            var start = DateTimeOffset.UtcNow;

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

            this.log.InfoFormat("RedisCacheHandler.Contains({1}) Time: {0}s", DateTimeOffset.UtcNow.Subtract(start).TotalSeconds, key);

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
            var start = DateTimeOffset.UtcNow;

            try
            {
                var db = this.configuration.Connection.GetDatabase();

                this.log.DebugFormat("Trying to get cache item with key '{0}' and type '{1}'", key, typeof(T));

                try
                {
                    RedisValue data;
                    var expires = DateTimeOffset.MinValue;

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
                            expires = DateTimeOffset.UtcNow.Add(ttl.Value);
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
                            expires = DateTimeOffset.UtcNow.Add(ttl.Value);
                        }
                    }

                    // If data is empty, return empty result
                    if (!data.HasValue)
                    {
                        this.log.WarnFormat("Couldn't find cache item with key '{0}'", key);

                        return CacheItem<T>.Empty(key);
                    }

                    // Convert data to typed result
                    var raw = data;
                    T result;

                    // If compression is specified, decompress result
                    if (this.configuration.Compress)
                    {
                        var serializer = SerializationContext.Default.GetSerializer<T>();

                        result = serializer.UnpackSingleObject(data);
                    }
                    else
                    {
                        var json = Encoding.UTF8.GetString(data);

                        result = JsonConvert.DeserializeObject<T>(json);
                    }

                    if (result == null)
                    {
                        this.log.WarnFormat("Couldn't retrieve cache item with key '{0}' and type '{1}'", key, typeof(T));
                    }
                    else
                    {
                        this.log.DebugFormat("Retrieved cache item with key '{0}' and type '{1}'. It expires at {2}", key, typeof(T), expires);
                        this.log.InfoFormat("RedisCacheHandler.GetCacheItem<{2}>({1}) Time: {0}s", DateTimeOffset.UtcNow.Subtract(start).TotalSeconds, key, typeof(T));

                        var ci = new CacheItem<T>(key, result, raw, expires);
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

            this.log.InfoFormat("(ERROR) RedisCacheHandler.GetCacheItem<{2}>({1}) Time: {0}s", DateTimeOffset.UtcNow.Subtract(start).TotalSeconds, key, typeof(T));

            return CacheItem<T>.Empty(key);
        }

        /// <summary>Adds or updates a cache item with specified key and object.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <param name="expires">The expire time.</param>
        /// <param name="tags">The tags.</param>
        /// <returns>A task.</returns>
        public override async Task<T> AddOrUpdate<T>(string key, T item, DateTimeOffset expires, params string[] tags)
        {
            var start = DateTimeOffset.UtcNow;

            if (item != null)
            {
                try
                {
                    var db = this.configuration.Connection.GetDatabase();

                    try
                    {
                        bool inserted;

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

                            // Make item expire
                            await db.KeyExpireAsync(h, expires.UtcDateTime);
                        }
                        else
                        {
                            // Add data to db
                            inserted = await db.StringSetAsync(key, data);

                            // Make item expire
                            await db.KeyExpireAsync(key, expires.UtcDateTime);
                        }

                        await this.AddOrUpdateKeyIndex(key, expires);
                        await this.AddOrUpdateTagIndex(tags, key, expires);

                        this.log.DebugFormat(
                            inserted
                                ? "Successfully inserted cache item with key '{0}'"
                                : "Successfully updated cache item with key '{0}'",
                            key);

                        // Set expires time if item is expireable
                        if (typeof(IExpires).IsAssignableFrom(typeof(T)) && expires.UtcDateTime > DateTimeOffset.UtcNow)
                        {
                            ((IExpires)item).Expires = expires.UtcDateTime;
                        }

                        this.log.InfoFormat(
                            "RedisCacheHandler.AddOrUpdate<{2}>({1}, {2}, {3}) Time: {0}s",
                            DateTimeOffset.UtcNow.Subtract(start).TotalSeconds,
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
                DateTimeOffset.UtcNow.Subtract(start).TotalSeconds,
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
        public override async Task<bool> RemoveByKey(string key)
        {
            var start = DateTimeOffset.UtcNow;

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
                        throw new ArgumentException($"Error when removing key '{key}' from database");
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
                "RedisCacheHandler.RemoveByKey({1}) Time: {0}s",
                DateTimeOffset.UtcNow.Subtract(start).TotalSeconds,
                key);

            return removed;
        }

        /// <summary>
        /// Removes all cache items matching the specified pattern.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public override async Task<bool> RemoveByPattern(string pattern)
        {
            var start = DateTimeOffset.UtcNow;

            var removed = false;

            this.log.DebugFormat("Removing cache items matching pattern '{0}'", pattern);

            try
            {
                var db = this.configuration.Connection.GetDatabase();

                try
                {
                    var keys = db.SortedSetScan(this.configuration.IndexKey, pattern);

                    await db.KeyDeleteAsync(keys.Select(x => (RedisKey)x.Element.ToString()).ToArray());

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
                "RedisCacheHandler.RemoveByPattern({1}) Time: {0}s",
                DateTimeOffset.UtcNow.Subtract(start).TotalSeconds,
                pattern);

            return removed;
        }

        /// <summary>
        /// Removes all cache items matching the specified regular expression.
        /// </summary>
        /// <param name="regex">The regular expression.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public override Task<bool> RemoveByPattern(Regex regex)
        {
            throw new NotSupportedException("Redis does not support regular expression matching");
        }

        /// <summary>Removes all cache items matching the specified tags.</summary>
        /// <param name="tags">The tags.</param>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public override async Task<bool> RemoveByTag(params string[] tags)
        {
            var start = DateTimeOffset.UtcNow;

            try
            {
                var db = this.configuration.Connection.GetDatabase();

                try
                {
                    foreach (var tag in tags)
                    {
                        var tagKey = $"{this.configuration.TagKey}{this.configuration.Separator}{tag}";

                        var keys = new List<RedisKey>();

                        // Get all keys expiring in the future from index
                        foreach (var entry in await db.SortedSetRangeByScoreAsync(tagKey, DateTimeOffset.UtcNow.ToUnixTime(), DateTimeOffset.MaxValue.ToUnixTime()))
                        {
                            var k = entry.ToString();

                            if (k.Contains("#"))
                            {
                                keys.Add(k.Split('#')[0]);
                            }
                            else
                            {
                                keys.Add(k);
                            }
                        }

                        await db.KeyDeleteAsync(keys.ToArray());
                        await db.KeyDeleteAsync(tagKey);
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

            this.log.InfoFormat("RedisCacheHandler.RemoveByTag({1}) Time: {0}s", DateTimeOffset.UtcNow.Subtract(start).TotalSeconds, string.Join(",", tags));

            return true;
        }

        /// <summary>
        /// Purges all cache items.
        /// </summary>
        /// <returns><c>True</c> if successful, <c>false</c> otherwise.</returns>
        public override async Task<bool> Purge()
        {
            var start = DateTimeOffset.UtcNow;

            var purged = false;

            try
            {
                var db = this.configuration.Connection.GetDatabase();

                try
                {
                    this.log.DebugFormat(
                        "Purging cache with prefix '{0}' in database {1}#{2}",
                        this.configuration.Prefix,
                        this.configuration.Host,
                        this.configuration.Database);

                    var keys = new List<RedisKey>();

                    // Get all keys expiring in the future from index
                    foreach (var entry in await db.SortedSetRangeByScoreAsync(this.configuration.IndexKey, DateTimeOffset.UtcNow.ToUnixTime(), DateTimeOffset.MaxValue.ToUnixTime()))
                    {
                        var k = entry.ToString();

                        if (k.Contains("#"))
                        {
                            keys.Add(k.Split('#')[0]);
                        }
                        else
                        {
                            keys.Add(k);
                        }
                    }

                    // Delete keys
                    await db.KeyDeleteAsync(keys.ToArray());

                    // Delete indexes
                    await db.KeyDeleteAsync(this.configuration.IndexKey);
                    await db.KeyDeleteAsync(this.configuration.TagKey);
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

            this.log.InfoFormat("RedisCacheHandler.Purge() Time: {0}s", DateTimeOffset.UtcNow.Subtract(start).TotalSeconds);

            return purged;
        }

        /// <summary>Adds or updates the specified tag collections with the specified key.</summary>
        /// <param name="tags">The tags.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="expires">The expire time.</param>
        /// <returns>An async void.</returns>
        private async Task AddOrUpdateTagIndex(string[] tags, string key, DateTimeOffset expires)
        {
            var db = this.configuration.Connection.GetDatabase();

            foreach (var tag in tags)
            {
                await db.SortedSetAddAsync($"{this.configuration.TagKey}{this.configuration.Separator}{tag}", key, expires.ToUnixTime());
            }
        }

        /// <summary>Adds or updated the key index with the specified data.</summary>
        /// <param name="key">The key.</param>
        /// <param name="expires">The expire time.</param>
        /// <returns>An async void.</returns>
        private async Task AddOrUpdateKeyIndex(string key, DateTimeOffset expires)
        {
            var db = this.configuration.Connection.GetDatabase();

            var score = expires > DateTimeOffset.MinValue ? expires.ToUnixTime() : double.MaxValue;

            // Update score if key exists, otherwise add it
            var oldScore = await db.SortedSetScoreAsync(this.configuration.IndexKey, key);
            if (oldScore.HasValue)
            {
                var newScore = (score - oldScore.Value) > DateTimeOffsetExtensions.Epoch.ToUnixTime() ? score - oldScore.Value : DateTimeOffsetExtensions.Epoch.ToUnixTime();

                await db.SortedSetIncrementAsync(this.configuration.IndexKey, key, newScore);
            }
            else
            {
                await db.SortedSetAddAsync(this.configuration.IndexKey, key, score);
            }
        }
    }
}

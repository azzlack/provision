namespace Provision.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Provision.Interfaces;

    /// <summary>
    /// Extensions for <see cref="ICacheItem{T}"/> instances.
    /// </summary>
    public static class CacheItemExtensions
    {
        /// <summary>
        /// Merges the expire date from the cache item onto its value, if applicable.
        /// </summary>
        /// <typeparam name="T">The value type</typeparam>
        /// <param name="cacheItem">The cache item.</param>
        public static void MergeExpire<T>(this ICacheItem<T> cacheItem)
        {
            if (cacheItem.HasValue && cacheItem.Expires.ToUniversalTime() > DateTime.MinValue)
            {
                if (typeof(IExpires).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo())
                    && ((IExpires)cacheItem.Value).Expires.ToUniversalTime() == DateTime.MinValue)
                {
                    ((IExpires)cacheItem.Value).Expires = cacheItem.Expires;
                }
            }
        }
    }
}
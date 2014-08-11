namespace Provision.Extensions
{
    using System;
    using System.Reflection;

    using Provision.Interfaces;

    /// <summary>
    /// Extensions for <see cref="ICacheItem{T}"/> instances.
    /// </summary>
    public static class ICacheItemExtensions
    {
        /// <summary>
        /// Merges the expire date from the cache item wrapper onto the value 
        /// if the value is of type <see cref="IExpires"/> and doesnt have an expiry date from before.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="obj">The object.</param>
        public static void MergeExpire<T>(this ICacheItem<T> obj)
        {
            if (obj.HasValue && obj.Expires.ToUniversalTime() > DateTime.MinValue)
            {
                if (typeof(IExpires).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo())
                    && ((IExpires)obj.Value).Expires.ToUniversalTime() == DateTime.MinValue)
                {
                    ((IExpires)obj.Value).Expires = obj.Expires;   
                }
            }
        }
    }
}
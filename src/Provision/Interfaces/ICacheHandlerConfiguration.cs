namespace Provision.Interfaces
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Interface for <see cref="ICacheHandler"/> implementations.
    /// </summary>
    public interface ICacheHandlerConfiguration
    {
        /// <summary>Gets the name.</summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>Gets the cache key prefix.</summary>
        /// <value>The cache key prefix.</value>
        string Prefix { get; }

        /// <summary>Gets the cache key segment separator.</summary>
        /// <value>The cache key segment separator.</value>
        string Separator { get; }

        /// <summary>
        /// Gets the cache handler type.
        /// </summary>
        /// <value>The cache handler type.</value>
        Type Type { get; }

        /// <summary>Gets the default expire time in CRON-format.</summary>
        /// <value>The default expire time.</value>
        string ExpireTime { get; }

        /// <summary>Gets the options.</summary>
        /// <value>The options.</value>
        IDictionary<string, object> Options { get; }

        /// <summary>Initializes the specified cache handler provider with the specified parameters.</summary>
        /// <param name="parameters">The parameters.</param>
        void Initialize(IDictionary<string, object> parameters);

        /// <summary>Gets the property value.</summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>The property value.</returns>
        T GetPropertyValue<T>(string propertyName);
    }
}
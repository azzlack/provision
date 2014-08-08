namespace Provision.Config
{
    using System;
    using System.Configuration;

    using Provision.Interfaces;

    public class CacheHandlerConfigurationElementCollection : ConfigurationElementCollection
    {
        /// <summary>
        /// Gets the cache handler configuration with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The cache handler configuration.</returns>
        public ICacheHandlerConfiguration Get(string name)
        {
            // Return default configuration if no name was specified
            if (string.IsNullOrEmpty(name))
            {
                return new CacheHandlerConfigurationElement();
            }

            // Get configuration element
            var e = this.BaseGet(name) as CacheHandlerConfigurationElement;

            if (e == null)
            {
                throw new ArgumentException("No provider with the specified name exist");
            }

            // If type has not been specified, just return the configuration
            if (e.Type == null)
            {
                return e;
            }

            var p = (ICacheHandlerConfiguration)Activator.CreateInstance(e.Type);
            p.Initialize(e.Options);

            return p;
        }

        /// <summary>
        /// Gets the cache handler configuration with the specified name.
        /// </summary>
        /// <typeparam name="T">The cache handler configuration type</typeparam>
        /// <param name="name">The name.</param>
        /// <returns>The cache handler configuration.</returns>
        /// <exception cref="System.ArgumentException">No provider with the specified name exist</exception>
        public T Get<T>(string name) where T : class, ICacheHandlerConfiguration
        {
            return this.Get(name) as T;
        }

        /// <summary>
        /// When overridden in a derived class, creates a new <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </summary>
        /// <returns>
        /// A newly created <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new CacheHandlerConfigurationElement();
        }

        /// <summary>
        /// Gets the element key for a specified configuration element when overridden in a derived class.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Object"/> that acts as the key for the specified <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </returns>
        /// <param name="element">The <see cref="T:System.Configuration.ConfigurationElement"/> to return the key for. </param>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((CacheHandlerConfigurationElement)element).Name;
        }
    }
}
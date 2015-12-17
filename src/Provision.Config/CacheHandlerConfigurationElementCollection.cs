namespace Provision.Config
{
    using Provision.Interfaces;
    using System;
    using System.Configuration;

    public class CacheHandlerConfigurationElementCollection : ConfigurationElementCollection
    {
        /// <summary>The cache handler configuration.</summary>
        private ICacheHandlerConfiguration cacheHandlerConfiguration;

        /// <summary>Gets the cache handler configuration with the specified name.</summary>
        /// <exception cref="ArgumentException">Thrown when one or more arguments have unsupported or illegal values.</exception>
        /// <returns>The cache handler configuration.</returns>
        public ICacheHandlerConfiguration Get()
        {
            if (this.cacheHandlerConfiguration == null)
            {
                // Get configuration element
                var e = this.BaseGet(0) as CacheHandlerConfigurationElement;

                if (e == null)
                {
                    throw new ArgumentException("No valid cache handler configuration exist");
                }

                // If type has not been specified, just return the configuration
                if (e.Type == null)
                {
                    this.cacheHandlerConfiguration = e;
                }
                else
                {
                    var p = (ICacheHandlerConfiguration)Activator.CreateInstance(e.Type);
                    p.Initialize(e.Options);

                    this.cacheHandlerConfiguration = p;
                }
            }

            return this.cacheHandlerConfiguration;
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
            return ((CacheHandlerConfigurationElement)element).Type;
        }
    }
}
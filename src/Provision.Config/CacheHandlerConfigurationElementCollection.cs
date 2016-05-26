using System.Collections.Generic;
using System.Linq;

namespace Provision.Config
{
    using Provision.Interfaces;
    using System;
    using System.Configuration;

    public class CacheHandlerConfigurationElementCollection : ConfigurationElementCollection
    {
        /// <summary>The cache handler configuration.</summary>
        private readonly IList<ICacheHandlerConfiguration> cacheHandlerConfiguration;

        public CacheHandlerConfigurationElementCollection()
        {
            this.cacheHandlerConfiguration = new List<ICacheHandlerConfiguration>();
        }

        /// <summary>Gets the cache handler configurations with the specified name.</summary>
        /// <exception cref="ArgumentException">Thrown when one or more arguments have unsupported or illegal values.</exception>
        /// <returns>The cache handler configurations.</returns>
        public IList<ICacheHandlerConfiguration> GetConfigurations()
        {
            if (this.Count == 0)
            {
                throw new ArgumentException("No valid cache handler configuration exist");
            }

            for (var i = 0; i < this.Count; i++)
            {
                var c = this.BaseGet(i);
                var e = c as CacheHandlerConfigurationElement;

                if (e == null)
                {
                    throw new ArgumentException("Invalid configuration for one or more cache handlers");
                }

                // If type has not been specified, throw error
                if (e.Type == null)
                {
                    throw new ArgumentException("Invalid configuration for one or more cache handlers");
                }

                var p = (ICacheHandlerConfiguration)Activator.CreateInstance(e.Type);
                p.Initialize(e.Options);

                this.cacheHandlerConfiguration.Add(p);
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Provision.Models;

namespace Provision.Config
{
    using Provision.Interfaces;
    using System;
    using System.ComponentModel;
    using System.Configuration;

    public class ProvisionConfiguration : ConfigurationSection, IProvisionConfiguration
    {
        /// <summary>The cache handler.</summary>
        private ICacheHandlerCollection cacheHandlers;

        /// <summary>The cache handler configurations.</summary>
        private IList<ICacheHandlerConfiguration> cacheHandlerConfigurations;

        /// <summary>
        /// The settings
        /// </summary>
        private static readonly Lazy<IProvisionConfiguration> Instance = new Lazy<IProvisionConfiguration>(() => ConfigurationManager.GetSection("provision") as ProvisionConfiguration);

        /// <summary>
        /// Gets the settings.
        /// </summary>
        /// <value>The settings.</value>
        public static IProvisionConfiguration Current => Instance.Value;

        /// <summary>
        /// Gets or sets the cache handler configurations.
        /// </summary>
        /// <value>The cache handler configurations.</value>
        [ConfigurationProperty("", IsDefaultCollection = true)]
        public CacheHandlerConfigurationElementCollection Providers
        {
            get
            {
                return base[string.Empty] as CacheHandlerConfigurationElementCollection;
            }

            set
            {
                base[string.Empty] = value;
            }
        }

        /// <summary>
        /// Gets the cache handler.
        /// </summary>
        /// <returns>The cache handler.</returns>
        public ICacheHandlerCollection GetHandlers()
        {
            var c = this.GetCacheHandlerConfigurations();

            return this.GetHandlers(c);
        }

        /// <summary>Gets the cache handler.</summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The cache handler.</returns>
        public ICacheHandlerCollection GetHandlers(IList<ICacheHandlerConfiguration> configuration)
        {
            if (this.cacheHandlers != null)
            {
                return this.cacheHandlers;
            }

            var handlers = new List<ICacheHandler>();

            foreach (var cacheHandlerConfiguration in configuration)
            {
                var handler = Activator.CreateInstance(cacheHandlerConfiguration.Type, cacheHandlerConfiguration) as ICacheHandler;

                handlers.Add(handler);
            }

            return (this.cacheHandlers = new CacheHandlerCollection(handlers));
        }

        /// <summary>Gets the configuration.</summary>
        /// <returns>The configuration.</returns>
        public IList<ICacheHandlerConfiguration> GetCacheHandlerConfigurations()
        {
            return this.cacheHandlerConfigurations ?? (this.cacheHandlerConfigurations = this.Providers.GetConfigurations());
        }
    }
}
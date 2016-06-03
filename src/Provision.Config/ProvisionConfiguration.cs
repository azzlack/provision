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

        /// <summary>
        /// The settings
        /// </summary>
        private static readonly Lazy<IProvisionConfiguration> Instance = new Lazy<IProvisionConfiguration>(() => ConfigurationManager.GetSection("provision") as ProvisionConfiguration);

        /// <summary>Prevents a default instance of the <see cref="ProvisionConfiguration" /> class from being created.</summary>
        private ProvisionConfiguration()
        {
            this.cacheHandlers = new CacheHandlerCollection();
        }

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
            return this.Providers.GetConfigurations();
        }
    }
}
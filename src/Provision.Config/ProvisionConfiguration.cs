namespace Provision.Config
{
    using System;
    using System.ComponentModel;
    using System.Configuration;

    using Provision.Interfaces;

    public class ProvisionConfiguration : ConfigurationSection
    {
        /// <summary>
        /// The settings
        /// </summary>
        private static readonly ProvisionConfiguration Instance = ConfigurationManager.GetSection("provision") as ProvisionConfiguration;

        /// <summary>
        /// Gets the settings.
        /// </summary>
        /// <value>The settings.</value>
        public static ProvisionConfiguration Configuration
        {
            get
            {
                return Instance;
            }
        }

        /// <summary>
        /// Gets or sets the handler.
        /// </summary>
        /// <value>The handler.</value>
        [ConfigurationProperty("handler", IsRequired = true)]
        [TypeConverter(typeof(TypeNameConverter))]
        public Type Handler
        {
            get
            {
                var v = this["handler"] as Type;

                if (v != null && v.IsClass && typeof(ICacheHandler).IsAssignableFrom(v))
                {
                    return v;
                }

                throw new ConfigurationErrorsException("No valid ICacheHandler specified.");
            }

            set
            {
                this["handler"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the default configuration.
        /// </summary>
        /// <value>The default configuration.</value>
        [ConfigurationProperty("defaultConfiguration", IsRequired = false)]
        public string DefaultConfiguration
        {
            get
            {
                return (string)this["defaultConfiguration"];
            }

            set
            {
                this["defaultConfiguration"] = value;
            }
        }

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
        public ICacheHandler GetHandler()
        {
            return this.GetHandler(this.DefaultConfiguration);
        }

        /// <summary>
        /// Gets the handler.
        /// </summary>
        /// <param name="configurationName">Name of the configuration.</param>
        /// <returns>The cache handler.</returns>
        public ICacheHandler GetHandler(string configurationName)
        {
            var c = this.Providers.Get(configurationName);

            return this.GetHandler(configurationName, c);
        }

        /// <summary>
        /// Gets the cache handler.
        /// </summary>
        /// <param name="configurationName">Name of the configuration.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The cache handler.</returns>
        public ICacheHandler GetHandler(string configurationName, ICacheHandlerConfiguration configuration)
        {
            return Activator.CreateInstance(this.Handler, configuration) as ICacheHandler;
        }
    }
}
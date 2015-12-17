namespace Provision.Config
{
    using Provision.Interfaces;
    using System;
    using System.ComponentModel;
    using System.Configuration;

    public class ProvisionConfiguration : ConfigurationSection, IProvisionConfiguration
    {
        /// <summary>The cache handler.</summary>
        private ICacheHandler cacheHandler;

        /// <summary>
        /// The settings
        /// </summary>
        private static readonly Lazy<IProvisionConfiguration> Instance = new Lazy<IProvisionConfiguration>(() => ConfigurationManager.GetSection("provision") as ProvisionConfiguration);

        /// <summary>Prevents a default instance of the <see cref="ProvisionConfiguration" /> class from being created.</summary>
        private ProvisionConfiguration()
        {
        }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        /// <value>The settings.</value>
        public static IProvisionConfiguration Current => Instance.Value;

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
            var c = this.GetConfiguration();

            return this.GetHandler(c);
        }

        /// <summary>Gets the cache handler.</summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The cache handler.</returns>
        public ICacheHandler GetHandler(ICacheHandlerConfiguration configuration)
        {
            if (this.cacheHandler == null)
            {
                this.cacheHandler = Activator.CreateInstance(this.Handler, configuration) as ICacheHandler;
            }

            return this.cacheHandler;
        }

        /// <summary>Gets the configuration.</summary>
        /// <returns>The configuration.</returns>
        public ICacheHandlerConfiguration GetConfiguration()
        {
            return this.Providers.Get();
        }
    }
}
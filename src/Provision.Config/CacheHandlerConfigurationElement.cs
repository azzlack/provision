namespace Provision.Config
{
    using Provision.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Configuration;

    public class CacheHandlerConfigurationElement : ConfigurationElement, ICacheHandlerConfiguration
    {
        /// <summary>The options.</summary>
        private readonly IDictionary<string, object> options;

        /// <summary>Initializes a new instance of the <see cref="CacheHandlerConfigurationElement"/> class.</summary>
        public CacheHandlerConfigurationElement()
        {
            this.options = new Dictionary<string, object>();
        }

        /// <summary>Gets the options.</summary>
        public IDictionary<string, object> Options => this.options;

        /// <summary>Gets or sets the host.</summary>
        /// <value>The host.</value>
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name
        {
            get
            {
                return (string)base["name"];
            }

            set
            {
                base["name"] = value;
            }
        }

        /// <summary>Gets the cache key prefix.</summary>
        /// <value>The cache key prefix.</value>
        public string Prefix
        {
            get
            {
                return (string)base["prefix"];
            }

            set
            {
                base["prefix"] = value;
            }
        }

        /// <summary>Gets the cache key segment separator.</summary>
        /// <value>The cache key segment separator.</value>
        public string Separator
        {
            get
            {
                return (string)base["separator"];
            }

            set
            {
                base["separator"] = value;
            }
        }

        /// <summary>Gets or sets the handler.</summary>
        /// <exception cref="ConfigurationErrorsException">Thrown when a Configuration Errors error condition occurs.</exception>
        /// <value>The handler.</value>
        [ConfigurationProperty("type", IsRequired = false)]
        [TypeConverter(typeof(TypeNameConverter))]
        public Type Type
        {
            get
            {
                var v = this["type"] as Type;

                if (v != null && v.IsClass && typeof(ICacheHandlerConfiguration).IsAssignableFrom(v))
                {
                    return v;
                }

                throw new ConfigurationErrorsException("No valid ICacheHandler specified.");
            }

            set
            {
                this["type"] = value;
            }
        }

        /// <summary>Gets or sets the default expire time in CRON-format.</summary>
        /// <value>The default expire time.</value>
        [ConfigurationProperty("expireTime", IsRequired = false, DefaultValue = "0 0/1 * 1/1 * ? *")]
        public string ExpireTime
        {
            get
            {
                return (string)base["expireTime"];
            }

            set
            {
                base["expireTime"] = value;
            }
        }

        /// <summary>Initializes the specified cache handler provider with the specified parameters.</summary>
        /// <exception cref="ArgumentNullException">Thrown when one or more required arguments are null.</exception>
        /// <param name="parameters">The parameters.</param>
        public void Initialize(IDictionary<string, object> parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            // Merge parameters with current options
            foreach (var parameter in parameters)
            {
                if (this.options.ContainsKey(parameter.Key))
                {
                    this.options[parameter.Key] = parameter.Value;
                }
                else
                {
                    this.options.Add(parameter);
                }
            }
        }

        /// <summary>Gets the property value.</summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>The property value.</returns>
        public T GetPropertyValue<T>(string propertyName)
        {
            try
            {
                return (T)base[propertyName];
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>Gets a value indicating whether an unknown attribute is encountered during deserialization.</summary>
        /// <param name="name">The name of the unrecognized attribute.</param>
        /// <param name="value">The value of the unrecognized attribute.</param>
        /// <returns>true when an unknown attribute is encountered while deserializing; otherwise, false.</returns>
        protected override bool OnDeserializeUnrecognizedAttribute(string name, string value)
        {
            this.options.Add(name, value);

            return true;
        }
    }
}

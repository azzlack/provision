namespace Provision.Models
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    using Provision.Interfaces;

    public class BaseCacheHandlerConfiguration : ICacheHandlerConfiguration
    {
        /// <summary>
        /// The options
        /// </summary>
        private readonly IDictionary<string, object> options;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCacheHandlerConfiguration"/> class.
        /// </summary>
        public BaseCacheHandlerConfiguration()
            : this("noop", typeof(BaseCacheHandler).GetTypeInfo())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCacheHandlerConfiguration"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="cacheHandlerType">The cache handler type.</param>
        public BaseCacheHandlerConfiguration(string name, TypeInfo cacheHandlerType)
        {
            this.options = new Dictionary<string, object>()
                               {
                                   { "name", name },
                                   { "type", cacheHandlerType.ToString() }
                               };
        }

        /// <summary>
        /// Gets the option with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The option.</returns>
        public object this[string key]
        {
            get
            {
                return this.options[key];
            }
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name 
        {
            get
            {
                return this.options["name"] as string;
            } 
        }

        /// <summary>
        /// Gets or sets the cache handler.
        /// </summary>
        /// <value>The cache handler.</value>
        public Type Type
        {
            get
            {
                var v = this.options["type"] as Type;

                if (v != null && v.GetTypeInfo().IsClass && typeof(ICacheHandlerConfiguration).GetTypeInfo().IsAssignableFrom(v.GetTypeInfo()))
                {
                    return v;
                }

                throw new Exception("No valid ICacheHandler specified.");
            }
        }

        /// <summary>
        /// Gets or sets the default expire time.
        /// </summary>
        /// <value>The default expire time.</value>
        public string ExpireTime
        {
            get
            {
                if (this.options.ContainsKey("expireTime"))
                {
                    return this.options["expireTime"] as string;
                }

                return "0 0/1 * 1/1 * ? *";
            }
        }

        /// <summary>
        /// Gets the options.
        /// </summary>
        /// <value>The options.</value>
        public IDictionary<string, object> Options
        {
            get
            {
                return this.options;
            }
        }

        /// <summary>
        /// Initializes the specified cache handler provider with the specified parameters.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        public virtual void Initialize(IDictionary<string, object> parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
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

        /// <summary>
        /// Gets the property value.
        /// </summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>The property value.</returns>
        public virtual T GetPropertyValue<T>(string propertyName)
        {
            try
            {
                return (T)this[propertyName];
            }
            catch
            {
                return default(T);
            }
        }
    }
}
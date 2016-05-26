namespace Provision.Providers
{
    using Provision.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public class BaseCacheHandlerConfiguration : ICacheHandlerConfiguration
    {
        /// <summary>The options.</summary>
        private readonly IDictionary<string, object> options;

        /// <summary>Initializes a new instance of the <see cref="BaseCacheHandlerConfiguration"/> class.</summary>
        public BaseCacheHandlerConfiguration()
            : this("noop", typeof(BaseCacheHandler).GetTypeInfo(), "", "")
        {
        }

        /// <summary>Initializes a new instance of the <see cref="BaseCacheHandlerConfiguration"/> class.</summary>
        /// <param name="name">The name.</param>
        /// <param name="cacheHandlerType">The cache handler type.</param>
        /// <param name="separator">The cache key segment separator.</param>
        /// <param name="prefix">The cache key prefix.</param>
        public BaseCacheHandlerConfiguration(string name, TypeInfo cacheHandlerType, string separator, string prefix)
        {
            this.options = new Dictionary<string, object>()
                               {
                                   { "name", name },
                                   { "type", cacheHandlerType.AsType() },
                                   { "separator", separator },
                                   { "prefix", prefix }
                               };
        }

        /// <summary>Gets the option with the specified key.</summary>
        public object this[string key] => this.options[key];

        /// <summary>Gets the name.</summary>
        public string Name
        {
            get
            {
                var n = this.options["name"] as string;

                if (string.IsNullOrEmpty(n))
                {
                    return this.Type.Name;
                }

                return n;
            }
        }

        /// <summary>Gets the cache key prefix.</summary>
        /// <value>The cache key prefix.</value>
        public string Prefix => this.options["prefix"] as string;

        /// <summary>Gets the cache key segment separator.</summary>
        /// <value>The cache key segment separator.</value>
        public string Separator => this.options["separator"] as string;

        /// <summary>Gets the cache handler.</summary>
        /// <exception cref="ArgumentException">Thrown when an exception error condition occurs.</exception>
        /// <value>The cache handler.</value>
        public Type Type
        {
            get
            {
                var v = this.options["type"] as Type;

                if (v != null && v.GetTypeInfo().IsClass && typeof(ICacheHandler).GetTypeInfo().IsAssignableFrom(v.GetTypeInfo()))
                {
                    return v;
                }

                throw new ArgumentException("No valid ICacheHandler specified.");
            }
        }

        /// <summary>Gets the default expire time.</summary>
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

        /// <summary>Gets the options.</summary>
        /// <value>The options.</value>
        public IDictionary<string, object> Options => this.options;

        /// <summary>Initializes the specified cache handler provider with the specified parameters.</summary>
        /// <exception cref="ArgumentNullException">Thrown when one or more required arguments are null.</exception>
        /// <param name="parameters">The parameters.</param>
        public virtual void Initialize(IDictionary<string, object> parameters)
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
        public virtual T GetPropertyValue<T>(string propertyName)
        {
            try
            {
                return (T)Convert.ChangeType(this[propertyName], typeof(T));
            }
            catch
            {
                return default(T);
            }
        }
    }
}
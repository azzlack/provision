namespace Provision.Providers.Redis
{
    using Provision.Models;

    public class RedisCacheHandlerConfiguration : BaseCacheHandlerConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCacheHandlerConfiguration"/> class.
        /// </summary>
        public RedisCacheHandlerConfiguration()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCacheHandlerConfiguration" /> class.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <param name="port">The port.</param>
        /// <param name="database">The database.</param>
        /// <param name="password">The password.</param>
        /// <param name="prefix">The prefix.</param>
        /// <param name="loggerName">The logger name.</param>
        /// <param name="compress">if set to <c>true</c> [compress].</param>
        public RedisCacheHandlerConfiguration(string host, int port, int database, string password, string prefix, string loggerName, bool compress)
        {
            this.Options["host"] = host;
            this.Options["port"] = port;
            this.Options["database"] = database;
            this.Options["password"] = password;
            this.Options["prefix"] = prefix;
            this.Options["loggerName"] = loggerName;
            this.Options["compress"] = compress;
        }

        /// <summary>
        /// Gets the name of the logger.
        /// </summary>
        /// <value>The name of the logger.</value>
        public string LoggerName
        {
            get
            {
                return this.GetPropertyValue<string>("loggerName");
            }
        }

        /// <summary>
        /// Gets the host where Redis is running.
        /// </summary>
        /// <value>The host.</value>
        public string Host
        {
            get
            {
                return this.GetPropertyValue<string>("host");
            }
        }

        /// <summary>
        /// Gets the port.
        /// </summary>
        /// <value>The port.</value>
        public int Port
        {
            get
            {
                if (this.Options.ContainsKey("port"))
                {
                    return this.GetPropertyValue<int>("port");
                }

                return 6379;
            }
        }

        /// <summary>
        /// Gets the database.
        /// </summary>
        /// <value>The database.</value>
        public int Database
        {
            get
            {

                if (this.Options.ContainsKey("database"))
                {
                    return this.GetPropertyValue<int>("database");
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets the password.
        /// </summary>
        /// <value>The password.</value>
        public string Password
        {
            get
            {
                return this.GetPropertyValue<string>("password");
            }
        }

        /// <summary>
        /// Gets the prefix.
        /// </summary>
        /// <value>The prefix.</value>
        public string Prefix
        {
            get
            {
                return this.GetPropertyValue<string>("prefix");
            }
        }

        /// <summary>
        /// Gets or sets the max number of zipmap entries.
        /// </summary>
        /// <value>The max number of zipmap entries.</value>
        public int MaxZipMapEntries
        {
            get
            {
                if (this.Options.ContainsKey("maxZipMapEntries"))
                {
                    return this.GetPropertyValue<int>("maxZipMapEntries");
                }

                return 512;
            }
        }
    }
}
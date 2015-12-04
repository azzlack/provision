namespace Provision.Providers.Redis
{
    using StackExchange.Redis;
    using System.Reflection;

    public class RedisCacheHandlerConfiguration : BaseCacheHandlerConfiguration
    {
        /// <summary>The server connection.</summary>
        private IConnectionMultiplexer connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCacheHandlerConfiguration"/> class.
        /// </summary>
        public RedisCacheHandlerConfiguration()
            : base("redis", typeof(RedisCacheHandler).GetTypeInfo(), ":", "")
        {
            this.Options["host"] = "localhost";
            this.Options["port"] = 6379;
            this.Options["database"] = 0;
            this.Options["compress"] = false;
            this.Options["maxZipMapEntries"] = 512;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCacheHandlerConfiguration" /> class.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <param name="port">The port.</param>
        /// <param name="database">The database.</param>
        /// <param name="password">The password.</param>
        /// <param name="prefix">The prefix.</param>
        /// <param name="maxZipMapEntries">The maximum number of zip map entries.</param>
        /// <param name="loggerName">The logger name.</param>
        /// <param name="compress">if set to <c>true</c> [compress].</param>
        /// <param name="expireTime">The expire time.</param>
        public RedisCacheHandlerConfiguration(string host = "localhost", int port = 6379, int database = 0, string password = null, string prefix = null, int maxZipMapEntries = 512, string loggerName = null, bool compress = false, string expireTime = "0 0/1 * 1/1 * ? *")
            : base("redis", typeof(RedisCacheHandler).GetTypeInfo(), ":", prefix)
        {
            this.Options["host"] = host;
            this.Options["port"] = port;
            this.Options["database"] = database;
            this.Options["password"] = password;
            this.Options["loggerName"] = loggerName;
            this.Options["compress"] = compress;
            this.Options["maxZipMapEntries"] = maxZipMapEntries;
            this.Options["expireTime"] = expireTime;
        }

        /// <summary>
        /// Gets the name of the logger.
        /// </summary>
        /// <value>The name of the logger.</value>
        public string LoggerName => this.GetPropertyValue<string>("loggerName");

        /// <summary>
        /// Gets the host where Redis is running.
        /// </summary>
        /// <value>The host.</value>
        public string Host => this.GetPropertyValue<string>("host");

        /// <summary>
        /// Gets the port.
        /// </summary>
        /// <value>The port.</value>
        public int Port => this.GetPropertyValue<int>("port");

        /// <summary>
        /// Gets the database.
        /// </summary>
        /// <value>The database.</value>
        public int Database => this.GetPropertyValue<int>("database");

        /// <summary>
        /// Gets the password.
        /// </summary>
        /// <value>The password.</value>
        public string Password => this.GetPropertyValue<string>("password");

        /// <summary>
        /// Gets a value indicating whether data should be compressed.
        /// </summary>
        /// <value><c>true</c> if data should be compressed; otherwise, <c>false</c>.</value>
        public bool Compress => this.GetPropertyValue<bool>("compress");

        /// <summary>
        /// Gets or sets the max number of zipmap entries.
        /// </summary>
        /// <value>The max number of zipmap entries.</value>
        public int MaxZipMapEntries => this.GetPropertyValue<int>("maxZipMapEntries");

        /// <summary>The tag key.</summary>
        public string TagKey => $"{this.Prefix}{this.Separator}{"_tags"}";

        /// <summary>The index key.</summary>
        public string IndexKey => $"{this.Prefix}{this.Separator}{"_keys"}";

        /// <summary>Gets the server connection.</summary>
        /// <value>The server connection.</value>
        public IConnectionMultiplexer Connection
        {
            get
            {
                if (this.connection == null)
                {
                    var configuration = new ConfigurationOptions()
                    {
                        EndPoints =
                            {
                                { this.Host, this.Port }
                            },
                        DefaultDatabase = this.Database,
                        Password = this.Password,
                        AbortOnConnectFail = false,
                        AllowAdmin = true
                    };

                    this.connection = ConnectionMultiplexer.Connect(configuration);
                }

                return this.connection;
            }
        }
    }
}
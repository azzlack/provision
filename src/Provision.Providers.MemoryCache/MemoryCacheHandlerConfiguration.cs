namespace Provision.Providers.MemoryCache
{
    using System.Reflection;

    public class MemoryCacheHandlerConfiguration : BaseCacheHandlerConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryCacheHandlerConfiguration"/> class.
        /// </summary>
        public MemoryCacheHandlerConfiguration()
            : base("mem", typeof(MemoryCacheHandler).GetTypeInfo(), "_", "")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryCacheHandlerConfiguration"/> class.
        /// </summary>
        /// <param name="expireTime">The expire time.</param>
        public MemoryCacheHandlerConfiguration(string expireTime = "0 0/1 * 1/1 * ? *")
            : base("mem", typeof(MemoryCacheHandler).GetTypeInfo(), "_", "")
        {
            this.Options["expireTime"] = expireTime;
        }
    }
}
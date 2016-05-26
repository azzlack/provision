using System;

namespace Provision.Providers.PortableMemoryCache
{
    using System.Reflection;

    public class PortableMemoryCacheHandlerConfiguration : BaseCacheHandlerConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PortableMemoryCacheHandlerConfiguration"/> class.
        /// </summary>
        public PortableMemoryCacheHandlerConfiguration()
            : base("pclmem", typeof(PortableMemoryCacheHandler).GetTypeInfo(), "_", "")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PortableMemoryCacheHandlerConfiguration"/> class.
        /// </summary>
        /// <param name="expireTime">The expire time.</param>
        public PortableMemoryCacheHandlerConfiguration(string expireTime = "0 0/1 * 1/1 * ? *")
            : base("pclmem", typeof(PortableMemoryCacheHandler).GetTypeInfo(), "_", "")
        {
            this.Options["expireTime"] = expireTime;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PortableMemoryCacheHandlerConfiguration" />
        /// class.
        /// </summary>
        /// <param name="expireTime">The expire time.</param>
        public PortableMemoryCacheHandlerConfiguration(TimeSpan expireTime)
            : base("pclmem", typeof(PortableMemoryCacheHandler).GetTypeInfo(), "_", "")
        {
            var fraction = $"{expireTime.Minutes}/{expireTime.Seconds}";

            this.Options["expireTime"] = $"0 {fraction} * * * ?";
        }
    }
}
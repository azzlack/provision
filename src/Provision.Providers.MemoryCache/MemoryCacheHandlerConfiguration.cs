using System;
using Provision.Quartz;

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
        /// <param name="expireTime">The expire time as a cron expression.</param>
        public MemoryCacheHandlerConfiguration(string expireTime = "0 0/1 * 1/1 * ? *")
            : base("mem", typeof(MemoryCacheHandler).GetTypeInfo(), "_", "")
        {
            this.Options["expireTime"] = expireTime;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryCacheHandlerConfiguration"/> class.
        /// </summary>
        /// <param name="expireTime">The expire time.</param>
        public MemoryCacheHandlerConfiguration(TimeSpan expireTime)
            : base("mem", typeof(MemoryCacheHandler).GetTypeInfo(), "_", "")
        {
            var fraction = $"{expireTime.Minutes}/{expireTime.Seconds}";

            this.Options["expireTime"] = $"0 {fraction} * * * ?";
        }

        /// <summary>
        /// Gets or sets the maximum amount if memory in bytes the cache is allowed to use.
        /// The cache will utilize a FIFO pattern if the cache is full.
        /// </summary>
        public double MaxMemory
        {
            get { return this.GetPropertyValue<double>("maxMemory"); }
            set { this.Options["maxMemory"] = value; }
        }
    }
}
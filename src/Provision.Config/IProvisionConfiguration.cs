using System.Collections.Generic;

namespace Provision.Config
{
    using Provision.Interfaces;

    public interface IProvisionConfiguration
    {
        /// <summary>
        /// Gets the cache handlers.
        /// </summary>
        /// <returns>The cache handler.</returns>
        ICacheHandlerCollection GetHandlers();

        /// <summary>Gets the cache handler.</summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The cache handler.</returns>
        ICacheHandlerCollection GetHandlers(IList<ICacheHandlerConfiguration> configuration);

        /// <summary>Gets the configuration.</summary>
        /// <returns>The configuration.</returns>
        IList<ICacheHandlerConfiguration> GetCacheHandlerConfigurations();
    }
}
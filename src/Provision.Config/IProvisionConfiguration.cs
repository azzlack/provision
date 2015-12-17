namespace Provision.Config
{
    using Provision.Interfaces;

    public interface IProvisionConfiguration
    {
        /// <summary>
        /// Gets the cache handler.
        /// </summary>
        /// <returns>The cache handler.</returns>
        ICacheHandler GetHandler();

        /// <summary>Gets the cache handler.</summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The cache handler.</returns>
        ICacheHandler GetHandler(ICacheHandlerConfiguration configuration);

        /// <summary>Gets the configuration.</summary>
        /// <returns>The configuration.</returns>
        ICacheHandlerConfiguration GetConfiguration();
    }
}
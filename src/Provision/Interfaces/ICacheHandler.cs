namespace Provision.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for cache handlers
    /// </summary>
    public interface ICacheHandler : ICacheProvider
    {
        /// <summary>Gets or sets the configuration.</summary>
        /// <value>The configuration.</value>
        ICacheHandlerConfiguration Configuration { get; set; }
    }
}
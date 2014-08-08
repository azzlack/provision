namespace Provision.Interfaces
{
    using System;

    /// <summary>
    /// Interface for items that can expire.
    /// </summary>
    public interface IExpires
    {
        /// <summary>
        /// Gets or sets the expire time.
        /// </summary>
        /// <value>The expire time.</value>
        DateTime Expires { get; set; }
    }
}
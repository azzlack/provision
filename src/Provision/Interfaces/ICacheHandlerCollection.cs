using System.Collections.Generic;

namespace Provision.Interfaces
{
    public interface ICacheHandlerCollection : IEnumerable<ICacheHandler>, ICacheProvider
    {
        /// <summary>Gets the cache handler at the specified index.</summary>
        /// <param name="index">The index.</param>
        /// <returns>The cache handler if it exists.</returns>
        ICacheHandler this[int index] { get; }

        /// <summary>Gets the cache handler with the specified name.</summary>
        /// <param name="name">The name.</param>
        /// <returns>The cache handler if it exists.</returns>
        ICacheHandler this[string name] { get; }
    }
}
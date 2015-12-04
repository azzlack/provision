namespace Provision.Interfaces
{
    using System;

    /// <summary>
    /// Interface for typed cache items.
    /// </summary>
    /// <typeparam name="T">The data type</typeparam>
    public interface ICacheItem<T> : IExpires
    {
        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        T Value { get; }

        /// <summary>
        /// Gets a value indicating whether this instance has value.
        /// </summary>
        /// <value><c>true</c> if this instance has value; otherwise, <c>false</c>.</value>
        bool HasValue { get; }

        /// <summary>
        /// Initializes this instance with the specified value and expiry date.
        /// </summary>
        /// <remarks>Used by the <see cref="ICacheHandler"/> <c>GetAs</c> method.</remarks>
        /// <param name="value">The value.</param>
        /// <param name="expires">The expiry date.</param>
        void Initialize(T value, DateTimeOffset expires);
    }
}
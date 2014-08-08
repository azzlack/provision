namespace Provision.Core.Interfaces
{
    /// <summary>
    /// Interface for typed cache items.
    /// </summary>
    /// <typeparam name="T">The data type</typeparam>
    public interface ICacheItem<out T> : IExpires
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
    }
}
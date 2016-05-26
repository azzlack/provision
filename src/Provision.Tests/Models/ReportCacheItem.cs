namespace Provision.Tests.Models
{
    using Provision.Interfaces;
    using System;

    public class ReportCacheItem : ICacheItem<Report>
    {
        /// <summary>
        /// The report
        /// </summary>
        private Report report;

        /// <summary>
        /// Gets or sets the expires.
        /// </summary>
        /// <value>The expires.</value>
        public DateTimeOffset Expires { get; set; }

        /// <summary>Gets or sets the cache handler name.</summary>
        /// <value>The cache handler name.</value>
        public string CacheHandler { get; set; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        public Report Value
        {
            get
            {
                return this.report;
            }
        }

        /// <summary>Gets the raw value.</summary>
        /// <value>The raw value.</value>
        public object RawValue { get; }

        /// <summary>
        /// Gets a value indicating whether this instance has value.
        /// </summary>
        /// <value><c>true</c> if this instance has value; otherwise, <c>false</c>.</value>
        public bool HasValue
        {
            get
            {
                return this.report != null;
            }
        }

        /// <summary>
        /// Initializes this instance with the specified value and expiry date.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="expires">The expiry date.</param>
        public void Initialize(Report value, DateTimeOffset expires)
        {
            this.report = value;
            this.Expires = expires;
        }
    }
}
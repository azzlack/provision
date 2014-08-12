﻿namespace Provision.Tests.Models
{
    using System;

    using Provision.Interfaces;

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
        public DateTime Expires { get; set; }

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
        public void Initialize(Report value, DateTime expires)
        {
            this.report = value;
            this.Expires = expires;
        }
    }
}
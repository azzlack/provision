namespace Provision.Tests.Models
{
    using System;
    using System.Collections.Generic;

    using Provision.Interfaces;

    public class Report : ICacheItem<Report>
    {
        public IEnumerable<ReportItem> Items { get; set; }

        public DateTime Expires { get; set; }

        public Report Value
        {
            get
            {
                return this;
            }
        }

        public bool HasValue
        {
            get
            {
                return true;
            }
        }
    }
}
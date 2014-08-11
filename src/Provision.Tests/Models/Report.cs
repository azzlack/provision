namespace Provision.Tests.Models
{
    using System.Collections.Generic;

    public class Report
    {
        public IEnumerable<ReportItem> Items { get; set; }
    }
}
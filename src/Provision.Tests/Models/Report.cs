namespace Provision.Tests.Models
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public class Report
    {
        public IEnumerable<ReportItem> Items { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal Rating { get; set; }
    }
}
namespace Provision.Tests.Models
{
    using Newtonsoft.Json;

    public class ReportItem
    {
        public string Key { get; set; }

        public object Data { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal Value { get; set; }
    }
}
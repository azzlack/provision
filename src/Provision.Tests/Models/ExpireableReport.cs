namespace Provision.Tests.Models
{
    using System;

    using Provision.Interfaces;

    public class ExpireableReport : IExpires
    {
        public DateTimeOffset Expires { get; set; }
    }
}
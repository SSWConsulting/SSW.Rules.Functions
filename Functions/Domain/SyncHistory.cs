using System;
using AzureGems.Repository.Abstractions;

namespace SSW.Rules.Functions
{
    public class SyncHistory : BaseEntity
    {
        public string CommitHash { get; set; }
    }
}
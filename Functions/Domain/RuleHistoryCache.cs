using System;
using AzureGems.Repository.Abstractions;

namespace SSW.Rules.Functions
{
    public class RuleHistoryCache : BaseEntity
    {
        public string MarkdownFilePath { get; set; }
        public string ChangedAtDateTime { get; set; }
        public string ChangedByDisplayName { get; set; }
        public string ChangedByEmail { get; set; }
    }
}
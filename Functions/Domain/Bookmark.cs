using System;
using AzureGems.Repository.Abstractions;

namespace SSW.Rules.Functions
{
    public class Bookmark : BaseEntity
    {
        public string RuleGuid { get; set; }
        public string UserId { get; set; }
    }
}
using AzureGems.Repository.Abstractions;

namespace SSW.Rules.AzFuncs.Domain;

public class Bookmark : BaseEntity
{
    public string RuleGuid { get; set; }
    public string UserId { get; set; }
}
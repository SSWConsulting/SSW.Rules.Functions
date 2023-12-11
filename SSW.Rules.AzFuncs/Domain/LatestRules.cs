using AzureGems.Repository.Abstractions;

namespace SSW.Rules.AzFuncs.Domain;

public class LatestRules : BaseEntity
{
    public string CommitHash { get; set; }
    public string RuleUri { get; set; }
    public string RuleName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; }
    public string UpdatedBy { get; set; }
    public string GitHubUsername { get; set; }
}
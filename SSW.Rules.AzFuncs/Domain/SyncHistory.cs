using AzureGems.Repository.Abstractions;

namespace SSW.Rules.AzFuncs.Domain;

public class SyncHistory : BaseEntity
{
    public string CommitHash { get; set; }
}
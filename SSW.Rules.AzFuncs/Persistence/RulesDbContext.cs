using SSW.Rules.AzFuncs.Domain;
using AzureGems.Repository.Abstractions;

namespace SSW.Rules.AzFuncs.Persistence;

public class RulesDbContext : CosmosContext
{
    public IRepository<Reaction> Reactions { get; set; }
    public IRepository<Bookmark> Bookmarks { get; set; }
    public IRepository<SecretContent> SecretContents { get; set; }
    public IRepository<User> Users { get; set; }
    public IRepository<SyncHistory> SyncHistory { get; set; }
    public IRepository<RuleHistoryCache> RuleHistoryCache { get; set; }
    public IRepository<LatestRules> LatestRules { get; set; }
}
using AzureGems.Repository.Abstractions;
using AzureGems.Repository.CosmosDB;

namespace SSW.Rules.Functions
{
    public class RulesDbContext : DbContext
    {
        public IRepository<Reaction> Reactions { get; set; }
        public IRepository<Bookmark> Bookmarks { get; set; }
        public IRepository<SecretContent> SecretContents { get; set; }
        public IRepository<User> Users { get; set; }
        public IRepository<SyncHistory> SyncHistory { get; set; }
        public IRepository<RuleHistoryCache> RuleHistoryCache { get; set; }
    }
}
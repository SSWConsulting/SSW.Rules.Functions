using AzureGems.Repository.Abstractions;
using AzureGems.Repository.CosmosDB;

namespace SSW.Rules.Functions {
    public class RulesDbContext : DbContext 
    {
        public IRepository<LikeDislike> LikeDislikes { get; set; }
    }
}
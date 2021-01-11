using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using OidcApiAuthorization;
using Microsoft.Extensions.Configuration;
using AzureGems.CosmosDB;
using System;
using Microsoft.Extensions.DependencyInjection;
using AzureGems.Repository.CosmosDB;

[assembly: FunctionsStartup(typeof(SSW.Rules.Functions.Startup))]
namespace SSW.Rules.Functions
{
    public class Startup : FunctionsStartup
    {
        private static readonly IConfigurationRoot Configuration = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("appsettings.json", true)
            .AddEnvironmentVariables()
            .Build();

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddOidcApiAuthorization();
            builder.Services.AddCosmosDb(builder =>
            {
                builder
                    .UseConnection(endPoint: Configuration["CosmosDb:Account"], authKey: Configuration["CosmosDb:Key"])
                    .UseDatabase(databaseId: Configuration["CosmosDb:DatabaseName"])
                    .WithSharedThroughput(400)
                    .WithContainerConfig(c =>
                    {
                        c.AddContainer<LikeDislike>(containerId: nameof(LikeDislike), partitionKeyPath: "/id");
                        c.AddContainer<Bookmark>(containerId: nameof(Bookmark), partitionKeyPath: "/id");
                    });
            });
            builder.Services.AddCosmosDbContext<RulesDbContext>();
        }
    }
}
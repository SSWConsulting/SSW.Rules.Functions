using AzureGems.CosmosDB;
using AzureGems.Repository.CosmosDB;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Octokit;
using OidcApiAuthorization;
using SSW.Rules.AzFuncs.Domain;
using SSW.Rules.AzFuncs.Persistence;

var configurationRoot = new ConfigurationBuilder()
    .SetBasePath(Environment.CurrentDirectory)
    .AddJsonFile("appsettings.json", true)
    .AddEnvironmentVariables()
    .Build();

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddOidcApiAuthorization();
        services.AddSingleton<IGitHubClient>(serviceProvider =>
        {
            var client = new GitHubClient(new ProductHeaderValue("SSW.Rules"));
            var githubToken = configurationRoot["GitHub:Token"]; 
            client.Credentials = new Credentials(githubToken);
            return client;
        });
        services.AddCosmosDb(builder =>
        {
            builder
                .Connect(endPoint: configurationRoot["CosmosDb:Account"],
                    authKey: configurationRoot["CosmosDb:Key"])
                .UseDatabase(databaseId: configurationRoot["CosmosDb:DatabaseName"])
                .WithSharedThroughput(400)
                .WithContainerConfig(c =>
                {
                    c.AddContainer<Bookmark>(containerId: nameof(Bookmark), partitionKeyPath: "/id");
                    c.AddContainer<SyncHistory>(containerId: nameof(SyncHistory), partitionKeyPath: "/id");
                    c.AddContainer<RuleHistoryCache>(containerId: nameof(RuleHistoryCache), partitionKeyPath: "/id");
                });
        });
        services.AddCosmosContext<RulesDbContext>();
    })
    .Build();

host.Run();
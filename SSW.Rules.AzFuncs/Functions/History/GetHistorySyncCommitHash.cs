using AzureGems.CosmosDB;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SSW.Rules.AzFuncs.Domain;
using SSW.Rules.AzFuncs.helpers;
using SSW.Rules.AzFuncs.Persistence;

namespace SSW.Rules.AzFuncs.Functions.History;

public class GetHistorySyncCommitHash(ILoggerFactory loggerFactory, RulesDbContext dbContext)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<GetHistorySyncCommitHash>();

    [Function("GetHistorySyncCommitHash")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogWarning($"HTTP trigger function {nameof(GetHistorySyncCommitHash)} received a request.");

        var results = await dbContext.SyncHistory.Query(q => q);

        var syncHash = results.FirstOrDefault();

        string responseMessage = syncHash?.CommitHash ?? string.Empty;
        return req.CreateJsonResponse(responseMessage);
    }
}
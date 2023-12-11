using System.Net;
using AzureGems.CosmosDB;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SSW.Rules.AzFuncs.Domain;
using SSW.Rules.AzFuncs.helpers;
using SSW.Rules.AzFuncs.Persistence;

namespace SSW.Rules.AzFuncs.Functions.History;

public class UpdateHistorySyncCommitHash(ILoggerFactory loggerFactory, RulesDbContext dbContext)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<UpdateHistorySyncCommitHash>();

    [Function("UpdateHistorySyncCommitHash")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
        HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogWarning($"HTTP trigger function {nameof(UpdateHistorySyncCommitHash)} received a request.");

        var formData = await req.ReadFormDataAsync();
        var commitHash = formData["commitHash"];

        if (string.IsNullOrEmpty(commitHash))
        {
            return req.CreateJsonResponse(new
            {
                error = true,
                message = "No commit hash"
            }, HttpStatusCode.BadRequest);
        }


        var results = await dbContext.SyncHistory.Query(q => q);
        var syncHash = results.FirstOrDefault();

        if (syncHash == null)
        {
            await dbContext.SyncHistory.Add(new SyncHistory
            {
                CommitHash = commitHash
            });
        }
        else
        {
            syncHash.CommitHash = commitHash;
            await dbContext.SyncHistory.Update(syncHash);
        }

        return req.CreateResponse(HttpStatusCode.OK);
    }
}
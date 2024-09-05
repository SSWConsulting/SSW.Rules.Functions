using System.Globalization;
using AzureGems.CosmosDB;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SSW.Rules.AzFuncs.Domain;
using SSW.Rules.AzFuncs.helpers;
using SSW.Rules.AzFuncs.Persistence;

namespace SSW.Rules.AzFuncs.Functions.History;

public class GenerateHistoryFileFunction(ILoggerFactory loggerFactory, RulesDbContext dbContext)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<GenerateHistoryFileFunction>();
    private const string DateFormat = "yyyy-MM-ddTHH:mm:sszzz";

    [Function("GenerateHistoryFileFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
        HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogWarning($"HTTP trigger function {nameof(GenerateHistoryFileFunction)} received a request.");

        var results = await dbContext.RuleHistoryCache.Query(q => q);

        List<RuleHistoryData> ruleHistory = [];
        ruleHistory.AddRange(results.Select(history => new RuleHistoryData
        {
            file = history.MarkdownFilePath,
            title = history.Title,
            uri = history.Uri,
            isArchived = history.IsArchived,
            lastUpdated = history.ChangedAtDateTime.ToString(DateFormat, CultureInfo.InvariantCulture),
            lastUpdatedBy = history.ChangedByDisplayName,
            lastUpdatedByEmail = history.ChangedByEmail,
            created = history.CreatedAtDateTime.ToString(DateFormat, CultureInfo.InvariantCulture),
            createdBy = history.CreatedByDisplayName,
            createdByEmail = history.CreatedByEmail
        }));

        var responseMessage = JsonConvert.SerializeObject(ruleHistory);
        return await req.SendJsonResponse(responseMessage);
    }
}
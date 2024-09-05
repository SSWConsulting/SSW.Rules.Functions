using System.Globalization;
using System.Net;
using AzureGems.CosmosDB;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SSW.Rules.AzFuncs.Domain;
using SSW.Rules.AzFuncs.Persistence;

namespace SSW.Rules.AzFuncs.Functions.History;

public class UpdateRuleHistory(ILoggerFactory loggerFactory, RulesDbContext dbContext)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<UpdateRuleHistory>();
    private readonly CultureInfo _provider = CultureInfo.InvariantCulture;
    private const string DateFormat = "yyyy-MM-ddTHH:mm:sszzz";

    [Function("UpdateRuleHistory")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
        HttpRequestData req,
        FunctionContext executionContext)
    {
        try
        {
            _logger.LogInformation($"HTTP trigger function {nameof(UpdateRuleHistory)} received a request.");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<List<RuleHistoryData>>(requestBody);
            if (data == null)
            {
                throw new InvalidOperationException("Request body cannot be parsed.");
            }

            
            foreach (var historyEntry in data)
            {
                var result = await dbContext.RuleHistoryCache.Query(q => q.Where(w => w.MarkdownFilePath == historyEntry.file));
                var historyCache = result.FirstOrDefault();

                _logger.LogInformation($"filename: {historyEntry.file}");
                _logger.LogInformation($"Last updated: {historyEntry.lastUpdated}");
                _logger.LogInformation($"Created: {historyEntry.created}");

                if (historyCache == null)
                {
                    await dbContext.RuleHistoryCache.Add(new RuleHistoryCache
                    {
                        MarkdownFilePath = historyEntry.file,
                        Title = historyEntry.title,
                        Uri = historyEntry.uri,
                        IsArchived = historyEntry.isArchived
                        ChangedAtDateTime = DateTime.ParseExact(historyEntry.lastUpdated, DateFormat, _provider),
                        ChangedByDisplayName = historyEntry.lastUpdatedBy,
                        ChangedByEmail = historyEntry.lastUpdatedByEmail,
                        CreatedAtDateTime = DateTime.ParseExact(historyEntry.created, DateFormat, _provider),
                        CreatedByDisplayName = historyEntry.createdBy,
                        CreatedByEmail = historyEntry.createdByEmail
                    });
                }
                else
                {
                    historyCache.Title = historyEntry.title;
                    historyCache.Uri = historyEntry.uri;
                    historyCache.IsArchived = historyEntry.isArchived;
                    historyCache.ChangedAtDateTime = DateTime.ParseExact(historyEntry.lastUpdated, DateFormat, _provider);
                    historyCache.ChangedByDisplayName = historyEntry.lastUpdatedBy;
                    historyCache.ChangedByEmail = historyEntry.lastUpdatedByEmail;
                    await dbContext.RuleHistoryCache.Update(historyCache);
                }
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in UpdateRuleHistory function.");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }
}
using System.Net;
using AzureGems.CosmosDB;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OidcApiAuthorization.Abstractions;
using OidcApiAuthorization.Models;
using SSW.Rules.AzFuncs.Domain;
using SSW.Rules.AzFuncs.helpers;
using SSW.Rules.AzFuncs.Persistence;

namespace SSW.Rules.AzFuncs.Functions.Bookmarks;

public class BookmarkRuleFunction(
    ILoggerFactory loggerFactory,
    RulesDbContext dbContext,
    IApiAuthorization apiAuthorization)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<BookmarkRuleFunction>();

    [Function("BookmarkRuleFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
        HttpRequestData req,
        FunctionContext executionContext)
    {
        var headers = Converters.ConvertToIHeaderDictionary(req.Headers);
        ApiAuthorizationResult authorizationResult = await apiAuthorization.AuthorizeAsync(headers);

        if (authorizationResult.Failed)
        {
            _logger.LogWarning(authorizationResult.FailureReason);
            return req.CreateJsonErrorResponse(HttpStatusCode.Unauthorized);
        }
        
        _logger.LogWarning($"HTTP trigger function {nameof(BookmarkRuleFunction)} request is authorized.");

        Bookmark data;

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        data = JsonConvert.DeserializeObject<Bookmark>(requestBody);
        var isNull = string.IsNullOrEmpty(data?.RuleGuid) || string.IsNullOrEmpty(data?.UserId);
        if (data == null || isNull)
        {
            return req.CreateJsonErrorResponse(HttpStatusCode.BadRequest, "Request body is empty");
        }

        var results =
        await dbContext.Bookmarks.Query(q => q.Where(w => w.RuleGuid == data.RuleGuid && w.UserId == data.UserId));


        var model = results.FirstOrDefault();

        if (model == null)
        {
            // model = await _client.Bookmarks.Add(data);
            var response = await dbContext.Bookmarks.Add(data);
            if (response.IsDefined())
            {
                model = response;
                _logger.LogInformation("Added new bookmark on rule. Id: {0}", model.Id);
            }
        }
        else
        {
            _logger.LogInformation("Bookmark already exists for user {0}", model.UserId);
            return req.CreateJsonErrorResponse(HttpStatusCode.BadRequest, "This rule has already been bookmarked");
        }

        _logger.LogInformation($"User: {model.UserId}, Rule: {model.RuleGuid}, Id: {model.Id}");

        return req.CreateJsonErrorResponse();
    }
}
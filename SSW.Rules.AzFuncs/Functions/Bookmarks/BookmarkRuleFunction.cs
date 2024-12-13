using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OidcApiAuthorization.Abstractions;
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
        HttpRequestData request,
        FunctionContext executionContext)
    {
        var authorizationResult = await apiAuthorization.AuthorizeAsync(Converters.ConvertToIHeaderDictionary(request.Headers));
        if (authorizationResult.Failed)
        {
            _logger.LogWarning(authorizationResult.FailureReason);
            return request.CreateJsonErrorResponse(HttpStatusCode.Unauthorized);
        }
        _logger.LogWarning($"HTTP trigger function {nameof(BookmarkRuleFunction)} request is authorized.");

        var requestBody = await new StreamReader(request.Body).ReadToEndAsync();
        var data = JsonConvert.DeserializeObject<Bookmark>(requestBody);
        if (data is null || string.IsNullOrEmpty(data?.RuleGuid) || string.IsNullOrEmpty(data?.UserId))
        {
            return request.CreateJsonErrorResponse(HttpStatusCode.BadRequest, "Request body is empty");
        }

        var results = await dbContext
            .Bookmarks
            .Query(q => q.Where(w => w.RuleGuid == data.RuleGuid && w.UserId == data.UserId));

        var existingBookmark = results.FirstOrDefault();
        Bookmark result;

        if (existingBookmark is null)
        {
            result = await dbContext.Bookmarks.Add(data);
            _logger.LogInformation($"User: {result.UserId} bookmarked this rule: {result.Id} successfully");
            return request.CreateJsonResponse(result, HttpStatusCode.Created);
        }
        else
        {
            _logger.LogInformation($"Bookmark already exists for user {existingBookmark.UserId}");
            return request.CreateJsonErrorResponse(HttpStatusCode.BadRequest, "This rule has already been bookmarked");
        }
    }
}
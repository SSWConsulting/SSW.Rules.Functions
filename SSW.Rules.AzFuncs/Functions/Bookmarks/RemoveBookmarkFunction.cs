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

public class RemoveBookmarkFunction(
    ILoggerFactory loggerFactory,
    RulesDbContext dbContext,
    IApiAuthorization apiAuthorization)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<RemoveBookmarkFunction>();

    [Function("RemoveBookmarkFunction")]
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
        _logger.LogWarning("HTTP trigger function {0} request is authorized.", nameof(RemoveBookmarkFunction));

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
        if (existingBookmark is null)
        {
            _logger.LogInformation("No bookmark exists for User {0} and Rule {1}", data.UserId, data.RuleGuid);
            return request.CreateJsonResponse(new
            {
                error = true,
                message = "No bookmark exists for this rule and user",
                data.UserId,
                data.RuleGuid,
            }, HttpStatusCode.NotFound);
        }

        await dbContext.Bookmarks.Delete(existingBookmark);
        _logger.LogInformation("User: {0}, Rule: {1}, Id: {2}", existingBookmark.UserId, existingBookmark.RuleGuid, existingBookmark.Id);
        return request.CreateResponse(HttpStatusCode.NoContent);
    }
}
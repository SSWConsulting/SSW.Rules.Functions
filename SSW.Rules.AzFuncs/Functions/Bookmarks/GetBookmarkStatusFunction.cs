using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SSW.Rules.AzFuncs.helpers;
using SSW.Rules.AzFuncs.Persistence;

namespace SSW.Rules.AzFuncs.Functions.Bookmarks;

public class GetBookmarkStatusFunction(ILoggerFactory loggerFactory, RulesDbContext dbContext)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<GetBookmarkStatusFunction>();

    [Function("GetBookmarkStatusFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogWarning($"HTTP trigger function {nameof(GetBookmarkStatusFunction)} received a request.");

        var ruleGuid = req.Query["rule_guid"];
        var userId = req.Query["user_id"];

        if (string.IsNullOrEmpty(userId))
        {
            return req.CreateJsonResponse(new
            {
                error = true,
                message = "Missing or empty user_id param",
            }, HttpStatusCode.BadRequest);
        }

        if (string.IsNullOrEmpty(ruleGuid))
        {
            return req.CreateJsonResponse(new
            {
                error = true,
                message = "Missing or empty rule_guid param",
            }, HttpStatusCode.BadRequest);
        }

        _logger.LogInformation("Checking for bookmark on rule: {0} and user: {1}", ruleGuid, userId);
        var bookmarks = await dbContext
            .Bookmarks
            .Query(q => q.Where(w => w.RuleGuid == ruleGuid && w.UserId == userId));
        
        var bookmarkStatus = bookmarks.Any();
        if (bookmarkStatus)
        {
            _logger.LogInformation($"Could not find results for rule id: {ruleGuid}, and user: {userId}");
        }

        return req.CreateJsonResponse(new
        {
            error = false,
            message = "",
            bookmarkStatus,
        });
    }
}
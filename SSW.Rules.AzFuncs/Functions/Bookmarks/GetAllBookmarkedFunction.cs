using System.Net;
using AzureGems.CosmosDB;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SSW.Rules.AzFuncs.Domain;
using SSW.Rules.AzFuncs.helpers;
using SSW.Rules.AzFuncs.Persistence;

namespace SSW.Rules.AzFuncs.Functions.Bookmarks;

public class GetAllBookmarkedFunction(
    ILoggerFactory loggerFactory,
    RulesDbContext dbContext)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<GetAllBookmarkedFunction>();

    [Function("GetAllBookmarkedFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogWarning($"HTTP trigger function {nameof(GetAllBookmarkedFunction)} received a request.");

        var userId = req.Query["user_id"];

        if (string.IsNullOrEmpty(userId))
        {
            return req.CreateJsonResponse(new
            {
                error = true,
                message = "Missing or empty user_id param"
            }, HttpStatusCode.BadRequest);
        }

        _logger.LogInformation("Checking for bookmarks by user: {0}", userId);

        var bookmarks = await dbContext.Bookmarks.Query<Bookmark>(q => q.Where(w => w.UserId == userId));
        // var userBookMarks = await bookmarks.Get<Bookmark>(q => q.UserId == userId);

        var bookmarksResult = bookmarks.ToList();
        if (bookmarksResult.Count != 0)
        {
            return req.CreateJsonResponse(new
            {
                error = false,
                message = "",
                bookmarkedRules = bookmarksResult,
            });
        }

        _logger.LogInformation($"Could not find results for user: {userId}");
        return req.CreateJsonResponse(new
        {
            error = true,
            message = $"Could not find results for user: {userId}",
            bookmarkedRules = bookmarksResult
        }, HttpStatusCode.NotFound);
    }
}
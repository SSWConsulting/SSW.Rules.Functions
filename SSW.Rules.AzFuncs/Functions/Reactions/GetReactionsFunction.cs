using System.Net;
using AzureGems.CosmosDB;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SSW.Rules.AzFuncs.Domain;
using SSW.Rules.AzFuncs.helpers;
using SSW.Rules.AzFuncs.Persistence;

namespace SSW.Rules.AzFuncs.Functions.Reactions;

public class GetReactionsFunction(ILoggerFactory loggerFactory, RulesDbContext dbContext)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<GetReactionsFunction>();

    [Function("GetReactionsFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
        HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogWarning($"HTTP trigger function {nameof(GetReactionsFunction)} received a request.");

        var ruleGuid = req.Query["rule_guid"];
        var userId = req.Query["user_id"];

        if (ruleGuid == null)
        {
            return req.CreateJsonResponse(new
            {
                error = true,
                message = "Missing RuleGuid param",
            }, HttpStatusCode.BadRequest);
        }

        var likes = await dbContext.Reactions.Query(q => q.Where(w => w.RuleGuid == ruleGuid));

        var likesList = likes.ToList();
        if (!likesList.Any())
        {
            return req.CreateJsonResponse(new
            {
                error = true,
                message = "Could not find results for rule id: " + ruleGuid,
            }, HttpStatusCode.NotFound);
        }

        // Group and count reactions by type
        var results = likesList
            .GroupBy(l => l.Type)
            .Select(g => new
            {
                Type = g.Key,
                Count = g.Count()
            }).ToList();

        ReactionType? userStatus = null;
        if (string.IsNullOrEmpty(userId))
        {
            return req.CreateJsonResponse(new
            {
                error = false,
                message = "",
                superLikeCount = results.FirstOrDefault(r => r.Type == ReactionType.SuperLike)?.Count ?? 0,
                likeCount = results.FirstOrDefault(r => r.Type == ReactionType.Like)?.Count ?? 0,
                dislikeCount = results.FirstOrDefault(r => r.Type == ReactionType.Dislike)?.Count ?? 0,
                superDislikeCount = results.FirstOrDefault(r => r.Type == ReactionType.SuperDislike)?.Count ?? 0,
                userStatus
            });
        }

        var userReaction = likesList.FirstOrDefault(w => w.UserId == userId);
        userStatus = userReaction?.Type;
        _logger.LogInformation("Found reaction for user: '{0}' reaction: '{1}'", userId, userStatus);

        return req.CreateJsonResponse(new
        {
            error = false,
            message = "",
            superLikeCount = results.FirstOrDefault(r => r.Type == ReactionType.SuperLike)?.Count ?? 0,
            likeCount = results.FirstOrDefault(r => r.Type == ReactionType.Like)?.Count ?? 0,
            dislikeCount = results.FirstOrDefault(r => r.Type == ReactionType.Dislike)?.Count ?? 0,
            superDislikeCount = results.FirstOrDefault(r => r.Type == ReactionType.SuperDislike)?.Count ?? 0,
            userStatus
        });
    }
}
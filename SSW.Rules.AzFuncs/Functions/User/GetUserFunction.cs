using System.Net;
using AzureGems.CosmosDB;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OidcApiAuthorization.Abstractions;
using OidcApiAuthorization.Models;
using SSW.Rules.AzFuncs.helpers;
using SSW.Rules.AzFuncs.Persistence;
using DomainUser = SSW.Rules.AzFuncs.Domain.User;

namespace SSW.Rules.AzFuncs.Functions.User;

public class GetUserFunction(
    ILoggerFactory loggerFactory,
    IApiAuthorization apiAuthorization,
    RulesDbContext dbContext)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<GetUserFunction>();

    [Function("GetUserFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, Route = null)]
        HttpRequestData req,
        FunctionContext executionContext)
    {
        ApiAuthorizationResult authorizationResult =
            await apiAuthorization.AuthorizeAsync(Converters.ConvertToIHeaderDictionary(req.Headers));

        if (authorizationResult.Failed)
        {
            _logger.LogWarning(authorizationResult.FailureReason);
            var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            return unauthorizedResponse;
        }

        _logger.LogInformation($"HTTP trigger function {nameof(GetUserFunction)} request is authorized.");

        var userId = req.Query["user_id"];

        if (string.IsNullOrEmpty(userId))
        {
            return req.CreateJsonResponse(new
            {
                message = "Missing or empty user_id param",
            }, HttpStatusCode.BadRequest);
        }

        var result = await dbContext.Users.Query(q => q.Where(w => w.UserId == userId));
        var user = result.FirstOrDefault();

        if (user == null)
        {
            _logger.LogInformation($"Could not find results for user: {userId}");
            return req.CreateJsonResponse(new
            {
                message = "User " + userId + " was not found",
            }, HttpStatusCode.BadRequest);
        }

        bool commentsConnected = !string.IsNullOrEmpty(user?.CommentsUserId);

        return req.CreateJsonResponse(new
        {
            user, commentsConnected,
        });
    }
}
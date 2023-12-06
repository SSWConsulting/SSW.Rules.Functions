using System.Net;
using AzureGems.CosmosDB;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OidcApiAuthorization.Abstractions;
using OidcApiAuthorization.Models;
using SSW.Rules.AzFuncs.helpers;
using SSW.Rules.AzFuncs.Persistence;
using DomainUser = SSW.Rules.AzFuncs.Domain.User;

namespace SSW.Rules.AzFuncs.Functions.User;

public class ConnectUserToCommentsFunction(
    ILoggerFactory loggerFactory,
    RulesDbContext dbContext,
    IApiAuthorization apiAuthorization)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ConnectUserToCommentsFunction>();

    [Function("ConnectUserToCommentsFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
        HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogWarning($"HTTP trigger function {nameof(ConnectUserToCommentsFunction)} received a request.");

        ApiAuthorizationResult authorizationResult =
            await apiAuthorization.AuthorizeAsync(Converters.ConvertToIHeaderDictionary(req.Headers));

        if (authorizationResult.Failed)
        {
            _logger.LogWarning(authorizationResult.FailureReason);
            return req.CreateResponse(HttpStatusCode.Unauthorized);
        }

        _logger.LogWarning($"HTTP trigger function {nameof(ConnectUserToCommentsFunction)} request is authorized.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonConvert.DeserializeObject<DomainUser>(requestBody);

        if (data == null || string.IsNullOrEmpty(data.CommentsUserId) || string.IsNullOrEmpty(data.UserId))
        {
            return req.CreateJsonResponse(
                new { message = "Request body is empty or is missing CommentsUserId or UserId fields" },
                HttpStatusCode.BadRequest);
        }


        var existingUser = await dbContext.Users.Query(q => q.Where(w => w.UserId == data.UserId));
        var user = existingUser.FirstOrDefault();

        if (user == null)
        {
            await dbContext.Users.Add(data);
            return req.CreateResponse(HttpStatusCode.OK);
        }

        if (user.CommentsUserId == data.CommentsUserId)
        {
            return req.CreateJsonResponse(new { message = "User already has the same comments account associated" },
                HttpStatusCode.OK);
        }

        if (!string.IsNullOrEmpty(user.CommentsUserId))
        {
            return req.CreateJsonResponse(new { message = "Different comments account already connected" },
                HttpStatusCode.Conflict);
        }

        var existingCommentsId =
            await dbContext.Users.Query(q => q.Where(w => w.CommentsUserId == data.CommentsUserId));

        if (existingCommentsId.Any())
        {
            return req.CreateJsonResponse(
                new { message = "This comments account is already being used by another user" },
                HttpStatusCode.Conflict);
        }

        user.CommentsUserId = data.CommentsUserId;
        await dbContext.Users.Update(user);

        return req.CreateResponse(HttpStatusCode.OK);
    }
}
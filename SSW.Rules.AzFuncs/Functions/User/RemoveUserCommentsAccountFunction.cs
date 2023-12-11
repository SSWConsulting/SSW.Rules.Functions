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

public class RemoveUserCommentsAccountFunction(
    ILoggerFactory loggerFactory,
    IApiAuthorization apiAuthorization,
    RulesDbContext dbContext)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<RemoveUserCommentsAccountFunction>();

    [Function("RemoveUserCommentsAccountFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
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

        _logger.LogInformation(
            $"HTTP trigger function {nameof(RemoveUserCommentsAccountFunction)} request is authorized.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        DomainUser data = JsonConvert.DeserializeObject<DomainUser>(requestBody);

        if (data == null || string.IsNullOrEmpty(data?.UserId))
        {
            return req.CreateJsonResponse(new
            {
                message = "Request body is empty or UserId is missing",
            }, HttpStatusCode.BadRequest);
        }

        var user = await dbContext.Users.Query(q => q.Where(w => w.UserId == data.UserId));
        var model = user.FirstOrDefault();

        if (model == null)
        {
            return req.CreateJsonResponse(new
            {
                message = "User does not exist"
            }, HttpStatusCode.BadRequest);
        }

        model.CommentsUserId = null;
        await dbContext.Users.Update(model);

        return req.CreateResponse(HttpStatusCode.OK);
    }
}
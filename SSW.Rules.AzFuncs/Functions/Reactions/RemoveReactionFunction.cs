using System.Net;
using AzureGems.CosmosDB;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OidcApiAuthorization.Abstractions;
using OidcApiAuthorization.Models;
using SSW.Rules.AzFuncs.Domain;
using SSW.Rules.AzFuncs.helpers;
using SSW.Rules.AzFuncs.Persistence;

namespace SSW.Rules.AzFuncs.Functions.Reactions;

public class RemoveReactionFunction(
    ILoggerFactory loggerFactory,
    RulesDbContext dbContext,
    IApiAuthorization apiAuthorization)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<RemoveReactionFunction>();

    [Function("RemoveReactionFunction")]
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
            return req.CreateResponse(HttpStatusCode.Unauthorized);
        }

        _logger.LogWarning($"HTTP trigger function {nameof(RemoveReactionFunction)} request is authorized.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        Reaction data = JsonConvert.DeserializeObject<Reaction>(requestBody);

        bool isNull = string.IsNullOrEmpty(data?.RuleGuid) || string.IsNullOrEmpty(data?.UserId) || data?.Type == null;
        if (data == null || isNull)
        {
            return req.CreateJsonResponse(new
            {
                error = true,
                message = "Request body is empty",
            }, HttpStatusCode.BadRequest);
        }

        var results =
            await dbContext.Reactions.Query(q => q.Where(w => w.RuleGuid == data.RuleGuid && w.UserId == data.UserId));
        var model = results.FirstOrDefault();

        if (model == null)
        {
            _logger.LogInformation("No reaction exists for User {0} and Rule {1}", data.UserId, data.RuleGuid);
            return req.CreateJsonResponse(new
            {
                error = true,
                message = "No reaction exists for this rule and user",
                data.UserId,
                data.RuleGuid,
            }, HttpStatusCode.NotFound);
        }

        await dbContext.Reactions.Delete(model);
        _logger.LogInformation($"User: {model.UserId}, Rule: {model.RuleGuid}, Id: {model.Id}");

        return req.CreateJsonResponse(new
        {
            error = false,
            message = ""
        });
    }
}
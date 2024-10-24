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

public class ReactFunction(
    ILoggerFactory loggerFactory,
    RulesDbContext dbContext,
    IApiAuthorization apiAuthorization)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ReactFunction>();

    [Function("ReactFunction")]
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
            return req.CreateJsonErrorResponse(HttpStatusCode.Unauthorized);
        }

        _logger.LogWarning($"HTTP trigger function {nameof(ReactFunction)} request is authorized.");

        Reaction data;

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        data = JsonConvert.DeserializeObject<Reaction>(requestBody);

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
        var reactionsList = results.ToList();
        var model = reactionsList.FirstOrDefault();
        _logger.LogInformation($"reactions on same rule by same user: {reactionsList.Count()}");

        if (model == null)
        {
            model = await dbContext.Reactions.Add(data);
            _logger.LogInformation("Added new reaction. Id: {0}", model.Id);
        }
        else
        {
            _logger.LogInformation("Reaction already exists for user {0}", model.UserId);

            if (model.Type != data.Type)
            {
                model.Type = data.Type;
                model = await dbContext.Reactions.Update(model);
                _logger.LogInformation("Updated reaction to " + model.Type);
            }
            else
            {
                _logger.LogInformation("Reaction is the same. No change");
            }
        }

        _logger.LogInformation($"User: {model.UserId}, Type: {model.Type}, Rule: {model.RuleGuid}, Id: {model.Id}");

        return req.CreateJsonResponse(new
        {
            error = false,
            message = "",
            reaction = model.Type
        });
    }
}
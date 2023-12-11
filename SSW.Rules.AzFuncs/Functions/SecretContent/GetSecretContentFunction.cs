using System.Net;
using AzureGems.CosmosDB;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OidcApiAuthorization.Abstractions;
using OidcApiAuthorization.Models;
using SSW.Rules.AzFuncs.helpers;
using SSW.Rules.AzFuncs.Persistence;

namespace SSW.Rules.AzFuncs.Functions.SecretContent;

public class GetSecretContentFunction(
    ILoggerFactory loggerFactory,
    RulesDbContext dbContext,
    IApiAuthorization apiAuthorization)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<GetSecretContentFunction>();

    [Function("GetSecretContentFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")]
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

        _logger.LogInformation($"C# HTTP trigger function {nameof(GetSecretContentFunction)} processed a request.");

        var secretContentId = req.Query["id"];

        if (string.IsNullOrEmpty(secretContentId))
        {
            return req.CreateJsonResponse(new
            {
                error = true,
                message = "Missing or empty id param",
            }, HttpStatusCode.BadRequest);
        }

        var secretContents = await dbContext.SecretContents.Query(q => q.Where(w => w.Id == secretContentId));
        var model = secretContents.FirstOrDefault();

        if (model == null)
        {
            return req.CreateJsonResponse(new
            {
                error = true,
                message = $"Could not find content with id: {secretContentId}"
            }, HttpStatusCode.NotFound);
        }

        return req.CreateJsonResponse(new
        {
            error = false,
            message = "",
            Content = model
        });
    }
}
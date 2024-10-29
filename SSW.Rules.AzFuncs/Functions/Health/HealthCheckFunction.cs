using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OidcApiAuthorization.Abstractions;
using OidcApiAuthorization.Models;
using SSW.Rules.AzFuncs.Domain;
using SSW.Rules.AzFuncs.helpers;
using SSW.Rules.AzFuncs.Persistence;

namespace SSW.Rules.AzFuncs.Functions.Health;

public class HealthCheckFunction(
    ILoggerFactory loggerFactory,
    IApiAuthorization apiAuthorization,
    RulesDbContext dbContext)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<HealthCheckFunction>();

    [Function("HealthCheckFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogWarning($"HTTP trigger function {nameof(HealthCheckFunction)} received a request.");

        HealthCheckResult result = await apiAuthorization.HealthCheckAsync();

        var bookmarkEntity = await dbContext.Bookmarks.Add(new Bookmark
        {
            RuleGuid = "exampleRule123",
            UserId = "exampleUser123",
            Discriminator = typeof(Bookmark).FullName
        });

        var secretContentEntity = await dbContext.SecretContents.Add(new Domain.SecretContent
        {
            OrganisationId = "123123",
            Content = "Don't tell anyone about this",
            Discriminator = typeof(Domain.SecretContent).FullName
        });

        if (result.IsHealthy && bookmarkEntity != null && secretContentEntity != null)
        {
            _logger.LogWarning($"{nameof(HealthCheckFunction)} health check OK.");
        }
        else
        {
            _logger.LogError(
                $"{nameof(HealthCheckFunction)} health check failed."
                + $" {nameof(HealthCheckResult)}: {JsonConvert.SerializeObject(result)}"
            );
        }

        return req.CreateJsonResponse(result);
    }
}
using System.Net;
using AzureGems.CosmosDB;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SSW.Rules.AzFuncs.helpers;
using SSW.Rules.AzFuncs.Persistence;
using DomainUser = SSW.Rules.AzFuncs.Domain.User;

namespace SSW.Rules.AzFuncs.Functions.User;

public class GetOrganisationsFunction(ILoggerFactory loggerFactory, RulesDbContext dbContext)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<GetOrganisationsFunction>();

    [Function("GetOrganisationsFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
        HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogWarning($"HTTP trigger function {nameof(GetOrganisationsFunction)} received a request.");

        var userId = req.Query["user_id"];

        if (string.IsNullOrEmpty(userId))
        {
            return req.CreateJsonResponse(new { error = true, message = "Missing or empty user_id param" },
                HttpStatusCode.BadRequest);
        }


        var organisations = await dbContext.Users.Query(q => q.Where(w => w.UserId == userId));

        var usersList = organisations.ToList();
        if (usersList.Count != 0)
        {
            return req.CreateJsonResponse(new { error = false, message = "", organisations = usersList });
        }

        _logger.LogInformation($"Could not find results for user: {userId}");
        return req.CreateJsonResponse(new { error = true, message = $"Could not find results for user: {userId}" },
            HttpStatusCode.NotFound);
    }
}
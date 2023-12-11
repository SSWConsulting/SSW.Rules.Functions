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

namespace SSW.Rules.AzFuncs.Functions.User;

public class AddUserOrganisationFunction(
    ILoggerFactory loggerFactory,
    IApiAuthorization apiAuthorization,
    RulesDbContext dbContext)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<AddUserOrganisationFunction>();

    [Function("AddUserOrganisationFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
        HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogWarning($"HTTP trigger function {nameof(AddUserOrganisationFunction)} received a request.");

        ApiAuthorizationResult authorizationResult =
            await apiAuthorization.AuthorizeAsync(Converters.ConvertToIHeaderDictionary(req.Headers));

        if (authorizationResult.Failed)
        {
            _logger.LogWarning(authorizationResult.FailureReason);
            return req.CreateResponse(HttpStatusCode.Unauthorized);
        }

        _logger.LogWarning($"HTTP trigger function {nameof(AddUserOrganisationFunction)} request is authorized.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonConvert.DeserializeObject<Domain.User>(requestBody);

        if (data == null || string.IsNullOrEmpty(data.UserId))
        {
            return req.CreateJsonResponse(new
                {
                    error = true,
                    message = "Request body is empty"
                },
                HttpStatusCode.BadRequest);
        }

        var existingOrganisation = await dbContext.Users.Query(q =>
            q.Where(w => w.UserId == data.UserId && w.OrganisationId == data.OrganisationId));
        var model = existingOrganisation.FirstOrDefault();

        if (model != null)
        {
            return req.CreateJsonResponse(new
                {
                    error = true,
                    message = "User is already assigned to this organisation"
                },
                HttpStatusCode.BadRequest);
        }

        var result = await dbContext.Users.Add(data);

        return req.CreateJsonResponse(new { error = false, message = "", user = result }, HttpStatusCode.OK);
    }
}
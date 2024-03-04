using System.Net;
using System.Web;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SSW.Rules.AzFuncs.helpers;
using SSW.Rules.AzFuncs.Persistence;

namespace SSW.Rules.AzFuncs.Functions.Widget;

public class GetLatestRules(ILoggerFactory loggerFactory, RulesDbContext context)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<GetLatestRules>();

    [Function("GetLatestRules")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
        HttpRequestData req,
        FunctionContext executionContext)
    {
        var query = HttpUtility.ParseQueryString(req.Url.Query);
        var skip = int.Parse(query["skip"] ?? "0");
        var take = int.Parse(query["take"] ?? "10");
        var githubUsername = query["githubUsername"]?.Trim('\"') ?? String.Empty;

        _logger.LogInformation($"Fetching latest rules, Skip: {skip}, Take: {take}, GitHubUsername: {githubUsername}");

        var rules = await context.LatestRules.GetAll();
        var filteredRules = rules
            .Where(r => string.IsNullOrEmpty(githubUsername) || r.GitHubUsername == githubUsername ||
                        r.CreatedBy == githubUsername || r.UpdatedBy == githubUsername)
            .GroupBy(r => r.RuleGuid)
            .Select(group => group.First())
            .DistinctBy(r => r.RuleGuid)
            .OrderByDescending(r => r.UpdatedAt)
            .Skip(skip)
            .Take(take);

        var filteredRulesList = filteredRules.ToList();
        return filteredRulesList.Count == 0
            ? req.CreateJsonErrorResponse(HttpStatusCode.NotFound, "Not Found")
            : req.CreateJsonResponse(filteredRules);
    }
}

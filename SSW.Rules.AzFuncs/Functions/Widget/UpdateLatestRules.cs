using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Octokit;
using SSW.Rules.AzFuncs.Domain;
using SSW.Rules.AzFuncs.helpers;
using SSW.Rules.AzFuncs.Persistence;

namespace SSW.Rules.AzFuncs.Functions.Widget;

public class UpdateLatestRules(ILoggerFactory loggerFactory, IGitHubClient gitHubClient, RulesDbContext context)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<UpdateLatestRules>();

    [Function("UpdateLatestRules")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
        HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogInformation("Processing UpdateLatestRules request.");
        try
        {
            // TODO: Get these from the ENV
            const string repositoryOwner = "SSWConsulting";
            const string repositoryName = "SSW.Rules.Content";
            var request = new PullRequestRequest
            {
                State = ItemStateFilter.Closed,
            };
            var apiOptions = new ApiOptions
            {
                PageSize = 50,
                PageCount = 1,
                StartPage = 1
            };

            var pullRequests =
                await gitHubClient.PullRequest.GetAllForRepository(repositoryOwner, repositoryName, request,
                    apiOptions);

            if (pullRequests == null || !pullRequests.Any())
            {
                throw new Exception("No Pull Requests found");
            }

            var syncHistoryHash = await context.SyncHistory.GetAll();
            var existingCommitHashes = new HashSet<string>(syncHistoryHash.Select(sh => sh.CommitHash));
            HttpClient httpClient = new HttpClient();
            var newRules = new List<LatestRules>();
            var updatedCount = 0;


            foreach (var pr in pullRequests)
            {
                _logger.LogInformation($"Scanning PR {pr.Number}");
                if (existingCommitHashes.Contains(pr.MergeCommitSha)) break;
                if (!pr.Merged) continue;

                var files = await gitHubClient.PullRequest.Files(repositoryOwner, repositoryName, pr.Number);
                if (files.Count > 100) // Skips big PRs as these will fail https://github.com/SSWConsulting/SSW.Rules/issues/1367
                {
                    _logger.LogInformation($"Skipping PR {pr.Number} because there are too many files changed");
                    continue;
                };

                foreach (var file in files)
                {
                    if (!file.FileName.Contains("rule.md")) continue;

                    var response = await httpClient.GetAsync(file.RawUrl);
                    if (!response.IsSuccessStatusCode) continue;

                    var fileContent = await response.Content.ReadAsStringAsync();
                    var frontMatter = Utils.ParseFrontMatter(fileContent);

                    if (frontMatter is null) continue;

                    var ruleHistoryCache = await context.RuleHistoryCache.Query(rhc =>
                        rhc.Where(w => w.MarkdownFilePath == file.FileName));
                    var foundRule = ruleHistoryCache.FirstOrDefault();

                    var searchLatest =
                        await context.LatestRules.Query(q => q.Where(w => w.RuleGuid == frontMatter.Guid));
                    var latestFound = searchLatest.FirstOrDefault();

                    if (latestFound is not null)
                    {
                        UpdateLatestRule(latestFound, pr, frontMatter);
                        updatedCount++;
                        continue;
                    }

                    var rule = CreateLatestRule(pr, frontMatter, foundRule);
                    newRules.Add(rule);
                    updatedCount++;
                }
            }

            foreach (var rule in newRules)
            {
                await context.LatestRules.Add(rule);
            }

            _logger.LogInformation($"Updated Latest rules with {updatedCount} new entries.");

            return req.CreateJsonResponse(new
            { message = $"Latest rules updated successfully with {updatedCount} new entries." });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Something went wrong: {ex.Message}");
            return req.CreateJsonErrorResponse(HttpStatusCode.BadRequest, ex.Message);
        }
    }

    private void UpdateLatestRule(LatestRules latestRule, PullRequest pr, FrontMatter frontMatter)
    {
        latestRule.CommitHash = pr.MergeCommitSha;
        latestRule.RuleUri = frontMatter.Uri;
        latestRule.RuleName = frontMatter.Title;
        latestRule.UpdatedAt = pr.UpdatedAt.UtcDateTime;
        latestRule.UpdatedBy = pr.User.Login;
        latestRule.GitHubUsername = pr.User.Login;

        context.LatestRules.Update(latestRule);
    }

    private static LatestRules CreateLatestRule(PullRequest pr, FrontMatter frontMatter, RuleHistoryCache? foundRule)
    {
        var rule = new LatestRules
        {
            CommitHash = pr.MergeCommitSha,
            RuleGuid = frontMatter.Guid,
            RuleUri = frontMatter.Uri,
            RuleName = frontMatter.Title,
            CreatedAt = foundRule?.CreatedAtDateTime ?? frontMatter.Created,
            UpdatedAt = pr.UpdatedAt.UtcDateTime,
            CreatedBy = foundRule?.CreatedByDisplayName ?? pr.User.Location,
            UpdatedBy = foundRule?.ChangedByDisplayName ?? pr.User.Login,
            GitHubUsername = pr.User.Login
        };

        return rule;
    }
}
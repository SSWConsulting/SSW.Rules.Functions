using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Octokit;
using Octokit.Internal;
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
                PageCount = 1, // Only retrieve the first page of results
                StartPage = 1
            };

            var pullRequests =
                await gitHubClient.PullRequest.GetAllForRepository(repositoryOwner, repositoryName, request,
                    apiOptions);
            var syncHistory = await context.SyncHistory.GetAll();
            var existingCommitHashes = new HashSet<string>(syncHistory.Select(sh => sh.CommitHash));
            HttpClient httpClient = new HttpClient();
            var newRules = new List<LatestRules>();
            foreach (var pr in pullRequests)
            {
                if (existingCommitHashes.Contains(pr.MergeCommitSha)) break;
                if (!pr.Merged) continue;

                var files = await gitHubClient.PullRequest.Files(repositoryOwner, repositoryName, pr.Number);
                foreach (var file in files)
                {
                    if (file.FileName.Contains("rule.md"))
                    {
                        var response = await httpClient.GetAsync(file.RawUrl);
                        if (!response.IsSuccessStatusCode) continue;

                        var fileContent = await response.Content.ReadAsStringAsync();
                        var frontMatter = Utils.ParseFrontMatter(fileContent);

                        if (frontMatter is null) continue;

                        var ruleHistoryCache = await context.RuleHistoryCache.Query(rhc =>
                            rhc.Where(w => w.MarkdownFilePath == file.FileName));
                        var foundRule = ruleHistoryCache.FirstOrDefault();

                        if (foundRule is not null)
                        {
                            var rule = new LatestRules
                            {
                                CommitHash = pr.MergeCommitSha,
                                RuleUri = frontMatter.Uri,
                                RuleName = frontMatter.Title,
                                CreatedAt = foundRule.CreatedAtDateTime,
                                UpdatedAt = pr.UpdatedAt.UtcDateTime,
                                CreatedBy = foundRule.CreatedByDisplayName,
                                UpdatedBy = foundRule.ChangedByDisplayName,
                                GitHubUsername = pr.User.Login
                            };
                            newRules.Add(rule);
                        }
                        else
                        {
                            var rule = new LatestRules
                            {
                                CommitHash = pr.MergeCommitSha,
                                RuleUri = frontMatter.Uri,
                                RuleName = frontMatter.Title,
                                CreatedAt = frontMatter.Created,
                                UpdatedAt = pr.UpdatedAt.UtcDateTime,
                                CreatedBy = pr.User.Location,
                                UpdatedBy = pr.User.Login,
                                GitHubUsername = pr.User.Login
                            };
                            newRules.Add(rule);
                        }
                    }
                }
            }

            foreach (var rule in newRules)
            {
                await context.LatestRules.Add(rule);
            }

            _logger.LogInformation($"Updated Latest rules with {newRules.Count} new entries.");

            return req.CreateJsonResponse(new
                { message = $"Latest rules updated successfully with {newRules.Count} new entries." });

        }
        catch (Exception ex)
        {
            _logger.LogError($"Something went wrong: {ex.Message}");
            return req.CreateJsonErrorResponse(HttpStatusCode.BadRequest, ex.Message);
        }
    }
}
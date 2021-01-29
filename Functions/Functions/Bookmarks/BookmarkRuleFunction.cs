using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OidcApiAuthorization.Abstractions;
using OidcApiAuthorization.Models;

namespace SSW.Rules.Functions
{
    public class BookmarkRuleFunction
    {
        private readonly RulesDbContext _dbContext;
        private readonly IApiAuthorization _apiAuthorization;

        public BookmarkRuleFunction(RulesDbContext dbContext, IApiAuthorization apiAuthorization)
        {
            _dbContext = dbContext;
            _apiAuthorization = apiAuthorization;
        }

        [FunctionName("BookmarkRuleFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            ApiAuthorizationResult authorizationResult = await _apiAuthorization.AuthorizeAsync(req.Headers);

            if (authorizationResult.Failed)
            {
                log.LogWarning(authorizationResult.FailureReason);
                return new UnauthorizedResult();
            }
            log.LogWarning($"HTTP trigger function {nameof(BookmarkRuleFunction)} request is authorized.");

            Bookmark data;

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            data = JsonConvert.DeserializeObject<Bookmark>(requestBody);

            if (data == null)
            {
                return new JsonResult(new
                {
                    error = true,
                    message = "Request body is empty",
                });
            }

            var results = await _dbContext.Bookmarks.Query(q => q.Where(w => w.RuleGuid == data.RuleGuid && w.UserId == data.UserId));
            var model = results.FirstOrDefault();

            if (model == null)
            {
                model = await _dbContext.Bookmarks.Add(data);
                log.LogInformation("Added new bookmark on rule. Id: {0}", model.Id);
            }
            else
            {
                log.LogInformation("Bookmark already exists for user {0}", model.UserId);
                return new JsonResult(new
                {
                    error = true,
                    message = "This rule has already been bookmarked"
                });
            }

            log.LogInformation($"User: {model.UserId}, Rule: {model.RuleGuid}, Id: {model.Id}");

            return new JsonResult(new
            {
                error = false,
                message = "",
            });
        }
    }
}
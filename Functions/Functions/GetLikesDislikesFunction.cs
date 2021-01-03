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
    public class GetLikesDislikesFunction
    {
        private readonly RulesDbContext _dbContext;

        public GetLikesDislikesFunction(RulesDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [FunctionName("GetLikesDislikesFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string RuleGuid = req.Query["rule_guid"];
            string UserId = req.Query["user_id"];

            if (RuleGuid == null)
            {
                return new JsonResult(new
                {
                    error = true,
                    message = "Missing RuleGuid param",
                });
            }

            var likes = await _dbContext.LikeDislikes.Query(q => q.Where(w => w.RuleGuid == RuleGuid));

            if(likes.Count() == 0 ) {
                return new JsonResult(new
                {
                    error = true,
                    message = "Could not find results for rule id: "+ RuleGuid,
                });
            }
            var results = likes
                .GroupBy(l => l.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count()
                });
            if (!string.IsNullOrEmpty(UserId))
            {
                log.LogInformation("Checking user status: " + UserId);
                log.LogInformation("RuleGuid: " + RuleGuid);
                var userReaction = await _dbContext.LikeDislikes.Query(q => q.Where(w => w.UserId == UserId && w.RuleGuid == RuleGuid));
                log.LogInformation("Count: " + userReaction.Count().ToString());
                log.LogInformation(userReaction.FirstOrDefault().Type.ToString());

                return new JsonResult(new
                {
                    error = false,
                    message = "",
                    likeCount = results.Where(r => r.Type == ReactionType.Like).FirstOrDefault()?.Count ?? 0,
                    dislikeCount = results.Where(r => r.Type == ReactionType.Dislike).FirstOrDefault()?.Count ?? 0,
                    userStatus = userReaction.FirstOrDefault()?.Type ?? 0
                });
            }

            return new JsonResult(new
            {
                error = false,
                message = "",
                likeCount = results.Where(r => r.Type == ReactionType.Like).FirstOrDefault()?.Count ?? 0,
                dislikeCount = results.Where(r => r.Type == ReactionType.Dislike).FirstOrDefault()?.Count ?? 0
            });
        }
    }
}
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

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
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogWarning($"HTTP trigger function {nameof(GetLikesDislikesFunction)} received a request.");

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
            if (likes.Count() == 0)
            {
                return new JsonResult(new
                {
                    error = true,
                    message = "Could not find results for rule id: " + RuleGuid,
                });
            }
            
            var results = likes
                .GroupBy(l => l.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count()
                });

            ReactionType? userStatus = null;
            if (!string.IsNullOrEmpty(UserId))
            {
                var userReaction = likes.Where(w => w.UserId == UserId).FirstOrDefault();
                userStatus = userReaction?.Type ?? null;
                log.LogInformation("Found reaction for user: '{0}' reaction: '{1}'", UserId, userStatus);
            }

            return new JsonResult(new
            {
                error = false,
                message = "",
                likeCount = results.Where(r => r.Type == ReactionType.Like).FirstOrDefault()?.Count ?? 0,
                dislikeCount = results.Where(r => r.Type == ReactionType.Dislike).FirstOrDefault()?.Count ?? 0,
                userStatus = userStatus
            });
        }
    }
}
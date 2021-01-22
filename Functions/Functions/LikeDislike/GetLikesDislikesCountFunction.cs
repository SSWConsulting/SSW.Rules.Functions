using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace SSW.Rules.Functions
{
    public class GetLikesDislikesCountFunction
    {
        private readonly RulesDbContext _dbContext;

        public GetLikesDislikesCountFunction(RulesDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [FunctionName("GetLikesDislikesCountFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogWarning($"HTTP trigger function {nameof(GetLikesDislikesCountFunction)} received a request.");

            string UserId = req.Query["user_id"];

            if (UserId == null)
            {
                return new JsonResult(new
                {
                    error = true,
                    message = "Missing user_id param",
                });
            }

            var reactions = await _dbContext.LikeDislikes.Query(q => q.Where(w => w.UserId == UserId));

            if (reactions.Count() == 0)
            {
                return new JsonResult(new
                {
                    error = true,
                    message = "Could not find results for user: " + UserId,
                });
            }
            
            var results = reactions
                .GroupBy(l => l.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count()
                });

            return new JsonResult(new
            {
                error = false,
                message = "",
                likeCount = results.Where(r => r.Type == ReactionType.Like).FirstOrDefault()?.Count ?? 0,
                dislikeCount = results.Where(r => r.Type == ReactionType.Dislike).FirstOrDefault()?.Count ?? 0,
            });
        }
    }
}
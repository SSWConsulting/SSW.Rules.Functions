using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace SSW.Rules.Functions
{
    public class GetAllReactionsFunction
    {
        private readonly RulesDbContext _dbContext;

        public GetAllReactionsFunction(RulesDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [FunctionName("GetAllReactionsFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogWarning($"HTTP trigger function {nameof(GetAllReactionsFunction)} received a request.");

            string UserId = req.Query["user_id"];

            if (string.IsNullOrEmpty(UserId))
            {
                return new JsonResult(new
                {
                    error = true,
                    message = "Missing or empty user_id param",
                });
            }
            log.LogInformation("Checking for bookmarks by user: {0}", UserId);
            var likesDislikes = await _dbContext.Reactions.Query(q => q.Where(w => w.UserId == UserId));
            if (likesDislikes.Count() == 0)
            {
                log.LogInformation($"Could not find results for user: {UserId}");
                return new JsonResult(new
                {
                    error = true,
                    message = $"Could not find results for user: {UserId}",
                    likesDislikedRules = likesDislikes,
                });
            }
            return new JsonResult(new
            {
                error = false,
                message = "",
                likesDislikedRules = likesDislikes,
            });
        }
    }
}
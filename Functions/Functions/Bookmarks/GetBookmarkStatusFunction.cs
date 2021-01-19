using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace SSW.Rules.Functions
{
    public class GetBookmarkStatusFunction
    {
        private readonly RulesDbContext _dbContext;

        public GetBookmarkStatusFunction(RulesDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [FunctionName("GetBookmarkStatusFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogWarning($"HTTP trigger function {nameof(GetBookmarkStatusFunction)} received a request.");

            string RuleGuid = req.Query["rule_guid"];
            string UserId = req.Query["user_id"];

            if (string.IsNullOrEmpty(UserId))
            {
                return new JsonResult(new
                {
                    error = true,
                    message = "Missing or empty user_id param",
                });
            }
            if (string.IsNullOrEmpty(RuleGuid))
            {
                return new JsonResult(new
                {
                    error = true,
                    message = "Missing or empty rule_guid param",
                });
            }
            log.LogInformation("Checking for bookmark on rule: {0} and user: {1}", RuleGuid, UserId);
            var bookmarks = await _dbContext.Bookmarks.Query(q => q.Where(w => w.RuleGuid == RuleGuid && w.UserId == UserId));
            if (bookmarks.Count() == 0)
            {
                log.LogInformation($"Could not find results for rule id: {RuleGuid}, and user: {UserId}");
                return new JsonResult(new
                {
                    error = false,
                    message = "",
                    bookmarkStatus = false,
                });
            }

            return new JsonResult(new
            {
                error = false,
                message = "",
                bookmarkStatus = true,
            });
        }
    }
}
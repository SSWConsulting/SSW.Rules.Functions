using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace SSW.Rules.Functions
{
    public class GetBookmarksCountFunction
    {
        private readonly RulesDbContext _dbContext;

        public GetBookmarksCountFunction(RulesDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [FunctionName("GetBookmarksCountFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogWarning($"HTTP trigger function {nameof(GetBookmarksCountFunction)} received a request.");

            string UserId = req.Query["user_id"];

            if (UserId == null)
            {
                return new JsonResult(new
                {
                    error = true,
                    message = "Missing user_id param",
                });
            }

            var bookmarks = await _dbContext.Bookmarks.Query(q => q.Where(w => w.UserId == UserId));

            if (bookmarks.Count() == 0)
            {
                return new JsonResult(new
                {
                    error = true,
                    message = "Could not find results for user: " + UserId,
                });
            }

            return new JsonResult(new
            {
                error = false,
                message = "",
                bookmarksCount = bookmarks?.Count() ?? 0,
            });
        }
    }
}
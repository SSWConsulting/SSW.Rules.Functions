using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace SSW.Rules.Functions
{
    public class GetOrganisationsFunction
    {
        private readonly RulesDbContext _dbContext;

        public GetOrganisationsFunction(RulesDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [FunctionName("GetOrganisationsFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogWarning($"HTTP trigger function {nameof(GetOrganisationsFunction)} received a request.");

            string UserId = req.Query["user_id"];

            if (string.IsNullOrEmpty(UserId))
            {
                return new JsonResult(new
                {
                    error = true,
                    message = "Missing or empty user_id param",
                });
            }

            var organisations = await _dbContext.Users.Query(q => q.Where(w => w.UserId == UserId));
            if (organisations.Count() == 0)
            {
                log.LogInformation($"Could not find results for user: {UserId}");
            }

            return new JsonResult(new
            {
                error = false,
                message = "",
                organisations = organisations,
            });
        }
    }
}
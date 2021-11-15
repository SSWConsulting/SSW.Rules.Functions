using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace SSW.Rules.Functions.Functions
{
    public class GetHistorySyncCommitHash
    {
        private readonly RulesDbContext _dbContext;

        public GetHistorySyncCommitHash(RulesDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        
        [FunctionName("GetHistorySyncCommitHash")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogWarning($"HTTP trigger function {nameof(GetHistorySyncCommitHash)} received a request.");
                        
            var results = await _dbContext.SyncHistory.Query(q => q);
            var syncHash = results.FirstOrDefault();

            string responseMessage = syncHash?.CommitHash ?? string.Empty;

            return new OkObjectResult(responseMessage);
        }
    }
}

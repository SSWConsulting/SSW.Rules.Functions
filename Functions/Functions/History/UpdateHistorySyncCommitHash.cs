using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;

namespace SSW.Rules.Functions.Functions.History
{
    public class UpdateHistorySyncCommitHash
    {
        private readonly RulesDbContext _dbContext;

        public UpdateHistorySyncCommitHash(RulesDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [FunctionName("UpdateHistorySyncCommitHash")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogWarning($"HTTP trigger function {nameof(UpdateHistorySyncCommitHash)} received a request.");

            string commitHash = req.Form["commitHash"];

            var results = await _dbContext.SyncHistory.Query(q => q);
            var syncHash = results.FirstOrDefault();

            if (syncHash == null)
            {
                await _dbContext.SyncHistory.Add(new SyncHistory
                {
                    CommitHash = commitHash
                });
            }
            else
            {
                syncHash.CommitHash = commitHash;
                await _dbContext.SyncHistory.Update(syncHash);
            }

            return new OkResult();
        }
    }
}

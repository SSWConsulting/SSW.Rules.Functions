using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SSW.Rules.Functions.Functions
{
    public class GenerateHistoryFileFunction
    {
        private readonly RulesDbContext _dbContext;

        public GenerateHistoryFileFunction(RulesDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [FunctionName("GenerateHistoryFileFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogWarning($"HTTP trigger function {nameof(GenerateHistoryFileFunction)} received a request.");

            var results = await _dbContext.RuleHistoryCache.Query(q => q);

            List<RuleHistoryData> ruleHistory = new List<RuleHistoryData>();

            foreach(var history in results)
            {
                ruleHistory.Add(new RuleHistoryData
                {
                    file = history.MarkdownFilePath,
                    lastUpdated = history.ChangedAtDateTime,
                    lastUpdatedBy = history.ChangedByDisplayName,
                    lastUpdatedByEmail = history.ChangedByEmail,
                    created = history.CreatedAtDateTime,
                    createdBy = history.CreatedByDisplayName,
                    createdByEmail = history.CreatedByEmail
                });
            }

            string responseMessage = JsonConvert.SerializeObject(ruleHistory);

            return new OkObjectResult(responseMessage);
        }
    }
}
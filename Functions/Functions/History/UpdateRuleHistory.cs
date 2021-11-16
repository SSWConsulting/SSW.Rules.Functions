using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace SSW.Rules.Functions.Functions.History
{
    public class UpdateRuleHistory
    {
        private readonly RulesDbContext _dbContext;

        public UpdateRuleHistory(RulesDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [FunctionName("UpdateRuleHistory")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogWarning($"HTTP trigger function {nameof(UpdateRuleHistory)} received a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<List<RuleHistoryData>>(requestBody);

            foreach(var historyEntry in data)
            {
                var result = await _dbContext.RuleHistoryCache.Query(q => q.Where(w => w.MarkdownFilePath == historyEntry.file));
                RuleHistoryCache historyCache = result.FirstOrDefault();

                if (historyCache == null)
                {
                    await _dbContext.RuleHistoryCache.Add(new RuleHistoryCache
                    {
                        MarkdownFilePath = historyEntry.file,
                        ChangedAtDateTime = DateTime.Parse(historyEntry.lastUpdated),
                        ChangedByDisplayName = historyEntry.lastUpdatedBy,
                        ChangedByEmail = historyEntry.lastUpdatedByEmail,
                        CreatedAtDateTime = DateTime.Parse(historyEntry.created),
                        CreatedByDisplayName = historyEntry.createdBy,
                        CreatedByEmail = historyEntry.createdByEmail
                    });
                } else
                {
                    historyCache.ChangedAtDateTime = DateTime.Parse(historyEntry.lastUpdated);
                    historyCache.ChangedByDisplayName = historyEntry.lastUpdatedBy;
                    historyCache.ChangedByEmail = historyEntry.lastUpdatedByEmail;
                    historyCache.CreatedAtDateTime = DateTime.Parse(historyEntry.created);
                    historyCache.CreatedByDisplayName = historyEntry.createdBy;
                    historyCache.CreatedByEmail = historyEntry.createdByEmail;

                    await _dbContext.RuleHistoryCache.Update(historyCache);
                }
            }

            return new OkResult();
        }
    }
}

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OidcApiAuthorization.Abstractions;
using OidcApiAuthorization.Models;
using System.Linq;

namespace SSW.Rules.Functions
{
    public class HealthCheckFunction
    {
        private readonly IApiAuthorization _apiAuthorization;
        private readonly RulesDbContext _dbContext;

        public HealthCheckFunction(
            IApiAuthorization apiAuthorization,
            RulesDbContext dbContext)
        {
            _apiAuthorization = apiAuthorization;
            _dbContext = dbContext;
        }

        [FunctionName("HealthCheckFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogWarning($"HTTP trigger function {nameof(HealthCheckFunction)} received a request.");

            HealthCheckResult result = await _apiAuthorization.HealthCheckAsync();
            var entity = await _dbContext.LikeDislikes.Add(new LikeDislike {
                Id = Guid.NewGuid().ToString(),
				Type = ReactionType.Like,
                RuleGuid = "qwert123456",
                UserId = "user123456",
                Discriminator = typeof(LikeDislike).FullName
			});

            if (result.IsHealthy && entity != null)
            {
                log.LogWarning($"{nameof(HealthCheckFunction)} health check OK.");
            }
            else
            {
                log.LogError(
                    $"{nameof(HealthCheckFunction)} health check failed."
                      + $" {nameof(HealthCheckResult)}: {JsonConvert.SerializeObject(result)}"
                    );
            }
            return new OkObjectResult(result);
        }
    }
}

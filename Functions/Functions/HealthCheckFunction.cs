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
            var likeDislikeEntity = await _dbContext.LikeDislikes.Add(new LikeDislike
            {
                Id = Guid.NewGuid().ToString(),
                Type = ReactionType.Like,
                RuleGuid = "exampleRule123",
                UserId = "exampleUser123",
                Discriminator = typeof(LikeDislike).FullName
            });

            var bookmarkEntity = await _dbContext.Bookmarks.Add(new Bookmark
            {
                Id = Guid.NewGuid().ToString(),
                RuleGuid = "exampleRule123",
                UserId = "exampleUser123",
                Discriminator = typeof(Bookmark).FullName
            });

            var secretContentEntity = await _dbContext.SecretContents.Add(new SecretContent
            {
                Id = Guid.NewGuid().ToString(),
                OrganisationId = "123123",
                Discriminator = typeof(SecretContent).FullName
            });

            if (result.IsHealthy && likeDislikeEntity != null && bookmarkEntity != null && secretContentEntity != null)
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

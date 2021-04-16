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
    /// <summary>
    /// Retrives a users object using their UserId
    /// </summary>
    public class GetUserFunction
    {
        private readonly RulesDbContext _dbContext;
        private readonly IApiAuthorization _apiAuthorization;

        public GetUserFunction(RulesDbContext dbContext, IApiAuthorization apiAuthorization)
        {
            _dbContext = dbContext;
            _apiAuthorization = apiAuthorization;
        }

        [FunctionName("GetUserFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = null)] HttpRequest req,
            ILogger log)
        {
            ApiAuthorizationResult authorizationResult = await _apiAuthorization.AuthorizeAsync(req.Headers);

            if (authorizationResult.Failed)
            {
                log.LogWarning(authorizationResult.FailureReason);
                return new UnauthorizedResult();
            }

            log.LogWarning($"HTTP trigger function {nameof(GetUserFunction)} request is authorized.");

            string UserId = req.Query["user_id"];

            if (string.IsNullOrEmpty(UserId))
            {
                return new BadRequestObjectResult(new
                {
                    message = "Missing or empty user_id param",
                });
            }

            var result = await _dbContext.Users.Query(q => q.Where(w => w.UserId == UserId));
            User user = result.FirstOrDefault();

            if (user == null)
            {
                log.LogInformation($"Could not find results for user: {UserId}");
                return new BadRequestObjectResult(new
                {
                    message = "User " + UserId + " was not found",
                });
            }

            bool commentsConnected = !string.IsNullOrEmpty(user?.CommentsUserId);

            return new OkObjectResult(new
            {
                user = user,
                commentsConnected = commentsConnected,
            });
        }
    }
}

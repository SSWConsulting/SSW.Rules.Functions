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
    /// Removes the CommentsUserId from a user object so they can connect a different comments account
    /// </summary>
    public class RemoveUserCommentsAccount
    {
        private readonly RulesDbContext _dbContext;
        private readonly IApiAuthorization _apiAuthorization;

        public RemoveUserCommentsAccount(RulesDbContext dbContext, IApiAuthorization apiAuthorization)
        {
            _dbContext = dbContext;
            _apiAuthorization = apiAuthorization;
        }

        [FunctionName("RemoveUserCommentsAccount")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            ApiAuthorizationResult authorizationResult = await _apiAuthorization.AuthorizeAsync(req.Headers);

            if (authorizationResult.Failed)
            {
                log.LogWarning(authorizationResult.FailureReason);
                return new UnauthorizedResult();
            }

            log.LogWarning($"HTTP trigger function {nameof(RemoveUserCommentsAccount)} request is authorized.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            User data = JsonConvert.DeserializeObject<User>(requestBody);

            if (data == null || string.IsNullOrEmpty(data?.UserId))
            {
                return new BadRequestObjectResult(new
                {
                    message = "Request body is empty or incorrect",
                });
            }

            var user = await _dbContext.Users.Query(q => q.Where(w => w.UserId == data.UserId));
            User model = user.FirstOrDefault();

            if (model == null)
            {
                return new BadRequestObjectResult(new
                {
                    message = "User does not exsist"
                });
            }

            model.CommentsUserId = string.Empty;

            await _dbContext.Users.Update(model);

            return new OkResult();
        }
    }
}

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
    /// Takes a Disqus ID and adds it to a user's account object in CosmosDB. Creates the user object if it doesn't already exist.
    /// </summary>
    public class ConnectUserCommentsFunction
    {
        private readonly RulesDbContext _dbContext;
        private readonly IApiAuthorization _apiAuthorization;
        public ConnectUserCommentsFunction(RulesDbContext dbContext, IApiAuthorization apiAuthorization)
        {
            _dbContext = dbContext;
            _apiAuthorization = apiAuthorization;
        }

        [FunctionName("ConnectUserCommentsFunction")]
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

            log.LogWarning($"HTTP trigger function {nameof(ConnectUserCommentsFunction)} request is authorized.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            User data = JsonConvert.DeserializeObject<User>(requestBody);

            if (data == null || string.IsNullOrEmpty(data?.CommentsUserId) || string.IsNullOrEmpty(data?.UserId))
            {
                return new BadRequestObjectResult(new
                {
                    message = "Request body is empty or incorrect",
                });
            }

            var existingUser = await _dbContext.Users.Query(q => q.Where(w => w.UserId == data.UserId));
            User user = existingUser.FirstOrDefault();

            if (user == null)
            {
                await _dbContext.Users.Add(data);
                return new OkResult();
            }

            var exisitingCommentsId = await _dbContext.Users.Query(q => q.Where(w => w.CommentsUserId == data.CommentsUserId));

            if (exisitingCommentsId.FirstOrDefault() != null)
            {
                return new ConflictObjectResult(new
                {
                    message = "This comments account is already being used by another user",
                });
            }

            if (user?.CommentsUserId == data.CommentsUserId)
            {
                return new ConflictObjectResult(new
                {
                    message = "User already has the same comments account associated",
                });
            }

            user.CommentsUserId = data.CommentsUserId;
            await _dbContext.Users.Update(user);

            return new OkResult();
        }
    }
}

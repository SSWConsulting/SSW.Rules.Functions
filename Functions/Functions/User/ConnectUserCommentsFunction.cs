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

            User data;

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            data = JsonConvert.DeserializeObject<User>(requestBody);

            bool isNull = string.IsNullOrEmpty(data?.CommentsUserId) || string.IsNullOrEmpty(data?.UserId);
            if (data == null || isNull)
            {
                return new BadRequestObjectResult(new
                {
                    message = "Request body is empty or incorrect",
                });
            }
            User result;

            var existingUser = await _dbContext.Users.Query(q => q.Where(w => w.UserId == data.UserId));
            var model = existingUser.FirstOrDefault();

            if (model == null)
            {
                result = await _dbContext.Users.Add(data);
            }

            var exisitingCommentsId = await _dbContext.Users.Query(q => q.Where(w => w.CommentsUserId == data.CommentsUserId));

            if (exisitingCommentsId.FirstOrDefault() != null) {

            }
                User user = existingUser.FirstOrDefault();

            if (!string.IsNullOrEmpty(data?.CommentsUserId) && user?.CommentsUserId == data.CommentsUserId)
            {
                log.LogInformation(data.CommentsUserId.ToString());
                log.LogInformation(user.CommentsUserId.ToString());
                return new ConflictObjectResult(new
                {
                    message = "User already has the same comments account assosiated",
                });
            }

            user.CommentsUserId = data.CommentsUserId;
            result = await _dbContext.Users.Update(user);

            return new OkResult();
        }
    }
}

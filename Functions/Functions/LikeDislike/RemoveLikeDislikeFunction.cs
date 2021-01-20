using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OidcApiAuthorization.Abstractions;
using OidcApiAuthorization.Models;

namespace SSW.Rules.Functions
{
    public class RemoveLikeDislikeFunction
    {
        private readonly RulesDbContext _dbContext;
        private readonly IApiAuthorization _apiAuthorization;

        public RemoveLikeDislikeFunction(RulesDbContext dbContext, IApiAuthorization apiAuthorization)
        {
            _dbContext = dbContext;
            _apiAuthorization = apiAuthorization;
        }

        [FunctionName("RemoveLikeDislikeFunction")]
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
            log.LogWarning($"HTTP trigger function {nameof(RemoveLikeDislikeFunction)} request is authorized.");

            LikeDislike data;

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            data = JsonConvert.DeserializeObject<LikeDislike>(requestBody);

            if (data == null)
            {
                return new JsonResult(new
                {
                    error = true,
                    message = "Request body is empty",
                });
            }

            var results = await _dbContext.LikeDislikes.Query(q => q.Where(w => w.RuleGuid == data.RuleGuid && w.UserId == data.UserId));
            var model = results.FirstOrDefault();

            if (model == null)
            {
                log.LogInformation("No reaction exists for User {0} and Rule {1}", data.UserId, data.RuleGuid);
                return new JsonResult(new
                {
                    error = true,
                    message = "No reaction exists for this rule and user",
                    data.UserId,
                    data.RuleGuid,
                });
            }
            var deleteResults = await _dbContext.LikeDislikes.Delete(model);
            
            log.LogInformation($"User: {model.UserId}, Rule: {model.RuleGuid}, Id: {model.Id}");
            
            return new JsonResult(new
            {
                error = false,
                message = ""
            });
        }
    }
}
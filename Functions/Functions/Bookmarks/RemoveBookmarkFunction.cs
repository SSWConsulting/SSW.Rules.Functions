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
    public class RemoveBookmarkFunction
    {
        private readonly RulesDbContext _dbContext;
        private readonly IApiAuthorization _apiAuthorization;

        public RemoveBookmarkFunction(RulesDbContext dbContext, IApiAuthorization apiAuthorization)
        {
            _dbContext = dbContext;
            _apiAuthorization = apiAuthorization;
        }

        [FunctionName("RemoveBookmarkFunction")]
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
            log.LogWarning($"HTTP trigger function {nameof(RemoveBookmarkFunction)} request is authorized.");

            Bookmark data;

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            data = JsonConvert.DeserializeObject<Bookmark>(requestBody);

            bool isNull = string.IsNullOrEmpty(data?.RuleGuid) || string.IsNullOrEmpty(data?.UserId);
            if (data == null || isNull)
            {
                return new JsonResult(new
                {
                    error = true,
                    message = "Request body is empty",
                });
            }

            var results = await _dbContext.Bookmarks.Query(q => q.Where(w => w.RuleGuid == data.RuleGuid && w.UserId == data.UserId));
            var model = results.FirstOrDefault();

            if (model == null)
            {
                log.LogInformation("No bookmark exists for User {0} and Rule {1}", data.UserId, data.RuleGuid);
                return new JsonResult(new
                {
                    error = true,
                    message = "No bookmark exists for this rule and user",
                    data.UserId,
                    data.RuleGuid,
                });
            }
            var deleteResults = await _dbContext.Bookmarks.Delete(model);
            
            log.LogInformation($"User: {model.UserId}, Rule: {model.RuleGuid}, Id: {model.Id}");
            
            return new JsonResult(new
            {
                error = false,
                message = ""
            });
        }
    }
}
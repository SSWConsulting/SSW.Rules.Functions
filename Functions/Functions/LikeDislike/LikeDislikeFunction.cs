using System;
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
    public class LikeDislikeFunction
    {
        private readonly RulesDbContext _dbContext;
        private readonly IApiAuthorization _apiAuthorization;

        public LikeDislikeFunction(RulesDbContext dbContext, IApiAuthorization apiAuthorization)
        {
            _dbContext = dbContext;
            _apiAuthorization = apiAuthorization;
        }

        [FunctionName("LikeDislikeFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            //Add Auth
            ApiAuthorizationResult authorizationResult = await _apiAuthorization.AuthorizeAsync(req.Headers);

            if (authorizationResult.Failed)
            {
                log.LogWarning(authorizationResult.FailureReason);
                return new UnauthorizedResult();
            }
            log.LogWarning($"HTTP trigger function {nameof(LikeDislikeFunction)} request is authorized.");

            LikeDislike data;

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            data = JsonConvert.DeserializeObject<LikeDislike>(requestBody);

            // 1. validate data
            if (data == null)
            {
                return new JsonResult(new
                {
                    error = true,
                    message = "Request body is empty",
                });
            }

            // 2. Check if data already exists (i.e. user has already liked or disliked)
            var results = await _dbContext.LikeDislikes.Query(q => q.Where(w => w.RuleGuid == data.RuleGuid && w.UserId == data.UserId));
            var model = results.FirstOrDefault();
            log.LogInformation($"reactions on same rule by same user: {results.Count()}");

            if (model == null)
            {
                data.Id = Guid.NewGuid().ToString();
                model = await _dbContext.LikeDislikes.Add(data);
                log.LogInformation("Added new reaction. Id: {0}", model.Id);
            }
            else
            {
                log.LogInformation("Reaction already exists for user {0}", model.UserId);

                if (model.Type != data.Type)
                {
                    model.Type = data.Type;
                    model = await _dbContext.LikeDislikes.Update(model);
                    log.LogInformation("Updated reaction to " + model.Type);
                }
                else
                {
                    log.LogInformation("Reaction is the same. No change");
                }
            }

            log.LogInformation($"User: {model.UserId}, Type: {model.Type}, Rule: {model.RuleGuid}, Id: {model.Id}");

            return new JsonResult(new
            {
                error = false,
                message = "",
                reaction = model.Type
            });
        }
    }
}
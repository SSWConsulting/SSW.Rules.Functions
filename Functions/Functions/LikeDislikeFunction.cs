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
using SSW.Rules.Functions;

namespace SSW.Rules.Functions {

    public class LikeDislikeFunction {

        private readonly RulesDbContext _dbContext;

        public LikeDislikeFunction(RulesDbContext dbContext) {
            _dbContext = dbContext;
        }

        [FunctionName("LikeDislikeFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log) {
            log.LogInformation("C# HTTP trigger function processed a request.");

            LikeDislike data;

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            data = JsonConvert.DeserializeObject<LikeDislike>(requestBody);

            // 1. validate data
            if (data == null) {
                return new JsonResult(new {
                    error = true,
                    message = "Request body is empty or in the wrong format",
                });
            }

            // 2. Check if data already exists (i.e. user has already liked or disliked)
            var results = await _dbContext.LikeDislikes.Query(q => q.Where(w => w.RuleGuid == data.RuleGuid && w.UserId == data.UserId));
            var model = results.FirstOrDefault();
            log.LogInformation($"reactions on same rule by same user: {results.Count()}");

            if (model != null) {
                log.LogInformation("Reaction already exists for user {0}", model.UserId);

                if (model.Type != data.Type) {
                    model.Type = data.Type;
                    model = await _dbContext.LikeDislikes.Update(model);
                    log.LogInformation("Updated reaction to " + model.Type);
                } else {
                    log.LogInformation("Reaction is the same. No change");
                }
            } else if (model == null) {
                data.Id = Guid.NewGuid().ToString();
                model = await _dbContext.LikeDislikes.Add(data);
                log.LogInformation("Added new reaction. Id: {0}", model.Id);
            }

            log.LogInformation($"User: {model.UserId}, Type: {model.Type}, Rule: {model.RuleGuid}, Id: {model.Id}");

            return new JsonResult(new {
                error = false,
                message = "",
                reaction = model.Type
            });
        }
    }
}
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OidcApiAuthorization.Models;
using OidcApiAuthorization.Abstractions;
using System.Linq;

namespace SSW.Rules.Functions
{
    public class GetSecretContentFunction
    {
        private readonly RulesDbContext _dbContext;
        private readonly IApiAuthorization _apiAuthorization;
        public GetSecretContentFunction(RulesDbContext dbContext, IApiAuthorization apiAuthorization)
        {
            _dbContext = dbContext;
            _apiAuthorization = apiAuthorization;
        }
        [FunctionName("GetSecretContentFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            ApiAuthorizationResult authorizationResult = await _apiAuthorization.AuthorizeAsync(req.Headers);

            if (authorizationResult.Failed)
            {
                log.LogWarning(authorizationResult.FailureReason);
                return new UnauthorizedResult();
            }
            log.LogInformation($"C# HTTP trigger function {nameof(GetSecretContentFunction)} processed a request.");

            string SecretContentId = req.Query["id"];

            if (string.IsNullOrEmpty(SecretContentId))
            {
                return new JsonResult(new
                {
                    error = true,
                    message = "Missing or empty id param",
                });
            }

            var SecretContents = await _dbContext.SecretContents.Query(q => q.Where(w => w.Id == SecretContentId));
            var model = SecretContents.FirstOrDefault();
            if (model == null)
            {
                return new JsonResult(new
                {
                    error = true,
                    message = $"Could not find content with id: {SecretContentId}"
                });
            }

            return new JsonResult(new
            {
                error = false,
                message = "",
                Content = model
            });
        }
    }
}

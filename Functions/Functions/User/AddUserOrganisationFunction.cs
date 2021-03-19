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
    public class AddUserOrganisationFunction
    {
        private readonly RulesDbContext _dbContext;
        private readonly IApiAuthorization _apiAuthorization;

        public AddUserOrganisationFunction(RulesDbContext dbContext, IApiAuthorization apiAuthorization)
        {
            _dbContext = dbContext;
            _apiAuthorization = apiAuthorization;
        }

        [FunctionName("AddUserOrganisationFunction")]
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

            log.LogWarning($"HTTP trigger function {nameof(AddUserOrganisationFunction)} request is authorized.");

            User data;

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            data = JsonConvert.DeserializeObject<User>(requestBody);

            bool isNull = string.IsNullOrEmpty(data?.OrganisationId) || string.IsNullOrEmpty(data?.UserId);
            if (data == null || isNull)
            {
                return new JsonResult(new
                {
                    error = true,
                    message = "Request body is empty",
                });
            }

            var existingOrganisation = await _dbContext.Users.Query(q => q.Where(w => w.UserId == data.UserId && w.OrganisationId == data.OrganisationId));
            var model = existingOrganisation.FirstOrDefault();

            if (model != null)
            {
                return new JsonResult(new
                {
                    error = true,
                    message = "User is already assigned to this organisation",
                });
            }

            var result = await _dbContext.Users.Add(data);

            return new JsonResult(new
            {
                error = false,
                message = "",
                user = result,
            });
        }
    }
}

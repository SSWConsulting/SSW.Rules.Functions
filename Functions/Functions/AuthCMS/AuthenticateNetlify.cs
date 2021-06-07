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
using System.Configuration;

namespace SSW.Rules.Functions
{
    public class AuthenticateNetlify
    {
        [FunctionName("AuthenticateNetlify")]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function {nameof(AuthenticateNetlify)} processed a request.");

            string scope = req.Query["scope"];

            if (string.IsNullOrEmpty(scope))
            {
                log.LogError("Missing scope param");
                return new BadRequestObjectResult(new
                {
                    message = "Missing scope param",
                });
            }

            string clientId = System.Environment.GetEnvironmentVariable("CMS_OAUTH_CLIENT_ID", EnvironmentVariableTarget.Process);

            if (string.IsNullOrEmpty(clientId))
            {
                log.LogError("Missing CMS_OAUTH_CLIENT_ID");
                throw new ConfigurationErrorsException("Missing CMS_OAUTH_CLIENT_ID");
            }

            return new RedirectResult($"https://github.com/login/oauth/authorize?client_id={clientId}&scope={scope}", true);
        }
    }
}

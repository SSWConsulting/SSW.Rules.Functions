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
                return new BadRequestResult();
            }

            log.LogInformation(scope);

            string ClientId = System.Environment.GetEnvironmentVariable("OAUTH_CLIENT_ID", EnvironmentVariableTarget.Process);

            return new RedirectResult($"https://github.com/login/oauth/authorize?client_id={ClientId}&scope=repo,user", true); //TODO: Check needed scope
        }
    }
}

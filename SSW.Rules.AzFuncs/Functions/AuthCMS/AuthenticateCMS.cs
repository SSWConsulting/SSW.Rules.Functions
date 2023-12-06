using System.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace SSW.Rules.AzFuncs.Functions.AuthCMS;

public class AuthenticateCms(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<AuthenticateCms>();

    // TODO: Find where this is called and update the name 
    // Old name: AuthenticateNetlify
    [Function("AuthenticateCMS")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth")] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogInformation($"C# HTTP trigger function {nameof(AuthenticateCms)} processed a request.");

        var scope = req.Query["scope"];

        if (string.IsNullOrEmpty(scope))
        {
            _logger.LogError("Missing scope param");
            return new BadRequestObjectResult(new 
            {
                message = "Missing scope param",
            });
        }

        var clientId = Environment.GetEnvironmentVariable("CMS_OAUTH_CLIENT_ID", EnvironmentVariableTarget.Process);

        if (!string.IsNullOrEmpty(clientId))
            return new RedirectResult($"https://github.com/login/oauth/authorize?client_id={clientId}&scope={scope}",
                true);
        
        _logger.LogError("Missing CMS_OAUTH_CLIENT_ID");
        throw new ConfigurationErrorsException("Missing CMS_OAUTH_CLIENT_ID");

    }
}
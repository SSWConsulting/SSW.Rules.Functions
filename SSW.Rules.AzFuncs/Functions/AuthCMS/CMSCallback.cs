using System.Configuration;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SSW.Rules.AzFuncs.Functions.AuthCMS;

public class CmsCallback(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<CmsCallback>();

    // TODO: Find where this is called and update the name 
    // Old name: NetlifyCallback
    [Function("CMSCallback")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "callback")] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogInformation($"C# HTTP trigger function {nameof(CmsCallback)} processed a request.");

        try
        {
            var code = req.Query["code"];
            if (string.IsNullOrEmpty(code))
            {
                _logger.LogError("Missing code param");
                return new BadRequestObjectResult(new
                {
                    message = "Missing code param",
                });
            }

            const string tokenUrl = "https://github.com/login/oauth/access_token";
            var newClient = new HttpClient();
            newClient.DefaultRequestHeaders.Add("Accept", "application/json");
            var newRequest = new HttpRequestMessage(HttpMethod.Post, tokenUrl);

            var clientId =
                System.Environment.GetEnvironmentVariable("CMS_OAUTH_CLIENT_ID", EnvironmentVariableTarget.Process);
            var clientSecret =
                System.Environment.GetEnvironmentVariable("CMS_OAUTH_CLIENT_SECRET", EnvironmentVariableTarget.Process);

            if (string.IsNullOrEmpty(clientId))
            {
                _logger.LogError("Missing CMS_OAUTH_CLIENT_ID");
                throw new ConfigurationErrorsException("Missing CMS_OAUTH_CLIENT_ID");
            }

            if (string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogError("Missing CMS_OAUTH_CLIENT_SECRET");
                throw new ConfigurationErrorsException("Missing CMS_OAUTH_CLIENT_SECRET");
            }

            var body = new
            {
                code,
                client_id = clientId,
                client_secret = clientSecret
            };

            newRequest.Content =
                new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            var response = await newClient.SendAsync(newRequest);

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            dynamic jsonBody = JsonConvert.DeserializeObject(responseBody);

            var authorisedObject = JsonConvert.SerializeObject(new
            {
                token = jsonBody.access_token,
                provider = "github"
            });

            var script = @"<script>
                (function() {
                    function recieveMessage(e) {
                    console.log(""recieveMessage %o"", e);
                    
                    // send message to main window with the app
                    window.opener.postMessage(
                        'authorization:github:success:" + authorisedObject + @"', 
                        e.origin
                    );
                    }
                    window.addEventListener(""message"", recieveMessage, false);
                    window.opener.postMessage(""authorizing:github"", ""*"");
                })()
                </script>";

            return new ContentResult { Content = script, ContentType = "text/html" };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }
}
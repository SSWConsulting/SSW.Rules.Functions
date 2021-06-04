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
using System.Net.Http;
using System.Text;

namespace SSW.Rules.Functions
{
    public class NetlifyCallback
    {

        [FunctionName("NetlifyCallback")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "callback")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function {nameof(NetlifyCallback)} processed a request.");

            string code = req.Query["code"];
            string host = req.Headers["host"];
            log.LogInformation("host: " + host);

            if (string.IsNullOrEmpty(code))
            {
                return new BadRequestObjectResult(new
                {
                    message = "Missing code param",
                });
            }

            log.LogInformation(code);

            // Call Your  API
            string authorisedObject;

            try
            {
                string tokenUrl = "https://github.com/login/oauth/access_token";
                HttpClient newClient = new HttpClient();
                newClient.DefaultRequestHeaders.Add("Accept", "application/json");
                HttpRequestMessage newRequest = new HttpRequestMessage(HttpMethod.Post, tokenUrl);

                string clientId = System.Environment.GetEnvironmentVariable("CMS_OAUTH_CLIENT_ID", EnvironmentVariableTarget.Process);
                string clientSecret = System.Environment.GetEnvironmentVariable("CMS_OAUTH_CLIENT_SECRET", EnvironmentVariableTarget.Process);

                var body = new
                {
                    code,
                    client_id = clientId,
                    client_secret = clientSecret
                };

                newRequest.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await newClient.SendAsync(newRequest);

                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                dynamic jsonBody = JsonConvert.DeserializeObject(responseBody);

                authorisedObject = JsonConvert.SerializeObject(new
                {
                    token = jsonBody.access_token,
                    provider = "github"
                });
            }
            catch (HttpRequestException ex)
            {
                log.LogError(ex.Message);
                throw;
            }

            string script = @"<script>
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
    }
}

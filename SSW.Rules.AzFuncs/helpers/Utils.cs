using System.Collections.Specialized;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;
using SSW.Rules.AzFuncs.Domain;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SSW.Rules.AzFuncs.helpers;

public static partial class Utils
{
    /// <summary>
    /// Simple Json Object { error: bool, message: string }
    /// Error is true if status code >= 400
    /// </summary>
    /// <param name="req"></param>
    /// <param name="statusCode">Default 200</param>
    /// <param name="message">Default empty</param>
    /// <returns></returns>
    public static HttpResponseData CreateJsonErrorResponse(this HttpRequestData req, HttpStatusCode statusCode = HttpStatusCode.OK, string message = "")
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        response.StatusCode = statusCode;
        
        var content = new
        {
            error = (int)statusCode is >= 400,
            message = message
        };

        var jsonContent = JsonConvert.SerializeObject(content);
        response.WriteString(jsonContent);

        return response;
    }
    
    /// <summary>
    /// Create a JSON object response 
    /// </summary>
    /// <param name="req"></param>
    /// <param name="statusCode">Default 200</param>
    /// <param name="message">Default empty</param>
    /// <returns></returns>
    public static HttpResponseData CreateJsonResponse(this HttpRequestData req, object message, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        response.StatusCode = statusCode;

        var jsonContent = JsonConvert.SerializeObject(message);
        response.WriteString(jsonContent);

        return response;
    }

    public static async Task<NameValueCollection> ReadFormDataAsync(this HttpRequestData req)
    {
        using var reader = new StreamReader(req.Body);
        var body = await reader.ReadToEndAsync();
        var parsedForm = HttpUtility.ParseQueryString(body);
        return parsedForm;
    }

    public static FrontMatter? ParseFrontMatter(string markdownContent)
    {
        // Regular expression to extract the YAML front matter
        var frontMatterRegex = FrontmatterRegex();
        var match = frontMatterRegex.Match(markdownContent);

        if (!match.Success) return null; // No front matter found

        var frontMatterYaml = match.Groups[1].Value;

        // Deserializer for YAML
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        // Deserialize YAML to FrontMatter object
        try
        {
            return deserializer.Deserialize<FrontMatter>(frontMatterYaml);
        }
        catch (Exception ex)
        {
            // Handle parsing error
            Console.WriteLine("Error parsing YAML: " + ex.Message);
            return null;
        }
    }

    [GeneratedRegex(@"^---\s+(.*?)\s+---", RegexOptions.Singleline)]
    private static partial Regex FrontmatterRegex();
}
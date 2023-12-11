using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Primitives;

namespace SSW.Rules.AzFuncs.helpers;

public static class Converters
{
    public static IHeaderDictionary ConvertToIHeaderDictionary(HttpHeadersCollection headers)
    {
        var headerDictionary = new HeaderDictionary();

        foreach (var header in headers)
        {
            headerDictionary.Add(header.Key, new StringValues(header.Value.ToArray()));
        }

        return headerDictionary;
    }
}
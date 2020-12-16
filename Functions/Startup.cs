using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using OidcApiAuthorization;

[assembly: FunctionsStartup(typeof(SSW.Rules.Functions.Startup))]
namespace SSW.Rules.Functions {
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddOidcApiAuthorization();
        }
    }
}
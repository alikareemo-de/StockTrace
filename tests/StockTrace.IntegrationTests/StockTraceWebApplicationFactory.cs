using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace StockTrace.IntegrationTests;

public sealed class StockTraceWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=localhost;Database=StockTraceIntegrationTests;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True",
                ["Database:InitializeOnStartup"] = "true"
            });
        });
    }
}

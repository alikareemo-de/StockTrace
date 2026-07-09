namespace StockTrace.Api.Configuration;

internal sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public const string PolicyName = "ConfiguredFrontendOrigins";

    public string[] AllowedOrigins { get; init; } = [];
}

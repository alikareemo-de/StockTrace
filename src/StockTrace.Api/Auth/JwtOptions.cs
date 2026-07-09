namespace StockTrace.Api.Auth;

internal sealed class JwtOptions
{
    public string Issuer { get; init; } = "StockTrace";
    public string Audience { get; init; } = "StockTrace.TestingUI";
    public string SigningKey { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; } = 120;
}

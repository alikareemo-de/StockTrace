using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace StockTrace.Api.Auth;

public sealed record AuthenticatedUserResult(
    string Username,
    string DisplayName,
    string Role,
    IReadOnlyCollection<string> Permissions);

public sealed record LoginResult(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string Username,
    string DisplayName,
    string Role,
    IReadOnlyCollection<string> Permissions);

public interface ITokenService
{
    LoginResult CreateToken(TestUser user);
}

internal sealed class TokenService(IOptions<JwtOptions> options) : ITokenService
{
    public LoginResult CreateToken(TestUser user)
    {
        var jwtOptions = options.Value;
        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("JWT signing key must be configured and at least 32 characters long.");
        }

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(jwtOptions.ExpirationMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Username),
            new(ClaimTypes.Name, user.Username),
            new("display_name", user.DisplayName),
            new(ClaimTypes.Role, user.Role)
        };
        claims.AddRange(user.Permissions.Select(permission => new Claim(AppClaimTypes.Permission, permission)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            jwtOptions.Issuer,
            jwtOptions.Audience,
            claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new LoginResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            user.Username,
            user.DisplayName,
            user.Role,
            user.Permissions);
    }
}

using Microsoft.Extensions.Options;

namespace StockTrace.Api.Auth;

public sealed class TestUser
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public List<string> Permissions { get; init; } = [];
    public string Description { get; init; } = string.Empty;
}

internal sealed class TestUsersOptions
{
    public List<TestUser> Users { get; init; } = [];
}

public interface ITestUserStore
{
    TestUser? Find(string username, string password);
}

internal sealed class ConfiguredTestUserStore(IOptions<TestUsersOptions> options) : ITestUserStore
{
    public TestUser? Find(string username, string password) =>
        options.Value.Users.SingleOrDefault(x =>
            string.Equals(x.Username, username, StringComparison.OrdinalIgnoreCase) &&
            x.Password == password);
}

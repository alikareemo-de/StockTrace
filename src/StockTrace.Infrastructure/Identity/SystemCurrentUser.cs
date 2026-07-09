using StockTrace.Application.Abstractions;

namespace StockTrace.Infrastructure.Identity;

internal sealed class SystemCurrentUser : ICurrentUser
{
    public string UserId => "system";
}

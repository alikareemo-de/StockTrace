using StockTrace.Application.Inventory;
using Microsoft.AspNetCore.SignalR;

namespace StockTrace.Api.Realtime;

public interface ILowStockClient
{
    Task StockChanged(StockChangedAlert alert);
    Task LowStockReached(LowStockAlert alert);
}

public sealed class LowStockHub : Hub<ILowStockClient>
{
    public const string Route = "/hubs/low-stock";
}

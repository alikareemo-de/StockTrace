using StockTrace.Application.Reports;
using StockTrace.Application.Inventory;
using StockTrace.Application.MasterData;
using StockTrace.Application.Purchases;
using StockTrace.Application.Sales;
using StockTrace.Application.Transfers;
using Microsoft.Extensions.DependencyInjection;

namespace StockTrace.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPurchaseReceiptService, PurchaseReceiptService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<IInventoryQueryService, InventoryQueryService>();
        services.AddScoped<IStockTransferService, StockTransferService>();
        services.AddScoped<IInventoryReportService, InventoryReportService>();
        services.AddScoped<IMasterDataService, MasterDataService>();
        return services;
    }
}

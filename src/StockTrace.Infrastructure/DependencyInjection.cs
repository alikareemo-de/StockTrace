using StockTrace.Application.Reports;
using StockTrace.Application.Abstractions;
using StockTrace.Application.Inventory;
using StockTrace.Application.MasterData;
using StockTrace.Application.Purchases;
using StockTrace.Application.Sales;
using StockTrace.Application.Transfers;
using StockTrace.Infrastructure.Identity;
using StockTrace.Infrastructure.Persistence;
using StockTrace.Infrastructure.Persistence.Interceptors;
using StockTrace.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace StockTrace.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICurrentUser, SystemCurrentUser>();
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            options
                .UseSqlServer(connectionString, sqlOptions =>
                    sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
                .AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>()));

        services.AddScoped<DatabaseInitializer>();
        services.AddScoped<IPurchaseReceiptRepository, PurchaseReceiptRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IInventoryAvailabilityRepository, InventoryAvailabilityRepository>();
        services.AddScoped<IInventoryStockRepository, InventoryStockRepository>();
        services.AddScoped<ITransactionRunner, TransactionRunner>();
        services.AddScoped<IStockTransferRepository, StockTransferRepository>();
        services.AddScoped<IInventoryReportRepository, InventoryReportRepository>();
        services.AddScoped<IMasterDataRepository, MasterDataRepository>();

        return services;
    }
}

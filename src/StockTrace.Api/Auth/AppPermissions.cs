namespace StockTrace.Api.Auth;

public static class AppClaimTypes
{
    public const string Permission = "permission";
}

public static class AppPermissions
{
    public const string MasterDataRead = "MasterData.Read";
    public const string MasterDataManageThresholds = "MasterData.ManageThresholds";
    public const string InventoryRead = "Inventory.Read";
    public const string PurchaseReceiptsRead = "PurchaseReceipts.Read";
    public const string PurchaseReceiptsCreate = "PurchaseReceipts.Create";
    public const string SalesRead = "Sales.Read";
    public const string SalesCreate = "Sales.Create";
    public const string StockTransfersRead = "StockTransfers.Read";
    public const string StockTransfersCreate = "StockTransfers.Create";
    public const string ReportsRead = "Reports.Read";
    public const string RealtimeRead = "Realtime.Read";

    public static readonly string[] All =
    [
        MasterDataRead,
        MasterDataManageThresholds,
        InventoryRead,
        PurchaseReceiptsRead,
        PurchaseReceiptsCreate,
        SalesRead,
        SalesCreate,
        StockTransfersRead,
        StockTransfersCreate,
        ReportsRead,
        RealtimeRead
    ];
}

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string WarehouseManager = "WarehouseManager";
    public const string SalesUser = "SalesUser";
    public const string Reporter = "Reporter";
    public const string InventoryViewer = "InventoryViewer";
}

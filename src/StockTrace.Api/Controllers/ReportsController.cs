using StockTrace.Api.Auth;
using StockTrace.Application.Reports;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StockTrace.Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController(IInventoryReportService service) : ControllerBase
{
    [HttpGet("inventory-movements")]
    [Authorize(Policy = AppPermissions.ReportsRead)]
    [ProducesResponseType<PagedResult<InventoryMovementReportItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<InventoryMovementReportItem>>> GetInventoryMovements(
        [FromQuery] InventoryMovementReportQuery query,
        CancellationToken cancellationToken) =>
        Ok(await service.GetMovementsAsync(query, cancellationToken));

    [HttpGet("inventory-movements/export")]
    [Authorize(Policy = AppPermissions.ReportsExport)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportInventoryMovements(
        [FromQuery] InventoryMovementReportQuery query,
        CancellationToken cancellationToken)
    {
        var exportQuery = query with { PageNumber = 1, PageSize = 100 };
        var report = await service.GetMovementsAsync(exportQuery, cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Inventory Movements");
        var headers = new[]
        {
            "Occurred At", "Warehouse", "Product SKU", "Product", "Category", "Supplier",
            "Lot Number", "Movement Type", "Quantity", "Unit Cost", "Value", "Reference Type", "Reference Id"
        };

        for (var column = 0; column < headers.Length; column++)
        {
            worksheet.Cell(1, column + 1).Value = headers[column];
        }

        var row = 2;
        foreach (var item in report.Items)
        {
            worksheet.Cell(row, 1).Value = item.OccurredAt.UtcDateTime;
            worksheet.Cell(row, 2).Value = item.WarehouseName;
            worksheet.Cell(row, 3).Value = item.ProductSku;
            worksheet.Cell(row, 4).Value = item.ProductName;
            worksheet.Cell(row, 5).Value = item.CategoryName;
            worksheet.Cell(row, 6).Value = item.SupplierName;
            worksheet.Cell(row, 7).Value = item.LotNumber;
            worksheet.Cell(row, 8).Value = item.MovementType;
            worksheet.Cell(row, 9).Value = item.Quantity;
            worksheet.Cell(row, 10).Value = item.UnitCost;
            worksheet.Cell(row, 11).Value = item.Value;
            worksheet.Cell(row, 12).Value = item.ReferenceType;
            worksheet.Cell(row, 13).Value = item.ReferenceId.ToString();
            row++;
        }

        worksheet.Columns().AdjustToContents();
        await using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"inventory-movements-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.xlsx");
    }
}

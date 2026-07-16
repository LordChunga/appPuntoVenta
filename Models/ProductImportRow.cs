namespace MiniPosWpf.Models;

public sealed record ProductImportRow(
    int Id,
    string Barcode,
    string Name,
    decimal SalePrice,
    decimal CostPrice,
    string CategoryName);

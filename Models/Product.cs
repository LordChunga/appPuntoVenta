namespace MiniPosWpf.Models;

public sealed class Product
{
    public int Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string InternalCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public decimal CostPrice { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string UnitType { get; set; } = "Unidad";
}

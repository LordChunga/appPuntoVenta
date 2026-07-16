using CommunityToolkit.Mvvm.ComponentModel;

namespace MiniPosWpf.Models;

public sealed partial class CartItem : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    [NotifyPropertyChangedFor(nameof(QuantityDisplay))]
    private decimal quantity;

    partial void OnQuantityChanged(decimal value)
    {
        if (UnitType == "Unidad" && value < 1)
        {
            Quantity = 1;
        }
        else if (value <= 0)
        {
            Quantity = 0.001m;
        }
    }

    public int ProductId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public decimal CostPrice { get; init; }
    public string UnitType { get; init; } = "Unidad";
    /// <summary>Stock disponible al momento de agregar el producto al carrito (usado para validación).</summary>
    public int StockAvailable { get; init; }
    public decimal LineTotal => UnitType == "Gramo" ? UnitPrice * (Quantity / 100m) : UnitPrice * Quantity;

    public bool IsUnitType => UnitType == "Unidad";
    public bool IsOutOfStock => StockAvailable <= 0;

    public string QuantityDisplay => UnitType switch
    {
        "Kilo" => $"{Quantity:F3} kg",
        "Gramo" => $"{Quantity:F0} g",
        "Litro" => $"{Quantity:F3} L",
        _ => $"{(int)Quantity}"
    };
}

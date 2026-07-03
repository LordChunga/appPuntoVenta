using CommunityToolkit.Mvvm.ComponentModel;

namespace MiniPosWpf.Models;

public sealed partial class CartItem : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    private int quantity;

    partial void OnQuantityChanged(int value)
    {
        if (value < 1)
        {
            Quantity = 1;
        }
    }

    public int ProductId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public decimal LineTotal => UnitPrice * Quantity;
}

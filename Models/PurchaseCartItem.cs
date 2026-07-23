using CommunityToolkit.Mvvm.ComponentModel;

namespace MiniPosWpf.Models;

public partial class PurchaseCartItem : ObservableObject
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    
    [ObservableProperty]
    private decimal costPrice;
    
    [ObservableProperty]
    private int quantity;
    
    [ObservableProperty]
    private bool modifySalePrice;
    
    [ObservableProperty]
    private decimal newSalePrice;
    
    public decimal LineTotal => CostPrice * Quantity;
}

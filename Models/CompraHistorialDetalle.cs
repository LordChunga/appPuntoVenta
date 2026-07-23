namespace MiniPosWpf.Models;

public class CompraHistorialDetalle
{
    public int Id { get; set; }
    public string CompraId { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

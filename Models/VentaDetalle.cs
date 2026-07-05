namespace MiniPosWpf.Models;

public sealed class VentaDetalle
{
    public int Id { get; set; }
    public string VentaId { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

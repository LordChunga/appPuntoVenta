namespace MiniPosWpf.Models;

public class CompraHistorial
{
    public string Id { get; set; } = string.Empty;
    public string Fecha { get; set; } = string.Empty;
    public decimal Total { get; set; }
    
    // Virtual property for UI
    public string ProductosResumen { get; set; } = string.Empty;
}

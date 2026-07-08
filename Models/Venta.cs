namespace MiniPosWpf.Models;

public sealed class Venta
{
    public string Id { get; set; } = string.Empty;          // GUID
    public DateTime Fecha { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public string MetodoPago { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public bool Factura { get; set; }
    public string Estado { get; set; } = "Completada";      // "Completada" | "Cancelada" | "Pendiente" | "En Deuda"
    public string InvoiceNumber { get; set; } = string.Empty;
    public int? ClientId { get; set; }

    // Calculado: resumen de productos para el DataGrid (llenado por query)
    public string ProductosResumen { get; set; } = string.Empty;
}

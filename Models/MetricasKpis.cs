namespace MiniPosWpf.Models;

/// <summary>KPIs calculados para la vista Métricas.</summary>
public sealed class MetricasKpis
{
    public int TotalVentas { get; set; }
    public decimal IngresosTotales { get; set; }
    public string MetodoPagoMasUsado { get; set; } = "-";
    public string ProductoMasPopular { get; set; } = "-";
}

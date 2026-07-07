namespace MiniPosWpf.Models;

public sealed class Client
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string FechaRegistro { get; set; } = string.Empty;
    public int TotalCompras { get; set; }
    public decimal DeudaTotal { get; set; }
}

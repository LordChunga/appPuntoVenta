using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniPosWpf.Data;
using MiniPosWpf.Models;

namespace MiniPosWpf.ViewModels;

public sealed partial class MetricasViewModel : ObservableObject
{
    private readonly StoreRepository repository;

    // ── KPIs ──────────────────────────────────────────────────────────────────
    [ObservableProperty] private int totalVentas;
    [ObservableProperty] private decimal ingresosTotales;
    [ObservableProperty] private string metodoPagoMasUsado = "-";
    [ObservableProperty] private string productoMasPopular = "-";

    // ── Lista de ventas ───────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<Venta> ventas = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelarVentaCommand))]
    [NotifyCanExecuteChangedFor(nameof(AceptarTransferenciaCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmarPagoCuentaCorrienteCommand))]
    private Venta? selectedVenta;

    [ObservableProperty] private bool isSaleDetailsVisible;
    [ObservableProperty] private ObservableCollection<VentaDetalle> selectedVentaDetalles = [];

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string statusMessage = string.Empty;

    // ── Tab activo dentro de Métricas ─────────────────────────────────────────
    [ObservableProperty] private int activeMetricasTab;

    public MetricasViewModel(StoreRepository repository)
    {
        this.repository = repository;
    }

    partial void OnSearchTextChanged(string value) => _ = LoadAsync();

    /// <summary>Carga KPIs y lista de ventas. Se llama al navegar a la pestaña.</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            // KPIs
            var kpis = await repository.GetMetricasKpisAsync();
            TotalVentas       = kpis.TotalVentas;
            IngresosTotales   = kpis.IngresosTotales;
            MetodoPagoMasUsado = kpis.MetodoPagoMasUsado;
            ProductoMasPopular = kpis.ProductoMasPopular;

            // DataGrid
            var rows = await repository.GetVentasAsync(SearchText);
            Ventas = new ObservableCollection<Venta>(rows);

            StatusMessage = $"Datos actualizados — {Ventas.Count} venta(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al cargar métricas: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancelarVenta))]
    private async Task CancelarVentaAsync()
    {
        if (SelectedVenta is null) return;

        try
        {
            await repository.CancelarVentaAsync(SelectedVenta.Id);
            StatusMessage = $"Venta {SelectedVenta.InvoiceNumber} cancelada.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo cancelar: {ex.Message}";
        }
    }

    private bool CanCancelarVenta() =>
        SelectedVenta is not null && SelectedVenta.Estado == "Completada";

    [RelayCommand(CanExecute = nameof(CanAceptarTransferencia))]
    private async Task AceptarTransferenciaAsync()
    {
        if (SelectedVenta is null) return;

        try
        {
            await repository.AceptarTransferenciaAsync(SelectedVenta.Id);
            StatusMessage = $"Transferencia {SelectedVenta.InvoiceNumber} aceptada.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo aceptar: {ex.Message}";
        }
    }

    private bool CanAceptarTransferencia() =>
        SelectedVenta is not null && SelectedVenta.Estado == "Pendiente" && SelectedVenta.MetodoPago == "Transferencia";

    [RelayCommand(CanExecute = nameof(CanConfirmarPagoCuentaCorriente))]
    private async Task ConfirmarPagoCuentaCorrienteAsync()
    {
        if (SelectedVenta is null || !SelectedVenta.ClientId.HasValue) return;

        try
        {
            await repository.ConfirmarPagoCuentaCorrienteAsync(SelectedVenta.Id, SelectedVenta.ClientId.Value, SelectedVenta.Total);
            StatusMessage = $"Pago de cuenta corriente confirmado (Factura: {SelectedVenta.InvoiceNumber}).";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo confirmar el pago: {ex.Message}";
        }
    }

    private bool CanConfirmarPagoCuentaCorriente() =>
        SelectedVenta is not null && SelectedVenta.Estado == "En Deuda" && SelectedVenta.MetodoPago == "Cuenta Corriente";

    [RelayCommand]
    private async Task ShowSaleDetailsAsync(Venta? venta)
    {
        var target = venta ?? SelectedVenta;
        if (target is null) return;

        var detalles = await repository.GetVentaDetallesAsync(target.Id);
        SelectedVentaDetalles = new ObservableCollection<VentaDetalle>(detalles);
        IsSaleDetailsVisible = true;
    }

    [RelayCommand]
    private void CloseSaleDetails()
    {
        IsSaleDetailsVisible = false;
        SelectedVentaDetalles.Clear();
    }
}

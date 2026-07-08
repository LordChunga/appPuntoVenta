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

    [RelayCommand]
    private async Task CancelarVentaAsync(Venta? venta)
    {
        var target = venta ?? SelectedVenta;
        if (target is null) return;

        if (target.Estado == "Cancelada")
        {
            StatusMessage = "Esta venta ya está cancelada.";
            return;
        }

        try
        {
            await repository.CancelarVentaAsync(target.Id);
            StatusMessage = $"Venta {target.InvoiceNumber} cancelada.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo cancelar: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task AceptarTransferenciaAsync(Venta? venta)
    {
        var target = venta ?? SelectedVenta;
        if (target is null) return;

        if (target.Estado != "Pendiente" || target.MetodoPago != "Transferencia")
        {
            StatusMessage = "Solo se pueden confirmar transferencias pendientes.";
            return;
        }

        try
        {
            await repository.AceptarTransferenciaAsync(target.Id);
            StatusMessage = $"Transferencia {target.InvoiceNumber} aceptada.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo aceptar: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task EliminarVentaAsync(Venta? venta)
    {
        var target = venta ?? SelectedVenta;
        if (target is null) return;

        if (target.Estado != "Cancelada")
        {
            StatusMessage = "Solo se pueden eliminar ventas que ya estén canceladas.";
            return;
        }

        try
        {
            await repository.EliminarVentaAsync(target.Id);
            StatusMessage = $"Venta {target.InvoiceNumber} eliminada correctamente.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo eliminar la venta: {ex.Message}";
        }
    }

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

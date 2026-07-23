using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using MiniPosWpf.Data;
using MiniPosWpf.Models;

namespace MiniPosWpf.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly StoreRepository repository;

    [ObservableProperty]
    private int activeTabIndex;

    [ObservableProperty]
    private ObservableCollection<Category> categories = [];

    [ObservableProperty]
    private ObservableCollection<Product> products = [];

    [ObservableProperty]
    private ObservableCollection<Product> saleProductResults = [];

    [ObservableProperty]
    private ObservableCollection<Product> purchaseProducts = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveProductCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteProductCommand))]
    private int editingProductId;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveProductCommand))]
    private string productBarcode = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveProductCommand))]
    private string productInternalCode = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveProductCommand))]
    private string productName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveProductCommand))]
    private decimal productSalePrice;

    [ObservableProperty]
    private decimal productCostPrice;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveProductCommand))]
    private int productStock;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveProductCommand))]
    private Category? selectedCategory;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateCategoryCommand))]
    private string newCategoryName = string.Empty;

    [ObservableProperty]
    private Product? selectedProduct;

    [ObservableProperty]
    private string productSearchText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddSaleSearchToCartCommand))]
    private string saleSearchText = string.Empty;

    [ObservableProperty]
    private string purchaseProductSearchText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddProductToPurchaseCartCommand))]
    private Product? purchaseProduct;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddProductToPurchaseCartCommand))]
    private int purchaseQuantity = 1;

    [ObservableProperty]
    private ObservableCollection<CompraHistorial> historialCompras = [];

    [ObservableProperty]
    private bool isNuevaCompraDialogOpen;

    [ObservableProperty]
    private ObservableCollection<PurchaseCartItem> purchaseCart = [];

    [ObservableProperty]
    private decimal purchaseCostPrice;

    [ObservableProperty]
    private bool purchaseModifySalePrice;

    [ObservableProperty]
    private decimal purchaseNewSalePrice;

    public decimal PurchaseSubtotal => PurchaseCart.Sum(item => item.LineTotal);
    public int PurchaseItemsCount => PurchaseCart.Count;



    // ── Caja Diaria ───────────────────────────────────────────

    [ObservableProperty]
    private decimal cajaActualTotal;

    [ObservableProperty]
    private bool isCajaInicialDialogOpen;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCajaInicialCommand))]
    private string cajaInicialInput = string.Empty;

    private readonly string todayString = DateTime.Now.ToString("yyyy-MM-dd");

    // ── Dialogs ───────────────────────────────────────────────

    [ObservableProperty]
    private ObservableCollection<CartItem> cart = [];

    // ── Clients ───────────────────────────────────────────────

    [ObservableProperty]
    private ObservableCollection<Client> clients = [];

    [ObservableProperty]
    private ObservableCollection<Client> posClients = [];

    [ObservableProperty]
    private Client? selectedPosClient;

    [ObservableProperty]
    private string clientSearchText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveClientCommand))]
    private int editingClientId;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveClientCommand))]
    private string clientNombre = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveClientCommand))]
    private string clientTelefono = string.Empty;

    [ObservableProperty]
    private Client? selectedClient;

    [ObservableProperty]
    private bool isClientFormVisible;

    [ObservableProperty]
    private bool isClientAccountVisible;

    [ObservableProperty]
    private ObservableCollection<Venta> currentClientDebts = [];

    [ObservableProperty]
    private string statusMessage = "Listo.";

    /// <summary>Opciones de método de pago disponibles en el POS.</summary>
    public List<string> PaymentMethods { get; } =
    [
        "Efectivo",
        "Tarjeta de Débito",
        "Tarjeta de Crédito",
        "Transferencia",
        "Mercado Pago",
        "Cuenta Corriente"
    ];

    [ObservableProperty]
    private string metodoPago = "Efectivo";

    [ObservableProperty]
    private bool isProductDialogOpen;

    // ── Unit Type ─────────────────────────────────────────────

    public List<string> UnitTypes { get; } = ["Unidad", "Kilo", "Gramo", "Litro"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PriceLabel))]
    [NotifyPropertyChangedFor(nameof(StockLabel))]
    private string productUnitType = "Unidad";

    public string PriceLabel => ProductUnitType switch
    {
        "Kilo" => "Precio de venta por kilo",
        "Gramo" => "Precio de venta por 100 gramos",
        "Litro" => "Precio de venta por litro",
        _ => "Precio de venta"
    };

    public string StockLabel => ProductUnitType switch
    {
        "Kilo" => "Stock actual (en kilos)",
        "Gramo" => "Stock actual (en gramos)",
        "Litro" => "Stock actual (en litros)",
        _ => "Stock actual (unidades)"
    };

    // ── Weight Dialog (POS) ──────────────────────────────────

    [ObservableProperty]
    private bool isWeightDialogOpen;

    [ObservableProperty]
    private Product? weightDialogProduct;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmWeightDialogCommand))]
    private string weightDialogQuantity = string.Empty;

    [ObservableProperty]
    private string weightDialogUnitLabel = string.Empty;

    // ── Custom Amount Dialog (POS) ───────────────────────────

    [ObservableProperty]
    private bool isCustomAmountDialogOpen;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCustomAmountCommand))]
    private string customAmountName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCustomAmountCommand))]
    private string customAmountPrice = string.Empty;

    public decimal Subtotal => Cart.Sum(item => item.LineTotal);
    public decimal Total => Subtotal;
    public int ItemsCount => Cart.Count;

    /// <summary>ViewModel de la pestaña Métricas, accesible desde la vista principal.</summary>
    public MetricasViewModel Metricas { get; }

    public MainViewModel(StoreRepository repository)
    {
        this.repository = repository;
        Metricas = new MetricasViewModel(repository);
        Cart.CollectionChanged += OnCartCollectionChanged;
    }

    partial void OnSelectedProductChanged(Product? value)
    {
        if (value is null)
        {
            return;
        }

        EditingProductId = value.Id;
        ProductBarcode = value.Barcode;
        ProductInternalCode = value.InternalCode;
        ProductName = value.Name;
        ProductSalePrice = value.SalePrice;
        ProductCostPrice = value.CostPrice;
        ProductStock = value.Stock;
        SelectedCategory = Categories.FirstOrDefault(category => category.Id == value.CategoryId);
        ProductUnitType = value.UnitType;
    }

    partial void OnProductSearchTextChanged(string value)
    {
        _ = SearchProductsAsync();
    }

    partial void OnSaleSearchTextChanged(string value)
    {
        _ = SearchSaleProductsAsync();
    }

    partial void OnPurchaseProductSearchTextChanged(string value)
    {
        _ = SearchPurchaseProductsAsync();
    }

    partial void OnClientSearchTextChanged(string value)
    {
        _ = SearchClientsAsync();
    }

    // Índice 4 = pestaña Métricas → cargar datos automáticamente al navegar
    partial void OnActiveTabIndexChanged(int value)
    {
        if (value == 4)
        {
            _ = Metricas.LoadAsync();
        }
        else if (value == 5)
        {
            _ = SearchClientsAsync();
        }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        await RefreshCategoriesAsync();
        await SearchProductsAsync();
        await SearchSaleProductsAsync();
        await SearchPurchaseProductsAsync();
        await RefreshPosClientsAsync();
        await InitCajaDiariaAsync();
        await RefreshHistorialComprasAsync();
        SelectedCategory ??= Categories.FirstOrDefault();
        StatusMessage = "Datos cargados.";
    }

    // ── Caja Diaria Methods ───────────────────────────────────

    private async Task InitCajaDiariaAsync()
    {
        var inicial = await repository.GetCajaInicialAsync(todayString);
        if (inicial is null)
        {
            CajaInicialInput = string.Empty;
            IsCajaInicialDialogOpen = true;
        }
        else
        {
            CajaInicialInput = inicial.Value.ToString("F2");
            await RefreshCajaTotalAsync();
        }
    }

    [RelayCommand]
    private void OpenEditCaja()
    {
        IsCajaInicialDialogOpen = true;
    }

    private bool CanConfirmCajaInicial() =>
        decimal.TryParse(CajaInicialInput.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var monto) && monto >= 0;

    [RelayCommand(CanExecute = nameof(CanConfirmCajaInicial))]
    private async Task ConfirmCajaInicialAsync()
    {
        if (decimal.TryParse(CajaInicialInput.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var monto) && monto >= 0)
        {
            await repository.SaveCajaInicialAsync(todayString, monto);
            IsCajaInicialDialogOpen = false;
            await RefreshCajaTotalAsync();
        }
    }

    private async Task RefreshCajaTotalAsync()
    {
        var inicial = await repository.GetCajaInicialAsync(todayString) ?? 0;
        var efectivoHoy = await repository.GetTotalVentasEfectivoHoyAsync(todayString);
        CajaActualTotal = inicial + efectivoHoy;
    }

    [RelayCommand(CanExecute = nameof(CanSaveProduct))]
    private async Task SaveProductAsync()
    {
        if (SelectedCategory is null)
        {
            StatusMessage = "Seleccioná una categoría.";
            return;
        }

        var product = new Product
        {
            Id = EditingProductId,
            Barcode = ProductBarcode.Trim(),
            InternalCode = ProductInternalCode.Trim(),
            Name = ProductName.Trim(),
            SalePrice = ProductSalePrice,
            CostPrice = ProductCostPrice,
            Stock = ProductStock,
            CategoryId = SelectedCategory.Id,
            UnitType = ProductUnitType
        };

        try
        {
            await repository.SaveProductAsync(product);
            StatusMessage = EditingProductId == 0 ? "Producto creado." : "Producto actualizado.";
            ClearProductForm();
            IsProductDialogOpen = false;
            await SearchProductsAsync();
            await SearchPurchaseProductsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo guardar: {ex.Message}";
        }
    }

    private bool CanSaveProduct()
    {
        return !string.IsNullOrWhiteSpace(ProductBarcode)
            && !string.IsNullOrWhiteSpace(ProductInternalCode)
            && !string.IsNullOrWhiteSpace(ProductName)
            && ProductSalePrice >= 0
            && ProductStock >= 0
            && SelectedCategory is not null;
    }

    [RelayCommand(CanExecute = nameof(CanCreateCategory))]
    private async Task CreateCategoryAsync()
    {
        try
        {
            var category = await repository.CreateCategoryAsync(NewCategoryName);
            await RefreshCategoriesAsync();
            SelectedCategory = Categories.FirstOrDefault(item => item.Id == category.Id);
            NewCategoryName = string.Empty;
            StatusMessage = $"Categoría '{category.Name}' asignada.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo crear la categoría: {ex.Message}";
        }
    }

    private bool CanCreateCategory() => !string.IsNullOrWhiteSpace(NewCategoryName);

    [RelayCommand]
    private void NewProduct()
    {
        ClearProductForm();
        StatusMessage = "Formulario listo para un nuevo producto.";
    }

    [RelayCommand]
    private void OpenProductDialog()
    {
        ClearProductForm();
        IsProductDialogOpen = true;
    }

    [RelayCommand]
    private void CloseProductDialog()
    {
        IsProductDialogOpen = false;
    }

    [RelayCommand]
    private void EditProductDialog()
    {
        if (SelectedProduct is null)
        {
            StatusMessage = "Seleccioná un producto de la tabla para editar.";
            return;
        }
        IsProductDialogOpen = true;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteProduct))]
    private async Task DeleteProductAsync()
    {
        try
        {
            await repository.DeleteProductAsync(EditingProductId);
            ClearProductForm();
            await SearchProductsAsync();
            await SearchPurchaseProductsAsync();
            StatusMessage = "Producto eliminado.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo eliminar: {ex.Message}";
        }
    }

    private bool CanDeleteProduct() => EditingProductId > 0;

    [RelayCommand]
    private async Task ImportProductsAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Importar productos",
            Filter = "Archivos Excel (*.xlsx)|*.xlsx",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var rows = XlsxProductImporter.ReadProducts(dialog.FileName, out var invalidRows);
            var result = await repository.ImportProductsAsync(rows);

            await RefreshCategoriesAsync();
            await SearchProductsAsync();
            await SearchPurchaseProductsAsync();
            ClearProductForm();

            StatusMessage = $"Importados: {result.ImportedProducts}. Omitidos: {result.SkippedProducts}. Categorias nuevas: {result.CreatedCategories}. Filas invalidas: {invalidRows}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo importar el archivo: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SearchProductsAsync()
    {
        var rows = await repository.SearchProductsAsync(ProductSearchText);
        Products = new ObservableCollection<Product>(rows);

        if (PurchaseProduct is not null)
        {
            PurchaseProduct = PurchaseProducts.FirstOrDefault(product => product.Id == PurchaseProduct.Id);
        }
    }

    [RelayCommand]
    private async Task SearchSaleProductsAsync()
    {
        if (string.IsNullOrWhiteSpace(SaleSearchText))
        {
            SaleProductResults = new ObservableCollection<Product>();
            return;
        }

        var rows = await repository.SearchProductsAsync(SaleSearchText);
        SaleProductResults = new ObservableCollection<Product>(rows);
    }

    [RelayCommand]
    private async Task SearchPurchaseProductsAsync()
    {
        var currentProductId = PurchaseProduct?.Id;
        var rows = await repository.SearchProductsAsync(PurchaseProductSearchText);
        PurchaseProducts = new ObservableCollection<Product>(rows);

        if (currentProductId is not null)
        {
            PurchaseProduct = PurchaseProducts.FirstOrDefault(product => product.Id == currentProductId);
        }
    }

    // ── Compras (Historial y Modal) ──────────────────────────

    [RelayCommand]
    private async Task RefreshHistorialComprasAsync()
    {
        var compras = await repository.GetHistorialComprasAsync();
        HistorialCompras = new ObservableCollection<CompraHistorial>(compras);
    }

    [RelayCommand]
    private void OpenNuevaCompraDialog()
    {
        PurchaseCart.Clear();
        PurchaseProductSearchText = string.Empty;
        PurchaseProduct = null;
        PurchaseQuantity = 1;
        PurchaseCostPrice = 0;
        PurchaseModifySalePrice = false;
        PurchaseNewSalePrice = 0;
        IsNuevaCompraDialogOpen = true;
    }

    [RelayCommand]
    private void CloseNuevaCompraDialog()
    {
        IsNuevaCompraDialogOpen = false;
    }

    partial void OnPurchaseProductChanged(Product? value)
    {
        if (value is not null)
        {
            PurchaseCostPrice = value.CostPrice;
            PurchaseModifySalePrice = false;
            PurchaseNewSalePrice = value.SalePrice;
            PurchaseQuantity = 1;
        }

        try
        {
            AddProductToPurchaseCartCommand.NotifyCanExecuteChanged();
        }
        catch
        {
            // Ignore during re-entrant property change cascades
        }
    }

    private bool CanAddProductToPurchaseCart() => PurchaseProduct is not null && PurchaseQuantity > 0 && PurchaseCostPrice >= 0;

    [RelayCommand(CanExecute = nameof(CanAddProductToPurchaseCart))]
    private void AddProductToPurchaseCart()
    {
        if (PurchaseProduct is null) return;

        var productName = PurchaseProduct.Name;

        var item = new PurchaseCartItem
        {
            ProductId = PurchaseProduct.Id,
            Name = PurchaseProduct.Name,
            Quantity = PurchaseQuantity,
            CostPrice = PurchaseCostPrice,
            ModifySalePrice = PurchaseModifySalePrice,
            NewSalePrice = PurchaseModifySalePrice ? PurchaseNewSalePrice : PurchaseProduct.SalePrice
        };

        PurchaseCart.Add(item);
        OnPropertyChanged(nameof(PurchaseSubtotal));
        OnPropertyChanged(nameof(PurchaseItemsCount));
        ConfirmPurchaseCartCommand.NotifyCanExecuteChanged();

        // Reset form — null PurchaseProduct first to avoid re-entrant cascade
        // when SearchPurchaseProductsAsync replaces the collection.
        PurchaseProduct = null;
        PurchaseProductSearchText = string.Empty;

        StatusMessage = $"{productName} agregado a la nueva compra.";
    }

    [RelayCommand]
    private void RemovePurchaseCartItem(PurchaseCartItem? item)
    {
        if (item is null) return;
        PurchaseCart.Remove(item);
        OnPropertyChanged(nameof(PurchaseSubtotal));
        OnPropertyChanged(nameof(PurchaseItemsCount));
        ConfirmPurchaseCartCommand.NotifyCanExecuteChanged();
    }

    private bool CanConfirmPurchaseCart() => PurchaseCart.Count > 0;

    [RelayCommand(CanExecute = nameof(CanConfirmPurchaseCart))]
    private async Task ConfirmPurchaseCartAsync()
    {
        try
        {
            await repository.ConfirmPurchaseCartAsync(PurchaseCart);
            IsNuevaCompraDialogOpen = false;
            PurchaseCart.Clear();
            await RefreshHistorialComprasAsync();
            await SearchProductsAsync();
            StatusMessage = "Compra confirmada y stock actualizado.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al confirmar compra: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddSaleSearchToCart))]
    private async Task AddSaleSearchToCartAsync()
    {
        var search = SaleSearchText.Trim();
        var product = await repository.GetProductByCodeAsync(search);
        product ??= SaleProductResults.Count == 1 ? SaleProductResults[0] : null;

        if (product is null)
        {
            StatusMessage = "Busca por id, nombre o codigo y selecciona un producto.";
            return;
        }

        AddProductToCart(product);
        SaleSearchText = string.Empty;
        SaleProductResults = new ObservableCollection<Product>();
    }

    private bool CanAddSaleSearchToCart() => !string.IsNullOrWhiteSpace(SaleSearchText);

    [RelayCommand]
    private void AddProductToCart(Product? product)
    {
        if (product is null)
        {
            return;
        }

        // For non-unit products, open the weight dialog
        if (product.UnitType != "Unidad")
        {
            WeightDialogProduct = product;
            WeightDialogQuantity = string.Empty;
            WeightDialogUnitLabel = product.UnitType switch
            {
                "Kilo" => "Cantidad en kilos (kg)",
                "Gramo" => "Cantidad en gramos (g)",
                "Litro" => "Cantidad en litros (L)",
                _ => "Cantidad"
            };
            IsWeightDialogOpen = true;
            return;
        }

        // Unit products: normal flow
        var cartItem = Cart.FirstOrDefault(item => item.ProductId == product.Id);
        if (cartItem is null)
        {


            Cart.Add(new CartItem
            {
                ProductId = product.Id,
                Code = string.IsNullOrWhiteSpace(product.Barcode) ? product.InternalCode : product.Barcode,
                Name = product.Name,
                UnitPrice = product.SalePrice,
                CostPrice = product.CostPrice,
                UnitType = product.UnitType,
                Quantity = 1,
                StockAvailable = product.Stock
            });
        }
        else
        {


            cartItem.Quantity++;
            RefreshTotals();
        }

        StatusMessage = $"{product.Name} agregado al carrito.";
        ConfirmSaleCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanConfirmWeightDialog))]
    private void ConfirmWeightDialog()
    {
        if (WeightDialogProduct is null)
        {
            return;
        }

        if (!decimal.TryParse(WeightDialogQuantity.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var qty) || qty <= 0)
        {
            StatusMessage = "Ingresá una cantidad válida mayor a 0.";
            return;
        }

        Cart.Add(new CartItem
        {
            ProductId = WeightDialogProduct.Id,
            Code = string.IsNullOrWhiteSpace(WeightDialogProduct.Barcode)
                ? WeightDialogProduct.InternalCode
                : WeightDialogProduct.Barcode,
            Name = WeightDialogProduct.Name,
            UnitPrice = WeightDialogProduct.SalePrice,
            CostPrice = WeightDialogProduct.CostPrice,
            UnitType = WeightDialogProduct.UnitType,
            Quantity = qty,
            StockAvailable = WeightDialogProduct.Stock
        });

        StatusMessage = $"{WeightDialogProduct.Name} agregado al carrito.";
        IsWeightDialogOpen = false;
        WeightDialogProduct = null;
        WeightDialogQuantity = string.Empty;
        ConfirmSaleCommand.NotifyCanExecuteChanged();
        SaleSearchText = string.Empty;
        SaleProductResults = new ObservableCollection<Product>();
    }

    private bool CanConfirmWeightDialog() => !string.IsNullOrWhiteSpace(WeightDialogQuantity);

    [RelayCommand]
    private void CloseWeightDialog()
    {
        IsWeightDialogOpen = false;
        WeightDialogProduct = null;
        WeightDialogQuantity = string.Empty;
    }

    [RelayCommand]
    private void OpenCustomAmountDialog()
    {
        CustomAmountName = "Varios";
        CustomAmountPrice = string.Empty;
        IsCustomAmountDialogOpen = true;
    }

    [RelayCommand]
    private void CloseCustomAmountDialog()
    {
        IsCustomAmountDialogOpen = false;
        CustomAmountName = string.Empty;
        CustomAmountPrice = string.Empty;
    }

    private bool CanConfirmCustomAmount() =>
        !string.IsNullOrWhiteSpace(CustomAmountName) &&
        decimal.TryParse(CustomAmountPrice.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var price) && price > 0;

    [RelayCommand(CanExecute = nameof(CanConfirmCustomAmount))]
    private void ConfirmCustomAmount()
    {
        if (decimal.TryParse(CustomAmountPrice.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var price) && price > 0)
        {
            var cartItem = new CartItem
            {
                ProductId = 0,
                Code = "MANUAL",
                Name = CustomAmountName.Trim(),
                UnitPrice = price,
                CostPrice = 0,
                UnitType = "Unidad",
                Quantity = 1,
                StockAvailable = 999999
            };

            Cart.Add(cartItem);
            RefreshTotals();
            ConfirmSaleCommand.NotifyCanExecuteChanged();

            IsCustomAmountDialogOpen = false;
            CustomAmountName = string.Empty;
            CustomAmountPrice = string.Empty;
        }
    }

    [RelayCommand]
    private void RemoveCartItem(CartItem? item)
    {
        if (item is null)
        {
            return;
        }

        Cart.Remove(item);
        StatusMessage = $"{item.Name} eliminado del carrito.";
        ConfirmSaleCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void IncrementCartItem(CartItem? item)
    {
        if (item is null || !item.IsUnitType)
        {
            return;
        }



        item.Quantity++;
        RefreshTotals();
    }

    [RelayCommand]
    private void DecrementCartItem(CartItem? item)
    {
        if (item is null)
        {
            return;
        }

        // For weight items, just remove entirely
        if (!item.IsUnitType)
        {
            RemoveCartItem(item);
            return;
        }

        if (item.Quantity > 1)
        {
            item.Quantity--;
            return;
        }

        RemoveCartItem(item);
    }

    [RelayCommand(CanExecute = nameof(CanConfirmSale))]
    private async Task ConfirmSaleAsync()
    {
        try
        {
            if (MetodoPago == "Cuenta Corriente" && (SelectedPosClient == null || SelectedPosClient.Id == 0))
            {
                StatusMessage = "Debe seleccionar un cliente registrado para vender a cuenta corriente.";
                return;
            }

            int? finalClientId = SelectedPosClient?.Id > 0 ? SelectedPosClient.Id : null;
            string finalClienteName = "Consumidor Final";

            await repository.ConfirmSaleAsync(Cart, metodoPago: MetodoPago, cliente: finalClienteName, clientId: finalClientId);
            await RefreshCajaTotalAsync();
            
            Cart.Clear();
            MetodoPago = "Efectivo";   // resetear al método por defecto
            SelectedPosClient = PosClients.FirstOrDefault();  // resetear cliente a Consumidor Final
            await SearchProductsAsync();
            await SearchPurchaseProductsAsync();
            // Refrescar métricas en background para mantener KPIs actualizados
            _ = Metricas.LoadAsync();
            StatusMessage = "Venta confirmada.";
            ConfirmSaleCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo confirmar la venta: {ex.Message}";
        }
    }

    private bool CanConfirmSale() => Cart.Count > 0;

    /// <summary>Cambia el método de pago activo desde los botones del panel POS.</summary>
    [RelayCommand]
    private void SetMetodoPago(string metodo)
    {
        if (!string.IsNullOrWhiteSpace(metodo))
            MetodoPago = metodo;
    }

    [RelayCommand]
    private void ClearCart()
    {
        Cart.Clear();
        StatusMessage = "Carrito vaciado.";
        ConfirmSaleCommand.NotifyCanExecuteChanged();
    }

    // ── Client Commands ───────────────────────────────────────

    [RelayCommand]
    private async Task SearchClientsAsync()
    {
        var rows = await repository.SearchClientsAsync(ClientSearchText);
        Clients = new ObservableCollection<Client>(rows);
    }

    [RelayCommand(CanExecute = nameof(CanSaveClient))]
    private async Task SaveClientAsync()
    {
        try
        {
            var client = new Client
            {
                Id = EditingClientId,
                Nombre = ClientNombre.Trim(),
                Telefono = ClientTelefono.Trim()
            };

            await repository.SaveClientAsync(client);
            StatusMessage = EditingClientId == 0 ? "Cliente creado." : "Cliente actualizado.";
            ClearClientForm();
            IsClientFormVisible = false;
            await SearchClientsAsync();
            await RefreshPosClientsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo guardar el cliente: {ex.Message}";
        }
    }

    private bool CanSaveClient() => !string.IsNullOrWhiteSpace(ClientNombre);

    [RelayCommand]
    private async Task DeleteClientAsync(Client? client)
    {
        var target = client ?? SelectedClient;
        if (target is null) return;

        if (target.DeudaTotal > 0)
        {
            StatusMessage = "No se puede eliminar este cliente porque tiene una deuda pendiente.";
            return;
        }

        bool hasPendingSales = await repository.HasPendingSalesAsync(target.Id);
        if (hasPendingSales)
        {
            StatusMessage = "No se puede borrar este cliente porque tiene una transferencia pendiente.";
            return;
        }

        try
        {
            await repository.DeleteClientAsync(target.Id);
            StatusMessage = $"Cliente '{target.Nombre}' eliminado.";
            ClearClientForm();
            await SearchClientsAsync();
            await RefreshPosClientsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo eliminar: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenEditClient(Client? client)
    {
        if (client is null) return;
        EditClient(client);
        IsClientFormVisible = true;
    }

    [RelayCommand]
    private void OpenNewClient()
    {
        ClearClientForm();
        IsClientFormVisible = true;
        StatusMessage = "Formulario listo para un nuevo cliente.";
    }

    [RelayCommand]
    private void CancelClientEdit()
    {
        ClearClientForm();
        IsClientFormVisible = false;
        StatusMessage = "Operación cancelada.";
    }

    [RelayCommand]
    private async Task OpenClientAccountAsync(Client? client)
    {
        var target = client ?? SelectedClient;
        if (target is null) return;

        SelectedClient = target;
        CurrentClientDebts.Clear();
        var debts = await repository.GetPendingDebtsByClientIdAsync(target.Id);
        foreach (var d in debts)
        {
            CurrentClientDebts.Add(d);
        }
        IsClientAccountVisible = true;
    }

    [RelayCommand]
    private void CloseClientAccount()
    {
        IsClientAccountVisible = false;
        CurrentClientDebts.Clear();
    }

    [RelayCommand]
    private async Task PayClientDebtAsync(Venta? venta)
    {
        if (venta is null || SelectedClient is null) return;

        try
        {
            await repository.ConfirmarPagoCuentaCorrienteAsync(venta.Id, SelectedClient.Id, venta.Total);
            StatusMessage = $"Deuda cancelada (Factura: {venta.InvoiceNumber}).";
            
            CurrentClientDebts.Remove(venta);
            await SearchClientsAsync();
            
            var updatedClient = Clients.FirstOrDefault(c => c.Id == SelectedClient.Id);
            if (updatedClient != null)
            {
                SelectedClient = updatedClient;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al cancelar deuda: {ex.Message}";
        }
    }

    [RelayCommand]
    private void EditClient(Client? client)
    {
        if (client is null) return;

        EditingClientId = client.Id;
        ClientNombre = client.Nombre;
        ClientTelefono = client.Telefono;
        SelectedClient = client;
    }

    [RelayCommand]
    private void NewClient()
    {
        ClearClientForm();
        StatusMessage = "Formulario listo para un nuevo cliente.";
    }

    private void ClearClientForm()
    {
        EditingClientId = 0;
        ClientNombre = string.Empty;
        ClientTelefono = string.Empty;
        SelectedClient = null;
    }

    private async Task RefreshPosClientsAsync()
    {
        var rows = await repository.GetAllClientsAsync();
        var list = new ObservableCollection<Client>(rows);
        list.Insert(0, new Client { Id = 0, Nombre = "Consumidor Final" });
        PosClients = list;
        SelectedPosClient = list.FirstOrDefault();
    }

    private async Task RefreshCategoriesAsync()
    {
        var rows = await repository.GetCategoriesAsync();
        Categories = new ObservableCollection<Category>(rows);
    }

    private void ClearProductForm()
    {
        EditingProductId = 0;
        ProductBarcode = string.Empty;
        ProductInternalCode = string.Empty;
        ProductName = string.Empty;
        ProductSalePrice = 0;
        ProductCostPrice = 0;
        ProductStock = 0;
        SelectedCategory = Categories.FirstOrDefault();
        SelectedProduct = null;
        ProductUnitType = "Unidad";
    }

    private void RefreshTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(ItemsCount));
    }

    private void OnCartCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (CartItem item in e.OldItems)
            {
                item.PropertyChanged -= OnCartItemPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (CartItem item in e.NewItems)
            {
                item.PropertyChanged += OnCartItemPropertyChanged;
            }
        }

        RefreshTotals();
    }

    private void OnCartItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CartItem.Quantity) or nameof(CartItem.LineTotal))
        {
            RefreshTotals();
            ConfirmSaleCommand.NotifyCanExecuteChanged();
        }
    }
}

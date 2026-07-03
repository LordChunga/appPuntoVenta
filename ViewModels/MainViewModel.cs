using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniPosWpf.Data;
using MiniPosWpf.Models;

namespace MiniPosWpf.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly StoreRepository repository;

    [ObservableProperty]
    private ObservableCollection<Category> categories = [];

    [ObservableProperty]
    private ObservableCollection<Product> products = [];

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
    private string inventorySearchText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmPurchaseCommand))]
    private Product? purchaseProduct;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmPurchaseCommand))]
    private int purchaseQuantity = 1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCodeToCartCommand))]
    private string saleCode = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCodeToCartCommand))]
    private string busquedaProducto = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> clientes = ["Consumidor Final", "Cliente registrado"];

    [ObservableProperty]
    private string clienteSeleccionado = "Consumidor Final";

    [ObservableProperty]
    private decimal descuentos;

    [ObservableProperty]
    private decimal recargos;

    [ObservableProperty]
    private ObservableCollection<CartItem> cart = [];

    [ObservableProperty]
    private string statusMessage = "Listo.";

    public decimal Subtotal => Cart.Sum(item => item.LineTotal);
    public decimal Total => Subtotal - Descuentos + Recargos;
    public int ItemsCount => Cart.Sum(item => item.Quantity);

    public MainViewModel(StoreRepository repository)
    {
        this.repository = repository;
        Cart.CollectionChanged += (_, _) => RefreshTotals();
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
        ProductStock = value.Stock;
        SelectedCategory = Categories.FirstOrDefault(category => category.Id == value.CategoryId);
    }

    partial void OnInventorySearchTextChanged(string value)
    {
        _ = SearchProductsAsync();
    }

    partial void OnDescuentosChanged(decimal value)
    {
        OnPropertyChanged(nameof(Total));
    }

    partial void OnRecargosChanged(decimal value)
    {
        OnPropertyChanged(nameof(Total));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        await RefreshCategoriesAsync();
        await SearchProductsAsync();
        SelectedCategory ??= Categories.FirstOrDefault();
        StatusMessage = "Datos cargados.";
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
            Stock = ProductStock,
            CategoryId = SelectedCategory.Id
        };

        try
        {
            await repository.SaveProductAsync(product);
            StatusMessage = EditingProductId == 0 ? "Producto creado." : "Producto actualizado.";
            ClearProductForm();
            await SearchProductsAsync();
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

    [RelayCommand(CanExecute = nameof(CanDeleteProduct))]
    private async Task DeleteProductAsync()
    {
        try
        {
            await repository.DeleteProductAsync(EditingProductId);
            ClearProductForm();
            await SearchProductsAsync();
            StatusMessage = "Producto eliminado.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo eliminar: {ex.Message}";
        }
    }

    private bool CanDeleteProduct() => EditingProductId > 0;

    [RelayCommand]
    private async Task SearchProductsAsync()
    {
        var rows = await repository.SearchProductsAsync(InventorySearchText);
        Products = new ObservableCollection<Product>(rows);

        if (PurchaseProduct is not null)
        {
            PurchaseProduct = Products.FirstOrDefault(product => product.Id == PurchaseProduct.Id);
        }
    }

    [RelayCommand(CanExecute = nameof(CanConfirmPurchase))]
    private async Task ConfirmPurchaseAsync()
    {
        if (PurchaseProduct is null)
        {
            return;
        }

        await repository.AddStockAsync(PurchaseProduct.Id, PurchaseQuantity);
        StatusMessage = $"Stock ingresado: {PurchaseProduct.Name} +{PurchaseQuantity}.";
        PurchaseQuantity = 1;
        await SearchProductsAsync();
    }

    private bool CanConfirmPurchase() => PurchaseProduct is not null && PurchaseQuantity > 0;

    [RelayCommand(CanExecute = nameof(CanAddCodeToCart))]
    private async Task AddCodeToCartAsync()
    {
        var code = string.IsNullOrWhiteSpace(BusquedaProducto)
            ? SaleCode.Trim()
            : BusquedaProducto.Trim();
        var product = await repository.GetProductByCodeAsync(code);

        if (product is null)
        {
            StatusMessage = "No se encontró un producto con ese código.";
            return;
        }

        var cartItem = Cart.FirstOrDefault(item => item.ProductId == product.Id);
        if (cartItem is null)
        {
            Cart.Add(new CartItem
            {
                ProductId = product.Id,
                Code = string.IsNullOrWhiteSpace(product.Barcode) ? product.InternalCode : product.Barcode,
                Name = product.Name,
                UnitPrice = product.SalePrice,
                Quantity = 1
            });
        }
        else
        {
            cartItem.Quantity++;
            RefreshTotals();
        }

        SaleCode = string.Empty;
        BusquedaProducto = string.Empty;
        StatusMessage = $"{product.Name} agregado al carrito.";
        ConfirmSaleCommand.NotifyCanExecuteChanged();
        ProcesarVentaCommand.NotifyCanExecuteChanged();
    }

    private bool CanAddCodeToCart()
    {
        return !string.IsNullOrWhiteSpace(SaleCode)
            || !string.IsNullOrWhiteSpace(BusquedaProducto);
    }

    [RelayCommand(CanExecute = nameof(CanConfirmSale))]
    private async Task ConfirmSaleAsync()
    {
        try
        {
            await repository.ConfirmSaleAsync(Cart);
            Cart.Clear();
            await SearchProductsAsync();
            StatusMessage = "Venta confirmada.";
            ConfirmSaleCommand.NotifyCanExecuteChanged();
            ProcesarVentaCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo confirmar la venta: {ex.Message}";
        }
    }

    private bool CanConfirmSale() => Cart.Count > 0;

    [RelayCommand]
    private void ClearCart()
    {
        Cart.Clear();
        StatusMessage = "Carrito vaciado.";
        ConfirmSaleCommand.NotifyCanExecuteChanged();
        ProcesarVentaCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanConfirmSale))]
    private async Task ProcesarVentaAsync()
    {
        await ConfirmSaleAsync();
    }

    [RelayCommand]
    private void GestionarExtras()
    {
        StatusMessage = "Gestión de extras pendiente para el próximo paso del MVP.";
    }

    [RelayCommand]
    private void NuevoProducto()
    {
        ClearProductForm();
        StatusMessage = "Formulario listo para cargar un nuevo producto.";
    }

    [RelayCommand]
    private void MasOpciones()
    {
        StatusMessage = "Menú de más opciones pendiente.";
    }

    [RelayCommand]
    private void ConsultarPrecio()
    {
        StatusMessage = "Ingresá o escaneá un código para consultar el precio.";
    }

    [RelayCommand]
    private void IngresarPeso()
    {
        StatusMessage = "Ingreso por peso pendiente.";
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
        ProductStock = 0;
        SelectedCategory = Categories.FirstOrDefault();
        SelectedProduct = null;
    }

    private void RefreshTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(ItemsCount));
    }
}

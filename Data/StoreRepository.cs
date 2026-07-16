using Dapper;
using MiniPosWpf.Models;

namespace MiniPosWpf.Data;

public sealed class StoreRepository(Database database)
{
    public async Task<IReadOnlyList<Category>> GetCategoriesAsync()
    {
        using var connection = database.CreateConnection();
        var rows = await connection.QueryAsync<Category>(
            "SELECT Id, Name FROM Categories ORDER BY Name;");
        return rows.ToList();
    }

    public async Task<Category> CreateCategoryAsync(string name)
    {
        using var connection = database.CreateConnection();
        var normalizedName = name.Trim();

        var existing = await connection.QuerySingleOrDefaultAsync<Category>(
            "SELECT Id, Name FROM Categories WHERE Name = @Name COLLATE NOCASE;",
            new { Name = normalizedName });

        if (existing is not null)
        {
            return existing;
        }

        var id = await connection.ExecuteScalarAsync<long>(
            "INSERT INTO Categories (Name) VALUES (@Name); SELECT last_insert_rowid();",
            new { Name = normalizedName });

        return new Category { Id = (int)id, Name = normalizedName };
    }

    public async Task<IReadOnlyList<Product>> SearchProductsAsync(string? searchText = null)
    {
        using var connection = database.CreateConnection();
        var term = $"%{searchText?.Trim() ?? string.Empty}%";

        var rows = await connection.QueryAsync<Product>("""
            SELECT
                p.Id,
                p.Barcode,
                p.InternalCode,
                p.Name,
                p.SalePrice,
                p.Stock,
                p.CategoryId,
                c.Name AS CategoryName,
                p.UnitType
            FROM Products p
            INNER JOIN Categories c ON c.Id = p.CategoryId
            WHERE @Search = ''
               OR CAST(p.Id AS TEXT) LIKE @Term
               OR p.Name LIKE @Term
               OR p.Barcode LIKE @Term
               OR p.InternalCode LIKE @Term
            ORDER BY p.Name;
            """, new { Search = searchText?.Trim() ?? string.Empty, Term = term });

        return rows.ToList();
    }

    public async Task<Product?> GetProductByCodeAsync(string code)
    {
        using var connection = database.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Product>("""
            SELECT
                p.Id,
                p.Barcode,
                p.InternalCode,
                p.Name,
                p.SalePrice,
                p.Stock,
                p.CategoryId,
                c.Name AS CategoryName,
                p.UnitType
            FROM Products p
            INNER JOIN Categories c ON c.Id = p.CategoryId
            WHERE CAST(p.Id AS TEXT) = @Code
               OR p.Barcode = @Code COLLATE NOCASE
               OR p.InternalCode = @Code COLLATE NOCASE;
            """, new { Code = code.Trim() });
    }

    public async Task SaveProductAsync(Product product)
    {
        using var connection = database.CreateConnection();

        if (product.Id == 0)
        {
            await connection.ExecuteAsync("""
                INSERT INTO Products (Barcode, InternalCode, Name, SalePrice, Stock, CategoryId, UnitType)
                VALUES (@Barcode, @InternalCode, @Name, @SalePrice, @Stock, @CategoryId, @UnitType);
                """, product);
            return;
        }

        await connection.ExecuteAsync("""
            UPDATE Products
            SET Barcode = @Barcode,
                InternalCode = @InternalCode,
                Name = @Name,
                SalePrice = @SalePrice,
                Stock = @Stock,
                CategoryId = @CategoryId,
                UnitType = @UnitType
            WHERE Id = @Id;
            """, product);
    }

    public async Task<ProductImportResult> ImportProductsAsync(IEnumerable<ProductImportRow> products)
    {
        using var connection = database.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var importedProducts = 0;
        var skippedProducts = 0;
        var createdCategories = 0;

        foreach (var product in products)
        {
            // Check if a product with the same non-empty barcode or same name already exists
            int exists;
            if (!string.IsNullOrWhiteSpace(product.Barcode))
            {
                exists = await connection.ExecuteScalarAsync<int>("""
                    SELECT COUNT(1)
                    FROM Products
                    WHERE Barcode = @Barcode COLLATE NOCASE
                       OR Name = @Name COLLATE NOCASE;
                    """, new
                    {
                        product.Barcode,
                        product.Name
                    }, transaction);
            }
            else
            {
                exists = await connection.ExecuteScalarAsync<int>("""
                    SELECT COUNT(1)
                    FROM Products
                    WHERE Name = @Name COLLATE NOCASE;
                    """, new
                    {
                        product.Name
                    }, transaction);
            }

            if (exists > 0)
            {
                skippedProducts++;
                continue;
            }

            var categoryName = product.CategoryName.Trim();
            var categoryId = await connection.QuerySingleOrDefaultAsync<int?>(
                "SELECT Id FROM Categories WHERE Name = @Name COLLATE NOCASE;",
                new { Name = categoryName },
                transaction);

            if (categoryId is null)
            {
                var newCategoryId = await connection.ExecuteScalarAsync<long>(
                    "INSERT INTO Categories (Name) VALUES (@Name); SELECT last_insert_rowid();",
                    new { Name = categoryName },
                    transaction);

                categoryId = (int)newCategoryId;
                createdCategories++;
            }

            await connection.ExecuteAsync("""
                INSERT INTO Products (Barcode, InternalCode, Name, SalePrice, Stock, CategoryId)
                VALUES (@Barcode, @InternalCode, @Name, @SalePrice, 0, @CategoryId);
                """, new
                {
                    Barcode = product.Barcode ?? string.Empty,
                    InternalCode = product.Id.ToString(),
                    product.Name,
                    product.SalePrice,
                    CategoryId = categoryId.Value
                }, transaction);

            importedProducts++;
        }

        transaction.Commit();
        return new ProductImportResult(importedProducts, skippedProducts, createdCategories, 0);
    }

    // ── Clients ───────────────────────────────────────────────

    public async Task<IReadOnlyList<Client>> SearchClientsAsync(string? searchText = null)
    {
        using var connection = database.CreateConnection();
        var term = $"%{searchText?.Trim() ?? string.Empty}%";

        var rows = await connection.QueryAsync<Client>("""
            SELECT Id, Nombre, Telefono, FechaRegistro, TotalCompras, DeudaTotal
            FROM Clients
            WHERE @Search = ''
               OR Nombre LIKE @Term
               OR Telefono LIKE @Term
            ORDER BY Nombre;
            """, new { Search = searchText?.Trim() ?? string.Empty, Term = term });

        return rows.ToList();
    }

    public async Task<IReadOnlyList<Client>> GetAllClientsAsync()
    {
        using var connection = database.CreateConnection();
        var rows = await connection.QueryAsync<Client>(
            "SELECT Id, Nombre, Telefono, FechaRegistro, TotalCompras, DeudaTotal FROM Clients ORDER BY Nombre;");
        return rows.ToList();
    }

    public async Task SaveClientAsync(Client client)
    {
        using var connection = database.CreateConnection();

        if (client.Id == 0)
        {
            await connection.ExecuteAsync("""
                INSERT INTO Clients (Nombre, Telefono)
                VALUES (@Nombre, @Telefono);
                """, client);
            return;
        }

        await connection.ExecuteAsync("""
            UPDATE Clients
            SET Nombre = @Nombre,
                Telefono = @Telefono,
                DeudaTotal = @DeudaTotal
            WHERE Id = @Id;
            """, client);
    }

    public async Task DeleteClientAsync(int clientId)
    {
        using var connection = database.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync("UPDATE Ventas SET ClientId = NULL WHERE ClientId = @Id;", new { Id = clientId }, transaction);
            await connection.ExecuteAsync("DELETE FROM Clients WHERE Id = @Id;", new { Id = clientId }, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task DeleteProductAsync(int productId)
    {
        using var connection = database.CreateConnection();
        await connection.ExecuteAsync("DELETE FROM Products WHERE Id = @ProductId;", new { ProductId = productId });
    }

    public async Task AddStockAsync(int productId, int quantity)
    {
        using var connection = database.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE Products SET Stock = Stock + @Quantity WHERE Id = @ProductId;",
            new { ProductId = productId, Quantity = quantity });
    }

    /// <summary>
    /// Confirma la venta: descuenta stock, inserta en Ventas y VentasDetalle dentro de una
    /// única transacción. Retorna el Id (GUID) de la venta creada.
    /// </summary>
    public async Task<string> ConfirmSaleAsync(
        IEnumerable<CartItem> cartItems,
        string metodoPago = "Efectivo",
        string cliente = "Consumidor Final",
        bool factura = false,
        string usuario = "Isabel",
        int? clientId = null)
    {
        using var connection = database.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var ventaId = Guid.NewGuid().ToString();
        var items = cartItems.ToList();
        var total = items.Sum(i => i.LineTotal);
        var estado = metodoPago == "Transferencia" ? "Pendiente" : 
                     metodoPago == "Cuenta Corriente" ? "En Deuda" : "Completada";

        // Generate InvoiceNumber
        var todayStr = DateTime.Now.ToString("yyMMdd");
        var countToday = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Ventas WHERE date(Fecha) = date('now', 'localtime');", transaction);
        var invoiceNumber = $"{todayStr}-{(countToday + 1):D3}";

        // 1. Descontar stock permitiendo negativos y omitiendo productos manuales (Id == 0)
        foreach (var item in items)
        {
            if (item.ProductId == 0) continue;

            await connection.ExecuteAsync("""
                UPDATE Products
                SET Stock = Stock - @Quantity
                WHERE Id = @ProductId;
                """, new { item.ProductId, item.Quantity }, transaction);
        }

        // 2. Insertar cabecera de venta
        await connection.ExecuteAsync("""
            INSERT INTO Ventas (Id, Fecha, Usuario, Cliente, MetodoPago, Total, Factura, Estado, InvoiceNumber, ClientId)
            VALUES (@Id, @Fecha, @Usuario, @Cliente, @MetodoPago, @Total, @Factura, @Estado, @InvoiceNumber, @ClientId);
            """, new
        {
            Id = ventaId,
            Fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Usuario = usuario,
            Cliente = cliente,
            MetodoPago = metodoPago,
            Total = total,
            Factura = factura ? 1 : 0,
            Estado = estado,
            InvoiceNumber = invoiceNumber,
            ClientId = clientId
        }, transaction);

        // 3. Insertar detalle de cada ítem
        foreach (var item in items)
        {
            await connection.ExecuteAsync("""
                INSERT INTO VentasDetalle (VentaId, ProductoNombre, Cantidad, PrecioUnitario, Subtotal)
                VALUES (@VentaId, @ProductoNombre, @Cantidad, @PrecioUnitario, @Subtotal);
                """, new
            {
                VentaId = ventaId,
                ProductoNombre = item.Name,
                Cantidad = item.Quantity,
                PrecioUnitario = item.UnitPrice,
                Subtotal = item.LineTotal
            }, transaction);
        }

        // 4. Actualizar deuda del cliente si es Cuenta Corriente
        if (metodoPago == "Cuenta Corriente" && clientId.HasValue)
        {
            await connection.ExecuteAsync(
                "UPDATE Clients SET DeudaTotal = DeudaTotal + @Total WHERE Id = @Id;",
                new { Total = total, Id = clientId.Value }, transaction);
        }

        transaction.Commit();

        // Update client purchase stats outside the sale transaction
        if (clientId is not null)
        {
            await connection.ExecuteAsync("""
                UPDATE Clients
                SET TotalCompras = TotalCompras + 1
                WHERE Id = @Id;
                """, new { Id = clientId.Value });
        }

        return ventaId;
    }

    /// <summary>
    /// Cancela una venta (cambia Estado a "Cancelada") y repone el stock de sus ítems.
    /// </summary>
    public async Task CancelarVentaAsync(string ventaId)
    {
        using var connection = database.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var detalles = await connection.QueryAsync<VentaDetalle>(
            "SELECT * FROM VentasDetalle WHERE VentaId = @VentaId;",
            new { VentaId = ventaId }, transaction);

        foreach (var d in detalles)
        {
            // Reponer stock buscando por nombre (mejor unir por ProductId en una extensión futura)
            await connection.ExecuteAsync("""
                UPDATE Products SET Stock = Stock + @Cantidad WHERE Name = @Nombre;
                """, new { d.Cantidad, Nombre = d.ProductoNombre }, transaction);
        }

        await connection.ExecuteAsync(
            "UPDATE Ventas SET Estado = 'Cancelada' WHERE Id = @Id;",
            new { Id = ventaId }, transaction);

        transaction.Commit();
    }

    /// <summary>
    /// Elimina una venta físicamente de la base de datos y repone el stock.
    /// </summary>
    public async Task EliminarVentaAsync(string ventaId)
    {
        using var connection = database.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync("DELETE FROM VentasDetalle WHERE VentaId = @Id;", new { Id = ventaId }, transaction);
        await connection.ExecuteAsync("DELETE FROM Ventas WHERE Id = @Id;", new { Id = ventaId }, transaction);

        transaction.Commit();
    }

    public async Task AceptarTransferenciaAsync(string ventaId)
    {
        using var connection = database.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE Ventas SET Estado = 'Completada' WHERE Id = @Id;",
            new { Id = ventaId });
    }

    public async Task ConfirmarPagoCuentaCorrienteAsync(string ventaId, int clientId, decimal total)
    {
        using var connection = database.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync(
            "UPDATE Ventas SET Estado = 'Completada' WHERE Id = @Id;",
            new { Id = ventaId }, transaction);

        await connection.ExecuteAsync(
            "UPDATE Clients SET DeudaTotal = DeudaTotal - @Total WHERE Id = @Id;",
            new { Total = total, Id = clientId }, transaction);

        transaction.Commit();
    }

    public async Task<IReadOnlyList<VentaDetalle>> GetVentaDetallesAsync(string ventaId)
    {
        using var connection = database.CreateConnection();
        var rows = await connection.QueryAsync<VentaDetalle>(
            "SELECT * FROM VentasDetalle WHERE VentaId = @VentaId;",
            new { VentaId = ventaId });
        return rows.ToList();
    }

    /// <summary>
    /// Retorna la lista de ventas con un resumen de productos por fila.
    /// </summary>
    public async Task<IReadOnlyList<Venta>> GetPendingDebtsByClientIdAsync(int clientId)
    {
        using var connection = database.CreateConnection();
        var debts = await connection.QueryAsync<Venta>("""
            SELECT
                Id, ClientId, InvoiceNumber, Total, Fecha, MetodoPago, Estado
            FROM Ventas
            WHERE ClientId = @ClientId AND Estado = 'En Deuda'
            ORDER BY Fecha DESC;
            """, new { ClientId = clientId });
        return debts.ToList();
    }

    public async Task<bool> HasPendingSalesAsync(int clientId)
    {
        using var connection = database.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Ventas WHERE ClientId = @ClientId AND Estado = 'Pendiente';",
            new { ClientId = clientId });
        return count > 0;
    }

    public async Task<IReadOnlyList<Venta>> GetVentasAsync(string? searchText = null)
    {
        using var connection = database.CreateConnection();

        var ventas = await connection.QueryAsync<Venta>("""
            SELECT
                v.Id,
                v.Fecha,
                v.Usuario,
                v.Cliente,
                v.MetodoPago,
                v.Total,
                v.Factura,
                v.Estado,
                v.InvoiceNumber,
                v.ClientId,
                IFNULL(
                    (SELECT GROUP_CONCAT(d.ProductoNombre || ' x' || d.Cantidad, ', ')
                     FROM VentasDetalle d WHERE d.VentaId = v.Id),
                    ''
                ) AS ProductosResumen
            FROM Ventas v
            WHERE @Search = ''
               OR v.Id LIKE @Term
               OR v.InvoiceNumber LIKE @Term
               OR v.MetodoPago LIKE @Term
               OR v.Estado LIKE @Term
            ORDER BY v.Fecha DESC;
            """, new
        {
            Search = searchText?.Trim() ?? string.Empty,
            Term = $"%{searchText?.Trim() ?? string.Empty}%"
        });

        return ventas.ToList();
    }

    /// <summary>
    /// Calcula los 4 KPIs para la vista Métricas usando Dapper directamente.
    /// </summary>
    public async Task<MetricasKpis> GetMetricasKpisAsync()
    {
        using var connection = database.CreateConnection();

        var totalVentas = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Ventas WHERE Estado = 'Completada';");

        var ingresosTotales = await connection.ExecuteScalarAsync<decimal>(
            "SELECT IFNULL(SUM(Total), 0) FROM Ventas WHERE Estado = 'Completada';");

        var metodoPago = await connection.ExecuteScalarAsync<string?>("""
            SELECT MetodoPago
            FROM Ventas
            WHERE Estado = 'Completada'
            GROUP BY MetodoPago
            ORDER BY COUNT(*) DESC
            LIMIT 1;
            """) ?? "-";

        var productoPopular = await connection.ExecuteScalarAsync<string?>("""
            SELECT d.ProductoNombre
            FROM VentasDetalle d
            INNER JOIN Ventas v ON v.Id = d.VentaId
            WHERE v.Estado = 'Completada'
            GROUP BY d.ProductoNombre
            ORDER BY SUM(d.Cantidad) DESC
            LIMIT 1;
            """) ?? "-";

        return new MetricasKpis
        {
            TotalVentas = totalVentas,
            IngresosTotales = ingresosTotales,
            MetodoPagoMasUsado = metodoPago,
            ProductoMasPopular = productoPopular
        };
    }

    // ── Caja Diaria ──────────────────────────────────────────

    public async Task<decimal?> GetCajaInicialAsync(string fecha)
    {
        using var connection = database.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<decimal?>(
            "SELECT MontoInicial FROM CajaDiaria WHERE Fecha = @Fecha;",
            new { Fecha = fecha });
    }

    public async Task SaveCajaInicialAsync(string fecha, decimal monto)
    {
        using var connection = database.CreateConnection();
        await connection.ExecuteAsync("""
            INSERT INTO CajaDiaria (Fecha, MontoInicial) VALUES (@Fecha, @Monto)
            ON CONFLICT(Fecha) DO UPDATE SET MontoInicial = @Monto;
            """, new { Fecha = fecha, Monto = monto });
    }

    public async Task<decimal> GetTotalVentasEfectivoHoyAsync(string fecha)
    {
        using var connection = database.CreateConnection();
        return await connection.ExecuteScalarAsync<decimal>("""
            SELECT COALESCE(SUM(Total), 0)
            FROM Ventas
            WHERE date(Fecha) = date(@Fecha)
              AND MetodoPago = 'Efectivo'
              AND Estado = 'Completada';
            """, new { Fecha = fecha });
    }
}

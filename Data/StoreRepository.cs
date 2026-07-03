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
                c.Name AS CategoryName
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
                c.Name AS CategoryName
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
                INSERT INTO Products (Barcode, InternalCode, Name, SalePrice, Stock, CategoryId)
                VALUES (@Barcode, @InternalCode, @Name, @SalePrice, @Stock, @CategoryId);
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
                CategoryId = @CategoryId
            WHERE Id = @Id;
            """, product);
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

    public async Task ConfirmSaleAsync(IEnumerable<CartItem> cartItems)
    {
        using var connection = database.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        foreach (var item in cartItems)
        {
            var updated = await connection.ExecuteAsync("""
                UPDATE Products
                SET Stock = Stock - @Quantity
                WHERE Id = @ProductId
                  AND Stock >= @Quantity;
                """, new { item.ProductId, item.Quantity }, transaction);

            if (updated == 0)
            {
                transaction.Rollback();
                throw new InvalidOperationException($"Stock insuficiente para {item.Name}.");
            }
        }

        transaction.Commit();
    }
}

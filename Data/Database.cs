using System.Data;
using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;

namespace MiniPosWpf.Data;

public sealed class Database
{
    private readonly string connectionString;

    public Database()
    {
        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MiniPosWpf");
        Directory.CreateDirectory(dataDirectory);

        var databasePath = Path.Combine(dataDirectory, "minipos.db");
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            ForeignKeys = true
        }.ToString();
    }

    public IDbConnection CreateConnection() => new SqliteConnection(connectionString);

    public async Task InitializeAsync()
    {
        using var connection = CreateConnection();
        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS Categories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE COLLATE NOCASE
            );

            CREATE TABLE IF NOT EXISTS Products (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Barcode TEXT NOT NULL DEFAULT '' COLLATE NOCASE,
                InternalCode TEXT NOT NULL DEFAULT '' COLLATE NOCASE,
                Name TEXT NOT NULL,
                SalePrice NUMERIC NOT NULL CHECK (SalePrice >= 0),
                CostPrice NUMERIC NOT NULL DEFAULT 0,
                Stock INTEGER NOT NULL,
                CategoryId INTEGER NOT NULL,
                FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
            );

            CREATE TABLE IF NOT EXISTS Ventas (
                Id      TEXT PRIMARY KEY,
                Fecha   TEXT NOT NULL,
                Usuario TEXT NOT NULL DEFAULT 'Isabel',
                Cliente TEXT NOT NULL DEFAULT 'Consumidor Final',
                MetodoPago TEXT NOT NULL DEFAULT 'Efectivo',
                Total   NUMERIC NOT NULL CHECK (Total >= 0),
                Factura INTEGER NOT NULL DEFAULT 0,
                Estado  TEXT NOT NULL DEFAULT 'Completada',
                InvoiceNumber TEXT NOT NULL DEFAULT ''
            );

            CREATE TABLE IF NOT EXISTS VentasDetalle (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                VentaId         TEXT NOT NULL,
                ProductoNombre  TEXT NOT NULL,
                Cantidad        INTEGER NOT NULL CHECK (Cantidad > 0),
                PrecioUnitario  NUMERIC NOT NULL CHECK (PrecioUnitario >= 0),
                CostoUnitario   NUMERIC NOT NULL DEFAULT 0,
                Subtotal        NUMERIC NOT NULL CHECK (Subtotal >= 0),
                FOREIGN KEY (VentaId) REFERENCES Ventas(Id)
            );

            CREATE TABLE IF NOT EXISTS Clients (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Telefono TEXT NOT NULL DEFAULT '',
                FechaRegistro TEXT NOT NULL DEFAULT (datetime('now','localtime')),
                TotalCompras INTEGER NOT NULL DEFAULT 0,
                DeudaTotal NUMERIC NOT NULL DEFAULT 0
            );

            INSERT OR IGNORE INTO Categories (Name) VALUES
                ('alfajores'),
                ('botellas');

            CREATE TABLE IF NOT EXISTS CajaDiaria (
                Fecha TEXT PRIMARY KEY,
                MontoInicial NUMERIC NOT NULL DEFAULT 0
            );
            """);

        await MigrateRemoveUniqueConstraintsAsync(connection);
        await MigrateAddClientIdToVentasAsync(connection);
        await MigrateAddInvoiceNumberToVentasAsync(connection);
        await MigrateAddUnitTypeToProductsAsync(connection);
        await MigrateRemoveStockCheckAsync(connection);
        await MigrateCreateCajaDiariaAsync(connection);
        await MigrateAddCostPriceToProductsAsync(connection);
        await MigrateAddCostoUnitarioToVentasDetalleAsync(connection);
    }

    private static async Task MigrateRemoveUniqueConstraintsAsync(IDbConnection connection)
    {
        var tableInfo = await connection.QueryAsync<dynamic>(
            "SELECT sql FROM sqlite_master WHERE type='table' AND name='Products';");

        var sql = tableInfo.FirstOrDefault()?.sql as string;
        if (sql is null || !sql.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS Products_new (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Barcode TEXT NOT NULL DEFAULT '' COLLATE NOCASE,
                InternalCode TEXT NOT NULL DEFAULT '' COLLATE NOCASE,
                Name TEXT NOT NULL,
                SalePrice NUMERIC NOT NULL CHECK (SalePrice >= 0),
                Stock INTEGER NOT NULL,
                CategoryId INTEGER NOT NULL,
                FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
            );

            INSERT INTO Products_new (Id, Barcode, InternalCode, Name, SalePrice, Stock, CategoryId)
            SELECT Id, Barcode, InternalCode, Name, SalePrice, Stock, CategoryId FROM Products;

            DROP TABLE Products;

            ALTER TABLE Products_new RENAME TO Products;
            """);
    }

    private static async Task MigrateAddClientIdToVentasAsync(IDbConnection connection)
    {
        var columns = await connection.QueryAsync<dynamic>(
            "PRAGMA table_info(Ventas);");

        var hasClientId = columns.Any(c => ((string)c.name) == "ClientId");
        if (hasClientId)
        {
            return;
        }

        await connection.ExecuteAsync(
            "ALTER TABLE Ventas ADD COLUMN ClientId INTEGER REFERENCES Clients(Id);");
    }

    private static async Task MigrateAddInvoiceNumberToVentasAsync(IDbConnection connection)
    {
        var columns = await connection.QueryAsync<dynamic>(
            "PRAGMA table_info(Ventas);");

        var hasInvoiceNumber = columns.Any(c => ((string)c.name) == "InvoiceNumber");
        if (!hasInvoiceNumber)
        {
            await connection.ExecuteAsync(
                "ALTER TABLE Ventas ADD COLUMN InvoiceNumber TEXT NOT NULL DEFAULT '';");
        }
    }

    private static async Task MigrateAddUnitTypeToProductsAsync(IDbConnection connection)
    {
        var columns = await connection.QueryAsync<dynamic>(
            "PRAGMA table_info(Products);");

        var hasUnitType = columns.Any(c => ((string)c.name) == "UnitType");
        if (!hasUnitType)
        {
            await connection.ExecuteAsync(
                "ALTER TABLE Products ADD COLUMN UnitType TEXT NOT NULL DEFAULT 'Unidad';");
        }
    }

    private static async Task MigrateRemoveStockCheckAsync(IDbConnection connection)
    {
        var tableInfo = await connection.QueryAsync<dynamic>(
            "SELECT sql FROM sqlite_master WHERE type='table' AND name='Products';");

        var sql = tableInfo.FirstOrDefault()?.sql as string;
        if (sql is null || !sql.Contains("Stock >= 0", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS Products_new_2 (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Barcode TEXT NOT NULL DEFAULT '' COLLATE NOCASE,
                InternalCode TEXT NOT NULL DEFAULT '' COLLATE NOCASE,
                Name TEXT NOT NULL,
                SalePrice NUMERIC NOT NULL CHECK (SalePrice >= 0),
                Stock INTEGER NOT NULL,
                CategoryId INTEGER NOT NULL,
                UnitType TEXT NOT NULL DEFAULT 'Unidad',
                FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
            );

            INSERT INTO Products_new_2 (Id, Barcode, InternalCode, Name, SalePrice, Stock, CategoryId, UnitType)
            SELECT Id, Barcode, InternalCode, Name, SalePrice, Stock, CategoryId, UnitType FROM Products;

            DROP TABLE Products;

            ALTER TABLE Products_new_2 RENAME TO Products;
            """);
    }

    private static async Task MigrateCreateCajaDiariaAsync(IDbConnection connection)
    {
        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS CajaDiaria (
                Fecha TEXT PRIMARY KEY,
                MontoInicial NUMERIC NOT NULL DEFAULT 0
            );
            """);
    }

    private static async Task MigrateAddCostPriceToProductsAsync(IDbConnection connection)
    {
        var columns = await connection.QueryAsync<dynamic>(
            "PRAGMA table_info(Products);");

        var hasCostPrice = columns.Any(c => ((string)c.name) == "CostPrice");
        if (!hasCostPrice)
        {
            await connection.ExecuteAsync(
                "ALTER TABLE Products ADD COLUMN CostPrice NUMERIC NOT NULL DEFAULT 0;");
        }
    }

    private static async Task MigrateAddCostoUnitarioToVentasDetalleAsync(IDbConnection connection)
    {
        var columns = await connection.QueryAsync<dynamic>(
            "PRAGMA table_info(VentasDetalle);");

        var hasCostoUnitario = columns.Any(c => ((string)c.name) == "CostoUnitario");
        if (!hasCostoUnitario)
        {
            await connection.ExecuteAsync(
                "ALTER TABLE VentasDetalle ADD COLUMN CostoUnitario NUMERIC NOT NULL DEFAULT 0;");
        }
    }
}

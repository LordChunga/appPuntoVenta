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
                Stock INTEGER NOT NULL CHECK (Stock >= 0),
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
                Estado  TEXT NOT NULL DEFAULT 'Completada'
            );

            CREATE TABLE IF NOT EXISTS VentasDetalle (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                VentaId         TEXT NOT NULL,
                ProductoNombre  TEXT NOT NULL,
                Cantidad        INTEGER NOT NULL CHECK (Cantidad > 0),
                PrecioUnitario  NUMERIC NOT NULL CHECK (PrecioUnitario >= 0),
                Subtotal        NUMERIC NOT NULL CHECK (Subtotal >= 0),
                FOREIGN KEY (VentaId) REFERENCES Ventas(Id)
            );

            INSERT OR IGNORE INTO Categories (Name) VALUES
                ('alfajores'),
                ('botellas');
            """);

        await MigrateRemoveUniqueConstraintsAsync(connection);
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
                Stock INTEGER NOT NULL CHECK (Stock >= 0),
                CategoryId INTEGER NOT NULL,
                FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
            );

            INSERT INTO Products_new (Id, Barcode, InternalCode, Name, SalePrice, Stock, CategoryId)
            SELECT Id, Barcode, InternalCode, Name, SalePrice, Stock, CategoryId FROM Products;

            DROP TABLE Products;

            ALTER TABLE Products_new RENAME TO Products;
            """);
    }
}

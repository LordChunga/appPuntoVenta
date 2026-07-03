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
                Barcode TEXT NOT NULL UNIQUE COLLATE NOCASE,
                InternalCode TEXT NOT NULL UNIQUE COLLATE NOCASE,
                Name TEXT NOT NULL,
                SalePrice NUMERIC NOT NULL CHECK (SalePrice >= 0),
                Stock INTEGER NOT NULL CHECK (Stock >= 0),
                CategoryId INTEGER NOT NULL,
                FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
            );

            INSERT OR IGNORE INTO Categories (Name) VALUES
                ('alfajores'),
                ('botellas');
            """);
    }
}

using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MiniPosWpf.Models;

namespace MiniPosWpf.Data;

public static class XlsxProductImporter
{
    private static readonly XNamespace SpreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    public static IReadOnlyList<ProductImportRow> ReadProducts(string path, out int invalidRows)
    {
        using var archive = ZipFile.OpenRead(path);
        var sharedStrings = ReadSharedStrings(archive);
        var worksheet = archive.GetEntry("xl/worksheets/sheet1.xml")
            ?? throw new InvalidOperationException("No se encontro la primera hoja del archivo Excel.");

        using var stream = worksheet.Open();
        var document = XDocument.Load(stream);
        var rows = document.Descendants(SpreadsheetNamespace + "row").ToList();

        invalidRows = 0;
        if (rows.Count == 0)
        {
            return [];
        }

        var headers = ReadRow(rows[0], sharedStrings)
            .ToDictionary(cell => cell.Key, cell => NormalizeHeader(cell.Value));

        var idColumn = FindColumn(headers, "id");
        var barcodeColumn = FindColumn(headers, "codigobarra", "codigodebarra", "codigobarras", "barcode", "barras");
        var nameColumn = FindColumn(headers, "producto", "nombre", "nombreproducto");
        var priceColumn = FindColumn(headers, "precio", "precioventa");
        var categoryColumn = FindColumn(headers, "categoria");

        if (idColumn is null || barcodeColumn is null || nameColumn is null || priceColumn is null || categoryColumn is null)
        {
            throw new InvalidOperationException("El Excel debe tener columnas: id, codigo de barra, producto, precio y categoria.");
        }

        var products = new List<ProductImportRow>();
        foreach (var row in rows.Skip(1))
        {
            var cells = ReadRow(row, sharedStrings);
            var idText = GetValue(cells, idColumn);
            var barcode = GetValue(cells, barcodeColumn);
            var name = GetValue(cells, nameColumn);
            var priceText = GetValue(cells, priceColumn);
            var category = GetValue(cells, categoryColumn);

            if (!TryParseId(idText, out var id)
                || string.IsNullOrWhiteSpace(name)
                || !TryParsePrice(priceText, out var price)
                || string.IsNullOrWhiteSpace(category))
            {
                invalidRows++;
                continue;
            }

            products.Add(new ProductImportRow(
                id,
                barcode.Trim(),
                name.Trim(),
                price,
                category.Trim()));
        }

        return products;
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return [];
        }

        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        return document
            .Descendants(SpreadsheetNamespace + "si")
            .Select(item => string.Concat(item.Descendants(SpreadsheetNamespace + "t").Select(text => text.Value)))
            .ToList();
    }

    private static Dictionary<string, string> ReadRow(XElement row, IReadOnlyList<string> sharedStrings)
    {
        var values = new Dictionary<string, string>();

        foreach (var cell in row.Elements(SpreadsheetNamespace + "c"))
        {
            var reference = cell.Attribute("r")?.Value;
            if (string.IsNullOrWhiteSpace(reference))
            {
                continue;
            }

            var column = Regex.Match(reference, "^[A-Z]+", RegexOptions.IgnoreCase).Value.ToUpperInvariant();
            values[column] = ReadCellValue(cell, sharedStrings);
        }

        return values;
    }

    private static string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var type = cell.Attribute("t")?.Value;
        if (type == "s")
        {
            var rawIndex = cell.Element(SpreadsheetNamespace + "v")?.Value;
            return int.TryParse(rawIndex, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
                && index >= 0
                && index < sharedStrings.Count
                    ? sharedStrings[index]
                    : string.Empty;
        }

        if (type == "inlineStr")
        {
            return string.Concat(cell.Descendants(SpreadsheetNamespace + "t").Select(text => text.Value));
        }

        return cell.Element(SpreadsheetNamespace + "v")?.Value ?? string.Empty;
    }

    private static string? FindColumn(Dictionary<string, string> headers, params string[] names)
    {
        return headers.FirstOrDefault(header => names.Contains(header.Value)).Key;
    }

    private static string GetValue(Dictionary<string, string> cells, string column)
    {
        return cells.TryGetValue(column, out var value) ? value : string.Empty;
    }

    private static bool TryParseId(string value, out int id)
    {
        if (int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
        {
            return id > 0;
        }

        if (decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalId))
        {
            id = (int)decimalId;
            return id > 0 && decimalId == id;
        }

        return false;
    }

    private static bool TryParsePrice(string value, out decimal price)
    {
        var styles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign;
        return decimal.TryParse(value.Trim(), styles, CultureInfo.InvariantCulture, out price)
            || decimal.TryParse(value.Trim(), styles, new CultureInfo("es-AR"), out price);
    }

    private static string NormalizeHeader(string value)
    {
        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark
                && char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }
}

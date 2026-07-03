namespace MiniPosWpf.Models;

public sealed record ProductImportResult(
    int ImportedProducts,
    int SkippedProducts,
    int CreatedCategories,
    int InvalidRows);

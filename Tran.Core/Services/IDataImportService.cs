namespace Tran.Core.Services;

public enum ImportType
{
    Company,
    Product,
    Sale,
    Purchase
}

public enum ImportRowStatus
{
    Valid,
    Invalid,
    Skipped
}

public abstract class ImportRowBase
{
    public int RowNumber { get; set; }
    public ImportRowStatus Status { get; set; } = ImportRowStatus.Valid;
    public string? ErrorMessage { get; set; }
}

public class CompanyImportRow : ImportRowBase
{
    public string CompanyId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string BusinessNumber { get; set; } = string.Empty;
    public string? Representative { get; set; }
    public string? Contact { get; set; }
    public string? Address { get; set; }
}

public class ProductImportRow : ImportRowBase
{
    public string? ProductCode { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Unit { get; set; } = "개";
    public decimal DefaultPrice { get; set; }
}

public class SaleImportRow : ImportRowBase
{
    public DateTime SaleDate { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Note { get; set; }

    // Resolved after validation
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
}

public class PurchaseImportRow : ImportRowBase
{
    public DateTime PurchaseDate { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Note { get; set; }

    // Resolved after validation
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
}

public class ImportResult
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

public interface IDataImportService
{
    Task<List<CompanyImportRow>> ParseCompaniesAsync(string filePath);
    Task<List<ProductImportRow>> ParseProductsAsync(string filePath);
    Task<List<SaleImportRow>> ParseSalesAsync(string filePath);
    Task<List<PurchaseImportRow>> ParsePurchasesAsync(string filePath);

    Task<ImportResult> ImportCompaniesAsync(List<CompanyImportRow> rows);
    Task<ImportResult> ImportProductsAsync(List<ProductImportRow> rows);
    Task<ImportResult> ImportSalesAsync(List<SaleImportRow> rows);
    Task<ImportResult> ImportPurchasesAsync(List<PurchaseImportRow> rows);
}

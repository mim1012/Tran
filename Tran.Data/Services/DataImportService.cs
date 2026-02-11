using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;
using Tran.Core.Services;

namespace Tran.Data.Services;

public class DataImportService : IDataImportService
{
    private readonly TranDbContext _context;

    public DataImportService(TranDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // ═══════════════════════════════════════════════════════════
    // Parse Methods
    // ═══════════════════════════════════════════════════════════

    public async Task<List<CompanyImportRow>> ParseCompaniesAsync(string filePath)
    {
        var rows = new List<CompanyImportRow>();

        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook(filePath);
            var ws = workbook.Worksheet(1);
            var dataRows = ws.RowsUsed().Skip(1); // skip header

            foreach (var row in dataRows)
            {
                var importRow = new CompanyImportRow
                {
                    RowNumber = row.RowNumber(),
                    CompanyId = row.Cell(1).GetString().Trim(),
                    CompanyName = row.Cell(2).GetString().Trim(),
                    BusinessNumber = row.Cell(3).GetString().Trim(),
                    Representative = GetOptionalString(row, 4),
                    Contact = GetOptionalString(row, 5),
                    Address = GetOptionalString(row, 6)
                };
                rows.Add(importRow);
            }
        });

        await ValidateCompaniesAsync(rows);
        return rows;
    }

    public async Task<List<ProductImportRow>> ParseProductsAsync(string filePath)
    {
        var rows = new List<ProductImportRow>();

        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook(filePath);
            var ws = workbook.Worksheet(1);
            var dataRows = ws.RowsUsed().Skip(1);

            foreach (var row in dataRows)
            {
                var importRow = new ProductImportRow
                {
                    RowNumber = row.RowNumber(),
                    ProductCode = GetOptionalString(row, 1),
                    ProductName = row.Cell(2).GetString().Trim(),
                    Category = GetOptionalString(row, 3),
                    Unit = row.Cell(4).GetString().Trim(),
                    DefaultPrice = ParseDecimal(row.Cell(5).GetString())
                };

                if (string.IsNullOrWhiteSpace(importRow.Unit))
                    importRow.Unit = "개";

                rows.Add(importRow);
            }
        });

        await ValidateProductsAsync(rows);
        return rows;
    }

    public async Task<List<SaleImportRow>> ParseSalesAsync(string filePath)
    {
        var rows = new List<SaleImportRow>();

        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook(filePath);
            var ws = workbook.Worksheet(1);
            var dataRows = ws.RowsUsed().Skip(1);

            foreach (var row in dataRows)
            {
                var importRow = new SaleImportRow
                {
                    RowNumber = row.RowNumber(),
                    CompanyId = row.Cell(2).GetString().Trim(),
                    ProductCode = row.Cell(3).GetString().Trim(),
                    Quantity = ParseDecimal(row.Cell(4).GetString()),
                    UnitPrice = ParseDecimal(row.Cell(5).GetString()),
                    Note = GetOptionalString(row, 6)
                };

                if (TryParseDate(row.Cell(1).GetString(), out var date))
                    importRow.SaleDate = date;

                rows.Add(importRow);
            }
        });

        await ValidateSalesAsync(rows);
        return rows;
    }

    public async Task<List<PurchaseImportRow>> ParsePurchasesAsync(string filePath)
    {
        var rows = new List<PurchaseImportRow>();

        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook(filePath);
            var ws = workbook.Worksheet(1);
            var dataRows = ws.RowsUsed().Skip(1);

            foreach (var row in dataRows)
            {
                var importRow = new PurchaseImportRow
                {
                    RowNumber = row.RowNumber(),
                    CompanyId = row.Cell(2).GetString().Trim(),
                    ProductCode = row.Cell(3).GetString().Trim(),
                    Quantity = ParseDecimal(row.Cell(4).GetString()),
                    UnitPrice = ParseDecimal(row.Cell(5).GetString()),
                    Note = GetOptionalString(row, 6)
                };

                if (TryParseDate(row.Cell(1).GetString(), out var date))
                    importRow.PurchaseDate = date;

                rows.Add(importRow);
            }
        });

        await ValidatePurchasesAsync(rows);
        return rows;
    }

    // ═══════════════════════════════════════════════════════════
    // Import Methods
    // ═══════════════════════════════════════════════════════════

    public async Task<ImportResult> ImportCompaniesAsync(List<CompanyImportRow> rows)
    {
        var result = new ImportResult();

        foreach (var row in rows)
        {
            if (row.Status == ImportRowStatus.Invalid)
            {
                result.FailedCount++;
                result.Errors.Add($"행 {row.RowNumber}: {row.ErrorMessage}");
                continue;
            }
            if (row.Status == ImportRowStatus.Skipped)
            {
                result.SkippedCount++;
                continue;
            }

            var company = new Company
            {
                CompanyId = row.CompanyId,
                CompanyName = row.CompanyName,
                BusinessNumber = row.BusinessNumber,
                Representative = row.Representative,
                Contact = row.Contact,
                Address = row.Address,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Companies.Add(company);
            result.SuccessCount++;
        }

        if (result.SuccessCount > 0)
            await _context.SaveChangesAsync();

        return result;
    }

    public async Task<ImportResult> ImportProductsAsync(List<ProductImportRow> rows)
    {
        var result = new ImportResult();

        foreach (var row in rows)
        {
            if (row.Status == ImportRowStatus.Invalid)
            {
                result.FailedCount++;
                result.Errors.Add($"행 {row.RowNumber}: {row.ErrorMessage}");
                continue;
            }
            if (row.Status == ImportRowStatus.Skipped)
            {
                result.SkippedCount++;
                continue;
            }

            var product = new Product
            {
                ProductName = row.ProductName,
                ProductCode = row.ProductCode,
                Category = row.Category,
                Unit = row.Unit,
                DefaultPrice = row.DefaultPrice,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Products.Add(product);
            result.SuccessCount++;
        }

        if (result.SuccessCount > 0)
            await _context.SaveChangesAsync();

        return result;
    }

    public async Task<ImportResult> ImportSalesAsync(List<SaleImportRow> rows)
    {
        var result = new ImportResult();
        var validRows = rows.Where(r => r.Status == ImportRowStatus.Valid).ToList();

        result.FailedCount = rows.Count(r => r.Status == ImportRowStatus.Invalid);
        result.SkippedCount = rows.Count(r => r.Status == ImportRowStatus.Skipped);

        foreach (var row in rows.Where(r => r.Status == ImportRowStatus.Invalid))
            result.Errors.Add($"행 {row.RowNumber}: {row.ErrorMessage}");

        // Group by (SaleDate + CompanyId) → one Sale per group
        var groups = validRows
            .GroupBy(r => new { r.SaleDate, r.CompanyId })
            .ToList();

        foreach (var group in groups)
        {
            var sale = new Sale
            {
                CompanyId = group.Key.CompanyId,
                SaleDate = group.Key.SaleDate,
                State = SaleState.Confirmed,
                ConfirmedAt = group.Key.SaleDate.AddDays(1),
                CreatedAt = DateTime.Now,
                Items = new List<SaleItem>()
            };

            foreach (var row in group)
            {
                var lineAmount = row.Quantity * row.UnitPrice;
                sale.Items.Add(new SaleItem
                {
                    ProductId = row.ProductId,
                    ProductName = row.ProductName,
                    Quantity = row.Quantity,
                    UnitPrice = row.UnitPrice,
                    LineAmount = lineAmount,
                    Note = row.Note
                });
            }

            sale.TotalAmount = sale.Items.Sum(i => i.LineAmount);
            _context.Sales.Add(sale);
            result.SuccessCount += group.Count();
        }

        if (result.SuccessCount > 0)
            await _context.SaveChangesAsync();

        return result;
    }

    public async Task<ImportResult> ImportPurchasesAsync(List<PurchaseImportRow> rows)
    {
        var result = new ImportResult();
        var validRows = rows.Where(r => r.Status == ImportRowStatus.Valid).ToList();

        result.FailedCount = rows.Count(r => r.Status == ImportRowStatus.Invalid);
        result.SkippedCount = rows.Count(r => r.Status == ImportRowStatus.Skipped);

        foreach (var row in rows.Where(r => r.Status == ImportRowStatus.Invalid))
            result.Errors.Add($"행 {row.RowNumber}: {row.ErrorMessage}");

        // Group by (PurchaseDate + CompanyId) → one Purchase per group
        var groups = validRows
            .GroupBy(r => new { r.PurchaseDate, r.CompanyId })
            .ToList();

        foreach (var group in groups)
        {
            var purchase = new Purchase
            {
                CompanyId = group.Key.CompanyId,
                PurchaseDate = group.Key.PurchaseDate,
                State = PurchaseState.Delivered,
                DeliveredAt = group.Key.PurchaseDate.AddDays(1),
                CreatedAt = DateTime.Now,
                Items = new List<PurchaseItem>()
            };

            foreach (var row in group)
            {
                var lineAmount = row.Quantity * row.UnitPrice;
                purchase.Items.Add(new PurchaseItem
                {
                    ProductId = row.ProductId,
                    ProductName = row.ProductName,
                    OrderedQuantity = row.Quantity,
                    ReceivedQuantity = row.Quantity,
                    DefectQuantity = 0,
                    UnitPrice = row.UnitPrice,
                    LineAmount = lineAmount,
                    Note = row.Note
                });
            }

            purchase.TotalAmount = purchase.Items.Sum(i => i.LineAmount);
            _context.Purchases.Add(purchase);
            result.SuccessCount += group.Count();
        }

        if (result.SuccessCount > 0)
            await _context.SaveChangesAsync();

        return result;
    }

    // ═══════════════════════════════════════════════════════════
    // Validation
    // ═══════════════════════════════════════════════════════════

    private async Task ValidateCompaniesAsync(List<CompanyImportRow> rows)
    {
        var existingCompanyIds = await _context.Companies
            .Select(c => c.CompanyId)
            .ToListAsync();
        var existingBizNumbers = await _context.Companies
            .Select(c => c.BusinessNumber)
            .ToListAsync();

        var existingIdSet = new HashSet<string>(existingCompanyIds, StringComparer.OrdinalIgnoreCase);
        var existingBizSet = new HashSet<string>(existingBizNumbers, StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.CompanyId))
            {
                row.Status = ImportRowStatus.Invalid;
                row.ErrorMessage = "거래처코드 필수";
                continue;
            }
            if (string.IsNullOrWhiteSpace(row.CompanyName))
            {
                row.Status = ImportRowStatus.Invalid;
                row.ErrorMessage = "거래처명 필수";
                continue;
            }
            if (string.IsNullOrWhiteSpace(row.BusinessNumber))
            {
                row.Status = ImportRowStatus.Invalid;
                row.ErrorMessage = "사업자번호 필수";
                continue;
            }

            if (existingIdSet.Contains(row.CompanyId))
            {
                row.Status = ImportRowStatus.Skipped;
                row.ErrorMessage = $"거래처코드 '{row.CompanyId}' 이미 존재";
                continue;
            }
            if (existingBizSet.Contains(row.BusinessNumber))
            {
                row.Status = ImportRowStatus.Skipped;
                row.ErrorMessage = $"사업자번호 '{row.BusinessNumber}' 이미 존재";
                continue;
            }

            // Track within batch to prevent duplicates
            existingIdSet.Add(row.CompanyId);
            existingBizSet.Add(row.BusinessNumber);
        }
    }

    private async Task ValidateProductsAsync(List<ProductImportRow> rows)
    {
        var existingCodes = await _context.Products
            .Where(p => p.ProductCode != null)
            .Select(p => p.ProductCode!)
            .ToListAsync();
        var existingCodeSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.ProductName))
            {
                row.Status = ImportRowStatus.Invalid;
                row.ErrorMessage = "품목명 필수";
                continue;
            }
            if (row.DefaultPrice < 0)
            {
                row.Status = ImportRowStatus.Invalid;
                row.ErrorMessage = "기본단가는 0 이상이어야 합니다";
                continue;
            }

            if (!string.IsNullOrWhiteSpace(row.ProductCode) && existingCodeSet.Contains(row.ProductCode))
            {
                row.Status = ImportRowStatus.Skipped;
                row.ErrorMessage = $"품목코드 '{row.ProductCode}' 이미 존재";
                continue;
            }

            if (!string.IsNullOrWhiteSpace(row.ProductCode))
                existingCodeSet.Add(row.ProductCode);
        }
    }

    private async Task ValidateSalesAsync(List<SaleImportRow> rows)
    {
        var companyIds = await _context.Companies
            .Select(c => c.CompanyId)
            .ToListAsync();
        var companyIdSet = new HashSet<string>(companyIds, StringComparer.OrdinalIgnoreCase);

        var products = await _context.Products
            .Where(p => p.ProductCode != null)
            .Select(p => new { p.ProductId, p.ProductCode, p.ProductName })
            .ToListAsync();
        var productMap = products.ToDictionary(p => p.ProductCode!, p => p, StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (row.SaleDate == default)
            {
                row.Status = ImportRowStatus.Invalid;
                row.ErrorMessage = "판매일자 필수";
                continue;
            }
            if (string.IsNullOrWhiteSpace(row.CompanyId))
            {
                row.Status = ImportRowStatus.Invalid;
                row.ErrorMessage = "거래처코드 필수";
                continue;
            }
            if (string.IsNullOrWhiteSpace(row.ProductCode))
            {
                row.Status = ImportRowStatus.Invalid;
                row.ErrorMessage = "품목코드 필수";
                continue;
            }
            if (row.Quantity <= 0)
            {
                row.Status = ImportRowStatus.Invalid;
                row.ErrorMessage = "수량은 0보다 커야 합니다";
                continue;
            }
            if (row.UnitPrice < 0)
            {
                row.Status = ImportRowStatus.Invalid;
                row.ErrorMessage = "단가는 0 이상이어야 합니다";
                continue;
            }

            if (!companyIdSet.Contains(row.CompanyId))
            {
                row.Status = ImportRowStatus.Invalid;
                row.ErrorMessage = $"거래처 '{row.CompanyId}' 없음";
                continue;
            }

            if (!productMap.TryGetValue(row.ProductCode, out var product))
            {
                row.Status = ImportRowStatus.Invalid;
                row.ErrorMessage = $"품목 '{row.ProductCode}' 없음";
                continue;
            }

            row.ProductId = product.ProductId;
            row.ProductName = product.ProductName;
        }
    }

    private async Task ValidatePurchasesAsync(List<PurchaseImportRow> rows)
    {
        var companyIds = await _context.Companies
            .Select(c => c.CompanyId)
            .ToListAsync();
        var companyIdSet = new HashSet<string>(companyIds, StringComparer.OrdinalIgnoreCase);

        var products = await _context.Products
            .Where(p => p.ProductCode != null)
            .Select(p => new { p.ProductId, p.ProductCode, p.ProductName })
            .ToListAsync();
        var productMap = products.ToDictionary(p => p.ProductCode!, p => p, StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (row.PurchaseDate == default)
            {
                row.Status = ImportRowStatus.Invalid;
                row.ErrorMessage = "매입일자 필수";
                continue;
            }
            if (string.IsNullOrWhiteSpace(row.CompanyId))
            {
                row.Status = ImportRowStatus.Invalid;
                row.ErrorMessage = "거래처코드 필수";
                continue;
            }
            if (string.IsNullOrWhiteSpace(row.ProductCode))
            {
                row.Status = ImportRowStatus.Invalid;
                row.ErrorMessage = "품목코드 필수";
                continue;
            }
            if (row.Quantity <= 0)
            {
                row.Status = ImportRowStatus.Invalid;
                row.ErrorMessage = "수량은 0보다 커야 합니다";
                continue;
            }
            if (row.UnitPrice < 0)
            {
                row.Status = ImportRowStatus.Invalid;
                row.ErrorMessage = "단가는 0 이상이어야 합니다";
                continue;
            }

            if (!companyIdSet.Contains(row.CompanyId))
            {
                row.Status = ImportRowStatus.Invalid;
                row.ErrorMessage = $"거래처 '{row.CompanyId}' 없음";
                continue;
            }

            if (!productMap.TryGetValue(row.ProductCode, out var product))
            {
                row.Status = ImportRowStatus.Invalid;
                row.ErrorMessage = $"품목 '{row.ProductCode}' 없음";
                continue;
            }

            row.ProductId = product.ProductId;
            row.ProductName = product.ProductName;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════

    private static string? GetOptionalString(IXLRow row, int column)
    {
        var value = row.Cell(column).GetString().Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static decimal ParseDecimal(string value)
    {
        var cleaned = value.Trim().Replace(",", "");
        return decimal.TryParse(cleaned, out var result) ? result : 0;
    }

    private static bool TryParseDate(string value, out DateTime date)
    {
        var cleaned = value.Trim();
        // Support common formats: yyyy-MM-dd, yyyy/MM/dd, yyyyMMdd
        if (DateTime.TryParse(cleaned, out date))
            return true;
        if (DateTime.TryParseExact(cleaned, "yyyyMMdd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out date))
            return true;
        return false;
    }
}

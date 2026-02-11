using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Win32;
using Tran.Core.Services;
using Tran.Data;
using Tran.Data.Services;

namespace Tran.Desktop.ViewModels;

public class DataImportViewModel : ViewModelBase
{
    private ImportType _selectedImportType = ImportType.Company;
    private string _selectedFilePath = string.Empty;
    private bool _showErrorsOnly;
    private string _statusMessage = string.Empty;
    private bool _isImporting;
    private int _totalRows;
    private int _validRows;
    private int _invalidRows;
    private int _skippedRows;

    // Store raw parsed rows for import
    private List<CompanyImportRow>? _parsedCompanies;
    private List<ProductImportRow>? _parsedProducts;
    private List<SaleImportRow>? _parsedSales;
    private List<PurchaseImportRow>? _parsedPurchases;

    public DataImportViewModel()
    {
        PreviewRows = new ObservableCollection<PreviewRowViewModel>();
        AllPreviewRows = new List<PreviewRowViewModel>();

        SelectFileCommand = new RelayCommand(ExecuteSelectFile);
        ImportCommand = new AsyncRelayCommand(ExecuteImportAsync, () => CanImport);
        ClearCommand = new RelayCommand(ExecuteClear);
    }

    // ═══════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════

    public ImportType SelectedImportType
    {
        get => _selectedImportType;
        set
        {
            if (SetProperty(ref _selectedImportType, value))
                ExecuteClear();
        }
    }

    public string SelectedFilePath
    {
        get => _selectedFilePath;
        set => SetProperty(ref _selectedFilePath, value);
    }

    public bool ShowErrorsOnly
    {
        get => _showErrorsOnly;
        set
        {
            if (SetProperty(ref _showErrorsOnly, value))
                ApplyFilter();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsImporting
    {
        get => _isImporting;
        set => SetProperty(ref _isImporting, value);
    }

    public bool CanImport => _validRows > 0 && !_isImporting && !string.IsNullOrEmpty(_selectedFilePath);

    public int TotalRows
    {
        get => _totalRows;
        set => SetProperty(ref _totalRows, value);
    }

    public int ValidRows
    {
        get => _validRows;
        set => SetProperty(ref _validRows, value);
    }

    public int InvalidRows
    {
        get => _invalidRows;
        set => SetProperty(ref _invalidRows, value);
    }

    public int SkippedRows
    {
        get => _skippedRows;
        set => SetProperty(ref _skippedRows, value);
    }

    public ObservableCollection<PreviewRowViewModel> PreviewRows { get; }
    private List<PreviewRowViewModel> AllPreviewRows { get; set; }

    public ICommand SelectFileCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand ClearCommand { get; }

    // ═══════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════

    private void ExecuteSelectFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Excel 파일 선택",
            Filter = "Excel 파일 (*.xlsx)|*.xlsx|모든 파일 (*.*)|*.*",
            DefaultExt = ".xlsx"
        };

        if (dialog.ShowDialog() == true)
        {
            SelectedFilePath = dialog.FileName;
            _ = ParseFileAsync();
        }
    }

    private async Task ParseFileAsync()
    {
        StatusMessage = "파일을 분석하는 중...";
        IsImporting = true;

        try
        {
            using var context = DbContextFactory.Create();
            var service = new DataImportService(context);

            AllPreviewRows.Clear();
            _parsedCompanies = null;
            _parsedProducts = null;
            _parsedSales = null;
            _parsedPurchases = null;

            switch (SelectedImportType)
            {
                case ImportType.Company:
                    _parsedCompanies = await service.ParseCompaniesAsync(SelectedFilePath);
                    foreach (var row in _parsedCompanies)
                        AllPreviewRows.Add(CreatePreview(row, FormatCompanyData(row)));
                    break;

                case ImportType.Product:
                    _parsedProducts = await service.ParseProductsAsync(SelectedFilePath);
                    foreach (var row in _parsedProducts)
                        AllPreviewRows.Add(CreatePreview(row, FormatProductData(row)));
                    break;

                case ImportType.Sale:
                    _parsedSales = await service.ParseSalesAsync(SelectedFilePath);
                    foreach (var row in _parsedSales)
                        AllPreviewRows.Add(CreatePreview(row, FormatSaleData(row)));
                    break;

                case ImportType.Purchase:
                    _parsedPurchases = await service.ParsePurchasesAsync(SelectedFilePath);
                    foreach (var row in _parsedPurchases)
                        AllPreviewRows.Add(CreatePreview(row, FormatPurchaseData(row)));
                    break;
            }

            UpdateStatistics();
            ApplyFilter();
            StatusMessage = $"분석 완료: 총 {TotalRows}건 (유효 {ValidRows}, 오류 {InvalidRows}, 건너뜀 {SkippedRows})";
        }
        catch (Exception ex)
        {
            StatusMessage = $"파일 분석 오류: {ex.Message}";
            AllPreviewRows.Clear();
            UpdateStatistics();
            ApplyFilter();
        }
        finally
        {
            IsImporting = false;
        }
    }

    private async Task ExecuteImportAsync()
    {
        IsImporting = true;
        StatusMessage = "가져오는 중...";

        try
        {
            using var context = DbContextFactory.Create();
            var service = new DataImportService(context);
            ImportResult result;

            switch (SelectedImportType)
            {
                case ImportType.Company:
                    result = await service.ImportCompaniesAsync(_parsedCompanies!);
                    break;
                case ImportType.Product:
                    result = await service.ImportProductsAsync(_parsedProducts!);
                    break;
                case ImportType.Sale:
                    result = await service.ImportSalesAsync(_parsedSales!);
                    break;
                case ImportType.Purchase:
                    result = await service.ImportPurchasesAsync(_parsedPurchases!);
                    break;
                default:
                    return;
            }

            StatusMessage = $"가져오기 완료: 성공 {result.SuccessCount}건, 실패 {result.FailedCount}건, 건너뜀 {result.SkippedCount}건";
        }
        catch (Exception ex)
        {
            StatusMessage = $"가져오기 오류: {ex.Message}";
        }
        finally
        {
            IsImporting = false;
        }
    }

    private void ExecuteClear()
    {
        SelectedFilePath = string.Empty;
        AllPreviewRows.Clear();
        PreviewRows.Clear();
        _parsedCompanies = null;
        _parsedProducts = null;
        _parsedSales = null;
        _parsedPurchases = null;
        TotalRows = 0;
        ValidRows = 0;
        InvalidRows = 0;
        SkippedRows = 0;
        StatusMessage = string.Empty;
    }

    // ═══════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════

    private void UpdateStatistics()
    {
        TotalRows = AllPreviewRows.Count;
        ValidRows = AllPreviewRows.Count(r => r.Status == ImportRowStatus.Valid);
        InvalidRows = AllPreviewRows.Count(r => r.Status == ImportRowStatus.Invalid);
        SkippedRows = AllPreviewRows.Count(r => r.Status == ImportRowStatus.Skipped);
        RaisePropertyChanged(nameof(CanImport));
    }

    private void ApplyFilter()
    {
        PreviewRows.Clear();
        var source = ShowErrorsOnly
            ? AllPreviewRows.Where(r => r.Status != ImportRowStatus.Valid)
            : AllPreviewRows;

        foreach (var row in source)
            PreviewRows.Add(row);
    }

    private static PreviewRowViewModel CreatePreview(ImportRowBase row, string dataSummary)
    {
        return new PreviewRowViewModel
        {
            RowNumber = row.RowNumber,
            Status = row.Status,
            ErrorMessage = row.ErrorMessage ?? string.Empty,
            DataSummary = dataSummary
        };
    }

    private static string FormatCompanyData(CompanyImportRow row)
        => $"{row.CompanyId} | {row.CompanyName} | {row.BusinessNumber}";

    private static string FormatProductData(ProductImportRow row)
        => $"{row.ProductCode} | {row.ProductName} | {row.DefaultPrice:N0}원";

    private static string FormatSaleData(SaleImportRow row)
        => $"{row.SaleDate:yyyy-MM-dd} | {row.CompanyId} | {row.ProductCode} | {row.Quantity} x {row.UnitPrice:N0}";

    private static string FormatPurchaseData(PurchaseImportRow row)
        => $"{row.PurchaseDate:yyyy-MM-dd} | {row.CompanyId} | {row.ProductCode} | {row.Quantity} x {row.UnitPrice:N0}";

    // ═══════════════════════════════════════════════════════════
    // Inner Classes
    // ═══════════════════════════════════════════════════════════

    public class PreviewRowViewModel
    {
        public int RowNumber { get; set; }
        public ImportRowStatus Status { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string DataSummary { get; set; } = string.Empty;

        public string StatusIcon => Status switch
        {
            ImportRowStatus.Valid => "\u2713",
            ImportRowStatus.Invalid => "\u2717",
            ImportRowStatus.Skipped => "\u2298",
            _ => ""
        };

        public string StatusColor => Status switch
        {
            ImportRowStatus.Valid => "#1E7F34",
            ImportRowStatus.Invalid => "#D32F2F",
            ImportRowStatus.Skipped => "#E67700",
            _ => "#555555"
        };
    }
}

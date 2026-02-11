using System.Collections.ObjectModel;
using System.Windows.Input;
using Tran.Core.Services;
using Tran.Data;
using Tran.Data.Services;

namespace Tran.Desktop.ViewModels;

/// <summary>
/// 매출 통계 ViewModel
/// 대시보드 요약 + 거래처별/상품별/카테고리별/월별 분석
/// </summary>
public class SalesStatisticsViewModel : ViewModelBase
{
    private decimal _todaySales;
    private int _todayCount;
    private decimal _monthSales;
    private int _monthCount;
    private decimal _prevMonthSales;
    private int _draftCount;
    private int _lowStockCount;
    private decimal _monthPurchases;
    private int _pendingOrderCount;
    private int _pendingDeliveryCount;
    private DateTime _fromDate;
    private DateTime _toDate;
    private string _statusMessage = string.Empty;
    private int _trendMonths = 12;
    private ProductDetailStatsDto? _selectedProduct;
    private CategorySalesDto? _selectedCategory;

    public SalesStatisticsViewModel()
    {
        CompanyRankList = new ObservableCollection<CompanySalesRankDto>();
        ProductSalesList = new ObservableCollection<ProductSalesDto>();
        MonthlyTrendList = new ObservableCollection<MonthlyTrendDto>();
        CategoryList = new ObservableCollection<CategorySalesDto>();
        ProductDetailList = new ObservableCollection<ProductDetailStatsDto>();
        SelectedProductMonthly = new ObservableCollection<ProductMonthlyBreakdownDto>();
        SelectedProductCustomers = new ObservableCollection<ProductCustomerDistributionDto>();
        FilteredCategoryProducts = new ObservableCollection<ProductDetailStatsDto>();

        // 기본 기간: 이번 달
        var today = DateTime.Today;
        _fromDate = new DateTime(today.Year, today.Month, 1);
        _toDate = today;

        RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
        SetTrend6MonthsCommand = new RelayCommand(async () => { TrendMonths = 6; await LoadMonthlyTrendAsync(); });
        SetTrend12MonthsCommand = new RelayCommand(async () => { TrendMonths = 12; await LoadMonthlyTrendAsync(); });
    }

    // ═══════════════════════════════════════════════════════════
    // Summary Properties
    // ═══════════════════════════════════════════════════════════

    public decimal TodaySales
    {
        get => _todaySales;
        set => SetProperty(ref _todaySales, value);
    }

    public int TodayCount
    {
        get => _todayCount;
        set => SetProperty(ref _todayCount, value);
    }

    public decimal MonthSales
    {
        get => _monthSales;
        set
        {
            if (SetProperty(ref _monthSales, value))
            {
                RaisePropertyChanged(nameof(MonthGrowthRate));
                RaisePropertyChanged(nameof(MonthProfit));
                RaisePropertyChanged(nameof(MonthProfitRate));
                RaisePropertyChanged(nameof(IsMonthProfitNegative));
            }
        }
    }

    public int MonthCount
    {
        get => _monthCount;
        set => SetProperty(ref _monthCount, value);
    }

    public decimal PrevMonthSales
    {
        get => _prevMonthSales;
        set
        {
            if (SetProperty(ref _prevMonthSales, value))
                RaisePropertyChanged(nameof(MonthGrowthRate));
        }
    }

    public int DraftCount
    {
        get => _draftCount;
        set => SetProperty(ref _draftCount, value);
    }

    public int LowStockCount
    {
        get => _lowStockCount;
        set => SetProperty(ref _lowStockCount, value);
    }

    public decimal MonthPurchases
    {
        get => _monthPurchases;
        set
        {
            if (SetProperty(ref _monthPurchases, value))
            {
                RaisePropertyChanged(nameof(MonthProfit));
                RaisePropertyChanged(nameof(MonthProfitRate));
                RaisePropertyChanged(nameof(IsMonthProfitNegative));
            }
        }
    }

    public int PendingOrderCount
    {
        get => _pendingOrderCount;
        set => SetProperty(ref _pendingOrderCount, value);
    }

    public int PendingDeliveryCount
    {
        get => _pendingDeliveryCount;
        set => SetProperty(ref _pendingDeliveryCount, value);
    }

    /// <summary>
    /// 전월 대비 증감율 (%)
    /// </summary>
    public string MonthGrowthRate
    {
        get
        {
            if (PrevMonthSales == 0) return MonthSales > 0 ? "+100%" : "0%";
            var rate = (MonthSales - PrevMonthSales) / PrevMonthSales * 100;
            var sign = rate >= 0 ? "+" : "";
            return $"{sign}{rate:F1}%";
        }
    }

    /// <summary>
    /// 이번달 이익 (매출 - 매입)
    /// </summary>
    public decimal MonthProfit => MonthSales - MonthPurchases;

    /// <summary>
    /// 이번달 이익률 (%)
    /// </summary>
    public decimal MonthProfitRate => MonthSales > 0 ? Math.Round(MonthProfit / MonthSales * 100, 1) : 0;

    /// <summary>
    /// 이번달 이익이 음수인지 여부 (DataTrigger 바인딩용)
    /// </summary>
    public bool IsMonthProfitNegative => MonthProfit < 0;

    // ═══════════════════════════════════════════════════════════
    // Filter Properties
    // ═══════════════════════════════════════════════════════════

    public DateTime FromDate
    {
        get => _fromDate;
        set => SetProperty(ref _fromDate, value);
    }

    public DateTime ToDate
    {
        get => _toDate;
        set => SetProperty(ref _toDate, value);
    }

    // ═══════════════════════════════════════════════════════════
    // Monthly Trend
    // ═══════════════════════════════════════════════════════════

    public int TrendMonths
    {
        get => _trendMonths;
        set => SetProperty(ref _trendMonths, value);
    }

    public ObservableCollection<MonthlyTrendDto> MonthlyTrendList { get; }

    public decimal TrendTotalSales => MonthlyTrendList.Sum(m => m.SalesAmount);
    public decimal TrendTotalPurchases => MonthlyTrendList.Sum(m => m.PurchaseAmount);
    public decimal TrendTotalProfit => TrendTotalSales - TrendTotalPurchases;
    public decimal TrendAvgProfitRate => TrendTotalSales > 0 ? Math.Round(TrendTotalProfit / TrendTotalSales * 100, 1) : 0;

    // ═══════════════════════════════════════════════════════════
    // Category
    // ═══════════════════════════════════════════════════════════

    public ObservableCollection<CategorySalesDto> CategoryList { get; }

    public CategorySalesDto? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
                FilterCategoryProducts();
        }
    }

    public ObservableCollection<ProductDetailStatsDto> FilteredCategoryProducts { get; }

    // ═══════════════════════════════════════════════════════════
    // Product Master-Detail
    // ═══════════════════════════════════════════════════════════

    public ObservableCollection<ProductDetailStatsDto> ProductDetailList { get; }

    public ProductDetailStatsDto? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value))
                _ = LoadProductDetailAsync();
        }
    }

    public ObservableCollection<ProductMonthlyBreakdownDto> SelectedProductMonthly { get; }
    public ObservableCollection<ProductCustomerDistributionDto> SelectedProductCustomers { get; }

    // ═══════════════════════════════════════════════════════════
    // Collections (기존)
    // ═══════════════════════════════════════════════════════════

    public ObservableCollection<CompanySalesRankDto> CompanyRankList { get; }
    public ObservableCollection<ProductSalesDto> ProductSalesList { get; }

    // ═══════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════

    public ICommand RefreshCommand { get; }
    public ICommand SetTrend6MonthsCommand { get; }
    public ICommand SetTrend12MonthsCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    // ═══════════════════════════════════════════════════════════
    // Data Loading
    // ═══════════════════════════════════════════════════════════

    public async Task LoadDataAsync()
    {
        var errors = new List<string>();

        using var context = DbContextFactory.Create();
        var service = new SalesStatisticsService(context);

        // 1. 요약 데이터
        try
        {
            var summary = await service.GetSalesSummaryAsync();
            TodaySales = summary.TodaySales;
            TodayCount = summary.TodayCount;
            MonthSales = summary.MonthSales;
            MonthCount = summary.MonthCount;
            PrevMonthSales = summary.PrevMonthSales;
            DraftCount = summary.DraftCount;
            LowStockCount = summary.LowStockCount;
            MonthPurchases = summary.MonthPurchases;
            PendingOrderCount = summary.PendingOrderCount;
            PendingDeliveryCount = summary.PendingDeliveryCount;
        }
        catch (Exception ex)
        {
            errors.Add($"요약: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[통계] 요약 로드 실패: {ex}");
        }

        // 2. 거래처별 매출
        try
        {
            var companyRanks = await service.GetSalesByCompanyAsync(FromDate, ToDate);
            CompanyRankList.Clear();
            foreach (var rank in companyRanks)
                CompanyRankList.Add(rank);
        }
        catch (Exception ex)
        {
            errors.Add($"거래처별: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[통계] 거래처별 로드 실패: {ex}");
        }

        // 3. 상품별 매출
        try
        {
            var productSales = await service.GetSalesByProductAsync(FromDate, ToDate);
            ProductSalesList.Clear();
            foreach (var ps in productSales)
                ProductSalesList.Add(ps);
        }
        catch (Exception ex)
        {
            errors.Add($"상품별: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[통계] 상품별 로드 실패: {ex}");
        }

        // 4. 월별 추이
        try
        {
            var monthlyTrend = await service.GetMonthlyTrendAsync(TrendMonths);
            MonthlyTrendList.Clear();
            foreach (var mt in monthlyTrend)
                MonthlyTrendList.Add(mt);
            RaiseTrendSummaryChanged();
        }
        catch (Exception ex)
        {
            errors.Add($"월별추이: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[통계] 월별 추이 로드 실패: {ex}");
        }

        // 5. 카테고리별
        try
        {
            var categories = await service.GetSalesByCategoryAsync(FromDate, ToDate);
            CategoryList.Clear();
            foreach (var cat in categories)
                CategoryList.Add(cat);
        }
        catch (Exception ex)
        {
            errors.Add($"카테고리: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[통계] 카테고리 로드 실패: {ex}");
        }

        // 6. 품목별 세부
        try
        {
            var productDetails = await service.GetProductDetailStatsAsync(FromDate, ToDate);
            ProductDetailList.Clear();
            foreach (var pd in productDetails)
                ProductDetailList.Add(pd);
        }
        catch (Exception ex)
        {
            errors.Add($"품목세부: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[통계] 품목세부 로드 실패: {ex}");
        }

        SelectedProduct = null;
        SelectedCategory = CategoryList.Count > 0 ? CategoryList[0] : null;

        if (errors.Count == 0)
        {
            StatusMessage = $"통계 로드 완료 (거래처 {CompanyRankList.Count}건, 상품 {ProductDetailList.Count}건, 카테고리 {CategoryList.Count}건)";
        }
        else
        {
            StatusMessage = $"일부 로드 실패: {string.Join(" / ", errors)}";
            System.Diagnostics.Debug.WriteLine($"[통계] 로드 오류 {errors.Count}건: {string.Join(", ", errors)}");
        }
    }

    private async Task LoadMonthlyTrendAsync()
    {
        try
        {
            using var context = DbContextFactory.Create();
            var service = new SalesStatisticsService(context);

            var monthlyTrend = await service.GetMonthlyTrendAsync(TrendMonths);
            MonthlyTrendList.Clear();
            foreach (var mt in monthlyTrend)
                MonthlyTrendList.Add(mt);
            RaiseTrendSummaryChanged();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"월별 추이 로드 실패: {ex.Message}");
        }
    }

    private async Task LoadProductDetailAsync()
    {
        if (SelectedProduct == null)
        {
            SelectedProductMonthly.Clear();
            SelectedProductCustomers.Clear();
            return;
        }

        try
        {
            using var context = DbContextFactory.Create();
            var service = new SalesStatisticsService(context);

            var monthly = await service.GetProductMonthlyBreakdownAsync(SelectedProduct.ProductId, 6);
            SelectedProductMonthly.Clear();
            foreach (var m in monthly)
                SelectedProductMonthly.Add(m);

            var customers = await service.GetProductCustomerDistributionAsync(SelectedProduct.ProductId, FromDate, ToDate);
            SelectedProductCustomers.Clear();
            foreach (var c in customers)
                SelectedProductCustomers.Add(c);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"품목 세부 로드 실패: {ex.Message}");
        }
    }

    private void FilterCategoryProducts()
    {
        FilteredCategoryProducts.Clear();
        if (SelectedCategory == null) return;

        var category = SelectedCategory.Category;
        foreach (var p in ProductDetailList)
        {
            var pCat = p.Category ?? "미분류";
            if (pCat == category)
                FilteredCategoryProducts.Add(p);
        }
    }

    private void RaiseTrendSummaryChanged()
    {
        RaisePropertyChanged(nameof(TrendTotalSales));
        RaisePropertyChanged(nameof(TrendTotalPurchases));
        RaisePropertyChanged(nameof(TrendTotalProfit));
        RaisePropertyChanged(nameof(TrendAvgProfitRate));
    }
}

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;
using Tran.Core.Services;
using Tran.Data;
using Tran.Data.Services;

namespace Tran.Desktop.ViewModels;

/// <summary>
/// 판매 화면 ViewModel (3-split layout)
/// 좌측: 품목 목록, 우측: 자주 거래 품목, 하단: 판매 편집/이력
/// 재고 확인 기능 포함
/// </summary>
public class SaleViewModel : ViewModelBase
{
    private string _companyId = string.Empty;
    private string _searchText = string.Empty;
    private decimal _totalAmount;
    private string _statusMessage = string.Empty;
    private Product? _selectedProduct;
    private CompanyProduct? _selectedFrequentProduct;

    public SaleViewModel()
    {
        AllProducts = new ObservableCollection<Product>();
        FrequentProducts = new ObservableCollection<CompanyProduct>();
        CurrentSaleItems = new ObservableCollection<SaleItemRow>();
        SaleHistory = new ObservableCollection<Sale>();
        DraftSales = new ObservableCollection<Sale>();

        AddProductCommand = new RelayCommand<Product>(ExecuteAddProduct);
        AddFrequentProductCommand = new RelayCommand<CompanyProduct>(ExecuteAddFrequentProduct);
        RemoveItemCommand = new RelayCommand<SaleItemRow>(ExecuteRemoveItem);
        ConfirmSaleCommand = new AsyncRelayCommand(ExecuteConfirmSaleAsync);
        SaveDraftCommand = new AsyncRelayCommand(ExecuteSaveDraftAsync);
        SearchCommand = new RelayCommand(ExecuteSearch);
        AddToFrequentCommand = new AsyncRelayCommand(ExecuteAddToFrequentAsync);
        RemoveFromFrequentCommand = new AsyncRelayCommand(ExecuteRemoveFromFrequentAsync);

        CurrentSaleItems.CollectionChanged += (_, _) => RecalculateTotal();
    }

    // ═══════════════════════════════════════════════════════════
    // Nested Helper Class
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 판매 품목 행 (편집 가능, 재고 상태 포함)
    /// </summary>
    public class SaleItemRow : ViewModelBase
    {
        private int _productId;
        private string _productName = string.Empty;
        private decimal _quantity = 1;
        private decimal _unitPrice;
        private decimal _lineAmount;
        private string? _note;
        private string _stockStatus = string.Empty;
        private decimal _stockQuantity;

        public int ProductId
        {
            get => _productId;
            set => SetProperty(ref _productId, value);
        }

        public string ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    LineAmount = _quantity * _unitPrice;
                    UpdateStockStatus();
                }
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (SetProperty(ref _unitPrice, value))
                {
                    LineAmount = _quantity * _unitPrice;
                }
            }
        }

        public decimal LineAmount
        {
            get => _lineAmount;
            set => SetProperty(ref _lineAmount, value);
        }

        public string? Note
        {
            get => _note;
            set => SetProperty(ref _note, value);
        }

        /// <summary>
        /// 재고 상태 텍스트 ("충분", "부족", "품절")
        /// </summary>
        public string StockStatus
        {
            get => _stockStatus;
            set => SetProperty(ref _stockStatus, value);
        }

        /// <summary>
        /// 현재 재고 수량 (가용 재고)
        /// </summary>
        public decimal StockQuantity
        {
            get => _stockQuantity;
            set
            {
                if (SetProperty(ref _stockQuantity, value))
                {
                    UpdateStockStatus();
                }
            }
        }

        /// <summary>
        /// 재고 상태 표시 텍스트 (수량 포함)
        /// </summary>
        public string StockDisplayText => StockQuantity <= 0
            ? "품절"
            : $"{StockQuantity:N0}";

        private void UpdateStockStatus()
        {
            if (StockQuantity <= 0)
                StockStatus = "품절";
            else if (StockQuantity < Quantity)
                StockStatus = "부족";
            else
                StockStatus = "충분";
            RaisePropertyChanged(nameof(StockDisplayText));
        }
    }

    /// <summary>
    /// 데이터 변경 시 다른 탭 갱신을 위한 콜백
    /// </summary>
    public Func<Task>? OnDataChanged { get; set; }

    // ═══════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 현재 거래처 ID
    /// </summary>
    public string CompanyId
    {
        get => _companyId;
        set => SetProperty(ref _companyId, value);
    }

    /// <summary>
    /// 전체 품목 목록 (원본 데이터)
    /// </summary>
    public ObservableCollection<Product> AllProducts { get; }

    /// <summary>
    /// 필터링된 품목 목록 (좌측 패널 표시용)
    /// </summary>
    public ObservableCollection<Product> FilteredProducts { get; } = new();

    /// <summary>
    /// 자주 거래 품목
    /// </summary>
    public ObservableCollection<CompanyProduct> FrequentProducts { get; }

    /// <summary>
    /// 현재 판매 품목 (편집 가능한 행 목록)
    /// </summary>
    public ObservableCollection<SaleItemRow> CurrentSaleItems { get; }

    /// <summary>
    /// 판매 이력
    /// </summary>
    public ObservableCollection<Sale> SaleHistory { get; }

    /// <summary>
    /// 임시 저장 목록
    /// </summary>
    public ObservableCollection<Sale> DraftSales { get; }

    /// <summary>
    /// 품목 검색어
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyProductFilter();
            }
        }
    }

    /// <summary>
    /// 합계 금액
    /// </summary>
    public decimal TotalAmount
    {
        get => _totalAmount;
        private set => SetProperty(ref _totalAmount, value);
    }

    /// <summary>
    /// 상태 메시지
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// 좌측 전체 품목에서 선택된 품목
    /// </summary>
    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set => SetProperty(ref _selectedProduct, value);
    }

    /// <summary>
    /// 우측 자주 거래 품목에서 선택된 품목
    /// </summary>
    public CompanyProduct? SelectedFrequentProduct
    {
        get => _selectedFrequentProduct;
        set => SetProperty(ref _selectedFrequentProduct, value);
    }

    // ═══════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 품목 추가 (재고 확인 포함)
    /// </summary>
    public ICommand AddProductCommand { get; }

    /// <summary>
    /// 자주 거래 품목 추가 (CompanyProduct → Product 변환)
    /// </summary>
    public ICommand AddFrequentProductCommand { get; }

    /// <summary>
    /// 품목 제거
    /// </summary>
    public ICommand RemoveItemCommand { get; }

    /// <summary>
    /// 판매 확정
    /// </summary>
    public ICommand ConfirmSaleCommand { get; }

    /// <summary>
    /// 임시 저장
    /// </summary>
    public ICommand SaveDraftCommand { get; }

    /// <summary>
    /// 검색
    /// </summary>
    public ICommand SearchCommand { get; }

    /// <summary>
    /// 자주거래 품목에 추가 (→ 버튼)
    /// </summary>
    public ICommand AddToFrequentCommand { get; }

    /// <summary>
    /// 자주거래 품목에서 제거 (← 버튼)
    /// </summary>
    public ICommand RemoveFromFrequentCommand { get; }

    // ═══════════════════════════════════════════════════════════
    // Public Methods
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 데이터 로드 (거래처 설정 후 호출)
    /// </summary>
    public async Task LoadDataAsync(string companyId)
    {
        CompanyId = companyId;

        try
        {
            using var context = CreateDbContext();

            // 전체 품목 로드
            var products = await context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            AllProducts.Clear();
            foreach (var product in products)
            {
                AllProducts.Add(product);
            }

            // 검색 필터 적용
            ApplyProductFilter();

            // 자주 거래 품목 로드
            var frequentProducts = await context.CompanyProducts
                .Include(cp => cp.Product)
                .Where(cp => cp.CompanyId == companyId)
                .OrderByDescending(cp => cp.OrderCount)
                .Take(20)
                .ToListAsync();

            FrequentProducts.Clear();
            foreach (var cp in frequentProducts)
            {
                FrequentProducts.Add(cp);
            }

            // 판매 이력 로드
            var sales = await context.Sales
                .Include(s => s.Items)
                .Where(s => s.CompanyId == companyId && s.State != SaleState.Draft)
                .OrderByDescending(s => s.SaleDate)
                .Take(50)
                .ToListAsync();

            SaleHistory.Clear();
            foreach (var sale in sales)
            {
                SaleHistory.Add(sale);
            }

            // 임시 저장 목록 로드
            var drafts = await context.Sales
                .Include(s => s.Items)
                .Where(s => s.CompanyId == companyId && s.State == SaleState.Draft)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            DraftSales.Clear();
            foreach (var draft in drafts)
            {
                DraftSales.Add(draft);
            }

            // 현재 판매 초기화
            UnsubscribeAllItems();
            CurrentSaleItems.Clear();
            TotalAmount = 0;
        }
        catch (Exception ex)
        {
            StatusMessage = $"데이터 로드 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"SaleViewModel 로드 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 재고 확인
    /// </summary>
    public async Task<string> CheckStock(int productId, decimal quantity)
    {
        try
        {
            using var context = CreateDbContext();
            var inventoryService = new InventoryService(context);
            var inventory = await inventoryService.GetByProductIdAsync(productId);

            if (inventory == null)
                return "품절";

            if (inventory.AvailableQuantity <= 0)
                return "품절";

            if (inventory.AvailableQuantity < quantity)
                return "부족";

            return "충분";
        }
        catch
        {
            return "확인불가";
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Event Handlers
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// SaleItemRow.PropertyChanged 이벤트 핸들러 (명명된 메서드로 구독 해제 가능)
    /// </summary>
    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(SaleItemRow.LineAmount))
        {
            RecalculateTotal();
        }
    }

    /// <summary>
    /// 모든 현재 품목의 PropertyChanged 구독 해제
    /// </summary>
    private void UnsubscribeAllItems()
    {
        foreach (var item in CurrentSaleItems)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Private Methods
    // ═══════════════════════════════════════════════════════════

    private TranDbContext CreateDbContext()
    {
        return DbContextFactory.Create();
    }

    private void ExecuteAddFrequentProduct(CompanyProduct? companyProduct)
    {
        if (companyProduct?.Product == null) return;
        ExecuteAddProduct(companyProduct.Product);
    }

    private async void ExecuteAddProduct(Product? product)
    {
        if (product == null) return;

        try
        {
            // 이미 추가된 품목인지 확인
            var existing = CurrentSaleItems.FirstOrDefault(i => i.ProductId == product.ProductId);
            if (existing != null)
            {
                existing.Quantity += 1;
                RecalculateTotal();
                return;
            }

            // 재고 확인
            decimal stockQuantity = 0;
            try
            {
                using var context = CreateDbContext();
                var inventoryService = new InventoryService(context);
                var inventory = await inventoryService.GetByProductIdAsync(product.ProductId);
                stockQuantity = inventory?.AvailableQuantity ?? 0;
            }
            catch
            {
                // 재고 확인 실패 시 0으로 처리
            }

            var row = new SaleItemRow
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Quantity = 1,
                UnitPrice = product.DefaultPrice,
                LineAmount = product.DefaultPrice,
                StockQuantity = stockQuantity
            };

            row.PropertyChanged += OnItemPropertyChanged;

            // 재고 부족 경고
            if (stockQuantity <= 0)
            {
                StatusMessage = $"경고: {product.ProductName} 품절 상태입니다.";
            }
            else if (stockQuantity < 1)
            {
                StatusMessage = $"경고: {product.ProductName} 재고 부족 (가용: {stockQuantity})";
            }

            CurrentSaleItems.Add(row);
        }
        catch (Exception ex)
        {
            StatusMessage = $"품목 추가 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"SaleViewModel.ExecuteAddProduct 오류: {ex}");
        }
    }

    private void ExecuteRemoveItem(SaleItemRow? item)
    {
        if (item == null) return;
        item.PropertyChanged -= OnItemPropertyChanged;
        CurrentSaleItems.Remove(item);
    }

    private async Task ExecuteConfirmSaleAsync()
    {
        if (!CurrentSaleItems.Any())
        {
            StatusMessage = "품목을 추가해 주세요.";
            return;
        }

        try
        {
            using var context = CreateDbContext();
            var saleService = new SaleService(context);

            // Draft 생성
            var sale = BuildSaleFromItems();
            var createdSale = await saleService.CreateDraftAsync(sale);

            // 판매 확정
            var confirmedSale = await saleService.ConfirmSaleAsync(createdSale.SaleId);

            StatusMessage = $"판매 #{confirmedSale.SaleId} 확정 완료";

            // 이력에 추가
            SaleHistory.Insert(0, confirmedSale);

            // 현재 판매 초기화
            UnsubscribeAllItems();
            CurrentSaleItems.Clear();
            TotalAmount = 0;

            // 다른 탭 갱신 알림
            if (OnDataChanged != null) await OnDataChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = $"판매 확정 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"판매 확정 실패: {ex.Message}");
        }
    }

    private async Task ExecuteSaveDraftAsync()
    {
        if (!CurrentSaleItems.Any())
        {
            StatusMessage = "품목을 추가해 주세요.";
            return;
        }

        try
        {
            using var context = CreateDbContext();
            var saleService = new SaleService(context);

            var sale = BuildSaleFromItems();
            var savedSale = await saleService.CreateDraftAsync(sale);

            StatusMessage = $"임시 저장 완료 (#{savedSale.SaleId})";

            // 임시 저장 목록에 추가
            DraftSales.Insert(0, savedSale);

            // 현재 판매 초기화
            UnsubscribeAllItems();
            CurrentSaleItems.Clear();
            TotalAmount = 0;
        }
        catch (Exception ex)
        {
            StatusMessage = $"임시 저장 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"임시 저장 실패: {ex.Message}");
        }
    }

    private async Task ExecuteAddToFrequentAsync()
    {
        if (SelectedProduct == null) return;

        try
        {
            using var context = CreateDbContext();
            var service = new PricePolicyService(context);
            await service.RegisterCompanyProductAsync(CompanyId, SelectedProduct.ProductId);
            await ReloadFrequentProductsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"자주거래 품목 추가 실패: {ex.Message}";
        }
    }

    private async Task ExecuteRemoveFromFrequentAsync()
    {
        if (SelectedFrequentProduct == null) return;

        try
        {
            using var context = CreateDbContext();
            var service = new PricePolicyService(context);
            await service.RemoveCompanyProductAsync(CompanyId, SelectedFrequentProduct.ProductId);
            await ReloadFrequentProductsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"자주거래 품목 제거 실패: {ex.Message}";
        }
    }

    private async Task ReloadFrequentProductsAsync()
    {
        using var context = CreateDbContext();
        var frequentProducts = await context.CompanyProducts
            .Include(cp => cp.Product)
            .Where(cp => cp.CompanyId == CompanyId)
            .OrderByDescending(cp => cp.OrderCount)
            .Take(20)
            .ToListAsync();

        FrequentProducts.Clear();
        foreach (var cp in frequentProducts)
            FrequentProducts.Add(cp);
    }

    private void ExecuteSearch()
    {
        ApplyProductFilter();
    }

    private void ApplyProductFilter()
    {
        FilteredProducts.Clear();

        var source = string.IsNullOrWhiteSpace(SearchText)
            ? AllProducts
            : AllProducts.Where(p =>
                p.ProductName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        foreach (var product in source)
        {
            FilteredProducts.Add(product);
        }
    }

    private Sale BuildSaleFromItems()
    {
        var sale = new Sale
        {
            CompanyId = CompanyId,
            SaleDate = DateTime.Today,
            State = SaleState.Draft,
            TotalAmount = TotalAmount,
            Items = CurrentSaleItems.Select(row => new SaleItem
            {
                ProductId = row.ProductId,
                ProductName = row.ProductName,
                Quantity = row.Quantity,
                UnitPrice = row.UnitPrice,
                LineAmount = row.LineAmount,
                Note = row.Note
            }).ToList()
        };
        return sale;
    }

    private void RecalculateTotal()
    {
        TotalAmount = CurrentSaleItems.Sum(i => i.LineAmount);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UnsubscribeAllItems();
        }
        base.Dispose(disposing);
    }
}

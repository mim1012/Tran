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
/// 발주 화면 ViewModel (3-split layout)
/// 좌측: 품목 목록, 우측: 자주 거래 품목, 하단: 발주 편집/이력
/// </summary>
public class OrderViewModel : ViewModelBase
{
    private string _companyId = string.Empty;
    private Order? _currentOrder;
    private string _searchText = string.Empty;
    private decimal _totalAmount;
    private string _statusMessage = string.Empty;
    private Product? _selectedProduct;
    private CompanyProduct? _selectedFrequentProduct;

    public OrderViewModel()
    {
        AllProducts = new ObservableCollection<Product>();
        FrequentProducts = new ObservableCollection<CompanyProduct>();
        CurrentOrderItems = new ObservableCollection<OrderItemRow>();
        OrderHistory = new ObservableCollection<Order>();
        DraftOrders = new ObservableCollection<Order>();

        AddProductCommand = new RelayCommand<Product>(ExecuteAddProduct);
        AddFrequentProductCommand = new RelayCommand<CompanyProduct>(ExecuteAddFrequentProduct);
        RemoveItemCommand = new RelayCommand<OrderItemRow>(ExecuteRemoveItem);
        CompleteOrderCommand = new AsyncRelayCommand(ExecuteCompleteOrderAsync);
        SaveDraftCommand = new AsyncRelayCommand(ExecuteSaveDraftAsync);
        LoadDraftCommand = new RelayCommand<Order>(ExecuteLoadDraft);
        SearchCommand = new RelayCommand(ExecuteSearch);
        AddToFrequentCommand = new AsyncRelayCommand(ExecuteAddToFrequentAsync);
        RemoveFromFrequentCommand = new AsyncRelayCommand(ExecuteRemoveFromFrequentAsync);

        CurrentOrderItems.CollectionChanged += (_, _) => RecalculateTotal();
    }

    /// <summary>
    /// OrderItemRow.PropertyChanged 이벤트 핸들러 (명명된 메서드로 구독 해제 가능)
    /// </summary>
    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(OrderItemRow.LineAmount))
        {
            RecalculateTotal();
        }
    }

    /// <summary>
    /// 모든 현재 품목의 PropertyChanged 구독 해제
    /// </summary>
    private void UnsubscribeAllItems()
    {
        foreach (var item in CurrentOrderItems)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Nested Helper Class
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 발주 품목 행 (편집 가능한 UI 행)
    /// </summary>
    public class OrderItemRow : ViewModelBase
    {
        private int _productId;
        private string _productName = string.Empty;
        private decimal _quantity = 1;
        private decimal _unitPrice;
        private decimal _lineAmount;
        private string? _note;

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
        /// 행 금액 변경 시 부모에게 통지
        /// </summary>
        public Action? OnAmountChanged { get; set; }
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
    /// 자주 거래 품목 (우측 패널)
    /// </summary>
    public ObservableCollection<CompanyProduct> FrequentProducts { get; }

    /// <summary>
    /// 현재 작성 중인 발주
    /// </summary>
    public Order? CurrentOrder
    {
        get => _currentOrder;
        set => SetProperty(ref _currentOrder, value);
    }

    /// <summary>
    /// 현재 발주 품목 (편집 가능한 행 목록)
    /// </summary>
    public ObservableCollection<OrderItemRow> CurrentOrderItems { get; }

    /// <summary>
    /// 발주 이력 (하단 탭)
    /// </summary>
    public ObservableCollection<Order> OrderHistory { get; }

    /// <summary>
    /// 임시 저장 목록 (하단 탭)
    /// </summary>
    public ObservableCollection<Order> DraftOrders { get; }

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
    /// 상태 메시지 (토스트 대용)
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
    /// 품목 추가
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
    /// 발주 확정
    /// </summary>
    public ICommand CompleteOrderCommand { get; }

    /// <summary>
    /// 임시 저장
    /// </summary>
    public ICommand SaveDraftCommand { get; }

    /// <summary>
    /// 임시 저장 불러오기
    /// </summary>
    public ICommand LoadDraftCommand { get; }

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

            // 발주 이력 로드
            var orders = await context.Orders
                .Include(o => o.Items)
                .Where(o => o.CompanyId == companyId && o.State != OrderState.Draft)
                .OrderByDescending(o => o.OrderDate)
                .Take(50)
                .ToListAsync();

            OrderHistory.Clear();
            foreach (var order in orders)
            {
                OrderHistory.Add(order);
            }

            // 임시 저장 목록 로드
            var drafts = await context.Orders
                .Include(o => o.Items)
                .Where(o => o.CompanyId == companyId && o.State == OrderState.Draft)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            DraftOrders.Clear();
            foreach (var draft in drafts)
            {
                DraftOrders.Add(draft);
            }

            // 새 발주 초기화
            ClearCurrentOrder();
        }
        catch (Exception ex)
        {
            StatusMessage = $"데이터 로드 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"OrderViewModel 로드 실패: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Private Methods
    // ═══════════════════════════════════════════════════════════

    private TranDbContext CreateDbContext()
    {
        return DbContextFactory.Create();
    }

    private void ExecuteAddProduct(Product? product)
    {
        if (product == null) return;

        // 이미 추가된 품목인지 확인
        var existing = CurrentOrderItems.FirstOrDefault(i => i.ProductId == product.ProductId);
        if (existing != null)
        {
            existing.Quantity += 1;
            RecalculateTotal();
            return;
        }

        var row = new OrderItemRow
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            Quantity = 1,
            UnitPrice = product.DefaultPrice,
            LineAmount = product.DefaultPrice,
            OnAmountChanged = RecalculateTotal
        };

        // 행 속성 변경 시 합계 재계산 (명명된 메서드로 구독 - 해제 가능)
        row.PropertyChanged += OnItemPropertyChanged;

        CurrentOrderItems.Add(row);
    }

    private void ExecuteAddFrequentProduct(CompanyProduct? companyProduct)
    {
        if (companyProduct?.Product == null) return;
        ExecuteAddProduct(companyProduct.Product);
    }

    private void ExecuteRemoveItem(OrderItemRow? item)
    {
        if (item == null) return;
        item.PropertyChanged -= OnItemPropertyChanged;
        CurrentOrderItems.Remove(item);
    }

    private async Task ExecuteCompleteOrderAsync()
    {
        if (!CurrentOrderItems.Any())
        {
            StatusMessage = "품목을 추가해 주세요.";
            return;
        }

        try
        {
            using var context = CreateDbContext();
            var orderService = new OrderService(context);

            // Draft 생성
            var order = BuildOrderFromItems();
            var createdOrder = await orderService.CreateDraftAsync(order);

            // 발주 확정
            var completedOrder = await orderService.CompleteOrderAsync(createdOrder.OrderId);

            StatusMessage = $"발주 #{completedOrder.OrderId} 확정 완료";

            // 이력에 추가
            OrderHistory.Insert(0, completedOrder);

            // 현재 발주 초기화
            ClearCurrentOrder();

            // 다른 탭 갱신 알림
            if (OnDataChanged != null) await OnDataChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = $"발주 확정 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"발주 확정 실패: {ex.Message}");
        }
    }

    private async Task ExecuteSaveDraftAsync()
    {
        if (!CurrentOrderItems.Any())
        {
            StatusMessage = "품목을 추가해 주세요.";
            return;
        }

        try
        {
            using var context = CreateDbContext();
            var orderService = new OrderService(context);

            var order = BuildOrderFromItems();
            var savedOrder = await orderService.CreateDraftAsync(order);

            StatusMessage = $"임시 저장 완료 (#{savedOrder.OrderId})";

            // 임시 저장 목록에 추가
            DraftOrders.Insert(0, savedOrder);

            // 현재 발주 초기화
            ClearCurrentOrder();
        }
        catch (Exception ex)
        {
            StatusMessage = $"임시 저장 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"임시 저장 실패: {ex.Message}");
        }
    }

    private void ExecuteLoadDraft(Order? order)
    {
        if (order == null) return;

        CurrentOrder = order;
        UnsubscribeAllItems();
        CurrentOrderItems.Clear();

        foreach (var item in order.Items)
        {
            var row = new OrderItemRow
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineAmount = item.LineAmount,
                Note = item.Note
            };

            row.PropertyChanged += OnItemPropertyChanged;

            CurrentOrderItems.Add(row);
        }

        RecalculateTotal();
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

    private Order BuildOrderFromItems()
    {
        var order = new Order
        {
            CompanyId = CompanyId,
            OrderDate = DateTime.Today,
            State = OrderState.Draft,
            TotalAmount = TotalAmount,
            Items = CurrentOrderItems.Select(row => new OrderItem
            {
                ProductId = row.ProductId,
                ProductName = row.ProductName,
                Quantity = row.Quantity,
                UnitPrice = row.UnitPrice,
                LineAmount = row.LineAmount,
                Note = row.Note
            }).ToList()
        };
        return order;
    }

    private void ClearCurrentOrder()
    {
        CurrentOrder = null;
        UnsubscribeAllItems();
        CurrentOrderItems.Clear();
        TotalAmount = 0;
    }

    private void RecalculateTotal()
    {
        TotalAmount = CurrentOrderItems.Sum(i => i.LineAmount);
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

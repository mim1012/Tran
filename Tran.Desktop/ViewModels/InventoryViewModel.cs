using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;
using Tran.Core.Services;
using Tran.Data;
using Tran.Data.Services;

namespace Tran.Desktop.ViewModels;

/// <summary>
/// 재고 관리 ViewModel (읽기 전용 테이블 + 이력 탭)
/// 재고 현황 조회 및 수동 조정
/// </summary>
public class InventoryViewModel : ViewModelBase
{
    private string _searchText = string.Empty;
    private InventoryRow? _selectedProduct;
    private string _statusMessage = string.Empty;

    public InventoryViewModel()
    {
        InventoryList = new ObservableCollection<InventoryRow>();
        TransactionHistory = new ObservableCollection<InventoryTransaction>();
        OrderSuggestions = new ObservableCollection<OrderSuggestionRow>();

        AdjustInventoryCommand = new RelayCommand(async () => await ExecuteAdjustInventoryAsync());
        RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
        CreateOrderCommand = new RelayCommand<OrderSuggestionRow>(async (row) => await ExecuteCreateOrderAsync(row));
        CreateAllOrdersCommand = new RelayCommand(async () => await ExecuteCreateAllOrdersAsync());
        DeleteInventoryCommand = new RelayCommand<InventoryRow>(async (row) => await ExecuteDeleteInventoryAsync(row));
    }

    // ═══════════════════════════════════════════════════════════
    // Nested Helper Class
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 재고 현황 행 (읽기 전용, 계산 속성 포함)
    /// </summary>
    public class InventoryRow : ViewModelBase
    {
        private int _productId;
        private string _productName = string.Empty;
        private string? _productCode;
        private decimal _confirmedQuantity;
        private decimal _pendingInQuantity;
        private decimal _pendingOutQuantity;
        private decimal _availableQuantity;
        private decimal _safetyStock;
        private string _category = string.Empty;
        private string _stockStatus = string.Empty;
        private string _stockStatusColor = string.Empty;

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

        public string? ProductCode
        {
            get => _productCode;
            set => SetProperty(ref _productCode, value);
        }

        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        /// <summary>
        /// 재고 상태 정렬용 (0=정상, 1=부족, 2=품절)
        /// </summary>
        public int StockStatusOrder => StockStatus == "품절" ? 2 : StockStatus == "부족" ? 1 : 0;

        /// <summary>
        /// 확정 수량 (실재고)
        /// </summary>
        public decimal ConfirmedQuantity
        {
            get => _confirmedQuantity;
            set => SetProperty(ref _confirmedQuantity, value);
        }

        /// <summary>
        /// 입고 예정 수량
        /// </summary>
        public decimal PendingInQuantity
        {
            get => _pendingInQuantity;
            set => SetProperty(ref _pendingInQuantity, value);
        }

        /// <summary>
        /// 출고 예정 수량
        /// </summary>
        public decimal PendingOutQuantity
        {
            get => _pendingOutQuantity;
            set => SetProperty(ref _pendingOutQuantity, value);
        }

        /// <summary>
        /// 가용 수량 (확정 - 출고예정)
        /// </summary>
        public decimal AvailableQuantity
        {
            get => _availableQuantity;
            set => SetProperty(ref _availableQuantity, value);
        }

        /// <summary>
        /// 안전 재고 수량
        /// </summary>
        public decimal SafetyStock
        {
            get => _safetyStock;
            set => SetProperty(ref _safetyStock, value);
        }

        /// <summary>
        /// 재고 상태 ("정상", "부족", "품절")
        /// </summary>
        public string StockStatus
        {
            get => _stockStatus;
            set => SetProperty(ref _stockStatus, value);
        }

        /// <summary>
        /// 재고 상태 색상 (UI 바인딩용)
        /// </summary>
        public string StockStatusColor
        {
            get => _stockStatusColor;
            set => SetProperty(ref _stockStatusColor, value);
        }

        /// <summary>
        /// 재고 상태 계산 (현재재고 기준)
        /// </summary>
        public void ComputeStockStatus()
        {
            if (ConfirmedQuantity <= 0)
            {
                StockStatus = "품절";
                StockStatusColor = "#C92A2A";
            }
            else if (SafetyStock > 0 && ConfirmedQuantity < SafetyStock)
            {
                StockStatus = "부족";
                StockStatusColor = "#E67700";
            }
            else
            {
                StockStatus = "정상";
                StockStatusColor = "#1E7F34";
            }
        }
    }

    /// <summary>
    /// 발주 제안 행 (수량 편집 가능)
    /// </summary>
    public class OrderSuggestionRow : ViewModelBase
    {
        private decimal _suggestedQty;

        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductCode { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal SafetyStock { get; set; }
        public decimal ShortageQty { get; set; }

        public decimal SuggestedQty
        {
            get => _suggestedQty;
            set => SetProperty(ref _suggestedQty, value);
        }

        public string? SuggestedCompanyId { get; set; }
        public string? SuggestedCompanyName { get; set; }
        public decimal? SuggestedUnitPrice { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 재고 목록 (메인 테이블)
    /// </summary>
    public ObservableCollection<InventoryRow> InventoryList { get; }

    /// <summary>
    /// 재고 이동 이력 (이력 탭)
    /// </summary>
    public ObservableCollection<InventoryTransaction> TransactionHistory { get; }

    /// <summary>
    /// 발주 제안 목록
    /// </summary>
    public ObservableCollection<OrderSuggestionRow> OrderSuggestions { get; }

    /// <summary>
    /// 발주 제안 건수
    /// </summary>
    public int SuggestionCount => OrderSuggestions.Count;

    // 재고 요약 프로퍼티 (하단 요약 바인딩)
    public int TotalProductCount => InventoryList.Count;
    public int NormalStockCount => InventoryList.Count(i => i.StockStatus == "정상");
    public int LowStockCount => InventoryList.Count(i => i.StockStatus == "부족");
    public int OutOfStockCount => InventoryList.Count(i => i.StockStatus == "품절");

    // 부족 재고 알림 프로퍼티
    private int _lowStockAlertCount;
    public int LowStockAlertCount
    {
        get => _lowStockAlertCount;
        set
        {
            if (SetProperty(ref _lowStockAlertCount, value))
                RaisePropertyChanged(nameof(HasLowStockAlert));
        }
    }

    private string _lowStockAlertMessage = string.Empty;
    public string LowStockAlertMessage
    {
        get => _lowStockAlertMessage;
        set => SetProperty(ref _lowStockAlertMessage, value);
    }

    public bool HasLowStockAlert => LowStockAlertCount > 0;

    /// <summary>
    /// 검색어
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    /// <summary>
    /// 선택된 품목 (이력 로드 트리거)
    /// </summary>
    public InventoryRow? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value))
            {
                if (value != null)
                {
                    _ = LoadHistoryAsync(value.ProductId);
                }
            }
        }
    }

    /// <summary>
    /// 상태 메시지
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    // ═══════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 재고 수동 조정
    /// </summary>
    public ICommand AdjustInventoryCommand { get; }

    /// <summary>
    /// 새로고침
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// 개별 발주 생성
    /// </summary>
    public ICommand CreateOrderCommand { get; }

    /// <summary>
    /// 전체 발주 일괄 생성
    /// </summary>
    public ICommand CreateAllOrdersCommand { get; }

    /// <summary>
    /// 재고 레코드 삭제
    /// </summary>
    public ICommand DeleteInventoryCommand { get; }

    // ═══════════════════════════════════════════════════════════
    // Public Methods
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 전체 재고 데이터 로드
    /// </summary>
    public async Task LoadDataAsync()
    {
        try
        {
            using var context = CreateDbContext();
            var inventoryService = new InventoryService(context);

            var inventories = await inventoryService.GetAllInventoryAsync();

            // Product 정보 로드 (Navigation property가 로드되지 않을 수 있으므로)
            var productIds = inventories.Select(i => i.ProductId).ToList();
            var products = await context.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId);

            InventoryList.Clear();
            foreach (var inv in inventories)
            {
                products.TryGetValue(inv.ProductId, out var product);

                var row = new InventoryRow
                {
                    ProductId = inv.ProductId,
                    ProductName = product?.ProductName ?? $"품목 #{inv.ProductId}",
                    ProductCode = product?.ProductCode,
                    ConfirmedQuantity = inv.ConfirmedQuantity,
                    PendingInQuantity = inv.PendingInQuantity,
                    PendingOutQuantity = inv.PendingOutQuantity,
                    AvailableQuantity = inv.AvailableQuantity,
                    SafetyStock = inv.SafetyStock
                };

                row.ComputeStockStatus();
                InventoryList.Add(row);
            }

            StatusMessage = $"재고 {InventoryList.Count}건 로드 완료";
            RaisePropertyChanged(nameof(TotalProductCount));
            RaisePropertyChanged(nameof(NormalStockCount));
            RaisePropertyChanged(nameof(LowStockCount));
            RaisePropertyChanged(nameof(OutOfStockCount));

            // 부족 재고 알림 계산
            var lowStockItems = InventoryList
                .Where(i => i.SafetyStock > 0 && i.ConfirmedQuantity < i.SafetyStock)
                .ToList();
            LowStockAlertCount = lowStockItems.Count;
            if (lowStockItems.Count > 0)
            {
                var names = string.Join(", ", lowStockItems.Select(i => i.ProductName));
                LowStockAlertMessage = $"안전재고 미달 {lowStockItems.Count}건: {names}";
                StatusMessage = LowStockAlertMessage;
            }

            // 발주 제안 로드
            await LoadSuggestionsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"재고 로드 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"InventoryViewModel 로드 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 품목별 이력 로드
    /// </summary>
    public async Task LoadHistoryAsync(int productId)
    {
        try
        {
            using var context = CreateDbContext();
            var inventoryService = new InventoryService(context);

            var history = await inventoryService.GetTransactionHistoryAsync(productId, 100);

            TransactionHistory.Clear();
            foreach (var tx in history)
            {
                TransactionHistory.Add(tx);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"이력 로드 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"이력 로드 실패: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Private Methods
    // ═══════════════════════════════════════════════════════════

    private TranDbContext CreateDbContext()
    {
        return DbContextFactory.Create();
    }

    /// <summary>
    /// 재고 수동 조정 실행
    /// 실제 UI에서는 다이얼로그를 통해 조정 수량/사유를 입력받지만,
    /// 현재는 SelectedProduct 기반으로 처리
    /// </summary>
    private Task ExecuteAdjustInventoryAsync()
    {
        if (SelectedProduct == null)
        {
            StatusMessage = "품목을 선택해 주세요.";
            return Task.CompletedTask;
        }

        // TODO: 실제 UI에서는 MessageBox/다이얼로그로 수량/사유 입력
        // 여기서는 ViewModel 레벨의 조정 메서드만 제공
        // View 레이어에서 다이얼로그 처리 후 AdjustInventoryAsync 호출

        StatusMessage = "재고 조정 다이얼로그를 열어주세요.";
        return Task.CompletedTask;
    }

    /// <summary>
    /// 재고 조정 실행 (View 레이어에서 호출)
    /// </summary>
    /// <param name="productId">품목 ID</param>
    /// <param name="adjustmentQty">조정 수량 (양수: 증가, 음수: 감소)</param>
    /// <param name="reason">조정 사유</param>
    public async Task ExecuteAdjustAsync(int productId, decimal adjustmentQty, string reason)
    {
        try
        {
            using var context = CreateDbContext();
            var inventoryService = new InventoryService(context);

            await inventoryService.AdjustInventoryAsync(productId, adjustmentQty, reason);

            StatusMessage = $"재고 조정 완료 (품목 #{productId}, {(adjustmentQty >= 0 ? "+" : "")}{adjustmentQty})";

            // 데이터 새로고침
            await LoadDataAsync();

            // 이력 새로고침
            if (SelectedProduct?.ProductId == productId)
            {
                await LoadHistoryAsync(productId);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"재고 조정 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"재고 조정 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 안전재고 기반 발주 제안 로드
    /// </summary>
    public async Task LoadSuggestionsAsync()
    {
        try
        {
            using var context = CreateDbContext();
            var autoOrderService = new AutoOrderService(context);

            var suggestions = await autoOrderService.GetOrderSuggestionsAsync();

            OrderSuggestions.Clear();
            foreach (var s in suggestions)
            {
                OrderSuggestions.Add(new OrderSuggestionRow
                {
                    ProductId = s.ProductId,
                    ProductName = s.ProductName,
                    ProductCode = s.ProductCode,
                    CurrentStock = s.CurrentStock,
                    SafetyStock = s.SafetyStock,
                    ShortageQty = s.ShortageQty,
                    SuggestedQty = s.SuggestedQty,
                    SuggestedCompanyId = s.SuggestedCompanyId,
                    SuggestedCompanyName = s.SuggestedCompanyName,
                    SuggestedUnitPrice = s.SuggestedUnitPrice
                });
            }

            RaisePropertyChanged(nameof(SuggestionCount));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"발주 제안 로드 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 개별 발주 생성
    /// </summary>
    private async Task ExecuteCreateOrderAsync(OrderSuggestionRow? row)
    {
        if (row == null || string.IsNullOrEmpty(row.SuggestedCompanyId))
        {
            StatusMessage = "추천 거래처가 없는 품목은 발주를 생성할 수 없습니다.";
            return;
        }

        try
        {
            using var context = CreateDbContext();
            var autoOrderService = new AutoOrderService(context);

            var order = await autoOrderService.CreateOrderFromSuggestionAsync(
                row.ProductId,
                row.SuggestedCompanyId,
                row.SuggestedQty,
                row.SuggestedUnitPrice ?? 0);

            StatusMessage = $"발주 #{order.OrderId} 생성 완료 ({row.ProductName}, {row.SuggestedQty}개)";

            // 제안 목록에서 제거
            OrderSuggestions.Remove(row);
            RaisePropertyChanged(nameof(SuggestionCount));
        }
        catch (Exception ex)
        {
            StatusMessage = $"발주 생성 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"발주 생성 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 전체 발주 일괄 생성
    /// </summary>
    private async Task ExecuteCreateAllOrdersAsync()
    {
        var validSuggestions = OrderSuggestions
            .Where(s => !string.IsNullOrEmpty(s.SuggestedCompanyId))
            .ToList();

        if (!validSuggestions.Any())
        {
            StatusMessage = "생성 가능한 발주 제안이 없습니다.";
            return;
        }

        int created = 0;
        try
        {
            foreach (var row in validSuggestions)
            {
                using var context = CreateDbContext();
                var autoOrderService = new AutoOrderService(context);

                await autoOrderService.CreateOrderFromSuggestionAsync(
                    row.ProductId,
                    row.SuggestedCompanyId!,
                    row.SuggestedQty,
                    row.SuggestedUnitPrice ?? 0);

                created++;
            }

            StatusMessage = $"발주 {created}건 일괄 생성 완료";

            // 새로고침
            await LoadSuggestionsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"일괄 발주 생성 중 오류 ({created}건 생성 후): {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"일괄 발주 생성 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 재고 레코드 삭제
    /// </summary>
    private async Task ExecuteDeleteInventoryAsync(InventoryRow? row)
    {
        if (row == null) return;

        var result = System.Windows.MessageBox.Show(
            $"'{row.ProductName}' 재고 레코드를 삭제하시겠습니까?",
            "재고 삭제 확인",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            using var context = CreateDbContext();
            var inventoryService = new InventoryService(context);

            await inventoryService.DeleteInventoryAsync(row.ProductId);

            InventoryList.Remove(row);
            StatusMessage = $"'{row.ProductName}' 재고 레코드 삭제 완료";
            RaisePropertyChanged(nameof(TotalProductCount));
            RaisePropertyChanged(nameof(NormalStockCount));
            RaisePropertyChanged(nameof(LowStockCount));
            RaisePropertyChanged(nameof(OutOfStockCount));
        }
        catch (Exception ex)
        {
            StatusMessage = $"재고 삭제 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"재고 삭제 실패: {ex.Message}");
        }
    }
}

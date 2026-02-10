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

        AdjustInventoryCommand = new RelayCommand(async () => await ExecuteAdjustInventoryAsync());
        RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
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
        /// 재고 상태 계산
        /// </summary>
        public void ComputeStockStatus()
        {
            if (AvailableQuantity <= 0)
            {
                StockStatus = "품절";
                StockStatusColor = "#C92A2A";
            }
            else if (SafetyStock > 0 && AvailableQuantity <= SafetyStock)
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

    // 재고 요약 프로퍼티 (하단 요약 바인딩)
    public int TotalProductCount => InventoryList.Count;
    public int NormalStockCount => InventoryList.Count(i => i.StockStatus == "정상");
    public int LowStockCount => InventoryList.Count(i => i.StockStatus == "부족");
    public int OutOfStockCount => InventoryList.Count(i => i.StockStatus == "품절");

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
}

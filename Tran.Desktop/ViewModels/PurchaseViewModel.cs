using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;
using Tran.Core.Services;
using Tran.Data;
using Tran.Data.Services;

namespace Tran.Desktop.ViewModels;

/// <summary>
/// 구매(입고) 관리 ViewModel (마스터-디테일 레이아웃)
/// 발주에서 자동 생성된 Purchase를 입고 확인 처리
/// </summary>
public class PurchaseViewModel : ViewModelBase
{
    private string _companyId = string.Empty;
    private Purchase? _selectedPurchase;
    private string _stateFilter = "전체";
    private string _statusMessage = string.Empty;

    public PurchaseViewModel()
    {
        Purchases = new ObservableCollection<Purchase>();
        SelectedPurchaseItems = new ObservableCollection<PurchaseItemRow>();

        ConfirmDeliveryCommand = new AsyncRelayCommand(ExecuteConfirmDeliveryAsync);
        FilterCommand = new RelayCommand(ExecuteFilter);
        RefreshCommand = new RelayCommand(async () => await LoadPurchasesAsync());
    }

    // ═══════════════════════════════════════════════════════════
    // Nested Helper Class
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 입고 품목 행 (수령/불량 수량 편집 가능)
    /// </summary>
    public class PurchaseItemRow : ViewModelBase
    {
        private int _purchaseItemId;
        private string _productName = string.Empty;
        private decimal _orderedQuantity;
        private decimal _receivedQuantity;
        private decimal _defectQuantity;
        private decimal _unitPrice;
        private decimal _lineAmount;

        public int PurchaseItemId
        {
            get => _purchaseItemId;
            set => SetProperty(ref _purchaseItemId, value);
        }

        public string ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        /// <summary>
        /// 발주 수량 (읽기 전용)
        /// </summary>
        public decimal OrderedQuantity
        {
            get => _orderedQuantity;
            set => SetProperty(ref _orderedQuantity, value);
        }

        /// <summary>
        /// 수령 수량 (편집 가능)
        /// </summary>
        public decimal ReceivedQuantity
        {
            get => _receivedQuantity;
            set
            {
                if (SetProperty(ref _receivedQuantity, value))
                {
                    LineAmount = _receivedQuantity * _unitPrice;
                }
            }
        }

        /// <summary>
        /// 불량 수량 (편집 가능)
        /// </summary>
        public decimal DefectQuantity
        {
            get => _defectQuantity;
            set => SetProperty(ref _defectQuantity, value);
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set => SetProperty(ref _unitPrice, value);
        }

        public decimal LineAmount
        {
            get => _lineAmount;
            set => SetProperty(ref _lineAmount, value);
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
    /// 입고 목록 (마스터)
    /// </summary>
    public ObservableCollection<Purchase> Purchases { get; }

    /// <summary>
    /// 선택된 입고 (디테일 표시 트리거)
    /// </summary>
    public Purchase? SelectedPurchase
    {
        get => _selectedPurchase;
        set
        {
            if (SetProperty(ref _selectedPurchase, value))
            {
                _ = LoadPurchaseDetailAsync();
            }
        }
    }

    /// <summary>
    /// 선택된 입고의 품목 (디테일, 편집 가능)
    /// </summary>
    public ObservableCollection<PurchaseItemRow> SelectedPurchaseItems { get; }

    /// <summary>
    /// 상태 필터 ("전체", "입고대기", "부분입고", "입고완료")
    /// </summary>
    public string StateFilter
    {
        get => _stateFilter;
        set
        {
            if (SetProperty(ref _stateFilter, value))
            {
                ExecuteFilter();
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
    /// 입고 확인
    /// </summary>
    public ICommand ConfirmDeliveryCommand { get; }

    /// <summary>
    /// 필터 적용
    /// </summary>
    public ICommand FilterCommand { get; }

    /// <summary>
    /// 새로고침 (F9)
    /// </summary>
    public ICommand RefreshCommand { get; }

    // ═══════════════════════════════════════════════════════════
    // Public Methods
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 데이터 로드 (거래처 설정 후 호출)
    /// </summary>
    public async Task LoadDataAsync(string companyId)
    {
        CompanyId = companyId;
        await LoadPurchasesAsync();
    }

    // ═══════════════════════════════════════════════════════════
    // Private Methods
    // ═══════════════════════════════════════════════════════════

    private TranDbContext CreateDbContext()
    {
        return DbContextFactory.Create();
    }

    /// <summary>
    /// 전체 입고 목록 로드
    /// </summary>
    private List<Purchase> _allPurchases = new();

    private async Task LoadPurchasesAsync()
    {
        try
        {
            using var context = CreateDbContext();

            _allPurchases = await context.Purchases
                .Include(p => p.Items)
                .Where(p => p.CompanyId == CompanyId)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();

            ApplyStateFilter();
        }
        catch (Exception ex)
        {
            StatusMessage = $"입고 목록 로드 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"PurchaseViewModel 로드 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 선택된 입고의 상세 품목 로드
    /// </summary>
    private async Task LoadPurchaseDetailAsync()
    {
        SelectedPurchaseItems.Clear();

        if (SelectedPurchase == null) return;

        try
        {
            using var context = CreateDbContext();

            var purchase = await context.Purchases
                .Include(p => p.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(p => p.PurchaseId == SelectedPurchase.PurchaseId);

            if (purchase == null) return;

            foreach (var item in purchase.Items)
            {
                var row = new PurchaseItemRow
                {
                    PurchaseItemId = item.PurchaseItemId,
                    ProductName = item.ProductName,
                    OrderedQuantity = item.OrderedQuantity,
                    ReceivedQuantity = item.ReceivedQuantity,
                    DefectQuantity = item.DefectQuantity,
                    UnitPrice = item.UnitPrice,
                    LineAmount = item.ReceivedQuantity * item.UnitPrice
                };

                // 입고 대기 상태인 경우 기본값으로 발주 수량 설정
                if (SelectedPurchase.State == PurchaseState.PendingDelivery && item.ReceivedQuantity == 0)
                {
                    row.ReceivedQuantity = item.OrderedQuantity;
                }

                SelectedPurchaseItems.Add(row);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"입고 상세 로드 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"입고 상세 로드 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 입고 확인 실행
    /// </summary>
    private async Task ExecuteConfirmDeliveryAsync()
    {
        if (SelectedPurchase == null)
        {
            StatusMessage = "입고 건을 선택해 주세요.";
            return;
        }

        if (!SelectedPurchaseItems.Any())
        {
            StatusMessage = "입고 품목이 없습니다.";
            return;
        }

        try
        {
            using var context = CreateDbContext();
            var purchaseService = new PurchaseService(context);

            var receivedItems = SelectedPurchaseItems
                .Select(row => (row.PurchaseItemId, row.ReceivedQuantity, row.DefectQuantity))
                .ToList();

            var updatedPurchase = await purchaseService.ConfirmDeliveryAsync(
                SelectedPurchase.PurchaseId,
                receivedItems);

            StatusMessage = $"입고 확인 완료 (#{updatedPurchase.PurchaseId}, 상태: {GetStateDisplayText(updatedPurchase.State)})";

            // 목록 새로고침
            await LoadPurchasesAsync();

            // 다른 탭 갱신 알림
            if (OnDataChanged != null) await OnDataChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = $"입고 확인 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"입고 확인 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 필터 적용
    /// </summary>
    private void ExecuteFilter()
    {
        ApplyStateFilter();
    }

    /// <summary>
    /// 상태별 필터 적용
    /// </summary>
    private void ApplyStateFilter()
    {
        Purchases.Clear();

        var filtered = _allPurchases.AsEnumerable();

        if (StateFilter != "전체")
        {
            var targetState = StateFilter switch
            {
                "입고대기" => PurchaseState.PendingDelivery,
                "부분입고" => PurchaseState.PartiallyDelivered,
                "입고완료" => PurchaseState.Delivered,
                _ => (PurchaseState?)null
            };

            if (targetState.HasValue)
            {
                filtered = filtered.Where(p => p.State == targetState.Value);
            }
        }

        foreach (var purchase in filtered)
        {
            Purchases.Add(purchase);
        }
    }

    /// <summary>
    /// 입고 상태 표시 텍스트
    /// </summary>
    private static string GetStateDisplayText(PurchaseState state) => state switch
    {
        PurchaseState.PendingDelivery => "입고대기",
        PurchaseState.PartiallyDelivered => "부분입고",
        PurchaseState.Delivered => "입고완료",
        PurchaseState.Cancelled => "취소",
        _ => "알 수 없음"
    };
}

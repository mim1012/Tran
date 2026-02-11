using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Tran.Core.Models;
using Tran.Core.Services;

namespace Tran.Desktop.ViewModels;

/// <summary>
/// 메인 워크스페이스 ViewModel
/// 다중 거래처 탭 지원. 각 거래처는 CompanyWorkspace로 관리.
/// 거래처 워크스페이스 탭: 0=발주, 1=견적, 2=구매, 3=판매, 4=재고, 5=통계
/// 품목관리는 상단 독립 탭으로 분리 (글로벌)
/// </summary>
public class MainWorkspaceViewModel : ViewModelBase
{
    private int _selectedTabIndex;
    private bool _isProductManagementMode;
    private ProductManagementViewModel? _globalProductVM;

    public MainWorkspaceViewModel()
    {
        ChangeCompanyCommand = new RelayCommand(ExecuteChangeCompany);
        SwitchTabCommand = new RelayCommand<int>(ExecuteSwitchTab);
        AddCompanyCommand = new RelayCommand(ExecuteAddCompany);
        CloseCompanyCommand = new RelayCommand<CompanyWorkspace>(ExecuteCloseCompany);
        OpenProductManagementCommand = new RelayCommand(ExecuteOpenProductManagement);

        // 거래명세표/정산/양식/거래처 관리 창 열기 Commands
        OpenDocumentManagementCommand = new RelayCommand(ExecuteOpenDocumentManagement);
        OpenPartnerManagementCommand = new RelayCommand(ExecuteOpenPartnerManagement);
        OpenSettlementManagementCommand = new RelayCommand(ExecuteOpenSettlementManagement);
        OpenTemplateManagementCommand = new RelayCommand(ExecuteOpenTemplateManagement);
    }

    // ═══════════════════════════════════════════════════════════
    // CompanyWorkspace Collection
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 열린 거래처 워크스페이스 목록 (상단 탭 바 바인딩)
    /// </summary>
    public ObservableCollection<CompanyWorkspace> CompanyWorkspaces { get; } = new();

    private CompanyWorkspace? _activeWorkspace;

    /// <summary>
    /// 현재 활성 워크스페이스
    /// </summary>
    public CompanyWorkspace? ActiveWorkspace
    {
        get => _activeWorkspace;
        set
        {
            // 이전 워크스페이스 탭 인덱스 저장
            if (_activeWorkspace != null)
                _activeWorkspace.SelectedTabIndex = SelectedTabIndex;

            if (!SetProperty(ref _activeWorkspace, value))
                return;

            // 회사 워크스페이스 선택 시 품목관리 모드 해제
            if (value != null)
                IsProductManagementMode = false;

            // 모든 위임 프로퍼티 갱신 알림
            RaisePropertyChanged(nameof(OrderVM));
            RaisePropertyChanged(nameof(QuotationVM));
            RaisePropertyChanged(nameof(PurchaseVM));
            RaisePropertyChanged(nameof(SaleVM));
            RaisePropertyChanged(nameof(InventoryVM));
            RaisePropertyChanged(nameof(SalesStatisticsVM));
            RaisePropertyChanged(nameof(CompanyName));
            RaisePropertyChanged(nameof(WindowTitle));
            RaisePropertyChanged(nameof(CompanyType));

            // 탭 인덱스 복원
            if (value != null)
                SelectedTabIndex = value.SelectedTabIndex;

            // IsActive 상태 업데이트
            foreach (var ws in CompanyWorkspaces)
                ws.IsActive = ws == value;

            // UserContext 업데이트
            if (value != null)
                UserContext.SetUser(value.Company.CompanyId, value.Company.CompanyName, value.Company.CompanyId);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Delegated Properties (XAML 바인딩 호환 유지)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 선택된 탭 인덱스
    /// </summary>
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    /// <summary>
    /// 윈도우 타이틀
    /// </summary>
    public string WindowTitle => ActiveWorkspace != null
        ? $"Tran - {ActiveWorkspace.Company.CompanyName}"
        : "Tran - 작업 화면";

    /// <summary>
    /// 현재 거래처명 (상단 바 표시용)
    /// </summary>
    public string CompanyName => ActiveWorkspace?.Company.CompanyName ?? "거래처명";

    /// <summary>
    /// 거래처 유형 (상단 바 배지용)
    /// </summary>
    public string CompanyType => "거래처";

    /// <summary>
    /// 오늘 처리 건수 (placeholder)
    /// </summary>
    public int TodayDocumentCount => 0;

    /// <summary>
    /// 상태 메시지 (하단 상태바)
    /// </summary>
    public string StatusMessage => "준비";

    // ═══════════════════════════════════════════════════════════
    // 품목관리 모드 전환
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 품목관리 모드 여부 (true: 품목관리 화면, false: 거래처 워크스페이스)
    /// </summary>
    public bool IsProductManagementMode
    {
        get => _isProductManagementMode;
        set
        {
            if (SetProperty(ref _isProductManagementMode, value))
            {
                RaisePropertyChanged(nameof(IsCompanyWorkspaceMode));

                // 품목관리 모드 진입 시 회사 탭 비활성화
                if (value)
                {
                    foreach (var ws in CompanyWorkspaces)
                        ws.IsActive = false;
                }
                else if (ActiveWorkspace != null)
                {
                    ActiveWorkspace.IsActive = true;
                }
            }
        }
    }

    /// <summary>
    /// 거래처 워크스페이스 모드 여부 (IsProductManagementMode의 반전)
    /// </summary>
    public bool IsCompanyWorkspaceMode => !IsProductManagementMode;

    /// <summary>
    /// 글로벌 품목관리 ViewModel (단일 인스턴스, lazy 초기화)
    /// </summary>
    public ProductManagementViewModel GlobalProductVM
    {
        get
        {
            if (_globalProductVM == null)
            {
                _globalProductVM = new ProductManagementViewModel();
                _ = _globalProductVM.LoadDataAsync();
            }
            return _globalProductVM;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Child ViewModels (ActiveWorkspace 위임)
    // ═══════════════════════════════════════════════════════════

    public OrderViewModel? OrderVM => ActiveWorkspace?.OrderVM;
    public QuotationViewModel? QuotationVM => ActiveWorkspace?.QuotationVM;
    public PurchaseViewModel? PurchaseVM => ActiveWorkspace?.PurchaseVM;
    public SaleViewModel? SaleVM => ActiveWorkspace?.SaleVM;
    public InventoryViewModel? InventoryVM => ActiveWorkspace?.InventoryVM;
    public SalesStatisticsViewModel? SalesStatisticsVM => ActiveWorkspace?.SalesStatisticsVM;

    // ═══════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════

    public ICommand ChangeCompanyCommand { get; }
    public ICommand SwitchTabCommand { get; }
    public ICommand AddCompanyCommand { get; }
    public ICommand CloseCompanyCommand { get; }
    public ICommand OpenProductManagementCommand { get; }
    public ICommand OpenDocumentManagementCommand { get; }
    public ICommand OpenPartnerManagementCommand { get; }
    public ICommand OpenSettlementManagementCommand { get; }
    public ICommand OpenTemplateManagementCommand { get; }

    // ═══════════════════════════════════════════════════════════
    // Events / Actions
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 거래처 변경 요청 시 콜백 (거래처 선택 화면으로 복귀)
    /// </summary>
    public Action? OnChangeCompanyRequested { get; set; }

    /// <summary>
    /// 거래처 추가 요청 시 콜백 (CompanySelectionWindow 다이얼로그 열기)
    /// </summary>
    public Action? OnAddCompanyRequested { get; set; }

    // ═══════════════════════════════════════════════════════════
    // Public Methods
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 거래처 추가 (이미 열려있으면 해당 탭으로 전환)
    /// </summary>
    public async Task AddCompany(Company company)
    {
        // 이미 열려있는 거래처면 해당 탭으로 전환
        var existing = CompanyWorkspaces.FirstOrDefault(w => w.Company.CompanyId == company.CompanyId);
        if (existing != null)
        {
            ActiveWorkspace = existing;
            return;
        }

        var workspace = new CompanyWorkspace(company);

        // 자식 VM 생성 (ProductVM은 글로벌로 이동)
        workspace.OrderVM = new OrderViewModel();
        workspace.QuotationVM = new QuotationViewModel();
        workspace.PurchaseVM = new PurchaseViewModel();
        workspace.SaleVM = new SaleViewModel();
        workspace.InventoryVM = new InventoryViewModel();
        workspace.SalesStatisticsVM = new SalesStatisticsViewModel();

        // Part A: 탭 간 데이터 동기화 콜백 연결
        workspace.OrderVM.OnDataChanged = async () =>
        {
            await workspace.PurchaseVM.LoadDataAsync(company.CompanyId);
            await workspace.InventoryVM.LoadDataAsync();
        };
        workspace.SaleVM.OnDataChanged = async () =>
        {
            await workspace.InventoryVM.LoadDataAsync();
        };
        workspace.PurchaseVM.OnDataChanged = async () =>
        {
            await workspace.InventoryVM.LoadDataAsync();
        };

        // 데이터 로드
        try
        {
            await Task.WhenAll(
                workspace.OrderVM.LoadDataAsync(company.CompanyId),
                workspace.QuotationVM.LoadDataAsync(company.CompanyId),
                workspace.PurchaseVM.LoadDataAsync(company.CompanyId),
                workspace.SaleVM.LoadDataAsync(company.CompanyId),
                workspace.InventoryVM.LoadDataAsync(),
                workspace.SalesStatisticsVM.LoadDataAsync()
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"하위 ViewModel 데이터 로드 실패: {ex.Message}");
        }

        CompanyWorkspaces.Add(workspace);
        ActiveWorkspace = workspace;

        // 기본 탭 선택 (발주)
        SelectedTabIndex = 0;
    }

    // ═══════════════════════════════════════════════════════════
    // Private Methods
    // ═══════════════════════════════════════════════════════════

    private void ExecuteChangeCompany()
    {
        OnChangeCompanyRequested?.Invoke();
    }

    private void ExecuteAddCompany()
    {
        OnAddCompanyRequested?.Invoke();
    }

    private void ExecuteCloseCompany(CompanyWorkspace? workspace)
    {
        if (workspace == null) return;

        var index = CompanyWorkspaces.IndexOf(workspace);
        CompanyWorkspaces.Remove(workspace);

        if (ActiveWorkspace == workspace)
        {
            // 닫힌 탭이 활성 탭이면 인접 탭으로 전환
            if (CompanyWorkspaces.Count > 0)
            {
                var newIndex = Math.Min(index, CompanyWorkspaces.Count - 1);
                ActiveWorkspace = CompanyWorkspaces[newIndex];
            }
            else
            {
                ActiveWorkspace = null;
            }
        }
    }

    private void ExecuteSwitchTab(int index)
    {
        SelectedTabIndex = index;
    }

    private void ExecuteOpenProductManagement()
    {
        IsProductManagementMode = true;
    }

    private void ExecuteOpenDocumentManagement()
    {
        var window = new MainWindow();
        window.Show();
    }

    private void ExecuteOpenPartnerManagement()
    {
        var viewModel = new PartnerManagementViewModel();
        var window = new PartnerManagementWindow(viewModel);
        window.Show();
    }

    private void ExecuteOpenSettlementManagement()
    {
        var viewModel = new SettlementManagementViewModel();
        var window = new SettlementManagementWindow(viewModel);
        window.Show();
    }

    private void ExecuteOpenTemplateManagement()
    {
        var window = new TemplateManagementWindow();
        window.Show();
    }
}

/// <summary>
/// 거래처별 워크스페이스 (VM 인스턴스 + 탭 상태 보관)
/// </summary>
public class CompanyWorkspace : ViewModelBase
{
    public Company Company { get; }
    public string CompanyName => Company.CompanyName;

    public OrderViewModel? OrderVM { get; set; }
    public QuotationViewModel? QuotationVM { get; set; }
    public PurchaseViewModel? PurchaseVM { get; set; }
    public SaleViewModel? SaleVM { get; set; }
    public InventoryViewModel? InventoryVM { get; set; }
    public SalesStatisticsViewModel? SalesStatisticsVM { get; set; }
    public int SelectedTabIndex { get; set; }

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public CompanyWorkspace(Company company)
    {
        Company = company;
    }
}

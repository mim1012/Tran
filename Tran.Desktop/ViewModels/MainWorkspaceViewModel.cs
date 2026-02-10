using System.Windows;
using System.Windows.Input;
using Tran.Core.Models;

namespace Tran.Desktop.ViewModels;

/// <summary>
/// 메인 워크스페이스 ViewModel
/// 탭 기반 화면 구성. 선택된 거래처 컨텍스트를 유지한다.
/// 탭: 0=발주, 1=견적, 2=구매, 3=판매, 4=재고, 5=품목
/// </summary>
public class MainWorkspaceViewModel : ViewModelBase
{
    private Company? _currentCompany;
    private string _currentCompanyDisplay = string.Empty;
    private int _selectedTabIndex;

    public MainWorkspaceViewModel()
    {
        ChangeCompanyCommand = new RelayCommand(ExecuteChangeCompany);
        SwitchTabCommand = new RelayCommand<int>(ExecuteSwitchTab);

        // 거래명세표/정산/양식/거래처 관리 창 열기 Commands
        OpenDocumentManagementCommand = new RelayCommand(ExecuteOpenDocumentManagement);
        OpenPartnerManagementCommand = new RelayCommand(ExecuteOpenPartnerManagement);
        OpenSettlementManagementCommand = new RelayCommand(ExecuteOpenSettlementManagement);
        OpenTemplateManagementCommand = new RelayCommand(ExecuteOpenTemplateManagement);
    }

    // ═══════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 현재 선택된 거래처
    /// </summary>
    public Company? CurrentCompany
    {
        get => _currentCompany;
        private set => SetProperty(ref _currentCompany, value);
    }

    /// <summary>
    /// 거래처 표시 문자열 (예: "A병원 (고객사)")
    /// </summary>
    public string CurrentCompanyDisplay
    {
        get => _currentCompanyDisplay;
        private set => SetProperty(ref _currentCompanyDisplay, value);
    }

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
    public string WindowTitle => CurrentCompany != null
        ? $"Tran - {CurrentCompany.CompanyName}"
        : "Tran - 작업 화면";

    /// <summary>
    /// 현재 거래처명 (상단 바 표시용)
    /// </summary>
    public string CompanyName => CurrentCompany?.CompanyName ?? "거래처명";

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
    // Child ViewModels (각 탭의 ViewModel)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 발주 화면 ViewModel
    /// </summary>
    public OrderViewModel? OrderVM { get; private set; }

    /// <summary>
    /// 견적 화면 ViewModel
    /// </summary>
    public QuotationViewModel? QuotationVM { get; private set; }

    /// <summary>
    /// 구매 관리 ViewModel
    /// </summary>
    public PurchaseViewModel? PurchaseVM { get; private set; }

    /// <summary>
    /// 판매 화면 ViewModel
    /// </summary>
    public SaleViewModel? SaleVM { get; private set; }

    /// <summary>
    /// 재고 관리 ViewModel
    /// </summary>
    public InventoryViewModel? InventoryVM { get; private set; }

    /// <summary>
    /// 품목 관리 ViewModel
    /// </summary>
    public ProductManagementViewModel? ProductVM { get; private set; }

    // ═══════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 거래처 변경 (거래처 선택 화면으로 돌아가기)
    /// </summary>
    public ICommand ChangeCompanyCommand { get; }

    /// <summary>
    /// 탭 전환
    /// </summary>
    public ICommand SwitchTabCommand { get; }

    /// <summary>
    /// 거래명세표 관리 화면 열기
    /// </summary>
    public ICommand OpenDocumentManagementCommand { get; }

    /// <summary>
    /// 거래처 관리 화면 열기
    /// </summary>
    public ICommand OpenPartnerManagementCommand { get; }

    /// <summary>
    /// 정산 관리 화면 열기
    /// </summary>
    public ICommand OpenSettlementManagementCommand { get; }

    /// <summary>
    /// 양식 관리 화면 열기
    /// </summary>
    public ICommand OpenTemplateManagementCommand { get; }

    // ═══════════════════════════════════════════════════════════
    // Events / Actions
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 거래처 변경 요청 시 콜백 (거래처 선택 화면으로 복귀)
    /// </summary>
    public Action? OnChangeCompanyRequested { get; set; }

    // ═══════════════════════════════════════════════════════════
    // Public Methods
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 거래처 설정 및 모든 하위 ViewModel 초기화
    /// </summary>
    public async Task SetCompany(Company company)
    {
        CurrentCompany = company;
        RaisePropertyChanged(nameof(WindowTitle));
        RaisePropertyChanged(nameof(CompanyName));
        RaisePropertyChanged(nameof(CompanyType));
        CurrentCompanyDisplay = $"{company.CompanyName} ({company.BusinessNumber})";

        // 하위 ViewModel 생성
        OrderVM = new OrderViewModel();
        RaisePropertyChanged(nameof(OrderVM));

        QuotationVM = new QuotationViewModel();
        RaisePropertyChanged(nameof(QuotationVM));

        PurchaseVM = new PurchaseViewModel();
        RaisePropertyChanged(nameof(PurchaseVM));

        SaleVM = new SaleViewModel();
        RaisePropertyChanged(nameof(SaleVM));

        InventoryVM = new InventoryViewModel();
        RaisePropertyChanged(nameof(InventoryVM));

        ProductVM = new ProductManagementViewModel();
        RaisePropertyChanged(nameof(ProductVM));

        // 각 ViewModel 데이터 로드
        try
        {
            await Task.WhenAll(
                OrderVM.LoadDataAsync(company.CompanyId),
                QuotationVM.LoadDataAsync(company.CompanyId),
                PurchaseVM.LoadDataAsync(company.CompanyId),
                SaleVM.LoadDataAsync(company.CompanyId),
                InventoryVM.LoadDataAsync(),
                ProductVM.LoadDataAsync()
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"하위 ViewModel 데이터 로드 실패: {ex.Message}");
        }

        // 기본 탭 선택 (발주)
        SelectedTabIndex = 0;
    }

    // ═══════════════════════════════════════════════════════════
    // Private Methods
    // ═══════════════════════════════════════════════════════════

    private void ExecuteChangeCompany()
    {
        CurrentCompany = null;
        RaisePropertyChanged(nameof(WindowTitle));
        RaisePropertyChanged(nameof(CompanyName));
        RaisePropertyChanged(nameof(CompanyType));
        CurrentCompanyDisplay = string.Empty;
        OrderVM = null;
        QuotationVM = null;
        PurchaseVM = null;
        SaleVM = null;
        InventoryVM = null;
        ProductVM = null;

        RaisePropertyChanged(nameof(OrderVM));
        RaisePropertyChanged(nameof(QuotationVM));
        RaisePropertyChanged(nameof(PurchaseVM));
        RaisePropertyChanged(nameof(SaleVM));
        RaisePropertyChanged(nameof(InventoryVM));
        RaisePropertyChanged(nameof(ProductVM));

        OnChangeCompanyRequested?.Invoke();
    }

    private void ExecuteSwitchTab(int index)
    {
        SelectedTabIndex = index;
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

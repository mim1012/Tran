using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;
using Tran.Data;

namespace Tran.Desktop.ViewModels;

/// <summary>
/// 거래처 관리 ViewModel
/// 책임: 거래처 주소록 CRUD + Soft Delete
/// </summary>
public class PartnerManagementViewModel : ViewModelBase
{
    private readonly TranDbContext _context;
    private ObservableCollection<Company> _companies;
    private Company? _selectedCompany;
    private string _searchText = string.Empty;
    private bool _showInactiveCompanies = false;

    public PartnerManagementViewModel(TranDbContext context)
    {
        _context = context;
        _companies = new ObservableCollection<Company>();

        // Commands
        LoadCompaniesCommand = new RelayCommand(async () => await LoadCompaniesAsync());
        AddCompanyCommand = new RelayCommand(AddCompany);
        EditCompanyCommand = new RelayCommand(EditCompany, () => SelectedCompany != null);
        DeactivateCompanyCommand = new RelayCommand(DeactivateCompany, CanDeactivate);
        ReactivateCompanyCommand = new RelayCommand(ReactivateCompany, CanReactivate);
        SearchCommand = new RelayCommand(async () => await SearchCompaniesAsync());

        // 초기 로딩
        Task.Run(async () => await LoadCompaniesAsync());
    }

    // ═══════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════

    public ObservableCollection<Company> Companies
    {
        get => _companies;
        set => SetProperty(ref _companies, value);
    }

    public Company? SelectedCompany
    {
        get => _selectedCompany;
        set
        {
            if (SetProperty(ref _selectedCompany, value))
            {
                RaisePropertyChanged(nameof(CanEdit));
                RaisePropertyChanged(nameof(CanDeactivate));
                RaisePropertyChanged(nameof(CanReactivate));
                RaisePropertyChanged(nameof(StatusBadgeText));
                RaisePropertyChanged(nameof(StatusBadgeBackground));
                RaisePropertyChanged(nameof(StatusBadgeForeground));
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public bool ShowInactiveCompanies
    {
        get => _showInactiveCompanies;
        set
        {
            if (SetProperty(ref _showInactiveCompanies, value))
            {
                Task.Run(async () => await LoadCompaniesAsync());
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // UI 권한 Boolean
    // ═══════════════════════════════════════════════════════════

    public bool CanEdit => SelectedCompany != null;

    private bool CanDeactivate()
    {
        return SelectedCompany != null && SelectedCompany.IsActive;
    }

    private bool CanReactivate()
    {
        return SelectedCompany != null && !SelectedCompany.IsActive;
    }

    // ═══════════════════════════════════════════════════════════
    // UI 상태 표시 (Enterprise B2B 스타일)
    // ═══════════════════════════════════════════════════════════

    public string StatusBadgeText => SelectedCompany?.IsActive == true ? "활성" : "비활성";

    public string StatusBadgeBackground => SelectedCompany?.IsActive == true
        ? "#E6F4EA"  // 연한 초록
        : "#F5F5F5"; // 연한 회색

    public string StatusBadgeForeground => SelectedCompany?.IsActive == true
        ? "#1E7F34"  // 초록
        : "#868E96"; // 회색

    // ═══════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════

    public ICommand LoadCompaniesCommand { get; }
    public ICommand AddCompanyCommand { get; }
    public ICommand EditCompanyCommand { get; }
    public ICommand DeactivateCompanyCommand { get; }
    public ICommand ReactivateCompanyCommand { get; }
    public ICommand SearchCommand { get; }

    // ═══════════════════════════════════════════════════════════
    // Business Logic
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 거래처 목록 로딩
    /// </summary>
    private async Task LoadCompaniesAsync()
    {
        try
        {
            var query = _context.Companies.AsQueryable();

            // 필터: 활성/비활성
            if (!ShowInactiveCompanies)
            {
                query = query.Where(c => c.IsActive);
            }

            var companies = await query
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            Application.Current.Dispatcher.Invoke(() =>
            {
                Companies.Clear();
                foreach (var company in companies)
                {
                    Companies.Add(company);
                }
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"거래처 목록 로딩 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 거래처 검색
    /// </summary>
    private async Task SearchCompaniesAsync()
    {
        try
        {
            var query = _context.Companies.AsQueryable();

            // 필터: 활성/비활성
            if (!ShowInactiveCompanies)
            {
                query = query.Where(c => c.IsActive);
            }

            // 검색어 필터
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                query = query.Where(c =>
                    c.CompanyName.ToLower().Contains(searchLower) ||
                    c.BusinessNumber.Contains(searchLower));
            }

            var companies = await query
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            Application.Current.Dispatcher.Invoke(() =>
            {
                Companies.Clear();
                foreach (var company in companies)
                {
                    Companies.Add(company);
                }
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"검색 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 새 거래처 추가
    /// TODO: PartnerManagementWindow에서 XAML 창으로 구현 필요
    /// </summary>
    private void AddCompany()
    {
        MessageBox.Show(
            "거래처 추가 기능은 PartnerManagementWindow XAML에서 구현하세요.\n\n" +
            "임시로 Company 테이블에 직접 INSERT하거나,\n" +
            "별도 CompanyEditWindow.xaml을 생성하세요.",
            "구현 필요",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    /// <summary>
    /// 선택한 거래처 수정
    /// TODO: PartnerManagementWindow에서 XAML 창으로 구현 필요
    /// </summary>
    private void EditCompany()
    {
        if (SelectedCompany == null) return;

        MessageBox.Show(
            $"'{SelectedCompany.CompanyName}' 수정 기능은 구현 예정입니다.\n\n" +
            "PartnerManagementWindow XAML에서 편집 UI를 추가하세요.",
            "구현 필요",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    /// <summary>
    /// 거래처 비활성화 (Soft Delete)
    /// </summary>
    private void DeactivateCompany()
    {
        if (SelectedCompany == null || !SelectedCompany.IsActive) return;

        var result = MessageBox.Show(
            $"'{SelectedCompany.CompanyName}'을(를) 비활성화하시겠습니까?\n\n" +
            "비활성화된 거래처는 새 문서 작성 시 선택할 수 없습니다.\n" +
            "기존 문서와의 관계는 유지됩니다.",
            "거래처 비활성화",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            SelectedCompany.IsActive = false;
            SelectedCompany.Status = CompanyStatus.Inactive;
            _context.SaveChanges();

            Task.Run(async () => await LoadCompaniesAsync());

            MessageBox.Show("거래처가 비활성화되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// 거래처 재활성화
    /// </summary>
    private void ReactivateCompany()
    {
        if (SelectedCompany == null || SelectedCompany.IsActive) return;

        var result = MessageBox.Show(
            $"'{SelectedCompany.CompanyName}'을(를) 다시 활성화하시겠습니까?",
            "거래처 활성화",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            SelectedCompany.IsActive = true;
            SelectedCompany.Status = CompanyStatus.Active;
            _context.SaveChanges();

            Task.Run(async () => await LoadCompaniesAsync());

            MessageBox.Show("거래처가 활성화되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

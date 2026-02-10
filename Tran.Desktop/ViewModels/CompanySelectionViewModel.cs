using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;
using Tran.Data;

namespace Tran.Desktop.ViewModels;

/// <summary>
/// 거래처 선택 화면 ViewModel
/// 앱 진입 시 거래처를 선택하는 화면
/// </summary>
public class CompanySelectionViewModel : ViewModelBase
{
    private string _searchText = string.Empty;
    private string _selectedType = "전체";
    private Company? _selectedCompany;

    public CompanySelectionViewModel()
    {
        Companies = new ObservableCollection<Company>();
        FilteredCompanies = new ObservableCollection<Company>();
        RecentCompanies = new ObservableCollection<Company>();

        SelectCompanyCommand = new RelayCommand<Company>(ExecuteSelectCompany);
        SearchCommand = new RelayCommand(ExecuteSearch);
        FocusSearchCommand = new RelayCommand(() => { /* UI에서 Focus 처리 */ });
        AddNewCompanyCommand = new RelayCommand(() => { /* 거래처 추가 - 향후 구현 */ });

        CompanyTypes = new ObservableCollection<string> { "전체", "고객사", "공급사" };

        _ = LoadDataAsync();
    }

    // ═══════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 전체 거래처 목록
    /// </summary>
    public ObservableCollection<Company> Companies { get; }

    /// <summary>
    /// 필터링된 거래처 목록 (검색 + 유형 필터 적용)
    /// </summary>
    public ObservableCollection<Company> FilteredCompanies { get; }

    /// <summary>
    /// 최근 거래 거래처 (상위 5개)
    /// </summary>
    public ObservableCollection<Company> RecentCompanies { get; }

    /// <summary>
    /// 검색어 (실시간 필터링)
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    /// <summary>
    /// 선택된 유형 필터 ("전체", "고객사", "공급사")
    /// </summary>
    public string SelectedType
    {
        get => _selectedType;
        set
        {
            if (SetProperty(ref _selectedType, value))
            {
                ApplyFilter();
            }
        }
    }

    /// <summary>
    /// 선택된 거래처 (결과)
    /// </summary>
    public Company? SelectedCompany
    {
        get => _selectedCompany;
        set => SetProperty(ref _selectedCompany, value);
    }

    // ═══════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 거래처 선택 명령
    /// </summary>
    public ICommand SelectCompanyCommand { get; }

    /// <summary>
    /// 검색 명령
    /// </summary>
    public ICommand SearchCommand { get; }

    /// <summary>
    /// 검색 포커스 명령 (Ctrl+F)
    /// </summary>
    public ICommand FocusSearchCommand { get; }

    /// <summary>
    /// 거래처 추가 명령
    /// </summary>
    public ICommand AddNewCompanyCommand { get; }

    /// <summary>
    /// 거래처 유형 목록 (필터용)
    /// </summary>
    public ObservableCollection<string> CompanyTypes { get; }

    /// <summary>
    /// 최근 거래처 존재 여부
    /// </summary>
    public bool HasRecentCompanies => RecentCompanies.Count > 0;

    /// <summary>
    /// 필터 결과 비어있는지 여부
    /// </summary>
    public bool IsEmpty => FilteredCompanies.Count == 0;

    /// <summary>
    /// 전체 거래처 수
    /// </summary>
    public int TotalCompanyCount => Companies.Count;

    // ═══════════════════════════════════════════════════════════
    // Events / Actions
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 거래처 선택 완료 시 콜백 (부모 윈도우에 통지)
    /// </summary>
    public Action<Company>? OnCompanySelected { get; set; }

    // ═══════════════════════════════════════════════════════════
    // Private Methods
    // ═══════════════════════════════════════════════════════════

    private TranDbContext CreateDbContext()
    {
        return DbContextFactory.Create();
    }

    /// <summary>
    /// 초기 데이터 로드
    /// </summary>
    private async Task LoadDataAsync()
    {
        try
        {
            using var context = CreateDbContext();
            var companies = await context.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            Companies.Clear();
            foreach (var company in companies)
            {
                Companies.Add(company);
            }

            // 최근 거래 거래처: CreatedAt 기준 최근 5개
            var recentCompanies = companies
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToList();

            RecentCompanies.Clear();
            foreach (var company in recentCompanies)
            {
                RecentCompanies.Add(company);
            }

            ApplyFilter();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"거래처 로드 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 필터 적용 (검색어 + 유형)
    /// </summary>
    private void ApplyFilter()
    {
        FilteredCompanies.Clear();

        var filtered = Companies.AsEnumerable();

        // 검색어 필터
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var keyword = SearchText.Trim().ToLower();
            filtered = filtered.Where(c =>
                c.CompanyName.ToLower().Contains(keyword) ||
                c.BusinessNumber.Contains(keyword) ||
                (c.Representative?.ToLower().Contains(keyword) ?? false) ||
                (c.ContactName?.ToLower().Contains(keyword) ?? false));
        }

        // 유형 필터 (현재 Company 모델에 유형 구분 없으므로 전체 표시)
        // 향후 CompanyType 필드 추가 시 필터링 가능
        // if (SelectedType != "전체") { ... }

        foreach (var company in filtered)
        {
            FilteredCompanies.Add(company);
        }

        RaisePropertyChanged(nameof(IsEmpty));
        RaisePropertyChanged(nameof(TotalCompanyCount));
        RaisePropertyChanged(nameof(HasRecentCompanies));
    }

    /// <summary>
    /// 거래처 선택 실행
    /// </summary>
    private void ExecuteSelectCompany(Company? company)
    {
        if (company == null) return;

        SelectedCompany = company;
        OnCompanySelected?.Invoke(company);
    }

    /// <summary>
    /// 검색 실행
    /// </summary>
    private void ExecuteSearch()
    {
        ApplyFilter();
    }
}

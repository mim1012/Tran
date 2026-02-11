using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;
using Tran.Core.Services;
using Tran.Data;
using Tran.Data.Services;

namespace Tran.Desktop.ViewModels;

/// <summary>
/// 품목 관리 ViewModel (CRUD 테이블)
/// 글로벌 품목관리: 전체 회사의 품목을 한 화면에서 관리
/// </summary>
public class ProductManagementViewModel : ViewModelBase
{
    private ProductDisplayItem? _selectedProduct;
    private string _searchText = string.Empty;
    private bool _isEditing;
    private Product? _editingProduct;
    private string _statusMessage = string.Empty;
    private bool _sortAscending = true;
    private Company? _selectedCompanyFilter;

    public ProductManagementViewModel()
    {
        Products = new ObservableCollection<ProductDisplayItem>();
        FilterCompanies = new ObservableCollection<Company>();
        Categories = new ObservableCollection<string>
        {
            "식품", "음료", "과일", "채소", "수산물", "축산물", "유제품", "냉동식품", "조미료", "기타"
        };
        Units = new ObservableCollection<string>
        {
            "개", "박스", "팩", "kg", "g", "L", "mL", "병", "캔", "봉", "포", "세트"
        };

        AddProductCommand = new RelayCommand(ExecuteAddProduct);
        EditProductCommand = new RelayCommand(ExecuteEditProduct, () => SelectedProduct != null);
        SaveProductCommand = new RelayCommand(async () => await ExecuteSaveProductAsync(), () => IsEditing);
        CancelEditCommand = new RelayCommand(ExecuteCancelEdit);
        SearchCommand = new RelayCommand(async () => await ApplyFilterAsync());
        DeleteProductCommand = new RelayCommand(async () => await ExecuteDeleteProductAsync(), () => SelectedProduct != null);
        SortProductsCommand = new RelayCommand(ExecuteSortProducts);
        ClearFilterCommand = new RelayCommand(async () =>
        {
            SelectedCompanyFilter = null;
            SearchText = string.Empty;
            await ApplyFilterAsync();
        });
    }

    // ═══════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 품목 목록 (회사명 포함)
    /// </summary>
    public ObservableCollection<ProductDisplayItem> Products { get; }

    /// <summary>
    /// 필터용 회사 목록
    /// </summary>
    public ObservableCollection<Company> FilterCompanies { get; }

    /// <summary>
    /// 선택된 회사 필터 (null = 전체)
    /// </summary>
    public Company? SelectedCompanyFilter
    {
        get => _selectedCompanyFilter;
        set
        {
            if (SetProperty(ref _selectedCompanyFilter, value))
            {
                _ = ApplyFilterAsync();
            }
        }
    }

    /// <summary>
    /// 선택된 품목
    /// </summary>
    public ProductDisplayItem? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    /// 검색어
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    /// <summary>
    /// 편집 모드 여부
    /// </summary>
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (SetProperty(ref _isEditing, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    /// 편집 중인 품목 (추가/수정 폼) - Product 타입 유지
    /// </summary>
    public Product? EditingProduct
    {
        get => _editingProduct;
        set
        {
            if (SetProperty(ref _editingProduct, value))
            {
                RaisePropertyChanged(nameof(IsNewProduct));
            }
        }
    }

    /// <summary>
    /// 신규 품목 여부 (품목코드 편집 가능 여부 결정)
    /// </summary>
    public bool IsNewProduct => EditingProduct?.ProductId == 0;

    /// <summary>
    /// 카테고리 프리셋 목록
    /// </summary>
    public ObservableCollection<string> Categories { get; }

    /// <summary>
    /// 단위 프리셋 목록
    /// </summary>
    public ObservableCollection<string> Units { get; }

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

    public ICommand AddProductCommand { get; }
    public ICommand EditProductCommand { get; }
    public ICommand SaveProductCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand DeleteProductCommand { get; }
    public ICommand SortProductsCommand { get; }
    public ICommand ClearFilterCommand { get; }

    // ═══════════════════════════════════════════════════════════
    // Public Methods
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 전체 품목 + 회사 목록 로드
    /// </summary>
    public async Task LoadDataAsync()
    {
        try
        {
            using var context = CreateDbContext();
            var productService = new ProductService(context);

            // 회사 목록 로드 (필터용)
            var companies = await context.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            FilterCompanies.Clear();
            foreach (var company in companies)
            {
                FilterCompanies.Add(company);
            }

            // 품목 로드 (회사명 포함)
            var products = await productService.GetAllProductsWithCompaniesAsync(includeInactive: false);

            Products.Clear();
            foreach (var product in products)
            {
                Products.Add(product);
            }

            StatusMessage = $"품목 {Products.Count}건 로드 완료";
        }
        catch (Exception ex)
        {
            StatusMessage = $"품목 로드 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"ProductManagementViewModel 로드 실패: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Private Methods
    // ═══════════════════════════════════════════════════════════

    private TranDbContext CreateDbContext()
    {
        return DbContextFactory.Create();
    }

    private void ExecuteAddProduct()
    {
        EditingProduct = new Product
        {
            ProductName = string.Empty,
            Unit = "개",
            DefaultPrice = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        IsEditing = true;
    }

    private void ExecuteEditProduct()
    {
        if (SelectedProduct == null) return;

        EditingProduct = SelectedProduct.ToProduct();
        IsEditing = true;
    }

    private async Task ExecuteSaveProductAsync()
    {
        if (EditingProduct == null) return;

        if (string.IsNullOrWhiteSpace(EditingProduct.ProductName))
        {
            StatusMessage = "품목명을 입력해 주세요.";
            return;
        }

        try
        {
            using var context = CreateDbContext();
            var productService = new ProductService(context);

            if (EditingProduct.ProductId == 0)
            {
                var created = await productService.CreateAsync(EditingProduct);
                var displayItem = ProductDisplayItem.FromProduct(created, "", new List<string>());
                Products.Add(displayItem);
                StatusMessage = $"품목 '{created.ProductName}' 등록 완료";
            }
            else
            {
                await productService.UpdateAsync(EditingProduct);

                // 목록에서 해당 품목 갱신 (회사명 유지)
                var index = -1;
                for (int i = 0; i < Products.Count; i++)
                {
                    if (Products[i].ProductId == EditingProduct.ProductId)
                    {
                        index = i;
                        break;
                    }
                }

                if (index >= 0)
                {
                    var existing = Products[index];
                    var updated = ProductDisplayItem.FromProduct(
                        EditingProduct, existing.CompanyNames, existing.CompanyIds);
                    Products[index] = updated;
                }

                StatusMessage = $"품목 '{EditingProduct.ProductName}' 수정 완료";
            }

            IsEditing = false;
            EditingProduct = null;
        }
        catch (Exception ex)
        {
            StatusMessage = $"품목 저장 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"품목 저장 실패: {ex.Message}");
        }
    }

    private void ExecuteCancelEdit()
    {
        IsEditing = false;
        EditingProduct = null;
    }

    /// <summary>
    /// 검색어 + 회사 필터 조합 적용
    /// </summary>
    private async Task ApplyFilterAsync()
    {
        try
        {
            using var context = CreateDbContext();
            var productService = new ProductService(context);

            List<ProductDisplayItem> results;
            var hasKeyword = !string.IsNullOrWhiteSpace(SearchText);
            var hasCompanyFilter = SelectedCompanyFilter != null;

            if (hasKeyword || hasCompanyFilter)
            {
                results = await productService.SearchProductsWithCompaniesAsync(
                    SearchText?.Trim() ?? "",
                    SelectedCompanyFilter?.CompanyId);
            }
            else
            {
                results = await productService.GetAllProductsWithCompaniesAsync(includeInactive: false);
            }

            Products.Clear();
            foreach (var product in results)
            {
                Products.Add(product);
            }

            StatusMessage = $"검색 결과: {Products.Count}건";
        }
        catch (Exception ex)
        {
            StatusMessage = $"검색 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"품목 검색 실패: {ex.Message}");
        }
    }

    private async Task ExecuteDeleteProductAsync()
    {
        if (SelectedProduct == null) return;

        var result = System.Windows.MessageBox.Show(
            $"'{SelectedProduct.ProductName}' 품목을 삭제(비활성화)하시겠습니까?",
            "품목 삭제 확인",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            using var context = CreateDbContext();

            var product = await context.Products.FindAsync(SelectedProduct.ProductId);
            if (product != null)
            {
                product.IsActive = false;
                await context.SaveChangesAsync();
            }

            var deletedName = SelectedProduct.ProductName;
            Products.Remove(SelectedProduct);
            SelectedProduct = null;
            StatusMessage = $"품목 '{deletedName}' 삭제(비활성화) 완료";
        }
        catch (Exception ex)
        {
            StatusMessage = $"품목 삭제 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"품목 삭제 실패: {ex.Message}");
        }
    }

    private void ExecuteSortProducts()
    {
        var comparer = StringComparer.Create(new System.Globalization.CultureInfo("ko-KR"), false);
        var sorted = _sortAscending
            ? Products.OrderBy(p => p.ProductName, comparer).ToList()
            : Products.OrderByDescending(p => p.ProductName, comparer).ToList();

        Products.Clear();
        foreach (var product in sorted)
        {
            Products.Add(product);
        }

        StatusMessage = _sortAscending ? "ㄱ→ㅎ 순 정렬 완료" : "ㅎ→ㄱ 순 정렬 완료";
        _sortAscending = !_sortAscending;
    }
}

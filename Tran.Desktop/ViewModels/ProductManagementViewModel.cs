using System.Collections.ObjectModel;
using System.Windows.Input;
using Tran.Core.Models;
using Tran.Core.Services;
using Tran.Data;
using Tran.Data.Services;

namespace Tran.Desktop.ViewModels;

/// <summary>
/// 품목 관리 ViewModel (CRUD 테이블)
/// 품목 등록, 수정, 검색
/// </summary>
public class ProductManagementViewModel : ViewModelBase
{
    private Product? _selectedProduct;
    private string _searchText = string.Empty;
    private bool _isEditing;
    private Product? _editingProduct;
    private string _statusMessage = string.Empty;

    public ProductManagementViewModel()
    {
        Products = new ObservableCollection<Product>();

        AddProductCommand = new RelayCommand(ExecuteAddProduct);
        EditProductCommand = new RelayCommand(ExecuteEditProduct, () => SelectedProduct != null);
        SaveProductCommand = new RelayCommand(async () => await ExecuteSaveProductAsync(), () => IsEditing);
        CancelEditCommand = new RelayCommand(ExecuteCancelEdit);
        SearchCommand = new RelayCommand(async () => await ExecuteSearchAsync());
    }

    // ═══════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 품목 목록
    /// </summary>
    public ObservableCollection<Product> Products { get; }

    /// <summary>
    /// 선택된 품목
    /// </summary>
    public Product? SelectedProduct
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
    /// 편집 중인 품목 (추가/수정 폼)
    /// </summary>
    public Product? EditingProduct
    {
        get => _editingProduct;
        set => SetProperty(ref _editingProduct, value);
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
    /// 새 품목 추가
    /// </summary>
    public ICommand AddProductCommand { get; }

    /// <summary>
    /// 품목 수정
    /// </summary>
    public ICommand EditProductCommand { get; }

    /// <summary>
    /// 품목 저장 (생성/수정)
    /// </summary>
    public ICommand SaveProductCommand { get; }

    /// <summary>
    /// 편집 취소
    /// </summary>
    public ICommand CancelEditCommand { get; }

    /// <summary>
    /// 검색
    /// </summary>
    public ICommand SearchCommand { get; }

    // ═══════════════════════════════════════════════════════════
    // Public Methods
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 전체 품목 로드
    /// </summary>
    public async Task LoadDataAsync()
    {
        try
        {
            using var context = CreateDbContext();
            var productService = new ProductService(context);

            var products = await productService.GetAllProductsAsync(includeInactive: false);

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

    /// <summary>
    /// 새 품목 추가 모드
    /// </summary>
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

    /// <summary>
    /// 기존 품목 수정 모드
    /// </summary>
    private void ExecuteEditProduct()
    {
        if (SelectedProduct == null) return;

        // 복사본 생성 (편집용)
        EditingProduct = new Product
        {
            ProductId = SelectedProduct.ProductId,
            ProductName = SelectedProduct.ProductName,
            ProductCode = SelectedProduct.ProductCode,
            Category = SelectedProduct.Category,
            Unit = SelectedProduct.Unit,
            DefaultPrice = SelectedProduct.DefaultPrice,
            IsActive = SelectedProduct.IsActive,
            CreatedAt = SelectedProduct.CreatedAt
        };
        IsEditing = true;
    }

    /// <summary>
    /// 품목 저장 (생성 또는 수정)
    /// </summary>
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
                // 새 품목 생성
                var created = await productService.CreateAsync(EditingProduct);
                Products.Add(created);
                StatusMessage = $"품목 '{created.ProductName}' 등록 완료";
            }
            else
            {
                // 기존 품목 수정
                await productService.UpdateAsync(EditingProduct);

                // 목록에서 해당 품목 갱신
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
                    Products[index] = EditingProduct;
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

    /// <summary>
    /// 편집 취소
    /// </summary>
    private void ExecuteCancelEdit()
    {
        IsEditing = false;
        EditingProduct = null;
    }

    /// <summary>
    /// 검색 실행
    /// </summary>
    private async Task ExecuteSearchAsync()
    {
        try
        {
            using var context = CreateDbContext();
            var productService = new ProductService(context);

            List<Product> results;

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                results = await productService.GetAllProductsAsync(includeInactive: false);
            }
            else
            {
                results = await productService.SearchProductsAsync(SearchText.Trim());
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
}

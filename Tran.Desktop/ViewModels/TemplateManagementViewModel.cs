using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;
using Tran.Data;

namespace Tran.Desktop.ViewModels;

/// <summary>
/// 양식 관리 ViewModel (완전한 읽기 전용)
/// - ICommand 없음 (조회용 메서드만 존재)
/// - 편집/저장 기능 절대 금지
/// - "관찰 전용 레이어"
/// </summary>
public class TemplateManagementViewModel : ViewModelBase
{
    private readonly TranDbContext _dbContext;

    // 템플릿 목록
    private ObservableCollection<TemplateDisplayItem> _templates = new();
    public ObservableCollection<TemplateDisplayItem> Templates
    {
        get => _templates;
        set => SetProperty(ref _templates, value);
    }

    // 거래처 목록 (필터용)
    private ObservableCollection<CompanyFilterItem> _companies = new();
    public ObservableCollection<CompanyFilterItem> Companies
    {
        get => _companies;
        set => SetProperty(ref _companies, value);
    }

    // 선택된 템플릿
    private TemplateDisplayItem? _selectedTemplate;
    public TemplateDisplayItem? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            if (SetProperty(ref _selectedTemplate, value))
            {
                LoadTemplatePreview();
            }
        }
    }

    // 필터: 활성/비활성
    private bool _showActiveOnly = true;
    public bool ShowActiveOnly
    {
        get => _showActiveOnly;
        set
        {
            if (SetProperty(ref _showActiveOnly, value))
            {
                _ = LoadTemplatesAsync();
            }
        }
    }

    // 필터: 거래처
    private string? _selectedCompanyId;
    public string? SelectedCompanyId
    {
        get => _selectedCompanyId;
        set
        {
            if (SetProperty(ref _selectedCompanyId, value))
            {
                _ = LoadTemplatesAsync();
            }
        }
    }

    // JSON 미리보기 (포맷팅됨)
    private string _schemaJsonPreview = string.Empty;
    public string SchemaJsonPreview
    {
        get => _schemaJsonPreview;
        set => SetProperty(ref _schemaJsonPreview, value);
    }

    private string _layoutJsonPreview = string.Empty;
    public string LayoutJsonPreview
    {
        get => _layoutJsonPreview;
        set => SetProperty(ref _layoutJsonPreview, value);
    }

    // 로딩 상태
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public TemplateManagementViewModel(TranDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 초기 로드 (템플릿 목록 + 거래처 목록)
    /// </summary>
    public async Task InitializeAsync()
    {
        await LoadCompaniesAsync();
        await LoadTemplatesAsync();
    }

    /// <summary>
    /// 거래처 목록 로드 (필터용)
    /// </summary>
    private async Task LoadCompaniesAsync()
    {
        try
        {
            var companies = await _dbContext.Companies
                .Where(c => c.Status == CompanyStatus.Active)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            Companies.Clear();
            Companies.Add(new CompanyFilterItem { CompanyId = null, CompanyName = "전체" });

            foreach (var company in companies)
            {
                Companies.Add(new CompanyFilterItem
                {
                    CompanyId = company.CompanyId,
                    CompanyName = company.CompanyName
                });
            }
        }
        catch (Exception ex)
        {
            SchemaJsonPreview = $"거래처 목록 로드 실패: {ex.Message}";
        }
    }

    /// <summary>
    /// 템플릿 목록 로드 (필터 적용)
    /// </summary>
    public async Task LoadTemplatesAsync()
    {
        IsLoading = true;
        try
        {
            var query = _dbContext.DocumentTemplates
                .Include(t => t.Company)
                .AsQueryable();

            // 필터: 활성/비활성
            if (ShowActiveOnly)
            {
                query = query.Where(t => t.IsActive);
            }

            // 필터: 거래처
            if (!string.IsNullOrEmpty(SelectedCompanyId))
            {
                query = query.Where(t => t.CompanyId == SelectedCompanyId);
            }

            var templates = await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            Templates.Clear();
            foreach (var template in templates)
            {
                Templates.Add(new TemplateDisplayItem
                {
                    TemplateId = template.TemplateId,
                    TemplateName = template.TemplateName,
                    CompanyName = template.Company?.CompanyName ?? "알 수 없음",
                    TemplateTypeDisplay = template.TemplateType == TemplateType.Statement
                        ? "거래명세표"
                        : "정산서",
                    IsActive = template.IsActive,
                    ActiveStatusDisplay = template.IsActive ? "활성" : "비활성",
                    CreatedAt = template.CreatedAt,
                    SchemaJson = template.SchemaJson,
                    LayoutJson = template.LayoutJson
                });
            }
        }
        catch (Exception ex)
        {
            SchemaJsonPreview = $"템플릿 로드 실패: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 선택된 템플릿의 JSON 미리보기 생성
    /// </summary>
    private void LoadTemplatePreview()
    {
        if (SelectedTemplate == null)
        {
            SchemaJsonPreview = string.Empty;
            LayoutJsonPreview = string.Empty;
            return;
        }

        // SchemaJson 포맷팅
        try
        {
            var schemaObj = JsonSerializer.Deserialize<object>(SelectedTemplate.SchemaJson);
            SchemaJsonPreview = JsonSerializer.Serialize(schemaObj, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
        catch (JsonException ex)
        {
            SchemaJsonPreview = $"JSON 파싱 오류:\n{ex.Message}\n\n원본:\n{SelectedTemplate.SchemaJson}";
        }

        // LayoutJson 포맷팅
        try
        {
            var layoutObj = JsonSerializer.Deserialize<object>(SelectedTemplate.LayoutJson);
            LayoutJsonPreview = JsonSerializer.Serialize(layoutObj, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
        catch (JsonException ex)
        {
            LayoutJsonPreview = $"JSON 파싱 오류:\n{ex.Message}\n\n원본:\n{SelectedTemplate.LayoutJson}";
        }
    }
}

/// <summary>
/// 템플릿 표시용 아이템 (DataGrid 바인딩용)
/// </summary>
public class TemplateDisplayItem
{
    public string TemplateId { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string TemplateTypeDisplay { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string ActiveStatusDisplay { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string SchemaJson { get; set; } = string.Empty;
    public string LayoutJson { get; set; } = string.Empty;

    // UI 색상 (활성/비활성 Pill)
    public string StatusBackground => IsActive ? "#E6F4EA" : "#F5F5F5";
    public string StatusForeground => IsActive ? "#1E7F34" : "#868E96";
}

/// <summary>
/// 거래처 필터 아이템
/// </summary>
public class CompanyFilterItem
{
    public string? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}

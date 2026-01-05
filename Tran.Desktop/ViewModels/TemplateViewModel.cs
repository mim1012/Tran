using Tran.Core.Models;

namespace Tran.Desktop.ViewModels;

/// <summary>
/// Document Template ViewModel
/// </summary>
public class TemplateViewModel : ViewModelBase
{
    private DocumentTemplate _template;

    public TemplateViewModel(DocumentTemplate template)
    {
        _template = template;
    }

    public string TemplateId => _template.TemplateId;
    public string TemplateName => _template.TemplateName;
    public string CompanyId => _template.CompanyId;

    public string TemplateTypeDisplayText => _template.TemplateType switch
    {
        TemplateType.Statement => "거래명세표",
        TemplateType.Settlement => "정산서",
        _ => "알 수 없음"
    };

    public string StatusDisplayText => _template.IsActive ? "활성" : "비활성";

    public string StatusBadgeColor => _template.IsActive ? "#E6F4EA" : "#F5F5F5";

    public string StatusBadgeForeground => _template.IsActive ? "#1E7F34" : "#868E96";

    public DateTime CreatedAt => _template.CreatedAt;
}

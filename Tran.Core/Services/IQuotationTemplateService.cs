using Tran.Core.Models;

namespace Tran.Core.Services;

/// <summary>
/// 견적서 양식 관리 서비스 인터페이스
/// </summary>
public interface IQuotationTemplateService
{
    Task<QuotationTemplate> UploadAsync(QuotationTemplate template);
    Task<List<QuotationTemplate>> GetByCompanyAsync(string companyId);
    Task<QuotationTemplate?> GetByIdAsync(int templateId);
    Task DeleteAsync(int templateId);
}

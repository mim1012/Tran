using Tran.Core.Models;

namespace Tran.Core.Services;

/// <summary>
/// 견적 관리 서비스 인터페이스
/// 견적 CRUD 및 상태 전이 (Draft → Sent → Confirmed → 단가/거래처 품목 업데이트)
/// </summary>
public interface IQuotationService
{
    /// <summary>
    /// 거래처별 견적 목록 조회
    /// </summary>
    Task<List<Quotation>> GetQuotationsByCompanyAsync(string companyId);

    /// <summary>
    /// 견적 ID로 조회 (Items 포함)
    /// </summary>
    Task<Quotation?> GetByIdAsync(int quotationId);

    /// <summary>
    /// 견적 초안 생성 (Draft 상태)
    /// </summary>
    Task<Quotation> CreateDraftAsync(Quotation quotation);

    /// <summary>
    /// 견적 전송 (Draft → Sent)
    /// </summary>
    Task<Quotation> SendQuotationAsync(int quotationId);

    /// <summary>
    /// 견적 확정 → CompanyPrice 업데이트 + CompanyProduct 등록/갱신
    /// </summary>
    Task<Quotation> ConfirmQuotationAsync(int quotationId);
}

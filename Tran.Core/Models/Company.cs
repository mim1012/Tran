using System.ComponentModel.DataAnnotations.Schema;

namespace Tran.Core.Models;

/// <summary>
/// 거래처 / 회사 엔티티
/// 문서의 from_company_id, to_company_id 기준이 됨
/// </summary>
public class Company
{
    public string CompanyId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// 사업자번호 (필수, 중복 체크 필요)
    /// </summary>
    public string BusinessNumber { get; set; } = string.Empty;

    /// <summary>
    /// 대표자명 (선택)
    /// </summary>
    public string? Representative { get; set; }

    /// <summary>
    /// 연락처 (통합 - 전화번호 또는 이메일)
    /// </summary>
    public string? Contact { get; set; }

    /// <summary>
    /// 주소 (선택)
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// 담당자명 (기존 ContactName 유지 - 호환성)
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// 담당자 이메일 (기존 유지 - 호환성)
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// 담당자 전화번호 (기존 유지 - 호환성)
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// 활성 상태 (true: 활성, false: 비활성)
    /// Soft Delete 방식: IsActive=false로 삭제 처리
    /// 단일 진실 소스 (Single Source of Truth)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 상태 - IsActive에서 파생 (DB에 매핑하지 않음)
    /// setter에서 IsActive를 동기화
    /// </summary>
    [NotMapped]
    public CompanyStatus Status
    {
        get => IsActive ? CompanyStatus.Active : CompanyStatus.Inactive;
        set => IsActive = (value == CompanyStatus.Active);
    }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum CompanyStatus
{
    Active,
    Inactive
}

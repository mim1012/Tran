namespace Tran.Core.Models;

/// <summary>
/// 사용자 엔티티 (로컬 기준)
/// MVP에서는 단일 사용자여도 테이블 유지 (로그·책임 추적용)
/// </summary>
public class User
{
    public string UserId { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Staff;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum UserRole
{
    Admin,
    Staff
}

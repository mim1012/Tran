namespace Tran.Web.Services;

/// <summary>
/// 앱 전역 상태 관리 서비스 (Scoped - 사용자 세션별)
/// WPF의 UserContext 정적 클래스를 대체
/// </summary>
public class AppStateService
{
    public string CurrentUserId { get; set; } = "USER001";
    public string CurrentUserName { get; set; } = "관리자";
    public string CurrentCompanyId { get; set; } = "COMP001";
    public bool IsInitialized { get; set; } = true;

    public string? SelectedCompanyId { get; set; }
    public string? SelectedCompanyName { get; set; }

    public event Action? OnStateChanged;

    public void NotifyStateChanged() => OnStateChanged?.Invoke();

    public void SetSelectedCompany(string companyId, string companyName)
    {
        SelectedCompanyId = companyId;
        SelectedCompanyName = companyName;
        NotifyStateChanged();
    }
}

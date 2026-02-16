namespace Tran.Web.Services;

/// <summary>
/// 앱 전역 상태 관리 서비스 (Scoped - 사용자 세션별)
/// WPF의 UserContext 정적 클래스를 대체
/// </summary>
public class AppStateService
{
    public string CurrentUserId { get; private set; } = "";
    public string CurrentUserName { get; private set; } = "";
    public string CurrentCompanyId { get; private set; } = "";
    public bool IsInitialized { get; private set; }

    public string? SelectedCompanyId { get; set; }
    public string? SelectedCompanyName { get; set; }

    public event Action? OnStateChanged;

    public void SetUser(string userId, string userName, string companyId)
    {
        CurrentUserId = userId;
        CurrentUserName = userName;
        CurrentCompanyId = companyId;
        IsInitialized = true;
        NotifyStateChanged();
    }

    public void Clear()
    {
        CurrentUserId = "";
        CurrentUserName = "";
        CurrentCompanyId = "";
        IsInitialized = false;
        SelectedCompanyId = null;
        SelectedCompanyName = null;
        NotifyStateChanged();
    }

    public void NotifyStateChanged() => OnStateChanged?.Invoke();

    public void SetSelectedCompany(string companyId, string companyName)
    {
        SelectedCompanyId = companyId;
        SelectedCompanyName = companyName;
        NotifyStateChanged();
    }
}

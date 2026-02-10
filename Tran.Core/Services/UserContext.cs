namespace Tran.Core.Services;

/// <summary>
/// 현재 사용자 컨텍스트 (앱 수준 싱글톤)
/// MVP 단계에서는 앱 시작 시 설정, 이후 로그인 기능과 연동
/// </summary>
public static class UserContext
{
    private static string _currentUserId = "SYSTEM";
    private static string _currentUserName = "시스템";
    private static string _currentCompanyId = string.Empty;
    private static bool _isInitialized;

    /// <summary>
    /// 사용자 정보가 명시적으로 설정되었는지 여부
    /// false이면 기본값("SYSTEM")이므로 상태 전이 등 주요 작업을 거부해야 함
    /// </summary>
    public static bool IsInitialized => _isInitialized;

    /// <summary>
    /// 현재 로그인 사용자 ID
    /// </summary>
    public static string CurrentUserId => _currentUserId;

    /// <summary>
    /// 현재 로그인 사용자 이름
    /// </summary>
    public static string CurrentUserName => _currentUserName;

    /// <summary>
    /// 현재 사용자 소속 거래처 ID
    /// </summary>
    public static string CurrentCompanyId => _currentCompanyId;

    /// <summary>
    /// 사용자 정보 설정 (앱 시작 / 로그인 시 호출)
    /// </summary>
    public static void SetUser(string userId, string userName, string companyId)
    {
        _currentUserId = userId;
        _currentUserName = userName;
        _currentCompanyId = companyId;
        _isInitialized = true;
    }

    /// <summary>
    /// 사용자 정보 초기화 (로그아웃 시)
    /// </summary>
    public static void Clear()
    {
        _currentUserId = "SYSTEM";
        _currentUserName = "시스템";
        _currentCompanyId = string.Empty;
        _isInitialized = false;
    }
}

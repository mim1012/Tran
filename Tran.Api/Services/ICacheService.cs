namespace Tran.Api.Services;

/// <summary>
/// Redis 캐시 서비스 인터페이스
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// 캐시에서 값 조회
    /// </summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// 캐시에 값 저장
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// 캐시에서 값 삭제
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// 패턴에 해당하는 캐시 키 일괄 삭제
    /// </summary>
    Task RemoveByPatternAsync(string pattern);

    /// <summary>
    /// 캐시 키 존재 여부 확인
    /// </summary>
    Task<bool> ExistsAsync(string key);
}

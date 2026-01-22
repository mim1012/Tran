using System.Text.Json;
using Tran.Core.Models;

namespace Tran.Core.Utilities;

/// <summary>
/// 규격 JSON Canonicalization
/// 해시 안정성을 위한 정규화 - 분쟁 방지의 핵심
/// </summary>
/// <remarks>
/// ✅ Rule 1: Key 정렬 (유니코드 오름차순, Culture/Locale 무시)
/// ✅ Rule 2: Value 정규화 (앞뒤 공백 제거, 중간 공백 유지)
/// ✅ Rule 3: Null/Empty 제거 (빈 값은 Key 자체를 제거)
/// ✅ Rule 4: 데이터 타입 고정 (spec 값은 항상 string)
/// </remarks>
public static class SpecCanonicalizer
{
    /// <summary>
    /// 규격 컬렉션을 Canonical Dictionary로 변환
    /// </summary>
    /// <param name="specs">원본 규격 컬렉션</param>
    /// <returns>정렬되고 정규화된 Dictionary</returns>
    public static Dictionary<string, string> Canonicalize(IEnumerable<SpecEntry> specs)
    {
        var cleaned = new Dictionary<string, string>();

        foreach (var spec in specs)
        {
            // ✅ Rule 2: Value 정규화 (Trim)
            var key = spec.Key?.Trim() ?? string.Empty;
            var value = spec.Value?.Trim() ?? string.Empty;

            // ✅ Rule 3: Null/Empty 제거
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            {
                cleaned[key] = value;
            }
        }

        // ✅ Rule 1: Key 정렬 (유니코드 오름차순, Ordinal 비교)
        var canonical = new Dictionary<string, string>();
        foreach (var key in cleaned.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            canonical[key] = cleaned[key];
        }

        return canonical;
    }

    /// <summary>
    /// Canonical spec을 JSON 문자열로 직렬화
    /// ContentHash 계산에 사용
    /// </summary>
    /// <param name="canonical">정규화된 spec Dictionary</param>
    /// <returns>JSON 문자열</returns>
    public static string ToJson(Dictionary<string, string> canonical)
    {
        return JsonSerializer.Serialize(canonical, new JsonSerializerOptions
        {
            WriteIndented = false,  // 압축 형식 (공백 없음)
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    /// <summary>
    /// JSON 문자열을 SpecEntry 컬렉션으로 파싱
    /// (문서 로드 시 사용)
    /// </summary>
    /// <param name="json">JSON 문자열</param>
    /// <returns>SpecEntry 컬렉션</returns>
    public static List<SpecEntry> FromJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new List<SpecEntry>();

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (dict == null)
                return new List<SpecEntry>();

            return dict.Select(kvp => new SpecEntry
            {
                Key = kvp.Key,
                Value = kvp.Value
            }).ToList();
        }
        catch
        {
            return new List<SpecEntry>();
        }
    }
}

namespace Tran.Api.DTOs;

/// <summary>
/// 로그인 요청 DTO
/// </summary>
public class LoginRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 로그인 응답 DTO
/// </summary>
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
}

/// <summary>
/// API 응답 래퍼
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> SuccessResult(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> ErrorResult(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}

/// <summary>
/// 페이징 요청 DTO
/// </summary>
public class PagedRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}

/// <summary>
/// 페이징 응답 DTO
/// </summary>
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
/// 대시보드 요약 DTO
/// </summary>
public class DashboardSummary
{
    public int TotalOrders { get; set; }
    public int ApprovedOrders { get; set; }
    public int PendingOrders { get; set; }
    public int LowStockItems { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public decimal TotalPurchaseAmount { get; set; }
    public List<RecentOrderDto> RecentOrders { get; set; } = new();
}

/// <summary>
/// 최근 발주 DTO
/// </summary>
public class RecentOrderDto
{
    public int OrderId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string State { get; set; } = string.Empty;
}

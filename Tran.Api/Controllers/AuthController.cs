using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Tran.Api.DTOs;
using Tran.Data;
using Microsoft.EntityFrameworkCore;

namespace Tran.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly TranDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(TranDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    /// <summary>
    /// 로그인
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        // MVP: 간단한 인증 (실제 환경에서는 암호화된 비밀번호 비교 필요)
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == request.UserId);

        if (user == null)
        {
            return Unauthorized(ApiResponse<LoginResponse>.ErrorResult("사용자를 찾을 수 없습니다."));
        }

        // 회사 정보 조회
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.CompanyId == user.CompanyId);

        // JWT 토큰 생성
        var token = GenerateJwtToken(user.UserId, user.UserName, user.CompanyId);

        var response = new LoginResponse
        {
            Token = token,
            UserId = user.UserId,
            UserName = user.UserName,
            CompanyId = user.CompanyId,
            CompanyName = company?.CompanyName ?? ""
        };

        return Ok(ApiResponse<LoginResponse>.SuccessResult(response, "로그인 성공"));
    }

    /// <summary>
    /// 현재 사용자 정보 조회
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<LoginResponse>.ErrorResult("인증되지 않은 사용자입니다."));
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return NotFound(ApiResponse<LoginResponse>.ErrorResult("사용자를 찾을 수 없습니다."));
        }

        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.CompanyId == user.CompanyId);

        var response = new LoginResponse
        {
            Token = "", // 기존 토큰 사용
            UserId = user.UserId,
            UserName = user.UserName,
            CompanyId = user.CompanyId,
            CompanyName = company?.CompanyName ?? ""
        };

        return Ok(ApiResponse<LoginResponse>.SuccessResult(response));
    }

    private string GenerateJwtToken(string userId, string userName, string companyId)
    {
        var jwtKey = _configuration.GetValue<string>("Jwt:Key") ?? throw new InvalidOperationException("JWT Key not configured");
        var jwtIssuer = _configuration.GetValue<string>("Jwt:Issuer") ?? "TranERP";
        var jwtAudience = _configuration.GetValue<string>("Jwt:Audience") ?? "TranERP-Client";
        var expireMinutes = _configuration.GetValue<int>("Jwt:ExpireMinutes");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName),
            new Claim("CompanyId", companyId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

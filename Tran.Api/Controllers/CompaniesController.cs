using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tran.Api.DTOs;
using Tran.Core.Models;
using Tran.Data;

namespace Tran.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CompaniesController : ControllerBase
{
    private readonly TranDbContext _context;

    public CompaniesController(TranDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<Company>>>> GetAllCompanies()
    {
        try
        {
            var companies = await _context.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();
            return Ok(ApiResponse<List<Company>>.SuccessResult(companies));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<Company>>.ErrorResult(ex.Message));
        }
    }

    [HttpGet("{companyId}")]
    public async Task<ActionResult<ApiResponse<Company>>> GetCompanyById(string companyId)
    {
        try
        {
            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.CompanyId == companyId);
            
            if (company == null)
            {
                return NotFound(ApiResponse<Company>.ErrorResult("거래처를 찾을 수 없습니다."));
            }
            
            return Ok(ApiResponse<Company>.SuccessResult(company));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<Company>.ErrorResult(ex.Message));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Company>>> CreateCompany([FromBody] Company company)
    {
        try
        {
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetCompanyById), new { companyId = company.CompanyId }, 
                ApiResponse<Company>.SuccessResult(company, "거래처가 생성되었습니다."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<Company>.ErrorResult(ex.Message));
        }
    }

    [HttpPut("{companyId}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateCompany(string companyId, [FromBody] Company company)
    {
        try
        {
            if (companyId != company.CompanyId)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("거래처 ID가 일치하지 않습니다."));
            }

            _context.Entry(company).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            return Ok(ApiResponse<bool>.SuccessResult(true, "거래처가 수정되었습니다."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResult(ex.Message));
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tran.Api.DTOs;
using Tran.Core.Models;
using Tran.Core.Services;

namespace Tran.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class QuotationsController : ControllerBase
{
    private readonly IQuotationService _quotationService;

    public QuotationsController(IQuotationService quotationService)
    {
        _quotationService = quotationService;
    }

    [HttpGet("company/{companyId}")]
    public async Task<ActionResult<ApiResponse<List<Quotation>>>> GetQuotationsByCompany(string companyId)
    {
        try
        {
            var quotations = await _quotationService.GetQuotationsByCompanyAsync(companyId);
            return Ok(ApiResponse<List<Quotation>>.SuccessResult(quotations));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<Quotation>>.ErrorResult(ex.Message));
        }
    }

    [HttpGet("{quotationId}")]
    public async Task<ActionResult<ApiResponse<Quotation>>> GetQuotationById(int quotationId)
    {
        try
        {
            var quotation = await _quotationService.GetByIdAsync(quotationId);
            if (quotation == null)
            {
                return NotFound(ApiResponse<Quotation>.ErrorResult("견적을 찾을 수 없습니다."));
            }
            return Ok(ApiResponse<Quotation>.SuccessResult(quotation));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<Quotation>.ErrorResult(ex.Message));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Quotation>>> CreateDraft([FromBody] Quotation quotation)
    {
        try
        {
            var created = await _quotationService.CreateDraftAsync(quotation);
            return CreatedAtAction(nameof(GetQuotationById), new { quotationId = created.QuotationId }, 
                ApiResponse<Quotation>.SuccessResult(created, "견적 초안이 생성되었습니다."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<Quotation>.ErrorResult(ex.Message));
        }
    }
}

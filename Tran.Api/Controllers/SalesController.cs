using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tran.Api.DTOs;
using Tran.Core.Models;
using Tran.Core.Services;

namespace Tran.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;

    public SalesController(ISaleService saleService)
    {
        _saleService = saleService;
    }

    [HttpGet("company/{companyId}")]
    public async Task<ActionResult<ApiResponse<List<Sale>>>> GetSalesByCompany(string companyId)
    {
        try
        {
            var sales = await _saleService.GetSalesByCompanyAsync(companyId);
            return Ok(ApiResponse<List<Sale>>.SuccessResult(sales));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<Sale>>.ErrorResult(ex.Message));
        }
    }

    [HttpGet("{saleId}")]
    public async Task<ActionResult<ApiResponse<Sale>>> GetSaleById(int saleId)
    {
        try
        {
            var sale = await _saleService.GetByIdAsync(saleId);
            if (sale == null)
            {
                return NotFound(ApiResponse<Sale>.ErrorResult("판매를 찾을 수 없습니다."));
            }
            return Ok(ApiResponse<Sale>.SuccessResult(sale));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<Sale>.ErrorResult(ex.Message));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Sale>>> CreateDraft([FromBody] Sale sale)
    {
        try
        {
            var created = await _saleService.CreateDraftAsync(sale);
            return CreatedAtAction(nameof(GetSaleById), new { saleId = created.SaleId }, 
                ApiResponse<Sale>.SuccessResult(created, "판매 초안이 생성되었습니다."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<Sale>.ErrorResult(ex.Message));
        }
    }

    [HttpPost("{saleId}/confirm")]
    public async Task<ActionResult<ApiResponse<Sale>>> ConfirmSale(int saleId)
    {
        try
        {
            var sale = await _saleService.ConfirmSaleAsync(saleId);
            return Ok(ApiResponse<Sale>.SuccessResult(sale, "판매가 확정되었습니다."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<Sale>.ErrorResult(ex.Message));
        }
    }
}

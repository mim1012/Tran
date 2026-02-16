using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tran.Api.DTOs;
using Tran.Core.Models;
using Tran.Core.Services;

namespace Tran.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PurchasesController : ControllerBase
{
    private readonly IPurchaseService _purchaseService;

    public PurchasesController(IPurchaseService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    [HttpGet("company/{companyId}")]
    public async Task<ActionResult<ApiResponse<List<Purchase>>>> GetPurchasesByCompany(string companyId)
    {
        try
        {
            var purchases = await _purchaseService.GetPurchasesByCompanyAsync(companyId);
            return Ok(ApiResponse<List<Purchase>>.SuccessResult(purchases));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<Purchase>>.ErrorResult(ex.Message));
        }
    }

    [HttpGet("{purchaseId}")]
    public async Task<ActionResult<ApiResponse<Purchase>>> GetPurchaseById(int purchaseId)
    {
        try
        {
            var purchase = await _purchaseService.GetByIdAsync(purchaseId);
            if (purchase == null)
            {
                return NotFound(ApiResponse<Purchase>.ErrorResult("입고를 찾을 수 없습니다."));
            }
            return Ok(ApiResponse<Purchase>.SuccessResult(purchase));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<Purchase>.ErrorResult(ex.Message));
        }
    }
}

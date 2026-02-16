using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tran.Api.DTOs;
using Tran.Core.Models;
using Tran.Core.Services;

namespace Tran.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<Inventory>>>> GetAllInventory()
    {
        try
        {
            var inventory = await _inventoryService.GetAllInventoryAsync();
            return Ok(ApiResponse<List<Inventory>>.SuccessResult(inventory));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<Inventory>>.ErrorResult(ex.Message));
        }
    }

    [HttpGet("product/{productId}")]
    public async Task<ActionResult<ApiResponse<Inventory>>> GetInventoryByProduct(int productId)
    {
        try
        {
            var inventory = await _inventoryService.GetByProductIdAsync(productId);
            if (inventory == null)
            {
                return NotFound(ApiResponse<Inventory>.ErrorResult("재고를 찾을 수 없습니다."));
            }
            return Ok(ApiResponse<Inventory>.SuccessResult(inventory));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<Inventory>.ErrorResult(ex.Message));
        }
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<ApiResponse<List<Inventory>>>> GetLowStockItems()
    {
        try
        {
            var items = await _inventoryService.GetAllInventoryAsync();
            var lowStock = items.Where(i => i.ConfirmedQuantity <= i.SafetyStock).ToList();
            return Ok(ApiResponse<List<Inventory>>.SuccessResult(lowStock));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<Inventory>>.ErrorResult(ex.Message));
        }
    }
}

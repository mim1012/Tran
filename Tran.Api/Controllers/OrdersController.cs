using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tran.Api.DTOs;
using Tran.Core.Models;
using Tran.Core.Services;

namespace Tran.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// 거래처별 발주 목록 조회
    /// </summary>
    [HttpGet("company/{companyId}")]
    public async Task<ActionResult<ApiResponse<List<Order>>>> GetOrdersByCompany(string companyId)
    {
        try
        {
            var orders = await _orderService.GetOrdersByCompanyAsync(companyId);
            return Ok(ApiResponse<List<Order>>.SuccessResult(orders));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<Order>>.ErrorResult(ex.Message));
        }
    }

    /// <summary>
    /// 발주 상세 조회
    /// </summary>
    [HttpGet("{orderId}")]
    public async Task<ActionResult<ApiResponse<Order>>> GetOrderById(int orderId)
    {
        try
        {
            var order = await _orderService.GetByIdAsync(orderId);
            if (order == null)
            {
                return NotFound(ApiResponse<Order>.ErrorResult("발주를 찾을 수 없습니다."));
            }
            return Ok(ApiResponse<Order>.SuccessResult(order));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<Order>.ErrorResult(ex.Message));
        }
    }

    /// <summary>
    /// 발주 초안 생성
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Order>>> CreateDraft([FromBody] Order order)
    {
        try
        {
            var created = await _orderService.CreateDraftAsync(order);
            return CreatedAtAction(nameof(GetOrderById), new { orderId = created.OrderId }, 
                ApiResponse<Order>.SuccessResult(created, "발주 초안이 생성되었습니다."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<Order>.ErrorResult(ex.Message));
        }
    }

    /// <summary>
    /// 발주 확정
    /// </summary>
    [HttpPost("{orderId}/complete")]
    public async Task<ActionResult<ApiResponse<Order>>> CompleteOrder(int orderId)
    {
        try
        {
            var order = await _orderService.CompleteOrderAsync(orderId);
            return Ok(ApiResponse<Order>.SuccessResult(order, "발주가 확정되었습니다."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<Order>.ErrorResult(ex.Message));
        }
    }

    /// <summary>
    /// 발주 취소
    /// </summary>
    [HttpPost("{orderId}/cancel")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelOrder(int orderId)
    {
        try
        {
            await _orderService.CancelOrderAsync(orderId);
            return Ok(ApiResponse<bool>.SuccessResult(true, "발주가 취소되었습니다."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResult(ex.Message));
        }
    }
}

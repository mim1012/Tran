using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tran.Api.DTOs;
using Tran.Data;
using Tran.Core.Models;

namespace Tran.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly TranDbContext _context;

    public DashboardController(TranDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummary>>> GetDashboardSummary()
    {
        try
        {
            var summary = new DashboardSummary();

            // 이번 달 발주 수
            var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            summary.TotalOrders = await _context.Orders
                .Where(o => o.OrderDate >= startOfMonth)
                .CountAsync();

            // 승인 완료 발주
            summary.ApprovedOrders = await _context.Orders
                .Where(o => o.OrderDate >= startOfMonth && o.State == OrderState.Completed)
                .CountAsync();

            // 승인 대기 발주
            summary.PendingOrders = await _context.Orders
                .Where(o => o.State == OrderState.Draft)
                .CountAsync();

            // 재고 부족 품목
            summary.LowStockItems = await _context.Inventories
                .Where(i => i.ConfirmedQuantity <= i.SafetyStock)
                .CountAsync();

            // 이번 달 판매 총액
            summary.TotalSalesAmount = await _context.Sales
                .Where(s => s.SaleDate >= startOfMonth && s.State == SaleState.Confirmed)
                .SumAsync(s => s.TotalAmount);

            // 이번 달 구매 총액
            summary.TotalPurchaseAmount = await _context.Purchases
                .Where(p => p.PurchaseDate >= startOfMonth && p.State == PurchaseState.Delivered)
                .SumAsync(p => p.TotalAmount);

            // 최근 발주 내역 (최근 6건)
            summary.RecentOrders = await _context.Orders
                .Include(o => o.Company)
                .OrderByDescending(o => o.OrderDate)
                .Take(6)
                .Select(o => new RecentOrderDto
                {
                    OrderId = o.OrderId,
                    CompanyName = o.Company.CompanyName,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    State = o.State.ToString()
                })
                .ToListAsync();

            return Ok(ApiResponse<DashboardSummary>.SuccessResult(summary));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<DashboardSummary>.ErrorResult(ex.Message));
        }
    }
}

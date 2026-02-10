using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;
using Tran.Core.Services;

namespace Tran.Data.Services;

/// <summary>
/// 발주 관리 서비스 구현
/// 발주 확정 시 Purchase 자동 생성 + 재고 PendingIn 반영
/// </summary>
public class OrderService : IOrderService
{
    private readonly TranDbContext _context;

    public OrderService(TranDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// 거래처별 발주 목록 조회
    /// </summary>
    public async Task<List<Order>> GetOrdersByCompanyAsync(string companyId)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Company)
            .Where(o => o.CompanyId == companyId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    /// <summary>
    /// 발주 ID로 조회 (Items 포함)
    /// </summary>
    public async Task<Order?> GetByIdAsync(int orderId)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Company)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);
    }

    /// <summary>
    /// 발주 초안 생성 (Draft 상태)
    /// </summary>
    public async Task<Order> CreateDraftAsync(Order order)
    {
        order.State = OrderState.Draft;
        order.CreatedAt = DateTime.UtcNow;
        order.OrderDate = order.OrderDate == default ? DateTime.UtcNow : order.OrderDate;

        // 합계 계산
        order.TotalAmount = order.Items.Sum(i => i.LineAmount);

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return order;
    }

    /// <summary>
    /// 발주 확정 처리
    /// 1. Order → Completed 상태 전이
    /// 2. Purchase 자동 생성 (PendingDelivery 상태)
    /// 3. 재고 PendingInQuantity 업데이트
    /// 모든 작업을 단일 트랜잭션으로 실행
    /// </summary>
    public async Task<Order> CompleteOrderAsync(int orderId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. 발주 조회 및 상태 변경
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                throw new InvalidOperationException($"발주를 찾을 수 없습니다. (ID: {orderId})");
            }

            if (order.State != OrderState.Draft)
            {
                throw new InvalidOperationException($"Draft 상태의 발주만 확정할 수 있습니다. (현재 상태: {order.State})");
            }

            if (!order.Items.Any())
            {
                throw new InvalidOperationException("발주 품목이 없습니다.");
            }

            order.State = OrderState.Completed;
            order.CompletedAt = DateTime.UtcNow;

            // 2. Purchase 자동 생성
            var purchase = new Purchase
            {
                OrderId = order.OrderId,
                CompanyId = order.CompanyId,
                PurchaseDate = DateTime.UtcNow,
                State = PurchaseState.PendingDelivery,
                TotalAmount = order.TotalAmount,
                Memo = $"발주 #{order.OrderId}에서 자동 생성",
                CreatedAt = DateTime.UtcNow,
                Items = new List<PurchaseItem>()
            };

            foreach (var orderItem in order.Items)
            {
                if (orderItem.Quantity <= 0)
                {
                    throw new InvalidOperationException($"품목 '{orderItem.ProductName}' 수량이 유효하지 않습니다.");
                }

                purchase.Items.Add(new PurchaseItem
                {
                    ProductId = orderItem.ProductId,
                    ProductName = orderItem.ProductName,
                    OrderedQuantity = orderItem.Quantity,
                    ReceivedQuantity = 0,
                    DefectQuantity = 0,
                    UnitPrice = orderItem.UnitPrice,
                    LineAmount = orderItem.LineAmount,
                    Note = orderItem.Note
                });
            }

            _context.Purchases.Add(purchase);

            // 3. 재고 PendingInQuantity 업데이트
            foreach (var orderItem in order.Items)
            {
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(inv => inv.ProductId == orderItem.ProductId);

                if (inventory == null)
                {
                    inventory = new Inventory
                    {
                        ProductId = orderItem.ProductId,
                        ConfirmedQuantity = 0,
                        PendingInQuantity = 0,
                        PendingOutQuantity = 0,
                        SafetyStock = 0,
                        LastUpdatedAt = DateTime.UtcNow
                    };
                    _context.Inventories.Add(inventory);
                }

                inventory.PendingInQuantity += orderItem.Quantity;
                inventory.LastUpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return order;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 발주 취소
    /// </summary>
    public async Task CancelOrderAsync(int orderId)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            throw new InvalidOperationException($"발주를 찾을 수 없습니다. (ID: {orderId})");
        }

        if (order.State != OrderState.Draft)
        {
            throw new InvalidOperationException($"Draft 상태의 발주만 취소할 수 있습니다. (현재 상태: {order.State})");
        }

        order.State = OrderState.Cancelled;
        await _context.SaveChangesAsync();
    }
}

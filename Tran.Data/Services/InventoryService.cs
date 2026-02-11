using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;
using Tran.Core.Services;

namespace Tran.Data.Services;

/// <summary>
/// 재고 관리 서비스 구현
/// 재고 현황 조회, 입출고 처리, 재고 조정, 트랜잭션 로깅
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly TranDbContext _context;

    public InventoryService(TranDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// 전체 재고 현황 조회
    /// </summary>
    public async Task<List<Inventory>> GetAllInventoryAsync()
    {
        return await _context.Inventories
            .Include(i => i.Product)
            .OrderBy(i => i.Product.ProductName)
            .ToListAsync();
    }

    /// <summary>
    /// 품목별 재고 조회
    /// </summary>
    public async Task<Inventory?> GetByProductIdAsync(int productId)
    {
        return await _context.Inventories
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.ProductId == productId);
    }

    /// <summary>
    /// 재고 이동 이력 조회
    /// </summary>
    public async Task<List<InventoryTransaction>> GetTransactionHistoryAsync(int? productId = null, int limit = 100)
    {
        var query = _context.InventoryTransactions
            .Include(t => t.Product)
            .AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(t => t.ProductId == productId.Value);
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// 재고 수동 조정 (실사 등)
    /// </summary>
    public async Task AdjustInventoryAsync(int productId, decimal adjustmentQty, string reason)
    {
        var inventory = await GetOrCreateInventoryAsync(productId);

        if (inventory.ConfirmedQuantity + adjustmentQty < 0)
            throw new InvalidOperationException($"조정 후 재고가 음수가 됩니다: 현재 {inventory.ConfirmedQuantity}, 조정 {adjustmentQty}");

        inventory.ConfirmedQuantity += adjustmentQty;
        inventory.LastUpdatedAt = DateTime.UtcNow;

        // 조정 트랜잭션 로그
        var inventoryTransaction = new InventoryTransaction
        {
            ProductId = productId,
            Type = InventoryTransactionType.Adjustment,
            Quantity = adjustmentQty,
            ReferenceType = "Adjustment",
            ReferenceId = null,
            Note = reason,
            CreatedAt = DateTime.UtcNow
        };
        _context.InventoryTransactions.Add(inventoryTransaction);

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 재고 레코드 삭제
    /// </summary>
    public async Task DeleteInventoryAsync(int productId)
    {
        var inventory = await _context.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == productId);

        if (inventory != null)
        {
            _context.Inventories.Remove(inventory);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 입고 처리: ConfirmedQuantity 증가 + 트랜잭션 로그
    /// </summary>
    public async Task ProcessInboundAsync(int productId, decimal quantity, string referenceType, int referenceId)
    {
        if (quantity <= 0) throw new ArgumentException("입고 수량은 양수여야 합니다.", nameof(quantity));

        var inventory = await GetOrCreateInventoryAsync(productId);

        inventory.ConfirmedQuantity += quantity;
        inventory.LastUpdatedAt = DateTime.UtcNow;

        var inventoryTransaction = new InventoryTransaction
        {
            ProductId = productId,
            Type = InventoryTransactionType.Inbound,
            Quantity = quantity,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Note = $"{referenceType} #{referenceId} 입고",
            CreatedAt = DateTime.UtcNow
        };
        _context.InventoryTransactions.Add(inventoryTransaction);

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 출고 처리: ConfirmedQuantity 감소 + 트랜잭션 로그
    /// </summary>
    public async Task ProcessOutboundAsync(int productId, decimal quantity, string referenceType, int referenceId)
    {
        if (quantity <= 0) throw new ArgumentException("출고 수량은 양수여야 합니다.", nameof(quantity));

        var inventory = await GetOrCreateInventoryAsync(productId);

        if (inventory.ConfirmedQuantity < quantity)
            throw new InvalidOperationException($"재고 부족: 현재 {inventory.ConfirmedQuantity}, 요청 {quantity}");

        inventory.ConfirmedQuantity -= quantity;
        inventory.LastUpdatedAt = DateTime.UtcNow;

        var inventoryTransaction = new InventoryTransaction
        {
            ProductId = productId,
            Type = InventoryTransactionType.Outbound,
            Quantity = quantity,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Note = $"{referenceType} #{referenceId} 출고",
            CreatedAt = DateTime.UtcNow
        };
        _context.InventoryTransactions.Add(inventoryTransaction);

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 입고 예정 수량 추가 (발주 확정 시)
    /// </summary>
    public async Task AddPendingInboundAsync(int productId, decimal quantity)
    {
        var inventory = await GetOrCreateInventoryAsync(productId);

        inventory.PendingInQuantity += quantity;
        inventory.LastUpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 입고 예정 수량 제거 (입고 확인 시)
    /// </summary>
    public async Task RemovePendingInboundAsync(int productId, decimal quantity)
    {
        var inventory = await GetOrCreateInventoryAsync(productId);

        inventory.PendingInQuantity -= quantity;
        if (inventory.PendingInQuantity < 0)
        {
            inventory.PendingInQuantity = 0;
        }
        inventory.LastUpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 품목에 대한 Inventory 레코드 조회 또는 생성
    /// </summary>
    private async Task<Inventory> GetOrCreateInventoryAsync(int productId)
    {
        var inventory = await _context.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == productId);

        if (inventory == null)
        {
            inventory = new Inventory
            {
                ProductId = productId,
                ConfirmedQuantity = 0,
                PendingInQuantity = 0,
                PendingOutQuantity = 0,
                SafetyStock = 0,
                LastUpdatedAt = DateTime.UtcNow
            };
            _context.Inventories.Add(inventory);
        }

        return inventory;
    }
}

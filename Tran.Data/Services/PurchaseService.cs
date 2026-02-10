using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;
using Tran.Core.Services;

namespace Tran.Data.Services;

/// <summary>
/// 입고(구매) 관리 서비스 구현
/// 입고 확인 시 재고 입고 처리 + PendingIn 제거
/// </summary>
public class PurchaseService : IPurchaseService
{
    private readonly TranDbContext _context;

    public PurchaseService(TranDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// 거래처별 입고 목록 조회
    /// </summary>
    public async Task<List<Purchase>> GetPurchasesByCompanyAsync(string companyId)
    {
        return await _context.Purchases
            .Include(p => p.Items)
            .Include(p => p.Company)
            .Where(p => p.CompanyId == companyId)
            .OrderByDescending(p => p.PurchaseDate)
            .ToListAsync();
    }

    /// <summary>
    /// 입고 ID로 조회 (Items 포함)
    /// </summary>
    public async Task<Purchase?> GetByIdAsync(int purchaseId)
    {
        return await _context.Purchases
            .Include(p => p.Items)
            .Include(p => p.Company)
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.PurchaseId == purchaseId);
    }

    /// <summary>
    /// 입고 확인 처리
    /// 1. 각 품목별 ReceivedQuantity, DefectQuantity 설정
    /// 2. 재고 ConfirmedQuantity += ReceivedQty, PendingInQuantity -= OrderedQty
    /// 3. InventoryTransaction(Type=Inbound) 생성
    /// 4. 전체/부분 입고에 따라 Delivered 또는 PartiallyDelivered 상태 설정
    /// </summary>
    public async Task<Purchase> ConfirmDeliveryAsync(
        int purchaseId,
        List<(int PurchaseItemId, decimal ReceivedQty, decimal DefectQty)> receivedItems)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. 입고 조회
            var purchase = await _context.Purchases
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.PurchaseId == purchaseId);

            if (purchase == null)
            {
                throw new InvalidOperationException($"입고를 찾을 수 없습니다. (ID: {purchaseId})");
            }

            if (purchase.State == PurchaseState.Cancelled)
            {
                throw new InvalidOperationException("취소된 입고는 처리할 수 없습니다.");
            }

            if (purchase.State == PurchaseState.Delivered)
            {
                throw new InvalidOperationException("이미 입고 완료된 건은 다시 처리할 수 없습니다.");
            }

            bool allFullyDelivered = true;

            // 2. 각 품목별 수량 반영
            foreach (var (purchaseItemId, receivedQty, defectQty) in receivedItems)
            {
                var purchaseItem = purchase.Items
                    .FirstOrDefault(i => i.PurchaseItemId == purchaseItemId);

                if (purchaseItem == null)
                {
                    throw new InvalidOperationException($"입고 품목을 찾을 수 없습니다. (ID: {purchaseItemId})");
                }

                if (receivedQty < 0) throw new ArgumentException("입고수량은 0 이상이어야 합니다.");
                if (defectQty < 0) throw new ArgumentException("불량수량은 0 이상이어야 합니다.");
                if (defectQty > receivedQty) throw new ArgumentException("불량수량이 입고수량을 초과할 수 없습니다.");

                // 이전 입고분 계산 (PartiallyDelivered 재처리 시 델타만 반영)
                var previousGoodQty = purchaseItem.ReceivedQuantity - purchaseItem.DefectQuantity;

                purchaseItem.ReceivedQuantity = receivedQty;
                purchaseItem.DefectQuantity = defectQty;

                // 부분 입고 여부 확인
                if (receivedQty < purchaseItem.OrderedQuantity)
                {
                    allFullyDelivered = false;
                }

                // 3. 재고 업데이트
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(inv => inv.ProductId == purchaseItem.ProductId);

                if (inventory == null)
                {
                    inventory = new Inventory
                    {
                        ProductId = purchaseItem.ProductId,
                        ConfirmedQuantity = 0,
                        PendingInQuantity = 0,
                        PendingOutQuantity = 0,
                        SafetyStock = 0,
                        LastUpdatedAt = DateTime.UtcNow
                    };
                    _context.Inventories.Add(inventory);
                }

                // 확정 수량: 이번 정상 입고분 - 이전 정상 입고분 (델타만 반영)
                var currentGoodQty = receivedQty - defectQty;
                var deltaGoodQty = currentGoodQty - previousGoodQty;
                inventory.ConfirmedQuantity += deltaGoodQty;

                if (inventory.ConfirmedQuantity < 0)
                {
                    throw new InvalidOperationException(
                        $"재고 확정수량이 음수가 될 수 없습니다. (품목 ID: {purchaseItem.ProductId}, 현재: {inventory.ConfirmedQuantity})");
                }

                // 입고 예정 수량 감소: 최초 입고 시에만 차감 (PartiallyDelivered 재처리 시 중복 차감 방지)
                if (purchase.State != PurchaseState.PartiallyDelivered)
                {
                    inventory.PendingInQuantity -= purchaseItem.OrderedQuantity;
                    if (inventory.PendingInQuantity < 0)
                    {
                        inventory.PendingInQuantity = 0;
                    }
                }
                inventory.LastUpdatedAt = DateTime.UtcNow;

                // 4. 재고 이동 트랜잭션 로그 (델타 수량만)
                if (deltaGoodQty != 0)
                {
                    var inventoryTransaction = new InventoryTransaction
                    {
                        ProductId = purchaseItem.ProductId,
                        Type = deltaGoodQty > 0 ? InventoryTransactionType.Inbound : InventoryTransactionType.Adjustment,
                        Quantity = Math.Abs(deltaGoodQty),
                        ReferenceType = "Purchase",
                        ReferenceId = purchase.PurchaseId,
                        Note = purchase.State == PurchaseState.PartiallyDelivered
                            ? $"입고 #{purchase.PurchaseId} 추가 입고 (이전: {previousGoodQty}, 이번: {currentGoodQty})"
                            : $"입고 #{purchase.PurchaseId} 확인 - 입고 (불량: {defectQty})",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.InventoryTransactions.Add(inventoryTransaction);
                }
            }

            // 5. 입고 상태 설정
            purchase.State = allFullyDelivered
                ? PurchaseState.Delivered
                : PurchaseState.PartiallyDelivered;
            purchase.DeliveredAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return purchase;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

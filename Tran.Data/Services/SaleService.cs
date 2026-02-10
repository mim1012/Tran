using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;
using Tran.Core.Services;

namespace Tran.Data.Services;

/// <summary>
/// 판매 관리 서비스 구현
/// 판매 확정 시 재고 출고 자동 처리
/// </summary>
public class SaleService : ISaleService
{
    private readonly TranDbContext _context;

    public SaleService(TranDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// 거래처별 판매 목록 조회
    /// </summary>
    public async Task<List<Sale>> GetSalesByCompanyAsync(string companyId)
    {
        return await _context.Sales
            .Include(s => s.Items)
            .Include(s => s.Company)
            .Where(s => s.CompanyId == companyId)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();
    }

    /// <summary>
    /// 판매 ID로 조회 (Items 포함)
    /// </summary>
    public async Task<Sale?> GetByIdAsync(int saleId)
    {
        return await _context.Sales
            .Include(s => s.Items)
            .Include(s => s.Company)
            .FirstOrDefaultAsync(s => s.SaleId == saleId);
    }

    /// <summary>
    /// 판매 초안 생성 (Draft 상태)
    /// </summary>
    public async Task<Sale> CreateDraftAsync(Sale sale)
    {
        sale.State = SaleState.Draft;
        sale.CreatedAt = DateTime.UtcNow;
        sale.SaleDate = sale.SaleDate == default ? DateTime.UtcNow : sale.SaleDate;

        // 합계 계산
        sale.TotalAmount = sale.Items.Sum(i => i.LineAmount);

        _context.Sales.Add(sale);
        await _context.SaveChangesAsync();

        return sale;
    }

    /// <summary>
    /// 판매 확정 처리
    /// 1. Sale → Confirmed 상태 전이
    /// 2. 각 품목별 재고 출고 (ConfirmedQuantity 감소)
    /// 3. InventoryTransaction(Type=Outbound) 생성
    /// 모든 작업을 단일 트랜잭션으로 실행
    /// </summary>
    public async Task<Sale> ConfirmSaleAsync(int saleId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. 판매 조회 및 상태 변경
            var sale = await _context.Sales
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.SaleId == saleId);

            if (sale == null)
            {
                throw new InvalidOperationException($"판매를 찾을 수 없습니다. (ID: {saleId})");
            }

            if (sale.State != SaleState.Draft)
            {
                throw new InvalidOperationException($"Draft 상태의 판매만 확정할 수 있습니다. (현재 상태: {sale.State})");
            }

            sale.State = SaleState.Confirmed;
            sale.ConfirmedAt = DateTime.UtcNow;

            // 2. 각 품목별 재고 출고 처리
            foreach (var saleItem in sale.Items)
            {
                if (saleItem.Quantity <= 0)
                {
                    throw new InvalidOperationException($"품목 수량이 유효하지 않습니다. (ProductId: {saleItem.ProductId}, Quantity: {saleItem.Quantity})");
                }

                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(inv => inv.ProductId == saleItem.ProductId);

                if (inventory == null)
                {
                    inventory = new Inventory
                    {
                        ProductId = saleItem.ProductId,
                        ConfirmedQuantity = 0,
                        PendingInQuantity = 0,
                        PendingOutQuantity = 0,
                        SafetyStock = 0,
                        LastUpdatedAt = DateTime.UtcNow
                    };
                    _context.Inventories.Add(inventory);
                }

                if (inventory.ConfirmedQuantity < saleItem.Quantity)
                {
                    throw new InvalidOperationException($"재고 부족: 품목 {saleItem.ProductId}, 현재 {inventory.ConfirmedQuantity}, 요청 {saleItem.Quantity}");
                }

                inventory.ConfirmedQuantity -= saleItem.Quantity;
                inventory.LastUpdatedAt = DateTime.UtcNow;

                // 3. 재고 이동 트랜잭션 로그
                var inventoryTransaction = new InventoryTransaction
                {
                    ProductId = saleItem.ProductId,
                    Type = InventoryTransactionType.Outbound,
                    Quantity = saleItem.Quantity,
                    ReferenceType = "Sale",
                    ReferenceId = sale.SaleId,
                    Note = $"판매 #{sale.SaleId} 확정 - 출고",
                    CreatedAt = DateTime.UtcNow
                };
                _context.InventoryTransactions.Add(inventoryTransaction);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return sale;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 판매 취소
    /// </summary>
    public async Task CancelSaleAsync(int saleId)
    {
        var sale = await _context.Sales
            .FirstOrDefaultAsync(s => s.SaleId == saleId);

        if (sale == null)
        {
            throw new InvalidOperationException($"판매를 찾을 수 없습니다. (ID: {saleId})");
        }

        if (sale.State != SaleState.Draft)
        {
            throw new InvalidOperationException($"Draft 상태의 판매만 취소할 수 있습니다. (현재 상태: {sale.State})");
        }

        sale.State = SaleState.Cancelled;
        await _context.SaveChangesAsync();
    }
}

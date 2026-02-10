using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;
using Tran.Core.Services;

namespace Tran.Data.Services;

/// <summary>
/// 견적 관리 서비스 구현
/// 견적 확정 시 CompanyPrice 업데이트 + CompanyProduct 등록/갱신
/// </summary>
public class QuotationService : IQuotationService
{
    private readonly TranDbContext _context;

    public QuotationService(TranDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// 거래처별 견적 목록 조회
    /// </summary>
    public async Task<List<Quotation>> GetQuotationsByCompanyAsync(string companyId)
    {
        return await _context.Quotations
            .Include(q => q.Items)
            .Include(q => q.Company)
            .Where(q => q.CompanyId == companyId)
            .OrderByDescending(q => q.QuotationDate)
            .ToListAsync();
    }

    /// <summary>
    /// 견적 ID로 조회 (Items 포함)
    /// </summary>
    public async Task<Quotation?> GetByIdAsync(int quotationId)
    {
        return await _context.Quotations
            .Include(q => q.Items)
            .Include(q => q.Company)
            .FirstOrDefaultAsync(q => q.QuotationId == quotationId);
    }

    /// <summary>
    /// 견적 초안 생성 (Draft 상태)
    /// </summary>
    public async Task<Quotation> CreateDraftAsync(Quotation quotation)
    {
        quotation.State = QuotationState.Draft;
        quotation.CreatedAt = DateTime.UtcNow;
        quotation.QuotationDate = quotation.QuotationDate == default ? DateTime.UtcNow : quotation.QuotationDate;

        // 합계 계산
        quotation.TotalAmount = quotation.Items.Sum(i => i.LineAmount);

        _context.Quotations.Add(quotation);
        await _context.SaveChangesAsync();

        return quotation;
    }

    /// <summary>
    /// 견적 전송 (Draft → Sent)
    /// </summary>
    public async Task<Quotation> SendQuotationAsync(int quotationId)
    {
        var quotation = await _context.Quotations
            .FirstOrDefaultAsync(q => q.QuotationId == quotationId);

        if (quotation == null)
        {
            throw new InvalidOperationException($"견적을 찾을 수 없습니다. (ID: {quotationId})");
        }

        if (quotation.State != QuotationState.Draft)
        {
            throw new InvalidOperationException($"Draft 상태의 견적만 전송할 수 있습니다. (현재 상태: {quotation.State})");
        }

        quotation.State = QuotationState.Sent;
        quotation.SentAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return quotation;
    }

    /// <summary>
    /// 견적 확정 처리
    /// 1. Quotation → Confirmed 상태 전이
    /// 2. 각 품목별 CompanyPrice upsert + PriceHistory 생성
    /// 3. CompanyProduct upsert (OrderCount 증가)
    /// 모든 작업을 단일 트랜잭션으로 실행
    /// </summary>
    public async Task<Quotation> ConfirmQuotationAsync(int quotationId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. 견적 조회 및 상태 변경
            var quotation = await _context.Quotations
                .Include(q => q.Items)
                .FirstOrDefaultAsync(q => q.QuotationId == quotationId);

            if (quotation == null)
            {
                throw new InvalidOperationException($"견적을 찾을 수 없습니다. (ID: {quotationId})");
            }

            if (quotation.State != QuotationState.Draft &&
                quotation.State != QuotationState.Sent &&
                quotation.State != QuotationState.UnderReview)
            {
                throw new InvalidOperationException(
                    $"확정 가능한 상태가 아닙니다. (현재 상태: {quotation.State})");
            }

            quotation.State = QuotationState.Confirmed;
            quotation.ConfirmedAt = DateTime.UtcNow;

            // 2. 각 품목별 CompanyPrice + PriceHistory + CompanyProduct 처리
            foreach (var item in quotation.Items)
            {
                if (item.UnitPrice < 0)
                    throw new InvalidOperationException($"품목 '{item.ProductName}' 단가가 유효하지 않습니다.");

                // CompanyPrice upsert
                var companyPrice = await _context.CompanyPrices
                    .FirstOrDefaultAsync(cp =>
                        cp.CompanyId == quotation.CompanyId &&
                        cp.ProductId == item.ProductId);

                decimal? oldPrice = null;

                if (companyPrice == null)
                {
                    companyPrice = new CompanyPrice
                    {
                        CompanyId = quotation.CompanyId,
                        ProductId = item.ProductId,
                        UnitPrice = item.UnitPrice,
                        EffectiveFrom = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.CompanyPrices.Add(companyPrice);
                }
                else
                {
                    oldPrice = companyPrice.UnitPrice;
                    companyPrice.UnitPrice = item.UnitPrice;
                    companyPrice.EffectiveFrom = DateTime.UtcNow;
                }

                // PriceHistory 생성
                var priceHistory = new PriceHistory
                {
                    CompanyId = quotation.CompanyId,
                    ProductId = item.ProductId,
                    OldPrice = oldPrice ?? 0,
                    NewPrice = item.UnitPrice,
                    ChangedAt = DateTime.UtcNow,
                    Reason = $"견적 #{quotation.QuotationId} 확정"
                };
                _context.PriceHistories.Add(priceHistory);

                // CompanyProduct upsert
                var companyProduct = await _context.CompanyProducts
                    .FirstOrDefaultAsync(cp =>
                        cp.CompanyId == quotation.CompanyId &&
                        cp.ProductId == item.ProductId);

                if (companyProduct == null)
                {
                    companyProduct = new CompanyProduct
                    {
                        CompanyId = quotation.CompanyId,
                        ProductId = item.ProductId,
                        OrderCount = 1,
                        LastOrderDate = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.CompanyProducts.Add(companyProduct);
                }
                else
                {
                    companyProduct.OrderCount++;
                    companyProduct.LastOrderDate = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return quotation;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

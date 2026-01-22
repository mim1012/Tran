# 서류 관리 (Document Management) 상세 명세

> **핵심 원칙**: 견적서 확정 → 단가 정책 반영 → 품목 리스트 자동 등록

---

## 1. 견적서 (Quotation)

### 1.1 데이터 모델

```csharp
public class Quotation
{
    public int Id { get; set; }
    public string QuotationNumber { get; set; }  // 자동 생성: QT-YYYYMMDD-XXXX

    // 거래처 정보
    public int CompanyId { get; set; }
    public Company Company { get; set; }

    // 담당자
    public int UserId { get; set; }
    public User User { get; set; }

    // 상태
    public QuotationState State { get; set; }

    // 기간
    public DateTime CreatedAt { get; set; }
    public DateTime ValidUntil { get; set; }  // 유효기간

    // 품목
    public List<QuotationItem> Items { get; set; }

    // 메타
    public string Memo { get; set; }
    public int? PreviousVersionId { get; set; }  // 수정요청 시 이전 버전
    public int Version { get; set; } = 1;

    // 계산 필드
    public decimal TotalAmount => Items?.Sum(i => i.Amount) ?? 0;
}

public class QuotationItem
{
    public int Id { get; set; }
    public int QuotationId { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount => Quantity * UnitPrice;

    public string Specification { get; set; }  // 규격
    public string Memo { get; set; }
}

public enum QuotationState
{
    Draft = 0,              // 작성
    Sent = 1,               // 발송
    UnderReview = 2,        // 수신 검토중
    Confirmed = 3,          // 확정 (Terminal)
    RevisionRequested = 4   // 수정요청
}
```

### 1.2 상태 전이 규칙

```csharp
public class QuotationStateTransitionService
{
    private static readonly Dictionary<QuotationState, QuotationState[]> _allowedTransitions = new()
    {
        { QuotationState.Draft, new[] { QuotationState.Sent } },
        { QuotationState.Sent, new[] { QuotationState.UnderReview } },
        { QuotationState.UnderReview, new[] { QuotationState.Confirmed, QuotationState.RevisionRequested } },
        { QuotationState.RevisionRequested, new[] { QuotationState.Draft } },  // 새 버전 생성
        { QuotationState.Confirmed, Array.Empty<QuotationState>() }  // Terminal
    };

    public bool CanTransition(QuotationState from, QuotationState to)
        => _allowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public async Task<QuotationStateLog> TransitionAsync(
        Quotation quotation,
        QuotationState newState,
        int userId,
        string reason = null)
    {
        if (!CanTransition(quotation.State, newState))
            throw new InvalidStateTransitionException(quotation.State, newState);

        var log = new QuotationStateLog
        {
            QuotationId = quotation.Id,
            FromState = quotation.State,
            ToState = newState,
            UserId = userId,
            Reason = reason,
            Timestamp = DateTime.UtcNow
        };

        quotation.State = newState;

        // 확정 시 후속 처리
        if (newState == QuotationState.Confirmed)
        {
            await ApplyPricePolicyAsync(quotation);
            await RegisterProductsToCompanyAsync(quotation);
        }

        // 수정요청 시 새 버전 생성
        if (newState == QuotationState.RevisionRequested)
        {
            await CreateNewVersionAsync(quotation);
        }

        return log;
    }
}
```

### 1.3 견적 확정 시 자동 처리

#### 1.3.1 단가 정책 반영

```csharp
public class PricePolicyService
{
    public async Task ApplyPricePolicyAsync(Quotation quotation)
    {
        foreach (var item in quotation.Items)
        {
            // 기존 단가 확인
            var existingPrice = await _context.CompanyPrices
                .Where(p => p.CompanyId == quotation.CompanyId
                         && p.ProductId == item.ProductId)
                .OrderByDescending(p => p.EffectiveDate)
                .FirstOrDefaultAsync();

            // 단가가 변경된 경우에만 새 레코드 생성
            if (existingPrice == null || existingPrice.UnitPrice != item.UnitPrice)
            {
                var newPrice = new CompanyPrice
                {
                    CompanyId = quotation.CompanyId,
                    ProductId = item.ProductId,
                    UnitPrice = item.UnitPrice,
                    EffectiveDate = DateTime.UtcNow,
                    Source = PriceSource.Quotation,
                    SourceId = quotation.Id
                };

                _context.CompanyPrices.Add(newPrice);

                // 단가 이력 저장
                var history = new PriceHistory
                {
                    CompanyId = quotation.CompanyId,
                    ProductId = item.ProductId,
                    OldPrice = existingPrice?.UnitPrice,
                    NewPrice = item.UnitPrice,
                    ChangedAt = DateTime.UtcNow,
                    ChangedBy = quotation.UserId,
                    Reason = $"견적서 #{quotation.QuotationNumber} 확정"
                };

                _context.PriceHistories.Add(history);
            }
        }

        await _context.SaveChangesAsync();
    }
}
```

#### 1.3.2 품목 리스트 등록

```csharp
public class CompanyProductService
{
    public async Task RegisterProductsToCompanyAsync(Quotation quotation)
    {
        foreach (var item in quotation.Items)
        {
            // 이미 등록된 품목인지 확인
            var existing = await _context.CompanyProducts
                .FirstOrDefaultAsync(cp =>
                    cp.CompanyId == quotation.CompanyId &&
                    cp.ProductId == item.ProductId);

            if (existing == null)
            {
                // 신규 등록
                var companyProduct = new CompanyProduct
                {
                    CompanyId = quotation.CompanyId,
                    ProductId = item.ProductId,
                    DefaultQuantity = item.Quantity,
                    LastOrderedAt = null,
                    OrderCount = 0,
                    IsActive = true,
                    RegisteredAt = DateTime.UtcNow,
                    RegisteredFrom = $"견적서 #{quotation.QuotationNumber}"
                };

                _context.CompanyProducts.Add(companyProduct);
            }
            else
            {
                // 기존 품목 활성화 (비활성 상태였다면)
                existing.IsActive = true;
            }
        }

        await _context.SaveChangesAsync();
    }
}
```

### 1.4 견적서 입력 화면

> **품목 선택**: 견적서에서 품목 추가 시 **품목 선택 모달**을 사용합니다.
> **판매 품목만 표시**: 견적서는 내가 파는 품목(📤 Sell + ↔️ Both)만 선택 가능
> **상세 명세**: `docs/features/05-product-master.md`

```
┌─────────────────────────────────────────────────────────────────────┐
│ 견적서 작성                                          [임시저장] [발송] │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  견적번호: QT-20250122-0001 (자동)     상태: ● 작성                  │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ 거래처 정보                                                  │   │
│  ├─────────────────────────────────────────────────────────────┤   │
│  │ 거래처: [A병원                    ▼]                        │   │
│  │ 담당자: [홍길동                   ▼]                        │   │
│  │ 유효기간: [2025-02-22            📅]  (30일 후)             │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ 품목 목록                           [기존 견적 불러오기] [+추가] │   │
│  ├─────────────────────────────────────────────────────────────┤   │
│  │ No │ 유형│ 품목명      │ 규격    │ 수량  │ 단가      │ 금액   │   │
│  ├────┼────┼─────────────┼─────────┼───────┼───────────┼────────┤   │
│  │ 1  │ 📤 │ 테이프 10EA │ 의료용  │ 100   │ ₩3,500    │ ₩350,000│   │
│  │ 2  │ 📤 │ 거즈 1BOX   │ 멸균    │ 50    │ ₩12,000   │ ₩600,000│   │
│  │ 3  │    │ [+ 품목 추가] ← 클릭 시 품목 선택 모달 표시       │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ 비고                                                         │   │
│  │ [                                                         ]  │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ────────────────────────────────────────────────────────────────  │
│  공급가액: ₩950,000    부가세: ₩95,000    합계: ₩1,045,000         │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

**[+ 품목 추가] 클릭 시:**
```csharp
// 견적서용 품목 선택 모달 (판매 품목만)
var modal = ProductSelectionModal.ForQuotation();
modal.OnProductSelected += (sender, args) => {
    quotation.Items.Add(new QuotationItem
    {
        ProductId = args.ProductId,
        ProductName = args.ProductName,
        Quantity = args.Quantity,
        UnitPrice = args.UnitPrice
    });
};
modal.Show();
```




### 1.5 기존 견적 불러오기

```csharp
public class QuotationLoadService
{
    /// <summary>
    /// 거래처의 확정된 견적서 목록 조회
    /// </summary>
    public async Task<List<QuotationSummary>> GetConfirmedQuotationsAsync(int companyId)
    {
        return await _context.Quotations
            .Where(q => q.CompanyId == companyId && q.State == QuotationState.Confirmed)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => new QuotationSummary
            {
                Id = q.Id,
                QuotationNumber = q.QuotationNumber,
                CreatedAt = q.CreatedAt,
                TotalAmount = q.Items.Sum(i => i.Quantity * i.UnitPrice),
                ItemCount = q.Items.Count,
                ItemPreview = string.Join(", ", q.Items.Take(3).Select(i => i.Product.Name))
            })
            .ToListAsync();
    }

    /// <summary>
    /// 견적서 품목을 현재 입력 폼에 반영
    /// </summary>
    public async Task<List<QuotationItem>> LoadQuotationItemsAsync(int quotationId)
    {
        var items = await _context.QuotationItems
            .Include(i => i.Product)
            .Where(i => i.QuotationId == quotationId)
            .ToListAsync();

        // 최신 단가로 업데이트
        foreach (var item in items)
        {
            var latestPrice = await GetLatestPriceAsync(item.Product.Id, /* companyId */);
            if (latestPrice != null)
            {
                item.UnitPrice = latestPrice.UnitPrice;
            }
        }

        return items;
    }
}
```

### 1.6 Excel/PDF 업로드 (무료 기능)

```csharp
public class QuotationImportService
{
    public async Task<ImportResult> ImportFromExcelAsync(Stream fileStream, int companyId)
    {
        var result = new ImportResult();

        using var package = new ExcelPackage(fileStream);
        var worksheet = package.Workbook.Worksheets[0];

        int row = 2; // 헤더 스킵
        while (worksheet.Cells[row, 1].Value != null)
        {
            try
            {
                var productName = worksheet.Cells[row, 1].GetValue<string>();
                var quantity = worksheet.Cells[row, 2].GetValue<int>();
                var unitPrice = worksheet.Cells[row, 3].GetValue<decimal>();

                // 품목 매칭
                var product = await MatchProductAsync(productName);

                if (product != null)
                {
                    result.Items.Add(new ImportedItem
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        MatchStatus = MatchStatus.Matched
                    });
                }
                else
                {
                    result.Items.Add(new ImportedItem
                    {
                        OriginalName = productName,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        MatchStatus = MatchStatus.Unmatched,
                        SuggestedProducts = await GetSimilarProductsAsync(productName)
                    });
                }

                row++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Row {row}: {ex.Message}");
                row++;
            }
        }

        return result;
    }

    private async Task<Product> MatchProductAsync(string name)
    {
        // 정확히 일치
        var exact = await _context.Products
            .FirstOrDefaultAsync(p => p.Name == name);
        if (exact != null) return exact;

        // 유사 매칭 (Levenshtein distance)
        var candidates = await _context.Products.ToListAsync();
        var best = candidates
            .Select(p => new { Product = p, Distance = LevenshteinDistance(p.Name, name) })
            .Where(x => x.Distance <= 3)  // 3자 이내 차이
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        return best?.Product;
    }
}
```

---

## 2. 계약서 (Contract)

### 2.1 데이터 모델

```csharp
public class Contract
{
    public int Id { get; set; }
    public string ContractNumber { get; set; }  // CT-YYYYMMDD-XXXX

    // 거래처
    public int CompanyId { get; set; }
    public Company Company { get; set; }

    // 계약 기간
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // 상태
    public ContractState State { get; set; }

    // 계약 조건
    public string DiscountCondition { get; set; }  // 할인 조건 텍스트
    public decimal? DiscountRate { get; set; }     // 할인율 (%)
    public string SpecialTerms { get; set; }       // 특이사항

    // 품목
    public List<ContractItem> Items { get; set; }

    // 메타
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public string AttachmentPath { get; set; }  // 첨부 파일

    // 계산 필드
    public bool IsActive => State == ContractState.Active
                         && StartDate <= DateTime.Today
                         && EndDate >= DateTime.Today;
    public int DaysUntilExpiry => (EndDate - DateTime.Today).Days;
}

public class ContractItem
{
    public int Id { get; set; }
    public int ContractId { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; }

    public decimal ContractPrice { get; set; }  // 계약 단가
    public decimal? MinQuantity { get; set; }   // 최소 주문 수량
    public string Specification { get; set; }
}

public enum ContractState
{
    Draft = 0,      // 작성
    UnderReview = 1, // 검토중
    Active = 2,      // 체결/유효
    Expired = 3,     // 만료
    Terminated = 4,  // 해지
    Rejected = 5     // 거절
}
```

### 2.2 계약서 관리 화면

```
┌─────────────────────────────────────────────────────────────────────┐
│ 계약서 관리                                             [+ 신규 계약] │
├─────────────────────────────────────────────────────────────────────┤
│ 필터: [거래처 ▼] [상태 ▼] [만료예정 ▼]         [검색...]           │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─ A병원 ────────────────────────────────────────────────────────┐ │
│  │                                                                 │ │
│  │  📄 CT-20250101-0001                                           │ │
│  │  기간: 2025-01-01 ~ 2025-12-31  │  상태: ● 유효               │ │
│  │  품목: 테이프, 거즈, 주사기 외 5건                              │ │
│  │  만료까지: 344일                                    [상세보기] │ │
│  │                                                                 │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  ┌─ B의원 ────────────────────────────────────────────────────────┐ │
│  │                                                                 │ │
│  │  📄 CT-20241215-0003                              ⚠️ 만료임박   │ │
│  │  기간: 2024-12-15 ~ 2025-02-15  │  상태: ● 유효               │ │
│  │  품목: 소독약, 붕대 외 2건                                      │ │
│  │  만료까지: 24일                                     [상세보기] │ │
│  │                                                                 │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 2.3 계약 만료 알림

```csharp
public class ContractAlertService
{
    private readonly INotificationService _notification;

    public async Task CheckExpiringContractsAsync()
    {
        var today = DateTime.Today;

        // 30일 이내 만료 예정
        var expiring30 = await _context.Contracts
            .Where(c => c.State == ContractState.Active)
            .Where(c => c.EndDate >= today && c.EndDate <= today.AddDays(30))
            .ToListAsync();

        // 7일 이내 만료 예정
        var expiring7 = expiring30.Where(c => c.DaysUntilExpiry <= 7);

        // 당일 만료
        var expiringToday = expiring30.Where(c => c.DaysUntilExpiry == 0);

        foreach (var contract in expiringToday)
        {
            await _notification.SendAsync(new ContractExpiryNotification
            {
                ContractId = contract.Id,
                CompanyName = contract.Company.Name,
                ExpiryDate = contract.EndDate,
                Urgency = NotificationUrgency.High,
                Recipients = await GetContractManagersAsync(contract.Id)
            });
        }

        foreach (var contract in expiring7.Except(expiringToday))
        {
            await _notification.SendAsync(new ContractExpiryNotification
            {
                ContractId = contract.Id,
                Urgency = NotificationUrgency.Medium
            });
        }
    }

    // 매일 아침 9시 실행
    [Schedule("0 9 * * *")]
    public async Task DailyContractCheck() => await CheckExpiringContractsAsync();
}
```

### 2.4 계약 단가 자동 적용

```csharp
public class ContractPriceService
{
    /// <summary>
    /// 거래처/품목에 대한 유효 계약 단가 조회
    /// </summary>
    public async Task<decimal?> GetContractPriceAsync(int companyId, int productId)
    {
        var today = DateTime.Today;

        var contractItem = await _context.ContractItems
            .Include(ci => ci.Contract)
            .Where(ci => ci.Contract.CompanyId == companyId)
            .Where(ci => ci.ProductId == productId)
            .Where(ci => ci.Contract.State == ContractState.Active)
            .Where(ci => ci.Contract.StartDate <= today && ci.Contract.EndDate >= today)
            .OrderByDescending(ci => ci.Contract.StartDate)  // 최신 계약 우선
            .FirstOrDefaultAsync();

        return contractItem?.ContractPrice;
    }

    /// <summary>
    /// 발주/판매 시 계약 단가 자동 적용
    /// </summary>
    public async Task ApplyContractPricesAsync(Order order)
    {
        foreach (var item in order.Items)
        {
            var contractPrice = await GetContractPriceAsync(order.CompanyId, item.ProductId);
            if (contractPrice.HasValue)
            {
                item.UnitPrice = contractPrice.Value;
                item.PriceSource = PriceSource.Contract;
            }
        }
    }
}
```

---

## 3. 단가 관리 (Price Management)

### 3.1 데이터 모델

```csharp
public class CompanyPrice
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company Company { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; }

    public decimal UnitPrice { get; set; }
    public DateTime EffectiveDate { get; set; }

    // 단가 출처
    public PriceSource Source { get; set; }
    public int? SourceId { get; set; }  // 견적서/계약서 ID

    public bool IsActive { get; set; } = true;
}

public class PriceHistory
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public int ProductId { get; set; }

    public decimal? OldPrice { get; set; }
    public decimal NewPrice { get; set; }

    public DateTime ChangedAt { get; set; }
    public int ChangedBy { get; set; }
    public string Reason { get; set; }
}

public enum PriceSource
{
    Manual = 0,      // 수동 입력
    Quotation = 1,   // 견적서
    Contract = 2,    // 계약서
    Import = 3       // Excel 업로드
}
```

### 3.2 단가 조회 화면

```
┌─────────────────────────────────────────────────────────────────────┐
│ 단가 관리                                      [거래처: A병원 ▼]     │
├─────────────────────────────────────────────────────────────────────┤
│ 🔒 이 화면의 정보는 선택된 거래처에게만 적용됩니다                    │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ 품목명      │ 현재 단가  │ 적용일     │ 출처     │ 이력     │   │
│  ├─────────────┼────────────┼────────────┼──────────┼──────────┤   │
│  │ 테이프 10EA │ ₩3,500    │ 2025-01-15 │ 견적#123 │ [📊 5]   │   │
│  │ 거즈 1BOX   │ ₩12,000   │ 2025-01-10 │ 계약#45  │ [📊 3]   │   │
│  │ 주사기 100EA│ ₩8,000    │ 2024-12-01 │ 수동     │ [📊 8]   │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─ 단가 이력: 테이프 10EA ─────────────────────────────────────┐   │
│  │                                                               │   │
│  │  📈 단가 변동 그래프                                          │   │
│  │  ┌────────────────────────────────────────────────────────┐  │   │
│  │  │     ₩4,000 ─┐                                          │  │   │
│  │  │             └─ ₩3,800 ─┐                               │  │   │
│  │  │                        └─ ₩3,500 ──────────────────    │  │   │
│  │  │  ───────────────────────────────────────────────────   │  │   │
│  │  │  2024-10    2024-11    2025-01                         │  │   │
│  │  └────────────────────────────────────────────────────────┘  │   │
│  │                                                               │   │
│  │  일자        │ 변경 전  │ 변경 후  │ 출처     │ 변경자      │   │
│  │  ────────────┼──────────┼──────────┼──────────┼─────────────│   │
│  │  2025-01-15  │ ₩3,800   │ ₩3,500   │ 견적#123 │ 홍길동      │   │
│  │  2024-11-20  │ ₩4,000   │ ₩3,800   │ 계약#45  │ 김영희      │   │
│  │  2024-10-01  │ -        │ ₩4,000   │ 수동     │ 박철수      │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 3.3 단가 우선순위

```csharp
public class PriceResolutionService
{
    /// <summary>
    /// 거래처/품목에 대한 최종 단가 결정
    /// 우선순위: 1. 계약 > 2. 견적 > 3. 수동 > 4. 기본가
    /// </summary>
    public async Task<PriceResolution> ResolvePriceAsync(int companyId, int productId)
    {
        var today = DateTime.Today;

        // 1. 유효한 계약 단가 확인
        var contractPrice = await GetActiveContractPriceAsync(companyId, productId, today);
        if (contractPrice != null)
        {
            return new PriceResolution
            {
                UnitPrice = contractPrice.Value,
                Source = PriceSource.Contract,
                Confidence = PriceConfidence.High
            };
        }

        // 2. 거래처별 단가 확인 (견적/수동)
        var companyPrice = await _context.CompanyPrices
            .Where(p => p.CompanyId == companyId && p.ProductId == productId && p.IsActive)
            .OrderByDescending(p => p.EffectiveDate)
            .FirstOrDefaultAsync();

        if (companyPrice != null)
        {
            return new PriceResolution
            {
                UnitPrice = companyPrice.UnitPrice,
                Source = companyPrice.Source,
                Confidence = PriceConfidence.Medium
            };
        }

        // 3. 제품 기본가
        var product = await _context.Products.FindAsync(productId);
        if (product?.DefaultPrice != null)
        {
            return new PriceResolution
            {
                UnitPrice = product.DefaultPrice.Value,
                Source = PriceSource.Default,
                Confidence = PriceConfidence.Low
            };
        }

        // 4. 단가 없음
        return new PriceResolution
        {
            UnitPrice = 0,
            Source = PriceSource.None,
            Confidence = PriceConfidence.None
        };
    }
}
```

### 3.4 최소 단가 설정 (검토중 기능)

```csharp
public class MinimumPriceService
{
    /// <summary>
    /// 품목별 최소 판매가 설정
    /// </summary>
    public class ProductMinPrice
    {
        public int ProductId { get; set; }
        public decimal MinPrice { get; set; }
        public bool RequireApproval { get; set; } = true;  // 최소가 이하 시 승인 필요
    }

    /// <summary>
    /// 단가 검증
    /// </summary>
    public async Task<PriceValidationResult> ValidatePriceAsync(int productId, decimal price)
    {
        var minPrice = await _context.ProductMinPrices
            .FirstOrDefaultAsync(p => p.ProductId == productId);

        if (minPrice == null)
        {
            return PriceValidationResult.Valid();
        }

        if (price >= minPrice.MinPrice)
        {
            return PriceValidationResult.Valid();
        }

        // 최소가 이하
        return new PriceValidationResult
        {
            IsValid = false,
            RequiresApproval = minPrice.RequireApproval,
            MinPrice = minPrice.MinPrice,
            ProposedPrice = price,
            Difference = minPrice.MinPrice - price,
            Message = $"최소 판매가({minPrice.MinPrice:N0}원)보다 {minPrice.MinPrice - price:N0}원 낮습니다."
        };
    }

    /// <summary>
    /// 최소가 이하 판매 승인 요청
    /// </summary>
    public async Task<ApprovalRequest> RequestMinPriceApprovalAsync(
        int orderId,
        int productId,
        decimal proposedPrice,
        string reason)
    {
        var request = new ApprovalRequest
        {
            Type = ApprovalType.MinPriceOverride,
            ReferenceId = orderId,
            ProductId = productId,
            ProposedValue = proposedPrice,
            Reason = reason,
            RequestedBy = _currentUser.Id,
            RequestedAt = DateTime.UtcNow,
            Status = ApprovalStatus.Pending
        };

        _context.ApprovalRequests.Add(request);
        await _context.SaveChangesAsync();

        // 관리자에게 알림
        await _notification.NotifyApprovalRequestAsync(request);

        return request;
    }
}
```

### 3.5 단가 보안 (Private)

```csharp
public class PriceSecurityService
{
    /// <summary>
    /// 거래처별 단가 격리 확인
    /// </summary>
    public async Task<bool> CanAccessPriceAsync(int userId, int companyId)
    {
        var user = await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId);

        // 관리자는 모든 단가 접근 가능
        if (user.Roles.Any(r => r.Name == "Admin"))
            return true;

        // 본사 사용자는 모든 거래처 단가 접근 가능
        if (user.Roles.Any(r => r.Name == "Headquarters"))
            return true;

        // 거래처 사용자는 본인 거래처만 접근 가능
        if (user.CompanyId == companyId)
            return true;

        return false;
    }

    /// <summary>
    /// 단가 조회 시 권한 검증
    /// </summary>
    public async Task<IQueryable<CompanyPrice>> GetAccessiblePricesAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);

        if (await IsAdminOrHeadquartersAsync(userId))
        {
            // 전체 접근
            return _context.CompanyPrices;
        }
        else
        {
            // 본인 거래처만
            return _context.CompanyPrices
                .Where(p => p.CompanyId == user.CompanyId);
        }
    }
}
```

---

## 4. 품목 관리 (Product)

### 4.1 데이터 모델

```csharp
public class Product
{
    public int Id { get; set; }
    public string Code { get; set; }        // 품목 코드
    public string Name { get; set; }        // 품목명
    public string Specification { get; set; } // 규격
    public string Unit { get; set; }        // 단위 (EA, BOX, etc.)

    public int CategoryId { get; set; }
    public ProductCategory Category { get; set; }

    public decimal? DefaultPrice { get; set; }  // 기본 단가
    public decimal? MinPrice { get; set; }      // 최소 판매가

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 거래처별 품목 (자주 거래하는 품목)
/// </summary>
public class CompanyProduct
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public int ProductId { get; set; }

    public int DefaultQuantity { get; set; }   // 기본 주문 수량
    public int OrderCount { get; set; }        // 주문 횟수
    public DateTime? LastOrderedAt { get; set; } // 최근 주문일

    public bool IsActive { get; set; } = true;
    public DateTime RegisteredAt { get; set; }
    public string RegisteredFrom { get; set; } // 등록 출처 (견적서 등)
}
```

### 4.2 거래처별 품목 리스트

```
┌─────────────────────────────────────────────────────────────────────┐
│ 품목 관리 - A병원                                   [+ 품목 추가]   │
├─────────────────────────────────────────────────────────────────────┤
│ [자주 주문 ▼] [카테고리 ▼]                          [검색...]      │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ 품목명       │ 규격   │ 현재 단가 │ 기본수량 │ 주문횟수│ 상태 │   │
│  ├──────────────┼────────┼───────────┼──────────┼─────────┼──────┤   │
│  │ ⭐ 테이프 10EA│ 의료용 │ ₩3,500   │ 100     │ 45      │ 활성 │   │
│  │ ⭐ 거즈 1BOX  │ 멸균   │ ₩12,000  │ 50      │ 38      │ 활성 │   │
│  │    주사기 100EA│ 일반 │ ₩8,000   │ 20      │ 12      │ 활성 │   │
│  │    소독약 500ml│ 에탄올│ ₩5,000   │ 10      │ 8       │ 활성 │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ⭐ = 자주 주문하는 품목 (상위 30%)                                  │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 4.3 자주 거래하는 품목 조회

```csharp
public class FrequentProductService
{
    /// <summary>
    /// 거래처별 자주 거래하는 품목 조회
    /// </summary>
    public async Task<List<CompanyProductDto>> GetFrequentProductsAsync(
        int companyId,
        int limit = 10)
    {
        return await _context.CompanyProducts
            .Include(cp => cp.Product)
            .Where(cp => cp.CompanyId == companyId && cp.IsActive)
            .OrderByDescending(cp => cp.OrderCount)
            .ThenByDescending(cp => cp.LastOrderedAt)
            .Take(limit)
            .Select(cp => new CompanyProductDto
            {
                ProductId = cp.ProductId,
                ProductName = cp.Product.Name,
                Specification = cp.Product.Specification,
                Unit = cp.Product.Unit,
                DefaultQuantity = cp.DefaultQuantity,
                CurrentPrice = GetCurrentPrice(companyId, cp.ProductId),
                OrderCount = cp.OrderCount,
                LastOrderedAt = cp.LastOrderedAt,
                IsFrequent = true
            })
            .ToListAsync();
    }

    /// <summary>
    /// 주문 완료 시 통계 업데이트
    /// </summary>
    public async Task UpdateOrderStatisticsAsync(int companyId, int productId)
    {
        var companyProduct = await _context.CompanyProducts
            .FirstOrDefaultAsync(cp => cp.CompanyId == companyId && cp.ProductId == productId);

        if (companyProduct != null)
        {
            companyProduct.OrderCount++;
            companyProduct.LastOrderedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
```

# 발주/구매/판매 관리 상세 명세

> **핵심 흐름**: 발주 → 구매 → 입고 → 판매 → 출고 → 정산

---

## 1. 발주 관리 (Order Management)

### 1.1 데이터 모델

```csharp
public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; }  // OR-YYYYMMDD-XXXX

    // 발주자/수주자
    public int FromCompanyId { get; set; }   // 발주 회사 (병원)
    public Company FromCompany { get; set; }

    public int ToCompanyId { get; set; }     // 수주 회사 (도매/본사)
    public Company ToCompany { get; set; }

    // 상태
    public OrderState State { get; set; }
    public OrderType Type { get; set; }      // Personal / B2B

    // 일자
    public DateTime OrderDate { get; set; }
    public DateTime? DesiredDeliveryDate { get; set; }  // 희망 납품일
    public DateTime? ActualDeliveryDate { get; set; }   // 실제 납품일

    // 품목
    public List<OrderItem> Items { get; set; }

    // 담당자
    public int? RequestedBy { get; set; }
    public int? ApprovedBy { get; set; }

    // 메타
    public string Memo { get; set; }
    public DateTime CreatedAt { get; set; }

    // 계산
    public decimal TotalAmount => Items?.Sum(i => i.Amount) ?? 0;
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount => Quantity * UnitPrice;

    public PriceSource PriceSource { get; set; }  // 단가 출처
    public string Memo { get; set; }
}

public enum OrderState
{
    Requested = 0,       // 요청
    PendingApproval = 1, // 승인대기 (B2B)
    Approved = 2,        // 승인 (B2B)
    Completed = 3,       // 완료
    Rejected = 4,        // 반려 (B2B)
    Cancelled = 5        // 취소
}

public enum OrderType
{
    Personal = 0,  // 개인용 (승인 과정 없음)
    B2B = 1        // 교류 (승인 과정 있음)
}
```

### 1.2 상태 전이 규칙

```csharp
public class OrderStateTransitionService
{
    private static readonly Dictionary<(OrderType, OrderState), OrderState[]> _transitions = new()
    {
        // Personal (개인용) - 간단한 흐름
        { (OrderType.Personal, OrderState.Requested), new[] { OrderState.Completed, OrderState.Cancelled } },
        { (OrderType.Personal, OrderState.Completed), Array.Empty<OrderState>() },

        // B2B (교류) - 승인 과정 포함
        { (OrderType.B2B, OrderState.Requested), new[] { OrderState.PendingApproval, OrderState.Cancelled } },
        { (OrderType.B2B, OrderState.PendingApproval), new[] { OrderState.Approved, OrderState.Rejected } },
        { (OrderType.B2B, OrderState.Approved), new[] { OrderState.Completed } },
        { (OrderType.B2B, OrderState.Completed), Array.Empty<OrderState>() },
        { (OrderType.B2B, OrderState.Rejected), Array.Empty<OrderState>() },
    };

    public bool CanTransition(Order order, OrderState to)
    {
        var key = (order.Type, order.State);
        return _transitions.TryGetValue(key, out var allowed) && allowed.Contains(to);
    }

    public async Task<OrderStateLog> TransitionAsync(
        Order order,
        OrderState newState,
        int userId,
        string reason = null)
    {
        if (!CanTransition(order, newState))
            throw new InvalidStateTransitionException(order.State, newState);

        var log = new OrderStateLog
        {
            OrderId = order.Id,
            FromState = order.State,
            ToState = newState,
            UserId = userId,
            Reason = reason,
            Timestamp = DateTime.UtcNow
        };

        order.State = newState;

        // 완료 시 후속 처리
        if (newState == OrderState.Completed)
        {
            await CreatePurchaseFromOrderAsync(order);
            await UpdateProductOrderStatisticsAsync(order);
        }

        return log;
    }
}
```

### 1.3 발주 입력 화면 (3분할 레이아웃)

> **품목 선택**: 발주에서 품목 추가 시 **품목 선택 모달**을 사용합니다.
> **구매 품목만 표시**: 발주는 내가 사는 품목(📥 Buy + ↔️ Both)만 선택 가능
> **상세 명세**: `docs/features/05-product-master.md`

#### 1.3.1 거래처 탭 기반 UX

```
┌─────────────────────────────────────────────────────────────────────┐
│  [+ 거래처 선택]  [A도매 ✕]  [B본사 ✕]                               │
├─────────────────────────────────────────────────────────────────────┤
│  A도매 - 발주관리                                        [새 발주]  │
├───────────────────────────────┬─────────────────────────────────────┤
│                               │                                     │
│  📦 품목 리스트               │  ⭐ 최근거래 품목                    │
│  ─────────────────────────    │  ─────────────────────────          │
│                               │  자주 시키는 품목 (최근 30일)       │
│  [품목 검색...]               │                                     │
│                               │  ☑ 테이프 10EA   수량: [___100___]  │
│  ☐ 테이프 10EA    ₩3,500     │     단가: ₩3,500                    │
│  ☐ 거즈 1BOX      ₩12,000    │  ☑ 거즈 1BOX     수량: [____50___]  │
│  ☐ 주사기 100EA   ₩8,000     │     단가: ₩12,000                   │
│  ☐ 소독약 500ml   ₩5,000     │  ☐ 주사기 100EA  수량: [_________]  │
│  ☐ 붕대 10M       ₩2,000     │                                     │
│  ...                          │  ──────────────────────────────     │
│                               │  합계: ₩950,000                     │
│  [Excel 업로드]               │                                     │
│                               │  [임시저장]  [발주서 보내기]        │
│                               │                                     │
├───────────────────────────────┴─────────────────────────────────────┤
│                                                                     │
│  📋 최근 거래 내역                                                   │
│  ─────────────────────────────────────────────────────────────────  │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ [등록]  [최근 작업 (임시저장)]  [최근 성사 내역]             │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  (현재: [최근 작업] 탭 선택)                                        │
│  ┌───────────────────────────────────────────────────────────────┐ │
│  │ 📝 임시저장 #1                               2025-01-22 14:30 │ │
│  │    테이프 10EA × 100, 거즈 1BOX × 50                          │ │
│  │    합계: ₩950,000                      [불러오기]  [삭제]     │ │
│  ├───────────────────────────────────────────────────────────────┤ │
│  │ 📝 임시저장 #2                               2025-01-21 16:20 │ │
│  │    주사기 100EA × 200                                         │ │
│  │    합계: ₩1,600,000                    [불러오기]  [삭제]     │ │
│  └───────────────────────────────────────────────────────────────┘ │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

#### 1.3.2 하단 탭 상세

| 탭 | 설명 |
|------|------|
| **[등록]** | 새 발주서 작성 폼 (날짜, 품목 테이블, 비고) |
| **[최근 작업 (임시저장)]** | 임시저장된 발주서 목록, [불러오기]/[삭제] |
| **[최근 성사 내역]** | 완료된 발주 내역, 기간 필터, Excel 내보내기 |

#### 1.3.3 발주 흐름

```
① [+ 거래처 선택] 클릭
       ↓
② 거래처 2컬럼 리스트에서 업체명 검색 → 클릭
       ↓
③ 거래처 상세화면 (3분할) 진입
       ↓
④ 우상단 [최근거래 품목]에서 수량만 입력
       ↓
⑤ [임시저장] → 하단 [최근 작업] 탭에 저장
       ↓
⑥ [최근 작업]에서 [불러오기] → [등록] 탭으로 이동
       ↓
⑦ [발주서 보내기] → 발주 확정 → [최근 성사 내역]에 반영
```

#### 1.3.2 Excel 업로드

```csharp
public class OrderImportService
{
    /// <summary>
    /// Excel 파일에서 발주 품목 가져오기
    /// </summary>
    public async Task<ImportResult> ImportFromExcelAsync(Stream file, int toCompanyId)
    {
        var result = new ImportResult();

        using var package = new ExcelPackage(file);
        var worksheet = package.Workbook.Worksheets[0];

        int row = 2;
        while (worksheet.Cells[row, 1].Value != null)
        {
            var productName = worksheet.Cells[row, 1].GetValue<string>();
            var quantity = worksheet.Cells[row, 2].GetValue<int>();

            var product = await MatchProductAsync(productName);
            var price = await _priceService.ResolvePriceAsync(toCompanyId, product?.Id ?? 0);

            result.Items.Add(new ImportedOrderItem
            {
                ProductId = product?.Id,
                ProductName = product?.Name ?? productName,
                Quantity = quantity,
                UnitPrice = price?.UnitPrice ?? 0,
                MatchStatus = product != null ? MatchStatus.Matched : MatchStatus.Unmatched
            });

            row++;
        }

        return result;
    }
}
```

### 1.4 발주 관리 화면

```
┌─────────────────────────────────────────────────────────────────────┐
│ 발주 관리                                                [+ 신규]   │
├─────────────────────────────────────────────────────────────────────┤
│ [날짜 ▼] [업체 ▼] [품목 ▼] [상태 ▼]                    [검색...]   │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│ ┌─────────────────────────────────────────────────────────────────┐│
│ │  │ 발주번호       │ 발주처   │ 품목     │ 금액      │ 상태     ││
│ ├───┼────────────────┼──────────┼──────────┼───────────┼──────────┤│
│ │ ☐ │ OR-0122-0001  │ A도매    │ 3건      │ ₩950,000 │ ● 완료   ││
│ │ ☐ │ OR-0121-0003  │ B본사    │ 5건      │ ₩1.2M    │ ○ 승인대기││
│ │ ☐ │ OR-0121-0002  │ A도매    │ 2건      │ ₩450,000 │ ● 승인   ││
│ │ ☐ │ OR-0120-0001  │ C도매    │ 1건      │ ₩80,000  │ ✕ 반려   ││
│ └─────────────────────────────────────────────────────────────────┘│
│                                                                     │
│ 선택: 0건                           ◀ 1 2 3 ▶        [Excel 내보내기]│
└─────────────────────────────────────────────────────────────────────┘
```

### 1.5 발주 승인 조건 (Premium)

```csharp
public class OrderApprovalRuleService
{
    /// <summary>
    /// 승인 규칙 정의
    /// </summary>
    public class ApprovalRule
    {
        public int Id { get; set; }
        public ApprovalRuleType Type { get; set; }
        public string Condition { get; set; }      // JSON 조건
        public ApprovalAction Action { get; set; }
        public int? ApproverId { get; set; }       // 승인자 (null = 자동)
        public int Priority { get; set; }          // 우선순위
    }

    public enum ApprovalRuleType
    {
        ByCompany,   // 거래처별
        ByAmount,    // 금액별
        ByProduct    // 품목별
    }

    public enum ApprovalAction
    {
        AutoApprove,     // 자동 승인
        RequireApproval, // 승인 필요
        RequireManager,  // 관리자 승인 필요
        RequireCEO       // 대표 승인 필요
    }

    /// <summary>
    /// 발주에 대한 승인 요구사항 결정
    /// </summary>
    public async Task<ApprovalRequirement> DetermineApprovalAsync(Order order)
    {
        // 규칙 우선순위대로 평가
        var rules = await _context.ApprovalRules
            .OrderBy(r => r.Priority)
            .ToListAsync();

        foreach (var rule in rules)
        {
            if (await MatchesRuleAsync(order, rule))
            {
                return new ApprovalRequirement
                {
                    Action = rule.Action,
                    ApproverId = rule.ApproverId,
                    RuleId = rule.Id,
                    Reason = GetRuleDescription(rule)
                };
            }
        }

        // 기본: 자동 승인
        return new ApprovalRequirement { Action = ApprovalAction.AutoApprove };
    }

    private async Task<bool> MatchesRuleAsync(Order order, ApprovalRule rule)
    {
        return rule.Type switch
        {
            ApprovalRuleType.ByCompany => await MatchCompanyRuleAsync(order, rule),
            ApprovalRuleType.ByAmount => MatchAmountRule(order, rule),
            ApprovalRuleType.ByProduct => await MatchProductRuleAsync(order, rule),
            _ => false
        };
    }

    private bool MatchAmountRule(Order order, ApprovalRule rule)
    {
        var condition = JsonSerializer.Deserialize<AmountCondition>(rule.Condition);
        return order.TotalAmount >= condition.MinAmount
            && order.TotalAmount <= (condition.MaxAmount ?? decimal.MaxValue);
    }
}
```

### 1.6 발주 현황 (캘린더)

```csharp
public class OrderCalendarService
{
    /// <summary>
    /// 납품 예정 캘린더 데이터
    /// </summary>
    public async Task<List<DeliverySchedule>> GetDeliveryScheduleAsync(
        DateTime startDate,
        DateTime endDate)
    {
        return await _context.Orders
            .Where(o => o.State == OrderState.Completed || o.State == OrderState.Approved)
            .Where(o => o.DesiredDeliveryDate >= startDate && o.DesiredDeliveryDate <= endDate)
            .Select(o => new DeliverySchedule
            {
                Date = o.DesiredDeliveryDate.Value,
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                CompanyName = o.FromCompany.Name,
                ItemSummary = string.Join(", ", o.Items.Take(2).Select(i => i.Product.Name)),
                ItemCount = o.Items.Count,
                TotalAmount = o.TotalAmount,
                Status = o.State == OrderState.Completed ? "완료" : "예정"
            })
            .OrderBy(s => s.Date)
            .ThenBy(s => s.CompanyName)
            .ToListAsync();
    }
}
```

---

## 2. 구매 관리 (Purchase Management)

### 2.1 데이터 모델

```csharp
public class Purchase
{
    public int Id { get; set; }
    public string PurchaseNumber { get; set; }  // PU-YYYYMMDD-XXXX

    // 연결된 발주
    public int? OrderId { get; set; }
    public Order Order { get; set; }

    // 공급업체
    public int SupplierId { get; set; }
    public Company Supplier { get; set; }

    // 상태
    public PurchaseState State { get; set; }

    // 일자
    public DateTime PurchaseDate { get; set; }
    public DateTime? DeliveryDate { get; set; }    // 입고일
    public DateTime? InspectionDate { get; set; }  // 검수일

    // 품목
    public List<PurchaseItem> Items { get; set; }

    // 메타
    public string Memo { get; set; }
    public string InvoiceNumber { get; set; }  // 거래명세표 번호
    public DateTime CreatedAt { get; set; }

    // 계산
    public decimal TotalAmount => Items?.Sum(i => i.Amount) ?? 0;
}

public class PurchaseItem
{
    public int Id { get; set; }
    public int PurchaseId { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; }

    public int OrderedQuantity { get; set; }   // 주문 수량
    public int? ReceivedQuantity { get; set; } // 입고 수량
    public int? DefectQuantity { get; set; }   // 불량 수량

    public decimal UnitPrice { get; set; }
    public decimal Amount => OrderedQuantity * UnitPrice;
}

public enum PurchaseState
{
    Created = 0,         // 생성됨
    PendingDelivery = 1, // 입고대기
    Delivered = 2,       // 입고완료
    Inspected = 3,       // 검수완료
    Defective = 4        // 불량/반품
}
```

### 2.2 발주 → 구매 자동 전환

```csharp
public class PurchaseCreationService
{
    /// <summary>
    /// 발주 완료 시 구매 레코드 자동 생성
    /// </summary>
    public async Task<Purchase> CreateFromOrderAsync(Order order)
    {
        var purchase = new Purchase
        {
            PurchaseNumber = await GeneratePurchaseNumberAsync(),
            OrderId = order.Id,
            SupplierId = order.ToCompanyId,
            State = PurchaseState.Created,
            PurchaseDate = DateTime.UtcNow,
            Items = order.Items.Select(oi => new PurchaseItem
            {
                ProductId = oi.ProductId,
                OrderedQuantity = oi.Quantity,
                UnitPrice = oi.UnitPrice
            }).ToList(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // 상태 자동 전이: 생성됨 → 입고대기
        await TransitionAsync(purchase, PurchaseState.PendingDelivery);

        return purchase;
    }

    /// <summary>
    /// 수동 구매 입력 (발주 없이 직접 구매)
    /// </summary>
    public async Task<Purchase> CreateManualPurchaseAsync(PurchaseCreateDto dto)
    {
        var purchase = new Purchase
        {
            PurchaseNumber = await GeneratePurchaseNumberAsync(),
            OrderId = null,  // 발주 연결 없음
            SupplierId = dto.SupplierId,
            State = PurchaseState.Created,
            PurchaseDate = dto.PurchaseDate,
            Items = dto.Items.Select(i => new PurchaseItem
            {
                ProductId = i.ProductId,
                OrderedQuantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList(),
            Memo = "수동 입력 (발주 없음)",
            CreatedAt = DateTime.UtcNow
        };

        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        return purchase;
    }
}
```

### 2.3 입고 처리

```csharp
public class DeliveryService
{
    /// <summary>
    /// 입고 처리
    /// </summary>
    public async Task ProcessDeliveryAsync(int purchaseId, DeliveryDto dto)
    {
        var purchase = await _context.Purchases
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == purchaseId);

        // 각 품목별 입고 수량 기록
        foreach (var item in purchase.Items)
        {
            var deliveryItem = dto.Items.FirstOrDefault(d => d.ProductId == item.ProductId);
            if (deliveryItem != null)
            {
                item.ReceivedQuantity = deliveryItem.ReceivedQuantity;
                item.DefectQuantity = deliveryItem.DefectQuantity;
            }
        }

        purchase.DeliveryDate = dto.DeliveryDate;
        purchase.State = PurchaseState.Delivered;

        await _context.SaveChangesAsync();

        // 재고 자동 반영
        await _inventoryService.ProcessInboundAsync(purchase);
    }

    /// <summary>
    /// 검수 처리
    /// </summary>
    public async Task ProcessInspectionAsync(int purchaseId, InspectionDto dto)
    {
        var purchase = await _context.Purchases.FindAsync(purchaseId);

        purchase.InspectionDate = dto.InspectionDate;

        // 불량 있으면 불량 상태로
        var hasDefects = purchase.Items.Any(i => i.DefectQuantity > 0);
        purchase.State = hasDefects ? PurchaseState.Defective : PurchaseState.Inspected;

        await _context.SaveChangesAsync();

        // 불량 처리
        if (hasDefects)
        {
            await _defectService.CreateDefectRecordsAsync(purchase, dto);
        }
    }
}
```

### 2.4 구매 관리 화면

```
┌─────────────────────────────────────────────────────────────────────┐
│ 구매 관리                                                           │
├─────────────────────────────────────────────────────────────────────┤
│ [업체별 ▼] [품목별 ▼] [날짜별 ▼] [상태 ▼]              [검색...]   │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│ ┌─────────────────────────────────────────────────────────────────┐│
│ │  │ 구매번호      │ 공급업체 │ 발주연결  │ 금액      │ 상태      ││
│ ├───┼───────────────┼──────────┼───────────┼───────────┼───────────┤│
│ │ ☐ │ PU-0122-0001 │ A도매    │ OR-0121-01│ ₩950,000 │ ● 검수완료││
│ │ ☐ │ PU-0121-0002 │ B본사    │ OR-0120-03│ ₩1.2M    │ ○ 입고대기││
│ │ ☐ │ PU-0120-0001 │ A도매    │ 수동입력  │ ₩450,000 │ ⚠️ 불량   ││
│ └─────────────────────────────────────────────────────────────────┘│
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 2.5 거래명세표 자동 생성 (Premium)

```csharp
public class TransactionStatementService
{
    /// <summary>
    /// 구매 확정 시 거래명세표 자동 생성
    /// </summary>
    public async Task<TransactionStatement> CreateFromPurchaseAsync(Purchase purchase)
    {
        var statement = new TransactionStatement
        {
            StatementNumber = await GenerateStatementNumberAsync(),
            Type = StatementType.Inbound,  // 입고
            PurchaseId = purchase.Id,
            CompanyId = purchase.SupplierId,
            StatementDate = purchase.DeliveryDate ?? DateTime.UtcNow,
            State = StatementState.Created,
            Items = purchase.Items.Select(pi => new StatementItem
            {
                ProductId = pi.ProductId,
                Quantity = pi.ReceivedQuantity ?? pi.OrderedQuantity,
                UnitPrice = pi.UnitPrice
            }).ToList(),
            CreatedAt = DateTime.UtcNow
        };

        _context.TransactionStatements.Add(statement);
        await _context.SaveChangesAsync();

        return statement;
    }
}
```

---

## 3. 판매 관리 (Sales Management)

### 3.1 데이터 모델

```csharp
public class Sale
{
    public int Id { get; set; }
    public string SaleNumber { get; set; }  // SA-YYYYMMDD-XXXX

    // 고객 (병원)
    public int CustomerId { get; set; }
    public Company Customer { get; set; }

    // 연결된 발주 (있는 경우)
    public int? OrderId { get; set; }
    public Order Order { get; set; }

    // 상태
    public SaleState State { get; set; }

    // 일자
    public DateTime SaleDate { get; set; }
    public DateTime? DeliveryDate { get; set; }  // 출고일
    public DateTime? SettlementDate { get; set; } // 정산일

    // 품목
    public List<SaleItem> Items { get; set; }

    // 거래명세표
    public int? StatementId { get; set; }
    public TransactionStatement Statement { get; set; }

    // 메타
    public string Memo { get; set; }
    public DateTime CreatedAt { get; set; }

    // 계산
    public decimal TotalAmount => Items?.Sum(i => i.Amount) ?? 0;
    public decimal VatAmount => TotalAmount * 0.1m;
    public decimal GrandTotal => TotalAmount + VatAmount;
}

public class SaleItem
{
    public int Id { get; set; }
    public int SaleId { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount => Quantity * UnitPrice;

    // 출고 정보
    public int? ShippedQuantity { get; set; }
    public DateTime? ShippedAt { get; set; }
}

public enum SaleState
{
    Scheduled = 0,          // 예정
    Confirmed = 1,          // 확정 (출고완료)
    PendingSettlement = 2,  // 정산대기
    Settled = 3,            // 정산완료
    Cancelled = 4           // 취소
}
```

### 3.2 판매 자동 등록 흐름

```csharp
public class SaleAutoCreationService
{
    /// <summary>
    /// 발주 요청 시 판매 자동 등록 (재고 확인 후)
    /// </summary>
    public async Task<SaleCreationResult> ProcessIncomingOrderAsync(Order order)
    {
        var result = new SaleCreationResult { OrderId = order.Id };

        // 재고 확인
        var stockCheck = await CheckStockAvailabilityAsync(order.Items);

        if (stockCheck.AllAvailable)
        {
            // 전체 재고 있음 → 판매 자동 등록
            var sale = await CreateSaleFromOrderAsync(order);
            result.SaleId = sale.Id;
            result.Status = SaleCreationStatus.Created;
        }
        else if (stockCheck.PartiallyAvailable)
        {
            // 부분 재고 → 알림 + 부분 판매 등록
            result.Status = SaleCreationStatus.PartialStock;
            result.AvailableItems = stockCheck.AvailableItems;
            result.UnavailableItems = stockCheck.UnavailableItems;

            await _notificationService.NotifyStockShortageAsync(order, stockCheck);
        }
        else
        {
            // 재고 없음 → 알림만
            result.Status = SaleCreationStatus.NoStock;
            await _notificationService.NotifyNoStockAsync(order);
        }

        return result;
    }

    private async Task<StockCheckResult> CheckStockAvailabilityAsync(List<OrderItem> items)
    {
        var result = new StockCheckResult();

        foreach (var item in items)
        {
            var stock = await _inventoryService.GetAvailableStockAsync(item.ProductId);

            if (stock >= item.Quantity)
            {
                result.AvailableItems.Add(item);
            }
            else if (stock > 0)
            {
                result.PartialItems.Add((item, stock));
            }
            else
            {
                result.UnavailableItems.Add(item);
            }
        }

        return result;
    }
}
```

### 3.3 판매 입력 화면 (출고자 UX)

> **품목 선택**: 판매에서 품목 추가 시 **품목 선택 모달**을 사용합니다.
> **판매 품목만 표시**: 판매는 내가 파는 품목(📤 Sell + ↔️ Both)만 선택 가능
> **재고 표시**: 판매 모달에서는 현재 재고 수량이 함께 표시됩니다.
> **상세 명세**: `docs/features/05-product-master.md`

```
┌─────────────────────────────────────────────────────────────────────┐
│ 판매 등록                                              [판매 등록]  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  판매번호: SA-20250122-0001 (자동)                                  │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ 거래처: [A병원                     ▼]                       │   │
│  │ 판매일: [2025-01-22               📅]                       │   │
│  │ 출고예정일: [2025-01-22           📅]                       │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ [자주 나가는 품목 ▼]  [전체 품목 검색...]   [Excel 업로드]   │   │
│  ├─────────────────────────────────────────────────────────────┤   │
│  │ ⭐ 자주 나가는 품목                                          │   │
│  │ ┌────────────────────────────────────────────────────────┐  │   │
│  │ │ ☑ 테이프 10EA   │ 재고:500 │ 단가:₩3,500│ 수량:[__50_]│  │   │
│  │ │ ☑ 거즈 1BOX     │ 재고:200 │ 단가:₩12K  │ 수량:[__20_]│  │   │
│  │ │ ☐ 주사기 100EA  │ 재고:0⚠️ │ 단가:₩8,000│ 수량:[____]│  │   │
│  │ └────────────────────────────────────────────────────────┘  │   │
│  │                                                              │   │
│  │ 📋 추가된 품목                                               │   │
│  │ ┌───┬─────────────┬───────┬───────────┬───────┬───────────┐ │   │
│  │ │   │ 품목명      │ 재고  │ 단가      │ 수량  │ 금액      │ │   │
│  │ ├───┼─────────────┼───────┼───────────┼───────┼───────────┤ │   │
│  │ │ ✕ │ 테이프 10EA │ 500   │ ₩3,500   │ 50    │ ₩175,000 │ │   │
│  │ │ ✕ │ 거즈 1BOX   │ 200   │ ₩12,000  │ 20    │ ₩240,000 │ │   │
│  │ └───┴─────────────┴───────┴───────────┴───────┴───────────┘ │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ────────────────────────────────────────────────────────────────  │
│  공급가액: ₩415,000    부가세: ₩41,500    합계: ₩456,500           │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 3.4 거래명세표 발급

```csharp
public class SaleStatementService
{
    /// <summary>
    /// 판매 등록 후 거래명세표 발급
    /// </summary>
    public async Task<TransactionStatement> IssueSaleStatementAsync(int saleId)
    {
        var sale = await _context.Sales
            .Include(s => s.Items)
            .ThenInclude(i => i.Product)
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == saleId);

        var statement = new TransactionStatement
        {
            StatementNumber = await GenerateStatementNumberAsync(),
            Type = StatementType.Outbound,  // 출고
            SaleId = sale.Id,
            CompanyId = sale.CustomerId,
            StatementDate = DateTime.UtcNow,
            State = StatementState.Created,
            Items = sale.Items.Select(si => new StatementItem
            {
                ProductId = si.ProductId,
                ProductName = si.Product.Name,
                Specification = si.Product.Specification,
                Unit = si.Product.Unit,
                Quantity = si.Quantity,
                UnitPrice = si.UnitPrice
            }).ToList(),
            TotalAmount = sale.TotalAmount,
            VatAmount = sale.VatAmount,
            GrandTotal = sale.GrandTotal,
            CreatedAt = DateTime.UtcNow
        };

        _context.TransactionStatements.Add(statement);

        // 판매에 거래명세표 연결
        sale.StatementId = statement.Id;

        await _context.SaveChangesAsync();

        return statement;
    }

    /// <summary>
    /// 거래명세표 발송
    /// </summary>
    public async Task SendStatementAsync(int statementId, SendMethod method)
    {
        var statement = await _context.TransactionStatements
            .Include(s => s.Company)
            .FirstOrDefaultAsync(s => s.Id == statementId);

        switch (method)
        {
            case SendMethod.Email:
                await _emailService.SendStatementAsync(statement);
                break;
            case SendMethod.Print:
                await _printService.PrintStatementAsync(statement);
                break;
            case SendMethod.Fax:
                await _faxService.SendStatementAsync(statement);
                break;
        }

        statement.State = StatementState.Sent;
        statement.SentAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}
```

### 3.5 판매 상태 관리

```csharp
public class SaleStateService
{
    /// <summary>
    /// 출고 처리 (판매 확정)
    /// </summary>
    public async Task ConfirmSaleAsync(int saleId, ShippingDto dto)
    {
        var sale = await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == saleId);

        // 출고 수량 기록
        foreach (var item in sale.Items)
        {
            var shipped = dto.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
            item.ShippedQuantity = shipped?.Quantity ?? item.Quantity;
            item.ShippedAt = dto.ShippedAt;
        }

        sale.DeliveryDate = dto.ShippedAt;
        sale.State = SaleState.Confirmed;

        await _context.SaveChangesAsync();

        // 재고 출고 반영
        await _inventoryService.ProcessOutboundAsync(sale);

        // 채권 등록
        await _receivableService.CreateReceivableAsync(sale);
    }

    /// <summary>
    /// 정산 처리
    /// </summary>
    public async Task SettleSaleAsync(int saleId, PaymentDto dto)
    {
        var sale = await _context.Sales.FindAsync(saleId);

        sale.SettlementDate = dto.PaymentDate;
        sale.State = SaleState.Settled;

        await _context.SaveChangesAsync();

        // 채권 정리
        await _receivableService.SettleReceivableAsync(sale.Id, dto);
    }
}
```

### 3.6 과거 데이터 복원 (5년치)

```csharp
public class HistoricalDataImportService
{
    /// <summary>
    /// Excel/CSV 업로드로 과거 데이터 복원
    /// </summary>
    public async Task<ImportResult> ImportHistoricalDataAsync(
        Stream file,
        int companyId,
        DataType dataType)
    {
        var result = new ImportResult();

        // 1. RAW 데이터 저장
        var rawData = await SaveRawDataAsync(file, companyId, dataType);
        result.RawDataId = rawData.Id;

        // 2. 파싱 및 매핑
        var records = await ParseFileAsync(file, dataType);

        foreach (var record in records)
        {
            try
            {
                var mapped = await MapRecordAsync(record, companyId);

                if (mapped.IsValid)
                {
                    // 과거 데이터로 저장 (읽기 전용 플래그)
                    var historicalSale = new Sale
                    {
                        SaleNumber = $"HIST-{record.Date:yyyyMMdd}-{record.LineNumber}",
                        CustomerId = companyId,
                        SaleDate = record.Date,
                        State = SaleState.Settled,  // 과거 데이터는 정산완료 상태
                        IsHistorical = true,        // 과거 데이터 플래그
                        IsReadOnly = true,          // 읽기 전용
                        Items = mapped.Items,
                        CreatedAt = DateTime.UtcNow,
                        ImportedFrom = rawData.Id
                    };

                    _context.Sales.Add(historicalSale);
                    result.SuccessCount++;
                }
                else
                {
                    result.Errors.Add(new ImportError
                    {
                        LineNumber = record.LineNumber,
                        Field = mapped.ErrorField,
                        Message = mapped.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportError
                {
                    LineNumber = record.LineNumber,
                    Message = ex.Message
                });
            }
        }

        await _context.SaveChangesAsync();
        return result;
    }

    /// <summary>
    /// 과거 데이터 조회 (읽기 전용)
    /// </summary>
    public async Task<List<SaleDto>> GetHistoricalSalesAsync(
        int companyId,
        DateTime startDate,
        DateTime endDate)
    {
        return await _context.Sales
            .Where(s => s.CustomerId == companyId)
            .Where(s => s.IsHistorical)
            .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate)
            .OrderByDescending(s => s.SaleDate)
            .Select(s => new SaleDto
            {
                // ... 매핑
                IsHistorical = true,
                Label = "[과거 데이터]"
            })
            .ToListAsync();
    }
}
```

---

## 4. 일별 납품 현황

### 4.1 오늘 납품 예정 대시보드

```csharp
public class DailyDeliveryService
{
    /// <summary>
    /// 오늘 납품 예정 리스트
    /// </summary>
    public async Task<DailyDeliveryDashboard> GetTodayDeliveriesAsync()
    {
        var today = DateTime.Today;

        var deliveries = await _context.Sales
            .Include(s => s.Items)
            .ThenInclude(i => i.Product)
            .Include(s => s.Customer)
            .Where(s => s.State == SaleState.Scheduled || s.State == SaleState.Confirmed)
            .Where(s => s.DeliveryDate.HasValue && s.DeliveryDate.Value.Date == today)
            .ToListAsync();

        return new DailyDeliveryDashboard
        {
            Date = today,
            TotalCount = deliveries.Count,
            CompletedCount = deliveries.Count(d => d.State == SaleState.Confirmed),
            PendingCount = deliveries.Count(d => d.State == SaleState.Scheduled),
            Deliveries = deliveries.Select(d => new DeliveryItem
            {
                SaleId = d.Id,
                Time = d.DeliveryDate.Value.TimeOfDay,
                CustomerName = d.Customer.Name,
                CustomerAddress = d.Customer.Address,
                Items = d.Items.Select(i => new DeliveryProductItem
                {
                    ProductName = i.Product.Name,
                    Quantity = i.Quantity,
                    IsShipped = i.ShippedQuantity.HasValue
                }).ToList(),
                Status = d.State == SaleState.Confirmed ? "완료" : "대기"
            })
            .OrderBy(d => d.Time)
            .ToList()
        };
    }
}
```

### 4.2 기사/물류용 화면

```
┌─────────────────────────────────────────────────────────────────────┐
│ 📦 오늘 배송                                2025-01-22   [새로고침] │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  배송 완료: 8/15건                                                  │
│  ████████████████░░░░░░░░░░░░░░  53%                               │
│                                                                     │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ⏰ 다음 배송                                                       │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  10:00  B의원                                               │   │
│  │  ───────────────────────────────────────────────────────────│   │
│  │  📍 서울시 강남구 테헤란로 123, 2층                          │   │
│  │                                                              │   │
│  │  품목:                                                       │   │
│  │  • 주사기 100EA × 30                                        │   │
│  │  • 소독약 500ml × 10                                        │   │
│  │                                                              │   │
│  │  [🗺️ 네비게이션]          [✅ 배송완료]                      │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  📋 전체 배송 리스트                                                │
│  ┌───────┬────────────┬─────────────────────┬────────────────────┐ │
│  │ 시간  │ 병원       │ 품목               │ 상태              │ │
│  ├───────┼────────────┼─────────────────────┼────────────────────┤ │
│  │ 09:00 │ A병원      │ 테이프 100, 거즈 50│ ✅ 완료           │ │
│  │ 09:30 │ A병원      │ 주사기 200         │ ✅ 완료           │ │
│  │ 10:00 │ B의원      │ 주사기 30, 소독약 10│ 🔄 진행중        │ │
│  │ 11:00 │ C병원      │ 거즈 100           │ ⏳ 대기           │ │
│  │ 14:00 │ D의원      │ 테이프 50          │ ⏳ 대기           │ │
│  └───────┴────────────┴─────────────────────┴────────────────────┘ │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 4.3 배송 완료 처리

```csharp
public class DeliveryCompletionService
{
    /// <summary>
    /// 배송 완료 처리 (기사용)
    /// </summary>
    public async Task CompleteDeliveryAsync(int saleId, DeliveryCompletionDto dto)
    {
        var sale = await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == saleId);

        // 배송 완료 처리
        foreach (var item in sale.Items)
        {
            item.ShippedQuantity = item.Quantity;
            item.ShippedAt = DateTime.UtcNow;
        }

        sale.State = SaleState.Confirmed;
        sale.DeliveryDate = DateTime.UtcNow;

        // 수령 확인 (서명/사진)
        if (dto.SignatureImage != null)
        {
            sale.SignatureImagePath = await SaveSignatureAsync(dto.SignatureImage);
        }
        if (dto.DeliveryPhoto != null)
        {
            sale.DeliveryPhotoPath = await SavePhotoAsync(dto.DeliveryPhoto);
        }

        sale.ReceiverName = dto.ReceiverName;
        sale.DeliveryNote = dto.Note;

        await _context.SaveChangesAsync();

        // 재고 출고 반영
        await _inventoryService.ProcessOutboundAsync(sale);

        // 채권 자동 등록
        await _receivableService.CreateReceivableAsync(sale);
    }
}
```

# 재고/채권/세금계산서 관리 상세 명세

> **자동화 원칙**: 구매확정→입고, 판매확정→출고, 판매→미수금 자동 연계

---

## 1. 입출고 / 재고 관리 (Inventory Management)

### 1.1 데이터 모델

```csharp
/// <summary>
/// 품목별 현재 재고
/// </summary>
public class Inventory
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }

    // 재고 수량
    public int ConfirmedQuantity { get; set; }   // 확정 재고 (실제 보유)
    public int PendingInQuantity { get; set; }   // 예정 입고
    public int PendingOutQuantity { get; set; }  // 예정 출고

    // 계산 필드
    public int AvailableQuantity => ConfirmedQuantity + PendingInQuantity - PendingOutQuantity;

    // 안전 재고
    public int? SafetyStock { get; set; }        // 안전 재고량
    public bool IsLowStock => SafetyStock.HasValue && ConfirmedQuantity <= SafetyStock;

    public DateTime LastUpdatedAt { get; set; }
}

/// <summary>
/// 입출고 이력
/// </summary>
public class InventoryTransaction
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; }

    public TransactionType Type { get; set; }
    public int Quantity { get; set; }
    public int BalanceAfter { get; set; }  // 거래 후 잔고

    // 연결 정보
    public int? PurchaseId { get; set; }   // 입고 시
    public int? SaleId { get; set; }       // 출고 시
    public int? DefectId { get; set; }     // 불량 시

    public string Reason { get; set; }
    public DateTime TransactionDate { get; set; }
    public int UserId { get; set; }
}

public enum TransactionType
{
    Inbound = 1,         // 입고
    Outbound = 2,        // 출고
    Adjustment = 3,      // 조정
    DefectOut = 4,       // 불량 출고
    ReturnIn = 5,        // 반품 입고
    TransferIn = 6,      // 이동 입고
    TransferOut = 7      // 이동 출고
}
```

### 1.2 자동 입출고 처리

```csharp
public class InventoryService
{
    /// <summary>
    /// 구매 확정 → 입고 자동 반영
    /// </summary>
    public async Task ProcessInboundAsync(Purchase purchase)
    {
        foreach (var item in purchase.Items)
        {
            var inventory = await GetOrCreateInventoryAsync(item.ProductId);
            var quantity = item.ReceivedQuantity ?? item.OrderedQuantity;

            // 예정 입고 → 확정 재고로 전환
            inventory.PendingInQuantity -= quantity;
            inventory.ConfirmedQuantity += quantity;
            inventory.LastUpdatedAt = DateTime.UtcNow;

            // 이력 기록
            var transaction = new InventoryTransaction
            {
                ProductId = item.ProductId,
                Type = TransactionType.Inbound,
                Quantity = quantity,
                BalanceAfter = inventory.ConfirmedQuantity,
                PurchaseId = purchase.Id,
                Reason = $"구매입고 #{purchase.PurchaseNumber}",
                TransactionDate = DateTime.UtcNow,
                UserId = _currentUser.Id
            };

            _context.InventoryTransactions.Add(transaction);
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 판매 확정 → 출고 자동 반영
    /// </summary>
    public async Task ProcessOutboundAsync(Sale sale)
    {
        foreach (var item in sale.Items)
        {
            var inventory = await GetInventoryAsync(item.ProductId);
            var quantity = item.ShippedQuantity ?? item.Quantity;

            // 예정 출고 → 확정 재고에서 차감
            inventory.PendingOutQuantity -= quantity;
            inventory.ConfirmedQuantity -= quantity;
            inventory.LastUpdatedAt = DateTime.UtcNow;

            // 재고 부족 체크
            if (inventory.ConfirmedQuantity < 0)
            {
                throw new InsufficientStockException(item.ProductId, quantity);
            }

            // 이력 기록
            var transaction = new InventoryTransaction
            {
                ProductId = item.ProductId,
                Type = TransactionType.Outbound,
                Quantity = -quantity,
                BalanceAfter = inventory.ConfirmedQuantity,
                SaleId = sale.Id,
                Reason = $"판매출고 #{sale.SaleNumber}",
                TransactionDate = DateTime.UtcNow,
                UserId = _currentUser.Id
            };

            _context.InventoryTransactions.Add(transaction);
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 발주 생성 → 예정 입고 등록
    /// </summary>
    public async Task RegisterPendingInboundAsync(Order order)
    {
        foreach (var item in order.Items)
        {
            var inventory = await GetOrCreateInventoryAsync(item.ProductId);
            inventory.PendingInQuantity += item.Quantity;
            inventory.LastUpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 판매 예정 → 예정 출고 등록
    /// </summary>
    public async Task RegisterPendingOutboundAsync(Sale sale)
    {
        foreach (var item in sale.Items)
        {
            var inventory = await GetInventoryAsync(item.ProductId);
            inventory.PendingOutQuantity += item.Quantity;
            inventory.LastUpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}
```

### 1.3 재고 조회 화면

```
┌─────────────────────────────────────────────────────────────────────┐
│ 재고 관리                                              [재고 조정]  │
├─────────────────────────────────────────────────────────────────────┤
│ [카테고리 ▼] [재고상태 ▼]                              [검색...]   │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ 품목명       │ 확정재고 │ 입고예정 │ 출고예정 │ 가용재고│상태│   │
│  ├──────────────┼──────────┼──────────┼──────────┼─────────┼────┤   │
│  │ 테이프 10EA  │ 500      │ +100     │ -50      │ 550    │ ✅ │   │
│  │ 거즈 1BOX    │ 200      │ +0       │ -80      │ 120    │ ✅ │   │
│  │ 주사기 100EA │ 30       │ +200     │ -100     │ 130    │ ⚠️ │   │
│  │ 소독약 500ml │ 5        │ +0       │ -10      │ -5     │ 🔴 │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ✅ 정상  ⚠️ 안전재고 이하  🔴 재고 부족                            │
│                                                                     │
│  ┌─ 재고 이력: 테이프 10EA ─────────────────────────────────────┐   │
│  │                                                               │   │
│  │  일시          │ 유형  │ 수량   │ 잔고  │ 사유              │   │
│  │  ──────────────┼───────┼────────┼───────┼───────────────────│   │
│  │  01-22 14:30   │ 출고  │ -50    │ 500   │ 판매 SA-0122-01  │   │
│  │  01-22 10:00   │ 입고  │ +100   │ 550   │ 구매 PU-0121-02  │   │
│  │  01-21 16:00   │ 출고  │ -30    │ 450   │ 판매 SA-0121-03  │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 1.4 재고 조정

```csharp
public class InventoryAdjustmentService
{
    /// <summary>
    /// 수동 재고 조정
    /// </summary>
    public async Task AdjustInventoryAsync(AdjustmentDto dto)
    {
        var inventory = await _context.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == dto.ProductId);

        var previousQuantity = inventory.ConfirmedQuantity;
        var difference = dto.NewQuantity - previousQuantity;

        inventory.ConfirmedQuantity = dto.NewQuantity;
        inventory.LastUpdatedAt = DateTime.UtcNow;

        // 조정 이력
        var transaction = new InventoryTransaction
        {
            ProductId = dto.ProductId,
            Type = TransactionType.Adjustment,
            Quantity = difference,
            BalanceAfter = dto.NewQuantity,
            Reason = dto.Reason,
            TransactionDate = DateTime.UtcNow,
            UserId = _currentUser.Id
        };

        _context.InventoryTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        // 감사 로그
        await _auditService.LogAsync(new AuditLog
        {
            Action = "InventoryAdjustment",
            EntityType = "Inventory",
            EntityId = inventory.Id,
            OldValue = previousQuantity.ToString(),
            NewValue = dto.NewQuantity.ToString(),
            Reason = dto.Reason,
            UserId = _currentUser.Id
        });
    }
}
```

---

## 2. 불량 관리 (Defect Management)

### 2.1 데이터 모델

```csharp
public class Defect
{
    public int Id { get; set; }
    public string DefectNumber { get; set; }  // DF-YYYYMMDD-XXXX

    public int ProductId { get; set; }
    public Product Product { get; set; }

    public int Quantity { get; set; }
    public DateTime DiscoveredAt { get; set; }

    // 원인/책임
    public DefectCause Cause { get; set; }
    public DefectResponsibility Responsibility { get; set; }

    // 처리
    public DefectResolution Resolution { get; set; }
    public DefectState State { get; set; }

    // 연결
    public int? PurchaseId { get; set; }   // 입고 불량
    public int? SaleId { get; set; }       // 출고 후 반품

    // 증거
    public string PhotoPath { get; set; }
    public string Description { get; set; }

    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
}

public enum DefectCause
{
    Manufacturing = 1,  // 제조 불량
    Shipping = 2,       // 운송 파손
    Storage = 3,        // 보관 불량
    Expired = 4,        // 유효기간 만료
    Other = 99
}

public enum DefectResponsibility
{
    Supplier = 1,      // 공급업체
    Internal = 2,      // 자사
    Logistics = 3,     // 운송사
    Customer = 4       // 고객
}

public enum DefectResolution
{
    Reship = 1,        // 재출고
    Refund = 2,        // 환불
    Dispose = 3,       // 폐기
    Return = 4         // 반품
}

public enum DefectState
{
    Reported = 0,      // 등록
    Investigating = 1, // 조사중
    Resolved = 2,      // 처리완료
    Closed = 3         // 종결
}
```

### 2.2 불량 처리 서비스

```csharp
public class DefectService
{
    /// <summary>
    /// 불량 등록
    /// </summary>
    public async Task<Defect> ReportDefectAsync(DefectReportDto dto)
    {
        var defect = new Defect
        {
            DefectNumber = await GenerateDefectNumberAsync(),
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            DiscoveredAt = dto.DiscoveredAt,
            Cause = dto.Cause,
            Responsibility = dto.Responsibility,
            Resolution = dto.Resolution,
            State = DefectState.Reported,
            PurchaseId = dto.PurchaseId,
            SaleId = dto.SaleId,
            PhotoPath = dto.PhotoPath,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.Id
        };

        _context.Defects.Add(defect);
        await _context.SaveChangesAsync();

        // 재고에서 불량 수량 차감
        await _inventoryService.ProcessDefectAsync(defect);

        return defect;
    }

    /// <summary>
    /// 불량 처리 완료
    /// </summary>
    public async Task ResolveDefectAsync(int defectId, ResolutionDto dto)
    {
        var defect = await _context.Defects.FindAsync(defectId);

        defect.Resolution = dto.Resolution;
        defect.State = DefectState.Resolved;

        switch (dto.Resolution)
        {
            case DefectResolution.Reship:
                // 재출고 처리
                await CreateReshipmentAsync(defect);
                break;

            case DefectResolution.Refund:
                // 환불 처리 (채권 조정)
                await ProcessRefundAsync(defect);
                break;

            case DefectResolution.Return:
                // 공급업체 반품
                await CreateSupplierReturnAsync(defect);
                break;

            case DefectResolution.Dispose:
                // 폐기 (재고 영구 차감)
                await DisposeDefectAsync(defect);
                break;
        }

        await _context.SaveChangesAsync();
    }
}
```

### 2.3 불량 관리 화면

```
┌─────────────────────────────────────────────────────────────────────┐
│ 불량 관리                                              [+ 불량 등록]│
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ 불량번호    │ 품목       │ 수량 │ 원인    │ 책임   │ 상태   │   │
│  ├─────────────┼────────────┼──────┼─────────┼────────┼────────┤   │
│  │ DF-0122-001│ 테이프 10EA│ 10   │ 제조불량│ 공급업체│ ○ 조사중│   │
│  │ DF-0121-003│ 거즈 1BOX  │ 5    │ 운송파손│ 운송사 │ ● 처리완료│   │
│  │ DF-0120-002│ 주사기 100EA│ 20  │ 보관불량│ 자사   │ ✕ 종결  │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─ 불량 상세: DF-0122-001 ─────────────────────────────────────┐   │
│  │                                                               │   │
│  │  품목: 테이프 10EA          수량: 10개                        │   │
│  │  발견일: 2025-01-22         등록자: 홍길동                    │   │
│  │                                                               │   │
│  │  원인: 제조 불량                                              │   │
│  │  책임: 공급업체 (A도매)                                       │   │
│  │  처리방법: 반품                                               │   │
│  │                                                               │   │
│  │  설명: 포장 상태 불량, 제품 손상 발견                         │   │
│  │                                                               │   │
│  │  📷 증거 사진                                                 │   │
│  │  [이미지]  [이미지]  [이미지]                                 │   │
│  │                                                               │   │
│  │  [처리 완료]                                                  │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 3. 채권 관리 (Accounts Receivable)

### 3.1 데이터 모델

```csharp
/// <summary>
/// 채권 (미수금)
/// </summary>
public class Receivable
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Company Customer { get; set; }

    public int SaleId { get; set; }
    public Sale Sale { get; set; }

    public decimal Amount { get; set; }          // 채권 금액
    public decimal PaidAmount { get; set; }      // 입금 금액
    public decimal RemainingAmount => Amount - PaidAmount;

    public DateTime DueDate { get; set; }        // 결제 기한
    public DateTime? PaidDate { get; set; }      // 완납일

    public ReceivableState State { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 입금 내역
/// </summary>
public class Payment
{
    public int Id { get; set; }

    public int ReceivableId { get; set; }
    public Receivable Receivable { get; set; }

    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }

    public DateTime PaymentDate { get; set; }
    public string Reference { get; set; }        // 입금 참조번호

    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
}

public enum ReceivableState
{
    Active = 0,         // 미수금 (정상)
    Overdue = 1,        // 연체
    PartiallyPaid = 2,  // 부분입금
    Paid = 3,           // 완납
    Written = 4         // 대손처리
}

public enum PaymentMethod
{
    BankTransfer = 1,   // 계좌이체
    Cash = 2,           // 현금
    Card = 3,           // 카드
    Check = 4           // 수표
}
```

### 3.2 채권 자동 생성

```csharp
public class ReceivableService
{
    /// <summary>
    /// 판매 확정 시 채권 자동 생성
    /// </summary>
    public async Task CreateReceivableAsync(Sale sale)
    {
        // 결제 기한 계산 (기본 30일)
        var paymentTerms = await GetPaymentTermsAsync(sale.CustomerId);
        var dueDate = sale.SaleDate.AddDays(paymentTerms.DueDays);

        var receivable = new Receivable
        {
            CustomerId = sale.CustomerId,
            SaleId = sale.Id,
            Amount = sale.GrandTotal,
            PaidAmount = 0,
            DueDate = dueDate,
            State = ReceivableState.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.Receivables.Add(receivable);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 입금 처리
    /// </summary>
    public async Task ProcessPaymentAsync(PaymentDto dto)
    {
        var receivable = await _context.Receivables
            .FirstOrDefaultAsync(r => r.Id == dto.ReceivableId);

        // 입금 기록
        var payment = new Payment
        {
            ReceivableId = dto.ReceivableId,
            Amount = dto.Amount,
            Method = dto.Method,
            PaymentDate = dto.PaymentDate,
            Reference = dto.Reference,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.Id
        };

        _context.Payments.Add(payment);

        // 채권 갱신
        receivable.PaidAmount += dto.Amount;

        if (receivable.RemainingAmount <= 0)
        {
            // 완납
            receivable.State = ReceivableState.Paid;
            receivable.PaidDate = dto.PaymentDate;

            // 판매 상태 변경
            var sale = await _context.Sales.FindAsync(receivable.SaleId);
            sale.State = SaleState.Settled;
            sale.SettlementDate = dto.PaymentDate;
        }
        else
        {
            // 부분입금
            receivable.State = ReceivableState.PartiallyPaid;
        }

        await _context.SaveChangesAsync();
    }
}
```

### 3.3 연체 알림 서비스

```csharp
public class OverdueAlertService
{
    /// <summary>
    /// 연체 체크 및 알림 발송 (매일 실행)
    /// </summary>
    [Schedule("0 9 * * *")]
    public async Task CheckOverdueReceivablesAsync()
    {
        var today = DateTime.Today;

        // 상태 업데이트: 기한 경과 → 연체
        var overdueItems = await _context.Receivables
            .Where(r => r.State == ReceivableState.Active || r.State == ReceivableState.PartiallyPaid)
            .Where(r => r.DueDate < today)
            .ToListAsync();

        foreach (var receivable in overdueItems)
        {
            receivable.State = ReceivableState.Overdue;
        }

        await _context.SaveChangesAsync();

        // 알림 발송
        await SendOverdueAlertsAsync(overdueItems);
    }

    private async Task SendOverdueAlertsAsync(List<Receivable> overdueItems)
    {
        var groupedByCustomer = overdueItems.GroupBy(r => r.CustomerId);

        foreach (var group in groupedByCustomer)
        {
            var customer = await _context.Companies.FindAsync(group.Key);
            var totalOverdue = group.Sum(r => r.RemainingAmount);
            var maxOverdueDays = group.Max(r => (DateTime.Today - r.DueDate).Days);

            var urgency = maxOverdueDays switch
            {
                >= 30 => NotificationUrgency.Critical,
                >= 7 => NotificationUrgency.High,
                _ => NotificationUrgency.Medium
            };

            await _notificationService.SendAsync(new OverdueNotification
            {
                CustomerId = group.Key,
                CustomerName = customer.Name,
                TotalAmount = totalOverdue,
                ItemCount = group.Count(),
                MaxOverdueDays = maxOverdueDays,
                Urgency = urgency
            });
        }
    }
}
```

### 3.4 채권 현황 화면

```
┌─────────────────────────────────────────────────────────────────────┐
│ 채권 관리                                                           │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  전체 미수금: ₩45,230,000    연체금액: ₩7,100,000                   │
│                                                                     │
├─────────────────────────────────────────────────────────────────────┤
│ [거래처 ▼] [상태 ▼] [기간 ▼]                          [검색...]    │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ 거래처    │ 미수금      │ 연체금액    │ 연체일 │ 상태       │   │
│  ├───────────┼─────────────┼─────────────┼────────┼────────────┤   │
│  │ A병원     │ ₩12,500,000 │ ₩0          │ -      │ ✅ 정상    │   │
│  │ B의원     │ ₩8,730,000  │ ₩2,100,000  │ 15일   │ ⚠️ 연체   │   │
│  │ C병원     │ ₩24,000,000 │ ₩5,000,000  │ 45일   │ 🔴 위험   │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─ 상세: B의원 ─────────────────────────────────────────────────┐ │
│  │                                                                │ │
│  │  미수 내역                                                     │ │
│  │  ┌───────────────┬────────────┬────────────┬─────────┬──────┐ │ │
│  │  │ 판매번호      │ 금액       │ 기한       │ 연체일  │ 상태 │ │ │
│  │  ├───────────────┼────────────┼────────────┼─────────┼──────┤ │ │
│  │  │ SA-0107-0012  │ ₩2,100,000│ 2025-01-07 │ 15일    │ 🔴   │ │ │
│  │  │ SA-0115-0008  │ ₩3,500,000│ 2025-02-15 │ -       │ ✅   │ │ │
│  │  │ SA-0120-0003  │ ₩3,130,000│ 2025-02-20 │ -       │ ✅   │ │ │
│  │  └───────────────┴────────────┴────────────┴─────────┴──────┘ │ │
│  │                                                                │ │
│  │  입금 내역                                                     │ │
│  │  ┌────────────┬────────────┬────────────┬────────────────────┐│ │
│  │  │ 일자       │ 금액       │ 방법       │ 참조              ││ │
│  │  ├────────────┼────────────┼────────────┼────────────────────┤│ │
│  │  │ 2025-01-20 │ ₩1,500,000│ 계좌이체   │ 국민 123-456      ││ │
│  │  └────────────┴────────────┴────────────┴────────────────────┘│ │
│  │                                                                │ │
│  │  [입금 등록]                                                   │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 4. 세금계산서 (Tax Invoice)

### 4.1 데이터 모델

```csharp
public class TaxInvoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; }  // 세금계산서 번호

    public int CustomerId { get; set; }
    public Company Customer { get; set; }

    // 공급자 정보
    public string SupplierBusinessNumber { get; set; }
    public string SupplierName { get; set; }
    public string SupplierRepresentative { get; set; }
    public string SupplierAddress { get; set; }

    // 공급받는자 정보
    public string CustomerBusinessNumber { get; set; }
    public string CustomerName { get; set; }
    public string CustomerRepresentative { get; set; }
    public string CustomerAddress { get; set; }

    // 금액
    public decimal SupplyAmount { get; set; }   // 공급가액
    public decimal TaxAmount { get; set; }      // 세액
    public decimal TotalAmount { get; set; }    // 합계

    // 품목
    public List<TaxInvoiceItem> Items { get; set; }

    // 기간
    public DateTime IssueDate { get; set; }     // 발행일
    public DateTime? SupplyDate { get; set; }   // 공급일 (작성일)
    public int TaxPeriodYear { get; set; }      // 과세기간 연도
    public int TaxPeriodMonth { get; set; }     // 과세기간 월

    // 상태
    public TaxInvoiceState State { get; set; }
    public DateTime? TransmittedAt { get; set; }  // 국세청 전송일
    public string TransmissionResult { get; set; }

    // 연결
    public List<int> SaleIds { get; set; }      // 연결된 판매 건

    public DateTime CreatedAt { get; set; }
}

public class TaxInvoiceItem
{
    public int Id { get; set; }
    public int TaxInvoiceId { get; set; }

    public DateTime SupplyDate { get; set; }   // 공급일자
    public string ProductName { get; set; }    // 품목
    public string Specification { get; set; }  // 규격
    public int Quantity { get; set; }          // 수량
    public decimal UnitPrice { get; set; }     // 단가
    public decimal SupplyAmount { get; set; }  // 공급가액
    public decimal TaxAmount { get; set; }     // 세액
}

public enum TaxInvoiceState
{
    Draft = 0,         // 작성
    Issued = 1,        // 발행
    Transmitted = 2,   // 국세청 전송완료
    Cancelled = 3      // 취소
}
```

### 4.2 자동 생성 서비스

```csharp
public class TaxInvoiceService
{
    /// <summary>
    /// 판매 건별 세금계산서 발행
    /// </summary>
    public async Task<TaxInvoice> IssueSingleInvoiceAsync(int saleId)
    {
        var sale = await _context.Sales
            .Include(s => s.Items)
            .ThenInclude(i => i.Product)
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == saleId);

        return await CreateInvoiceAsync(sale.Customer, new[] { sale });
    }

    /// <summary>
    /// 월별 합산 세금계산서 발행
    /// </summary>
    public async Task<TaxInvoice> IssueMonthlyInvoiceAsync(
        int customerId,
        int year,
        int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var sales = await _context.Sales
            .Include(s => s.Items)
            .ThenInclude(i => i.Product)
            .Include(s => s.Customer)
            .Where(s => s.CustomerId == customerId)
            .Where(s => s.State == SaleState.Confirmed || s.State == SaleState.PendingSettlement)
            .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate)
            .Where(s => !s.TaxInvoiceIssued)  // 미발행 건만
            .ToListAsync();

        if (!sales.Any())
            throw new NoSalesForInvoiceException(customerId, year, month);

        var customer = await _context.Companies.FindAsync(customerId);
        return await CreateInvoiceAsync(customer, sales);
    }

    private async Task<TaxInvoice> CreateInvoiceAsync(Company customer, IEnumerable<Sale> sales)
    {
        var supplierInfo = await GetSupplierInfoAsync();

        var items = sales.SelectMany(s => s.Items.Select(i => new TaxInvoiceItem
        {
            SupplyDate = s.SaleDate,
            ProductName = i.Product.Name,
            Specification = i.Product.Specification,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            SupplyAmount = i.Amount,
            TaxAmount = i.Amount * 0.1m
        })).ToList();

        var invoice = new TaxInvoice
        {
            InvoiceNumber = await GenerateInvoiceNumberAsync(),
            CustomerId = customer.Id,

            // 공급자
            SupplierBusinessNumber = supplierInfo.BusinessNumber,
            SupplierName = supplierInfo.Name,
            SupplierRepresentative = supplierInfo.Representative,
            SupplierAddress = supplierInfo.Address,

            // 공급받는자
            CustomerBusinessNumber = customer.BusinessNumber,
            CustomerName = customer.Name,
            CustomerRepresentative = customer.Representative,
            CustomerAddress = customer.Address,

            // 금액
            SupplyAmount = items.Sum(i => i.SupplyAmount),
            TaxAmount = items.Sum(i => i.TaxAmount),
            TotalAmount = items.Sum(i => i.SupplyAmount + i.TaxAmount),

            Items = items,

            IssueDate = DateTime.UtcNow,
            SupplyDate = sales.Min(s => s.SaleDate),
            TaxPeriodYear = DateTime.Now.Year,
            TaxPeriodMonth = DateTime.Now.Month,

            State = TaxInvoiceState.Issued,
            SaleIds = sales.Select(s => s.Id).ToList(),

            CreatedAt = DateTime.UtcNow
        };

        _context.TaxInvoices.Add(invoice);

        // 판매 건 발행 처리
        foreach (var sale in sales)
        {
            sale.TaxInvoiceIssued = true;
        }

        await _context.SaveChangesAsync();

        return invoice;
    }
}
```

### 4.3 외부 연동 (Premium)

```csharp
public class TaxInvoiceTransmissionService
{
    private readonly IHomeTaxApiClient _homeTaxClient;

    /// <summary>
    /// 국세청 홈택스 전송
    /// </summary>
    public async Task TransmitToHomeTaxAsync(int invoiceId)
    {
        var invoice = await _context.TaxInvoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        try
        {
            // 홈택스 API 호출
            var result = await _homeTaxClient.TransmitAsync(new HomeTaxInvoiceRequest
            {
                InvoiceNumber = invoice.InvoiceNumber,
                IssueDate = invoice.IssueDate,
                SupplierInfo = new SupplierInfo
                {
                    BusinessNumber = invoice.SupplierBusinessNumber,
                    Name = invoice.SupplierName,
                    Representative = invoice.SupplierRepresentative
                },
                CustomerInfo = new CustomerInfo
                {
                    BusinessNumber = invoice.CustomerBusinessNumber,
                    Name = invoice.CustomerName,
                    Representative = invoice.CustomerRepresentative
                },
                Amount = new AmountInfo
                {
                    SupplyAmount = invoice.SupplyAmount,
                    TaxAmount = invoice.TaxAmount,
                    TotalAmount = invoice.TotalAmount
                },
                Items = invoice.Items.Select(i => new ItemInfo
                {
                    Date = i.SupplyDate,
                    Name = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Amount = i.SupplyAmount
                }).ToList()
            });

            invoice.State = TaxInvoiceState.Transmitted;
            invoice.TransmittedAt = DateTime.UtcNow;
            invoice.TransmissionResult = result.ConfirmationNumber;
        }
        catch (HomeTaxApiException ex)
        {
            invoice.TransmissionResult = $"실패: {ex.ErrorCode} - {ex.Message}";
            throw;
        }

        await _context.SaveChangesAsync();
    }
}
```

### 4.4 세금계산서 관리 화면

```
┌─────────────────────────────────────────────────────────────────────┐
│ 세금계산서 관리                                                      │
├─────────────────────────────────────────────────────────────────────┤
│ 2025년 1월    [◀ 이전]  [다음 ▶]                [월별 합산 발행]    │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ 거래처    │ 공급가액     │ 세액        │ 상태     │ 발행    │   │
│  ├───────────┼──────────────┼─────────────┼──────────┼─────────┤   │
│  │ A병원     │ ₩10,000,000  │ ₩1,000,000  │ ✅ 전송완료│ [보기] │   │
│  │ B의원     │ ₩5,500,000   │ ₩550,000    │ ● 발행   │ [전송] │   │
│  │ C병원     │ ₩8,200,000   │ ₩820,000    │ ○ 미발행 │ [발행] │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─ 세금계산서 미리보기 ─────────────────────────────────────────┐ │
│  │                                                                │ │
│  │  ┌────────────────────────────────────────────────────────┐   │ │
│  │  │                    전 자 세 금 계 산 서                 │   │ │
│  │  ├────────────────────────────────────────────────────────┤   │ │
│  │  │  공급자                     공급받는자                  │   │ │
│  │  │  사업자: 123-45-67890       사업자: 987-65-43210       │   │ │
│  │  │  상호: (주)본사             상호: A병원                 │   │ │
│  │  │  대표: 홍길동               대표: 김영희                │   │ │
│  │  ├────────────────────────────────────────────────────────┤   │ │
│  │  │  작성일: 2025-01-31         공급가액: ₩10,000,000      │   │ │
│  │  │                             세    액: ₩1,000,000       │   │ │
│  │  │                             합    계: ₩11,000,000      │   │ │
│  │  ├────────────────────────────────────────────────────────┤   │ │
│  │  │  No│ 월일 │ 품목        │ 수량│ 단가    │ 공급가액    │   │ │
│  │  │  1 │ 01-15│ 테이프 10EA │ 100│ ₩3,500 │ ₩350,000   │   │ │
│  │  │  2 │ 01-15│ 거즈 1BOX   │ 50 │ ₩12,000│ ₩600,000   │   │ │
│  │  │  ...                                                    │   │ │
│  │  └────────────────────────────────────────────────────────┘   │ │
│  │                                                                │ │
│  │  [인쇄]  [이메일 발송]  [홈택스 전송]                          │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 5. 정산 현황 종합

### 5.1 정산 대시보드

```csharp
public class SettlementDashboardService
{
    public async Task<SettlementDashboard> GetDashboardAsync(int? companyId = null)
    {
        var query = _context.Receivables.AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(r => r.CustomerId == companyId);
        }

        var receivables = await query.ToListAsync();

        return new SettlementDashboard
        {
            // 미수금 현황
            TotalReceivables = receivables.Sum(r => r.RemainingAmount),
            OverdueAmount = receivables
                .Where(r => r.State == ReceivableState.Overdue)
                .Sum(r => r.RemainingAmount),

            // 연체 분석
            Overdue7Days = receivables.Count(r => GetOverdueDays(r) > 0 && GetOverdueDays(r) <= 7),
            Overdue30Days = receivables.Count(r => GetOverdueDays(r) > 7 && GetOverdueDays(r) <= 30),
            Overdue90Days = receivables.Count(r => GetOverdueDays(r) > 30),

            // 거래처별 TOP 5
            TopCustomers = await query
                .GroupBy(r => r.CustomerId)
                .Select(g => new CustomerReceivable
                {
                    CustomerId = g.Key,
                    TotalAmount = g.Sum(r => r.RemainingAmount)
                })
                .OrderByDescending(c => c.TotalAmount)
                .Take(5)
                .ToListAsync(),

            // 월별 추이
            MonthlyTrend = await GetMonthlyTrendAsync(companyId)
        };
    }

    private int GetOverdueDays(Receivable r)
    {
        if (r.State == ReceivableState.Paid) return 0;
        return Math.Max(0, (DateTime.Today - r.DueDate).Days);
    }
}
```

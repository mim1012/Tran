# 품목 마스터 관리 상세 명세

> **핵심 원칙**: 내 품목 등록 (사는 품목 / 파는 품목) → 모달에서 선택 → 발주/견적/재고 공통 적용

---

## 1. 품목 관리 개요

### 1.1 품목 관리 구조

```
┌─────────────────────────────────────────────────────────────────────┐
│                        품목 관리 시스템                              │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                    내 품목 등록 (품목 마스터)                  │   │
│  │  ┌─────────────────────┐  ┌─────────────────────┐           │   │
│  │  │  📥 구매 품목        │  │  📤 판매 품목        │           │   │
│  │  │  (사는 품목)         │  │  (파는 품목)         │           │   │
│  │  └──────────┬──────────┘  └──────────┬──────────┘           │   │
│  └─────────────┼────────────────────────┼───────────────────────┘   │
│                │                        │                           │
│                ▼                        ▼                           │
│  ┌─────────────────────┐  ┌─────────────────────────────────────┐  │
│  │  품목 선택 모달      │  │  품목 선택 모달                      │  │
│  │  (구매 품목용)       │  │  (판매 품목용)                       │  │
│  └──────────┬──────────┘  └──────────┬──────────────────────────┘  │
│             │                        │                              │
│             ▼                        ├──────────────┬───────────┐  │
│       ┌──────────┐            ┌──────────┐  ┌──────────┐  ┌─────┐  │
│       │   발주   │            │   판매   │  │  견적서  │  │재고 │  │
│       │ (사는것) │            │ (파는것) │  │  (파는것)│  │     │  │
│       └──────────┘            └──────────┘  └──────────┘  └─────┘  │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 1.2 핵심 원칙

| 원칙 | 설명 |
|------|------|
| **사전 등록 필수** | 발주/견적/재고에 사용할 품목은 먼저 품목 마스터에 등록 |
| **구매/판매 구분** | 품목은 **구매품목(사는것)** 과 **판매품목(파는것)** 으로 구분 |
| **모달 선택** | 품목 입력 시 모달창에서 등록된 품목 검색/선택 |
| **공통 적용** | 발주→구매품목, 판매/견적→판매품목 자동 필터 |
| **업체별 관리** | 각 업체가 자사 품목을 독립적으로 관리 |

### 1.3 품목 유형별 사용처

| 품목 유형 | 용도 | 사용 화면 |
|-----------|------|-----------|
| **구매 품목** (Buy) | 다른 업체에서 사는 품목 | 발주 |
| **판매 품목** (Sell) | 다른 업체에 파는 품목 | 판매, 견적서, 재고 |
| **구매+판매** (Both) | 사기도 하고 팔기도 하는 품목 | 모든 화면 |

---

## 2. 품목 마스터 데이터 모델

### 2.1 엔티티 정의

```csharp
/// <summary>
/// 품목 거래 유형 (구매/판매)
/// </summary>
public enum ProductTransactionType
{
    Buy,    // 구매 품목 (사는 품목) - 발주에서 사용
    Sell,   // 판매 품목 (파는 품목) - 판매/견적서에서 사용
    Both    // 구매+판매 (사기도 팔기도) - 모든 화면에서 사용
}

/// <summary>
/// 품목 마스터 (내가 사고 파는 품목)
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Code { get; set; }            // 품목 코드 (사용자 정의)
    public string Name { get; set; }            // 품목명
    public string Specification { get; set; }   // 규격
    public string Unit { get; set; }            // 단위 (EA, BOX, SET 등)

    // ★ 거래 유형 (구매/판매/둘다)
    public ProductTransactionType TransactionType { get; set; }

    // 카테고리
    public int? CategoryId { get; set; }
    public ProductCategory Category { get; set; }

    // 가격 (판매 품목용)
    public decimal? DefaultPrice { get; set; }  // 기본 판매 단가
    public decimal? MinPrice { get; set; }      // 최소 판매가

    // 가격 (구매 품목용)
    public decimal? PurchasePrice { get; set; } // 기본 구매 단가

    // 내부용
    public decimal? CostPrice { get; set; }     // 원가 (내부용)

    // 재고 설정 (판매 품목 / Both 품목)
    public int? SafetyStock { get; set; }       // 안전 재고량
    public bool TrackInventory { get; set; }    // 재고 추적 여부

    // 소속 업체 (누가 등록했는지)
    public int CompanyId { get; set; }
    public Company Company { get; set; }

    // 연관 거래처 (선택적)
    public int? LinkedCompanyId { get; set; }   // 주로 거래하는 업체 (구매처/판매처)
    public Company LinkedCompany { get; set; }

    // 상태
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // 추가 정보
    public string Barcode { get; set; }         // 바코드
    public string Manufacturer { get; set; }    // 제조사
    public string Description { get; set; }     // 설명
    public string ImagePath { get; set; }       // 이미지
}

/// <summary>
/// 품목 카테고리
/// </summary>
public class ProductCategory
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int? ParentId { get; set; }
    public ProductCategory Parent { get; set; }
    public List<ProductCategory> Children { get; set; }
    public int CompanyId { get; set; }
    public int SortOrder { get; set; }
}
```

### 2.2 품목 단위 (Unit)

```csharp
public static class ProductUnits
{
    public static readonly string[] StandardUnits = new[]
    {
        "EA",       // 개
        "BOX",      // 박스
        "SET",      // 세트
        "PACK",     // 팩
        "ROLL",     // 롤
        "BOTTLE",   // 병
        "TUBE",     // 튜브
        "BAG",      // 봉지
        "CASE",     // 케이스
        "KG",       // 킬로그램
        "L",        // 리터
        "M",        // 미터
    };
}
```

---

## 3. 품목 관리 화면

### 3.1 품목 리스트 화면

```
┌─────────────────────────────────────────────────────────────────────┐
│  품목 관리                                           [+ 품목 등록]  │
├─────────────────────────────────────────────────────────────────────┤
│  [전체▼] [📥구매품목] [📤판매품목] [↔️둘다]  [카테고리▼] [🔍 검색...] │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌───────────────────────────────────────────────────────────────┐ │
│  │   │유형│ 코드   │ 품목명      │ 규격  │ 단위│ 단가   │ 상태  │ │
│  ├───┼────┼────────┼─────────────┼───────┼─────┼────────┼───────┤ │
│  │ ☐ │📥 │ P-001  │ 원재료A     │ 대용량│ KG  │ ₩50,000│ 활성  │ │
│  │ ☐ │📤 │ P-002  │ 테이프 10EA │ 의료용│ EA  │ ₩3,500 │ 활성  │ │
│  │ ☐ │📤 │ P-003  │ 거즈 1BOX   │ 멸균  │ BOX │ ₩12,000│ 활성  │ │
│  │ ☐ │↔️ │ P-004  │ 주사기 100EA│ 일반  │ EA  │ ₩8,000 │ 활성  │ │
│  │ ☐ │📤 │ P-005  │ 소독약 500ml│ 에탄올│BOTTLE│ ₩5,000│ 활성  │ │
│  └───────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  📥 구매: 45건 │ 📤 판매: 98건 │ ↔️ 둘다: 13건 │ 총 156건          │
│                                              ◀ 1 2 3 ▶ [Excel 내보내기]│
├─────────────────────────────────────────────────────────────────────┤
│  [일괄 수정]  [일괄 삭제]  [Excel 업로드]                            │
└─────────────────────────────────────────────────────────────────────┘
```

**아이콘 범례:**
| 아이콘 | 거래 유형 | 설명 |
|--------|-----------|------|
| 📥 | 구매 품목 (Buy) | 다른 업체에서 사는 품목 → 발주에서 사용 |
| 📤 | 판매 품목 (Sell) | 다른 업체에 파는 품목 → 판매/견적서에서 사용 |
| ↔️ | 둘다 (Both) | 사기도 팔기도 하는 품목 → 모든 화면에서 사용 |

### 3.2 품목 등록/수정 모달

```
┌─────────────────────────────────────────────────────────────────────┐
│  품목 등록                                                    [✕]  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─ 거래 유형 ─────────────────────────────────────────────────┐  │
│  │                                                               │  │
│  │  이 품목은?  (○) 📥 구매 품목 (사는것)                       │  │
│  │              (●) 📤 판매 품목 (파는것)                       │  │
│  │              (○) ↔️  둘다 (사기도 팔기도)                     │  │
│  │                                                               │  │
│  │  💡 구매 품목: 발주에서 사용                                  │  │
│  │     판매 품목: 판매, 견적서, 재고에서 사용                    │  │
│  │                                                               │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌─ 기본 정보 ───────────────────────────────────────────────────┐ │
│  │                                                                │ │
│  │  품목코드: [P-006        ]  (자동생성 / 직접입력)             │ │
│  │  품목명:   [                                              ] * │ │
│  │  규격:     [                                              ]   │ │
│  │  단위:     [EA ▼]                                           * │ │
│  │  카테고리: [의료소모품 ▼]                                     │ │
│  │                                                                │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  ┌─ 가격 정보 ───────────────────────────────────────────────────┐ │
│  │                                                                │ │
│  │  [판매 품목인 경우]                                           │ │
│  │  기본 판매가:  [₩           ]  (거래처별 단가 미설정 시 적용) │ │
│  │  최소 판매가:  [₩           ]  (이 금액 이하 판매 시 경고)    │ │
│  │                                                                │ │
│  │  [구매 품목인 경우]                                           │ │
│  │  기본 구매가:  [₩           ]  (발주 시 기본 적용 단가)       │ │
│  │                                                                │ │
│  │  [공통]                                                        │ │
│  │  원가:        [₩           ]  (내부 참고용, 외부 노출 안함)   │ │
│  │                                                                │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  ┌─ 연관 거래처 (선택) ──────────────────────────────────────────┐ │
│  │                                                                │ │
│  │  주 거래처:  [A도매 ▼]  (이 품목을 주로 사거나 파는 업체)     │ │
│  │                                                                │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  ┌─ 재고 설정 (판매 품목만) ─────────────────────────────────────┐ │
│  │                                                                │ │
│  │  ☑ 재고 추적     안전 재고: [        ] 개                     │ │
│  │                                                                │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  ┌─ 추가 정보 ───────────────────────────────────────────────────┐ │
│  │                                                                │ │
│  │  바코드:   [                    ]                              │ │
│  │  제조사:   [                    ]                              │ │
│  │  설명:     [                                              ]   │ │
│  │  이미지:   [파일 선택]  product_image.jpg                     │ │
│  │                                                                │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│                                        [취소]  [저장]               │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 3.3 품목 관리 서비스

```csharp
public class ProductService
{
    /// <summary>
    /// 품목 등록
    /// </summary>
    public async Task<Product> CreateProductAsync(ProductCreateDto dto)
    {
        // 품목코드 중복 체크
        if (await _context.Products.AnyAsync(p =>
            p.CompanyId == _currentCompanyId && p.Code == dto.Code))
        {
            throw new DuplicateProductCodeException(dto.Code);
        }

        var product = new Product
        {
            Code = dto.Code ?? await GenerateProductCodeAsync(),
            Name = dto.Name,
            Specification = dto.Specification,
            Unit = dto.Unit,
            CategoryId = dto.CategoryId,
            DefaultPrice = dto.DefaultPrice,
            MinPrice = dto.MinPrice,
            CostPrice = dto.CostPrice,
            SafetyStock = dto.SafetyStock,
            TrackInventory = dto.TrackInventory,
            CompanyId = _currentCompanyId,
            Barcode = dto.Barcode,
            Manufacturer = dto.Manufacturer,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // 재고 추적 시 초기 재고 레코드 생성
        if (product.TrackInventory)
        {
            await _inventoryService.InitializeInventoryAsync(product.Id);
        }

        return product;
    }

    /// <summary>
    /// 품목 수정
    /// </summary>
    public async Task<Product> UpdateProductAsync(int id, ProductUpdateDto dto)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == _currentCompanyId);

        if (product == null)
            throw new ProductNotFoundException(id);

        product.Name = dto.Name;
        product.Specification = dto.Specification;
        product.Unit = dto.Unit;
        product.CategoryId = dto.CategoryId;
        product.DefaultPrice = dto.DefaultPrice;
        product.MinPrice = dto.MinPrice;
        product.CostPrice = dto.CostPrice;
        product.SafetyStock = dto.SafetyStock;
        product.TrackInventory = dto.TrackInventory;
        product.Barcode = dto.Barcode;
        product.Manufacturer = dto.Manufacturer;
        product.Description = dto.Description;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return product;
    }

    /// <summary>
    /// 품목 삭제 (비활성화)
    /// </summary>
    public async Task DeactivateProductAsync(int id)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == _currentCompanyId);

        if (product == null)
            throw new ProductNotFoundException(id);

        // 사용 중인 품목인지 확인
        var inUse = await IsProductInUseAsync(id);
        if (inUse)
        {
            // 비활성화만 (삭제 불가)
            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // 완전 삭제 가능
            _context.Products.Remove(product);
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 품목 검색 (모달용) - 거래 유형별 필터링
    /// </summary>
    /// <param name="searchTerm">검색어</param>
    /// <param name="transactionType">거래 유형 (Buy=발주용, Sell=판매/견적용)</param>
    /// <param name="categoryId">카테고리 필터</param>
    /// <param name="limit">최대 결과 수</param>
    public async Task<List<ProductSearchResult>> SearchProductsAsync(
        string searchTerm,
        ProductTransactionType? transactionType = null,
        int? categoryId = null,
        int limit = 20)
    {
        var query = _context.Products
            .Where(p => p.CompanyId == _currentCompanyId && p.IsActive);

        // ★ 거래 유형 필터링
        if (transactionType.HasValue)
        {
            // Buy → Buy 또는 Both 품목만
            // Sell → Sell 또는 Both 품목만
            query = transactionType.Value switch
            {
                ProductTransactionType.Buy => query.Where(p =>
                    p.TransactionType == ProductTransactionType.Buy ||
                    p.TransactionType == ProductTransactionType.Both),
                ProductTransactionType.Sell => query.Where(p =>
                    p.TransactionType == ProductTransactionType.Sell ||
                    p.TransactionType == ProductTransactionType.Both),
                _ => query
            };
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p =>
                p.Name.Contains(searchTerm) ||
                p.Code.Contains(searchTerm) ||
                p.Barcode.Contains(searchTerm));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }

        return await query
            .OrderBy(p => p.Name)
            .Take(limit)
            .Select(p => new ProductSearchResult
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Specification = p.Specification,
                Unit = p.Unit,
                TransactionType = p.TransactionType,
                DefaultPrice = p.TransactionType == ProductTransactionType.Buy
                    ? p.PurchasePrice
                    : p.DefaultPrice,
                CurrentStock = p.TrackInventory
                    ? _context.Inventories
                        .Where(i => i.ProductId == p.Id)
                        .Select(i => i.ConfirmedQuantity)
                        .FirstOrDefault()
                    : null
            })
            .ToListAsync();
    }

    /// <summary>
    /// 발주용 품목 검색 (구매 품목만)
    /// </summary>
    public Task<List<ProductSearchResult>> SearchBuyProductsAsync(string searchTerm, int limit = 20)
        => SearchProductsAsync(searchTerm, ProductTransactionType.Buy, null, limit);

    /// <summary>
    /// 판매/견적용 품목 검색 (판매 품목만)
    /// </summary>
    public Task<List<ProductSearchResult>> SearchSellProductsAsync(string searchTerm, int limit = 20)
        => SearchProductsAsync(searchTerm, ProductTransactionType.Sell, null, limit);
}
```

---

## 4. 품목 선택 모달 (공통 컴포넌트)

### 4.0 모달 자동 필터링 규칙

| 호출 화면 | 모달 제목 | 필터링 |
|-----------|-----------|--------|
| **발주** 화면 | 📥 구매 품목 선택 | `TransactionType = Buy OR Both` |
| **판매** 화면 | 📤 판매 품목 선택 | `TransactionType = Sell OR Both` |
| **견적서** 화면 | 📤 판매 품목 선택 | `TransactionType = Sell OR Both` |
| **재고** 화면 | 📤 판매 품목 선택 | `TransactionType = Sell OR Both` |

### 4.1 모달 UI (발주용 - 구매 품목)

```
┌─────────────────────────────────────────────────────────────────────┐
│  📥 구매 품목 선택 (발주용)                                    [✕]  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌───────────────────────────────────────────────────────────────┐ │
│  │ 🔍 [품목명, 코드, 바코드 검색...]                [카테고리 ▼] │ │
│  └───────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  ┌───────────────────────────────────────────────────────────────┐ │
│  │   │ 유형│ 코드  │ 품목명      │ 규격  │ 단위│ 구매단가│ 거래처│ │
│  ├───┼────┼───────┼─────────────┼───────┼─────┼─────────┼───────┤ │
│  │ ○ │ 📥 │ P-001 │ 원재료A     │ 대용량│ KG  │ ₩50,000│ A도매 │ │
│  │ ● │ 📥 │ P-002 │ 부품B       │ 일반  │ EA  │ ₩3,000 │ B공급 │ │
│  │ ○ │ ↔️ │ P-003 │ 주사기 100EA│ 일반  │ EA  │ ₩8,000 │ -     │ │
│  └───────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  선택: 부품B (구매가 ₩3,000)                                        │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ 수량: [        100       ]  금액: ₩300,000                  │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                              [취소]  [품목 추가]                    │
│  💡 원하는 품목이 없나요?  [+ 새 구매품목 등록]                      │
└─────────────────────────────────────────────────────────────────────┘
```

### 4.2 모달 UI (판매용 - 판매 품목)

```
┌─────────────────────────────────────────────────────────────────────┐
│  📤 판매 품목 선택 (판매/견적용)                               [✕]  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌───────────────────────────────────────────────────────────────┐ │
│  │ 🔍 [품목명, 코드, 바코드 검색...]                [카테고리 ▼] │ │
│  └───────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  ┌───────────────────────────────────────────────────────────────┐ │
│  │   │ 유형│ 코드  │ 품목명       │ 규격  │ 단위│ 판매단가│ 재고 │ │
│  ├───┼────┼───────┼──────────────┼───────┼─────┼─────────┼──────┤ │
│  │ ○ │ 📤 │ P-010 │ 테이프 10EA  │ 의료용│ EA  │ ₩3,500 │ 500  │ │
│  │ ○ │ 📤 │ P-011 │ 거즈 1BOX    │ 멸균  │ BOX │ ₩12,000│ 200  │ │
│  │ ● │ ↔️ │ P-003 │ 주사기 100EA │ 일반  │ EA  │ ₩8,000 │ 150  │ │
│  │ ○ │ 📤 │ P-012 │ 소독약 500ml │ 에탄올│BOTTLE│ ₩5,000│ 80   │ │
│  └───────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  선택: 주사기 100EA (판매가 ₩8,000, 재고 150)                       │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ 수량: [        10        ]  금액: ₩80,000                   │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                              [취소]  [품목 추가]                    │
│  💡 원하는 품목이 없나요?  [+ 새 판매품목 등록]                      │
└─────────────────────────────────────────────────────────────────────┘
```

### 4.3 다중 선택 모드

```
┌─────────────────────────────────────────────────────────────────────┐
│  📤 판매 품목 선택 (다중)                                     [✕]  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  🔍 [검색...]                                        [카테고리 ▼]  │
│                                                                     │
│  ┌───────────────────────────────────────────────────────────────┐ │
│  │   │ 유형│ 코드  │ 품목명       │ 단가   │ 재고│ 수량         │ │
│  ├───┼────┼───────┼──────────────┼────────┼─────┼──────────────┤ │
│  │ ☑ │ 📤 │ P-001 │ 테이프 10EA  │ ₩3,500│ 500 │ [____100___] │ │
│  │ ☑ │ 📤 │ P-002 │ 거즈 1BOX    │ ₩12,000│200 │ [_____50___] │ │
│  │ ☐ │ ↔️ │ P-003 │ 주사기 100EA │ ₩8,000│ 150 │ [__________] │ │
│  │ ☐ │ 📤 │ P-004 │ 소독약 500ml │ ₩5,000│ 80  │ [__________] │ │
│  └───────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  선택: 2건                                    합계: ₩950,000       │
│                                                                     │
│                              [취소]  [선택 완료]                    │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 4.5 모달 컴포넌트

```csharp
public class ProductSelectionModal
{
    // ★ 거래 유형 필터 (Buy=발주용, Sell=판매용)
    public ProductTransactionType? TransactionTypeFilter { get; set; }

    public bool AllowMultiple { get; set; } = false;
    public bool ShowStock { get; set; } = true;
    public bool ShowPrice { get; set; } = true;
    public bool AllowNewProduct { get; set; } = true;
    public int? CategoryFilter { get; set; }

    public event EventHandler<ProductSelectedEventArgs> OnProductSelected;
    public event EventHandler<ProductsSelectedEventArgs> OnProductsSelected;

    /// <summary>
    /// 발주용 모달 생성
    /// </summary>
    public static ProductSelectionModal ForOrder()
    {
        return new ProductSelectionModal
        {
            TransactionTypeFilter = ProductTransactionType.Buy,
            ShowStock = false,  // 구매 품목은 재고 표시 불필요
            ShowPrice = true    // 구매 단가 표시
        };
    }

    /// <summary>
    /// 판매용 모달 생성
    /// </summary>
    public static ProductSelectionModal ForSale()
    {
        return new ProductSelectionModal
        {
            TransactionTypeFilter = ProductTransactionType.Sell,
            ShowStock = true,   // 판매 시 재고 확인 필요
            ShowPrice = true    // 판매 단가 표시
        };
    }

    /// <summary>
    /// 견적서용 모달 생성
    /// </summary>
    public static ProductSelectionModal ForQuotation()
    {
        return new ProductSelectionModal
        {
            TransactionTypeFilter = ProductTransactionType.Sell,
            ShowStock = false,  // 견적서는 재고 불필요
            ShowPrice = true    // 판매 단가 표시
        };
    }
}

public class ProductSelectedEventArgs : EventArgs
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public ProductTransactionType TransactionType { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
```

```xml
<!-- ProductSelectionModal.xaml -->
<Window Title="품목 선택" Width="700" Height="500">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- 검색 -->
            <RowDefinition Height="*"/>      <!-- 리스트 -->
            <RowDefinition Height="Auto"/>  <!-- 수량 입력 -->
            <RowDefinition Height="Auto"/>  <!-- 버튼 -->
        </Grid.RowDefinitions>

        <!-- 검색 영역 -->
        <Grid Grid.Row="0" Margin="16">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <TextBox Text="{Binding SearchTerm, UpdateSourceTrigger=PropertyChanged}"
                     Watermark="품목명, 코드, 바코드 검색..."/>
            <ComboBox Grid.Column="1" ItemsSource="{Binding Categories}"
                      SelectedItem="{Binding SelectedCategory}"
                      DisplayMemberPath="Name" Width="150" Margin="8,0,0,0"/>
        </Grid>

        <!-- 품목 리스트 -->
        <DataGrid Grid.Row="1" ItemsSource="{Binding Products}"
                  SelectedItem="{Binding SelectedProduct}"
                  AutoGenerateColumns="False" Margin="16,0">
            <DataGrid.Columns>
                <DataGridTemplateColumn Width="40">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <RadioButton IsChecked="{Binding IsSelected}"
                                         GroupName="ProductSelection"/>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
                <DataGridTextColumn Header="코드" Binding="{Binding Code}" Width="80"/>
                <DataGridTextColumn Header="품목명" Binding="{Binding Name}" Width="*"/>
                <DataGridTextColumn Header="규격" Binding="{Binding Specification}" Width="80"/>
                <DataGridTextColumn Header="단위" Binding="{Binding Unit}" Width="60"/>
                <DataGridTextColumn Header="단가" Binding="{Binding DefaultPrice, StringFormat=₩{0:#,##0}}" Width="100"/>
                <DataGridTextColumn Header="재고" Binding="{Binding CurrentStock}" Width="80"/>
            </DataGrid.Columns>
        </DataGrid>

        <!-- 수량 입력 -->
        <Border Grid.Row="2" BorderBrush="#E0E0E0" BorderThickness="0,1,0,0"
                Padding="16" Margin="16,0">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="150"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <TextBlock Text="수량:" VerticalAlignment="Center"/>
                <TextBox Grid.Column="1" Text="{Binding Quantity}"
                         HorizontalContentAlignment="Right" Margin="8,0"/>
                <TextBlock Grid.Column="3" VerticalAlignment="Center">
                    <Run Text="금액: "/>
                    <Run Text="{Binding TotalAmount, StringFormat=₩{0:#,##0}}" FontWeight="Bold"/>
                </TextBlock>
            </Grid>
        </Border>

        <!-- 버튼 -->
        <StackPanel Grid.Row="3" Orientation="Horizontal"
                    HorizontalAlignment="Right" Margin="16">
            <Button Content="+ 새 품목 등록" Command="{Binding NewProductCommand}"
                    Style="{StaticResource LinkButtonStyle}" Margin="0,0,16,0"/>
            <Button Content="취소" Command="{Binding CancelCommand}"
                    Style="{StaticResource SecondaryButtonStyle}" Width="80"/>
            <Button Content="품목 추가" Command="{Binding ConfirmCommand}"
                    Style="{StaticResource PrimaryButtonStyle}" Width="100" Margin="8,0,0,0"/>
        </StackPanel>
    </Grid>
</Window>
```

---

## 5. 화면별 품목 선택 통합

### 5.1 발주 화면에서 품목 선택

```
┌───────────────────────────────┬─────────────────────────────────────┐
│  📦 품목 리스트               │  ⭐ 최근거래 품목                    │
│                               │                                     │
│  [품목 검색...]               │  ☑ 테이프 10EA  수량: [___100___]   │
│  [+ 품목 추가]  ← 클릭 시     │  ☑ 거즈 1BOX    수량: [____50___]   │
│       ↓                       │                                     │
│  ┌────────────────────┐      │  [+ 품목 추가]  ← 클릭 시            │
│  │ 품목 선택 모달     │      │       ↓                              │
│  │ 등장               │      │  모달에서 품목 선택 후               │
│  └────────────────────┘      │  수량 입력                           │
│                               │                                     │
└───────────────────────────────┴─────────────────────────────────────┘
```

### 5.2 견적서 화면에서 품목 선택

```
┌─────────────────────────────────────────────────────────────────────┐
│  견적서 작성                                                        │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  품목 목록                                          [+ 품목 추가]   │
│  ┌───────────────────────────────────────────────────────────────┐ │
│  │ No │ 품목명       │ 규격   │ 단가      │ 수량  │ 금액        │ │
│  ├────┼──────────────┼────────┼───────────┼───────┼─────────────┤ │
│  │ 1  │ 테이프 10EA  │ 의료용 │ ₩3,500   │ 100   │ ₩350,000   │ │
│  │ 2  │ 거즈 1BOX    │ 멸균   │ ₩12,000  │ 50    │ ₩600,000   │ │
│  │ 3  │ [+ 품목 추가]│        │           │       │             │ │
│  └───────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  [+ 품목 추가] 클릭 → 품목 선택 모달 등장                           │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 5.3 재고 조회에서 품목 필터

```
┌─────────────────────────────────────────────────────────────────────┐
│  재고 관리                                                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  [품목 선택 ▼]  ← 클릭 시 품목 선택 모달                            │
│  [카테고리 ▼]  [재고상태 ▼]                                         │
│                                                                     │
│  선택된 품목: 테이프 10EA, 거즈 1BOX (2건)          [초기화]        │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ 품목명       │ 확정재고 │ 입고예정 │ 출고예정 │ 가용재고│상태 │   │
│  ├──────────────┼──────────┼──────────┼──────────┼─────────┼────┤   │
│  │ 테이프 10EA  │ 500      │ +100     │ -50      │ 550    │ ✅ │   │
│  │ 거즈 1BOX    │ 200      │ +0       │ -80      │ 120    │ ✅ │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 6. Excel 일괄 등록

### 6.1 업로드 양식

```
| 품목코드 | 품목명      | 규격   | 단위 | 거래유형 | 판매단가 | 구매단가 | 카테고리   | 바코드        |
|----------|-------------|--------|------|----------|----------|----------|------------|---------------|
| P-001    | 원재료A     | 대용량 | KG   | 구매     |          | 50000    | 원자재     |               |
| P-002    | 테이프 10EA | 의료용 | EA   | 판매     | 3500     |          | 의료소모품 | 8801234567890 |
| P-003    | 거즈 1BOX   | 멸균   | BOX  | 판매     | 12000    |          | 의료소모품 |               |
| P-004    | 주사기 100EA| 일반   | EA   | 둘다     | 8000     | 6000     | 주사류     |               |
```

**거래유형 값:**
| 값 | 의미 |
|----|------|
| 구매 / Buy / 📥 | 구매 품목 (사는 품목) |
| 판매 / Sell / 📤 | 판매 품목 (파는 품목) |
| 둘다 / Both / ↔️ | 구매+판매 품목 |

### 6.2 업로드 서비스

```csharp
public class ProductImportService
{
    public async Task<ImportResult> ImportFromExcelAsync(Stream file)
    {
        var result = new ImportResult();

        using var package = new ExcelPackage(file);
        var worksheet = package.Workbook.Worksheets[0];

        int row = 2; // 헤더 스킵
        while (worksheet.Cells[row, 1].Value != null)
        {
            try
            {
                var dto = new ProductCreateDto
                {
                    Code = worksheet.Cells[row, 1].GetValue<string>(),
                    Name = worksheet.Cells[row, 2].GetValue<string>(),
                    Specification = worksheet.Cells[row, 3].GetValue<string>(),
                    Unit = worksheet.Cells[row, 4].GetValue<string>(),
                    TransactionType = ParseTransactionType(
                        worksheet.Cells[row, 5].GetValue<string>()),  // ★ 거래유형
                    DefaultPrice = worksheet.Cells[row, 6].GetValue<decimal?>(),
                    PurchasePrice = worksheet.Cells[row, 7].GetValue<decimal?>(),
                    CategoryName = worksheet.Cells[row, 8].GetValue<string>(),
                    Barcode = worksheet.Cells[row, 9].GetValue<string>()
                };

                // 카테고리 매칭
                if (!string.IsNullOrWhiteSpace(dto.CategoryName))
                {
                    dto.CategoryId = await GetOrCreateCategoryAsync(dto.CategoryName);
                }

                await _productService.CreateProductAsync(dto);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportError
                {
                    Row = row,
                    Message = ex.Message
                });
            }

            row++;
        }

        return result;
    }

    /// <summary>
    /// 거래유형 문자열 파싱
    /// </summary>
    private ProductTransactionType ParseTransactionType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ProductTransactionType.Sell; // 기본값: 판매

        return value.ToLower() switch
        {
            "구매" or "buy" or "📥" => ProductTransactionType.Buy,
            "판매" or "sell" or "📤" => ProductTransactionType.Sell,
            "둘다" or "both" or "↔️" => ProductTransactionType.Both,
            _ => ProductTransactionType.Sell
        };
    }
}
```

---

## 7. 메인 메뉴 구조

### 7.1 품목 관리 메뉴 위치

```
┌─────────────────────────────────────────────────────────────────────┐
│  [발주관리]  [판매관리]  [재고관리]  [품목관리]  [정산관리]  [설정] │
└─────────────────────────────────────────────────────────────────────┘
                                  ↑
                          품목 마스터 관리
```

### 7.2 품목 관리 하위 메뉴

```
품목관리
├── 전체 품목          // 모든 품목 조회/검색
├── 📥 구매 품목       // 사는 품목 필터
├── 📤 판매 품목       // 파는 품목 필터
├── 품목 등록          // 신규 품목 등록
├── 카테고리 관리      // 품목 분류 관리
└── Excel 일괄 등록    // 대량 품목 등록
```

---

## 8. 품목 유형 사용 흐름 정리

### 8.1 구매 품목 (📥 Buy) 흐름

```
[품목 등록 - 구매] → [발주 화면에서 품목 선택] → [발주서 작성]
                              ↓
                    📥 구매 품목만 표시
                    (Buy + Both)
```

### 8.2 판매 품목 (📤 Sell) 흐름

```
[품목 등록 - 판매] → [판매/견적서 화면에서 품목 선택] → [판매 등록/견적서 작성]
                              ↓                              ↓
                    📤 판매 품목만 표시                 재고 자동 반영
                    (Sell + Both)
```

### 8.3 둘다 품목 (↔️ Both) 흐름

```
[품목 등록 - 둘다] → [모든 화면에서 사용 가능]
                         ↓
        ┌────────────────┼────────────────┐
        ▼                ▼                ▼
    📥 발주          📤 판매          📤 견적서
```

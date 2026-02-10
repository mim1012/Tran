# Phase 1 화면 목록 및 우선순위

> **작성일**: 2026-01-26
> **대상**: Phase 1 (거래처 고정 모드)
> **목적**: 구현 전 필요한 모든 화면 식별 및 우선순위 설정

---

## 📊 화면 분류 체계

### 카테고리 정의
- **🎯 Foundation**: 앱 구동의 필수 기반 화면
- **💼 Core Business**: 핵심 업무 화면 (발주/판매/재고)
- **📋 Document Management**: 서류 관리 (견적/계약)
- **⚙️ Master Data**: 기준 정보 관리
- **📊 Support**: 보조 기능 화면

---

## 🎯 Foundation (기반 화면) - 총 4개

### F1. CompanySelectionScreen ⭐⭐⭐⭐⭐
**우선순위**: P0 (최우선)
**파일명**: `CompanySelectionScreen.xaml`

**목적**:
- 앱 시작 시 거래처 선택
- Phase 1의 핵심 개념인 "거래처 고정 모드"의 진입점

**레이아웃**:
- 2컬럼 그리드 카드 리스트
- 상단: 검색바 + 필터 (유형, 정렬)
- 하단: 페이지네이션

**의존성**:
- 없음 (앱 시작점)

**다음 화면**:
- → MainWorkspace (거래처 선택 후)

**주요 인터랙션**:
- 거래처 카드 클릭 → 전체 앱이 해당 거래처로 고정
- 검색/필터 → 실시간 결과 업데이트
- 페이지네이션

**데이터 요구사항**:
```csharp
- Company.Name
- Company.Address
- Company.LastTransactionDate
- Company.ActiveOrderCount (진행중 발주)
- Company.Type (병원/도매)
```

**참고 문서**: `docs/features/00-common-ux.md` (Section 1.5)

---

### F2. MainWorkspace ⭐⭐⭐⭐⭐
**우선순위**: P0 (최우선)
**파일명**: `MainWorkspace.xaml`

**목적**:
- 거래처 선택 후 메인 작업 공간
- 상단바(거래처 정보) + 업무 탭 + 콘텐츠 영역

**레이아웃**:
```
┌─────────────────────────────────────────────────┐
│  [현재 거래처: A병원 ▼]        [거래처 변경]     │
├─────────────────────────────────────────────────┤
│  [발주]  [견적]  [구매]  [판매]  [재고]  [품목]  │
├─────────────────────────────────────────────────┤
│                                                 │
│         (선택된 탭의 콘텐츠 영역)                │
│                                                 │
└─────────────────────────────────────────────────┘
```

**의존성**:
- CompanySelectionScreen (거래처 선택 완료)

**다음 화면**:
- → OrderWindow (발주 탭 클릭)
- → SaleWindow (판매 탭 클릭)
- → 등등

**주요 인터랙션**:
- [현재 거래처 ▼] 클릭 → 최근 거래처 5개 드롭다운
- [거래처 변경] 클릭 → 미저장 확인 다이얼로그 → CompanySelectionScreen
- 업무 탭 전환 → 콘텐츠 영역 변경

**상태 관리**:
```csharp
CompanyContextService.CurrentCompany (앱 전역)
```

**참고 문서**: `docs/features/00-common-ux.md` (Section 1.2)

---

### F3. ProductSelectionModal ⭐⭐⭐⭐⭐
**우선순위**: P0 (최우선)
**파일명**: `ProductSelectionModal.xaml`

**목적**:
- 모든 입력 화면에서 공통으로 사용하는 품목 선택 모달
- 발주/판매/견적서 작성 시 품목 추가할 때 사용

**레이아웃**:
```
┌─────────────────────────────────────────────┐
│  📥 구매 품목 선택                     [✕]  │
├─────────────────────────────────────────────┤
│  🔍 [품목명, 코드 검색...]  [카테고리 ▼]   │
│                                             │
│  [ ] P-001  원재료A   KG   ₩50,000   A도매  │
│  [●] P-002  부품B     EA   ₩3,000    B공급  │
│                                             │
│  수량: [___100___]  금액: ₩300,000          │
│                        [취소]  [품목 추가]  │
│  💡 [+ 새 품목 등록]                        │
└─────────────────────────────────────────────┘
```

**사용처**:
- OrderWindow (발주 입력)
- SaleWindow (판매 입력)
- QuotationWindow (견적서 작성)

**모달 유형별 필터링**:
| 호출 화면 | 필터 조건 | 표시 정보 |
|----------|----------|----------|
| 발주 | Buy + Both 품목만 | 구매단가, 거래처 |
| 판매 | Sell + Both 품목만 | 판매단가, 재고 |
| 견적서 | Sell + Both 품목만 | 판매단가 |

**주요 인터랙션**:
- 실시간 검색 (품목명/코드/바코드)
- 단일 선택 모드 / 다중 선택 모드
- 수량 입력 → 금액 자동 계산
- [품목 추가] → 호출한 화면에 데이터 전달

**데이터 요구사항**:
```csharp
- Product.TransactionType (Buy/Sell/Both)
- Product.Name
- Product.Code
- Product.DefaultPrice or PurchasePrice
- Inventory.ConfirmedQuantity (판매 시)
```

**참고 문서**:
- `docs/features/05-product-master.md` (Section 4)
- `docs/features/00-common-ux.md` (Section 7)

---

### F4. ThreeColumnLayoutControl ⭐⭐⭐⭐
**우선순위**: P1 (매우 중요)
**파일명**: `ThreeColumnLayoutControl.xaml` (UserControl)

**목적**:
- 발주/판매 화면에서 재사용하는 3분할 레이아웃 컴포넌트

**레이아웃**:
```
┌──────────────────┬─────────────────────────┐
│ 좌상: 품목 리스트 │ 우상: 최근거래 품목      │
│ (40% 너비)       │ (60% 너비)              │
│                  │ - 자주 시키는 품목       │
│ [품목 검색...]   │ - 수량만 입력하면 완료   │
│ ☐ 테이프 ₩3,500 │ ☑ 테이프  수량:[_100_]  │
│ ☐ 거즈  ₩12,000 │ ☑ 거즈    수량:[__50_]  │
│                  │ 합계: ₩950,000          │
│                  │ [임시저장] [발주서보내기]│
├──────────────────┴─────────────────────────┤
│ 하단: 최근 거래 내역 (100% 너비, 40% 높이) │
│ [등록] [최근작업(임시저장)] [최근성사내역] │
│                                            │
│ (선택된 탭의 내용)                         │
└────────────────────────────────────────────┘
```

**사용처**:
- OrderWindow (발주)
- SaleWindow (판매)

**컴포넌트 분리**:
- 좌상: ProductListPanel
- 우상: RecentProductsPanel
- 하단: TransactionHistoryTabs (3개 탭)

**참고 문서**: `docs/features/00-common-ux.md` (Section 2)

---

## 💼 Core Business (핵심 업무) - 총 6개

### C1. OrderWindow ⭐⭐⭐⭐⭐
**우선순위**: P0 (최우선)
**파일명**: `OrderWindow.xaml`

**목적**:
- 발주서 작성 (사는 것)
- 3분할 레이아웃 사용

**의존성**:
- MainWorkspace (발주 탭 클릭)
- ThreeColumnLayoutControl
- ProductSelectionModal (구매 품목용)

**주요 기능**:
1. 좌상: 전체 구매 품목 리스트 (검색 가능)
2. 우상: 최근 거래 품목 (빠른 입력)
3. 하단 3탭:
   - [등록]: 새 발주서 작성
   - [최근 작업]: 임시저장 목록
   - [최근 성사 내역]: 완료된 발주

**인터랙션**:
- [+ 품목 추가] → ProductSelectionModal (Buy 필터)
- [임시저장] → DraftDocument 테이블에 저장
- [발주서 보내기] → Order 생성 → 상태: Requested
- [불러오기] → 임시저장 데이터 로드

**데이터 흐름**:
```
임시저장 → DraftDocument (JSON)
발주 확정 → Order (state=Requested) → Purchase 자동 생성
```

**참고 문서**: `docs/features/02-order-management.md` (Section 1.3)

---

### C2. SaleWindow ⭐⭐⭐⭐⭐
**우선순위**: P0 (최우선)
**파일명**: `SaleWindow.xaml`

**목적**:
- 판매서 작성 (파는 것)
- 3분할 레이아웃 사용

**의존성**:
- MainWorkspace (판매 탭 클릭)
- ThreeColumnLayoutControl
- ProductSelectionModal (판매 품목용)

**주요 기능**:
1. 좌상: 전체 판매 품목 리스트
2. 우상: 자주 나가는 품목 (재고 표시)
3. 하단 3탭:
   - [등록]: 새 판매서 작성
   - [최근 작업]: 임시저장 목록
   - [최근 성사 내역]: 완료된 판매

**차이점 (발주 vs 판매)**:
| 항목 | 발주 | 판매 |
|------|------|------|
| 품목 유형 | 구매 품목 (Buy/Both) | 판매 품목 (Sell/Both) |
| 재고 표시 | 불필요 | **필수** (재고 부족 경고) |
| 가격 | 구매 단가 | 판매 단가 |

**재고 검증**:
- 판매 등록 전 재고 확인
- 재고 부족 시 경고 표시
- 재고 0 품목은 비활성화

**참고 문서**: `docs/features/02-order-management.md` (Section 3.3)

---

### C3. InventoryWindow ⭐⭐⭐⭐
**우선순위**: P1 (매우 중요)
**파일명**: `InventoryWindow.xaml`

**목적**:
- 재고 조회 및 관리
- 입출고 이력 확인

**레이아웃**:
```
┌─────────────────────────────────────────────┐
│ 재고 관리                      [재고 조정]  │
├─────────────────────────────────────────────┤
│ [카테고리 ▼] [재고상태 ▼]       [검색...]  │
├─────────────────────────────────────────────┤
│ 품목명    확정재고  입고예정  출고예정  상태 │
│ 테이프    500      +100      -50      ✅   │
│ 거즈      200      +0        -80      ✅   │
│ 주사기    30       +200      -100     ⚠️   │
│ 소독약    5        +0        -10      🔴   │
└─────────────────────────────────────────────┘
```

**의존성**:
- MainWorkspace (재고 탭 클릭)

**주요 기능**:
- 품목별 현재 재고 조회
- 입고 예정 / 출고 예정 수량 표시
- 재고 상태: ✅정상 / ⚠️안전재고 이하 / 🔴재고 부족
- 재고 조정 (수동 입력)
- 입출고 이력 조회

**데이터 요구사항**:
```csharp
Inventory.ConfirmedQuantity (확정 재고)
Inventory.PendingInQuantity (입고 예정)
Inventory.PendingOutQuantity (출고 예정)
Inventory.SafetyStock (안전 재고)
```

**참고 문서**: `docs/features/03-inventory-finance.md` (Section 1.3)

---

### C4. PurchaseWindow ⭐⭐⭐
**우선순위**: P2 (중요)
**파일명**: `PurchaseWindow.xaml`

**목적**:
- 구매 관리 (발주 → 구매 전환)
- 입고 처리

**레이아웃**:
- 단일 DataGrid (목록 기반)

**의존성**:
- MainWorkspace (구매 탭 클릭)

**주요 기능**:
- 발주 완료 건 → 구매 레코드 자동 생성
- 입고 처리 (수량, 날짜 입력)
- 검수 처리
- 불량 등록

**자동화 흐름**:
```
Order (완료) → Purchase 자동 생성 → 입고 처리 → Inventory 반영
```

**참고 문서**: `docs/features/02-order-management.md` (Section 2)

---

### C5. ReceivableWindow ⭐⭐⭐
**우선순위**: P2 (중요)
**파일명**: `ReceivableWindow.xaml`

**목적**:
- 채권(미수금) 관리
- 입금 처리

**레이아웃**:
- 단일 DataGrid

**의존성**:
- MainWorkspace (정산 탭 또는 별도 메뉴)

**주요 기능**:
- 판매 확정 → 채권 자동 생성
- 입금 처리
- 연체 경고
- 거래처별 미수금 조회

**참고 문서**: `docs/features/03-inventory-finance.md` (Section 3)

---

### C6. DefectWindow ⭐⭐
**우선순위**: P3 (보통)
**파일명**: `DefectWindow.xaml`

**목적**:
- 불량품 관리
- 반품 처리

**레이아웃**:
- 단일 DataGrid + 상세 패널

**참고 문서**: `docs/features/03-inventory-finance.md` (Section 2)

---

## 📋 Document Management (서류 관리) - 총 3개

### D1. QuotationWindow ⭐⭐⭐⭐
**우선순위**: P1 (매우 중요)
**파일명**: `QuotationWindow.xaml`

**목적**:
- 견적서 작성
- 견적 확정 → 단가 정책 반영 + 품목 리스트 등록

**레이아웃**:
```
┌─────────────────────────────────────────────┐
│ 견적서 작성              [임시저장] [발송]  │
├─────────────────────────────────────────────┤
│ 거래처: [A병원 ▼]                           │
│ 유효기간: [2025-02-22 📅]                   │
│                                             │
│ 품목 목록                   [+ 품목 추가]   │
│ No  품목명      규격  수량  단가      금액   │
│ 1   테이프 10EA 의료  100   ₩3,500  ₩350K  │
│ 2   거즈 1BOX   멸균  50    ₩12,000 ₩600K  │
│                                             │
│ 합계: ₩1,045,000 (VAT 포함)                │
└─────────────────────────────────────────────┘
```

**의존성**:
- ProductSelectionModal (판매 품목용)

**견적 확정 시 자동 처리**:
1. 단가 정책 반영 (CompanyPrice 테이블)
2. 품목 리스트 등록 (CompanyProduct 테이블)

**참고 문서**: `docs/features/01-document-management.md` (Section 1)

---

### D2. ContractWindow ⭐⭐
**우선순위**: P3 (보통)
**파일명**: `ContractWindow.xaml`

**목적**:
- 계약서 관리
- 계약 단가 자동 적용

**참고 문서**: `docs/features/01-document-management.md` (Section 2)

---

### D3. PriceManagementWindow ⭐⭐
**우선순위**: P3 (보통)
**파일명**: `PriceManagementWindow.xaml`

**목적**:
- 거래처별 단가 조회
- 단가 이력 관리

**참고 문서**: `docs/features/01-document-management.md` (Section 3)

---

## ⚙️ Master Data (기준 정보) - 총 3개

### M1. ProductManagementWindow ⭐⭐⭐⭐⭐
**우선순위**: P0 (최우선)
**파일명**: `ProductManagementWindow.xaml`

**목적**:
- 품목 마스터 관리
- 구매 품목 / 판매 품목 등록

**레이아웃**:
```
┌─────────────────────────────────────────────┐
│ 품목 관리                      [+ 품목 등록] │
├─────────────────────────────────────────────┤
│ [전체▼] [📥구매] [📤판매] [↔️둘다] [검색...] │
├─────────────────────────────────────────────┤
│   유형 코드   품목명       단가      상태    │
│ ☐ 📥  P-001  원재료A     ₩50,000   활성    │
│ ☐ 📤  P-002  테이프 10EA ₩3,500    활성    │
│ ☐ ↔️  P-003  주사기      ₩8,000    활성    │
└─────────────────────────────────────────────┘
```

**의존성**:
- 없음 (독립 실행)

**주요 기능**:
- 품목 등록/수정/삭제(비활성화)
- 거래 유형 선택: 📥Buy / 📤Sell / ↔️Both
- Excel 일괄 등록
- 카테고리 관리

**중요성**:
- **모든 입력 화면의 기반 데이터**
- ProductSelectionModal이 이 데이터를 사용

**참고 문서**: `docs/features/05-product-master.md`

---

### M2. ProductEditModal ⭐⭐⭐⭐
**우선순위**: P1 (매우 중요)
**파일명**: `ProductEditModal.xaml`

**목적**:
- 품목 등록/수정 모달
- ProductManagementWindow에서 호출
- ProductSelectionModal에서도 [+ 새 품목 등록] 시 호출

**레이아웃**:
```
┌─────────────────────────────────────────────┐
│ 품목 등록                              [✕]  │
├─────────────────────────────────────────────┤
│ 거래 유형:                                  │
│ (○) 📥 구매 품목 (사는것)                   │
│ (●) 📤 판매 품목 (파는것)                   │
│ (○) ↔️  둘다                                │
│                                             │
│ 품목명: [________________] *                │
│ 규격:   [________________]                  │
│ 단위:   [EA ▼] *                            │
│                                             │
│ 판매 단가: [₩________]                      │
│ 구매 단가: [₩________]                      │
│                              [취소] [저장]  │
└─────────────────────────────────────────────┘
```

**참고 문서**: `docs/features/05-product-master.md` (Section 3.2)

---

### M3. CategoryManagementWindow ⭐⭐
**우선순위**: P3 (보통)
**파일명**: `CategoryManagementWindow.xaml`

**목적**:
- 품목 카테고리 관리

---

## 📊 Support (보조 기능) - 총 3개

### S1. DailyDeliveryWindow ⭐⭐
**우선순위**: P3 (보통)
**파일명**: `DailyDeliveryWindow.xaml`

**목적**:
- 기사용 배송 현황 화면

**참고 문서**: `docs/features/02-order-management.md` (Section 4)

---

### S2. LogHistoryWindow ⭐
**우선순위**: P4 (낮음)
**파일명**: `LogHistoryWindow.xaml`

**목적**:
- 상태 변경 로그 조회

---

### S3. SettingsWindow ⭐
**우선순위**: P4 (낮음)
**파일명**: `SettingsWindow.xaml`

**목적**:
- 회사 정보, 백업 설정 등

---

## 📈 우선순위 요약

### P0 (최우선 - 앱 구동 필수)
1. CompanySelectionScreen
2. MainWorkspace
3. ProductSelectionModal
4. OrderWindow
5. SaleWindow
6. ProductManagementWindow

### P1 (매우 중요 - 핵심 기능)
7. ThreeColumnLayoutControl
8. QuotationWindow
9. InventoryWindow
10. ProductEditModal

### P2 (중요 - 주요 기능)
11. PurchaseWindow
12. ReceivableWindow

### P3 (보통 - 보조 기능)
13. DefectWindow
14. ContractWindow
15. PriceManagementWindow
16. CategoryManagementWindow
17. DailyDeliveryWindow

### P4 (낮음 - 선택 기능)
18. LogHistoryWindow
19. SettingsWindow

---

## 🔗 화면 간 의존성 맵

```
CompanySelectionScreen (시작점)
    ↓
MainWorkspace (거래처 고정)
    ├─→ OrderWindow
    │     ├─→ ThreeColumnLayoutControl
    │     └─→ ProductSelectionModal (Buy)
    │
    ├─→ SaleWindow
    │     ├─→ ThreeColumnLayoutControl
    │     └─→ ProductSelectionModal (Sell)
    │
    ├─→ QuotationWindow
    │     └─→ ProductSelectionModal (Sell)
    │
    ├─→ InventoryWindow
    │
    ├─→ PurchaseWindow
    │
    └─→ ReceivableWindow

ProductManagementWindow (독립)
    └─→ ProductEditModal
```

---

## 📋 다음 단계

1. ✅ **1단계 완료**: 화면 목록 및 우선순위 정의
2. ⏭️ **2단계**: 핵심 사용자 흐름도 작성 (Excalidraw)
3. ⏭️ **3단계**: P0 화면 와이어프레임 (6개)
4. ⏭️ **4단계**: 인터랙션 명세서
5. ⏭️ **5단계**: 예외 상황 가이드

---

**작성자**: John (PM)
**검토 필요 사항**:
- [ ] P0 화면 우선순위 동의 여부
- [ ] 누락된 화면 없는지 확인
- [ ] 의존성 관계 검증

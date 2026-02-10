# 화면 전환 다이어그램 (Screen Transition Diagram)

> **작성일**: 2026-01-26
> **목적**: Phase 1 모든 화면 간 전환 경로 시각화

---

## 🗺️ 전체 화면 전환 맵

```mermaid
graph TD
    Start([앱 시작]) --> CompanySelect[CompanySelectionScreen<br/>거래처 선택]

    CompanySelect --> |거래처 선택| Workspace[MainWorkspace<br/>메인 작업 공간]

    %% MainWorkspace에서 탭 전환
    Workspace --> |[발주] 탭| OrderWin[OrderWindow]
    Workspace --> |[견적] 탭| QuoteWin[QuotationWindow]
    Workspace --> |[구매] 탭| PurchaseWin[PurchaseWindow]
    Workspace --> |[판매] 탭| SaleWin[SaleWindow]
    Workspace --> |[재고] 탭| InvWin[InventoryWindow]
    Workspace --> |[품목] 탭| ProductWin[ProductManagementWindow]

    %% 거래처 전환
    Workspace --> |[거래처 변경]| ConfirmDlg{미저장 확인}
    ConfirmDlg --> |확인| CompanySelect
    ConfirmDlg --> |취소| Workspace

    %% 발주 화면에서
    OrderWin --> |[+ 품목 추가]| ProductModal1[ProductSelectionModal<br/>Buy 필터]
    ProductModal1 --> |[품목 추가]| OrderWin
    ProductModal1 --> |[✕ 닫기]| OrderWin
    ProductModal1 --> |[+ 새 품목]| ProductEdit1[ProductEditModal]
    ProductEdit1 --> |[저장]| ProductModal1
    ProductEdit1 --> |[취소]| ProductModal1

    %% 판매 화면에서
    SaleWin --> |[+ 품목 추가]| ProductModal2[ProductSelectionModal<br/>Sell 필터]
    ProductModal2 --> |[품목 추가]| SaleWin
    ProductModal2 --> |[✕ 닫기]| SaleWin
    ProductModal2 --> |[+ 새 품목]| ProductEdit2[ProductEditModal]
    ProductEdit2 --> |[저장]| ProductModal2

    %% 견적서 화면에서
    QuoteWin --> |[+ 품목 추가]| ProductModal3[ProductSelectionModal<br/>Sell 필터]
    ProductModal3 --> |[품목 추가]| QuoteWin
    ProductModal3 --> |[✕ 닫기]| QuoteWin

    %% 품목 관리 화면에서
    ProductWin --> |[+ 품목 등록]| ProductEdit3[ProductEditModal]
    ProductWin --> |품목 행 더블클릭| ProductEdit4[ProductEditModal<br/>수정 모드]
    ProductEdit3 --> |[저장]| ProductWin
    ProductEdit3 --> |[취소]| ProductWin
    ProductEdit4 --> |[저장]| ProductWin
    ProductEdit4 --> |[취소]| ProductWin

    %% 재고 화면에서
    InvWin --> |[재고 조정]| InvAdjust[재고 조정 다이얼로그]
    InvAdjust --> |[저장]| InvWin
    InvAdjust --> |[취소]| InvWin

    %% 구매 화면에서
    PurchaseWin --> |[입고 처리]| DeliveryDlg[입고 처리 다이얼로그]
    DeliveryDlg --> |[저장]| PurchaseWin
    PurchaseWin --> |[불량 등록]| DefectWin[DefectWindow]
    DefectWin --> |[닫기]| PurchaseWin

    %% 스타일
    classDef primary fill:#e3f2fd,stroke:#2196f3,stroke-width:3px
    classDef modal fill:#fff3e0,stroke:#ff9800,stroke-width:2px
    classDef dialog fill:#fce4ec,stroke:#e91e63,stroke-width:2px
    classDef start fill:#e1f5e1,stroke:#4caf50,stroke-width:3px

    class Start start
    class CompanySelect,Workspace primary
    class OrderWin,SaleWin,QuoteWin,PurchaseWin,InvWin,ProductWin primary
    class ProductModal1,ProductModal2,ProductModal3,ProductEdit1,ProductEdit2,ProductEdit3,ProductEdit4 modal
    class ConfirmDlg,InvAdjust,DeliveryDlg dialog
```

---

## 🔄 화면 유형별 분류

### Level 0: Entry Point (진입점)
```
앱 시작 → CompanySelectionScreen
```
- **특징**: 앱 최초 실행 화면
- **전환**: 거래처 선택 후 MainWorkspace로 이동

---

### Level 1: Main Container (메인 컨테이너)
```
MainWorkspace
```
- **특징**:
  - 거래처 고정 상태 유지
  - 상단바 + 탭 구조
  - 모든 업무 화면의 부모
- **전환**:
  - 탭 클릭 → 콘텐츠 영역만 교체
  - [거래처 변경] → CompanySelectionScreen

---

### Level 2: Tab Contents (탭 콘텐츠)
```
OrderWindow (발주)
QuotationWindow (견적)
PurchaseWindow (구매)
SaleWindow (판매)
InventoryWindow (재고)
ProductManagementWindow (품목)
```
- **특징**:
  - MainWorkspace의 콘텐츠 영역에 표시
  - 탭 전환 시 교체됨
  - 각자 독립적인 상태 유지
- **전환**:
  - 모달 호출 (ProductSelectionModal 등)
  - 다이얼로그 호출 (확인/입력)

---

### Level 3: Modals (모달)
```
ProductSelectionModal (품목 선택)
ProductEditModal (품목 등록/수정)
```
- **특징**:
  - 팝업 형태
  - 부모 화면 Dim 처리
  - 작업 완료 후 부모로 복귀
- **전환**:
  - [확인]/[저장] → 부모 화면으로 데이터 전달 후 닫기
  - [취소]/[✕] → 데이터 전달 없이 닫기
  - ProductSelectionModal → ProductEditModal (연쇄 모달)

---

### Level 4: Dialogs (다이얼로그)
```
미저장 확인
재고 조정
입고 처리
```
- **특징**:
  - 모달보다 작은 크기
  - 단순 확인/입력 용도
  - 빠른 작업 완료
- **전환**:
  - [확인]/[저장] → 즉시 닫기
  - [취소] → 변경사항 없이 닫기

---

## 📊 전환 패턴 분석

### 패턴 1: 탭 전환 (Tab Switching)
```
MainWorkspace 내에서:
[발주] ↔ [견적] ↔ [구매] ↔ [판매] ↔ [재고] ↔ [품목]
```
- **방식**: 콘텐츠 영역만 교체, 상단바 유지
- **속도**: 즉시 (< 100ms)
- **데이터**: 각 탭은 독립 상태 유지

---

### 패턴 2: 모달 팝업 (Modal Popup)
```
부모 화면 → [+ 품목 추가] → ProductSelectionModal
               ↓
          [품목 추가] → 부모 화면 (데이터 전달)
```
- **방식**: 부모 화면 위에 Overlay
- **속도**: 즉시 (< 100ms)
- **데이터**: 모달 → 부모로 단방향 전달

---

### 패턴 3: 연쇄 모달 (Chained Modal)
```
부모 화면 → ProductSelectionModal → [+ 새 품목] → ProductEditModal
                                                      ↓
           부모 화면 ← ProductSelectionModal ← [저장]
```
- **방식**: 모달 위에 또 다른 모달
- **주의**: 2단계까지만 허용 (3중 모달 금지)
- **데이터**: 역순으로 전달 (ProductEdit → ProductSelect → 부모)

---

### 패턴 4: 거래처 전환 (Company Switching)
```
MainWorkspace(A병원) → [거래처 변경] → 미저장 확인
                                        ↓
                       CompanySelect → MainWorkspace(B도매)
```
- **방식**: 전체 화면 리로드
- **속도**: 느림 (데이터 재조회)
- **데이터**:
  - 이전 거래처 상태 파기
  - 새 거래처 데이터 로드

---

## 🎯 전환 빈도 및 최적화 전략

### 매우 빈번 (하루 50회 이상)
1. **탭 전환** (발주 ↔ 판매)
   - 최적화: 각 탭 내용 캐싱
   - 목표: < 100ms

2. **품목 선택 모달**
   - 최적화: 최근 검색어 캐싱, 자주 쓰는 품목 우선 표시
   - 목표: < 200ms

---

### 빈번 (하루 10~50회)
3. **임시저장/불러오기**
   - 최적화: 로컬 스토리지 사용
   - 목표: < 500ms

4. **수량 입력 → 금액 계산**
   - 최적화: 클라이언트 사이드 계산
   - 목표: 즉시 (< 50ms)

---

### 보통 (하루 2~10회)
5. **거래처 전환**
   - 최적화: 최근 5개 거래처 빠른 전환 드롭다운
   - 목표: < 1초

6. **품목 등록 (새 품목)**
   - 최적화: 필수 입력만 요구
   - 목표: < 3초

---

### 드물게 (하루 1~2회 이하)
7. **재고 조정**
   - 최적화: 불필요
   - 목표: < 2초

8. **설정 변경**
   - 최적화: 불필요

---

## 🚫 금지된 전환 패턴

### ❌ 3중 모달
```
부모 → Modal1 → Modal2 → Modal3 (금지!)
```
- **이유**: 사용자 혼란, 닫기 버튼 복잡도 증가
- **대안**: 최대 2단계까지만 허용

---

### ❌ 탭 내 탭
```
MainWorkspace의 [발주] 탭 → 발주 내부에 또 탭 (금지!)
```
- **이유**: 계층 혼란
- **대안**: 발주 내부는 3분할 레이아웃 + 하단 3탭 사용

---

### ❌ 자동 화면 전환
```
발주 저장 완료 → 자동으로 판매 탭으로 이동 (금지!)
```
- **이유**: 사용자 의도 무시
- **대안**: 현재 화면 유지 + 성공 메시지

---

### ❌ 거래처 변경 없이 다른 거래처 데이터 표시
```
현재: A병원 → 발주 화면에서 B도매 발주 내역 표시 (금지!)
```
- **이유**: Phase 1의 핵심 원칙 위반
- **대안**: 거래처 전환 후 조회

---

## 🔐 전환 시 검증 로직

### 1. 탭 전환 시
```typescript
onTabChange(newTab: Tab) {
    if (hasUnsavedData()) {
        showConfirmDialog({
            message: "저장하지 않은 데이터가 있습니다.",
            buttons: [
                { label: "임시저장", action: () => saveDraft() },
                { label: "저장 안 함", action: () => switchTab(newTab) },
                { label: "취소", action: () => stay() }
            ]
        });
    } else {
        switchTab(newTab);
    }
}
```

---

### 2. 모달 닫기 시
```typescript
onModalClose() {
    if (modal.hasChanges()) {
        showConfirmDialog({
            message: "변경사항을 저장하시겠습니까?",
            buttons: [
                { label: "저장", action: () => saveAndClose() },
                { label: "저장 안 함", action: () => closeWithoutSaving() },
                { label: "취소", action: () => stay() }
            ]
        });
    } else {
        modal.close();
    }
}
```

---

### 3. 거래처 전환 시
```typescript
onCompanySwitch(newCompany: Company) {
    // 1. 모든 탭에서 미저장 데이터 확인
    const unsavedTabs = getAllTabsWithUnsavedData();

    if (unsavedTabs.length > 0) {
        showConfirmDialog({
            message: `${unsavedTabs.length}개 탭에 미저장 데이터가 있습니다.`,
            buttons: [
                { label: "모두 임시저장", action: () => saveAllAndSwitch() },
                { label: "저장 안 함", action: () => switchCompany(newCompany) },
                { label: "취소", action: () => stay() }
            ]
        });
    } else {
        switchCompany(newCompany);
    }
}
```

---

## 📱 키보드 단축키 (계획)

| 단축키 | 동작 | 화면 |
|--------|------|------|
| `Ctrl + 1~6` | 탭 전환 (발주/견적/구매/판매/재고/품목) | MainWorkspace |
| `Ctrl + N` | 새 문서 작성 | 모든 입력 화면 |
| `Ctrl + S` | 임시저장 | 모든 입력 화면 |
| `Ctrl + Enter` | 확정/발송 | 모든 입력 화면 |
| `Ctrl + F` | 검색 포커스 | 모든 리스트 화면 |
| `Ctrl + P` | 품목 선택 모달 열기 | 발주/판매/견적 |
| `Esc` | 모달/다이얼로그 닫기 | 모든 모달 |
| `Ctrl + Q` | 거래처 변경 | MainWorkspace |

---

**작성자**: John (PM)
**버전**: 1.0
**최종 수정**: 2026-01-26

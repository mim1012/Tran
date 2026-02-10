# Tran Phase 1 UI/UX 설계 진행 현황

> **최종 업데이트**: 2026-01-26
> **담당자**: John (PM)
> **목적**: 기획자/개발자를 위한 UI/UX 설계 진행 상황 종합 문서

---

## 📊 전체 진행률

```
전체 설계 단계: 5단계
완료: 1단계 (20%)
진행중: 2단계
대기: 3~5단계
```

### ✅ 완료된 작업

#### 1단계: 화면 목록 및 우선순위 정의 ✅
- **문서**: `01-screen-inventory-phase1.md`
- **완료일**: 2026-01-26
- **산출물**:
  - 총 19개 화면 식별
  - 4단계 우선순위 분류 (P0~P4)
  - 화면 간 의존성 매핑
  - 각 화면별 목적/레이아웃/데이터 요구사항 정의

**주요 결과**:
- **P0 (최우선)**: 6개 화면 - MVP 구현에 필수
- **P1 (매우 중요)**: 4개 화면 - 핵심 기능
- **P2~P4**: 9개 화면 - 보조 기능

---

### 🔄 진행중인 작업

#### 2단계: 사용자 흐름도 및 다이어그램 작성 (진행중)
- **예상 산출물**:
  - 전체 사용자 여정 플로우차트
  - 화면 전환 다이어그램
  - 데이터 흐름도
- **예상 완료**: 2026-01-26 (오늘)

---

### ⏳ 대기중인 작업

#### 3단계: 와이어프레임 작성
- **범위**: P0 화면 6개 우선 작성
- **예상 소요**: 2-3시간
- **산출물**:
  1. CompanySelectionScreen (2컬럼 카드)
  2. MainWorkspace (상단바 + 탭)
  3. ProductSelectionModal (공통 모달)
  4. OrderWindow (3분할 레이아웃)
  5. SaleWindow (3분할 레이아웃)
  6. ProductManagementWindow (품목 마스터)

#### 4단계: 인터랙션 명세서
- **범위**: 모든 버튼/링크 동작 정의
- **예상 소요**: 1시간

#### 5단계: 예외 상황 가이드
- **범위**: 오류 처리, 빈 화면, 검색 결과 없음 등
- **예상 소요**: 30분

---

## 📁 문서 구조

```
docs/ui-ux-design/
├── 00-progress-summary.md          (이 문서)
├── 01-screen-inventory-phase1.md   (화면 목록)
│
├── diagrams/                        (다이어그램 폴더)
│   ├── 01-user-journey-flow.md     (사용자 여정)
│   ├── 02-screen-transition.md     (화면 전환)
│   ├── 03-data-flow.md             (데이터 흐름)
│   └── 04-architecture-overview.md (아키텍처 개요)
│
├── wireframes/                      (와이어프레임 폴더)
│   ├── P0-01-CompanySelectionScreen.md
│   ├── P0-02-MainWorkspace.md
│   ├── P0-03-ProductSelectionModal.md
│   ├── P0-04-OrderWindow.md
│   ├── P0-05-SaleWindow.md
│   └── P0-06-ProductManagementWindow.md
│
├── interactions/                    (인터랙션 명세)
│   ├── button-behaviors.md
│   ├── keyboard-shortcuts.md
│   └── validation-rules.md
│
└── edge-cases/                      (예외 상황)
    ├── error-handling.md
    ├── empty-states.md
    └── loading-states.md
```

---

## 🎯 Phase 1 핵심 개념 요약

### 거래처 고정 모드란?

**기존 방식** (Phase 2에서 구현 예정):
```
여러 거래처를 탭으로 동시에 열고 작업
(예: A병원 탭, B도매 탭, C병원 탭 동시 오픈)
```

**Phase 1 방식** (현재 구현 대상):
```
1. 앱 시작 시 거래처 1개 선택
2. 전체 앱이 해당 거래처로 고정
3. 모든 발주/판매/재고가 선택된 거래처만 표시
4. 다른 거래처 작업 시 명시적으로 전환 필요
```

**이유**:
- 구현 복잡도 감소
- 사용자 혼란 방지 (어느 거래처 작업중인지 명확)
- MVP 빠른 출시

---

## 🔑 핵심 화면 6개 (P0)

### 1. CompanySelectionScreen
**역할**: 앱 진입점
```
앱 시작 → 거래처 선택 화면 → 거래처 카드 클릭 → MainWorkspace
```

### 2. MainWorkspace
**역할**: 메인 작업 공간
```
┌──────────────────────────────────┐
│ [현재: A병원▼]  [거래처 변경]    │
├──────────────────────────────────┤
│ [발주][견적][구매][판매][재고]   │
├──────────────────────────────────┤
│ (선택된 탭의 콘텐츠)              │
└──────────────────────────────────┘
```

### 3. ProductSelectionModal
**역할**: 모든 입력 화면에서 품목 선택 시 사용
- 발주 → 구매 품목만 표시
- 판매 → 판매 품목만 표시
- 견적 → 판매 품목만 표시

### 4. OrderWindow (발주)
**역할**: 사는 것 입력
- 3분할 레이아웃
- 좌상: 품목 리스트
- 우상: 최근 거래 품목 (빠른 입력)
- 하단: 등록/임시저장/최근성사 3탭

### 5. SaleWindow (판매)
**역할**: 파는 것 입력
- OrderWindow와 동일한 3분할 레이아웃
- 차이점: 재고 표시 필수

### 6. ProductManagementWindow
**역할**: 품목 마스터 데이터 관리
- 구매 품목 (📥 Buy): 사는 것
- 판매 품목 (📤 Sell): 파는 것
- 둘다 (↔️ Both): 사기도 팔기도

---

## 🔄 핵심 사용자 흐름 (간단 버전)

```
1. 앱 시작
   ↓
2. 거래처 선택 화면 (2컬럼 카드)
   ↓
3. "A병원" 카드 클릭
   ↓
4. MainWorkspace 로드 (A병원 고정)
   ↓
5. [발주] 탭 클릭
   ↓
6. OrderWindow (3분할 레이아웃)
   ↓
7. 우상단 "최근 거래 품목"에서 수량만 입력
   ↓
8. [발주서 보내기] 클릭
   ↓
9. 발주 완료 → Order 생성
   ↓
10. (자동) Purchase 레코드 생성
    ↓
11. (자동) 입고 예정 수량 Inventory 반영
```

---

## 📊 데이터 흐름 (간단 버전)

### 발주 흐름
```
[임시저장] → DraftDocument (JSON)
[발주 확정] → Order (state=Requested)
            → Purchase 자동 생성 (state=PendingDelivery)
            → Inventory.PendingInQuantity 증가
```

### 판매 흐름
```
[임시저장] → DraftDocument (JSON)
[판매 확정] → Sale (state=Confirmed)
            → Inventory.ConfirmedQuantity 감소
            → Receivable 자동 생성 (채권)
```

### 견적 확정 흐름
```
[견적 확정] → Quotation (state=Confirmed)
            → CompanyPrice 자동 생성 (단가 정책)
            → CompanyProduct 자동 등록 (품목 리스트)
```

---

## 🎨 UI/UX 설계 원칙

### 1. 일관성
- 모든 입력 화면은 동일한 3분할 레이아웃 사용
- 품목 선택은 항상 ProductSelectionModal 사용
- 버튼 색상/크기/위치 일관성 유지

### 2. 효율성
- 최근 거래 품목 → 수량만 입력하면 완료 (2초 이내)
- 임시저장 → 언제든 작업 중단 가능
- 검색 → 실시간 필터링

### 3. 명확성
- 현재 거래처 항상 상단에 표시
- 상태 뱃지로 진행 상황 시각화
- 재고 부족 시 빨간색 경고

### 4. 오류 방지
- 재고 0인 품목 비활성화
- 필수 입력 항목 표시 (*)
- 거래처 전환 시 미저장 확인

---

## 📌 중요 결정 사항

### 1. 거래처 관리 화면은?
**결정 대기**: Phase 1에 포함 여부 확인 필요
- Option A: Phase 1에 포함 (거래처 추가/수정)
- Option B: Phase 2로 연기 (초기 데이터는 수동 입력)

### 2. 탭 vs 별도 창?
**결정**: MainWorkspace 내 탭 방식
- 발주/견적/구매/판매/재고 모두 탭으로 전환
- 별도 창 불필요

### 3. 3분할 레이아웃 재사용?
**결정**: ThreeColumnLayoutControl로 컴포넌트화
- OrderWindow와 SaleWindow에서 재사용
- 다른 화면에도 필요 시 재사용 가능

---

## 🚀 다음 액션 아이템

### 즉시 실행
- [ ] 2단계: 사용자 흐름도 완성 (Mermaid/Excalidraw)
- [ ] 화면 전환 다이어그램 작성
- [ ] 데이터 흐름도 작성

### 금주 목표
- [ ] 3단계: P0 화면 6개 와이어프레임 완성
- [ ] 4단계: 인터랙션 명세서 작성
- [ ] 5단계: 예외 상황 가이드 작성

### 이후 계획
- [ ] 개발자에게 인계
- [ ] XAML 구현 시작
- [ ] 프로토타입 테스트

---

## 📞 문의 사항

**기획 관련**:
- John (PM)

**기술 관련**:
- Claude Code

**문서 위치**:
- `docs/ui-ux-design/`

---

**작성자**: John (PM)
**최종 검토일**: 2026-01-26

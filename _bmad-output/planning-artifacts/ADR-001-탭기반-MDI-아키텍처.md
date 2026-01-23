# ADR-001: 탭 기반 MDI 워크스페이스 아키텍처

> **상태**: 제안됨
> **작성일**: 2026-01-23
> **의사결정자**: PC_1M, John (PM)
> **대체**: 현재 사이드바 기반 네비게이션 (MainWindow.xaml)

---

## 배경 (Context)

### 현재 시스템 (As-Is)

**아키텍처**: 사이드바 메뉴 + 모달 윈도우 방식

```
┌──────────────┬────────────────────────────────┐
│ [사이드바]   │  문서 리스트                   │
│ - 거래명세표 │  (DataGrid)                    │
│ - 거래처관리 │                                │
│ - 정산관리   │                                │
│ - 양식관리   │                                │
└──────────────┴────────────────────────────────┘
```

**한계점**:
- ❌ PRD Section 0 (거래처 탭 시스템)과 불일치
- ❌ 동시에 여러 작업 컨텍스트 유지 불가
- ❌ 거래처별 독립 작업 공간 부재
- ❌ 임시저장 시스템 구현 어려움
- ❌ 작업 전환 시 컨텍스트 손실

### PRD 요구사항 (To-Be)

**PRD Section 0: 공통 UX 원칙**
```
Level 1: [발주관리] [판매관리] [재고관리] [정산관리]
          ↓
Level 2: [+ 거래처 선택] [A병원 ✕] [B의원 ✕] [C도매 ✕]
          ↓
Level 3: 3분할 레이아웃 (품목 리스트 | 최근거래 품목 | 거래 내역)
```

**핵심 요구사항**:
1. **다중 작업 컨텍스트**: 발주, 판매, 재고를 동시에 작업
2. **거래처별 독립 상태**: 각 거래처 탭마다 독립된 데이터/상태 유지
3. **임시저장**: 작업 중단 → 탭 닫기 → 재열람 시 데이터 복원
4. **Chrome 스타일 탭**: 탭 추가, 닫기, 순서 변경 가능

---

## 의사결정 (Decision)

### ✅ 채택: **2단계 탭 기반 MDI 아키텍처**

```
┌──────────────────────────────────────────────────────────────────┐
│ [발주관리 ✕] [재고관리 ✕] [정산관리 ✕]  [+]   ← Level 1 (외부)  │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  [+ 거래처 선택] [A병원 ✕] [B의원 ✕]          ← Level 2 (내부)  │
│  ──────────────────────────────────────────────────────────────  │
│                                                                  │
│  ┌──────────────────────┬──────────────────────────────────┐    │
│  │ 📦 품목 리스트       │ ⭐ 최근거래 품목                  │    │
│  │                      │                                  │    │
│  ├──────────────────────┴──────────────────────────────────┤    │
│  │ 📋 최근 거래 내역                                        │    │
│  │ [등록] [임시저장] [성사내역]                             │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### 아키텍처 계층

#### 계층 1: 워크스페이스 탭 (Chrome 스타일)

**범위**: 작업 유형별 독립 공간

| 탭 유형 | 설명 | 콘텐츠 |
|---------|------|--------|
| 발주관리 | 병원 → 도매 발주 | 거래처 서브탭 + 3분할 레이아웃 |
| 구매관리 | 도매 → 공급사 구매 | 거래처 서브탭 + 3분할 레이아웃 |
| 판매관리 | 도매 → 병원 판매 | 거래처 서브탭 + 3분할 레이아웃 |
| 재고관리 | 입출고 현황 | 단일 화면 (거래처 필터) |
| 정산관리 | 미수금/채권 | 단일 화면 (거래처 필터) |
| 견적관리 | 견적서 작성 | 거래처 서브탭 + 3분할 레이아웃 |

**특징**:
- 각 탭 = 독립 ViewModel + 독립 상태
- `[+]` 버튼 → 새 워크스페이스 선택 모달
- `[✕]` 버튼 → 탭 닫기 (임시저장 확인)
- 탭 드래그 → 순서 변경 가능

#### 계층 2: 거래처 탭 (내부 탭)

**범위**: 발주/판매/견적 워크스페이스 내부에만 존재

```
[발주관리] 탭 내부:
  [+ 거래처 선택] [A병원 ✕] [B의원 ✕]

[재고관리] 탭 내부:
  (거래처 탭 없음 - 단일 화면)
```

**특징**:
- `[+ 거래처 선택]` 클릭 → 2컬럼 리스트 표시
- 거래처 선택 → 3분할 레이아웃 표시
- 각 거래처 탭 = 독립 임시저장 데이터

#### 계층 3: 콘텐츠 영역 (3분할)

**범위**: 거래처별 작업 화면

```
┌──────────────────────┬──────────────────────────┐
│ 좌상: 품목 리스트    │ 우상: 최근거래 품목      │
│                      │ (수량 입력 → 빠른 작성)  │
├──────────────────────┴──────────────────────────┤
│ 하단: 최근 거래 내역                            │
│ [등록] [임시저장] [성사내역] ← 3개 탭           │
└─────────────────────────────────────────────────┘
```

---

## 기술 스택 (Technology Stack)

### 선택: Dragablz

**Dragablz를 선택한 이유**:
- ✅ Chrome 스타일 탭 기본 제공
- ✅ MVVM 친화적 (ItemsSource 바인딩)
- ✅ 탭 드래그 & 순서 변경
- ✅ 탭 tear-off (창 분리) 지원
- ✅ 커스텀 탭 헤더 템플릿

**설치**:
```bash
dotnet add package Dragablz --version 0.0.3.223
```

**기각된 대안**:
- ❌ AvalonDock: 너무 복잡 (Docking, Floating 기능 불필요)
- ❌ Custom TabControl: 구현 비용 높음

### 컴포넌트 구조

```
Tran.Desktop/
├── Views/
│   ├── WorkspaceShell.xaml               ← 메인 MDI 컨테이너
│   ├── Workspaces/
│   │   ├── OrderWorkspace.xaml           ← [발주관리] 탭
│   │   ├── SalesWorkspace.xaml           ← [판매관리] 탭
│   │   ├── InventoryWorkspace.xaml       ← [재고관리] 탭
│   │   └── SettlementWorkspace.xaml      ← [정산관리] 탭
│   ├── PartnerTabs/
│   │   ├── PartnerSelectorTab.xaml       ← [+ 거래처 선택]
│   │   └── PartnerDetailTab.xaml         ← [A병원] 상세
│   └── Shared/
│       ├── ProductListPanel.xaml         ← 품목 리스트 (좌상)
│       ├── RecentProductsPanel.xaml      ← 최근거래 품목 (우상)
│       └── TransactionHistoryPanel.xaml  ← 거래 내역 (하단)
└── ViewModels/
    ├── WorkspaceShellViewModel.cs        ← MDI 컨테이너 VM
    ├── Workspaces/
    │   ├── OrderWorkspaceViewModel.cs
    │   ├── SalesWorkspaceViewModel.cs
    │   ├── InventoryWorkspaceViewModel.cs
    │   └── SettlementWorkspaceViewModel.cs
    └── PartnerTabs/
        ├── PartnerSelectorViewModel.cs
        └── PartnerDetailViewModel.cs
```

---

## 데이터 흐름 & 상태 관리

### 1. 워크스페이스 생명주기

```
[*] → 생성됨 (사용자가 [+] 클릭)
  ↓
활성 (탭 선택됨)
  ↓
일시중단 (다른 탭 선택)
  ↓
활성 (탭 재선택)
  ↓
저장 중 (사용자가 [✕] 클릭)
  ↓
닫힘 (저장 완료)
  ↓
[*]
```

### 2. 상태 지속성

**임시저장 (Temporary Save)**:
```csharp
public class WorkspaceState
{
    public string WorkspaceType { get; set; }  // "Order", "Sales"
    public List<PartnerTabState> PartnerTabs { get; set; }
    public DateTime LastModified { get; set; }
}

public class PartnerTabState
{
    public string CompanyId { get; set; }
    public List<DocumentItemDto> DraftItems { get; set; }  // 임시저장 품목
    public string ActiveSubTab { get; set; }  // "Register", "Draft", "History"
}
```

**저장 위치**:
- SQLite: `WorkspaceStates` 테이블
- JSON: `{UserId}_{WorkspaceType}_{CompanyId}.json`

### 3. 네비게이션 흐름

```
사용자 액션: [+] 클릭 (워크스페이스 추가)
  ↓
WorkspaceShellViewModel.AddWorkspaceCommand
  ↓
모달 표시: "작업 선택" (발주/판매/재고/정산)
  ↓
Workspace ViewModel 생성
  ↓
ObservableCollection<WorkspaceViewModel>에 추가
  ↓
Dragablz가 ItemsSource에 바인딩
  ↓
[✕] 버튼이 있는 새 탭 표시됨
```

---

## 마이그레이션 전략

### Phase 1: 인프라 구축 (1주차)

**목표**:
- ✅ Dragablz 설치
- ✅ WorkspaceShell.xaml 스켈레톤 생성
- ✅ 기본 탭 추가/제거 기능

**산출물**:
1. Dragablz가 포함된 `WorkspaceShell.xaml`
2. 탭 컬렉션이 있는 `WorkspaceShellViewModel`
3. 샘플 워크스페이스 (OrderWorkspace 스텁)

### Phase 2: 기존 화면 변환 (2-3주차)

**전략**: Window → UserControl

| 현재 Window | 새 컴포넌트 | 비고 |
|-------------|-------------|------|
| MainWindow.xaml | WorkspaceShell.xaml | 완전 교체 |
| CreateDocumentWindow | OrderWorkspace | 발주관리 탭 내부 |
| PartnerManagementWindow | (모달 유지) | 거래처 마스터 관리 |
| SettlementManagementWindow | SettlementWorkspace | 정산관리 탭 |

### Phase 3: 3분할 레이아웃 구현 (4주차)

**목표**:
- 품목 리스트 (좌상)
- 최근거래 품목 (우상)
- 거래 내역 (하단 3탭)

### Phase 4: 상태 지속성 (5주차)

**목표**:
- 임시저장 DB 스키마
- WorkspaceState 직렬화
- 탭 복원 로직

---

## 리스크 & 완화 방안

| 리스크 | 영향도 | 완화 방안 |
|--------|--------|----------|
| Dragablz 학습 곡선 | 중간 | 2-3일 POC로 프로토타입 제작 |
| 기존 코드 리팩토링 범위 | 높음 | 점진적 마이그레이션 (Phase 2) |
| 임시저장 데이터 충돌 | 중간 | UserId + WorkspaceType + CompanyId 복합키 |
| 성능 (다중 탭 메모리) | 낮음 | Lazy loading + 탭 일시중단 |

---

## 성공 지표

**필수 (Must Have)**:
- [ ] Chrome 스타일 탭 동작 (추가/닫기/순서변경)
- [ ] 발주관리 워크스페이스 완성 (거래처 서브탭 + 3분할)
- [ ] 임시저장 → 탭 닫기 → 재열람 시 복원

**선택 (Nice to Have)**:
- [ ] 탭 드래그로 창 분리 (Tear-off)
- [ ] 키보드 단축키 (Ctrl+T: 새 탭, Ctrl+W: 탭 닫기)
- [ ] 탭 아이콘 + 뱃지 (진행 중인 문서 개수)

---

## 참조 문서

- **PRD**: `docs/prd.md` (Section 0, Section 11)
- **Features**: `docs/features/00-common-ux.md`
- **Dragablz**: https://github.com/ButchersBoy/Dragablz
- **현재 코드**: `Tran.Desktop/MainWindow.xaml`

---

## 승인

**Product Manager**: ✅ John (2026-01-23)
**Technical Lead**: ⏳ 검토 대기
**Stakeholder (PC_1M)**: ⏳ 승인 대기

---

## 다음 단계

1. **이 ADR 검토** → 이해관계자 승인 획득
2. **아키텍처 문서 작성** (컴포넌트 상세 설계)
3. **프로토타입 제작** → Dragablz POC (2-3일)
4. **구현** → Phase 1-4 로드맵 따라 진행

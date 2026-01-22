# 정산 관리 화면 (SettlementManagementWindow) 구현 가이드

## 개요
정산 관리 화면은 **읽기 전용 관찰 레이어**로, `DocumentState.Confirmed` 상태의 문서만을 집계하고 조회합니다. 문서의 수정/삭제는 불가능하며, 거래처별 집계와 상세 문서 목록 조회만 제공합니다.

## 아키텍처

### 1. Repository 패턴 적용
```
┌─────────────────────────────────────────────┐
│ SettlementManagementViewModel              │
│ - READ-ONLY Commands (조회/새로고침만)      │
└─────────────────┬───────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────┐
│ IDocumentQueryService (인터페이스)          │
│ - GetSettlementSummariesAsync()             │
│ - GetConfirmedDocumentsByCompanyAsync()     │
└─────────────────┬───────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────┐
│ DocumentQueryService (구현체)               │
│ - DbContext 직접 접근                       │
│ - EF Core LINQ 쿼리                         │
└─────────────────────────────────────────────┘
```

### 2. 생성된 파일 목록
```
D:\Project\Tran\
├── Tran.Core\
│   └── Services\
│       └── IDocumentQueryService.cs       ✅ 신규 생성
│
├── Tran.Data\
│   └── Services\
│       └── DocumentQueryService.cs        ✅ 신규 생성
│
└── Tran.Desktop\
    ├── ViewModels\
    │   └── SettlementManagementViewModel.cs  ✅ 신규 생성
    ├── SettlementManagementWindow.xaml       ✅ 신규 생성
    └── SettlementManagementWindow.xaml.cs    ✅ 신규 생성
```

## 주요 기능

### 1. 기간별 거래처 집계
- **대상 문서:** `DocumentState.Confirmed`만
- **집계 항목:**
  - 거래처명
  - 총 거래건수
  - 총 금액
  - 평균 금액
- **정렬:** 총 금액 내림차순

### 2. 거래처별 문서 목록 조회
- 집계 행 선택 시 자동으로 해당 거래처의 문서 목록 표시
- 문서 더블클릭 → `DocumentDetailWindow` 열기

### 3. UI 특징
- **READ ONLY 배너:** 상단에 읽기 전용 안내 표시
- **색상 테마:** Confirmed 상태 색상 (#E6F4EA/#1E7F34)
- **금액 포맷:** `{0:N0}원` (천 단위 구분)
- **로딩 상태:** 데이터 로드 중 오버레이 표시

## 사용법

### DI 등록 (Program.cs 또는 App.xaml.cs)
```csharp
using Microsoft.Extensions.DependencyInjection;
using Tran.Core.Services;
using Tran.Data.Services;

// ServiceCollection에 등록
services.AddDbContext<TranDbContext>(options =>
    options.UseSqlite("Data Source=tran.db"));

services.AddScoped<IDocumentQueryService, DocumentQueryService>();
services.AddTransient<SettlementManagementViewModel>();
services.AddTransient<SettlementManagementWindow>();
```

### 화면 열기
```csharp
// 방법 1: DI Container에서 가져오기 (권장)
var serviceProvider = ...; // DI Container
var window = serviceProvider.GetRequiredService<SettlementManagementWindow>();
window.ShowDialog();

// 방법 2: 수동 생성
var dbContext = ...; // DbContext 인스턴스
var queryService = new DocumentQueryService(dbContext);
var viewModel = new SettlementManagementViewModel(queryService);
var window = new SettlementManagementWindow(viewModel);
window.ShowDialog();
```

## 데이터 흐름

### 1. 초기 로드 (Window.Loaded)
```
Window.Loaded
    ↓
ViewModel.LoadSummariesCommand.Execute()
    ↓
DocumentQueryService.GetSettlementSummariesAsync(fromDate, toDate)
    ↓
GROUP BY ToCompanyId → 집계
    ↓
ObservableCollection<SettlementSummary> 바인딩
```

### 2. 거래처 선택 시
```
DataGrid.SelectedItem 변경
    ↓
ViewModel.SelectedSummary.set
    ↓
ViewModel.LoadDocumentsCommand.Execute(summary)
    ↓
DocumentQueryService.GetConfirmedDocumentsByCompanyAsync()
    ↓
ObservableCollection<Document> 바인딩
```

### 3. 문서 더블클릭 시
```
DataGrid.MouseDoubleClick
    ↓
new DocumentDetailViewModel(documentId)
    ↓
new DocumentDetailWindow(viewModel)
    ↓
ShowDialog()
```

## 쿼리 최적화

### DocumentQueryService.GetSettlementSummariesAsync()
```csharp
// EF Core LINQ 쿼리
var summaries = await _context.Documents
    .Where(d => d.State == DocumentState.Confirmed)      // 인덱스 사용
    .Where(d => d.TransactionDate >= fromDate
             && d.TransactionDate <= toDate)             // 범위 검색
    .GroupBy(d => d.ToCompanyId)                         // 거래처별 집계
    .Select(g => new {
        CompanyId = g.Key,
        TotalCount = g.Count(),
        TotalAmount = g.Sum(x => x.TotalAmount),
        AverageAmount = g.Average(x => x.TotalAmount)
    })
    .ToListAsync();
```

### 인덱스 권장사항
```sql
-- TranDbContext.OnModelCreating()에 이미 정의됨
CREATE INDEX idx_documents_state ON Documents(State);
CREATE INDEX idx_documents_company ON Documents(FromCompanyId, ToCompanyId);

-- 추가 권장 인덱스
CREATE INDEX idx_documents_transaction_date ON Documents(TransactionDate);
CREATE INDEX idx_documents_state_date ON Documents(State, TransactionDate);
```

## 제약사항 및 주의사항

### ✅ 준수사항
1. **READ-ONLY:** ViewModel에 Save/Update/Delete Command 없음
2. **Repository 패턴:** ViewModel이 DbContext 직접 접근 금지
3. **Confirmed만:** 다른 상태의 문서는 집계 대상 아님
4. **불변성:** Confirmed 상태는 변경 불가능

### ❌ 금지사항
1. ViewModel에서 `context.Documents.Where(...)` 직접 사용
2. 문서 수정/삭제 기능 추가
3. Confirmed 이외의 상태 포함

## 확장 가능성

### 1. 엑셀 내보내기
```csharp
// ViewModel에 추가
public ICommand ExportToExcelCommand { get; }

private void OnExportToExcel()
{
    // EPPlus 또는 ClosedXML 사용
    var exporter = new ExcelExporter();
    exporter.Export(Summaries, Documents);
}
```

### 2. 기간 프리셋
```csharp
// XAML에 추가
<ComboBox>
    <ComboBoxItem Content="이번 달"/>
    <ComboBoxItem Content="지난 달"/>
    <ComboBoxItem Content="이번 분기"/>
</ComboBox>
```

### 3. 차트 시각화
```csharp
// LiveCharts 또는 OxyPlot 사용
<lvc:CartesianChart>
    <lvc:CartesianChart.Series>
        <lvc:ColumnSeries Values="{Binding ChartValues}"/>
    </lvc:CartesianChart.Series>
</lvc:CartesianChart>
```

## 테스트 시나리오

### 1. 정상 시나리오
```
1. 화면 열기
2. 기본 기간(이번 달) 집계 자동 로드
3. 거래처 행 선택 → 문서 목록 표시
4. 문서 더블클릭 → 상세 창 열림
5. 상세 창에서 "확정됨" 상태 확인 (수정 버튼 없음)
```

### 2. 경계값 테스트
```
- 기간 내 Confirmed 문서 0건
- 거래처 1개만 존재
- 거래처 1000개 이상 (성능 테스트)
- From > To (잘못된 기간 입력)
```

### 3. 동시성 테스트
```
- 정산 조회 중 다른 사용자가 문서 확정
- 새로고침 버튼으로 최신 데이터 반영 확인
```

## 트러블슈팅

### 문제: "데이터가 표시되지 않습니다"
```
✅ 체크리스트:
1. DB에 Confirmed 상태 문서가 실제로 존재하는가?
2. 선택한 기간이 올바른가?
3. IDocumentQueryService가 DI 컨테이너에 등록되었는가?
4. DbContext 연결 문자열이 올바른가?
```

### 문제: "컴파일 오류"
```
✅ 체크리스트:
1. Tran.Core 프로젝트 먼저 빌드
2. Tran.Data 프로젝트 빌드
3. Tran.Desktop 프로젝트 빌드
4. 누락된 using 구문 추가
```

### 문제: "성능 저하"
```
✅ 최적화 방법:
1. 인덱스 추가 (State, TransactionDate)
2. 페이징 구현 (Skip/Take)
3. 가상화 (DataGrid.EnableRowVirtualization="True")
4. 비동기 로드 표시 (IsLoading 상태)
```

## 관련 문서
- [PRD: 거래명세표 관리 시스템](../PRD.md)
- [상태 전이 다이어그램](../StateDiagram.md)
- [DB 스키마 설계](../DatabaseSchema.md)

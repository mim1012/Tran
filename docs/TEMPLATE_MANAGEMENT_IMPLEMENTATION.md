# 양식 관리 화면 구현 완료

## 개요
완전한 읽기 전용 양식 관리 화면(TemplateManagementWindow)을 구현했습니다.

## 핵심 철학
**"양식 관리 = 관찰 전용 레이어"**
- 편집/저장 기능 절대 금지
- ICommand 구현 없음
- 읽기 및 조회 기능만 제공

## 구현 파일

### 1. TemplateManagementViewModel.cs
위치: `D:\Project\Tran\Tran.Desktop\ViewModels\TemplateManagementViewModel.cs`

**주요 기능:**
- DocumentTemplate 목록 조회 (필터링 지원)
- 거래처별 필터링
- 활성/비활성 상태 필터링
- JSON 미리보기 (Schema, Layout)

**제약사항 준수:**
- ICommand 구현 없음 (조회용 메서드만)
- 편집/저장 로직 없음
- 비동기 로드 메서드만 제공

**주요 메서드:**
```csharp
public async Task InitializeAsync()
public async Task LoadTemplatesAsync()
private async Task LoadCompaniesAsync()
private void LoadTemplatePreview()
```

**속성:**
- `ObservableCollection<TemplateDisplayItem> Templates`: 템플릿 목록
- `ObservableCollection<CompanyFilterItem> Companies`: 거래처 필터 목록
- `TemplateDisplayItem? SelectedTemplate`: 선택된 템플릿
- `bool ShowActiveOnly`: 활성 상태 필터
- `string? SelectedCompanyId`: 선택된 거래처 ID
- `string SchemaJsonPreview`: Schema JSON 미리보기 (포맷팅됨)
- `string LayoutJsonPreview`: Layout JSON 미리보기 (포맷팅됨)

### 2. TemplateManagementWindow.xaml
위치: `D:\Project\Tran\Tran.Desktop\TemplateManagementWindow.xaml`

**UI 구성:**

#### 상단 헤더
- 제목: "양식 관리"
- 부제: "템플릿 조회 전용 화면 (편집 불가)"
- 읽기 전용 모드 표시

#### 좌측: 템플릿 목록 (2*)
- 필터 영역:
  - 거래처 ComboBox
  - 활성/비활성 ComboBox
  - 새로고침 버튼
  - 총 템플릿 개수 표시
- DataGrid:
  - 상태 (활성/비활성 Pill)
  - 양식명
  - 거래처
  - 타입 (거래명세표/정산서)
  - 생성일

#### 우측: JSON 미리보기 (3*)
- 상단: SchemaJson (Monospace 폰트)
- 하단: LayoutJson (Monospace 폰트)
- 둘 다 JSON 포맷팅 적용

#### 하단 버튼 영역
- 안내 메시지: "이 화면은 조회 전용입니다. 편집/저장 기능은 제공하지 않습니다."
- 닫기 버튼만 제공

**스타일 가이드:**
- 회색 톤 배경 (#F8F9FA)
- 헤더 색상: #868E96
- 활성 Pill: #E6F4EA / #1E7F34
- 비활성 Pill: #F5F5F5 / #868E96
- JSON 폰트: Consolas, Courier New

### 3. TemplateManagementWindow.xaml.cs
위치: `D:\Project\Tran\Tran.Desktop\TemplateManagementWindow.xaml.cs`

**Code-Behind 로직:**
- DbContext 직접 생성 (DI 없이)
- ViewModel 초기화 및 DataContext 바인딩
- 비동기 로드 (Loaded 이벤트)

**이벤트 핸들러:**
```csharp
private void ActiveFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
private async void RefreshButton_Click(object sender, RoutedEventArgs e)
private void CloseButton_Click(object sender, RoutedEventArgs e)
```

### 4. MainWindow.xaml.cs 수정
위치: `D:\Project\Tran\Tran.Desktop\MainWindow.xaml.cs`

**변경 사항:**
```csharp
private void TemplateManagement_Click(object sender, RoutedEventArgs e)
{
    var templateWindow = new TemplateManagementWindow();
    templateWindow.Owner = this;
    templateWindow.ShowDialog();
}
```

## 데이터 모델

### DocumentTemplate (Tran.Core.Models)
```csharp
public class DocumentTemplate
{
    public string TemplateId { get; set; }
    public string CompanyId { get; set; }
    public string TemplateName { get; set; }
    public TemplateType TemplateType { get; set; }
    public string SchemaJson { get; set; }
    public string LayoutJson { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public Company? Company { get; set; }
}
```

### TemplateDisplayItem (ViewModel용)
```csharp
public class TemplateDisplayItem
{
    public string TemplateId { get; set; }
    public string TemplateName { get; set; }
    public string CompanyName { get; set; }
    public string TemplateTypeDisplay { get; set; }
    public bool IsActive { get; set; }
    public string ActiveStatusDisplay { get; set; }
    public DateTime CreatedAt { get; set; }
    public string SchemaJson { get; set; }
    public string LayoutJson { get; set; }

    // UI 바인딩 속성
    public string StatusBackground { get; }
    public string StatusForeground { get; }
}
```

## JSON 포맷팅

```csharp
// System.Text.Json 사용
var formattedJson = JsonSerializer.Serialize(
    JsonSerializer.Deserialize<object>(template.SchemaJson),
    new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    }
);
```

**오류 처리:**
- JSON 파싱 실패 시 에러 메시지와 원본 JSON 표시

## 빌드 및 실행

### 빌드
```bash
cd D:\Project\Tran
dotnet build Tran.Desktop/Tran.Desktop.csproj --configuration Debug
```

### 실행
1. Visual Studio에서 실행 또는
2. `dotnet run --project Tran.Desktop/Tran.Desktop.csproj`

### 접근 방법
1. MainWindow에서 좌측 사이드바의 "양식 관리" 버튼 클릭
2. TemplateManagementWindow 다이얼로그 표시

## 아키텍처 제약사항 준수 체크

- [x] ICommand 구현 없음
- [x] Save/Update/Delete 버튼 없음
- [x] 편집 로직 없음
- [x] 닫기 버튼만 존재
- [x] JSON 파싱 오류 처리
- [x] 이모지 사용 금지
- [x] MVVM 패턴 준수
- [x] 읽기 전용 시각화 (회색 톤)

## 사용자 경험 (UX)

### 초기 로드
1. 창이 열리면 자동으로 템플릿 목록 로드
2. 기본 필터: "활성만" 표시
3. 거래처: "전체" 선택

### 필터링
1. 거래처 선택 시 → 자동 재로드
2. 활성/전체 변경 시 → 자동 재로드
3. 새로고침 버튼 → 수동 재로드

### 템플릿 선택
1. DataGrid에서 템플릿 클릭
2. 우측 JSON 미리보기 영역 자동 업데이트
3. SchemaJson, LayoutJson 포맷팅되어 표시

### 닫기
1. 하단 "닫기" 버튼
2. 창 종료

## 향후 개선 가능 사항

1. **JSON 트리뷰:** 현재는 텍스트 포맷팅이지만, 트리뷰 컨트롤 추가 가능
2. **검색 기능:** 양식명 검색 필터 추가
3. **정렬:** 컬럼 헤더 클릭 시 정렬
4. **페이징:** 대량 데이터 시 페이징 처리
5. **내보내기:** JSON 파일로 내보내기 (읽기 전용 유지)

## 주의사항

1. **편집 금지:** 이 화면은 절대 편집 기능을 추가하지 않음
2. **관찰 전용:** "템플릿을 어떻게 사용하는지 확인하는 화면"
3. **철학적 시그널:** "양식은 조회만 가능"이라는 명확한 메시지 전달

## 결론

완전한 읽기 전용 양식 관리 화면을 성공적으로 구현했습니다.
- ICommand 없음
- 편집 버튼 없음
- JSON 미리보기 제공
- 필터링 지원
- 깔끔한 회색 톤 UI

이 화면은 "관찰 전용 레이어"로서 시스템의 철학적 경계를 명확히 합니다.

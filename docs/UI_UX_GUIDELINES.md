# Tran 시스템 UI/UX 가이드라인

## 1️⃣ 전체 UI 철학 (먼저 고정)

### 핵심 원칙
> **눈에 띄어야 하는 건 '행동'이 아니라 '상태'다**

```
버튼은 조용하게
상태는 과감하게
색은 의미 전달용이지 장식 ❌
```

---

## 2️⃣ 상태 색상 규칙 (절대 통일)

### 상태별 색상 매핑표

| 상태 | 배지 배경 | 배지 텍스트 | 행 배경 | 의미 |
|------|----------|------------|---------|------|
| **작성중** | `#F0F0F0` (연한 회색) | `#555555` (회색) | 흰색 | 아직 결정 안 됨 |
| **전송됨** | `#E8F1FF` (연한 파랑) | `#1E5EFF` (파랑) | 흰색 | 상대방 대기 |
| **확정됨** | `#E6F4EA` (연한 초록) | `#1E7F34` (초록) | 흰색 | 거래 고정 |
| **수정요청** | `#FFF4E5` (연한 주황) | `#E67700` (주황) | 연한 주황 | 액션 필요 |
| **구버전** | `#F5F5F5` (연회색) | `#868E96` (회색) | 회색 | 참고용 |
| **오류/위변조** | `#FFE5E5` (연한 빨강) | `#C92A2A` (빨강) | 연한 빨강 | 중단 |

### 구현 위치
```csharp
// Tran.Desktop/ViewModels/DocumentViewModel.cs
public string StatePillBackground => State switch
{
    DocumentState.Draft => "#F0F0F0",
    DocumentState.Sent => "#E8F1FF",
    DocumentState.Received => "#FFF4E5",
    DocumentState.RevisionRequested => "#FFF4E5",
    DocumentState.Confirmed => "#E6F4EA",
    DocumentState.Superseded => "#F5F5F5",
    DocumentState.Cancelled => "#FFE5E5",
    _ => "#F0F0F0"
};
```

### 절대 규칙
- ✅ 이 색상은 **전 화면 공통**
- ❌ "이 화면에서는 다른 색" 절대 금지
- ❌ 장식용 색상 사용 금지

---

## 3️⃣ 리스트(표) 행 강조 규칙

### 기본 상태
```xaml
<DataGridRow Background="White" />
<DataGridRow Background="#F8F9FA" /> <!-- Hover 시 -->
```

### 선택된 행
```xaml
<DataGridRow Background="#1E5EFF">  <!-- 짙은 파랑 -->
    <TextBlock Foreground="White" /> <!-- 글자 흰색 -->
</DataGridRow>
```

### 확정된 문서
```xaml
<DataGridRow BorderThickness="3,0,0,0" BorderBrush="#1E7F34">
    <!-- 왼쪽에 초록 세로 바 -->
    <!-- 행 전체는 흰색 유지 -->
</DataGridRow>
```

### 작성중 문서
```xaml
<DataGridRow BorderThickness="3,0,0,0" BorderBrush="#D0D0D0">
    <!-- 왼쪽에 회색 세로 바 -->
    <!-- 행 전체 색칠 ❌ -->
</DataGridRow>
```

### 원칙
- ⭕ 왼쪽 상태 바 + 배지가 핵심
- ❌ 행 전체 배경색 변경 금지 (가독성 저하)

---

## 4️⃣ 버튼 UX 규칙 (중요)

### 버튼 3종류만 존재

| 유형 | 예시 | 색상 | 구현 |
|------|------|------|------|
| **Primary** | 전송 / 확정 | `#3498DB` (파랑) | `Background="#3498DB" Foreground="White"` |
| **Secondary** | 저장 / 닫기 | `#95A5A6` (회색) | `Background="#95A5A6" Foreground="White"` |
| **Destructive** | 삭제 | `#E74C3C` (빨강) | `Background="#E74C3C" Foreground="White"` |

### Disabled 버튼 UX
```xaml
<Button IsEnabled="False" Opacity="0.5">
    <Button.ToolTip>
        <ToolTip>
            <TextBlock Text="확정된 문서는 수정할 수 없습니다" />
        </ToolTip>
    </Button.ToolTip>
</Button>
```

### 규칙
- ✅ 흐릿하게 (Opacity="0.5")
- ✅ Hover 시 **Tooltip 필수**
- ✅ 왜 안 되는지 **항상 설명**

---

## 5️⃣ 입력 필드 UX 규칙

### 작성 가능
```xaml
<TextBox Background="White"
         BorderBrush="#D0D0D0"
         BorderThickness="1" />
```

### Read-only
```xaml
<TextBox Background="#F0F0F0"
         IsReadOnly="True"
         Cursor="Arrow"
         Focusable="False" />
```

### 원칙
> **Read-only 필드는 "보이지만 만질 수 없다"는 인상을 줘야 함**

- ✅ 연회색 배경
- ✅ 커서 비활성
- ✅ 클릭해도 반응 ❌

---

## 6️⃣ 경고 / 알림 UX

### ❌ 팝업 남발 금지

### ⭕ 상태 기반 알림
1. **상단 상태바 메시지**
   ```xaml
   <Border Background="#FFF4E5" Padding="10">
       <TextBlock Text="수정 요청된 문서가 3건 있습니다" Foreground="#E67700" />
   </Border>
   ```

2. **리스트 배지 변화**
   - 상태 변경 시 자동으로 배지 색상 변경

3. **상세 화면 상단 배너**
   ```xaml
   <Border Background="#E6F4EA" Padding="10" Margin="0,0,0,10">
       <TextBlock Text="✓ 이 문서는 확정되었습니다" Foreground="#1E7F34" FontWeight="Bold" />
   </Border>
   ```

### 팝업은 오직 3가지만
1. **전송** 확인
2. **확정** 확인
3. **삭제** 확인

---

## 7️⃣ ViewModel 분리 구조

### ViewModel 트리 구조
```
MainViewModel
 ├─ DocumentListViewModel        (거래명세표)
 ├─ PartnerViewModel             (거래처 관리)
 ├─ SettlementViewModel          (정산 관리)
 ├─ TemplateViewModel            (양식 관리)
 ├─ AuditLogViewModel            (로그 및 이력)
 └─ SettingsViewModel            (설정)
```

### 핵심 원칙
> **사이드바 메뉴 = ViewModel 경계**

- ✅ 메뉴 하나 = ViewModel 하나
- ❌ 절대 섞지 않는다

---

## 8️⃣ DocumentListViewModel (Core)

### 책임
- ✅ 문서 목록 로딩
- ✅ 상태 필터
- ✅ 선택 문서 관리

### 절대 금지
- ❌ 정산 계산
- ❌ 보고서 로직
- ❌ 설정 접근

### 구현 예시
```csharp
public class DocumentListViewModel : ViewModelBase
{
    public ObservableCollection<DocumentViewModel> Documents { get; set; }
    public DocumentState? StateFilter { get; set; }
    public DocumentViewModel? SelectedDocument { get; set; }

    public ICommand LoadDocumentsCommand { get; }
    public ICommand FilterByStateCommand { get; }
}
```

👉 **가장 엄격하게 관리**

---

## 9️⃣ PartnerViewModel (거래처 관리)

### 책임
- ✅ 거래처 주소록
- ✅ 연결 상태 표시

### 특징
- ❌ 문서 상태에 영향 ❌
- ⭕ 단순 CRUD + 상태 표시

### 구현 예시
```csharp
public class PartnerViewModel : ViewModelBase
{
    public ObservableCollection<Company> Companies { get; set; }
    public Company? SelectedCompany { get; set; }

    public string ConnectionStatus { get; set; }  // "미연결" / "연결됨"
    public DateTime? LastTransactionDate { get; set; }
}
```

---

## 🔟 SettlementViewModel (정산 관리)

### 책임
- ✅ CONFIRMED 문서 조회
- ✅ 합계 계산
- ✅ Export

### 데이터 원칙
> **읽기만 한다**

```csharp
public class SettlementViewModel : ViewModelBase
{
    // Read-only
    public IReadOnlyCollection<Document> ConfirmedDocuments { get; set; }
    public decimal TotalAmount { get; set; }

    // Export만
    public ICommand ExportToExcelCommand { get; }
    public ICommand ExportToPdfCommand { get; }
}
```

👉 **documents를 절대 수정하지 않음**

---

## 1️⃣1️⃣ TemplateViewModel (양식 관리)

### 책임
- ✅ 출력 양식 관리
- ✅ 미리보기

### 절대 규칙
- ❌ 해시 계산 ❌
- ❌ 문서 내용 변경 ❌

### 구현 예시
```csharp
public class TemplateViewModel : ViewModelBase
{
    public ObservableCollection<DocumentTemplate> Templates { get; set; }
    public DocumentTemplate? SelectedTemplate { get; set; }

    // 미리보기만
    public ICommand PreviewTemplateCommand { get; }
}
```

---

## 1️⃣2️⃣ AuditLogViewModel (로그)

### 책임
- ✅ 상태 변경 이력 조회
- ✅ Export

### UX
- ❌ 수정 버튼 ❌
- ❌ 삭제 버튼 ❌

### 구현 예시
```csharp
public class AuditLogViewModel : ViewModelBase
{
    // Read-only
    public IReadOnlyCollection<DocumentStateLog> Logs { get; set; }

    // Export만
    public ICommand ExportLogsCommand { get; }
}
```

---

## 1️⃣3️⃣ SettingsViewModel (설정)

### 책임
- ✅ 환경 설정
- ✅ 백업
- ✅ 업데이트 상태

### 절대 규칙
> **설정 변경이 문서에 영향 ❌**

### 구현 예시
```csharp
public class SettingsViewModel : ViewModelBase
{
    public string CompanyName { get; set; }
    public string CompanyLogo { get; set; }
    public bool AutoBackupEnabled { get; set; }
    public string CurrentVersion { get; set; }
}
```

---

## 1️⃣4️⃣ ViewModel 간 통신 규칙

### ❌ 직접 참조 금지
```csharp
// 나쁜 예
SettlementViewModel.DocumentList = DocumentListViewModel.Documents;
```

### ⭕ Event / Message 기반
```csharp
// 좋은 예 - EventAggregator 패턴
public class DocumentSentEvent
{
    public string DocumentId { get; set; }
}

// Publish
_eventAggregator.Publish(new DocumentSentEvent { DocumentId = "DOC-001" });

// Subscribe
_eventAggregator.Subscribe<DocumentSentEvent>(OnDocumentSent);
```

👉 **결합도 최소화**

---

## 1️⃣5️⃣ 구현 체크리스트

### 모든 화면 공통
- [ ] 상태 색상 통일
- [ ] 버튼 3종류만 사용
- [ ] Read-only 필드 명확히 구분
- [ ] Disabled 버튼에 Tooltip
- [ ] Enterprise B2B 스타일 유지

### 리스트 화면
- [ ] 왼쪽 상태 바 구현
- [ ] 상태 배지 표시
- [ ] 선택 행 강조 (파란 배경)
- [ ] Hover 효과 (연한 회색)

### ViewModel 분리
- [ ] 메뉴당 1개 ViewModel
- [ ] 직접 참조 금지
- [ ] EventAggregator 사용
- [ ] Read-only 속성 명확히

---

**생성일:** 2026-01-06
**작성자:** Claude (Dev Agent)
**버전:** 1.0
**기준:** PRD 및 사용자 요구사항

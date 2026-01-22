# 📐 규격 입력 UI 구현 계획

## 🎯 설계 전제 (확정)

1. ✅ **규격은 컬럼이 아니다** - 품목의 하위 구조
2. ✅ **규격은 합의 대상이다** - ContentHash에 포함
3. ✅ **규격은 계산 로직에 관여하지 않는다** - TotalAmount 무관
4. ✅ **Canonical JSON** - 해시 안정성 보장

---

## 🏗️ 현재 시스템 분석

### ✅ 이미 준비된 것

**1. DocumentItem.ExtraDataJson**
```csharp
public class DocumentItem
{
    public string ItemName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? OptionText { get; set; }
    public decimal LineAmount { get; set; }

    // ✅ 규격 저장소로 사용 가능
    public string? ExtraDataJson { get; set; }
}
```

**2. ContentHash 계산 로직**
```csharp
// CreateDocumentViewModel.cs:295
private string CalculateContentHash(List<DocumentItem> items)
{
    var itemsData = items.Select(item => new
    {
        item.ItemName,
        item.OptionText,
        item.Quantity,
        item.UnitPrice,
        item.LineAmount
        // ⚠️ ExtraDataJson 누락 - 추가 필요
    }).OrderBy(x => x.ItemName).ThenBy(x => x.OptionText);
}
```

### ❌ 구현 필요한 것

1. DocumentItemViewModel에 Specs 컬렉션
2. CreateDocumentWindow.xaml에 "규격" 컬럼
3. 규격 편집 UI (모달 또는 패널)
4. SpecCanonicalizer 유틸리티
5. ContentHash에 spec 포함

---

## 📋 구현 순서

### Phase 1: 데이터 모델 (1일)

#### 1.1 SpecEntry 모델 생성
```csharp
// Tran.Core/Models/SpecEntry.cs
namespace Tran.Core.Models;

/// <summary>
/// 규격 항목 (Key-Value)
/// </summary>
public class SpecEntry
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
```

#### 1.2 DocumentItemViewModel 확장
```csharp
// Tran.Desktop/ViewModels/DocumentItemViewModel.cs
public class DocumentItemViewModel : ViewModelBase
{
    public string ItemName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    // ✅ 규격 컬렉션 추가
    public ObservableCollection<SpecEntry> Specs { get; } = new();

    public string OptionText { get; set; }

    public decimal LineAmount => Quantity * UnitPrice;

    // ✅ 규격 개수
    public int SpecCount => Specs.Count;

    // ✅ 규격 요약 텍스트
    public string SpecSummary
    {
        get
        {
            if (Specs.Count == 0) return "규격 입력";
            return $"규격 {Specs.Count}";
        }
    }

    // ✅ 규격 툴팁
    public string SpecTooltip
    {
        get
        {
            if (Specs.Count == 0) return "규격을 입력하세요";
            return string.Join("\n", Specs.Select(s => $"{s.Key}: {s.Value}"));
        }
    }
}
```

#### 1.3 SpecCanonicalizer 유틸리티
```csharp
// Tran.Core/Utilities/SpecCanonicalizer.cs
namespace Tran.Core.Utilities;

/// <summary>
/// 규격 JSON Canonicalization
/// 해시 안정성을 위한 정규화
/// </summary>
public static class SpecCanonicalizer
{
    /// <summary>
    /// 규격 Dictionary를 Canonical JSON으로 변환
    /// </summary>
    public static Dictionary<string, string> Canonicalize(
        IEnumerable<SpecEntry> specs)
    {
        var cleaned = new Dictionary<string, string>();

        foreach (var spec in specs)
        {
            var key = spec.Key?.Trim() ?? string.Empty;
            var value = spec.Value?.Trim() ?? string.Empty;

            // ✅ Rule 3: Null/Empty 제거
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            {
                cleaned[key] = value;
            }
        }

        // ✅ Rule 1: Key 정렬 (유니코드 오름차순)
        var canonical = new Dictionary<string, string>();
        foreach (var key in cleaned.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            canonical[key] = cleaned[key];
        }

        return canonical;
    }

    /// <summary>
    /// Canonical spec을 JSON 문자열로 직렬화
    /// </summary>
    public static string ToJson(Dictionary<string, string> canonical)
    {
        return JsonSerializer.Serialize(canonical, new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }
}
```

---

### Phase 2: UI 구현 (2일)

#### 2.1 CreateDocumentWindow.xaml 수정

**Before (5개 컬럼):**
```xml
<DataGrid.Columns>
    <DataGridTextColumn Header="품명" Binding="{Binding ItemName}"/>
    <DataGridTextColumn Header="옵션/비고" Binding="{Binding OptionText}"/>
    <DataGridTextColumn Header="수량" Binding="{Binding Quantity}"/>
    <DataGridTextColumn Header="단가" Binding="{Binding UnitPrice}"/>
    <DataGridTextColumn Header="금액" Binding="{Binding LineAmount}" IsReadOnly="True"/>
</DataGrid.Columns>
```

**After (6개 컬럼):**
```xml
<DataGrid.Columns>
    <!-- 품명 -->
    <DataGridTextColumn Header="품명" Binding="{Binding ItemName}" Width="2*"/>

    <!-- 수량 -->
    <DataGridTextColumn Header="수량" Binding="{Binding Quantity}" Width="*"/>

    <!-- 단가 -->
    <DataGridTextColumn Header="단가" Binding="{Binding UnitPrice}" Width="1.2*"/>

    <!-- ✅ 규격 (버튼) -->
    <DataGridTemplateColumn Header="규격" Width="1.5*">
        <DataGridTemplateColumn.CellTemplate>
            <DataTemplate>
                <Button Content="{Binding SpecSummary}"
                       ToolTip="{Binding SpecTooltip}"
                       Command="{Binding DataContext.EditSpecCommand,
                                RelativeSource={RelativeSource AncestorType=DataGrid}}"
                       CommandParameter="{Binding}"
                       Padding="8,4"
                       FontSize="12"
                       Cursor="Hand">
                    <Button.Style>
                        <Style TargetType="Button">
                            <!-- 규격 없음: 회색 -->
                            <Setter Property="Background" Value="#E0E0E0"/>
                            <Setter Property="Foreground" Value="#666"/>
                            <Style.Triggers>
                                <!-- 규격 있음: 파란색 -->
                                <DataTrigger Binding="{Binding SpecCount,
                                            Converter={StaticResource IsGreaterThanZero}}"
                                            Value="True">
                                    <Setter Property="Background" Value="#E8F1FF"/>
                                    <Setter Property="Foreground" Value="#1E5EFF"/>
                                    <Setter Property="FontWeight" Value="SemiBold"/>
                                </DataTrigger>
                            </Style.Triggers>
                        </Style>
                    </Button.Style>
                </Button>
            </DataTemplate>
        </DataGridTemplateColumn.CellTemplate>
    </DataGridTemplateColumn>

    <!-- 옵션/비고 -->
    <DataGridTextColumn Header="옵션/비고" Binding="{Binding OptionText}" Width="1.5*"/>

    <!-- 금액 (자동 계산) -->
    <DataGridTextColumn Header="금액" Binding="{Binding LineAmount}"
                       IsReadOnly="True" Width="1.5*"/>

    <!-- 삭제 -->
    <DataGridTemplateColumn Header="" Width="80">
        <!-- ... -->
    </DataGridTemplateColumn>
</DataGrid.Columns>
```

#### 2.2 SpecEditorWindow.xaml 생성

```xml
<Window x:Class="Tran.Desktop.SpecEditorWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="규격 정보 입력" Height="500" Width="600"
        WindowStartupLocation="CenterOwner"
        Background="White">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 헤더 -->
        <Border Grid.Row="0" Background="#F5F5F5" Padding="20,15">
            <StackPanel>
                <TextBlock Text="규격 정보 입력" FontSize="18" FontWeight="Bold"/>
                <TextBlock Text="{Binding ItemName, StringFormat='품목: {0}'}"
                          FontSize="13" Foreground="#666" Margin="0,5,0,0"/>
            </StackPanel>
        </Border>

        <!-- 규격 목록 -->
        <DataGrid Grid.Row="1"
                 ItemsSource="{Binding Specs}"
                 AutoGenerateColumns="False"
                 CanUserAddRows="False"
                 GridLinesVisibility="Horizontal"
                 Margin="20">
            <DataGrid.Columns>
                <DataGridTextColumn Header="규격명"
                                   Binding="{Binding Key, UpdateSourceTrigger=PropertyChanged}"
                                   Width="*"/>
                <DataGridTextColumn Header="값"
                                   Binding="{Binding Value, UpdateSourceTrigger=PropertyChanged}"
                                   Width="1.5*"/>
                <DataGridTemplateColumn Header="" Width="80">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <Button Content="삭제"
                                   Command="{Binding DataContext.RemoveSpecCommand,
                                            RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                   CommandParameter="{Binding}"
                                   Background="#E74C3C" Foreground="White"
                                   Padding="10,5" BorderThickness="0"
                                   FontSize="12" Cursor="Hand"/>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>

        <!-- 추가 버튼 -->
        <Border Grid.Row="2" Padding="20,0,20,20">
            <Button Content="+ 규격 추가"
                   Command="{Binding AddSpecCommand}"
                   Background="#3498DB" Foreground="White"
                   Padding="15,8" BorderThickness="0"
                   FontWeight="Bold" Cursor="Hand"
                   HorizontalAlignment="Left"/>
        </Border>

        <!-- 하단 버튼 -->
        <Border Grid.Row="3" Background="#F5F5F5" Padding="20,15">
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                <Button Content="저장"
                       Command="{Binding SaveCommand}"
                       Background="#27AE60" Foreground="White"
                       Padding="20,8" Margin="0,0,10,0"
                       BorderThickness="0" FontWeight="Bold"
                       Cursor="Hand"/>
                <Button Content="취소"
                       Command="{Binding CancelCommand}"
                       CommandParameter="{Binding RelativeSource={RelativeSource AncestorType=Window}}"
                       Background="#95A5A6" Foreground="White"
                       Padding="20,8" BorderThickness="0"
                       Cursor="Hand"/>
            </StackPanel>
        </Border>
    </Grid>
</Window>
```

#### 2.3 SpecEditorViewModel.cs 생성

```csharp
// Tran.Desktop/ViewModels/SpecEditorViewModel.cs
public class SpecEditorViewModel : ViewModelBase
{
    private readonly DocumentItemViewModel _item;

    public SpecEditorViewModel(DocumentItemViewModel item)
    {
        _item = item;
        ItemName = item.ItemName;

        // 기존 규격 복사
        foreach (var spec in item.Specs)
        {
            Specs.Add(new SpecEntry { Key = spec.Key, Value = spec.Value });
        }

        AddSpecCommand = new RelayCommand(OnAddSpec);
        RemoveSpecCommand = new RelayCommand<SpecEntry>(OnRemoveSpec);
        SaveCommand = new RelayCommand(OnSave);
        CancelCommand = new RelayCommand<Window>(OnCancel);
    }

    public string ItemName { get; }
    public ObservableCollection<SpecEntry> Specs { get; } = new();

    public ICommand AddSpecCommand { get; }
    public ICommand RemoveSpecCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private void OnAddSpec()
    {
        Specs.Add(new SpecEntry { Key = "", Value = "" });
    }

    private void OnRemoveSpec(SpecEntry? spec)
    {
        if (spec != null)
        {
            Specs.Remove(spec);
        }
    }

    private void OnSave()
    {
        // ✅ Canonical 규격으로 정규화
        var canonical = SpecCanonicalizer.Canonicalize(Specs);

        // 원본 아이템에 반영
        _item.Specs.Clear();
        foreach (var kvp in canonical)
        {
            _item.Specs.Add(new SpecEntry { Key = kvp.Key, Value = kvp.Value });
        }

        // 창 닫기
        Application.Current.Windows
            .OfType<SpecEditorWindow>()
            .FirstOrDefault()?.Close();
    }

    private void OnCancel(Window? window)
    {
        window?.Close();
    }
}
```

---

### Phase 3: 저장 로직 통합 (1일)

#### 3.1 CreateDocumentViewModel에 EditSpecCommand 추가

```csharp
// CreateDocumentViewModel.cs
public class CreateDocumentViewModel : ViewModelBase
{
    // 기존 코드...

    public ICommand EditSpecCommand { get; }

    public CreateDocumentViewModel()
    {
        // 기존 Commands...
        EditSpecCommand = new RelayCommand<DocumentItemViewModel>(OnEditSpec);
    }

    private void OnEditSpec(DocumentItemViewModel? item)
    {
        if (item == null) return;

        var editorViewModel = new SpecEditorViewModel(item);
        var editorWindow = new SpecEditorWindow(editorViewModel);
        editorWindow.Owner = Application.Current.MainWindow;
        editorWindow.ShowDialog();

        // 규격 변경 후 UI 갱신
        RaisePropertyChanged(nameof(Items));
    }
}
```

#### 3.2 저장 시 ExtraDataJson에 spec 포함

```csharp
// CreateDocumentViewModel.cs:SaveDocumentAsync()
private async Task SaveDocumentAsync(DocumentState initialState, bool sendAfterSave)
{
    // ... 기존 코드 ...

    // DocumentItem 목록 생성
    var documentItems = Items.Select((item, index) =>
    {
        // ✅ Canonical spec을 ExtraDataJson에 저장
        string? extraJson = null;
        if (item.Specs.Count > 0)
        {
            var canonical = SpecCanonicalizer.Canonicalize(item.Specs);
            var specJson = SpecCanonicalizer.ToJson(canonical);

            extraJson = JsonSerializer.Serialize(new
            {
                spec = canonical
            });
        }

        return new DocumentItem
        {
            ItemId = $"{documentId}-ITEM-{(index + 1):D3}",
            DocumentId = documentId,
            ItemName = item.ItemName,
            OptionText = item.OptionText,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            LineAmount = item.LineAmount,
            ExtraDataJson = extraJson  // ✅ spec 저장
        };
    }).ToList();

    // ... 기존 코드 ...
}
```

#### 3.3 ContentHash에 spec 포함

```csharp
// CreateDocumentViewModel.cs:CalculateContentHash()
private string CalculateContentHash(List<DocumentItem> items)
{
    // 품목 데이터를 JSON으로 직렬화
    var itemsData = items.Select(item =>
    {
        // ✅ spec 파싱
        Dictionary<string, string>? spec = null;
        if (!string.IsNullOrEmpty(item.ExtraDataJson))
        {
            var extra = JsonSerializer.Deserialize<Dictionary<string, object>>(item.ExtraDataJson);
            if (extra != null && extra.ContainsKey("spec"))
            {
                spec = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    extra["spec"].ToString() ?? "{}");
            }
        }

        return new
        {
            item.ItemName,
            item.OptionText,
            item.Quantity,
            item.UnitPrice,
            item.LineAmount,
            spec  // ✅ spec 포함
        };
    }).OrderBy(x => x.ItemName).ThenBy(x => x.OptionText);

    var json = JsonSerializer.Serialize(itemsData);
    var bytes = Encoding.UTF8.GetBytes(json);

    // SHA-256 해시 계산
    using var sha256 = SHA256.Create();
    var hashBytes = sha256.ComputeHash(bytes);
    return Convert.ToBase64String(hashBytes);
}
```

---

### Phase 4: 상태별 권한 제어 (0.5일)

#### 4.1 EditSpecCommand에 CanExecute 추가

```csharp
// CreateDocumentViewModel.cs
EditSpecCommand = new RelayCommand<DocumentItemViewModel>(
    OnEditSpec,
    item => true  // CreateDocumentWindow는 항상 Draft 상태이므로 항상 허용
);
```

#### 4.2 DocumentDetailWindow에서는 읽기 전용

```csharp
// DocumentDetailViewModel.cs
// Confirmed/Sent 문서는 규격 버튼 비활성화 또는 읽기 전용 표시
public bool CanEditSpec => Document.State == DocumentState.Draft;
```

---

## 📊 작업 예상 시간

| Phase | 작업 내용 | 예상 시간 |
|-------|----------|----------|
| Phase 1 | 데이터 모델 (SpecEntry, ViewModel, Canonicalizer) | 1일 |
| Phase 2 | UI 구현 (DataGrid 컬럼, SpecEditorWindow) | 2일 |
| Phase 3 | 저장 로직 통합 (ExtraDataJson, ContentHash) | 1일 |
| Phase 4 | 권한 제어 | 0.5일 |
| **총계** | **4.5일** | **(1인 풀타임 기준)** |

---

## ✅ 구현 후 기대 효과

### 1. 사용자 편의성
- ✅ 규격을 구조화된 형태로 입력
- ✅ 컬럼 추가 없이 유연한 확장
- ✅ 버튼 클릭만으로 직관적 입력

### 2. 데이터 무결성
- ✅ Canonical JSON으로 해시 안정성
- ✅ 분쟁 시 규격 비교 가능
- ✅ 자동 정규화 (공백 제거, 정렬)

### 3. 아키텍처 일관성
- ✅ DocumentItem.ExtraDataJson 활용
- ✅ 기존 DB 스키마 변경 없음
- ✅ ContentHash 계산에 포함

---

## 🚨 주의사항

### 1. Canonical 규칙 준수
```csharp
// ❌ 잘못된 예 (해시 불일치)
{ "재질": "SS400", "두께": "1.2T" }
{ "두께": "1.2T", "재질": "SS400" }

// ✅ 올바른 예 (항상 동일)
{ "두께": "1.2T", "재질": "SS400" }  // 유니코드 정렬
```

### 2. UI 렌더링 성능
- DataGrid에 1000개 품목 × 평균 3개 규격 = 3000개 spec 렌더링
- 필요 시 가상화(Virtualization) 적용

### 3. 마이그레이션
- 기존 문서는 ExtraDataJson이 null
- null 처리 로직 필수

---

## 🎯 구현 시작 여부 확인

**구현을 시작하시겠습니까?**

1. ✅ **지금 바로 구현** (4.5일 작업)
   - Phase 1부터 순차적으로 진행
   - 각 Phase 완료 후 빌드 및 테스트

2. ⏸️ **나중에 구현** (우선순위 낮음)
   - 엑셀 가져오기 기능 먼저 구현
   - 규격은 v2.0에서

3. ❌ **구현 안 함**
   - OptionText에 자유 텍스트로 입력
   - 규격 기능 제거

어떻게 하시겠습니까?

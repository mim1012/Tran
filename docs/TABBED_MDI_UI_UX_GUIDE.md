# Tran 탭 기반 MDI 워크스페이스 UI/UX 구현 가이드

> **버전**: 1.0
> **작성일**: 2026-01-23
> **대상**: 개발자, UI/UX 디자이너
> **관련 문서**: `docs/prd.md` (Section 0), `docs/features/00-common-ux.md`

---

## 목차

1. [UI 아키텍처 개요](#1-ui-아키텍처-개요)
2. [Chrome 스타일 탭 UI](#2-chrome-스타일-탭-ui)
3. [거래처 탭 시스템](#3-거래처-탭-시스템)
4. [3분할 레이아웃](#4-3분할-레이아웃)
5. [색상 & 스타일 가이드](#5-색상--스타일-가이드)
6. [사용자 인터랙션 시나리오](#6-사용자-인터랙션-시나리오)
7. [XAML 구현 예제](#7-xaml-구현-예제)

---

## 1. UI 아키텍처 개요

### 1.1 전체 구조

```
┌──────────────────────────────────────────────────────────────────┐
│ 상단바: [로고] [사용자: 관리자]                                   │
├──────────────────────────────────────────────────────────────────┤
│ 탭바: [발주관리 ✕] [재고관리 ✕] [정산관리 ✕]  [+]               │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  현재 선택된 워크스페이스 콘텐츠                                  │
│                                                                  │
│  - 거래처 기반: 내부 거래처 탭 + 3분할 레이아웃                   │
│  - 목록 기반: 단일 DataGrid 화면                                  │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### 1.2 워크스페이스 유형 정의

| 워크스페이스 | 내부 구조 | 거래처 탭 | 3분할 레이아웃 | 비고 |
|-------------|----------|----------|---------------|------|
| **발주관리** | 거래처 기반 | ✅ | ✅ | 병원별 발주 작성 |
| **구매관리** | 거래처 기반 | ✅ | ✅ | 공급사별 구매 작성 |
| **판매관리** | 거래처 기반 | ✅ | ✅ | 병원별 판매 작성 |
| **견적관리** | 거래처 기반 | ✅ | ✅ | 거래처별 견적 작성 |
| **재고관리** | 목록 기반 | ❌ | ❌ | 전체 재고 조회 |
| **정산관리** | 목록 기반 | ❌ | ❌ | 전체 미수금 조회 |

---

## 2. Chrome 스타일 탭 UI

### 2.1 탭 바 레이아웃

```
┌──────────────────────────────────────────────────────────────────┐
│ [발주관리 ✕] [재고관리 ✕] [정산관리 ✕]  [+]                      │
│  ─────────                                                       │
│   (활성)                                                          │
└──────────────────────────────────────────────────────────────────┘
```

**구성 요소**:
- **탭 아이템**: `[워크스페이스명 ✕]`
- **닫기 버튼**: `✕` (hover 시 배경색 변경)
- **추가 버튼**: `[+]` (우측 끝)
- **활성 표시**: 하단 3px 파란색 밑줄

### 2.2 탭 상태별 스타일

| 상태 | 배경색 | 텍스트색 | 하단 밑줄 | 호버 효과 |
|------|--------|---------|----------|----------|
| **활성** | `#FFFFFF` | `#1E5EFF` | `#1E5EFF` 3px | - |
| **비활성** | `#F8F9FA` | `#495057` | 없음 | 배경 `#E9ECEF` |
| **호버** | `#E9ECEF` | `#212529` | 없음 | - |

### 2.3 탭 닫기 버튼 (✕)

```xml
<!-- CloseButton Style -->
<Button Content="✕"
        Width="20" Height="20"
        Margin="8,0,0,0"
        Background="Transparent"
        BorderThickness="0"
        Foreground="#6C757D"
        FontSize="14"
        Cursor="Hand">
    <Button.Style>
        <Style TargetType="Button">
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border Background="{TemplateBinding Background}"
                                CornerRadius="3">
                            <ContentPresenter HorizontalAlignment="Center"
                                            VerticalAlignment="Center"/>
                        </Border>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
            <Style.Triggers>
                <Trigger Property="IsMouseOver" Value="True">
                    <Setter Property="Background" Value="#E67700"/>
                    <Setter Property="Foreground" Value="White"/>
                </Trigger>
            </Style.Triggers>
        </Style>
    </Button.Style>
</Button>
```

### 2.4 탭 추가 버튼 ([+])

```xml
<Button Content="+"
        Width="32" Height="32"
        Margin="8,0,0,0"
        Background="#F8F9FA"
        BorderThickness="1"
        BorderBrush="#DEE2E6"
        Foreground="#495057"
        FontSize="18"
        FontWeight="Bold"
        Cursor="Hand"
        ToolTip="새 작업 추가">
    <!-- Hover: Background=#E9ECEF -->
</Button>
```

**클릭 시 동작**:
1. 모달 표시: "작업 선택"
2. 선택지: 발주관리, 구매관리, 판매관리, 재고관리, 정산관리, 견적관리
3. 선택 완료 → 새 탭 생성 및 활성화

---

## 3. 거래처 탭 시스템

### 3.1 거래처 탭 바 (내부 탭)

**적용 대상**: 발주관리, 구매관리, 판매관리, 견적관리

```
┌──────────────────────────────────────────────────────────────────┐
│ [발주관리 ✕]  ← 외부 탭                                          │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  [+ 거래처 선택] [A병원 ✕] [B의원 ✕] [C도매 ✕]  ← 내부 탭       │
│  ──────────────────────────────────────────────────────────────  │
│                                                                  │
│  (선택된 거래처의 콘텐츠)                                         │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### 3.2 [+ 거래처 선택] 탭 UI

**초기 상태**: 이 탭이 기본으로 선택되어 있음

**콘텐츠**: 2컬럼 거래처 리스트

```
┌──────────────────────────────────────────────────────────────────┐
│  🔍 [거래처명 검색...]               [유형 ▼] [정렬: 최근거래 ▼] │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌────────────────────────┬────────────────────────┐            │
│  │                        │                        │            │
│  │  🏥 A병원              │  🏥 B의원              │            │
│  │  서울시 강남구         │  서울시 서초구         │            │
│  │  최근거래: 2025-01-22  │  최근거래: 2025-01-20  │            │
│  │  진행중: 3건           │  진행중: 1건           │            │
│  │                        │                        │            │
│  ├────────────────────────┼────────────────────────┤            │
│  │                        │                        │            │
│  │  🏢 C도매              │  🏢 D본사              │            │
│  │  경기도 성남시         │  부산시 해운대구       │            │
│  │  최근거래: 2025-01-18  │  최근거래: 2025-01-15  │            │
│  │  진행중: 0건           │  진행중: 2건           │            │
│  │                        │                        │            │
│  └────────────────────────┴────────────────────────┘            │
│                                                                  │
│  ◀  1  2  3  ...  10  ▶                       총 156개 거래처   │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### 3.3 거래처 카드 컴포넌트

**카드 크기**: 고정폭 (부모의 50%), 높이 자동

**카드 스타일**:
- Border: `#E0E0E0` 1px
- Padding: 16px
- Margin: 8px
- Border Radius: 8px
- Cursor: Hand

**Hover 효과**:
- Border: `#3498DB` 2px
- Box Shadow: `0 4px 8px rgba(0,0,0,0.1)`

**클릭 동작**:
1. 새 거래처 탭 생성: `[거래처명 ✕]`
2. 3분할 레이아웃 표시
3. 최근 거래 품목 자동 로드

---

## 4. 3분할 레이아웃

### 4.1 전체 구조

```
┌───────────────────────────────┬─────────────────────────────────┐
│ 좌상: 품목 리스트             │ 우상: 최근거래 품목 (빠른 입력) │
│ (40% 너비)                    │ (60% 너비)                      │
│                               │                                 │
│ [품목 검색...]                │ 자주 거래하는 품목 (최근 30일)  │
│                               │                                 │
│ ☐ 테이프 10EA    ₩3,500      │ ☑ 테이프 10EA   [___100___]     │
│ ☐ 거즈 1BOX      ₩12,000     │ ☑ 거즈 1BOX     [____50___]     │
│ ☐ 주사기 100EA   ₩8,000      │ ☐ 주사기 100EA  [________]      │
│ ...                           │                                 │
│                               │ ───────────────────────────     │
│                               │ 합계: ₩950,000                  │
│                               │ [임시저장]  [발주서 보내기]     │
│                               │                                 │
├───────────────────────────────┴─────────────────────────────────┤
│ 하단: 최근 거래 내역 (100% 너비, 40% 높이)                      │
│ ─────────────────────────────────────────────────────────────   │
│                                                                 │
│ ┌─────────────────────────────────────────────────────────┐     │
│ │ [등록] [최근 작업 (임시저장)] [최근 성사 내역]           │     │
│ └─────────────────────────────────────────────────────────┘     │
│                                                                 │
│ (선택된 탭의 콘텐츠)                                             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 4.2 좌상: 품목 리스트

**기능**: 전체 품목 조회 및 검색

**구성**:
- 검색바: 품목명 실시간 필터링
- 체크박스 리스트: 다중 선택 가능
- 품목 정보: 품목명, 규격, 단가

**XAML 구조**:
```xml
<Grid Grid.Row="0" Grid.Column="0">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
    </Grid.RowDefinitions>

    <!-- 검색바 -->
    <TextBox Grid.Row="0"
             Text="{Binding ProductSearchText, UpdateSourceTrigger=PropertyChanged}"
             Margin="0,0,0,10"
             Padding="10,8"
             FontSize="14">
        <TextBox.Style>
            <Style TargetType="TextBox">
                <Setter Property="Foreground" Value="#999"/>
                <Style.Triggers>
                    <Trigger Property="IsFocused" Value="True">
                        <Setter Property="Foreground" Value="#333"/>
                    </Trigger>
                </Style.Triggers>
            </Style>
        </TextBox.Style>
    </TextBox>

    <!-- 품목 리스트 -->
    <ListView Grid.Row="1"
              ItemsSource="{Binding FilteredProducts}"
              BorderThickness="1"
              BorderBrush="#E0E0E0">
        <ListView.ItemTemplate>
            <DataTemplate>
                <StackPanel Orientation="Horizontal" Margin="0,5">
                    <CheckBox IsChecked="{Binding IsSelected}"
                              Margin="0,0,10,0"
                              VerticalAlignment="Center"/>
                    <StackPanel>
                        <TextBlock Text="{Binding Name}" FontSize="14" FontWeight="SemiBold"/>
                        <TextBlock Text="{Binding Spec}" FontSize="12" Foreground="#666"/>
                    </StackPanel>
                    <TextBlock Text="{Binding Price, StringFormat='₩{0:N0}'}"
                               Margin="auto,0,0,0"
                               FontSize="14"
                               Foreground="#1E5EFF"/>
                </StackPanel>
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>
</Grid>
```

### 4.3 우상: 최근거래 품목 (빠른 입력)

**기능**: 자주 거래하는 품목 빠르게 수량 입력

**구성**:
- 제목: "자주 거래하는 품목 (최근 30일)"
- 체크박스 + 수량 입력
- 합계 표시
- 액션 버튼: [임시저장] [발주서 보내기]

**데이터 로딩**:
```sql
SELECT p.ProductId, p.Name, p.Spec, AVG(oi.Quantity) AS AvgQty
FROM OrderItems oi
JOIN Products p ON oi.ProductId = p.ProductId
WHERE oi.CompanyId = @CompanyId
  AND oi.OrderDate >= DATEADD(DAY, -30, GETDATE())
GROUP BY p.ProductId, p.Name, p.Spec
ORDER BY COUNT(*) DESC, AvgQty DESC
LIMIT 10
```

**XAML 구조**:
```xml
<Grid Grid.Row="0" Grid.Column="1">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>

    <!-- 제목 -->
    <TextBlock Grid.Row="0"
               Text="자주 거래하는 품목 (최근 30일)"
               FontSize="14"
               FontWeight="Bold"
               Foreground="#666"
               Margin="0,0,0,10"/>

    <!-- 품목 리스트 -->
    <ListView Grid.Row="1"
              ItemsSource="{Binding RecentProducts}"
              BorderThickness="1"
              BorderBrush="#E0E0E0">
        <ListView.ItemTemplate>
            <DataTemplate>
                <Grid Margin="0,5">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="120"/>
                    </Grid.ColumnDefinitions>

                    <CheckBox Grid.Column="0"
                              IsChecked="{Binding IsSelected}"
                              Margin="0,0,10,0"
                              VerticalAlignment="Center"/>

                    <StackPanel Grid.Column="1">
                        <TextBlock Text="{Binding Name}" FontSize="14"/>
                        <TextBlock Text="{Binding Spec}" FontSize="11" Foreground="#999"/>
                    </StackPanel>

                    <TextBox Grid.Column="2"
                             Text="{Binding Quantity, UpdateSourceTrigger=PropertyChanged}"
                             Padding="8,5"
                             TextAlignment="Right"
                             FontSize="14">
                        <TextBox.Style>
                            <Style TargetType="TextBox">
                                <Setter Property="IsEnabled" Value="{Binding IsSelected}"/>
                            </Style>
                        </TextBox.Style>
                    </TextBox>
                </Grid>
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>

    <!-- 합계 -->
    <Border Grid.Row="2"
            Background="#F8F9FA"
            Padding="15"
            BorderBrush="#DEE2E6"
            BorderThickness="0,1,0,0"
            Margin="0,10,0,0">
        <TextBlock Text="{Binding TotalAmount, StringFormat='합계: ₩{0:N0}'}"
                   FontSize="18"
                   FontWeight="Bold"
                   HorizontalAlignment="Right"/>
    </Border>

    <!-- 액션 버튼 -->
    <StackPanel Grid.Row="3"
                Orientation="Horizontal"
                HorizontalAlignment="Right"
                Margin="0,15,0,0">
        <Button Content="임시저장"
                Command="{Binding SaveDraftCommand}"
                Style="{StaticResource SecondaryButton}"
                Margin="0,0,10,0"/>
        <Button Content="발주서 보내기"
                Command="{Binding SendOrderCommand}"
                Style="{StaticResource PrimaryButton}"/>
    </StackPanel>
</Grid>
```

### 4.4 하단: 최근 거래 내역 (3탭)

**3개 탭**:

#### 탭 1: [등록]
- **용도**: 새 문서 작성 폼
- **콘텐츠**: 빈 입력 폼 또는 우상단에서 선택한 품목 자동 반영
- **액션**: [전송] [취소]

#### 탭 2: [최근 작업 (임시저장)]
- **용도**: 임시저장된 작업 목록
- **콘텐츠**:
  ```
  ┌─────────────────────────────────────────────────────────┐
  │ 임시저장 #1234                      2025-01-23 14:30    │
  │ 품목 5개 | 합계: ₩1,500,000                [불러오기]   │
  ├─────────────────────────────────────────────────────────┤
  │ 임시저장 #1235                      2025-01-22 10:15    │
  │ 품목 3개 | 합계: ₩850,000                  [불러오기]   │
  └─────────────────────────────────────────────────────────┘
  ```
- **액션**: [불러오기] → [등록] 탭으로 데이터 로드

#### 탭 3: [최근 성사 내역]
- **용도**: 완료된 발주/판매 문서 목록
- **콘텐츠**: DataGrid (발주번호, 날짜, 품목 수, 금액, 상태)
- **액션**: [복사하기] → [등록] 탭으로 복사

**탭 스타일**:
```xml
<TabControl Grid.Row="2" Grid.ColumnSpan="2">
    <TabControl.Resources>
        <Style TargetType="TabItem">
            <Setter Property="Padding" Value="20,10"/>
            <Setter Property="FontSize" Value="14"/>
            <Setter Property="BorderThickness" Value="0,0,0,3"/>
            <Setter Property="BorderBrush" Value="Transparent"/>
            <Style.Triggers>
                <Trigger Property="IsSelected" Value="True">
                    <Setter Property="BorderBrush" Value="#1E5EFF"/>
                    <Setter Property="Foreground" Value="#1E5EFF"/>
                </Trigger>
            </Style.Triggers>
        </Style>
    </TabControl.Resources>

    <TabItem Header="등록">
        <!-- 등록 폼 -->
    </TabItem>

    <TabItem Header="최근 작업 (임시저장)">
        <!-- 임시저장 목록 -->
    </TabItem>

    <TabItem Header="최근 성사 내역">
        <!-- 완료 문서 목록 -->
    </TabItem>
</TabControl>
```

---

## 5. 색상 & 스타일 가이드

### 5.1 Primary 색상

| 용도 | 색상명 | Hex Code | 사용처 |
|------|--------|----------|--------|
| **Primary** | 파란색 | `#1E5EFF` | 활성 탭, 링크, 주요 버튼 |
| **Success** | 초록색 | `#1E7F34` | 확정, 완료, 성공 메시지 |
| **Warning** | 주황색 | `#E67700` | 수정요청, 경고, 닫기 버튼 호버 |
| **Danger** | 빨간색 | `#E74C3C` | 삭제, 취소, 에러 |
| **Info** | 청록색 | `#3498DB` | 정보, 보조 버튼 |

### 5.2 Neutral 색상

| 용도 | 색상명 | Hex Code | 사용처 |
|------|--------|----------|--------|
| **배경 (밝음)** | 흰색 | `#FFFFFF` | 카드, 입력 필드 |
| **배경 (중간)** | 연회색 | `#F8F9FA` | 비활성 탭, 패널 배경 |
| **배경 (어두움)** | 회색 | `#E9ECEF` | 호버 배경 |
| **Border** | 경계선 | `#DEE2E6` | 테두리, 구분선 |
| **텍스트 (주)** | 검정 | `#212529` | 제목, 주요 텍스트 |
| **텍스트 (보조)** | 회색 | `#6C757D` | 설명, 부가 정보 |

### 5.3 문서 상태별 색상 (PRD Section 0.5)

| 상태 | 배경색 | 텍스트색 | 용도 |
|------|--------|----------|------|
| **Draft** (임시) | `#F0F0F0` | `#555555` | 작성 중 |
| **Sent** (발송) | `#E8F1FF` | `#1E5EFF` | 전송됨 |
| **Confirmed** (확정) | `#E6F4EA` | `#1E7F34` | 완료 |
| **RevisionRequested** (수정요청) | `#FFF4E5` | `#E67700` | 수정 필요 |

### 5.4 타이포그래피

| 요소 | 폰트 크기 | 굵기 | 용도 |
|------|----------|------|------|
| **Heading 1** | 20px | Bold | 페이지 제목 |
| **Heading 2** | 18px | SemiBold | 섹션 제목 |
| **Heading 3** | 16px | SemiBold | 서브섹션 제목 |
| **Body** | 14px | Regular | 본문, 입력 필드 |
| **Caption** | 12px | Regular | 부가 정보, 설명 |
| **Small** | 11px | Regular | 메타 정보 |

---

## 6. 사용자 인터랙션 시나리오

### 시나리오 1: 발주서 작성 (첫 방문)

```
1. 사용자가 [+] 클릭
   ↓
2. 모달 표시: "발주관리" 선택
   ↓
3. [발주관리 ✕] 탭 생성됨
   ↓
4. [+ 거래처 선택] 탭이 기본 활성화
   ↓
5. 2컬럼 거래처 리스트 표시
   ↓
6. 사용자가 "A병원" 카드 클릭
   ↓
7. [A병원 ✕] 탭 생성, 3분할 레이아웃 표시
   ↓
8. 우상단 "최근거래 품목" 자동 로드 (최근 30일)
   ↓
9. 사용자가 체크박스 선택 + 수량 입력
   ↓
10. [임시저장] 클릭
    ↓
11. 하단 [최근 작업 (임시저장)] 탭에 저장됨
    ↓
12. 알림: "임시저장되었습니다"
```

### 시나리오 2: 임시저장 불러오기

```
1. 사용자가 [발주관리] 탭 선택
   ↓
2. [A병원] 탭 선택
   ↓
3. 하단 [최근 작업 (임시저장)] 탭 클릭
   ↓
4. 임시저장 목록 표시
   ↓
5. "임시저장 #1234" 항목의 [불러오기] 클릭
   ↓
6. [등록] 탭으로 자동 전환
   ↓
7. 임시저장 데이터 로드됨 (품목, 수량)
   ↓
8. 사용자가 수정 후 [발주서 보내기] 클릭
   ↓
9. 확인 모달: "A병원에 발주서를 전송하시겠습니까?"
   ↓
10. [전송] 클릭 → 발주 상태: Draft → Sent
    ↓
11. [최근 성사 내역] 탭에 추가됨
```

### 시나리오 3: 다중 거래처 동시 작업

```
1. [발주관리] 탭에서 [A병원] 탭 작업 중
   ↓
2. [+ 거래처 선택] 클릭
   ↓
3. "B의원" 카드 클릭
   ↓
4. [B의원 ✕] 탭 생성, [A병원] 탭은 백그라운드로
   ↓
5. [B의원] 탭에서 발주 입력
   ↓
6. [A병원] 탭 클릭 → 이전 작업 상태 그대로 유지
   ↓
7. [발주관리 ✕] 탭 닫기 시도
   ↓
8. 확인 모달: "임시저장하지 않은 작업이 2개 있습니다"
   ↓
9. [모두 저장 후 닫기] / [저장 안 함] / [취소]
```

### 시나리오 4: 워크스페이스 전환

```
1. [발주관리] 탭에서 작업 중
   ↓
2. [+] 클릭 → "재고관리" 선택
   ↓
3. [재고관리 ✕] 탭 생성 (단일 DataGrid 화면)
   ↓
4. 재고 조회 후 [발주관리] 탭 클릭
   ↓
5. 이전 작업 상태 복원 ([A병원] 탭 활성 상태 유지)
```

---

## 7. XAML 구현 예제

### 7.1 WorkspaceShell.xaml (메인 컨테이너)

```xml
<Window x:Class="Tran.Desktop.WorkspaceShell"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:dragablz="clr-namespace:Dragablz;assembly=Dragablz"
        Title="Tran - 의료기기 유통 관리 시스템"
        Height="900" Width="1400"
        WindowStartupLocation="CenterScreen">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- 상단바 -->
        <Border Grid.Row="0" Background="#2C3E50" Padding="15,10">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <TextBlock Grid.Column="0"
                           Text="Tran 의료기기 유통 시스템"
                           Foreground="White"
                           FontSize="16"
                           FontWeight="Bold"
                           VerticalAlignment="Center"/>

                <StackPanel Grid.Column="2" Orientation="Horizontal">
                    <TextBlock Text="사용자: 관리자"
                               Foreground="#E8E8E8"
                               Margin="0,0,20,0"
                               VerticalAlignment="Center"
                               FontSize="13"/>
                </StackPanel>
            </Grid>
        </Border>

        <!-- 탭 컨테이너 (Dragablz) -->
        <dragablz:TabablzControl Grid.Row="2"
                                  ItemsSource="{Binding Workspaces}"
                                  SelectedItem="{Binding ActiveWorkspace}"
                                  NewItemFactory="{Binding NewWorkspaceFactory}"
                                  ShowDefaultAddButton="True"
                                  ShowDefaultCloseButton="True">

            <!-- 탭 헤더 템플릿 -->
            <dragablz:TabablzControl.HeaderItemTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal" Margin="10,8">
                        <TextBlock Text="{Binding Title}"
                                   FontSize="14"
                                   Margin="0,0,8,0"/>
                    </StackPanel>
                </DataTemplate>
            </dragablz:TabablzControl.HeaderItemTemplate>

            <!-- 탭 콘텐츠 템플릿 -->
            <dragablz:TabablzControl.ContentTemplate>
                <DataTemplate>
                    <ContentControl Content="{Binding}" />
                </DataTemplate>
            </dragablz:TabablzControl.ContentTemplate>

        </dragablz:TabablzControl>

    </Grid>

</Window>
```

### 7.2 OrderWorkspace.xaml (발주관리 워크스페이스)

```xml
<UserControl x:Class="Tran.Desktop.Workspaces.OrderWorkspace"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:dragablz="clr-namespace:Dragablz;assembly=Dragablz">

    <Grid>
        <!-- 내부 거래처 탭 -->
        <dragablz:TabablzControl ItemsSource="{Binding PartnerTabs}"
                                  SelectedItem="{Binding ActivePartnerTab}"
                                  ShowDefaultAddButton="True"
                                  ShowDefaultCloseButton="True"
                                  AddButtonContent="+ 거래처 선택">

            <dragablz:TabablzControl.HeaderItemTemplate>
                <DataTemplate>
                    <TextBlock Text="{Binding PartnerName}"
                               FontSize="13"
                               Margin="8,5"/>
                </DataTemplate>
            </dragablz:TabablzControl.HeaderItemTemplate>

            <dragablz:TabablzControl.ContentTemplate>
                <DataTemplate>
                    <!-- 3분할 레이아웃 또는 거래처 선택 리스트 -->
                    <ContentControl Content="{Binding}" />
                </DataTemplate>
            </dragablz:TabablzControl.ContentTemplate>

        </dragablz:TabablzControl>

    </Grid>

</UserControl>
```

### 7.3 PartnerDetailTab.xaml (3분할 레이아웃)

```xml
<UserControl x:Class="Tran.Desktop.PartnerTabs.PartnerDetailTab"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="60*"/>
            <RowDefinition Height="40*"/>
        </Grid.RowDefinitions>

        <!-- 상단 영역: 품목 리스트 + 최근거래 품목 -->
        <Grid Grid.Row="0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="40*"/>
                <ColumnDefinition Width="60*"/>
            </Grid.ColumnDefinitions>

            <!-- 좌상: 품목 리스트 -->
            <Border Grid.Column="0"
                    BorderBrush="#E0E0E0"
                    BorderThickness="1"
                    Margin="0,0,10,0"
                    Padding="15">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>

                    <TextBlock Grid.Row="0"
                               Text="📦 품목 리스트"
                               FontSize="16"
                               FontWeight="Bold"
                               Margin="0,0,0,10"/>

                    <TextBox Grid.Row="1"
                             Text="{Binding ProductSearchText, UpdateSourceTrigger=PropertyChanged}"
                             Padding="10,8"
                             FontSize="14"
                             Margin="0,0,0,10">
                        <TextBox.Style>
                            <Style TargetType="TextBox">
                                <Setter Property="Background" Value="#F8F9FA"/>
                                <Setter Property="BorderBrush" Value="#DEE2E6"/>
                            </Style>
                        </TextBox.Style>
                    </TextBox>

                    <ListView Grid.Row="2"
                              ItemsSource="{Binding FilteredProducts}"
                              BorderThickness="0">
                        <!-- ItemTemplate은 위 4.2 참조 -->
                    </ListView>
                </Grid>
            </Border>

            <!-- 우상: 최근거래 품목 -->
            <Border Grid.Column="1"
                    BorderBrush="#E0E0E0"
                    BorderThickness="1"
                    Margin="10,0,0,0"
                    Padding="15">
                <!-- 내용은 위 4.3 참조 -->
            </Border>

        </Grid>

        <!-- 하단: 최근 거래 내역 -->
        <Border Grid.Row="1"
                BorderBrush="#E0E0E0"
                BorderThickness="1"
                Margin="0,20,0,0"
                Padding="15">
            <!-- TabControl은 위 4.4 참조 -->
        </Border>

    </Grid>

</UserControl>
```

---

## 8. 구현 체크리스트

### Phase 1: 기본 인프라 (1주차)

- [ ] Dragablz NuGet 패키지 설치
- [ ] WorkspaceShell.xaml 생성
- [ ] WorkspaceShellViewModel 생성
- [ ] 탭 추가/제거 기능 구현
- [ ] 탭 스타일 적용 (색상, 폰트)

### Phase 2: 워크스페이스 구현 (2-3주차)

- [ ] OrderWorkspace.xaml (발주관리)
- [ ] SalesWorkspace.xaml (판매관리)
- [ ] InventoryWorkspace.xaml (재고관리)
- [ ] SettlementWorkspace.xaml (정산관리)
- [ ] 각 Workspace ViewModel 생성

### Phase 3: 거래처 탭 & 3분할 (4주차)

- [ ] PartnerSelectorTab.xaml (2컬럼 리스트)
- [ ] PartnerDetailTab.xaml (3분할 레이아웃)
- [ ] ProductListPanel (품목 리스트)
- [ ] RecentProductsPanel (최근거래 품목)
- [ ] TransactionHistoryPanel (거래 내역 3탭)

### Phase 4: 데이터 바인딩 & 로직 (5주차)

- [ ] 임시저장 DB 스키마 설계
- [ ] WorkspaceState CRUD 서비스
- [ ] 탭 복원 로직 구현
- [ ] 최근거래 품목 조회 쿼리
- [ ] 데이터 검증 & 에러 핸들링

---

## 9. 참조 링크

- **PRD**: `docs/prd.md` (Section 0)
- **Common UX**: `docs/features/00-common-ux.md`
- **ADR**: `_bmad-output/planning-artifacts/ADR-001-탭기반-MDI-아키텍처.md`
- **Dragablz GitHub**: https://github.com/ButchersBoy/Dragablz
- **WPF MVVM 패턴**: https://learn.microsoft.com/ko-kr/dotnet/desktop/wpf/

---

**문서 버전**: 1.0
**최종 수정**: 2026-01-23
**작성자**: John (PM), Claude Code

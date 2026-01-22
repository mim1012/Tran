# 엑셀 템플릿 업로드 기능 영향도 분석

## 📋 요구사항
사용자가 엑셀 파일(.xlsx)로 거래명세표 템플릿을 업로드하여 DocumentTemplate으로 변환

---

## 🏗️ 현재 시스템 아키텍처 분석

### 1. DocumentTemplate 구조
```csharp
public class DocumentTemplate
{
    public string TemplateId { get; set; }
    public string CompanyId { get; set; }
    public string TemplateName { get; set; }
    public TemplateType TemplateType { get; set; }

    // ⚠️ 핵심: JSON 기반 스키마 정의
    public string SchemaJson { get; set; }    // 필드 정의
    public string LayoutJson { get; set; }    // UI/PDF 레이아웃

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**SchemaJson 예시:**
```json
{
  "fields": [
    {"key": "item_name", "label": "상품명", "type": "text", "required": true},
    {"key": "quantity", "label": "수량", "type": "number", "required": true},
    {"key": "unit_price", "label": "단가", "type": "currency", "required": true},
    {"key": "option_text", "label": "옵션", "type": "text", "required": false}
  ]
}
```

**LayoutJson 예시:**
```json
{
  "columns": [
    {"field": "item_name", "width": "40%"},
    {"field": "quantity", "width": "15%"},
    {"field": "unit_price", "width": "20%"},
    {"field": "option_text", "width": "25%"}
  ]
}
```

### 2. DocumentItem 구조
```csharp
public class DocumentItem
{
    // 고정 필드 (모든 회사 공통)
    public string ItemName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? OptionText { get; set; }
    public decimal LineAmount { get; set; }  // 자동 계산

    // ⚠️ 핵심: 회사별 커스텀 필드 저장소
    public string? ExtraDataJson { get; set; }
    // 예: {"origin": "대한민국", "spec": "128GB", "warranty": "1년"}
}
```

### 3. CreateDocumentWindow 동작 방식

**현재 구현 (고정 UI):**
- XAML에 DataGrid 컬럼이 하드코딩됨
- ItemName, OptionText, Quantity, UnitPrice, LineAmount 컬럼만 표시
- **템플릿과 무관하게 동일한 UI 제공**

```xml
<DataGrid.Columns>
    <DataGridTextColumn Header="품명" Binding="{Binding ItemName}"/>
    <DataGridTextColumn Header="옵션/비고" Binding="{Binding OptionText}"/>
    <DataGridTextColumn Header="수량" Binding="{Binding Quantity}"/>
    <DataGridTextColumn Header="단가" Binding="{Binding UnitPrice}"/>
    <DataGridTextColumn Header="금액" Binding="{Binding LineAmount}" IsReadOnly="True"/>
</DataGrid.Columns>
```

**⚠️ 문제점:**
- DocumentTemplate의 SchemaJson/LayoutJson이 현재 **사용되지 않음**
- 모든 회사가 동일한 입력 화면 사용
- ExtraDataJson 필드에 데이터를 저장할 방법 없음

---

## ⚠️ 엑셀 템플릿 업로드 구현 시 부정적 영향

### 🔴 1. 아키텍처 불일치 (Critical)

**현재 상태:**
- DocumentTemplate은 "렌더링/입력 규칙"만 정의
- 실제 UI는 XAML에 고정된 DataGrid
- **템플릿과 UI가 분리되어 있음**

**엑셀 업로드 시 발생 문제:**
```
엑셀 파일 업로드
  ↓
SchemaJson/LayoutJson 자동 생성
  ↓
⚠️ 하지만 CreateDocumentWindow는 이 JSON을 읽지 않음
  ↓
❌ 템플릿이 있어도 화면에 반영 안됨
```

**해결 필요 작업:**
1. **동적 UI 생성 시스템 구축**
   - XAML 고정 컬럼 제거
   - SchemaJson 기반으로 런타임에 DataGrid 컬럼 생성
   - 예상 작업량: **3-5일**

2. **데이터 바인딩 리팩토링**
   - DocumentItemViewModel에 동적 프로퍼티 추가
   - ExtraDataJson 읽기/쓰기 로직 구현
   - 예상 작업량: **2-3일**

---

### 🔴 2. 데이터 매핑 복잡도 증가 (High)

**엑셀 → DocumentTemplate 변환 과정:**

```
엑셀 컬럼 헤더 → SchemaJson 생성
┌─────────────────────────────────────────────────┐
│ A      │ B    │ C    │ D          │ E          │
│ 상품명 │ 수량 │ 단가 │ 원산지     │ 보증기간   │
└─────────────────────────────────────────────────┘
         ↓
⚠️ 문제 1: 컬럼명 표준화
- "상품명" vs "제품명" vs "품목" → 어떻게 매핑?
- "수량" vs "개수" vs "Qty" → 자동 인식 불가

⚠️ 문제 2: 데이터 타입 추론
- "수량" → number? integer? decimal?
- "단가" → currency? number?
- "원산지" → text? select?

⚠️ 문제 3: 필수 필드 판별
- 어떤 컬럼이 필수인지 엑셀에서 알 수 없음
- 사용자가 수동으로 지정해야 함
```

**필요 작업:**
1. **컬럼 매핑 UI 개발**
   - 엑셀 컬럼 → 표준 필드 매핑 화면
   - 드롭다운: "이 컬럼은 [ItemName/Quantity/...] 입니다"
   - 예상 작업량: **2일**

2. **검증 로직 구현**
   - 필수 컬럼(ItemName, Quantity, UnitPrice) 존재 여부 체크
   - 데이터 타입 일관성 검증
   - 예상 작업량: **1일**

---

### 🟡 3. 템플릿 버전 관리 문제 (Medium)

**시나리오:**
```
2026-01-01: 회사A가 "5컬럼 템플릿" 업로드
  ↓
2026-01-15: 템플릿으로 문서 100건 작성
  ↓
2026-02-01: 회사A가 "7컬럼 템플릿" 재업로드 (컬럼 2개 추가)
  ↓
⚠️ 문제: 기존 100건 문서와 신규 문서의 스키마가 다름
```

**영향:**
- 과거 문서 조회 시 "원산지", "보증기간" 컬럼이 빈칸
- 새 문서 작성 시 추가 컬럼 입력 가능
- **혼란 발생**

**필요 작업:**
1. **템플릿 버전 관리**
   - TemplateVersion 필드 추가
   - Document 테이블에 TemplateVersion FK 추가
   - 예상 작업량: **2일**

2. **하위 호환성 보장**
   - 구 버전 템플릿으로 작성된 문서도 표시 가능하도록
   - 예상 작업량: **1-2일**

---

### 🟡 4. 보안 취약점 (Medium)

**엑셀 파일 업로드 시 보안 위험:**

1. **악성 매크로**
   - 엑셀 파일에 매크로 포함 가능
   - 서버에서 엑셀 파싱 시 실행될 수 있음
   - **대응**: EPPlus/ClosedXML 라이브러리 사용 (매크로 무시)

2. **XXE 공격**
   - XLSX는 내부적으로 XML
   - XML 파싱 시 외부 엔티티 참조 공격 가능
   - **대응**: XML 파서 안전 모드 설정

3. **파일 크기 공격**
   - 100MB 엑셀 파일 업로드 → 서버 메모리 부족
   - **대응**: 파일 크기 제한 (예: 5MB)

**필요 작업:**
- 보안 검증 로직 구현: **1-2일**

---

### 🟡 5. 사용자 경험(UX) 저하 (Medium)

**엑셀 업로드 UI/UX 설계 필요:**

```
[파일 선택] → [컬럼 매핑] → [미리보기] → [저장]
     ↓             ↓             ↓          ↓
   복잡함        혼란스러움    필수 단계   오류 가능
```

**일반 사용자 관점:**
- "엑셀만 올리면 되는 줄 알았는데 왜 이렇게 복잡해요?"
- "컬럼 매핑이 뭔가요?"
- "미리보기 화면이 이상하게 나와요"

**대안: 간단한 UI 제공**
- 템플릿 빌더 화면 (드래그 앤 드롭)
- 필드 추가/삭제 버튼
- 실시간 미리보기

**필요 작업:**
- 템플릿 빌더 UI 개발: **5-7일**

---

### 🟢 6. 기존 코드 영향도 (Low)

**변경 필요한 파일:**
1. `CreateDocumentWindow.xaml` - 동적 DataGrid로 전환
2. `CreateDocumentViewModel.cs` - 템플릿 로딩 로직 추가
3. `DocumentItemViewModel.cs` - ExtraDataJson 바인딩
4. `TemplateManagementWindow.xaml` - 업로드 버튼 추가
5. `TemplateManagementViewModel.cs` - 업로드 로직 추가

**새로 추가할 파일:**
1. `ExcelTemplateParser.cs` - 엑셀 → JSON 변환
2. `ColumnMappingWindow.xaml` - 컬럼 매핑 UI
3. `ColumnMappingViewModel.cs` - 매핑 로직
4. `TemplateSchemaValidator.cs` - 스키마 검증

**총 예상 작업량:** **15-20일** (2인 기준)

---

## 📊 위험도 요약

| 항목 | 위험도 | 영향 범위 | 작업량 |
|------|--------|-----------|--------|
| 아키텍처 불일치 | 🔴 Critical | CreateDocumentWindow 전면 리팩토링 | 5-8일 |
| 데이터 매핑 복잡도 | 🔴 High | 새 UI 3개 추가 | 3일 |
| 템플릿 버전 관리 | 🟡 Medium | DB 스키마 변경 | 3-4일 |
| 보안 취약점 | 🟡 Medium | 파일 업로드 보안 | 1-2일 |
| UX 저하 | 🟡 Medium | 템플릿 빌더 UI | 5-7일 |
| 기존 코드 영향 | 🟢 Low | 5개 파일 수정 | 2-3일 |

**총 작업 기간:** **15-20일** (풀타임 2명 기준)

---

## 🚨 치명적 문제: 현재 시스템은 템플릿을 사용하지 않음

### 현재 상황
```
DocumentTemplate 테이블에 데이터 있음
                    ↓
         BUT CreateDocumentWindow는
         SchemaJson/LayoutJson을 읽지 않음
                    ↓
         ❌ 템플릿이 있어도 무용지물
```

### 근본 원인
**"거래명세표는 행동, 나머지는 관찰"** 철학과 충돌:
- DocumentTemplate은 "관찰" 대상 (읽기 전용)
- CreateDocumentWindow는 "행동" (문서 작성)
- **둘이 연결되지 않음**

### 해결 방안

#### 방안 1: 템플릿 시스템 완전 구현 (권장하지 않음)
**작업 내용:**
1. CreateDocumentWindow를 동적 UI로 전환
2. 템플릿 선택 드롭다운 추가
3. SchemaJson 기반 DataGrid 생성
4. ExtraDataJson 저장/로드 로직

**문제점:**
- 작업량 방대 (15-20일)
- 아키텍처 전면 수정
- 테스트 기간 추가 5-7일
- **투자 대비 효과 낮음** (B2B 거래명세표는 표준화됨)

#### 방안 2: 엑셀 가져오기 기능으로 대체 (권장)
**작업 내용:**
1. CreateDocumentWindow에 "엑셀에서 가져오기" 버튼 추가
2. 엑셀 행 → DocumentItem 변환
3. 기존 고정 컬럼에 데이터 채우기

**장점:**
- 작업량 적음 (2-3일)
- 아키텍처 변경 없음
- 사용자 편의성 향상
- **실용적**

**예시:**
```
엑셀 파일:
A        B     C      D
노트북   10    150000  15인치
마우스   20    30000   무선
                ↓
CreateDocumentWindow DataGrid에 자동 입력됨
```

#### 방안 3: 템플릿 시스템 제거 (과감한 선택)
**작업 내용:**
1. DocumentTemplate 테이블 삭제
2. TemplateManagementWindow 제거
3. 불필요한 코드 정리

**장점:**
- 시스템 단순화
- 유지보수 비용 감소
- **YAGNI 원칙** (You Aren't Gonna Need It)

**단점:**
- 향후 확장성 제한
- 이미 구현된 코드 폐기

---

## 💡 최종 권장사항

### 🎯 추천: 방안 2 (엑셀 가져오기)

**이유:**
1. **현실적**: 템플릿보다 엑셀이 더 익숙함
2. **빠른 구현**: 2-3일이면 완성
3. **아키텍처 보존**: 기존 설계 유지
4. **즉시 효과**: 사용자가 바로 활용 가능

**구현 계획:**
```
[Phase 1] 엑셀 파싱 (1일)
- EPPlus 라이브러리 추가
- 엑셀 → List<DocumentItemViewModel> 변환

[Phase 2] UI 추가 (1일)
- CreateDocumentWindow에 "엑셀 가져오기" 버튼
- 파일 선택 다이얼로그
- 데이터 그리드에 자동 채우기

[Phase 3] 검증 및 테스트 (1일)
- 필수 컬럼 검증
- 데이터 타입 체크
- 사용자 테스트
```

### 🔮 장기 계획

**현재 (v1.0):**
- 엑셀 가져오기 기능만 제공
- DocumentTemplate은 "조회 전용" 유지

**향후 (v2.0 이후):**
- 회사별 커스터마이징 요구 증가 시
- 템플릿 시스템 완전 구현 재검토
- 그때까지는 **미루기**

---

## 📌 결론

**엑셀 템플릿 업로드 기능은:**
- ❌ 현재 시스템과 맞지 않음 (템플릿 미사용)
- ❌ 작업량 방대 (15-20일)
- ❌ 투자 대비 효과 낮음

**대신 추천:**
- ✅ **엑셀 가져오기 기능** (2-3일, 즉시 효과)
- ✅ 아키텍처 변경 없음
- ✅ 사용자 편의성 극대화

**질문:**
1. 엑셀 가져오기 기능으로 충분한가요?
2. 아니면 템플릿 시스템 완전 구현을 원하시나요? (15-20일 소요)

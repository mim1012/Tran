# Tran 시스템 UI 개발 전략

## 핵심 원칙

### 계층 구조
```
거래명세표 (Core Layer - 유일한 행위 공간)
    ↓
거래처 관리 (Address Book Layer - 관계 관리)
    ↓
정산 관리 (Derived Layer - 읽기 전용 집계)
    ↓
양식 관리 (Template Layer - 출력 전용)
    ↓
로그/이력 (Audit Layer - 증거 보관)
    ↓
설정 (Configuration Layer - 환경 설정)
```

### 불변 규칙
- ✅ 거래명세표만 상태 머신 작동
- ❌ 나머지 화면에서 문서 수정 금지
- ❌ 해시에 영향주는 변경 금지
- ❌ 중앙 승인 시스템 금지

---

## 1️⃣ 거래명세표 (Core - 절대 중심)

### 역할
- 시스템의 **유일한 '행위 공간'**
- 상태 머신이 실제로 작동하는 유일한 영역

### 개발 아이디어

#### 읽기/쓰기 분리
```
목록 화면 (MainWindow.xaml - 현재 구현됨)
- 항상 빠르게 로드
- 상태 뱃지로 시각화
- 더블클릭 → 상세 화면

상세 화면 (DocumentDetailWindow.xaml - 미구현)
- 선택 시에만 로드
- 상태별 버튼 활성화
- 품목 리스트 편집
```

#### 상태 뱃지 = UI 설명서
```
작성중 (Gray)   → CanEdit, CanSend
전송됨 (Blue)   → ReadOnly
수신됨 (Orange) → CanConfirm, CanRequestRevision
확정됨 (Green)  → CanIncludeInSettlement
```

#### 상태별 행 색상 고정
```xaml
<!-- 이미 구현됨 -->
<DataGridRow BorderBrush="#1E5EFF" />  <!-- 전송됨: 파란색 좌측 바 -->
<DataGridRow BorderBrush="#1E7F34" />  <!-- 확정됨: 녹색 좌측 바 -->
```

### 절대 규칙
- ❌ 여기서 통계, 보고서, 그래프 만들지 말 것
- ❌ "대표 승인" 같은 상태 추가 금지
- ⭕ 거래명세표는 **'행동'**, 나머지는 **'관찰'**

---

## 2️⃣ 거래처 관리 (Address Book Layer)

### 역할
- 회원관리 ❌
- 인증 ❌
- 연결 관계 관리 ⭕

### 개발 아이디어

#### 거래처 상태 컬럼
```
미연결  → 아직 거래 없음
연결됨  → 최소 1건 이상 문서 교환
```

#### 마지막 거래일 표시
```sql
SELECT MAX(TransactionDate)
FROM Documents
WHERE FromCompanyId = ? OR ToCompanyId = ?
```

#### 거래처 삭제 ❌ → 비활성화만
```csharp
public CompanyStatus Status { get; set; }  // Active / Inactive
// 삭제 버튼 없음, "비활성화" 버튼만
```

### UX 포인트
- 거래처 추가 = **주소록 추가**
- 실제 연결은 **문서 전송 후 자동**

### 절대 규칙
- ❌ 중앙 승인
- ❌ 사업자 인증
- ❌ 거래처 단독 수정 요청

---

## 3️⃣ 정산 관리 (Derived Layer)

### 역할
- **CONFIRMED 문서를 읽기만 하는 영역**

### 개발 아이디어

#### 필터
```
거래처: ComboBox (Companies)
기간: DatePicker (From ~ To)
```

#### 결과
```
문서 리스트 (DataGrid)
- DocumentId
- FromCompany
- ToCompany
- TotalAmount
- ConfirmedAt

합계 (TextBlock)
- SUM(TotalAmount)
```

#### 버튼
```
Export to Excel
Export to PDF
```

### 구조 원칙
```
documents (원본 테이블)
    ↓
settlement_view (읽기 전용 뷰 또는 LINQ)
```

### 절대 규칙
- ❌ 정산 화면에서 문서 수정
- ❌ 정산 기준 변경 → 문서 영향

---

## 4️⃣ 양식 관리 (Template Layer)

### 역할
- 출력 전용 UX 레이어
- 문서 의미와 **완전히 분리**

### 개발 아이디어

#### 양식 종류
```
기본 양식 (System)
거래처별 양식 (Per Company)
```

#### 미리보기
```
출력 항목 On/Off
- 로고 표시
- 담당자 표시
- 해시 표시
```

### 중요한 분리
```
양식 변경 ≠ 문서 변경
해시 계산에 절대 영향 없음
```

### 이미 구현된 부분
- ✅ DocumentTemplate 모델
- ✅ SchemaJson (필드 정의)
- ✅ LayoutJson (레이아웃)
- ✅ 사이드바 메뉴 버튼

---

## 5️⃣ 로그 및 이력 (Audit Layer)

### 역할
- 분쟁 시 **최종 증거**

### 개발 아이디어

#### 상태 변경 로그만 표시
```sql
SELECT * FROM DocumentStateLogs
ORDER BY ChangedAt DESC
```

#### 필터
```
문서: DocumentId
기간: From ~ To
```

#### Export 버튼
```
Export to Excel (감사용)
```

### UX 원칙
- 수정 ❌
- 삭제 ❌
- 정렬만 가능

**이 화면은 "보기 불편할수록" 안전하다**

---

## 6️⃣ 설정 (Configuration Layer)

### 역할
- 시스템 **동작이 아니라 환경 설정**

### 개발 아이디어

#### 회사 정보
```
회사명
로고 업로드
사업자 번호
```

#### 자동 백업 On/Off
```
Local Path 설정
주기 설정 (Daily / Weekly)
```

#### 업데이트 상태 표시
```
현재 버전: v1.0.0
최신 버전 확인
```

### 분리 원칙
```
설정 변경 → 문서 영향 ❌
설정 변경 → 해시 영향 ❌
```

---

## 구현 우선순위 제안

### Phase 1: Core (필수)
1. ✅ 거래명세표 목록 (완료)
2. 🔲 거래명세표 상세 화면
3. 🔲 문서 생성/편집 화면

### Phase 2: 관계 관리
4. 🔲 거래처 관리 화면

### Phase 3: 파생 기능
5. 🔲 정산 관리 화면
6. ✅ 양식 관리 (템플릿 시스템 완료)

### Phase 4: 감사 및 설정
7. 🔲 로그 및 이력 화면
8. 🔲 설정 화면

---

## 개발 시 체크리스트

### 모든 화면 공통
- [ ] Enterprise B2B 스타일 유지
- [ ] MVVM 패턴 준수
- [ ] INotifyPropertyChanged 구현
- [ ] XML 주석 작성

### 데이터 변경 화면
- [ ] State → Boolean 매핑 확인
- [ ] StateTransitionService 사용
- [ ] 낙관적 잠금 (StateVersion) 확인
- [ ] ContentHash 재계산

### 읽기 전용 화면
- [ ] 수정/삭제 버튼 없음
- [ ] Export 기능만 제공
- [ ] 필터링 성능 최적화

---

**생성일:** 2026-01-06
**작성자:** Claude (Dev Agent)
**버전:** 1.0

---
stepsCompleted: ["step-01-init", "step-02-discovery"]
inputDocuments: ["user-provided-initial-requirements", "user-provided-ui-flow-design", "user-provided-wpf-control-design", "user-provided-state-machine-definition", "user-provided-database-schema", "user-provided-mvvm-binding-rules", "user-provided-api-sync-rules", "user-provided-recovery-scenarios", "user-provided-hash-integrity"]
workflowType: 'prd'
lastStep: 2
briefCount: 0
researchCount: 0
brainstormingCount: 0
projectDocsCount: 0
userProvidedRequirements: 9
---

# Product Requirements Document - Tran

**Author:** PC_1M
**Date:** 2026-01-06

---

## Executive Summary

### 프로젝트 분류 및 개요

**기술 유형**: Desktop Application (WPF, C# .NET, SQLite)
**도메인**: B2B Transaction Statement Confirmation & Settlement Support System
**복잡도**: **High (분산 상태, 암호화, 동시성 제어) - 성숙한 아키텍처 패턴으로 관리됨**

이 시스템은 서로 다른 기업 간 거래에서 발생하는 **거래명세표 작성, 조건 확정, 정산 과정의 혼선과 분쟁을 줄이기 위한 오프라인 우선 데스크톱 애플리케이션**입니다.

**핵심 통찰력**:
> "엑셀에는 확정 개념과 책임 주체가 없음"

**제품 철학**:
> "엑셀처럼 편하지만, 계약처럼 엄격하다"

---

### 해결하려는 핵심 문제 (사용자의 실제 고통)

**현실의 고통 시나리오**:

1. **3개월 전 카톡에서 약속한 단가를 거래처가 기억하지 못함**
   - "그때 분명히 10% 할인해주기로 했잖아요"
   - "그런 적 없는데요?"
   - 증거를 찾기 위해 카톡 대화 내역 수백 개를 뒤짐

2. **엑셀 파일 10개 버전이 돌아다니는데 어떤 게 최종본인지 모름**
   - `거래명세_최종.xlsx`, `거래명세_최종_진짜.xlsx`, `거래명세_최종_진짜_수정.xlsx`
   - 정산 시점에 서로 다른 파일을 보고 있음을 발견

3. **정산 시점에 발생하는 분쟁으로 관계 악화 및 다음 거래 불발**
   - 분쟁 해결에 2주 소요
   - 신뢰 손상으로 거래처 상실
   - 매출 기회 손실

4. **거래 조건이 카톡, 전화, 이메일, 엑셀 등으로 흩어짐**
   - 정산 기준이 매번 달라짐
   - 책임 주체가 불명확
   - 명세표 작성 시 단가/수량/옵션 누락

---

### 이 시스템의 차별성

#### 1. **사용자가 느끼는 차이**

✅ **"확정 버튼을 누르면 진짜 끝. 상대방도 나도 못 바꿔요"**
- 확정 후 수정 불가 - 무조건 새 버전 생성
- 양측이 동의한 내용이 불변 기준이 됨

✅ **"3년 전 거래도 1초 만에 찾아서 PDF 출력"**
- 모든 변경 이력 자동 기록
- 분쟁 시 즉시 증빙 제공
- 정산 기준을 시스템이 강제

✅ **"인터넷 없어도 작업하고, 나중에 자동으로 전송"**
- 로컬 DB(SQLite) 기반 오프라인 우선 설계
- 네트워크 복구 시 자동 동기화
- 업무 마비 없음

#### 2. **이를 가능하게 하는 기술** (간략)

🔧 **State Machine 기반 상태 관리**
- 7가지 명확한 문서 상태 (Draft → Sent → Received → Confirmed 등)
- 각 상태마다 허용/차단되는 작업이 명확
- "상태가 곧 권한"

🔐 **암호학적 무결성 보장**
- SHA-256 해시로 위변조 방지
- Canonical JSON 직렬화로 결정적 해시 생성
- 문서 내용이 1바이트라도 바뀌면 즉시 탐지

🌐 **분산 상태 동기화**
- Single-Writer 원칙으로 동시성 충돌 90% 제거
- Optimistic Locking으로 상태 충돌 검출
- Idempotency Key로 중복 요청 방지
- Outbox Pattern으로 안전한 메시지 전달

🔄 **실패를 정상 흐름으로 설계**
- 네트워크 끊김, 서버 다운, 프로그램 크래시 대응
- 모든 상태 전이 전 스냅샷 저장
- 자동 복구 및 재시도 메커니즘

---

### 전략적 포지셔닝

**이 시스템은:**

❌ **실시간 협업 시스템이 아닙니다**
- 합의된 상태를 전달하는 시스템입니다
- 서버는 Stateless Relay + Verifier일 뿐, 진실의 원천이 아닙니다

❌ **법적 계약 효력을 주장하지 않습니다**
- 전자계약 시스템 규제를 회피
- 진입 장벽 최소화
- "분쟁 예방 장치"로 포지셔닝

❌ **협업 툴이 아닙니다**
- 분쟁을 억제하는 장치입니다
- 거래 기준을 고정하는 정산 보조 시스템입니다

---

### 서버 아키텍처 철학

**서버의 역할**:
- **중계자**: 문서 메타데이터만 저장 (내용 ❌)
- **검증자**: 상태 일치 및 버전 검증만 수행 (계산 ❌, 판단 ❌)
- **힌트 제공자**: 클라이언트 복구 시 최신 상태 제공

**이점**:
- 서버 비용 최소화
- 법적 리스크 최소화 (거래 내용 미보관)
- 확장성 우수

---

### 성공 기준 (정량 지표)

**비즈니스 임팩트**:
- 거래 조건 분쟁 **80% 감소**
- 정산 처리 시간 **50% 단축**
- 거래처 관계 악화로 인한 매출 손실 **30% 감소**

**기술 안정성**:
- 네트워크 장애 시에도 **로컬 작업 100% 가능**
- 상태 충돌 자동 해결률 **95% 이상**
- 데이터 무결성 검증 성공률 **100%**

---

### 타깃 사용자

**주요 타깃**:
- 도소매 거래 기업
- 대리점–가맹점 구조 사업자
- 외주/용역 거래 기업
- 거래처가 여러 곳이고 정산이 복잡한 업체

**사용자 특성**:
- 엑셀에 익숙함
- 웹 서비스보다 로컬 프로그램 선호
- 분쟁을 싫어하고 증빙을 중요하게 여김
- IT 숙련도 중~하

---

### 기술 스택

**클라이언트**:
- 플랫폼: Desktop Application (Windows)
- UI 프레임워크: WPF (Windows Presentation Foundation)
- 언어: C# (.NET)
- 로컬 DB: SQLite
- 아키텍처 패턴: MVVM + State Machine

**서버**:
- 경량 중계 서버 (HTTPS API)
- 최소 메타데이터 DB (문서 ID, 상태, 해시만)

**보안 및 무결성**:
- SHA-256 해시
- Canonical JSON Serialization
- (선택) RSA/ECDSA 전자서명

---

### MVP 범위 (1차 출시)

**포함**:
- 거래처 관리
- 거래명세표 작성
- 전달 및 확정
- 정산 계산
- PDF 출력
- 변경 이력 자동 기록
- 오프라인 모드

**제외**:
- 실시간 협업
- 모바일 지원
- 회계/세무 신고 기능
- 전자서명 (고급 옵션)

---

### 요금 모델 (초안)

- 기업당 월 이용료: **70,000원**
- PC 1대 기준
- 거래명세표 및 정산 무제한

---

### 핵심 설계 원칙

1. **"상태가 곧 권한이다"** - State Machine이 모든 UI/권한을 통제
2. **"엑셀처럼 편하지만, 계약처럼 엄격하다"** - UX와 제약의 균형
3. **"상태 테이블을 중심으로 설계한다"** - State-centric DB 설계
4. **"실패는 예외가 아니라 정상 흐름이다"** - Fail-safe 설계
5. **"서버는 판단하지 않고, 클라이언트는 믿지 않는다"** - 신뢰 모델

---

## Initial Requirements (User Provided)

### 1. 제품 개요

#### 1.1 제품 목적

본 제품은 **서로 다른 기업 간 거래에서 발생하는 거래명세표 작성, 조건 확정, 정산 과정의 혼선과 분쟁을 줄이기 위한 업무용 프로그램(EXE)**이다.

웹 기반 실시간 협업이 아닌,
**"명세표 전달 → 확인 → 확정 → 정산"**이라는
현실적인 거래 흐름을 시스템화하는 것을 목표로 한다.

#### 1.2 해결하려는 핵심 문제

- 거래 조건이 카톡, 전화, 엑셀 등으로 흩어짐
- 명세표 작성 시 단가/수량/옵션 누락
- 거래 후 조건 변경에 대한 분쟁
- 정산 기준이 매번 달라짐
- 엑셀에는 확정 개념과 책임 주체가 없음

#### 1.3 제품 포지션

- 문서 작성 툴 ❌
- 회계 프로그램 ❌
- 거래 기준을 고정하는 정산 보조 시스템 ⭕

### 2. 타깃 사용자

#### 2.1 주요 타깃

- 도소매 거래 기업
- 대리점–가맹점 구조 사업자
- 외주/용역 거래 기업
- 거래처가 여러 곳이고 정산이 복잡한 업체

#### 2.2 사용자 특성

- 엑셀에 익숙함
- 웹 서비스보다 로컬 프로그램 선호
- 분쟁을 싫어하고 증빙을 중요하게 여김
- IT 숙련도 중~하

### 3. 핵심 가치 제안

- 거래명세표를 합의된 기준 문서로 격상
- 조건 변경 이력 자동 기록
- 정산 기준을 시스템이 강제
- 분쟁 시 판단 근거 제공

### 4. 주요 기능 요구사항

#### 4.1 거래처 관리

**기능**
- 거래처 등록 / 수정 / 비활성화
- 거래처별 기본 정보 관리

**요구사항**
- 거래처 코드 자동 생성
- 메모 필드 제공

#### 4.2 거래명세표 작성

**기능**
- 신규 거래명세표 생성
- 품목 다중 입력
- 수량, 단가, 옵션 입력

**요구사항**
- 엑셀과 유사한 테이블 입력 UX
- 실시간 금액 자동 계산
- 금액 계산은 정밀도 보장 (소수점 오류 방지)

#### 4.3 거래명세표 전달 (통신)

**기능**
- 상대 거래처로 거래명세표 전송
- 수신 여부 확인

**요구사항**
- 중앙 중계 서버를 통한 전달
- 거래명세표는 프로그램 간에만 열림
- 서버에는 문서 메타데이터만 저장

#### 4.4 거래 조건 확정 (핵심)

**기능**
- 수신자가 거래명세표 확인 후 "확정" 처리
- 확정 상태 양측 동기화

**요구사항**
- 확정 후 수정 불가
- 수정 시 자동으로 새 버전 생성
- 이전 버전은 조회만 가능

#### 4.5 변경 이력 및 로그

**기능**
- 거래명세표 변경 이력 자동 기록

**로그 항목**
- 변경자
- 변경 시각
- 변경 항목
- 변경 전/후 값

**요구사항**
- 사용자가 임의 삭제/수정 불가
- PDF 출력 가능

#### 4.6 정산 기능

**기능**
- 거래명세표 기반 정산 계산
- 기간별 정산 집계

**요구사항**
- 거래처별 정산 기준 적용
- 동일 조건에서 동일 결과 재현 가능

#### 4.7 정산 리포트 및 출력

**기능**
- 거래처별 정산 요약
- 정산 완료 / 미정산 구분

**출력**
- 화면
- PDF
- Excel

### 5. 비기능 요구사항

#### 5.1 배포

- 로컬 설치형 EXE
- 단일 실행 파일 제공

#### 5.2 안정성

- 프로그램 비정상 종료 시 데이터 보호
- 자동 백업 기능 제공

#### 5.3 성능

- 로컬 DB 기반
- 수천 건 이상의 거래명세표 처리 가능

#### 5.4 보안

- 로컬 데이터 암호화 옵션
- 전송 데이터 무결성 검증

### 6. 기술 요구사항 (초안)

- 언어: C# (.NET)
- UI: WPF
- 로컬 DB: SQLite
- 서버: 경량 중계 서버
- 통신: HTTPS 기반 API

### 7. 요금 모델 (초안)

- 기업당 월 이용료: 70,000원
- PC 1대 기준
- 거래명세표 및 정산 무제한

### 8. MVP 범위 (1차 출시)

**포함**
- 거래처 관리
- 거래명세표 작성
- 전달 및 확정
- 정산 계산
- PDF 출력

**제외**
- 실시간 협업
- 모바일 지원
- 회계/세무 신고 기능

### 9. 제품 철학 (고정)

> **이 시스템에 기록되고 확정된 내용이 거래의 기준이다.**

---

## UI Flow Design (User Provided)

### 0️⃣ 전체 화면 구조 개요

**기본 레이아웃 (고정)**

- 좌측 사이드바
- 우측 메인 작업 영역
- 상단 상태 표시줄

```
┌───────────────┬───────────────────────────┐
│ 거래처 관리    │ 메인 작업 화면             │
│ 거래명세표     │ (목록 / 상세 / 입력)       │
│ 정산 관리      │                           │
│ 로그 / 설정    │                           │
└───────────────┴───────────────────────────┘
```

### 1️⃣ 프로그램 실행 흐름

**실행 시**

1. 프로그램 실행
2. 로컬 DB 로딩
3. (선택) 서버 연결 상태 확인
4. [거래명세표 목록 화면] 진입

### 2️⃣ 거래명세표 목록 화면 (기본 홈)

**목적**
- 전체 흐름의 허브
- "지금 뭐가 진행 중인지" 한눈에 보기

**화면 구성**
- 상단: 검색 / 필터
- 중앙: 거래명세표 리스트
- 하단: 상태 요약

**리스트 컬럼**
- 문서번호
- 거래처
- 상태
  - 작성중
  - 전송됨
  - 확정됨
- 금액
- 작성일
- 마지막 변경일

**사용자 액션**
- [신규 거래명세표]
- 더블클릭 → 상세 화면
- 우클릭 → 전송 / 삭제 / 복사

### 3️⃣ 신규 거래명세표 생성 흐름

**Step 1. 거래처 선택**
- 드롭다운 또는 검색
- 거래처 미등록 시 → [거래처 추가] 팝업

**Step 2. 기본 정보 입력**
- 거래일
- 비고
- 내부 메모 (상대방 비공개)

### 4️⃣ 거래명세표 작성 화면 (핵심)

**화면 구조**
- 상단: 문서 정보
- 중앙: 품목 테이블
- 우측: 금액 요약
- 하단: 액션 버튼

**품목 테이블**
```
품목명    수량    단가    옵션    금액
입력      입력    입력    입력    자동
```

**UX 규칙**
- Enter → 다음 셀 이동
- 수량/단가 변경 시 즉시 재계산
- 행 추가/삭제 단축키 제공

**우측 금액 요약**
- 공급가 합계
- 할인
- 기타 차감
- 최종 금액 (강조 표시)

**하단 버튼 상태**
- [임시저장]
- [전송]
- [닫기]

### 5️⃣ 거래명세표 전송 흐름

**전송 버튼 클릭 시**

1. 확인 팝업
   - "이 거래명세표를 전송하시겠습니까?"
2. 서버로 전송
3. 상태 변경:
   - 작성중 → 전송됨
4. 화면 상단 상태 뱃지 변경

**전송 후 제한**
- 내용 수정 ❌
- [수정 요청]만 가능

### 6️⃣ 수신 거래명세표 확인 흐름 (상대방)

**수신 시**
- 목록에 [수신됨] 표시
- 알림 아이콘 표시

**상세 화면 진입 시**
- 전체 내용 읽기 전용
- 하단 버튼:
  - [확정]
  - [수정 요청]

### 7️⃣ 거래 조건 확정 흐름 (핵심)

**[확정] 클릭 시**

1. 최종 확인 팝업
   - "확정 후 수정할 수 없습니다"
2. 확정 처리
   - 서버로 확정 상태 전송
   - 양측 상태: 확정됨

**확정 후 화면 변화**
- 모든 입력 비활성화
- "확정 일시" 표시
- 버전 번호 고정

### 8️⃣ 수정 요청 흐름

**수정 요청 클릭**
1. 요청 사유 입력 팝업
2. 서버로 요청 전송

**송신자 화면**
- "수정 요청 있음" 표시
- [새 버전 생성] 버튼 활성화

### 9️⃣ 버전 분기 흐름

**새 버전 생성**
1. 기존 명세표 복사
2. 버전 +1

**이전 버전:**
- 조회만 가능
- "확정됨(구버전)" 표시

### 🔟 정산 관리 화면

**목적**
- 확정된 거래명세표 기반 집계

**화면 구성**
- 거래처 선택
- 기간 선택
- 정산 결과 테이블

**정산 결과 컬럼**
- 문서번호
- 금액
- 상태 (정산 / 미정산)

**액션**
- [정산 확정]
- [PDF 출력]
- [Excel 출력]

### 1️⃣1️⃣ 로그 / 변경 이력 화면

**대상**
- 거래명세표 단위

**표시 항목**
- 변경 시각
- 변경자
- 변경 내용

**특징**
- 읽기 전용
- 출력 가능

### 1️⃣2️⃣ 오류 / 네트워크 상태 UX

**서버 연결 실패 시**

상단 상태바:
- "오프라인 모드"
- 로컬 작업은 정상 진행
- 전송 버튼 비활성화

### 1️⃣3️⃣ 설정 화면 (간단)

- 회사 정보
- 로고
- 기본 출력 양식
- 백업 설정

### 1️⃣4️⃣ UI 플로우 핵심 원칙 (절대 변경 금지)

1. **상태가 명확해야 한다**
   - 작성중 / 전송됨 / 확정됨

2. **확정 이후 수정 불가**
   - 무조건 새 버전

3. **모든 변경은 흔적을 남긴다**

### 한 문장 요약

> **"엑셀처럼 편하지만, 계약처럼 엄격하다."**

---

## WPF Control Design (User Provided)

### 0️⃣ 공통 컨트롤 (전 화면 공통)

**상단 상태바**

- `Label`: 회사명
- `Label`: 서버 상태 (온라인 / 오프라인)
- `Label`: 로그인/라이선스 상태
- `Button`: 설정 ⚙️

**공통 UX 규칙**

상태 뱃지 색상:
- 작성중: 회색
- 전송됨: 파랑
- 확정됨: 초록

확정 상태에서는 모든 입력 컨트롤 `Disabled`

### 1️⃣ 거래명세표 목록 화면 (홈)

**🔍 상단 필터 영역**

- `TextBox`: 문서번호 검색
- `ComboBox`: 거래처 선택
- `ComboBox`: 상태 필터 (작성중 / 전송됨 / 확정됨)
- `DatePicker`: 시작일
- `DatePicker`: 종료일
- `Button`: 검색

**📋 리스트 영역 (DataGrid)**

`DataGrid` 컬럼:
- 문서번호
- 거래처명
- 상태
- 총금액
- 작성일
- 최종수정일

**🔘 하단 버튼**

- `Button`: 신규 거래명세표
- `Button`: 열기
- `Button`: 삭제 (작성중만)
- `Button`: 새로고침

### 2️⃣ 거래처 관리 화면

**🧾 거래처 리스트**

`DataGrid` 컬럼:
- 거래처 코드
- 거래처명
- 상태 (활성/비활성)

**✍️ 거래처 입력/수정**

- `TextBox`: 거래처명
- `TextBox`: 담당자명
- `TextBox`: 연락처
- `TextBox`: 이메일
- `TextBox`: 메모

**🔘 버튼**

- `Button`: 신규
- `Button`: 저장
- `Button`: 비활성화
- `Button`: 닫기

### 3️⃣ 거래명세표 작성 화면 (핵심)

**🧾 상단 문서 정보**

- `Label`: 문서번호
- `ComboBox`: 거래처 선택
- `DatePicker`: 거래일
- `TextBox`: 비고
- `TextBox`: 내부 메모 (상대방 비공개)

**📦 품목 입력 테이블 (DataGrid)**

컬럼별 컨트롤:
- `TextBox`: 품목명
- `NumericUpDown`: 수량
- `NumericUpDown`: 단가
- `TextBox`: 옵션/비고
- `TextBlock`: 금액 (자동 계산)

행 컨트롤:
- `Button`: 행 추가
- `Button`: 행 삭제

**💰 우측 금액 요약 패널**

- `TextBlock`: 공급가 합계
- `NumericUpDown`: 할인
- `NumericUpDown`: 기타 차감
- `TextBlock`: 최종 금액 (Bold / 강조)

**🔘 하단 액션 버튼**

- `Button`: 임시저장
- `Button`: 전송
- `Button`: 닫기

### 4️⃣ 전송 완료 상태 화면

**상태 표시**

- `Label`: 상태 = 전송됨
- `Label`: 전송 일시

**제한 컨트롤**

- 모든 `TextBox`, `DataGrid` → `Disabled`

**🔘 버튼**

- `Button`: 수정 요청
- `Button`: 닫기

### 5️⃣ 수신 거래명세표 확인 화면

**📄 읽기 전용 상세**

- 모든 필드 `ReadOnly`

**🔘 하단 버튼**

- `Button`: 확정
- `Button`: 수정 요청
- `Button`: 닫기

### 6️⃣ 수정 요청 팝업

**필드**

- `TextBox` (Multiline): 수정 요청 사유

**버튼**

- `Button`: 요청 전송
- `Button`: 취소

### 7️⃣ 확정 처리 팝업

**메시지**

`TextBlock`: "확정 후에는 수정할 수 없습니다."

**버튼**

- `Button`: 확정
- `Button`: 취소

### 8️⃣ 버전 이력 화면

**📚 버전 리스트**

`DataGrid` 컬럼:
- 버전 번호
- 상태
- 생성일
- 확정일

**🔘 버튼**

- `Button`: 선택 버전 열기
- `Button`: 닫기

### 9️⃣ 정산 관리 화면

**🔍 필터 영역**

- `ComboBox`: 거래처
- `DatePicker`: 기간 시작
- `DatePicker`: 기간 종료
- `Button`: 조회

**📊 정산 결과 테이블**

`DataGrid` 컬럼:
- 문서번호
- 거래일
- 금액
- 정산 상태

**🔘 버튼**

- `Button`: 정산 확정
- `Button`: PDF 출력
- `Button`: Excel 출력

### 🔟 변경 로그 화면

**📜 로그 테이블**

`DataGrid` 컬럼:
- 변경 시각
- 변경자
- 변경 항목
- 변경 전
- 변경 후

**🔘 버튼**

- `Button`: PDF 출력
- `Button`: 닫기

### 1️⃣1️⃣ 설정 화면

**기본 설정**

- `TextBox`: 회사명
- `ImagePicker`: 로고
- `TextBox`: 주소/연락처

**백업 설정**

- `CheckBox`: 자동 백업 사용
- `TextBox`: 백업 경로
- `Button`: 백업 실행

**🔘 버튼**

- `Button`: 저장
- `Button`: 닫기

### 🔑 컨트롤 설계 핵심 원칙 (중요)

1. **상태가 바뀌면 컨트롤도 바뀐다**
   - 확정 = 모든 입력 차단

2. **삭제는 작성중에서만**

3. **전송 이후는 '요청'만 가능**

### 한 줄 요약

> **"엑셀 UX + 계약 시스템 제약"**을 동시에 만족시키는 구조다.

---

## State Machine Definition (User Provided)

### 🧠 거래명세표 상태 머신 정의 (State Transition Table)

> 이 표 하나면 버튼 활성/비활성, 수정 가능 여부, 서버 통신 타이밍 전부 결정된다.

### 1️⃣ 상태 목록 (States)

| 상태 코드 | 상태명 | 설명 |
|----------|--------|------|
| `DRAFT` | 작성중 | 송신자가 작성 중인 상태 |
| `SENT` | 전송됨 | 상대방에게 전달 완료 |
| `RECEIVED` | 수신됨 | 상대방이 수신 확인 |
| `CONFIRMED` | 확정됨 | 거래 조건 최종 확정 |
| `REVISION_REQUESTED` | 수정요청 | 수신자가 수정 요청 |
| `SUPERSEDED` | 구버전 | 새 버전 생성으로 대체됨 |
| `CANCELLED` | 취소됨 | 송신자가 전송 전 취소 |

### 2️⃣ 상태 전이 핵심 원칙 (절대 규칙)

1. **CONFIRMED 이후 수정 불가**
2. **수정은 새 버전 생성으로만 가능**
3. **상태 전이는 단방향**
4. **서버는 중계만, 판단은 로컬**

### 3️⃣ 상태 전이 테이블 (메인)

**📋 기본 흐름**

| 현재 상태 | 사용자 액션 | 다음 상태 | 서버 통신 | 비고 |
|----------|------------|----------|----------|------|
| `DRAFT` | 저장 | `DRAFT` | ❌ | 로컬 저장 |
| `DRAFT` | 전송 | `SENT` | ⭕ | 최초 전송 |
| `DRAFT` | 취소 | `CANCELLED` | ❌ | 완전 삭제 가능 |
| `SENT` | 수신 확인 | `RECEIVED` | ⭕ | 상대방 EXE |
| `RECEIVED` | 확정 | `CONFIRMED` | ⭕ | 핵심 |
| `RECEIVED` | 수정 요청 | `REVISION_REQUESTED` | ⭕ | 사유 포함 |
| `REVISION_REQUESTED` | 새 버전 생성 | `DRAFT` | ❌ | 버전 +1 |
| `CONFIRMED` | — | — | — | 종결 상태 |

### 4️⃣ 상태별 UI 제어 매트릭스

**🟡 DRAFT (작성중)**

| 항목 | 허용 |
|------|------|
| 필드 수정 | ⭕ |
| 전송 | ⭕ |
| 삭제 | ⭕ |
| 로그 기록 | ⭕ |
| 정산 포함 | ❌ |

**🔵 SENT (전송됨)**

| 항목 | 허용 |
|------|------|
| 필드 수정 | ❌ |
| 수정 요청 | ❌ |
| 재전송 | ❌ |
| 상태 표시 | ⭕ |
| 로그 기록 | ⭕ |

**🟣 RECEIVED (수신됨)**

| 항목 | 허용 |
|------|------|
| 필드 수정 | ❌ |
| 확정 | ⭕ |
| 수정 요청 | ⭕ |
| 로그 기록 | ⭕ |
| 정산 포함 | ❌ |

**🔴 REVISION_REQUESTED (수정요청)**

| 항목 | 허용 |
|------|------|
| 기존 문서 수정 | ❌ |
| 새 버전 생성 | ⭕ |
| 로그 기록 | ⭕ |
| 정산 포함 | ❌ |

**🟢 CONFIRMED (확정됨)**

| 항목 | 허용 |
|------|------|
| 필드 수정 | ❌ |
| 정산 포함 | ⭕ |
| PDF 출력 | ⭕ |
| 로그 기록 | ⭕ |
| 상태 변경 | ❌ |

**⚫ SUPERSEDED (구버전)**

| 항목 | 허용 |
|------|------|
| 조회 | ⭕ |
| 출력 | ⭕ |
| 수정 | ❌ |
| 정산 포함 | ❌ |

**❌ CANCELLED (취소됨)**

| 항목 | 허용 |
|------|------|
| 조회 | ⭕ |
| 출력 | ❌ |
| 수정 | ❌ |
| 재사용 | ❌ |

### 5️⃣ 서버 통신 발생 지점 (명확화)

| 이벤트 | 통신 |
|--------|------|
| 전송 (`DRAFT` → `SENT`) | `POST /documents` |
| 수신 확인 | `PATCH /documents/{id}/received` |
| 수정 요청 | `POST /documents/{id}/revision` |
| 확정 | `POST /documents/{id}/confirm` |

👉 **정산/계산은 서버 통신 없음**

### 6️⃣ 상태 전이 다이어그램 (텍스트 표현)

```
DRAFT
  ├─ 전송 ─▶ SENT
  │              │
  │              └─ 수신 ▶ RECEIVED
  │                         ├─ 확정 ▶ CONFIRMED
  │                         └─ 수정요청 ▶ REVISION_REQUESTED
  │                                           │
  │                                           └─ 새버전 ▶ DRAFT
  └─ 취소 ▶ CANCELLED
```

### 7️⃣ 코드 레벨 Enum 예시 (C#)

```csharp
public enum DocumentState
{
    Draft,
    Sent,
    Received,
    RevisionRequested,
    Confirmed,
    Superseded,
    Cancelled
}
```

### 8️⃣ 상태 머신 검증 체크리스트

- [ ] CONFIRMED에서 버튼 활성화되는가?
- [ ] 서버 다운 시 상태 꼬이지 않는가?
- [ ] 새 버전 생성 시 구버전이 SUPERSEDED 되는가?
- [ ] 로그가 상태 변경마다 기록되는가?

### 핵심 문장

> **"상태가 곧 권한이다."**

이 시스템은 **UI가 아니라 상태 머신이 모든 걸 통제한다.**

---

## Database Schema Design (User Provided)

### 🗂 DB 테이블 설계 (State 중심)

**전체 테이블 맵:**
- `companies`
- `users`
- `documents`
- `document_versions`
- `document_items`
- `document_state_logs`
- `revision_requests`
- `settlements`

**데이터베이스 분리:**
- ✔ **로컬 DB**: SQLite (전체 데이터)
- ✔ **서버 DB**: `documents` + `document_state_logs` (메타데이터만)

---

### 1️⃣ companies (거래처 / 회사)

```sql
companies (
    company_id        TEXT PRIMARY KEY,
    company_name      TEXT NOT NULL,
    contact_name      TEXT,
    contact_email     TEXT,
    contact_phone     TEXT,
    status            TEXT,        -- ACTIVE / INACTIVE
    created_at        DATETIME
)
```

**설계 포인트:**
- 회사는 "주체"
- 문서의 `from_company_id`, `to_company_id` 기준이 됨

---

### 2️⃣ users (사용자, 로컬 기준)

```sql
users (
    user_id       TEXT PRIMARY KEY,
    company_id    TEXT,
    user_name     TEXT,
    role          TEXT,     -- ADMIN / STAFF
    created_at    DATETIME
)
```

**설계 포인트:**
- MVP에서는 단일 사용자여도 테이블 유지
- 로그·책임 추적용

---

### 3️⃣ documents (문서 마스터 – 상태의 중심)

```sql
documents (
    document_id       TEXT PRIMARY KEY,
    parent_document_id TEXT,      -- 이전 버전 (nullable)
    version_number    INTEGER,

    from_company_id   TEXT,
    to_company_id     TEXT,

    state             TEXT,       -- Draft / Sent / Received / Confirmed / ...

    total_amount      DECIMAL,

    created_by        TEXT,
    created_at        DATETIME,
    sent_at           DATETIME,
    confirmed_at      DATETIME
)
```

**🔑 핵심 컬럼:**
- `state` → 모든 UI/권한 기준
- `parent_document_id` → 버전 트리
- `version_number` → 사용자 인지용

---

### 4️⃣ document_versions (버전 메타 – 선택적)

단일 테이블로도 가능하지만 이력 분리를 명확히 하고 싶으면 유지

```sql
document_versions (
    version_id     TEXT PRIMARY KEY,
    document_id    TEXT,
    version_number INTEGER,
    created_at     DATETIME,
    created_by     TEXT
)
```

---

### 5️⃣ document_items (품목)

```sql
document_items (
    item_id        TEXT PRIMARY KEY,
    document_id    TEXT,
    item_name      TEXT,
    quantity       DECIMAL,
    unit_price     DECIMAL,
    option_text    TEXT,
    line_amount    DECIMAL
)
```

**규칙:**
- `CONFIRMED` 상태에서 INSERT/UPDATE ❌
- 계산 값은 항상 로컬 재현 가능

---

### 6️⃣ document_state_logs (상태 로그 – 가장 중요)

```sql
document_state_logs (
    log_id         TEXT PRIMARY KEY,
    document_id    TEXT,
    from_state     TEXT,
    to_state       TEXT,
    changed_by     TEXT,
    changed_at     DATETIME,
    reason         TEXT
)
```

**💥 이 테이블의 의미:**
- 분쟁 시 최종 증빙
- 삭제/수정 불가
- 서버로 보내도 되는 유일한 "신뢰 데이터"

---

### 7️⃣ revision_requests (수정 요청)

```sql
revision_requests (
    request_id     TEXT PRIMARY KEY,
    document_id    TEXT,
    requested_by   TEXT,
    request_reason TEXT,
    requested_at   DATETIME,
    status         TEXT   -- OPEN / RESOLVED
)
```

---

### 8️⃣ settlements (정산)

```sql
settlements (
    settlement_id  TEXT PRIMARY KEY,
    company_id     TEXT,
    period_start   DATE,
    period_end     DATE,
    total_amount   DECIMAL,
    settled_at     DATETIME
)
```

**규칙:**
- `documents.state = CONFIRMED` 만 집계 대상
- 문서 삭제되어도 정산 기록은 유지

---

### 9️⃣ 상태 중심 설계 규칙 (절대 규칙)

**Rule 1:**
```
documents.state = CONFIRMED
→ document_items 수정 불가
```

**Rule 2:**
```
상태 변경 시
→ document_state_logs INSERT 필수
```

**Rule 3:**
```
새 버전 생성 시
→ 이전 document.state = SUPERSEDED
```

---

### 🔁 상태 전이 트리 (DB 기준)

```
documents
 ├─ document_id = A (state=CONFIRMED)
 ├─ document_id = B (parent=A, state=SUPERSEDED)
 └─ document_id = C (parent=B, state=DRAFT)
```

---

### 🔟 서버 DB에 저장되는 것 (최소)

**❌ 서버에 저장하지 않음:**
- `document_items` ❌
- 금액 상세 ❌

**✔ 서버에 저장:**
- `documents` (id, from, to, state, hash)
- `document_state_logs`

👉 **법적·보안 리스크 최소화**

---

### 1️⃣1️⃣ 인덱스 권장

```sql
CREATE INDEX idx_documents_state ON documents(state);
CREATE INDEX idx_documents_company ON documents(from_company_id, to_company_id);
CREATE INDEX idx_logs_document ON document_state_logs(document_id);
```

---

### 한 문장 요약

> **"문서 테이블이 아니라, 상태 테이블을 중심으로 설계한다."**

---

## ViewModel ↔ State Binding Rules (User Provided)

### 🧩 ViewModel ↔ State 바인딩 규칙 (WPF · MVVM 기준)

---

### 0️⃣ 핵심 원칙 (먼저 박아둬라)

1. **View는 상태를 해석하지 않는다.**
2. **State → ViewModel → UI는 단방향이다.**

**❌ 금지:**
- XAML에서 `State == Draft` 비교
- Code-behind에서 상태 판단

**⭕ 정답:**
- ViewModel이 UI용 Boolean을 제공

---

### 1️⃣ State → UI 권한 매핑 구조

**문서 상태 Enum (Domain)**

```csharp
public enum DocumentState
{
    Draft,
    Sent,
    Received,
    RevisionRequested,
    Confirmed,
    Superseded,
    Cancelled
}
```

---

### 2️⃣ ViewModel의 역할 정의

**DocumentViewModel의 책임:**

1. 현재 문서의 `State`를 해석
2. UI에서 쓸 권한 Boolean 제공
3. 버튼/입력 활성화 여부를 단일 출처로 통제

---

### 3️⃣ ViewModel 필수 속성 세트 (정답)

```csharp
public class DocumentViewModel : ViewModelBase
{
    public DocumentState State { get; }

    // ✍️ 입력 가능 여부
    public bool CanEdit { get; }
    public bool CanEditItems { get; }

    // 📤 전송
    public bool CanSend { get; }

    // ✅ 확정
    public bool CanConfirm { get; }

    // 🔁 수정 요청 / 새 버전
    public bool CanRequestRevision { get; }
    public bool CanCreateNewVersion { get; }

    // 🧾 출력 / 정산
    public bool CanPrint { get; }
    public bool CanIncludeInSettlement { get; }
}
```

👉 **UI는 이 Boolean만 본다.**

---

### 4️⃣ 상태 → Boolean 매핑 규칙 (표)

**🔑 핵심 매핑 테이블**

| State | CanEdit | CanSend | CanConfirm | CanRequestRevision | CanCreateNewVersion | CanIncludeInSettlement |
|-------|---------|---------|------------|-------------------|---------------------|------------------------|
| `Draft` | ⭕ | ⭕ | ❌ | ❌ | ❌ | ❌ |
| `Sent` | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| `Received` | ❌ | ❌ | ⭕ | ⭕ | ❌ | ❌ |
| `RevisionRequested` | ❌ | ❌ | ❌ | ❌ | ⭕ | ❌ |
| `Confirmed` | ❌ | ❌ | ❌ | ❌ | ❌ | ⭕ |
| `Superseded` | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| `Cancelled` | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |

---

### 5️⃣ ViewModel 구현 패턴 (중요)

**❌ 나쁜 예:**
```csharp
if (State == DocumentState.Draft)
{
    SendButtonEnabled = true;
}
```

**⭕ 정답 패턴:**
```csharp
public bool CanSend => State == DocumentState.Draft;
public bool CanConfirm => State == DocumentState.Received;
public bool CanEdit => State == DocumentState.Draft;
public bool CanIncludeInSettlement => State == DocumentState.Confirmed;
```

👉 **조건은 여기서 끝**

---

### 6️⃣ XAML 바인딩 규칙 (정석)

**버튼:**
```xaml
<Button
    Content="전송"
    IsEnabled="{Binding CanSend}"
    Command="{Binding SendCommand}" />
```

**입력 필드:**
```xaml
<TextBox
    Text="{Binding Memo}"
    IsReadOnly="{Binding CanEdit, Converter={StaticResource InverseBoolConverter}}" />
```

**DataGrid 전체 잠금:**
```xaml
<DataGrid
    ItemsSource="{Binding Items}"
    IsReadOnly="{Binding CanEditItems, Converter={StaticResource InverseBoolConverter}}" />
```

---

### 7️⃣ 상태 변경 시 ViewModel 갱신 규칙

**상태 전이 발생 시:**

1. Domain 상태 변경
2. ViewModel State 업데이트
3. 모든 Boolean PropertyChanged 발생

```csharp
void OnStateChanged(DocumentState newState)
{
    State = newState;
    RaisePropertyChanged(nameof(State));
    RaisePropertyChanged(nameof(CanEdit));
    RaisePropertyChanged(nameof(CanSend));
    RaisePropertyChanged(nameof(CanConfirm));
    // ... 모든 Boolean 속성
}
```

👉 **귀찮아도 전부 명시**

---

### 8️⃣ Command 실행 가능 여부도 State로 통제

**ICommand 패턴:**
```csharp
public ICommand SendCommand =>
    new RelayCommand(
        ExecuteSend,
        () => CanSend
    );
```

👉 **버튼 IsEnabled + Command CanExecute 이중 안전장치**

---

### 9️⃣ ViewModel 분리 규칙 (중요)

**절대 섞지 마라:**

- `DocumentViewModel`
- `SettlementViewModel`
- `LogViewModel`

👉 **State 기준이 다르다**

---

### 🔟 디버깅 체크리스트

- [ ] State 바뀌었는데 버튼 안 바뀐다 → PropertyChanged 누락
- [ ] XAML에서 State 직접 비교한다 → 설계 위반
- [ ] Code-behind에서 Enable 처리한다 → 바로 리팩토링

---

### 핵심 문장

> **"State는 Domain의 언어고, Boolean은 UI의 언어다."**

**이 둘을 섞지 않으면 UI는 절대 꼬이지 않는다.**

---

## API ↔ State Synchronization Rules (User Provided)

### 🌐 API ↔ State 동기화 규칙

**⚠️ 중복 전송 / 동시성 방지 설계**

---

### 0️⃣ 대원칙 (먼저 박아둬라)

1. **서버는 진실의 근원이 아니다**
2. **상태 결정권은 항상 클라이언트(EXE)에 있다**
3. **서버는 상태 전이의 "중계·검증자"다**
4. **Idempotency(멱등성)는 필수**

---

### 1️⃣ 상태 동기화 모델 (Single-Writer 원칙)

**문서 기준:**

- **작성자(송신자)**: `DRAFT → SENT` 전이의 단독 권한
- **수신자**: `RECEIVED → CONFIRMED` / `REVISION_REQUESTED` 전이의 단독 권한

👉 **한 상태 전이에 한 명의 작성자만 존재**
👉 **이게 동시성의 90%를 없앤다.**

---

### 2️⃣ API 엔드포인트 & 상태 전이 책임

| API | 호출 주체 | 허용 전이 |
|-----|----------|----------|
| `POST /documents` | 송신자 | `DRAFT → SENT` |
| `PATCH /documents/{id}/received` | 수신자 | `SENT → RECEIVED` |
| `POST /documents/{id}/confirm` | 수신자 | `RECEIVED → CONFIRMED` |
| `POST /documents/{id}/revision` | 수신자 | `RECEIVED → REVISION_REQUESTED` |

❌ **서버가 임의로 상태 바꾸는 API 없음**

---

### 3️⃣ 요청 Payload 필수 필드 (동시성 핵심)

**모든 상태 변경 요청에는 아래 4개가 반드시 포함된다:**

```json
{
  "documentId": "TS-2026-00231",
  "clientState": "RECEIVED",
  "stateVersion": 5,
  "idempotencyKey": "a3f9e1c4-..."
}
```

**필드 의미:**

- `clientState`: 내가 보고 있는 상태
- `stateVersion`: 상태 변경 횟수
- `idempotencyKey`: 중복 요청 방지 키

---

### 4️⃣ 서버의 검증 로직 (필수)

**서버는 이 3가지만 검증한다:**

**✅ Rule 1. 상태 일치 검증**
```
요청.clientState == 서버.state
아니면 → 409 Conflict
```

**✅ Rule 2. 상태 버전 검증**
```
요청.stateVersion == 서버.stateVersion
아니면 → 409 Conflict
```

**✅ Rule 3. 멱등성 키 검증**
```
idempotencyKey 이미 처리됨?
→ 이전 결과 그대로 반환
```

👉 **서버는 계산 ❌**
👉 **판단 ❌**
👉 **검증만 ⭕**

---

### 5️⃣ 중복 전송 방지 (Send 버튼 연타)

**클라이언트 규칙:**

`SendCommand` 실행 시:
1. 버튼 즉시 `Disabled`
2. 로컬 상태를 `SENT_PENDING` (UI 전용)으로 변경
3. 서버 응답 성공 시 → `SENT`

**서버 규칙:**

동일 `documentId` + `idempotencyKey` 재요청:
- 새 처리 ❌
- 이전 응답 반환

---

### 6️⃣ 동시 확정 방지 (가장 위험)

**시나리오:**
- 수신자 PC 2대
- 동시에 "확정" 클릭

**방어 구조:**

1. **수신자 단일 권한**
   - 수신 회사 ID 기준으로 1명만 허용

2. **stateVersion 체크**
   - 먼저 처리된 쪽만 성공
   - 나머지는 `409 Conflict`

**클라이언트 처리:**
```
409 수신 →
"이미 상태가 변경되었습니다"
→ 최신 상태 재동기화
```

---

### 7️⃣ 네트워크 실패 대비 규칙 (Fail-safe)

**요청 실패 시:**
- 상태 변경 ❌
- UI는 이전 상태 유지
- 로그에 "전송 실패" 기록

**타임아웃 시:**
- 재시도 가능
- 같은 `idempotencyKey` 재사용

👉 **재시도 = 안전**

---

### 8️⃣ 오프라인 → 온라인 복구 전략

**로컬 큐:**
- 상태 변경 요청은 로컬 큐에 기록
- 온라인 시 순차 재전송

**서버 응답에 따라:**
- 성공 → 큐 제거
- `409` → 상태 재동기화 후 큐 폐기

---

### 9️⃣ 서버 응답 표준

**성공:**
```json
{
  "result": "OK",
  "newState": "CONFIRMED",
  "stateVersion": 6
}
```

**충돌:**
```json
{
  "result": "CONFLICT",
  "currentState": "CONFIRMED",
  "stateVersion": 6
}
```

---

### 🔟 절대 하면 안 되는 패턴

- ❌ 서버에서 상태 강제 변경
- ❌ 클라이언트 `stateVersion` 없이 요청
- ❌ 멱등성 키 없는 POST
- ❌ 서버에 비즈니스 판단 위임

---

### 체크리스트 (출시 전)

- [ ] Send/Confirm 버튼 연타해도 중복 안 생김
- [ ] 네트워크 끊었다 연결해도 상태 복구됨
- [ ] 409 발생 시 UI가 정상 복원됨
- [ ] 서버 로그에 상태 충돌 이력 남음

---

### 핵심 문장

> **"서버는 판단하지 않고, 클라이언트는 믿지 않는다."**

**이 균형이 대규모에서도 상태 꼬임을 0에 가깝게 만든다.**

---

## Recovery Scenarios (User Provided)

### 🔄 복구 시나리오 (로컬 EXE · 중계 서버 · State 중심)

---

### 0️⃣ 최상위 원칙 (절대 규칙)

1. **로컬 상태가 1차 진실**
2. **서버는 검증자 + 힌트 제공자**
3. **모든 상태 전이는 되돌릴 수 있어야 한다**
4. **사용자에게 '판단' 맡기지 않는다 (자동 복구)**

---

### 1️⃣ 공통 장치 (모든 시나리오에 공통)

**📦 로컬 상태 스냅샷**

상태 전이 직전 스냅샷 저장

**저장 항목:**
- `documentId`
- `state`
- `stateVersion`
- `timestamp`

```sql
document_state_snapshot (
    document_id,
    state,
    state_version,
    saved_at
)
```

👉 **실패 시 즉시 롤백 가능**

---

**📥 로컬 전송 큐 (Outbox Pattern)**

```sql
outbox_queue (
    queue_id,
    document_id,
    target_state,
    payload,
    idempotency_key,
    retry_count,
    status -- PENDING / DONE / FAILED
)
```

👉 **네트워크 실패 = 큐에 남김**

---

### 2️⃣ 시나리오 A — 전송 중 네트워크 끊김

**상황:**
- 사용자가 [전송] 클릭
- 서버 요청 중 네트워크 끊김

**위험:**
- 로컬은 `SENT`로 바뀌었는데 서버는 모름

**방어 시나리오:**

**처리 흐름:**
1. 상태 전이 전 → 스냅샷 저장
2. 로컬 상태를 `SENT_PENDING` (UI 전용)
3. 요청 실패
4. 상태를 `DRAFT`로 자동 복구
5. Outbox에 요청 기록 (`PENDING`)

**UI 표시:**
- 상태: "전송 실패 (재시도 대기)"
- 전송 버튼: 활성화 ❌

**복구 트리거:**
- 네트워크 복구
- 프로그램 재실행

👉 **자동 재전송**

---

### 3️⃣ 시나리오 B — 서버는 성공, 응답 못 받음

**상황:**
- 서버는 상태 변경 완료
- 응답 전에 클라이언트 종료

**위험:**
- 재시도 시 중복 처리

**방어 시나리오:**

**처리 흐름:**
1. 재실행 시 Outbox 확인
2. 같은 `idempotencyKey`로 재요청
3. 서버:
   - 이미 처리됨 → 이전 결과 반환
4. 로컬 상태:
   - 서버 응답 기준으로 동기화

👉 **멱등성 키가 생명**

---

### 4️⃣ 시나리오 C — 상태 충돌 (409 Conflict)

**상황:**
- 다른 PC에서 먼저 확정
- 현재 PC는 이전 상태를 보고 있음

**서버 응답:**
```json
{
  "result": "CONFLICT",
  "currentState": "CONFIRMED",
  "stateVersion": 6
}
```

**방어 시나리오:**

**처리 흐름:**
1. 현재 요청 즉시 폐기
2. 서버 상태로 강제 동기화
3. 로컬 상태 업데이트
4. 모든 입력 컨트롤 `Disabled`

**사용자 UX:**
```
"이미 확정된 문서입니다.
최신 상태로 갱신되었습니다."
```

👉 **사용자 판단 없음**

---

### 5️⃣ 시나리오 D — 프로그램 강제 종료 / 크래시

**상황:**
- 상태 변경 중 EXE 종료

**방어 시나리오:**

**재실행 시:**
1. 스냅샷 존재 여부 확인
2. Outbox `PENDING` 확인
3. 서버 상태 조회 (`GET /documents/{id}`)

**분기:**
- 서버 상태 = 로컬 상태 → 정상 진행
- 서버 상태 ≠ 로컬 상태 → 서버 상태로 복구

---

### 6️⃣ 시나리오 E — 서버 장시간 다운

**상황:**
- 하루 이상 서버 응답 없음

**방어 시나리오:**

**허용되는 작업:**
- `DRAFT` 작성
- 수정
- 로컬 저장
- 출력 (PDF/Excel)

**차단되는 작업:**
- 전송
- 확정
- 수정 요청

**UX:**
- 상단 상태바: "오프라인 모드"
- 버튼 자동 `Disabled`

👉 **업무 마비 방지**

---

### 7️⃣ 시나리오 F — Outbox 재시도 실패 반복

**상황:**
- 동일 요청 5회 이상 실패

**처리 규칙:**
- `status = FAILED`
- 자동 재시도 중단

**사용자 UX:**
```
"네트워크 문제로 전송되지 않았습니다.
나중에 자동으로 재시도됩니다."
```

👉 **수동 조치 요구 ❌**

---

### 8️⃣ 시나리오 G — 데이터 손상 (로컬 DB)

**상황:**
- SQLite 파일 손상

**방어 시나리오:**

**처리 흐름:**
1. 최근 자동 백업 탐색
2. 최신 백업 복구
3. 서버 상태와 재동기화
4. Outbox 재처리

---

### 9️⃣ 복구 우선순위 (중요)

복구 시 신뢰 순서:

1. **CONFIRMED 상태 보존**
2. **상태Version 최신 값**
3. **서버 응답**
4. **로컬 스냅샷**
5. **사용자 입력**

👉 **항상 거래 확정이 최우선**

---

### 🔟 구현 체크리스트

- [ ] 모든 상태 전이 전에 스냅샷 저장됨
- [ ] 모든 POST에 `idempotencyKey` 있음
- [ ] Outbox 큐 재처리 로직 존재
- [ ] 409 처리 UX 자동화
- [ ] 서버 없이도 읽기/출력 가능

---

### 핵심 문장

> **"실패는 예외가 아니라 정상 흐름이다."**

**이 전제에서 설계하면:**
- 네트워크 ❌
- 서버 ❌
- 크래시 ❌

**모두 업무 중단으로 이어지지 않는다.**

---

## Document Hash & Integrity Verification (User Provided)

### 🔐 문서 해시 / 위변조 검증 설계 (로컬 EXE · 중계 서버 · State 중심)

---

### 0️⃣ 핵심 정의 (먼저 박아둬라)

**문서의 '의미 있는 내용'이 단 1바이트라도 바뀌면 해시는 반드시 달라진다.**

- **해시는 무결성 증명**
- **전자서명은 주체 증명**
- **이 시스템은 무결성 우선, 주체 보조**

---

### 1️⃣ 해시 대상 범위 (가장 중요)

**❌ 해시에 포함하면 안 되는 것:**
- 출력 포맷
- 공백/줄바꿈
- UI 전용 필드
- 정렬 순서(가변)

**⭕ 해시에 포함해야 할 것 (Canonical Payload):**
- 문서ID
- 버전번호
- 송신회사ID
- 수신회사ID
- 거래일
- 품목 리스트 (
  - 품목명
  - 수량
  - 단가
  - 옵션
  )
- 합계금액
- 정산 기준 키

👉 **"거래 의미"만 포함**

---

### 2️⃣ Canonicalization 규칙 (절대 고정)

**해시 전 항상 동일한 형태로 직렬화한다.**

**규칙:**
1. JSON Key 정렬 (사전순)
2. 숫자 포맷 고정
   - 수량/단가: 소수점 고정 (예: 2자리)
3. 문자열 Trim
4. 배열 정렬 기준 고정 (입력 순서 or ID)

**예시 (Canonical JSON):**
```json
{
  "docId":"TS-2026-00021",
  "version":2,
  "from":"A001",
  "to":"B014",
  "date":"2026-01-05",
  "items":[
    {"name":"상품A","qty":10.00,"price":12000.00,"opt":"블랙"},
    {"name":"상품B","qty":5.00,"price":8000.00,"opt":""}
  ],
  "total":160000.00
}
```

---

### 3️⃣ 해시 알고리즘 선택

**✅ SHA-256 (권장)**
- 빠름
- 충돌 가능성 현실적으로 무시
- 법적 분쟁에서 인정도 높음

```csharp
byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalJson));
```

---

### 4️⃣ 해시 생성 타이밍 (중요)

**생성 시점:**
- 전송 직전
- 확정 직전

**저장 위치:**
- 로컬 DB: `documents.content_hash`
- 서버 DB: `documents.content_hash` (메타)

👉 **서버는 해시만 알고 내용은 모른다**

---

### 5️⃣ 상태 전이 × 해시 규칙

| 상태 전이 | 해시 |
|---------|------|
| `DRAFT → DRAFT` | 재생성 |
| `DRAFT → SENT` | 확정 해시 생성 |
| `RECEIVED → CONFIRMED` | 해시 재검증 |
| `CONFIRMED → (종결)` | 변경 불가 |

---

### 6️⃣ 위변조 검증 흐름 (Step-by-Step)

**🔍 수신 시 검증:**

1. 서버에서 받은 해시 값 확보
2. 로컬에서 Canonical JSON 재생성
3. SHA-256 계산
4. 값 비교

```
hash_local == hash_received ? OK : TAMPERED
```

**❌ 불일치 시 처리:**
- 상태: `TAMPERED` (UI 전용)
- 모든 버튼 `Disabled`
- 경고 메시지 표시
- 로그 기록

```
"문서 내용이 변경되었습니다.
거래 확정을 중단합니다."
```

---

### 7️⃣ 서버 역할 (절대 최소)

**서버는:**
- 해시 저장 ✅
- 해시 비교 ❌
- 내용 해석 ❌

👉 **검증은 항상 클라이언트**

---

### 8️⃣ PDF / 출력 시 무결성 표시

**출력 하단 자동 삽입:**
```
문서 해시(SHA-256):
A9F3E2C7...
확정 일시: 2026-01-05 14:22
```

👉 **출력물도 검증 가능**

---

### 9️⃣ 선택 옵션: 주체 증명(고급)

**RSA/ECDSA 전자서명 (선택)**

- 회사별 Private Key
- 해시에 서명
- 서버에는 Public Key 등록

```csharp
signature = Sign(privateKey, hash)
```

👉 **법적 분쟁 대응력 ↑**
👉 **MVP에는 필수 아님**

---

### 🔟 공격 시나리오 방어

| 공격 | 결과 |
|------|------|
| 엑셀로 열어 수정 | 해시 불일치 |
| DB 직접 수정 | 해시 불일치 |
| 파일 복붙 | 해시 불일치 |
| 서버 변조 | 서버는 내용 모름 |

---


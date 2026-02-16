# UI/UX 문제점 분석

## 1. 글꼴(폰트) 문제
- index.css에 `font-family: 'Pretendard'`가 선언되어 있으나, 실제 Pretendard 웹폰트 로딩(CDN/import)이 없음
- 결과: 브라우저가 Pretendard를 찾지 못해 system-ui/sans-serif 폴백으로 렌더링
- 수정: index.html에 Pretendard CDN 추가 필요

## 2. 사이드바/메뉴 간격 문제
- 섹션 타이틀: `text-[10px]` → 너무 작아서 가독성 떨어짐
- 메뉴 아이템 간격: `mb-0.5` (2px) → 너무 좁아서 메뉴가 붙어 보임
- 섹션 간격: `mb-6` → 적절하나 메뉴 아이템 간격과 불균형
- nav padding: `p-4 py-4` → 중복 선언, 상하 여백 부족

## 3. padding/margin/spacing 불일관
- 콘텐츠 영역: `p-7` 일관 → 양호
- 카드 내부: `p-6` 일관 → 양호
- 하지만 Topbar h-16과 콘텐츠 사이 간격 없음
- 페이지 헤더 mb-6, 필터 mb-5, 테이블 헤더 mb-3 → 불일관

## 4. 컴포넌트 스타일 문제
- 테이블 헤더: `uppercase tracking-wide` → 한국어에 불필요, 공간 낭비
- 테이블 셀: px-5 py-3.5 → 양호하나 일부 모달 내부 테이블은 px-3 py-2로 불일관
- 버튼: btn-primary, btn-secondary 잘 정의되어 있으나 일부 인라인 스타일 버튼 존재
- select/input: 일관된 스타일 → 양호

## 5. 반응형 레이아웃 문제 (심각)
- 사이드바: 고정 `w-64`, 모바일 대응 없음
- 대시보드 그리드: `grid-cols-4` 고정, 반응형 breakpoint 없음
- 필터 그리드: `grid-cols-4` 고정
- 하단 그리드: `grid-cols-3` 고정
- 설정 그리드: `grid-cols-2` 고정
- 리포트 그리드: `grid-cols-3` 고정
- Topbar 검색: `w-52` 고정

## 6. 네이비 톤 디자인
- 사이드바 그라데이션: `from-[#2E4A7A] via-[#3B5998] to-[#4A6FA5]` → 올바름
- 로그인 배경: 동일 그라데이션 → 올바름
- @theme 커스텀 색상: 올바르게 정의
- tailwind.config.js: 올바르게 정의
- 일부 하드코딩: Login.tsx에 `focus:border-[#3B5998]` → primary 토큰 사용해야 함

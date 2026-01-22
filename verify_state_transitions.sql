-- 상태 전이 검증 스크립트
-- 테스트 후 실행하여 상태 변경 및 로그 기록 확인

-- 1. 문서 상태 확인
SELECT
    DocumentId,
    CASE State
        WHEN 0 THEN '작성중(Draft)'
        WHEN 1 THEN '전송됨(Sent)'
        WHEN 2 THEN '수신됨(Received)'
        WHEN 3 THEN '수정요청(RevisionRequested)'
        WHEN 4 THEN '확정됨(Confirmed)'
        WHEN 5 THEN '구버전(Superseded)'
        WHEN 6 THEN '취소됨(Cancelled)'
    END AS CurrentState,
    StateVersion,
    TotalAmount,
    datetime(CreatedAt, 'localtime') AS Created,
    datetime(SentAt, 'localtime') AS Sent,
    datetime(ConfirmedAt, 'localtime') AS Confirmed
FROM Documents
ORDER BY DocumentId;

-- 2. 상태 전이 로그 확인
SELECT
    LogId,
    DocumentId,
    CASE FromState
        WHEN 0 THEN '작성중'
        WHEN 1 THEN '전송됨'
        WHEN 2 THEN '수신됨'
        WHEN 3 THEN '수정요청'
        WHEN 4 THEN '확정됨'
    END AS FromState,
    CASE ToState
        WHEN 0 THEN '작성중'
        WHEN 1 THEN '전송됨'
        WHEN 2 THEN '수신됨'
        WHEN 3 THEN '수정요청'
        WHEN 4 THEN '확정됨'
    END AS ToState,
    ChangedBy,
    datetime(ChangedAt, 'localtime') AS ChangedAt,
    Reason
FROM DocumentStateLogs
ORDER BY ChangedAt DESC;

-- 3. 테스트 기대값
-- DOC-2026-0001: Draft(0) -> Sent(1), StateVersion=1, SentAt NOT NULL
-- DOC-2026-0002: Received(2) -> Confirmed(4), StateVersion=1, ConfirmedAt NOT NULL
-- StateLog 테이블에 2개 이상의 레코드 존재

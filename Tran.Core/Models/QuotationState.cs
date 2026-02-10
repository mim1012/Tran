namespace Tran.Core.Models;

public enum QuotationState
{
    Draft,              // 작성중
    Sent,               // 발송됨
    UnderReview,        // 검토중
    Confirmed,          // 확정
    RevisionRequested,  // 수정요청
    Expired             // 만료
}

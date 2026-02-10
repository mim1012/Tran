namespace Tran.Core.Models;

public enum PurchaseState
{
    PendingDelivery,      // 입고대기
    PartiallyDelivered,   // 부분입고
    Delivered,            // 입고완료
    Cancelled             // 취소
}

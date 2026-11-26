using System.ComponentModel.DataAnnotations;



namespace Entities
{
    public enum TransactionStatus
    {
        [Display(Name = "در انتظار پرداخت")]
        Pending = 0,

        [Display(Name = "پرداخت موفق")]
        Success = 1,

        [Display(Name = "پرداخت ناموفق")]
        Failed = 2,

        [Display(Name = "لغو شده")]
        Cancelled = 3,

        [Display(Name = "برگشت وجه")]
        Refunded = 4
    }
}
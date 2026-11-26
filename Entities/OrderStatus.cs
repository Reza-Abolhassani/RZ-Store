using System.ComponentModel.DataAnnotations;

namespace Entities
{
    public enum OrderStatus
    {
        [Display(Name = "پرداخت نشده")]
        NotPayed = 0,

        [Display(Name = "در انتظار پردازش")]
        Pending = 1,

        [Display(Name = "ارسال شده")]
        Shipped = 2,

        [Display(Name = "تحویل داده شده")]
        Delivered = 3,

        [Display(Name = "لغو شده")]
        Cancelled = 4,

        [Display(Name = "مرجوع شده")]
        Returned = 5
    }
}




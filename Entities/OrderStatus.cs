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


//using System.ComponentModel.DataAnnotations;

//namespace Entities
//{
//    public enum OrderStatus
//    {
//        [Display(Name = "پرداخت نشده")]
//        NotPayed,

//        [Display(Name = "در حال پردازش  ")]
//        Pending,

//        [Display(Name = "ارسال شده ")]
//        Delivered,

//        [Display(Name = "لغو شده")]
//        Cancelled,

//        [Display(Name = "مرجوع شده")]
//        Returned
//    }
//}

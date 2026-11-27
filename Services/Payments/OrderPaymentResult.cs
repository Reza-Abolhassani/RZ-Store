#nullable enable
using System;

namespace Services.Payments
{
    public class OrderPaymentResult
    {
        // آیا عملیات موفق بوده است؟
        public bool IsSuccess { get; set; }

        // آدرس انتقال به درگاه
        public string? PaymentUrl { get; set; }

        // شناسه پرداخت داخلی
        public Guid? PaymentId { get; set; }

        // توضیحات نتیجه یا خطا
        public string? Message { get; set; }
    }
}
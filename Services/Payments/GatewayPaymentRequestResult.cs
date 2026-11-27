namespace Services.Payments
{
    public class GatewayPaymentRequestResult
    {
        // موفق یا ناموفق بودن ایجاد درخواست
        public bool IsSuccess { get; set; }

        // شناسه درخواست درگاه (Authority)
        public string? Authority { get; set; }

        // آدرس انتقال کاربر به درگاه
        public string? PaymentUrl { get; set; }

        // کد خطای درگاه
        public string? ErrorCode { get; set; }

        // توضیحات خطا
        public string? ErrorMessage { get; set; }

        public bool IsIndeterminate { get; set; }
    }
}
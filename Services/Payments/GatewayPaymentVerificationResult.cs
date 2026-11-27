#nullable enable

namespace Services.Payments
{
    public class GatewayPaymentVerificationResult
    {
        // موفق یا ناموفق بودن تأیید پرداخت
        public bool IsSuccess { get; set; }

        // مشخص بودن یا نبودن نتیجه تأیید پرداخت
        public bool IsIndeterminate { get; set; }

        // کد پیگیری پرداخت درگاه
        public string? TrackingCode { get; set; }

        // کد خطای درگاه
        public string? ErrorCode { get; set; }

        // توضیحات خطا
        public string? ErrorMessage { get; set; }
    }
}
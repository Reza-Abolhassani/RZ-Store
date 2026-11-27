namespace Services.Payments
{
    public class GatewayPaymentCallbackResult
    {
        // آیا کاربر از درگاه به سایت برگشته است؟
        public bool IsCallbackReceived { get; set; }

        // شناسه درخواست درگاه (Authority)
        public string? Authority { get; set; }

        // وضعیت پرداخت اعلام‌شده توسط درگاه
        public string? Status { get; set; }
    }
}
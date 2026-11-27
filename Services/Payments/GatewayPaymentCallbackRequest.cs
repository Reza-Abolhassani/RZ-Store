namespace Services.Payments
{
    public class GatewayPaymentCallbackRequest
    {
        // شناسه درخواست درگاه (Authority)
        public string? Authority { get; set; }

        // وضعیت اعلام‌شده توسط درگاه
        public string? Status { get; set; }
    }
}
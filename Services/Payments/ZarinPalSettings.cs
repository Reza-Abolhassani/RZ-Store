namespace Services.Payments
{
    public class ZarinPalSettings
    {
        // شناسه پذیرنده زرین‌پال
        public string MerchantId { get; set; } = string.Empty;

        // تعیین محیط آزمایشی یا واقعی
        public bool IsSandbox { get; set; }

        // آدرس پایه API درگاه
        public string ApiBaseUrl { get; set; } = string.Empty;
    }
}
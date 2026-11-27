namespace Services.Payments
{
    public class GatewayPaymentRequest
    {
        // مبلغ پرداخت به تومان
        public long Amount { get; set; }

        // آدرس بازگشت از درگاه
        public string CallbackUrl { get; set; }

        // توضیحات پرداخت
        public string Description { get; set; }

        // شماره موبایل کاربر (اختیاری)
        public string? Mobile { get; set; }

        // ایمیل کاربر (اختیاری)
        public string? Email { get; set; }
    }
}
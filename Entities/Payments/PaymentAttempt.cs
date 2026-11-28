using System;

namespace Entities.Payments
{
    public class PaymentAttempt : BaseEntity
    {
        // ارتباط با پرداخت اصلی
        public Guid PaymentId { get; set; }
        public Payment Payment { get; set; }

        // اطلاعات درگاه
        public string GatewayName { get; set; }
        public string? GatewayRequestId { get; set; }
        public string? TrackingCode { get; set; }

        // مبلغ درخواست‌شده از درگاه
        public long Amount { get; set; }

        // وضعیت و نتیجه تلاش
        public PaymentAttemptStatus Status { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }

        // زمان‌های تلاش
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        // زمان شروع تأیید پرداخت
        public DateTime? VerificationStartedAt { get; set; }

        // کنترل هم‌زمانی تغییرات
        public byte[] RowVersion { get; set; }

        public string? PaymentUrl { get; set; }
    }
}
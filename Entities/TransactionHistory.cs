using System;
using Entities.Payments;

namespace Entities
{
    public class TransactionHistory : BaseEntity
    {
        public long Amount { get; set; }
        public string Description { get; set; }
        public TransactionStatus Status { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; }

        public TransactionType Type { get; set; }

        // ارتباط اختیاری با پرداخت
        public Guid? PaymentId { get; set; }
        public Payment? Payment { get; set; }
    }
}
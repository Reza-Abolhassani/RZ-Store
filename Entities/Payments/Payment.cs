using System;

using System.Collections.Generic;

namespace Entities.Payments
{
    public class Payment : BaseEntity
    {
        // ارتباط با کاربر
        public Guid UserId { get; set; }
        public User User { get; set; }

        // ارتباط اختیاری با سفارش
        public Guid? OrderId { get; set; }
        public Order? Order { get; set; }

        // اطلاعات مبلغ
        public long Amount { get; set; }
        public long WalletAmount { get; set; }
        public long GatewayAmount { get; set; }

        // نوع، روش و وضعیت پرداخت
        public PaymentType Type { get; set; }
        public PaymentMethod Method { get; set; }
        public PaymentStatus Status { get; set; }

        // اطلاعات تکمیلی
        public string? Description { get; set; }

        public DateTime? PaidAt { get; set; }

        public ICollection<PaymentAttempt> PaymentAttempts { get; set; }
            = new List<PaymentAttempt>();

        public ICollection<TransactionHistory> TransactionHistories { get; set; }
            = new List<TransactionHistory>();
    }
}
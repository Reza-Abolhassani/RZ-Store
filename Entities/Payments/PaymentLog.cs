using System;

namespace Entities.Payments
{
    public class PaymentLog : BaseEntity
    {
        public User User { get; set; }
        public Guid UserId { get; set; }
        public string RefId { get; set; }
        public string Description { get; set; }
        public long TotalPrice { get; set; }
    }
}

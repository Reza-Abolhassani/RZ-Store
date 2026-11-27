using System;

namespace Entities.Setting
{
    public class DiscountCode : BaseEntity
    {
        public string Code { get; set; }
        public DateTime DueDate { get; set; }
        public int Used { get; set; }
        public long Price { get; set; }
        public int Percentage { get; set; }
        public long MaxDiscountAmount { get; set; }
        public int MaxUsageCount { get; set; }
        public User User { get; set; }
        public Guid? UserId { get; set; }
    }
}
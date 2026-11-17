using System;

namespace Entities
{
    public class TempUser : BaseEntity
    {
        public string UserName { get; set; }
        public string OTP { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime UsedAt { get; set; }

    }
}

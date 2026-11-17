using System;

namespace Entities
{
    public class LoginCode : BaseEntity
    {
        public string UserName { get; set; }
        public string OTP { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime UsedAt { get; set; }
        public int FailedAttempts { get; set; }
    }
}

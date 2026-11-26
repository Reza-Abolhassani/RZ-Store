using System;

namespace Entities
{
    public class UserWallet : BaseEntity
    {
        public long Amount { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
        public byte[] RowVersion { get; set; }
    }
}
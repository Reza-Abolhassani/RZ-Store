using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class UserWallet : BaseEntity
    {
        public int Amount { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
    }
}

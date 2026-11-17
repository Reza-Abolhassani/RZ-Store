using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;

namespace Entities
{
    public class TransactionHistory : BaseEntity
    {

        public int Amount { get; set; }
        public string Description { get; set; }
        public TransactionStatus Status { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
    }
}

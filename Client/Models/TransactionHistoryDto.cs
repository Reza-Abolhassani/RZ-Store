using Entities;
using System;
using System.Collections.Generic;
using System.Transactions;
using WebFramework.Api;

namespace Client.Models
{
    public class TransactionHistoryDto : BaseDto<TransactionHistoryDto,TransactionHistory>
    {
        public int Amount { get; set; }
        public string Description { get; set; }
        public TransactionStatus Status { get; set; }
        public DateTime CreationDateTime { get; set; }
        public Guid UserId { get; set; }
        public List<UserDto> users { get; set; }
    }
}

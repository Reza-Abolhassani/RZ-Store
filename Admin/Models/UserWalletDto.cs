using Entities;
using System;
using System.Collections.Generic;
using WebFramework.Api;

namespace Admin.Models
{
    public class UserWalletDto : BaseDto<UserWalletDto, UserWallet>
    {
        public long Amount { get; set; }
        public Guid UserId { get; set; }
        public List<UserDto> users { get; set; }
    }
}
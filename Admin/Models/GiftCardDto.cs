using Entities;
using System;
using WebFramework.Api;

namespace Admin.Models
{
    public class GiftCardDto : BaseDto<GiftCardDto, GiftCard>
    {
        public string Code { get; set; }
        public long Amount { get; set; }
        public bool Isused { get; set; }
        public DateTime ExpireDate { get; set; }
    }
}

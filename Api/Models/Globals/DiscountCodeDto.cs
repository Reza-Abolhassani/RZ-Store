using Entities.Setting;

using System;

using WebFramework.Api;

namespace API.Models.Globals
{
    public class DiscountCodeDto : BaseDto<DiscountCodeDto, DiscountCode>
    {
        public string Code { get; set; }
        public DateTime DueDate { get; set; }
        public int Used { get; set; }
        public int Price { get; set; }
        public int Percentage { get; set; }
        public long MaxDiscountAmount { get; set; }
        public int MaxUsageCount { get; set; }
        public string? UserFirstName { get; set; }
        public string? UserLastName { get; set; }
        public string? UserUserName { get; set; }
        public string? UserPhoneNumber { get; set; }
        public Guid? UserId { get; set; }
    }
}

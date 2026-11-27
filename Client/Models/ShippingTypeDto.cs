using Entities;
using WebFramework.Api;

namespace Client.Models
{
    public class ShippingTypeDto : BaseDto<ShippingTypeDto, ShippingType>
    {
        public string Name { get; set; }
        public long Price { get; set; }
    }
}

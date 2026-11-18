using Entities;
using WebFramework.Api;

namespace Client.Models
{
    public class ShippingTypeDto : BaseDto<ShippingTypeDto, ShippingType>
    {
        public string Name { get; set; }
        public int Price { get; set; }
    }
}

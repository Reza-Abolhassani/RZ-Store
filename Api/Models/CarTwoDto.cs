using Entities;
using WebFramework.Api;

namespace API.Models
{
    public class CarTwoDto : BaseDto<CarTwoDto,CarTwo>
    {
        public string Name { get; set; }
        public string Color { get; set; }
    }
}

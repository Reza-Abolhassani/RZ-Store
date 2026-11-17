using Entities;

using WebFramework.Api;

namespace API.Models
{
    public class CarDto : BaseDto<CarDto, Car>
    {
        public string Name { get; set; }
        public string Color { get; set; }
        public int YearTolid { get; set; }
    }
}

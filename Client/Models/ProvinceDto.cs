using Entities;
using WebFramework.Api;

namespace Client.Models
{
    public class ProvinceDto : BaseDto<ProvinceDto, Province>
    {
        public string Name { get; set; }
    }
}

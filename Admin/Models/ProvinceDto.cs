using Entities;
using WebFramework.Api;

namespace Admin.Models
{
    public class ProvinceDto : BaseDto<ProvinceDto, Province>
    {
        public string Name { get; set; }
    }
}

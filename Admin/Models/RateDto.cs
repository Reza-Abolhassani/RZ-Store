using Entities;
using WebFramework.Api;

namespace Admin.Models
{
    public class RateDto : BaseDto<RateDto, Rate>
    {
        public int Score { get; set; }
        public string Comment { get; set; }
    }
}

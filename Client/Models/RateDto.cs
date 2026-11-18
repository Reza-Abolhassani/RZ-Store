using Entities;
using WebFramework.Api;

namespace Client.Models
{
    public class RateDto : BaseDto<RateDto, Rate>
    {
        public int Score { get; set; }
        public string Comment { get; set; }
    }
}

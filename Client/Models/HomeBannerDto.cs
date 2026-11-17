using Entities;
using WebFramework.Api;

namespace Admin.Models
{
    public class HomeBannerDto : BaseDto<HomeBannerDto, HomeBanner>
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Link { get; set; }
        public string ButtonText { get; set; }
        public int Position { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; }
    }
}
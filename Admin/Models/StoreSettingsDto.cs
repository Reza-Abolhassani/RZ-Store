using Entities.Setting;
using Microsoft.AspNetCore.Http;
using WebFramework.Api;

namespace Admin.Models
{
    public class StoreSettingsDto : BaseDto<StoreSettingsDto, StoreSettings>
    {
        public string SiteName { get; set; }

        public string Logo { get; set; }

        public IFormFile LogoFile { get; set; }

        public string PhoneNumber { get; set; }

        public string Email { get; set; }

        public string Address { get; set; }

        public string Description { get; set; }

        public string Instagram { get; set; }

        public string Telegram { get; set; }
    }
}

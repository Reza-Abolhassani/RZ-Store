using Entities.Setting;

using WebFramework.Api;

namespace API.Models
{
    public class SettingDto : BaseDto<SettingDto, Setting>
    {
        public string Banner1Image { get; set; }
        public string Banner1Text { get; set; }
        public string Banner1Link { get; set; }
        public string Banner2Image { get; set; }
        public string Banner2Text { get; set; }
        public string Banner2Link { get; set; }
        public string Banner3Image { get; set; }
        public string Banner3Text { get; set; }
        public string Banner3Link { get; set; }
        public string Banner4Image { get; set; }
        public string Banner4Text { get; set; }
        public string Banner4Link { get; set; }
        public string Banner5Image { get; set; }
        public string Banner5Text { get; set; }
        public string Banner5Link { get; set; }
    }
}

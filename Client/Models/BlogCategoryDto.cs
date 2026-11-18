using Entities;
using WebFramework.Api;

namespace Client.Models
{
    public class BlogCategoryDto : BaseDto<BlogCategoryDto, BlogCategory>
    {
        public string Name { get; set; }
    }
}

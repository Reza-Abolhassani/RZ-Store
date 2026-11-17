using Entities;
using WebFramework.Api;

namespace Admin.Models
{
    public class BlogCategoryDto : BaseDto<BlogCategoryDto, BlogCategory>
    {
        public string Name { get; set; }
    }
}

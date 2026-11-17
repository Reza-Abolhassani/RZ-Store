using Entities;
using WebFramework.Api;

namespace API.Models
{
    public class CategoryBookDto : BaseDto<CategoryBookDto,CategoryBook>
    {
        public string Title { get; set; }
        public int Order { get; set; }
    }
}

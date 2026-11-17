using Entities;

using System;

using WebFramework.Api;

namespace API.Models
{
    public class ProductCategoryDto : BaseDto<ProductCategoryDto, ProductCategory>
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public Guid MotherId { get; set; }

    }
}

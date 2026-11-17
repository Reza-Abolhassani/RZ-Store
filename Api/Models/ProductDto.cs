using Entities;

using Microsoft.AspNetCore.Http;

using System;

using WebFramework.Api;

namespace API.Models
{
    public class ProductDto:BaseDto<ProductDto,Product>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public IFormFile ImageFile { get; set; }
        public long RawPrice { get; set; }
        public long Discount { get; set; }
        public long TotalPrice { get; set; }
        public int Count { get; set; }
        public Guid ProductCategoryId { get; set; }

        public string ProductCategoryName { get; set; } //tavasot automapper (projectTo) automatic por mishe
    }
}

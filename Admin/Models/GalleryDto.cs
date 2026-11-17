using Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using WebFramework.Api;

namespace Admin.Models
{
    public class GalleryDto : BaseDto<GalleryDto, Gallery>
    {
        public string ImageLink { get; set; }
        public Guid ProductId { get; set; }
        public string ProductsName { get; set; }
        public List<ProductDto> Products { get; set; }
        public IFormFile? ImageFile { get; set; }

        public GalleryDto()
        {
            Products = new List<ProductDto>();
        }
    }
}

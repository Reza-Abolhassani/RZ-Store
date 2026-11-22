using Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using WebFramework.Api;

namespace Client.Models
{
    public class ProductDto : BaseDto<ProductDto,Product>
    {

        public string Title { get; set; }
        public int Count { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public int RawPrice { get; set; }
        public int Discount { get; set; }
        public int Total { get; set; }
        public string SKU { get; set; }
        public string ImageUrl { get; set; }
        public IFormFile ImageFile { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryTitle { get; set; }
        public List<CategoryDto> categories { get; set; }

        public int ShopCartItemCount { get; set; }

        public List<GalleryDto> galleries { get; set; }
        public ProductDto()
        {
            categories = new List<CategoryDto>();
            galleries = new List<GalleryDto>();
        }
    }
}

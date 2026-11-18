using Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using WebFramework.Api;

namespace Client.Models
{
    public class BlogDto : BaseDto<BlogDto, Blog>
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }

        public IFormFile ImageFile { get; set; }
        public Guid BlogCategoryId { get; set; }
        public string BlogCategoriesName { get; set; }
        public List<BlogCategoryDto> BlogCategories { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public string Author { get; set; }
        public BlogDto()
        {
            BlogCategories = new List<BlogCategoryDto>();
        }
    }
}

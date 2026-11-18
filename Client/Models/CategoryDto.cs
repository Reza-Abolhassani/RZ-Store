using Entities;
using Microsoft.AspNetCore.Http;
using System;


using WebFramework.Api;

namespace Client.Models
{
    public class CategoryDto : BaseDto<CategoryDto, Category>
    {

        public string Title { get; set; }
        public int Order { get; set; }
        public string ImageUrl { get; set; }
        public IFormFile ImageFile { get; set; }
        public string Description { get; set; }
    }
}

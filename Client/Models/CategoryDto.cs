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

        public Guid? ParentId { get; set; }

        public string ParentTitle { get; set; }

        public System.Collections.Generic.List<CategoryDto> Children { get; set; }
    = new System.Collections.Generic.List<CategoryDto>();

        public override void CustomMappings(
    AutoMapper.IMappingExpression<Category, CategoryDto> mapping)
        {
            mapping.ForMember(
                dest => dest.ParentTitle,
                opt => opt.MapFrom(src =>
                    src.Parent != null ? src.Parent.Title : "دسته اصلی")
            );
        }
    }
}

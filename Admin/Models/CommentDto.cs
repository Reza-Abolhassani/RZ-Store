using AutoMapper;
using Entities;
using Entities.Constants;
using System;
using System.Collections.Generic;
using WebFramework.Api;

namespace Admin.Models
{
    public class CommentDto : BaseDto<CommentDto, Comment>
    {
        public string AuthorName { get; set; }
        public string Email { get; set; }
        public string Content { get; set; }
       

        public CommentStatus Status { get; set; }
        public DateTime CreationDateTime { get; set; }
        public Guid ProductId { get; set; }
        public string ProductsName { get; set; }
        public List<ProductDto> Products { get; set; }
        public CommentDto()
        {
            Products = new List<ProductDto>();

        }

        public override void CustomMappings(IMappingExpression<Comment, CommentDto> mapping)
        {
            mapping.ForMember(
                dest => dest.ProductsName,
                opt => opt.MapFrom(src => src.Product.Title)
            );
        }
    }
}

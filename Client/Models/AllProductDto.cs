using System;
using System.Collections.Generic;

namespace Client.Models
{
    public class AllProductDto
    {
        public Guid ProductId { get; set; }

        public List<ProductDto> products { get; set; }

        public SettingDto Settings { get; set; }

        public Guid CategoryId { get; set; }

        public List<CategoryDto> Categories { get; set; }

        public AllProductDto()
        {
            products = new List<ProductDto>();
            Categories = new List<CategoryDto>();
        }
    }
}
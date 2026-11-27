using Entities;
using System;
using System.Collections.Generic;
using WebFramework.Api;

namespace Admin.Models
{
    public class AmazingProductsDto : BaseDto<AmazingProductsDto, AmazingProduct>
    {
        public Guid ProductId { get; set; }
        public ProductDto Product { get; set; }
        public string ProductsName { get; set; }
        public long ProductsTotalPrice { get; set; }
        public List<ProductDto> Products { get; set; }
        public int DiscountPercent { get; set; }
        public long ToatalPrice { get; set; }
    }
}

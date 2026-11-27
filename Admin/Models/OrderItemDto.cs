using Entities;
using System;
using System.Collections.Generic;
using WebFramework.Api;

namespace Admin.Models
{
    public class OrderItemDto : BaseDto<OrderItemDto,OrderItem>
    {


        public int Count { get; set; }
        public long Discount { get; set; }
        public long Total { get; set; } //mablaghe koll = count * productprice => totalprice
        public long ProductPrice { get; set; }
        public Guid OrderId { get; set; }
        public List<OrderDto> orders { get; set; }

        public Guid ProductId { get; set; }

        public List<ProductDto> products { get; set; }
        public string ProductTitle { get; set; }
    }
}

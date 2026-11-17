using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class OrderItem : BaseEntity
    {

        public int Count { get; set; }
        public int Discount { get; set; }
        public int Total { get; set; } //mablaghe koll = count * productprice => totalprice
        public int ProductPrice { get; set; }
        public Guid OrderId { get; set; }
        public Order Order { get; set; }
        public Guid ProductId { get; set; }
        public Product Product { get; set; }
    }
}

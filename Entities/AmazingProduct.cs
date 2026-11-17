using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class AmazingProduct : BaseEntity
    {
        public Guid ProductId { get; set; }
        public Product Product { get; set; }
        public int DiscountPercent { get; set; }
        public int ToatalPrice { get; set; }
    }
}

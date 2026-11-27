using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class ShopCardItem : BaseEntity
    {
        public Guid ShopCartId { get; set; }
        public ShopCart ShopCart { get; set; }

        public Guid ProductId { get; set; }
        public Product Product { get; set; }

        public int count { get; set; }

        public long TotalPrice { get; set; }
    }
}

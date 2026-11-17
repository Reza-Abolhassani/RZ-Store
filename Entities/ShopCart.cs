using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class ShopCart : BaseEntity
    {
        public Guid UserId { get; set; }

        public User User { get; set; }
        public int TotalRawPrice { get; set; }
        public int TotalPrice { get; set; }
        public int DiscountPrice { get; set; }
        public ICollection<ShopCardItem> Items { get; set; }
    }
}

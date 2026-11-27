using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class ShopCart : BaseEntity
    {
        public Guid UserId { get; set; }

        public User User { get; set; }
        public long TotalRawPrice { get; set; }
        public long TotalPrice { get; set; }
        public long DiscountPrice { get; set; }
        public ICollection<ShopCardItem> Items { get; set; }
    }
}

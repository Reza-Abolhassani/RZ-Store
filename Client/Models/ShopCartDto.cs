using System;
using System.Collections.Generic;

namespace Client.Dto
{
    public class ShopCartDto
    {
        public Guid Id { get; set; }

        public long TotalRawPrice { get; set; }

        public long TotalPrice { get; set; }

        public long DiscountPrice { get; set; }

        public List<ShopCardItemDto> Items { get; set; }
    }
}
using System;
using System.Collections.Generic;

namespace Client.Dto
{
    public class ShopCartDto
    {
        public Guid Id { get; set; }

        public int TotalRawPrice { get; set; }

        public int TotalPrice { get; set; }

        public int DiscountPrice { get; set; }

        public List<ShopCardItemDto> Items { get; set; }
    }
}
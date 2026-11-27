using System;

namespace Client.Dto
{
    public class ShopCardItemDto
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }

        public string ProductTitle { get; set; }

        public string ImageUrl { get; set; }

        public long RawPrice { get; set; }

        public long Price { get; set; }

        public long TotalPrice { get; set; }

        public int count { get; set; }
    }
}
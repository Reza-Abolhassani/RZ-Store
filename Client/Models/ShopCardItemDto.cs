using System;

namespace Client.Dto
{
    public class ShopCardItemDto
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }

        public string ProductTitle { get; set; }

        public string ImageUrl { get; set; }

        public int RawPrice { get; set; }

        public int Price { get; set; }

        public int TotalPrice { get; set; }

        public int count { get; set; }
    }
}
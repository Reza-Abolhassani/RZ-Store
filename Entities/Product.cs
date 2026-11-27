using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class Product : BaseEntity
    {

        public string Title { get; set; }
        public int Count { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public long RawPrice { get; set; }
        public int Discount { get; set; }
        public long Total { get; set; }
        public string SKU { get; set; }
        public string ImageUrl { get; set; }
        public Guid CategoryId { get; set; }
        public Category Category { get; set; }
    }
}

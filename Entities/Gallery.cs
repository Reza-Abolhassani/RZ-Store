using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class Gallery : BaseEntity
    {
        public string ImageLink { get; set; }
        public Guid ProductId { get; set; }
        public Product Product { get; set; }
    }
}

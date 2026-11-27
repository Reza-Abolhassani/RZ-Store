using System;
using System.Collections.Generic;

namespace Client.Models
{
    public class ProductSearchDto
    {

        public long FromPrice { get; set; }
        public long ToPrice { get; set; }
        public string Text { get; set; }
        public Guid CategoryId { get; set; }

        public List<ProductDto> Products { get; set; }
        public List<CategoryDto> Categories { get; set; }
    }
}
